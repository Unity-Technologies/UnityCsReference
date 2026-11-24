// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class TrustAnalyticsData
    {
        public string action;
        public string operationType;
        public string windowType;
        public string[] invalidSignaturePackageIds;
        public string[] missingSignaturePackageIds;
        public string[] limitedTrustPackageIds;
    }
}
