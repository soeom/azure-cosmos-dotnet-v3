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
        private static readonly string NonMultimasterSessionToken = "0#101#3=-1";
        private static readonly string NonMultimasterSessionToken2 = "0#121#3=-1";
        private readonly ISessionToken vectorToken1;
        private readonly ISessionToken vectorToken2;
        private readonly ISessionToken oldVectorToken1;
        private readonly ISessionToken oldVectorToken2;

        public VectorTokenBenchmark()
        {
            VectorSessionToken.TryCreate(
                VectorTokenBenchmark.NonMultimasterSessionToken,
                out this.vectorToken1);

            VectorSessionToken.TryCreate(
                VectorTokenBenchmark.NonMultimasterSessionToken2,
                out this.vectorToken2);

            VectorSessionTokenOld.TryCreate(
                VectorTokenBenchmark.NonMultimasterSessionToken,
                out this.oldVectorToken1);

            VectorSessionTokenOld.TryCreate(
                VectorTokenBenchmark.NonMultimasterSessionToken2,
                out this.oldVectorToken2);
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
            ISessionToken mergedToken = this.vectorToken1.Merge(this.vectorToken2);
            if(mergedToken == null)
            {
                throw new Exception("Merged token is null");
            }
        }

        [Benchmark]
        public void VectorTokenParseOld()
        {
            VectorSessionTokenOld.TryCreate(
                NonMultimasterSessionToken,
                out ISessionToken sessionToken);
        }

        [Benchmark]
        public void VectorTokenMergeOld()
        {
            ISessionToken mergedToken = this.oldVectorToken1.Merge(this.oldVectorToken2);
            if (mergedToken == null)
            {
                throw new Exception("Merged token is null");
            }
        }
    }
}