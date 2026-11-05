// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderApiReflection
{
    [ShouldBePublic]
    internal struct StructPart : IHasHints
    {
        // Public API

        public Shader Owner { get; internal set; }
        public ShaderScope Scope { get; internal set; }
        public ReadOnlyDictionary<string, string> Hints => m_Struct.Hints;
        public ReadOnlyCollection<ReflectedField> Fields => m_Struct.Fields;

        // Private API

        internal ReflectedStruct m_Struct;

        [NativeHeader("Modules/ShaderLabParserEditor/Public/Reflection/StructPart.h")]
        [NativeClass("ShaderApiReflection::StructPart")]
        internal struct MarshalledType
        {
            public Shader m_Owner;
            public ShaderScope m_Scope;
            public ReflectedStruct.MarshalledType m_Struct;
        }

        internal StructPart(MarshalledType nativeData)
        {
            Owner = nativeData.m_Owner;
            Scope = nativeData.m_Scope;
            m_Struct = new ReflectedStruct(nativeData.m_Struct);
        }
    }
}
