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
    internal struct VirtualFunctionDeclaration : IHasFunctionSignature
    {
        // Public API

        public enum Attribute
        {
            Virtual,
            PureVirtual,
        }
        public Attribute VirtualAttribute { get; internal set; }

        public ShaderScope Scope { get; internal set; }

        public ReadOnlyCollection<string> EnclosingNamespace => m_Signature.EnclosingNamespace;
        public string ReturnTypeName => m_Signature.ReturnTypeName;
        public string Name => m_Signature.Name;
        public ReadOnlyCollection<ReflectedParameter> Parameters => m_Signature.Parameters;

        public string GetSignature()
        {
            return m_Signature.GetSignature();
        }

        // Private API

        private ReflectedFunction m_Signature;

        internal VirtualFunctionDeclaration(Attribute attribute,
            ShaderScope scope,
            ReflectedFunction signature)
        {
            VirtualAttribute = attribute;
            Scope = scope;
            m_Signature = signature;
        }
    }
}
