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

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

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
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            visualInput.Add(m_IntegerField);
            m_IntegerField.RegisterValueChangedCallback(evt =>
            {
                // Update the value when the integer field changes
                value = EntityId.From(evt.newValue);
            });
        }

        public override void SetValueWithoutNotify(EntityId newValue)
        {
            base.SetValueWithoutNotify(newValue);
            // Update the integer field when the value changes
            m_IntegerField.SetValueWithoutNotify(newValue.GetRawData());
        }
    }
}
