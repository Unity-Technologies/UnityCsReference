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
    internal class ReflectedParameter
    {
        // Public API

        public enum Direction
        {
            In,
            Out,
            InOut,
        };

        public Direction DirectionFlags { get; private set; }
        public string TypeName { get; private set; }
        public string Name { get; private set; }
        public Dictionary<string, string> Hints { get; private set; }

        // Private API

        [NativeHeader("Modules/ShaderApiReflectionEditor/Public/DataStructures/ReflectedParameter.h")]
        [NativeClass("ShaderApiReflection::ReflectedParameter")]
        internal struct MarshalledType
        {
            public Direction m_Direction;
            public string m_TypeName;
            public string m_Name;
            public Hint[] m_Hints;
        }

        internal ReflectedParameter(MarshalledType nativeData)
        {
            DirectionFlags = nativeData.m_Direction;
            TypeName = nativeData.m_TypeName;
            Name = nativeData.m_Name;

            Hints = new Dictionary<string, string>(nativeData.m_Hints.Length);
            foreach (Hint hint in nativeData.m_Hints)
                Hints.Add(hint.m_Key, hint.m_Value);
        }
    }
}
