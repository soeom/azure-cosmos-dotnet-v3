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
        private String regionsContacted { get; set; }
        private Boolean greaterThan1Kb { get; set; }
        private ConsistencyLevel consistency { get; set; }
        private String databaseName { get; set; }
        private String containerName { get; set; }
        private OperationType operation { get; set; }
        private ResourceType resource { get; set; }
        private int statusCode { get; set; }
        private String operationId { get; set; }
        private MetricInfo metricInfo { get; set; }
    }
}
