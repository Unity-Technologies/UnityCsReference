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
    ///<summary>Information about a shader function parameter.</summary>
    ///<remarks>This struct represents a function parameter as reflected from shader source code. On top of information extracted directly from the syntax, such as <see cref="DirectionFlags" />, <see cref="TypeName" />, and <see cref="Name" />, it also contains <see cref="Hints" />: metadata extracted from structured comments decorating the function.</remarks>
    ///<example nocheck="true">
    /// In the following example, <c>PrintFunction</c> fetches <see cref="ReflectedParameter"/>s from a
    /// <see cref="ReflectedFunction"/> and prints each one's name and type to the console.
    ///<code source="../../Tests/UTFTests/FunctionalTests/Editor/Documentation/ReflectedFunctionExample.cs"/>
    ///</example>
    ///<seealso cref="ReflectedFunction.Parameters" />
    public struct ReflectedParameter : IHasHints
    {
        // Public API

        // NOTE: This enum must remain synchronized with its native counterpart.
        ///<summary>The read- and writability flags for a function parameter.</summary>
        ///<remarks>Indicates whether a <see cref="ReflectedParameter" /> may be read from, written to, or both from within a function.</remarks>
        ///<example nocheck="true">
        /// In the following example, <c>PrintFunction</c> prints the direction of each parameter
        /// of each <see cref="ReflectedFunction"/> to the console.
        ///<code source="../../Tests/UTFTests/FunctionalTests/Editor/Documentation/ReflectedFunctionExample.cs"/>
        ///</example>
        ///<seealso cref="ReflectedParameter.DirectionFlags" />
        public enum Direction
        {
            ///<summary>Indicates the parameter is an input to a function.</summary>
            In,
            ///<summary>Indicates the parameter is an output from a function.</summary>
            Out,
            ///<summary>Indicates the parameter is an input and output to a function.</summary>
            InOut,
        };

        ///<summary>The parameter's read- and writability.</summary>
        public Direction DirectionFlags { get; internal set; }
        ///<summary>The name of the parameter's type.</summary>
        public string TypeName { get; internal set; }
        ///<summary>The parameter's name.</summary>
        public string Name { get; internal set; }
        ///<inheritdoc/>
        public ReadOnlyDictionary<string, string> Hints => new ReadOnlyDictionary<string, string>(m_Hints);

        ///<summary>Gets the parameter's declaration, as would be written in the shader function's declaration.</summary>
        ///<returns>The parameter's declaration.</returns>
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
