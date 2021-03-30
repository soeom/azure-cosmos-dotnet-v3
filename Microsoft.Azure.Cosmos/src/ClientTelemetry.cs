//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using HdrHistogram;
    using Microsoft.Azure.Cosmos.CosmosElements;
    using Microsoft.Azure.Cosmos.CosmosElements.Telemetry;
    using Microsoft.Azure.Cosmos.Tracing;
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json.Linq;

    internal class ClientTelemetry
    {
        internal const int OneKbToBytes = 1024;

        internal const int RequestLatencyMaxMicroSec = 300000000;
        internal const int RequestLatencySuccessPrecision = 4;
        internal const int RequestLatencyFailurePrecision = 2;
        internal const string RequestLatencyName = "RequestLatency";
        internal const string RequestLatencyUnit = "MicroSec";

        internal const int RequestChargeMax = 10000;
        internal const int RequestChargePrecision = 2;
        internal const string RequestChargeName = "RequestCharge";
        internal const string RequestChargeUnit = "RU";

        internal const string VMMetadataURL = "http://169.254.169.254/metadata/instance?api-version=2020-06-01";

        internal const double Percentile50 = 50.0;
        internal const double Percentile90 = 90.0;
        internal const double Percentile95 = 95.0;
        internal const double Percentile99 = 99.0;
        internal const double Percentile999 = 99.9;

        internal readonly ClientTelemetryInfo clientTelemetryInfo;
        internal readonly bool isClientTelemetryEnabled;
        internal readonly CosmosHttpClient httpClient;

        public ClientTelemetry(bool acceleratedNetworking,
                               string clientId,
                               string processId,
                               string userAgent,
                               ConnectionMode connectionMode,
                               string globalDatabaseAccountName,
                               string applicationRegion,
                               string hostEnvInfo,
                               CosmosHttpClient httpClient,
                               bool isClientTelemetryEnabled)
        {
            this.clientTelemetryInfo = new ClientTelemetryInfo(clientId, processId, userAgent, connectionMode,
                globalDatabaseAccountName, applicationRegion, hostEnvInfo, acceleratedNetworking);

            this.httpClient = httpClient;
            this.isClientTelemetryEnabled = isClientTelemetryEnabled;

        }

        internal async Task<AzureVMMetadata> LoadAzureVmMetaDataAsync()
        {
            AzureVMMetadata azMetadata = null;
            try
            {
                static ValueTask<HttpRequestMessage> CreateRequestMessage()
                {
                    HttpRequestMessage request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(VMMetadataURL)
                    };
                    request.Headers.Add("Metadata", "true");

                    return new ValueTask<HttpRequestMessage>(request);
                }
                using HttpResponseMessage httpResponseMessage = await this.httpClient.SendHttpAsync(
                    createRequestMessageAsync: CreateRequestMessage,
                    resourceType: ResourceType.Unknown,
                    timeoutPolicy: HttpTimeoutPolicyControlPlaneRead.Instance,
                    trace: NoOpTrace.Singleton,
                    cancellationToken: default);
                azMetadata = await ProcessResponseAsync(httpResponseMessage);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get Azure VM info:" + e.ToString());
            }

            return azMetadata;
        }

        internal static async Task<AzureVMMetadata> ProcessResponseAsync(HttpResponseMessage httpResponseMessage)
        {
            string jsonVmInfo = await httpResponseMessage.Content.ReadAsStringAsync();
            return JObject.Parse(jsonVmInfo).ToObject<AzureVMMetadata>();
        }

        internal void Collect(CosmosClient client,
                            HttpStatusCode statusCode,
                            string containerId,
                            string databaseId,
                            OperationType operationType,
                            ResourceType resourceType,
                            Microsoft.Azure.Documents.ConsistencyLevel consistencyLevel,
                            double requestCharge,
                            TimeSpan latency)
        {
            ReportPayload reportPayloadLatency = 
                this.CreateReportPayload(
                    client, statusCode, containerId, databaseId, operationType, 
                    resourceType, consistencyLevel, RequestLatencyName, RequestLatencyUnit);

            this.clientTelemetryInfo
                .operationInfoMap
                .TryGetValue(reportPayloadLatency, out LongHistogram latencyHistogram);
            if (latencyHistogram == null)
            {
                latencyHistogram = statusCode.IsSuccess()
                    ? new LongHistogram(RequestLatencyMaxMicroSec, RequestLatencySuccessPrecision)
                    : new LongHistogram(RequestLatencyMaxMicroSec, RequestLatencyFailurePrecision);
            }
            latencyHistogram.RecordValue((long)latency.TotalMilliseconds * 1000);
            this.clientTelemetryInfo.operationInfoMap[reportPayloadLatency] = latencyHistogram;

            ReportPayload reportPayloadRequestCharge =
               this.CreateReportPayload(
                   client, statusCode, containerId, databaseId, operationType,
                   resourceType, consistencyLevel, RequestChargeName, RequestChargeUnit);

            this.clientTelemetryInfo
                .operationInfoMap
                .TryGetValue(reportPayloadLatency, out LongHistogram requestChargeHistogram);

            if (requestChargeHistogram == null)
            {
                requestChargeHistogram = new LongHistogram(RequestChargeMax, RequestChargePrecision);
            }
            requestChargeHistogram.RecordValue((long)requestCharge);
            this.clientTelemetryInfo.operationInfoMap[reportPayloadRequestCharge] = requestChargeHistogram;

        }

        internal void Collect(CosmosClient client,
                            CosmosDiagnostics cosmosDiagnostics,
                            HttpStatusCode statusCode,
                            int objectSize,
                            string containerId,
                            string databaseId,
                            OperationType operationType,
                            ResourceType resourceType,
                            Microsoft.Azure.Documents.ConsistencyLevel consistencyLevel,
                            double requestCharge)
        {
            Console.WriteLine(requestCharge);
            ReportPayload reportPayloadLatency =
                this.CreateReportPayload(
                    client, cosmosDiagnostics, statusCode, objectSize, containerId, databaseId, operationType,
                    resourceType, consistencyLevel, RequestLatencyName, RequestLatencyUnit);

            this.clientTelemetryInfo
                .operationInfoMap
                .TryGetValue(reportPayloadLatency, out LongHistogram latencyHistogram);

            if (latencyHistogram == null)
            {
                latencyHistogram = statusCode.IsSuccess()
                    ? new LongHistogram(RequestLatencyMaxMicroSec, RequestLatencySuccessPrecision)
                    : new LongHistogram(RequestLatencyMaxMicroSec, RequestLatencyFailurePrecision);
            }
            latencyHistogram.RecordValue((long)cosmosDiagnostics.GetClientElapsedTime().TotalMilliseconds * 1000);
            this.clientTelemetryInfo.operationInfoMap[reportPayloadLatency] = latencyHistogram;

            ReportPayload reportPayloadRequestCharge =
               this.CreateReportPayload(
                   client, cosmosDiagnostics, statusCode, objectSize, containerId, databaseId, operationType,
                   resourceType, consistencyLevel, RequestChargeName, RequestChargeUnit);

            this.clientTelemetryInfo
                .operationInfoMap
                .TryGetValue(reportPayloadLatency, out LongHistogram requestChargeHistogram);

            if (requestChargeHistogram == null)
            {
                requestChargeHistogram = new LongHistogram(RequestChargeMax, RequestChargePrecision);
            }
            requestChargeHistogram.RecordValue((long)requestCharge);
            this.clientTelemetryInfo.operationInfoMap[reportPayloadRequestCharge] = requestChargeHistogram;
        }

        internal ReportPayload CreateReportPayload(CosmosClient client,
                                                  CosmosDiagnostics cosmosDiagnostics,
                                                  HttpStatusCode statusCode,
                                                  int objectSize,
                                                  string containerId,
                                                  string databaseId,
                                                  OperationType operationType,
                                                  ResourceType resourceType,
                                                  Microsoft.Azure.Documents.ConsistencyLevel consistencyLevel,
                                                  string metricsName,
                                                  string unitName)
        {
            IReadOnlyList<(string regionName, Uri uri)> regionList = cosmosDiagnostics.GetContactedRegions();
            IList<Uri> regionUris = new List<Uri>();
            foreach ((_, Uri uri) in regionList)
                regionUris.Add(uri);

            ReportPayload reportPayload = new ReportPayload(metricsName, unitName)
            {
                regionsContacted = regionUris.ToString(),
                consistency = consistencyLevel.ToString() == null ? client.ClientContext.DocumentClient.ConsistencyLevel : consistencyLevel,
                databaseName = databaseId,
                containerName = containerId,
                operation = operationType,
                resource = resourceType,
                statusCode = (int)statusCode
            };

            if (objectSize != 0)
            {
                reportPayload.greaterThan1Kb = objectSize > OneKbToBytes;
            }

            return reportPayload;
        }

        internal ReportPayload CreateReportPayload(CosmosClient client,
                                              HttpStatusCode statusCode,
                                              string containerId,
                                              string databaseId,
                                              OperationType operationType,
                                              ResourceType resourceType,
                                              Microsoft.Azure.Documents.ConsistencyLevel consistencyLevel,
                                              string metricsName,
                                              string unitName)
        {
            return new ReportPayload(metricsName, unitName) 
            { 
                consistency = consistencyLevel.ToString() == null ? client.ClientContext.DocumentClient.ConsistencyLevel : consistencyLevel,
                databaseName = databaseId,
                containerName = containerId,
                operation = operationType,
                resource = resourceType,
                statusCode = (int)statusCode
            };
        }

        internal void Read()
        {
            foreach (KeyValuePair<ReportPayload, LongHistogram> entry in this.clientTelemetryInfo.cacheRefreshInfoMap)
            {
                this.FillMetricsInfo(entry.Key, entry.Value);
            }
            foreach (KeyValuePair<ReportPayload, LongHistogram> entry in this.clientTelemetryInfo.operationInfoMap)
            {
                this.FillMetricsInfo(entry.Key, entry.Value);
            }
        }

        private void FillMetricsInfo(ReportPayload payload, LongHistogram histogram)
        {
            LongHistogram copyHistogram = (LongHistogram)histogram.Copy();
            payload.metricInfo.count = copyHistogram.TotalCount;
            payload.metricInfo.max = copyHistogram.GetMaxValue();
            payload.metricInfo.mean = copyHistogram.GetMean();
            IDictionary<Double, Double> percentile = new Dictionary<Double, Double>
            {
                { Percentile50,  copyHistogram.GetValueAtPercentile(Percentile50) },
                { Percentile90,  copyHistogram.GetValueAtPercentile(Percentile90) },
                { Percentile95,  copyHistogram.GetValueAtPercentile(Percentile95) },
                { Percentile99,  copyHistogram.GetValueAtPercentile(Percentile99) },
                { Percentile999, copyHistogram.GetValueAtPercentile(Percentile999) }
            };
            payload.metricInfo.percentiles = percentile;
        }

        internal async Task<AzureVMMetadata> InitAsync()
        {
            //if (this.isClientTelemetryEnabled)
            return await this.LoadAzureVmMetaDataAsync();
        }

    }
}
