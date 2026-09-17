// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderApiReflection
{
    [ShouldBePublic]
    [NativeHeader("Modules/ShaderLabParserEditor/Public/Reflection/ShaderScope.h")]
    [NativeClass("ShaderApiReflection::ShaderScope")]
    internal struct ShaderScope
    {
        // Public API

        [NativeName("m_SubshaderIndex")]
        public UInt32 SubShaderIndex { get; internal set; }
        [NativeName("m_PassName")]
        public string PassName { get; internal set; }
    }
}
