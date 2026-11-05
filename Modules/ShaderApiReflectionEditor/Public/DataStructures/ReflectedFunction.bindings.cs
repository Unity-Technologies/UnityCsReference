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
    ///<summary>Information about a shader function.</summary>
    ///<remarks>This struct represents a function as reflected from shader source code. On top of information extracted directly from the syntax, such as <see cref="ReturnTypeName" />, <see cref="Parameters" />, and <see cref="Name" />, it also contains <see cref="Hints" />: metadata extracted from structured comments decorating the function.</remarks>
    ///<example nocheck="true">
    /// In the following example, <c>PrintFunction</c> fetches <see cref="ReflectedFunction"/>s from a
    /// <see cref="ShaderIncludeReflection"/> and prints each one's signature to the console.
    ///<code source="../../Tests/UTFTests/FunctionalTests/Editor/Documentation/ReflectedFunctionExample.cs"/>
    ///</example>
    ///<seealso cref="ShaderIncludeReflection.ReflectedFunctions" />
    public struct ReflectedFunction : IHasFunctionSignature, IHasHints
    {
        // Public API

        ///<summary>The namespace in which the function was defined.</summary>
        public ReadOnlyCollection<string> EnclosingNamespace => m_EnclosingNamespace.AsReadOnly();
        ///<summary>The name of the function's return type.</summary>
        public string ReturnTypeName { get; internal set; }
        ///<summary>The function's name.</summary>
        public string Name { get; internal set; }
        ///<summary>The function's parameters.</summary>
        public ReadOnlyCollection<ReflectedParameter> Parameters => m_Parameters.AsReadOnly();
        ///<summary>The function's unprocessed body text.</summary>
        ///<remarks>The string returned by this property represents all the text between the opening and closing braces, including whitespace. <see cref="GetNormalizedBodyText" /> returns a version of this text in a more readable format.</remarks>
        public string BodyText { get; internal set; }
        ///<inheritdoc/>
        public ReadOnlyDictionary<string, string> Hints => new ReadOnlyDictionary<string, string>(m_Hints);

        ///<summary>The function's fully qualified name, taking into account the enclosing namespace.</summary>
        public string FullyQualifiedName => EnclosingNamespace.Count > 0 ?
            (string.Join("::", EnclosingNamespace) + "::" + Name) : Name;

        ///<summary>Gets the function's signature.</summary>
        ///<remarks>Formats the function's return type, namespace, name, and parameters as one would write them in shader code.</remarks>
        ///<returns>The function's signature.</returns>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Modules/ShaderApiReflectionEditor/Tests/UTFTests/FunctionalTests/Editor/Documentation/ReflectedFunctionExample.cs}]]></code>
        ///</example>
        public string GetSignature()
        {
            return GetSignature(includeNamespace: true);
        }

        ///<summary>Gets the function's signature.</summary>
        ///<returns>The function's signature.</returns>
        ///<seealso cref="GetSignature" />
        public override string ToString()
        {
            return GetSignature();
        }

        ///<summary>Gets the function's body text in an easy-to-read format.</summary>
        ///<remarks>This method performs the following operations on the body text:
        ///
        ///* Tab characters are converted to spaces based on <c>tabWidth</c>.
        ///* The text is left-aligned such that the least-indented line within the raw body text has zero indentation.</remarks>
        ///<param name="tabWidth">The number of spaces to use when converting any tab characters within the body text.</param>
        ///<returns>A readable version of the function's body text.</returns>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Modules/ShaderApiReflectionEditor/Tests/UTFTests/FunctionalTests/Editor/Documentation/ReflectedFunctionExample.cs}]]></code>
        ///</example>
        ///<seealso cref="BodyText" />
        public string GetNormalizedBodyText(UInt32 tabWidth = 4)
        {
            const char kSpace = ' ';
            const char kEol = '\n';

            if (BodyText.Length == 0)
                return BodyText;

            // Convert tabs to spaces
            string body = BodyText.Replace("\t", new string(kSpace, (int)tabWidth));
            string[] bodyLines = body.Split(kEol);

            // Determine the minimum indentation in the string
            UInt32 minIndent = UInt32.MaxValue;
            for (int i = 0; i < bodyLines.Length; ++i)
            {
                bool lineIsWhitespaceOnly = true;
                UInt32 indent = 0;
                string line = bodyLines[i];
                for (int j = 0; j < line.Length; ++j)
                {
                    if (line[j] == kSpace)
                    {
                        ++indent;
                    }
                    else
                    {
                        lineIsWhitespaceOnly = false;
                        break;
                    }
                }
                if (lineIsWhitespaceOnly)
                {
                    // Lines that are whitespace only are replaced with just the EOL character
                    bodyLines[i] = string.Empty;
                }
                else
                {
                    minIndent = Math.Min(minIndent, indent);
                }
            }

            // Left-align text
            StringBuilder normalized = new StringBuilder(BodyText.Length);
            {
                string indentToStrip = new string(kSpace, (int)minIndent);
                for (int i = 0; i < bodyLines.Length; ++i)
                {
                    if (i > 0)
                        normalized.Append(kEol);

                    string line = bodyLines[i];
                    if (line.Length > minIndent)
                        normalized.Append(line.Substring((int)minIndent));
                    else if (line.Length > 0)
                        normalized.Append(line);
                }
            }
            return normalized.ToString();
        }

        // Private API

        internal string GetSignature(bool includeNamespace)
        {
            StringBuilder signature = new StringBuilder();

            signature.Append(ReturnTypeName);
            signature.Append(' ');
            signature.Append(includeNamespace ? FullyQualifiedName : Name);
            signature.Append('(');
            signature.AppendJoin(", ", Parameters);
            signature.Append(')');

            return signature.ToString();
        }

        internal List<string> m_EnclosingNamespace;
        internal List<ReflectedParameter> m_Parameters;
        internal Dictionary<string, string> m_Hints;

        [NativeHeader("Modules/ShaderApiReflectionEditor/Public/DataStructures/ReflectedFunction.h")]
        [NativeClass("ShaderApiReflection::ReflectedFunction")]
        internal struct MarshalledType
        {
            public string[] m_Namespace;
            public string m_ReturnTypeName;
            public string m_Name;
            public ReflectedParameter.MarshalledType[] m_Parameters;
            public string m_Body;
            public Hint[] m_Hints;
        }

        internal ReflectedFunction(MarshalledType nativeData)
        {
            m_EnclosingNamespace = new List<string>(nativeData.m_Namespace);
            ReturnTypeName = nativeData.m_ReturnTypeName;
            Name = nativeData.m_Name;

            m_Parameters = new List<ReflectedParameter>(nativeData.m_Parameters.Length);
            foreach (ReflectedParameter.MarshalledType nativeParam in nativeData.m_Parameters)
                m_Parameters.Add(new ReflectedParameter(nativeParam));

            BodyText = nativeData.m_Body;

            m_Hints = new Dictionary<string, string>(nativeData.m_Hints.Length);
            foreach (Hint hint in nativeData.m_Hints)
                m_Hints.TryAdd(hint.m_Key, hint.m_Value);
        }
    }
}
