//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Handler
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.CosmosElements;
    using Newtonsoft.Json.Linq;

    internal class TelemetryHandler : RequestHandler
    {
        private readonly CosmosClient client;

        public TelemetryHandler(CosmosClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public override async Task<ResponseMessage> SendAsync(
            RequestMessage request,
            CancellationToken cancellationToken)
        {
            ResponseMessage message = await base.SendAsync(request, cancellationToken);
            Console.WriteLine(message);
            return this.client.DocumentClient.clientTelemetry.Collect(client, message.Diagnostics, );
            
        }

    }
}