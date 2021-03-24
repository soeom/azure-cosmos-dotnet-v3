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
    using Microsoft.Azure.Cosmos.CosmosElements;
    using Microsoft.Azure.Cosmos.CosmosElements.Telemetry;
    using Microsoft.Azure.Cosmos.Tracing;
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json.Linq;

    internal class ClientTelemetry
    {
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

        public async Task<ResponseMessage> initAsync()
        {
            //if (this.isClientTelemetryEnabled)
            await this.LoadAzureVmMetaDataAsync();

            return new ResponseMessage();

        }
    }
}
