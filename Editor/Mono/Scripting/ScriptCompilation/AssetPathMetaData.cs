// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode(GenerateProxy = true)]
    [NativeHeader("Runtime/Scripting/ScriptingManagedProxySupport.h")]
    [NativeHeader("Runtime/ScriptingBackend/ScriptingNativeTypes.h")]
    [DebuggerDisplay("{DirectoryPath}")]
    class AssetPathMetaData
    {
        public string DirectoryPath;
        public bool IsTestable;
        public VersionMetaData VersionMetaData;
    }
}
