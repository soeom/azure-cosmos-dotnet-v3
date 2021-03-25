//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.CosmosElements
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using HdrHistogram;
    using Microsoft.Azure.Cosmos.CosmosElements.Telemetry;

    internal class ClientTelemetryInfo
    {
        public string timeStamp { get; set; }
        public string clientId { get; }
        public string processId { get; }
        public string userAgent { get; }
        public ConnectionMode connectionMode { get; }
        public string globalDatabaseAccountName { get; }
        public string applicationRegion { get; }
        public string hostEnvInfo { get; }
        public Boolean acceleratedNetworking { get; }
        public IDictionary<ReportPayload, LongHistogram> systemInfoMap { get; set; }
        public IDictionary<ReportPayload, LongHistogram> cacheRefreshInfoMap { get; set; }
        public IDictionary<ReportPayload, LongHistogram> operationInfoMap { get; set; }
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
            this.systemInfoMap = new Dictionary<ReportPayload, LongHistogram>();
            this.cacheRefreshInfoMap = new Dictionary<ReportPayload, LongHistogram>();
            this.operationInfoMap = new Dictionary<ReportPayload, LongHistogram>();
        }

    }
}
