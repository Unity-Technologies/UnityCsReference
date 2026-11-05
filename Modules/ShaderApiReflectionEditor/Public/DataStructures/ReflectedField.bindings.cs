// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderApiReflection
{
    /// <summary>
    /// Information about a struct field.
    /// </summary>
    /// <remarks>
    /// This struct represents a struct field as reflected from shader source code.
    /// On top of information extracted directly from the syntax, such as <see cref="TypeName" />,
    /// <see cref="Name" />, and <see cref="Semantic" />, it also contains <see cref="Hints" />:
    /// metadata extracted from structured comments decorating the field.
    /// </remarks>
    /// <example nocheck="true">
    /// In the following example, <c>PrintStruct</c> fetches <see cref="ReflectedField"/>s from a
    /// <see cref="ReflectedStruct"/> and prints each one's name and type to the console.
    /// <code source="../../Tests/UTFTests/FunctionalTests/Editor/Documentation/ReflectedFunctionExample.cs"/>
    /// </example>
    /// <seealso cref="ReflectedStruct.Fields"/>
    public struct ReflectedField : IHasHints
    {
        // Public API

        /// <summary>
        /// The name of the field's type.
        /// </summary>
        public string TypeName { get; internal set; }

        /// <summary>
        /// The field's name.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The semantic associated with the field.
        /// </summary>
        /// <remarks>
        /// A <see href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-semantics">semantic</see>
        /// elucidates the purpose of a piece of data.
        /// In the declaration <c>float2 uv : TEXCOORD</c> the field's semantic would be "TEXCOORD".
        /// </remarks>
        public string Semantic { get; internal set; }

        /// <inheritdoc/>
        public ReadOnlyDictionary<string, string> Hints => new ReadOnlyDictionary<string, string>(m_Hints);

        // Private API

        internal Dictionary<string, string> m_Hints;

        [NativeHeader("Modules/ShaderApiReflectionEditor/Public/DataStructures/ReflectedField.h")]
        [NativeClass("ShaderApiReflection::ReflectedField")]
        internal struct MarshalledType
        {
            public string m_TypeName;
            public string m_Name;
            public string m_Semantic;
            public Hint[] m_Hints;
        }

        internal ReflectedField(MarshalledType nativeData)
        {
            TypeName = nativeData.m_TypeName;
            Name = nativeData.m_Name;
            Semantic = nativeData.m_Semantic;

            m_Hints = new Dictionary<string, string>(nativeData.m_Hints.Length);
            foreach (Hint hint in nativeData.m_Hints)
                m_Hints.TryAdd(hint.m_Key, hint.m_Value);
        }
    }
}
