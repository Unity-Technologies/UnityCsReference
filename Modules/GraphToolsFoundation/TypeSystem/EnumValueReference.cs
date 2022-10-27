// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation
{
    /// <summary>
    /// Represents an Enum value as an object instance.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation", "Unity.GraphTools.Foundation")]
    struct EnumValueReference
    {
        [SerializeField]
        TypeHandle m_EnumType;

        [SerializeField]
        int m_Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumValueReference" /> class.to the value 0.
        /// </summary>
        /// <param name="handle">A <see cref="TypeHandle"/> representing the type of the Enum.</param>
        public EnumValueReference(TypeHandle handle)
        {
            m_EnumType = handle;
            m_Value = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumValueReference" /> class.
        /// </summary>
        /// <param name="e">A an Enum value.</param>
        public EnumValueReference(Enum e)
        {
            m_EnumType = e.GetType().GenerateTypeHandle();
            m_Value = Convert.ToInt32(e);
        }

        /// <summary>
        /// The Enum type. Changing the Enum type sets its value to 0.
        /// </summary>
        public TypeHandle EnumType
        {
            get => m_EnumType;
            set
            {
                m_EnumType = value;
                m_Value = 0;
            }
        }

        /// <summary>
        /// Gets an Enum representing the value stored in this instance.
        /// </summary>
        /// <returns>An Enum representing the value stored in this instance.</returns>
        public Enum ValueAsEnum()
        {
            return (Enum)Enum.ToObject(m_EnumType.Resolve(), m_Value);
        }

        /// <summary>
        /// The value of the instance.
        /// </summary>
        public int Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        /// <summary>
        /// Checks whether this instance EnumType is an enum type.
        /// </summary>
        /// <returns>True if this instance EnumType is an enum type.</returns>
        public bool IsValid() => m_EnumType.IsValid && m_EnumType.Resolve().IsEnum;
    }
}
