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
    internal struct PartialStruct
    {
        // Public API

        public ShaderScope Scope { get; internal set; }
        public ReadOnlyCollection<string> EnclosingNamespace => m_EnclosingNamespace.AsReadOnly();
        public string Name { get; internal set; }
        public ReadOnlyCollection<StructField> AggregateFields => m_AggregateFields.AsReadOnly();

        // Private API

        internal List<string> m_EnclosingNamespace;
        internal List<StructField> m_AggregateFields;

        [NativeHeader("Modules/ShaderLabParserEditor/Public/Reflection/PartialStruct.h")]
        [NativeClass("ShaderApiReflection::PartialStruct")]
        internal struct MarshalledType
        {
            public ShaderScope m_Scope;
            public string[] m_EnclosingNamespace;
            public string m_Name;
            public StructField[] m_AggregateFields;
        }

        internal PartialStruct(MarshalledType nativeData)
        {
            Scope = nativeData.m_Scope;
            m_EnclosingNamespace = new(nativeData.m_EnclosingNamespace);
            Name = nativeData.m_Name;
            m_AggregateFields = new(nativeData.m_AggregateFields);
        }
    }
}
