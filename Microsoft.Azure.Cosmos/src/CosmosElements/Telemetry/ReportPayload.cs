//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.CosmosElements.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Azure.Documents;

    internal class ReportPayload
    {
        public string regionsContacted { get; set; }
        public Boolean greaterThan1Kb { get; set; }
        public ConsistencyLevel consistency { get; set; }
        public string databaseName { get; set; }
        public string containerName { get; set; }
        public OperationType operation { get; set; }
        public ResourceType resource { get; set; }
        public int statusCode { get; set; }
        public MetricInfo metricInfo { get; set; }
        public ReportPayload(string metricInfoName, string unitName)
        {
            this.metricInfo = new MetricInfo(metricInfoName, unitName);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            hash = (hash * 397) ^ (this.regionsContacted == null ? 0 : this.regionsContacted.GetHashCode());
            hash = (hash * 397) ^ (this.greaterThan1Kb.GetHashCode());
            hash = (hash * 397) ^ (this.consistency.GetHashCode());
            hash = (hash * 397) ^ (this.databaseName == null ? 0 : this.databaseName.GetHashCode());
            hash = (hash * 397) ^ (this.containerName == null ? 0 : this.containerName.GetHashCode());
            hash = (hash * 397) ^ (this.operation.GetHashCode());
            hash = (hash * 397) ^ (this.resource.GetHashCode());
            hash = (hash * 397) ^ (this.statusCode.GetHashCode());
            hash = (hash * 397) ^ (this.metricInfo == null ? 0 : this.metricInfo.metricsName == null ? 0 :
                this.metricInfo.metricsName.GetHashCode());
            return hash;
        }

        public override bool Equals(object obj)
        {
            bool isequal = obj is ReportPayload payload &&
                   this.regionsContacted != null && payload.regionsContacted != null && this.regionsContacted.Equals(payload.regionsContacted) &&
                   this.greaterThan1Kb.Equals(payload.greaterThan1Kb) &&
                   this.consistency.GetTypeCode().Equals(payload.consistency.GetTypeCode()) &&
                   this.databaseName != null && payload.databaseName != null && this.databaseName.Equals(payload.databaseName) &&
                   this.containerName != null && payload.containerName != null && this.containerName.Equals(payload.containerName) &&
                   this.operation.GetTypeCode().Equals(payload.operation.GetTypeCode()) &&
                   this.resource.GetTypeCode().Equals(payload.resource.GetTypeCode()) &&
                   this.statusCode.GetTypeCode().Equals(payload.statusCode.GetTypeCode()) &&
                   this.metricInfo != null && this.metricInfo.metricsName != null && payload.metricInfo != null && payload.metricInfo.metricsName != null && this.metricInfo.metricsName.Equals(payload.metricInfo.metricsName);

            return isequal;
        }
    }
}
