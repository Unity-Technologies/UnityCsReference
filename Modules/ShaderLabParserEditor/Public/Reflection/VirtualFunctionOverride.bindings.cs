// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderApiReflection
{
    [ShouldBePublic]
    [NativeHeader("Modules/ShaderLabParserEditor/Public/Reflection/ShaderInterfaceReflection.h")]
    [NativeClass("ShaderApiReflection::VirtualFunctionOverride")]
    internal struct VirtualFunctionOverride
    {
        // Public API

        [NativeName("m_Scope")]
        public ShaderScope Scope { get; internal set; }
        [NativeName("m_BaseImplementationOwner")]
        public Shader BaseImplementationOwner { get; internal set; }
    }
}
