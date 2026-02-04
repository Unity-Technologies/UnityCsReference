// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderApiReflection
{
    [NativeHeader("Modules/ShaderApiReflectionEditor/Public/DataStructures/Hint.h")]
    [NativeClass("ShaderApiReflection::Hint")]
    internal struct Hint
    {
        public string m_Key;
        public string m_Value;
    }
}
