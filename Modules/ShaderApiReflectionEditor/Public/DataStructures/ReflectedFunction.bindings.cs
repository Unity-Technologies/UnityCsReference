// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderApiReflection
{
    internal class ReflectedFunction
    {
        // Public API

        public string ReturnTypeName { get; private set; }
        public string Name { get; private set; }
        public List<ReflectedParameter> Parameters { get; private set; }
        public Dictionary<string, string> Hints { get; private set; }

        // Private API

        [NativeHeader("Modules/ShaderApiReflectionEditor/Public/DataStructures/ReflectedFunction.h")]
        [NativeClass("ShaderApiReflection::ReflectedFunction")]
        internal struct MarshalledType
        {
            public string m_ReturnTypeName;
            public string m_Name;
            public ReflectedParameter.MarshalledType[] m_Parameters;
            public Hint[] m_Hints;
        }

        internal ReflectedFunction(MarshalledType nativeData)
        {
            ReturnTypeName = nativeData.m_ReturnTypeName;
            Name = nativeData.m_Name;

            Parameters = new List<ReflectedParameter>(nativeData.m_Parameters.Length);
            foreach (ReflectedParameter.MarshalledType nativeParam in nativeData.m_Parameters)
                Parameters.Add(new ReflectedParameter(nativeParam));

            Hints = new Dictionary<string, string>(nativeData.m_Hints.Length);
            foreach (Hint hint in nativeData.m_Hints)
                Hints.Add(hint.m_Key, hint.m_Value);
        }
    }
}
