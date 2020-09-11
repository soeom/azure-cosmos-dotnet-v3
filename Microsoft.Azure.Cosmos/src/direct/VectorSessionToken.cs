//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Documents
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Microsoft.Azure.Cosmos.Core.Trace;

    /// <summary>
    /// Models vector clock bases session token. Session token has the following format:
    /// {Version}#{GlobalLSN}#{RegionId1}={LocalLsn1}#{RegionId2}={LocalLsn2}....#{RegionIdN}={LocalLsnN}
    /// 'Version' captures the configuration number of the partition which returned this session token.
    /// 'Version' is incremented everytime topology of the partition is updated (say due to Add/Remove/Failover).
    /// 
    /// The choice of separators '#' and '=' is important. Separators ';' and ',' are used to delimit
    /// per-partitionKeyRange session token
    /// session
    /// 
    /// We make assumption that instances of this class are immutable (read only after they are constructed), so if you want to change
    /// this behaviour please review all of its uses and make sure that mutability doesn't break anything.
    /// </summary>
    internal sealed class VectorSessionToken : ISessionToken
    {
        private static readonly IReadOnlyDictionary<uint, long> DefaultLocalLsnByRegion = new Dictionary<uint, long>(0);
        private const char SegmentSeparator = '#';
        private const string SegmentSeparatorString = "#";
        private const char RegionProgressSeparator = '=';
        private readonly string sessionToken;
        private readonly long version;
        private readonly IReadOnlyDictionary<uint, long> localLsnByRegion;

        private VectorSessionToken(long version, long globalLsn, IReadOnlyDictionary<uint, long> localLsnByRegion, string sessionToken = null)
        {
            this.version = version;
            this.LSN = globalLsn;
            this.localLsnByRegion = localLsnByRegion;
            this.sessionToken = sessionToken;

            if (this.sessionToken == null)
            {
                string regionProgress = null;
                if (!object.ReferenceEquals(localLsnByRegion, VectorSessionToken.DefaultLocalLsnByRegion) &&
                    localLsnByRegion.Any())
                {
                    regionProgress = string.Join(
                        VectorSessionToken.SegmentSeparatorString,
                        localLsnByRegion.Select(kvp => string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", kvp.Key, VectorSessionToken.RegionProgressSeparator, kvp.Value)));
                }

                if (string.IsNullOrEmpty(regionProgress))
                {
                    this.sessionToken = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}{1}{2}",
                        this.version,
                        VectorSessionToken.SegmentSeparatorString,
                        this.LSN);
                }
                else
                {
                    this.sessionToken = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}{1}{2}{3}{4}",
                        this.version,
                        VectorSessionToken.SegmentSeparatorString,
                        this.LSN,
                        VectorSessionToken.SegmentSeparatorString,
                        regionProgress);
                }
            }
        }

        public VectorSessionToken(VectorSessionToken other, long globalLSN)
            : this(other.version, globalLSN, other.localLsnByRegion)
        {
        }

        public static bool TryCreate(string sessionToken, out ISessionToken parsedSessionToken)
        {
            parsedSessionToken = null;

            if (VectorSessionToken.TryParseSessionToken(
                sessionToken,
                out long version,
                out long globalLsn,
                out IReadOnlyDictionary<uint, long> localLsnByRegion))
            {
                parsedSessionToken = new VectorSessionToken(version, globalLsn, localLsnByRegion, sessionToken);
                return true;
            }
            else
            {
                return false;
            }
        }

        public long LSN { get; }

        public bool Equals(ISessionToken obj)
        {
            if (!(obj is VectorSessionToken other))
            {
                return false;
            }

            return this.version == other.version
                && this.LSN == other.LSN
                && this.AreRegionProgressEqual(other.localLsnByRegion);
        }

        public bool IsValid(ISessionToken otherSessionToken)
        {
            if (!(otherSessionToken is VectorSessionToken other))
            {
                throw new ArgumentNullException(nameof(otherSessionToken));
            }

            if (other.version < this.version || other.LSN < this.LSN)
            {
                return false;
            }

            if (other.version == this.version && other.localLsnByRegion.Count != this.localLsnByRegion.Count)
            {
                throw new InternalServerErrorException(
                    string.Format(CultureInfo.InvariantCulture, RMResources.InvalidRegionsInSessionToken, this.sessionToken, other.sessionToken));
            }

            foreach (KeyValuePair<uint, long> kvp in other.localLsnByRegion)
            {
                uint regionId = kvp.Key;
                long otherLocalLsn = kvp.Value;
                long localLsn = -1;

                if (!this.localLsnByRegion.TryGetValue(regionId, out localLsn))
                {
                    // Region mismatch: other session token has progress for a region which is missing in this session token 
                    // Region mismatch can be ignored only if this session token version is smaller than other session token version
                    if (this.version == other.version)
                    {
                        throw new InternalServerErrorException(
                            string.Format(CultureInfo.InvariantCulture, RMResources.InvalidRegionsInSessionToken, this.sessionToken, other.sessionToken));
                    }
                    else
                    {
                        // ignore missing region as other session token version > this session token version
                    }
                }
                else
                {
                    // region is present in both session tokens.
                    if (otherLocalLsn < localLsn)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // Merge is commutative operation, so a.Merge(b).Equals(b.Merge(a))
        public ISessionToken Merge(ISessionToken obj)
        {
            if (!(obj is VectorSessionToken other))
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (this.version == other.version && this.localLsnByRegion.Count != other.localLsnByRegion.Count)
            {
                throw new InternalServerErrorException(
                    string.Format(CultureInfo.InvariantCulture, RMResources.InvalidRegionsInSessionToken, this.sessionToken, other.sessionToken));
            }

            VectorSessionToken sessionTokenWithHigherVersion;
            VectorSessionToken sessionTokenWithLowerVersion;

            if (this.version < other.version)
            {
                sessionTokenWithLowerVersion = this;
                sessionTokenWithHigherVersion = other;
            }
            else
            {
                sessionTokenWithLowerVersion = other;
                sessionTokenWithHigherVersion = this;
            }

            // There is no localLsnByRegion dictionary. If one of the existing tokens
            // has both the max version and max lsn then just return it instead of
            // creating a new one.
            if (this.version >= other.version &&
                this.LSN >= other.LSN &&
                AreAllTokenHigherInLocalLsnByRegions(
                    higherToken: this,
                    lowerToken: other))
            {
                return this;
            }
            else if (other.version >= this.version &&
                other.LSN >= this.LSN &&
                AreAllTokenHigherInLocalLsnByRegions(
                    higherToken: other,
                    lowerToken: this))
            {
                return other;
            }

            Dictionary<uint, long> highestLocalLsnByRegion = new Dictionary<uint, long>();
            foreach (KeyValuePair<uint, long> kvp in sessionTokenWithHigherVersion.localLsnByRegion)
            {
                uint regionId = kvp.Key;
                long localLsn1 = kvp.Value;
                long localLsn2 = -1;

                if (sessionTokenWithLowerVersion.localLsnByRegion.TryGetValue(regionId, out localLsn2))
                {
                    highestLocalLsnByRegion[regionId] = Math.Max(localLsn1, localLsn2);
                }
                else if (this.version == other.version)
                {
                    throw new InternalServerErrorException(
                        string.Format(CultureInfo.InvariantCulture, RMResources.InvalidRegionsInSessionToken, this.sessionToken, other.sessionToken));
                }
                else
                {
                    highestLocalLsnByRegion[regionId] = localLsn1;
                }
            }

            return new VectorSessionToken(
                Math.Max(this.version, other.version),
                Math.Max(this.LSN, other.LSN),
                highestLocalLsnByRegion);
        }

        string ISessionToken.ConvertToString()
        {
            return this.sessionToken;
        }

        private bool AreRegionProgressEqual(IReadOnlyDictionary<uint, long> other)
        {
            if (this.localLsnByRegion.Count != other.Count)
            {
                return false;
            }

            foreach (KeyValuePair<uint, long> kvp in this.localLsnByRegion)
            {
                uint regionId = kvp.Key;
                long localLsn1 = kvp.Value;
                if (other.TryGetValue(regionId, out long localLsn2))
                {
                    if (localLsn1 != localLsn2)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool AreAllTokenHigherInLocalLsnByRegions(
            VectorSessionToken higherToken,
            VectorSessionToken lowerToken)
        {
            if (higherToken.localLsnByRegion.Count != lowerToken.localLsnByRegion.Count)
            {
                return false;
            }

            if (object.ReferenceEquals(higherToken.localLsnByRegion, lowerToken.localLsnByRegion))
            {
                return true;
            }

            foreach (KeyValuePair<uint, long> kvp in higherToken.localLsnByRegion)
            {
                uint higherRegionId = kvp.Key;
                long higherLocalLsn = kvp.Value;
                if (lowerToken.localLsnByRegion.TryGetValue(higherRegionId, out long lowerLocalLsn))
                {
                    if (lowerLocalLsn > higherLocalLsn)
                    {
                        return false;
                    }
                }
                else if (higherToken.version == lowerToken.version)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseSessionToken(
            string sessionToken,
            out long version,
            out long globalLsn,
            out IReadOnlyDictionary<uint, long> localLsnByRegion)
        {
            version = 0;
            localLsnByRegion = null;
            globalLsn = -1;

            if (string.IsNullOrEmpty(sessionToken))
            {
                DefaultTrace.TraceCritical("Session token is empty");
                return false;
            }

            int index = 0;
            if (!TryParseLongSegment(
                sessionToken,
                ref index,
                out version))
            {
                DefaultTrace.TraceCritical($"Unexpected session token version number from token: {sessionToken} .");
                return false;
            }

            if (!TryParseLongSegment(
                sessionToken,
                ref index,
                out globalLsn))
            {
                DefaultTrace.TraceCritical($"Unexpected session token global lsn from token: {sessionToken} .");
                return false;
            }

            if (index >= sessionToken.Length)
            {
                localLsnByRegion = VectorSessionToken.DefaultLocalLsnByRegion;
                return true;
            }

            Dictionary<uint, long> lsnByRegion = new Dictionary<uint, long>();

            while (index < sessionToken.Length)
            {
                if (!TryParseUintTillRegionProgressSeparator(
                    sessionToken,
                    ref index,
                    out uint regionId))
                {
                    DefaultTrace.TraceCritical($"Unexpected region progress segment in session token: {sessionToken}.");
                    return false;
                }

                if (!TryParseLongSegment(
                    sessionToken,
                    ref index,
                    out long localLsn))
                {
                    DefaultTrace.TraceCritical($"Unexpected local lsn for region id {regionId.ToString(CultureInfo.InvariantCulture)} for segment in session token: {sessionToken}.");
                    return false;
                }

                lsnByRegion[regionId] = localLsn;
            }

            localLsnByRegion = lsnByRegion;

            return true;
        }

        private static bool TryParseUintTillRegionProgressSeparator(
            string input,
            ref int index,
            out uint value)
        {
            value = 0;
            if (index >= input.Length)
            {
                return false;
            }

            long longValue = 0;
            while (index < input.Length)
            {
                char c = input[index];
                if (c >= '0' && c <= '9')
                {
                    longValue = (longValue * 10) + (c - '0');
                    index++;
                }
                else
                {
                    if (c == VectorSessionToken.RegionProgressSeparator)
                    {
                        // Always increase index pass stop character
                        index++;
                        break;
                    }

                    return false;
                }
            }

            if (longValue > uint.MaxValue ||
                longValue < 0)
            {
                return false;
            }

            value = (uint)longValue;
            return true;
        }

        private static bool TryParseLongSegment(
            string input,
            ref int index,
            out long value)
        {
            value = 0;
            if (index >= input.Length)
            {
                return false;
            }

            bool isNegative = false;
            if (input[index] == '-')
            {
                index++;
                isNegative = true;
            }

            while (index < input.Length)
            {
                char c = input[index];
                if (c >= '0' && c <= '9')
                {
                    value = (value * 10) + (c - '0');
                    index++;
                }
                else
                {
                    if (c == VectorSessionToken.SegmentSeparator)
                    {
                        // Always increase index pass stop character
                        index++;
                        break;
                    }

                    return false;
                }
            }

            if (isNegative)
            {
                value *= -1;
            }

            return true;
        }
    }
}
