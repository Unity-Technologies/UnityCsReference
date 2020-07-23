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
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    [NativeType(IntermediateScriptingStructName = "PackageManager_UpdateScopedRegistryOptions")]
    internal sealed class UpdateScopedRegistryOptions
    {
        [NativeName("name")]
        private string m_Name;

        [NativeName("url")]
        private string m_Url;

        [NativeName("scopes")]
        private string[] m_Scopes;

        internal UpdateScopedRegistryOptions(string name, string url, string[] scopes)
        {
            m_Name = name;
            m_Url = url;
            m_Scopes = scopes;
        }

        public string name => m_Name;
        public string url => m_Url;
        public string[] scopes => m_Scopes;
    }
}
