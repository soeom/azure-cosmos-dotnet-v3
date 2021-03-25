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
        public bool greaterThan1Kb { get; set; }
        public ConsistencyLevel consistency { get; set; }
        public string databaseName { get; set; }
        public string containerName { get; set; }
        public OperationType operation { get; set; }
        public ResourceType resource { get; set; }
        public int statusCode { get; set; }
        public string operationId { get; set; }
        public MetricInfo metricInfo { get; set; }
        public ReportPayload(string metricInfoName, string unitName)
        {
            this.metricInfo = new MetricInfo(metricInfoName, unitName);
        }
    }
}
