// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace Unity.Entities
{
    /// <summary>
    /// When added as an assembly-level attribute, allows creating generic ISystems, which will
    /// not work without being registered this way.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RegisterGenericSystemTypeAttribute : Attribute
    {
        /// <summary>
        /// Fully closed generic system type to register with the <see cref="TypeManager"/>
        /// </summary>
        public Type ConcreteType;

        /// <summary>
        /// Registers a fully closed generic system type with the <see cref="TypeManager"/>
        /// </summary>
        /// <param name="type">The system type.</param>
        public RegisterGenericSystemTypeAttribute(Type type)
        {
            ConcreteType = type;
        }
    }
}
