// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PackageSizeInfo : ISerializationCallbackReceiver
    {
        [SerializeField]
        private string m_SupportedUnityVersionString;
        private SemVersion m_SupportedUnityVersion;
        public SemVersion supportedUnityVersion
        {
            get
            {
                return m_SupportedUnityVersion;
            }
            set
            {
                m_SupportedUnityVersion = value;
                m_SupportedUnityVersionString = value.ToString();
            }
        }

        public ulong assetCount;
        public ulong downloadSize;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            // m_SupportedUnityVersionString will always be valid because it is set from an existing SemVersion
            m_SupportedUnityVersion = SemVersionParser.Parse(m_SupportedUnityVersionString);
        }
    }
}
