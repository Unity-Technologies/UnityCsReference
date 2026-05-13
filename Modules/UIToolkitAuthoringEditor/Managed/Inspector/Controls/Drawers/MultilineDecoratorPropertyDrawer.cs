// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Property drawer for Multiline boolean property.
    /// </summary>
    [CustomPropertyDrawer(typeof(MultilineDecoratorAttribute))]
    class MultilineDecoratorPropertyDrawer : PropertyDrawer
    {
        SerializedProperty m_Property;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_Property = property;

            var field = new Toggle(property.localizedDisplayName) {
                bindingPath = property.propertyPath
            }.WithClassList(Toggle.alignedFieldUssClassName);

            field.RegisterCallback<ChangeEvent<bool>>(evt => SetMultilineOfValueField(evt.newValue, evt.target as VisualElement));
            return field;
        }

        void SetMultilineOfValueField(bool multiline, VisualElement visualElement)
        {
            VisualElement inspector = null;

            // First try to find UxmlSerializedDataPropertyView ancestor
            inspector = visualElement.GetFirstAncestorOfType<UxmlSerializedDataPropertyView>();

            // Fall back to BuilderInspector if UxmlSerializedDataPropertyView not found
            // Use indirect lookup to avoid direct dependency on UI Builder module
            if (inspector == null)
            {
                var current = visualElement;
                while (current != null)
                {
                    if (current.ClassListContains(InspectorElement.ussClassName))
                    {
                        inspector = current;
                        break;
                    }
                    current = current.parent;
                }
            }

            if (inspector == null)
                return;

            var valueFieldInInspector = inspector.Query<TextField>().Where(x => x.label is "Value").First();
            if (valueFieldInInspector == null)
            {
                var propertyField = inspector.Query<PropertyField>().Where(x => x.label is "Value").First();
                propertyField?.RegisterCallback<SerializedPropertyBindEvent>(_ =>
                {
                    EditorApplication.delayCall += () => SetMultilineOfValueField(multiline, visualElement);
                });
                return;
            }

            valueFieldInInspector.multiline = multiline;
        }
    }
}
