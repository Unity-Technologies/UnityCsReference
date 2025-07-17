// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UnityConsent
{
    /// <summary>
    /// Consent status for a specific operation or feature.
    /// </summary>
    public enum ConsentStatus : int
    {
        /// <summary>
        /// The consent status is unspecified. No explicit consent has been given or denied.
        /// </summary>
        Unspecified,
        /// <summary>
        /// Consent has been explicitly granted.
        /// </summary>
        Granted,
        /// <summary>
        /// Consent has been explicitly denied.
        /// </summary>
        Denied,
    }
}
