// ----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Performance.Tests.Benchmarks
{
    using System;
    using BenchmarkDotNet.Attributes;
    using Microsoft.Azure.Documents;

    [MemoryDiagnoser]
    public class VectorTokenBenchmark
    {
        private static readonly string NonMultimasterSessionToken = "1#49252";
        private static readonly string NonMultimasterSessionToken2 = "1#99252";
        private readonly ISessionToken token1;
        private readonly ISessionToken token2;

        public VectorTokenBenchmark()
        {
            VectorSessionToken.TryCreate(
                VectorTokenBenchmark.NonMultimasterSessionToken,
                out this.token1);

            VectorSessionToken.TryCreate(
                NonMultimasterSessionToken,
                out this.token2);
        }

        [Benchmark]
        public void VectorTokenParse()
        {
            VectorSessionToken.TryCreate(
                NonMultimasterSessionToken,
                out ISessionToken sessionToken);
        }

        [Benchmark]
        public void VectorTokenMerge()
        {
            ISessionToken mergedToken = this.token1.Merge(this.token2);
            if(mergedToken == null)
            {
                throw new Exception("Merged token is null");
            }
        }
    }
}