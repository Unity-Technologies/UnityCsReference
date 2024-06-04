// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    internal class EditorCompatibilityInfo
    {
        [SerializeField]
        [NativeName("compatibilityLevel")]
        private EditorCompatibilityLevel m_CompatibilityLevel;

        [SerializeField]
        [NativeName("minimumPackageVersion")]
        private string m_MinimumPackageVersion = "";

        [SerializeField]
        [NativeName("minimumUnityVersion")]
        private string m_MinimumUnityVersion = "";

        public EditorCompatibilityLevel compatibilityLevel { get { return m_CompatibilityLevel; } }

        public string minimumPackageVersion { get {  return m_MinimumPackageVersion; } }

        public string minimumUnityVersion { get { return m_MinimumUnityVersion; } }
    }
}
