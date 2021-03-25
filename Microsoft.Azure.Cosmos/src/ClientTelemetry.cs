//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using HdrHistogram;
    using Microsoft.Azure.Cosmos.CosmosElements;
    using Microsoft.Azure.Cosmos.CosmosElements.Telemetry;
    using Microsoft.Azure.Cosmos.Tracing;
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json.Linq;

    internal class ClientTelemetry
    {
        public const int OneKbToBytes = 1024;

        public const int RequestLatencyMaxMicroSec = 300000000;
        public const int RequestLatencySuccessPrecision = 4;
        public const int RequestLatencyFailurePrecision = 2;
        public const string RequestLatencyName = "RequestLatency";
        public const string RequestLatencyUnit = "MicroSec";

        private readonly ClientTelemetryInfo clientTelemetryInfo;

        private readonly bool isClientTelemetryEnabled;
        private readonly CosmosHttpClient httpClient;

        public ClientTelemetry(Boolean acceleratedNetworking,
                          String clientId,
                          String processId,
                          String userAgent,
                          ConnectionMode connectionMode,
                          String globalDatabaseAccountName,
                          String applicationRegion,
                          String hostEnvInfo,
                          CosmosHttpClient httpClient,
                          bool isClientTelemetryEnabled)
        {
            this.clientTelemetryInfo = new ClientTelemetryInfo(clientId, processId, userAgent, connectionMode,
                globalDatabaseAccountName, applicationRegion, hostEnvInfo, acceleratedNetworking);

            this.httpClient = httpClient;
            this.isClientTelemetryEnabled = isClientTelemetryEnabled;

        }

        private async Task<AzureVMMetadata> LoadAzureVmMetaDataAsync()
        {
            try
            {
                static ValueTask<HttpRequestMessage> CreateRequestMessage()
                {
                    HttpRequestMessage request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri("http://169.254.169.254/metadata/instance?api-version=2020-06-01")
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

                string jsonVmInfo = await httpResponseMessage.Content.ReadAsStringAsync();
                AzureVMMetadata azMetadata = JObject.Parse(jsonVmInfo).ToObject<AzureVMMetadata>();
                Console.WriteLine(azMetadata);

                return azMetadata;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get Azure VM info:" + e.ToString());
                throw e;
            }
        }

        public void Collect(CosmosClient client,
                                    CosmosDiagnostics cosmosDiagnostics,
                                    int statusCode,
                                    int objectSize,
                                    String containerId,
                                    String databaseId,
                                    OperationType operationType,
                                    ResourceType resourceType,
                                    Microsoft.Azure.Documents.ConsistencyLevel consistencyLevel,
                                    float requestCharge)
        {
            ReportPayload reportPayloadLatency = 
                this.CreateReportPayload(
                    client, 
                    cosmosDiagnostics,
                    statusCode, 
                    objectSize, 
                    containerId, 
                    databaseId, 
                    operationType, 
                    resourceType, 
                    consistencyLevel,
                    RequestLatencyName,
                    RequestLatencyUnit);

            this.clientTelemetryInfo
                .operationInfoMap
                .TryGetValue(reportPayloadLatency, out LongHistogram latencyHistogram);
            if (latencyHistogram == null)
            {
                latencyHistogram = new LongHistogram(RequestLatencyMaxMicroSec, RequestLatencySuccessPrecision);
            }
            latencyHistogram.RecordValue((long)cosmosDiagnostics.GetClientElapsedTime().TotalSeconds);
            this.clientTelemetryInfo.operationInfoMap.Add(reportPayloadLatency, latencyHistogram);
            
        }

        private ReportPayload CreateReportPayload(CosmosClient client,
                                             CosmosDiagnostics cosmosDiagnostics,
                                             int statusCode,
                                             int objectSize,
                                             String containerId,
                                             String databaseId,
                                             OperationType operationType,
                                             ResourceType resourceType,
                                             Microsoft.Azure.Documents.ConsistencyLevel consistencyLevel,
                                             String metricsName,
                                             String unitName)
        {
            IReadOnlyList<(string regionName, Uri uri)> regionList = cosmosDiagnostics.GetContactedRegions();
            IList<Uri> regionUris = new List<Uri>();
            foreach ((_, Uri uri) in regionList)
                regionUris.Add(uri);

            ReportPayload reportPayload = new ReportPayload(metricsName, unitName)
            {
                regionsContacted = regionUris.ToString(),
                consistency = consistencyLevel,
                databaseName = databaseId,
                containerName = containerId,
                operation = operationType,
                resource = resourceType,
                statusCode = statusCode
            };

            if (objectSize != 0)
            {
                reportPayload.greaterThan1Kb = objectSize > OneKbToBytes;
            }

            return reportPayload;
        }

        public async Task<AzureVMMetadata> InitAsync()
        {
            //if (this.isClientTelemetryEnabled)
            return await this.LoadAzureVmMetaDataAsync();
        }

    }
}
