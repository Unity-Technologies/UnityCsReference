// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor.PackageManager;
using System.Runtime.InteropServices;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditorInternal
{
    internal partial class AssetStoreCachePathManager
    {
        public enum ConfigStatus
        {
            Success = 0,
            InvalidPath,
            ReadOnly,
            EnvironmentOverride,
            NotFound,
            Failed
        };

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        [RequiredByNativeCode]
        [NativeAsStruct]
        internal class CachePathConfig
        {
            [SerializeField]
            [NativeName("path")]
            private string m_Path = "";

            [SerializeField]
            [NativeName("source")]
            private ConfigSource m_Source = ConfigSource.Unknown;

            [SerializeField]
            [NativeName("status")]
            private ConfigStatus m_Status = ConfigStatus.Success;

            public string path => m_Path;

            public ConfigSource source => m_Source;

            public ConfigStatus status => m_Status;
        }
    }
}
