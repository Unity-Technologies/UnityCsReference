// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [ExcludeFromPreset]
    internal class PackageSelectionObject : ScriptableObject
    {
        [Serializable]
        public class Data
        {
            public string name;
            public string displayName;
            public string packageUniqueId;
            public string versionUniqueId;
        }

        public string displayName => m_Data?.displayName;
        public string packageUniqueId => m_Data?.packageUniqueId;
        public string versionUniqueId => m_Data?.versionUniqueId;

        public Data m_Data;
    }
}
