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
    public struct ReflectedFunction
    {
        // Public API

        public ReadOnlyCollection<string> EnclosingNamespace => m_EnclosingNamespace.AsReadOnly();
        public string ReturnTypeName { get; internal set; }
        public string Name { get; internal set; }
        public ReadOnlyCollection<ReflectedParameter> Parameters => m_Parameters.AsReadOnly();
        public string BodyText { get; internal set; }
        public ReadOnlyDictionary<string, string> Hints => new ReadOnlyDictionary<string, string>(m_Hints);

        // Gets the function's name, including any namespaces
        public string FullyQualifiedName => EnclosingNamespace.Count > 0 ?
            (string.Join("::", EnclosingNamespace) + "::" + Name) : Name;

        public string GetSignature()
        {
            StringBuilder signature = new StringBuilder();

            signature.Append(ReturnTypeName);
            signature.Append(' ');
            signature.Append(FullyQualifiedName);
            signature.Append('(');
            signature.AppendJoin(", ", Parameters);
            signature.Append(')');

            return signature.ToString();
        }

        public override string ToString()
        {
            return GetSignature();
        }

        // Gets the function's body text, but left-aligned and with consistent indentation characters
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
