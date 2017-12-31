// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/TypeSystem/UnityType.bindings.h")]
    internal partial class UnityType
    {
        #pragma warning disable 649
        [UsedByNativeCode]
        private struct UnityTypeTransport
        {
            public uint runtimeTypeIndex;
            public uint descendantCount;
            public uint baseClassIndex;
            public string className;
            public string classNamespace;
            public string module;
            public int persistentTypeID;
            public uint flags;
        }
        private static extern UnityTypeTransport[] Internal_GetAllTypes();
    }
}
