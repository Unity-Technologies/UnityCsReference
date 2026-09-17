// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderApiReflection
{
    [ShouldBePublic]
    [NativeHeader("Modules/ShaderLabParserEditor/Public/Reflection/StructField.h")]
    [NativeClass("ShaderApiReflection::StructField")]
    internal struct StructField
    {
        // Public API

        [NativeName("m_Scope")]
        public ShaderScope Scope { get; internal set; }
        [NativeName("m_TypeName")]
        public string TypeName { get; internal set; }
        [NativeName("m_Name")]
        public string Name { get; internal set; }
        [NativeName("m_Semantic")]
        public string Semantic { get; internal set; }
    }
}
