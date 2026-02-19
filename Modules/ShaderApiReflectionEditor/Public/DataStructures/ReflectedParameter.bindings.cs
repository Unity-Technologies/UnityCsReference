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
    public struct ReflectedParameter
    {
        // Public API

        // NOTE: This enum must remain synchronized with its native counterpart.
        public enum Direction
        {
            In,
            Out,
            InOut,
        };

        public Direction DirectionFlags { get; internal set; }
        public string TypeName { get; internal set; }
        public string Name { get; internal set; }
        public ReadOnlyDictionary<string, string> Hints => new ReadOnlyDictionary<string, string>(m_Hints);

        public override string ToString()
        {
            string directionString = string.Empty;
            if (DirectionFlags == Direction.In)
                directionString = "in";
            else if (DirectionFlags == Direction.Out)
                directionString = "out";
            else
                directionString = "inout";

            return $"{directionString} {TypeName} {Name}";
        }

        // Private API

        internal Dictionary<string, string> m_Hints;

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

            m_Hints = new Dictionary<string, string>(nativeData.m_Hints.Length);
            foreach (Hint hint in nativeData.m_Hints)
                m_Hints.TryAdd(hint.m_Key, hint.m_Value);
        }
    }
}
