// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class PackageAndVersionIdPair
    {
        public PackageAndVersionIdPair(string packageUniqueId, string versionUniqueId = null)
        {
            this.packageUniqueId = packageUniqueId;
            this.versionUniqueId = versionUniqueId;
        }

        public string packageUniqueId;
        public string versionUniqueId;
    }
}
