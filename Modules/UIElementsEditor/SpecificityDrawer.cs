// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [CustomPropertyDrawer(typeof(Specificity))]
    internal class SpecificityDrawer : PropertyDrawer
    {
        TextField m_Field;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_Field = new TextField
            {
                label = preferredLabel,
                bindingPath = property.propertyPath, isReadOnly = true,
                value = property.boxedValue.ToString()
            };
            m_Field.AddToClassList(TextField.alignedFieldUssClassName);
            m_Field.TrackPropertyValue(property, OnPropertyChanged);
            return m_Field;
        }

        void OnPropertyChanged(SerializedProperty property)
        {
            m_Field.SetValueWithoutNotify(property.boxedValue.ToString());
        }
    }
}
