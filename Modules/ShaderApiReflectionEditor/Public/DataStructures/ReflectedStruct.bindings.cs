// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderApiReflection
{
    /// <summary>
    /// Information about a shader struct.
    /// </summary>
    /// <remarks>
    /// This struct represents a struct as reflected from shader source code.
    /// On top of information extracted directly from the syntax, such as <see cref="Name" />
    /// and <see cref="Fields"/>, it also contains <see cref="Hints" />:  metadata extracted
    /// from structured comments decorating the struct.
    /// </remarks>
    /// <example nocheck="true">
    /// In the following example, <c>PrintStruct</c> fetches <see cref="ReflectedStruct"/>s from a
    /// <see cref="ShaderIncludeReflection"/> and prints each one's name to the console.
    /// <code source="../../Tests/UTFTests/FunctionalTests/Editor/Documentation/ReflectedFunctionExample.cs"/>
    /// </example>
    /// <seealso cref="ShaderIncludeReflection.ReflectedStructs"/>
    public struct ReflectedStruct : IHasHints
    {
        // Public API

        /// <summary>
        /// The namespace in which the struct was defined.
        /// </summary>
        public ReadOnlyCollection<string> EnclosingNamespace => m_EnclosingNamespace.AsReadOnly();

        /// <summary>
        /// The struct's name.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The struct's fields.
        /// </summary>
        public ReadOnlyCollection<ReflectedField> Fields => m_Fields.AsReadOnly();

        /// <inheritdoc/>
        public ReadOnlyDictionary<string, string> Hints => new ReadOnlyDictionary<string, string>(m_Hints);

        /// <summary>
        /// The struct's fully qualified name, taking into account the enclosing namespace.
        /// </summary>
        public string FullyQualifiedName => EnclosingNamespace.Count > 0 ?
            (string.Join("::", EnclosingNamespace) + "::" + Name) : Name;

        // Private API

        internal List<string> m_EnclosingNamespace;
        internal List<ReflectedField> m_Fields;
        internal Dictionary<string, string> m_Hints;

        [NativeHeader("Modules/ShaderApiReflectionEditor/Public/DataStructures/ReflectedStruct.h")]
        [NativeClass("ShaderApiReflection::ReflectedStruct")]
        internal struct MarshalledType
        {
            public string[] m_Namespace;
            public string m_Name;
            public ReflectedField.MarshalledType[] m_Fields;
            public Hint[] m_Hints;
        }

        internal ReflectedStruct(MarshalledType nativeData)
        {
            m_EnclosingNamespace = new List<string>(nativeData.m_Namespace);
            Name = nativeData.m_Name;

            m_Fields = new List<ReflectedField>(nativeData.m_Fields.Length);
            foreach (ReflectedField.MarshalledType nativeField in nativeData.m_Fields)
                m_Fields.Add(new ReflectedField(nativeField));

            m_Hints = new Dictionary<string, string>(nativeData.m_Hints.Length);
            foreach (Hint hint in nativeData.m_Hints)
                m_Hints.TryAdd(hint.m_Key, hint.m_Value);
        }
    }
}
