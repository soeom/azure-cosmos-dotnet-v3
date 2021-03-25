//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.CosmosElements.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class MetricInfo
    {
        public MetricInfo(string metricsName, string unitName)
        {
            this.metricsName = metricsName;
            this.unitName = unitName;
        }
        private String metricsName { get; set; }
        private String unitName { get; set; }
        private double mean { get; set; }
        private long count { get; set; }
        private double min { get; set; }
        private double max { get; set; }
        private IDictionary<Double, Double> percentiles { get; set; }
    }
}
