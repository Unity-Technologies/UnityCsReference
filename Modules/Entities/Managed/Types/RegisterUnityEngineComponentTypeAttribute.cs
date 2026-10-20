// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Entities
{
    /// <summary>
    /// When added as an assembly-level attribute, allows creating component type reflection data for UnityEngine objects
    /// for the <see cref="TypeManager"/> to reason about.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RegisterUnityEngineComponentTypeAttribute : Attribute
    {
        /// <summary>
        /// UnityEngine component type to register with the <see cref="TypeManager"/>
        /// </summary>
        public Type ConcreteType;

        /// <summary>
        /// Registers a UnityEngine component type with the <see cref="TypeManager"/>
        /// </summary>
        /// <param name="type">The component type.</param>
        public RegisterUnityEngineComponentTypeAttribute(Type type)
        {
            ConcreteType = type;
        }
    }
}
