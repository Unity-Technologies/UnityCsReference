// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager
{
    internal static class TrustPolicySettings
    {
        public static bool trustDisabled
        {
            get { return TrustPolicySettingsUtils.GetTrustDisabled(); }
            set { TrustPolicySettingsUtils.SetTrustDisabled(value); }
        }
    }
}
