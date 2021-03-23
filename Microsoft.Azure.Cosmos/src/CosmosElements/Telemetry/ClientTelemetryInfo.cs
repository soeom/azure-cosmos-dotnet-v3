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
        private String timeStamp { get; set; }
        private String clientId { get; set; }
        private String processId { get; set; }
        private String userAgent { get; set; }
        private ConnectionMode connectionMode { get; set; }
        private String globalDatabaseAccountName { get; set; }
        private String applicationRegion { get; set; }
        private String hostEnvInfo { get; set; }
        private Boolean acceleratedNetworking { get; set; }
        private IDictionary<ReportPayload, LongHistogram> systemInfoMap { get; set; }
        private IDictionary<ReportPayload, LongHistogram> cacheRefreshInfoMap { get; set; }
        private IDictionary<ReportPayload, LongHistogram> operationInfoMap { get; set; }

        public ClientTelemetryInfo(String clientId,
                                   String processId,
                                   String userAgent,
                                   ConnectionMode connectionMode,
                                   String globalDatabaseAccountName,
                                   String applicationRegion,
                                   String hostEnvInfo,
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
