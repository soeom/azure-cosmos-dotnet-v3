//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Routing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Common;
    using Microsoft.Azure.Cosmos.Tracing;
    using Microsoft.Azure.Cosmos.Tracing.TraceData;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Collections;

    /// <summary>
    /// Caches collection information.
    /// </summary>
    internal class ClientCollectionCache : CollectionCache
    {
        private readonly IStoreModel storeModel;
        private readonly IAuthorizationTokenProvider tokenProvider;
        private readonly IRetryPolicyFactory retryPolicy;
        private readonly ISessionContainer sessionContainer;

        public ClientCollectionCache(ISessionContainer sessionContainer, IStoreModel storeModel, IAuthorizationTokenProvider tokenProvider, IRetryPolicyFactory retryPolicy)
        {
            if (storeModel == null)
            {
                throw new ArgumentNullException("storeModel");
            }

            this.storeModel = storeModel;
            this.tokenProvider = tokenProvider;
            this.retryPolicy = retryPolicy;
            this.sessionContainer = sessionContainer;
        }

        protected override Task<ContainerProperties> GetByRidAsync(string apiVersion, string collectionRid, CancellationToken cancellationToken, ITrace trace)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IDocumentClientRetryPolicy retryPolicyInstance = new ClearingSessionContainerClientRetryPolicy(this.sessionContainer, this.retryPolicy.GetRequestPolicy());
            return TaskHelper.InlineIfPossible(
                  () => this.ReadCollectionAsync(PathsHelper.GeneratePath(ResourceType.Collection, collectionRid, false), cancellationToken, retryPolicyInstance, trace),
                  retryPolicyInstance,
                  cancellationToken);
        }

        protected override Task<ContainerProperties> GetByNameAsync(string apiVersion, string resourceAddress, CancellationToken cancellationToken, ITrace trace)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IDocumentClientRetryPolicy retryPolicyInstance = new ClearingSessionContainerClientRetryPolicy(this.sessionContainer, this.retryPolicy.GetRequestPolicy());
            return TaskHelper.InlineIfPossible(
                () => this.ReadCollectionAsync(resourceAddress, cancellationToken, retryPolicyInstance, trace),
                retryPolicyInstance,
                cancellationToken);
        }

        private async Task<ContainerProperties> ReadCollectionAsync(string collectionLink,
                                                                    CancellationToken cancellationToken,
                                                                    IDocumentClientRetryPolicy retryPolicyInstance,
                                                                    ITrace trace)
        {
            using (ITrace childTrace = trace.StartChild("Read Collection", TraceComponent.Transport, TraceLevel.Info))
            { 
                cancellationToken.ThrowIfCancellationRequested();

                using (DocumentServiceRequest request = DocumentServiceRequest.Create(
                       OperationType.Read,
                       ResourceType.Collection,
                       collectionLink,
                       AuthorizationTokenType.PrimaryMasterKey,
                       new StoreRequestNameValueCollection()))
                {
                    request.Headers[HttpConstants.HttpHeaders.XDate] = DateTime.UtcNow.ToString("r");
                    request.RequestContext.ClientRequestStatistics = new ClientSideRequestStatisticsTraceDatum(DateTime.UtcNow);

                    (string authorizationToken, string payload) = await this.tokenProvider.GetUserAuthorizationAsync(
                        request.ResourceAddress,
                        PathsHelper.GetResourcePath(request.ResourceType),
                        HttpConstants.HttpMethods.Get,
                        request.Headers,
                        AuthorizationTokenType.PrimaryMasterKey);

                    request.Headers[HttpConstants.HttpHeaders.Authorization] = authorizationToken;

                    using (new ActivityScope(Guid.NewGuid()))
                    {
                        if (retryPolicyInstance != null)
                        {
                            retryPolicyInstance.OnBeforeSendRequest(request);
                        }

                        try
                        {
                            using (DocumentServiceResponse response = await this.storeModel.ProcessMessageAsync(request))
                            {
                                childTrace.AddDatum("Client Side Request Stats", request.RequestContext.ClientRequestStatistics);
                                return CosmosResource.FromStream<ContainerProperties>(response);
                            }
                        }
                        catch (DocumentClientException ex)
                        {
                            childTrace.AddDatum("Exception Message", ex.Message);
                            throw;
                        }
                    }
                }
            }
        }
    }
}
