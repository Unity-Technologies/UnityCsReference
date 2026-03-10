// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.UnityConsent
{
    /// <summary>
    /// Represents the consent state of a user.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ConsentState
    {
        /// <summary>
        /// The consent status for Ads.
        /// </summary>
        public ConsentStatus AdsIntent;

        /// <summary>
        /// The consent status for Analytics.
        /// </summary>
        public ConsentStatus AnalyticsIntent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsentState"/> struct with default values.
        /// </summary>
        public ConsentState()
        {
            AdsIntent = ConsentStatus.Unspecified;
            AnalyticsIntent = ConsentStatus.Unspecified;
        }

        /// <summary>
        /// Returns a string representation of the consent state.
        /// </summary>
        /// <returns>A string describing the Ads and Analytics consent statuses.</returns>
        public override string ToString()
        {
            return $"{nameof(AdsIntent)}: {AdsIntent}, {nameof(AnalyticsIntent)}: {AnalyticsIntent}";
        }
    }
}
