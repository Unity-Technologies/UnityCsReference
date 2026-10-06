// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    internal partial class StyleMaterialDefinitionField : StylePropertyField<StyleMaterialDefinition, MaterialDefinitionStyleField, MaterialDefinition>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-material-definition-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleMaterialDefinitionField() : this(null) { }

        public StyleMaterialDefinitionField(string label) : base(label, new MaterialDefinitionStyleField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            // If label is null, remove the labelElement added with the affordance
            if (Contains(labelElement) && string.IsNullOrEmpty(labelElement.text))
            {
                AddToClassList(noLabelVariantUssClassName);
                labelElement.RemoveFromHierarchy();
            }
        }

        protected override MaterialDefinitionStyleField CreateValueField()
        {
            return new MaterialDefinitionStyleField();
        }

        protected override StyleMaterialDefinition CreateStyleValue(MaterialDefinition v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleMaterialDefinition v)
        {
            return value == v;
        }
    }
}
