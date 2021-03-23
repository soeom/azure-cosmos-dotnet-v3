//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.CosmosElements.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class AzureVMMetadata
    {
        private string Location { get; set; }
        private string SKU { get; set; }
        private string AzEnvironment { get; set; }
        private string OSType { get; set; }
        private string VMSize { get; set; }
    }
}
