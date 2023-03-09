// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Constant model for enums.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class EnumConstant : Constant<EnumValueReference>
    {
        [SerializeField]
        TypeHandle m_EnumType;

        /// <inheritdoc />
        public override object DefaultValue => new EnumValueReference(EnumType);

        /// <inheritdoc />
        public override EnumValueReference Value
        {
            get => base.Value;
            set
            {
                if (value.EnumType != EnumType)
                {
                    throw new ArgumentException(nameof(value));
                }

                base.Value = value;
            }
        }

        /// <summary>
        /// The constant value as an <see cref="Enum"/>.
        /// </summary>
        public Enum EnumValue => Value.ValueAsEnum();

        /// <summary>
        /// The <see cref="TypeHandle"/> for the type of the enum.
        /// </summary>
        public TypeHandle EnumType => m_EnumType;

        /// <inheritdoc />
        public override void Initialize(TypeHandle constantTypeHandle)
        {
            var resolvedType = constantTypeHandle.Resolve();
            if (!resolvedType.IsEnum || resolvedType == typeof(Enum))
            {
                throw new ArgumentException(nameof(constantTypeHandle));
            }

            m_EnumType = constantTypeHandle;

            base.Initialize(constantTypeHandle);
        }

        /// <inheritdoc />
        public override Constant Clone()
        {
            var copy = base.Clone();
            ((EnumConstant)copy).m_EnumType = m_EnumType;
            return copy;
        }

        /// <inheritdoc />
        public override TypeHandle GetTypeHandle()
        {
            return EnumType;
        }

        /// <inheritdoc />
        protected override EnumValueReference FromObject(object value)
        {
            if (value is Enum e)
                return new EnumValueReference(e);
            return base.FromObject(value);
        }

        /// <inheritdoc />
        public override bool IsAssignableFrom(Type t)
        {
            return Type == t;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            m_EnumType = m_Value.EnumType;
        }
    }
}
