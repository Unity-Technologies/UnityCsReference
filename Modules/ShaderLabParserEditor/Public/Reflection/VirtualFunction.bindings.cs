// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderApiReflection
{
    // A virtual function declaration plus additional data specific to a given ShaderInterface
    [ShouldBePublic]
    internal struct VirtualFunction
    {
        // Public API

        public VirtualFunctionDeclaration Declaration { get; internal set; }
        public ReadOnlyCollection<VirtualFunctionOverride> LocalOverrides => m_LocalOverrides.AsReadOnly();

        // Private API

        internal List<VirtualFunctionOverride> m_LocalOverrides;

        [NativeHeader("Modules/ShaderLabParserEditor/Public/Reflection/VirtualFunction.h")]
        [NativeClass("ShaderApiReflection::VirtualFunction")]
        internal struct MarshalledType
        {
            public bool m_IsPureVirtual;
            public ShaderScope m_Scope;
            public ReflectedFunction.MarshalledType m_Signature;
            public VirtualFunctionOverride[] m_LocalOverrides;
        }

        internal VirtualFunction(MarshalledType nativeData)
        {
            VirtualFunctionDeclaration.Attribute attribute = nativeData.m_IsPureVirtual ?
                VirtualFunctionDeclaration.Attribute.PureVirtual : VirtualFunctionDeclaration.Attribute.Virtual;
            Declaration = new(attribute, nativeData.m_Scope, new ReflectedFunction(nativeData.m_Signature));
            m_LocalOverrides = new(nativeData.m_LocalOverrides);
        }
    }
}
