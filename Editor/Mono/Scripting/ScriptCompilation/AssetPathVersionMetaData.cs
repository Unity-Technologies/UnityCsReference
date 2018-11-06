// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode(GenerateProxy = true)]
    [NativeHeader("Runtime/Scripting/ScriptingManagedProxySupport.h")]
    class AssetPathVersionMetaData
    {
        public string Name;
        public string Version;
    }
}
