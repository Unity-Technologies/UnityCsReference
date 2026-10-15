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
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var toggle = new Toggle(property.localizedDisplayName) {
                bindingPath = property.propertyPath
            }.WithClassList(Toggle.alignedFieldUssClassName);

            toggle.RegisterCallback<ChangeEvent<bool>>(_ => SetMultilineOfValueField(toggle, true));
            toggle.RegisterCallback<AttachToPanelEvent>(_ => SetMultilineOfValueField(toggle, true));
            return toggle;
        }

        static void SetMultilineOfValueField(Toggle toggle, bool retryIfNotCreated)
        {
            // First try to find UxmlSerializedDataPropertyView ancestor
            VisualElement inspector = toggle.GetFirstAncestorOfType<UxmlSerializedDataPropertyView>();

            // Fall back to BuilderInspector if UxmlSerializedDataPropertyView not found
            // Use indirect lookup to avoid direct dependency on UI Builder module
            if (inspector == null)
            {
                var current = toggle.parent;
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
            if (valueFieldInInspector != null)
            {
                valueFieldInInspector.multiline = toggle.value;
                valueFieldInInspector.UnregisterCallback<InputEvent>(StripNewLinesWhenSingleLine);
                valueFieldInInspector.RegisterCallback<InputEvent>(StripNewLinesWhenSingleLine);
                return;
            }

            var propertyField = inspector.Query<PropertyField>().Where(x => x.label is "Value").First();
            if (propertyField != null)
            {
                void OnValueFieldBound(SerializedPropertyBindEvent evt)
                {
                    propertyField.UnregisterCallback<SerializedPropertyBindEvent>(OnValueFieldBound);
                    SetMultilineOfValueField(toggle, false);
                }

                propertyField.RegisterCallback<SerializedPropertyBindEvent>(OnValueFieldBound);
            }
            else if (retryIfNotCreated)
            {
                toggle.schedule.Execute(() => SetMultilineOfValueField(toggle, false));
            }
        }

        // TextField.value re-applies the raw value to the displayed text, newlines included, even when single-line.
        static void StripNewLinesWhenSingleLine(InputEvent evt)
        {
            var valueField = (TextField)evt.currentTarget;
            if (!valueField.multiline && evt.newData != null && evt.newData.Contains("\n"))
                valueField.SetValueWithoutNotify(valueField.value);
        }
    }
}
