// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Windows
{
    [NativeHeader("PlatformDependent/MetroPlayer/Bindings/ApplicationTrialBindings.h")]
    public static class LicenseInformation
    {
        /// Returns whether the user is using App trial version (rather than full version)
        public extern static bool isOnAppTrial { get; }

        /// Windows Store apps:
        /// Pops up a dialog for a user asking whether he wants to buy an app
        /// If use buys an app, returns a valid purchase receipt string
        public extern static string PurchaseApp();
    }
}
