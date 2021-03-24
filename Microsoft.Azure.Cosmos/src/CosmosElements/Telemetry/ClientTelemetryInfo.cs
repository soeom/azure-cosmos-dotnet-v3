//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.CosmosElements
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Azure.Cosmos.CosmosElements.Telemetry;

    internal class ClientTelemetryInfo
    {
        private string timeStamp { get; set; }
        private string clientId { get; }
        private string processId { get; }
        private string userAgent { get; }
        private ConnectionMode connectionMode { get; }
        private string globalDatabaseAccountName { get; }
        private string applicationRegion { get; }
        private string hostEnvInfo { get; }
        private Boolean acceleratedNetworking { get; }
       /* private IDictionary<ReportPayload, LongHistogram> systemInfoMap { get; set; }
        private IDictionary<ReportPayload, LongHistogram> cacheRefreshInfoMap { get; set; }
        private IDictionary<ReportPayload, LongHistogram> operationInfoMap { get; set; }*/

        public ClientTelemetryInfo(string clientId,
                                   string processId,
                                   string userAgent,
                                   ConnectionMode connectionMode,
                                   string globalDatabaseAccountName,
                                   string applicationRegion,
                                   string hostEnvInfo,
                                   Boolean acceleratedNetworking)
        {
            this.clientId = clientId;
            this.processId = processId;
            this.userAgent = userAgent;
            this.connectionMode = connectionMode;
            this.globalDatabaseAccountName = globalDatabaseAccountName;
            this.applicationRegion = applicationRegion;
            this.hostEnvInfo = hostEnvInfo;
            this.acceleratedNetworking = acceleratedNetworking;

           /* this.systemInfoMap = new Dictionary<ReportPayload, LongHistogram>();
            this.cacheRefreshInfoMap = new Dictionary<ReportPayload, LongHistogram>();
            this.operationInfoMap = new Dictionary<ReportPayload, LongHistogram>();*/
        }

    }
}
