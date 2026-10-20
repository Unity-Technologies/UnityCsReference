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
    internal struct VirtualFunctionOverride : IHasHints
    {
        // Public API

        public Shader Owner { get; internal set; }
        public ShaderScope Scope { get; internal set; }
        public ReadOnlyDictionary<string, string> Hints => new ReadOnlyDictionary<string, string>(m_Hints);

        // Internal API

        internal Shader BaseImplementationOwner { get; set; }

        // Private API

        internal Dictionary<string, string> m_Hints;

        [NativeHeader("Modules/ShaderLabParserEditor/Public/Reflection/VirtualFunctionOverride.h")]
        [NativeClass("ShaderApiReflection::VirtualFunctionOverride")]
        internal struct MarshalledType
        {
            public Shader m_Owner;
            public ShaderScope m_Scope;
            public Hint[] m_Hints;
            public Shader m_BaseImplementationOwner;
        }

        internal VirtualFunctionOverride(MarshalledType nativeData)
        {
            Owner = nativeData.m_Owner;
            Scope = nativeData.m_Scope;
            m_Hints = new Dictionary<string, string>(nativeData.m_Hints.Length);
            foreach (var hint in nativeData.m_Hints)
                m_Hints.TryAdd(hint.m_Key, hint.m_Value);
            BaseImplementationOwner = nativeData.m_BaseImplementationOwner;
        }
    }
}
