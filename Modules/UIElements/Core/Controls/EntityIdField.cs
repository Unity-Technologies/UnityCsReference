// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a field for editing an <see cref="EntityId"/>. For more information, refer to [[wiki:UIE-uxml-element-EntityIdField|UXML element EntityIdField]].
    /// </summary>
    class EntityIdField : BaseField<EntityId>
    {
        readonly IntegerField m_IntegerField = new IntegerField();

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-entityId-field";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// Initializes and returns an instance of EntityIdField.
        /// </summary>
        public EntityIdField() : this(null)
        {
        }

        /// <summary>
        /// Initializes and returns an instance of EntityIdField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public EntityIdField(string label)
            : base(label, null)
        {
            AddToClassList(ussClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);

            visualInput.Add(m_IntegerField);
            m_IntegerField.RegisterValueChangedCallback(evt =>
            {
                // Update the value when the integer field changes
                value = EntityId.FromULong((ulong)evt.newValue);
            });
        }

        public override void SetValueWithoutNotify(EntityId newValue)
        {
            base.SetValueWithoutNotify(newValue);
            // Update the integer field when the value changes
            m_IntegerField.SetValueWithoutNotify((int)EntityId.ToULong(newValue));
        }

        protected override void UpdateMixedValueContent()
        {
            m_IntegerField.showMixedValue = showMixedValue;
        }
    }
}
