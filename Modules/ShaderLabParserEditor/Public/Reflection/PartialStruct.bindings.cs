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

        public ReadOnlyCollection<string> EnclosingNamespace => m_EnclosingNamespace.AsReadOnly();
        public string Name { get; internal set; }
        public ReadOnlyCollection<StructPart> Parts => m_Parts.AsReadOnly();

        // Private API

        internal List<string> m_EnclosingNamespace;
        internal List<StructPart> m_Parts;

        [NativeHeader("Modules/ShaderLabParserEditor/Public/Reflection/PartialStruct.h")]
        [NativeClass("ShaderApiReflection::PartialStruct")]
        internal struct MarshalledType
        {
            public string[] m_EnclosingNamespace;
            public string m_Name;
            public StructPart.MarshalledType[] m_Parts;
        }

        internal PartialStruct(MarshalledType nativeData)
        {
            m_EnclosingNamespace = new(nativeData.m_EnclosingNamespace);
            Name = nativeData.m_Name;
            m_Parts = new(nativeData.m_Parts.Length);
            foreach (var part in nativeData.m_Parts)
                m_Parts.Add(new(part));
        }
    }
}
