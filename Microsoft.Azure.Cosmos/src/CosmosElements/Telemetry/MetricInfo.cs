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
        internal String metricsName { get; set; }
        internal String unitName { get; set; }
        internal double mean { get; set; }
        internal long count { get; set; }
        internal double min { get; set; }
        internal double max { get; set; }
        internal IDictionary<Double, Double> percentiles { get; set; }
    }
}
