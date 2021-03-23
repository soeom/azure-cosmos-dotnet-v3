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
    using Newtonsoft.Json.Linq;

    internal class ClientTelemetry
    {
        private readonly ClientTelemetryInfo clientTelemetryInfo;

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
            Console.Write(httpClient);
            Console.Write(isClientTelemetryEnabled);

        }

        private async Task<AzureVMMetadata> LoadAzureVmMetaDataAsync()
        {
            using HttpClient httpClient = new HttpClient();
            using HttpRequestMessage httpRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "http://169.254.169.254/metadata/instance?api-version=2020-06-01");
            httpRequest.Headers.Add("Metadata", "true");

            try
            {
                using HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequest);
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

        public static void RecordValue(LongHistogram histogram, long value)
        {
            try
            {
                histogram.RecordValue(value);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ResponseMessage> initAsync()
        {
            await this.LoadAzureVmMetaDataAsync();

            return new ResponseMessage();

        }
    }
}
