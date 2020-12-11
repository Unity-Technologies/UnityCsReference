using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    static class BuilderInPlaceTextEditingUtilities
    {
        static readonly string s_DummyText = " ";
        static readonly string s_TextAttributeName = "text";
        static readonly float s_TextEditorMinWidth = 16;
        static readonly float s_TextEditorMinHeight = 16;

        static BuilderViewport s_Viewport;

        static UxmlAttributeDescription s_EditedTextAttribute;
        static VisualElement s_EditedElement;
        static VisualElement s_EditedTextElement;

        internal static void GetAlignmentFromTextAnchor(TextAnchor anchor, out Align align, out Justify justifyContent)
        {
            align = Align.FlexStart;
            justifyContent = Justify.FlexStart;

            switch (anchor)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    align = Align.Center;
                    break;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    align = Align.FlexEnd;
                    break;
            }

            switch (anchor)
            {
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    justifyContent = Justify.Center;
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    justifyContent = Justify.FlexEnd;
                    break;
            }
        }

        static void GetAttributeToEdit(VisualElement editedElement, Vector2 pos, out VisualElement textElement, out string attributeName)
        {
            VisualElement labelElement = null;

            textElement = null;
            attributeName = null;

            switch (editedElement)
            {
                case BaseField<bool> boolField: labelElement = boolField.labelElement; break;
                case BaseField<int> inField: labelElement = inField.labelElement; break;
                case BaseField<float> floatField: labelElement = floatField.labelElement; break;
                case BaseField<long> longField: labelElement = longField.labelElement; break;
                case BaseField<string> strField: labelElement = strField.labelElement; break;
                case BaseField<Vector2> vec2Field: labelElement = vec2Field.labelElement; break;
                case BaseField<Vector3> vec3Field: labelElement = vec3Field.labelElement; break;
                case BaseField<Vector4> vec4Field: labelElement = vec4Field.labelElement; break;
                case BaseField<Rect> rectField: labelElement = rectField.labelElement; break;
                case BaseField<Color> colorField: labelElement = colorField.labelElement; break;
                case BaseField<Gradient> gradField: labelElement = gradField.labelElement; break;
                case BaseField<Object> objField: labelElement = objField.labelElement; break;
                case BaseField<Bounds> boundsField: labelElement = boundsField.labelElement; break;
                case BaseField<AnimationCurve> animCurveField: labelElement = animCurveField.labelElement; break;
                case BaseField<Enum> enumField: labelElement = enumField.labelElement; break;
            }

            if (labelElement != null)
            {
                textElement = labelElement;
                attributeName = "label";
            }
            else if (editedElement is TextElement)
            {
                textElement = editedElement;
                attributeName = s_TextAttributeName;
            }
        }

        public static void OpenEditor(VisualElement element, Vector2 pos)
        {
            BuilderViewport viewport = element.GetFirstAncestorOfType<BuilderViewport>();
            var editorLayer = viewport.editorLayer;
            var textEditor = viewport.textEditor;

            GetAttributeToEdit(element, pos, out var textElement, out var attributeName);

            if (textElement == null || string.IsNullOrEmpty(attributeName))
                return;

            var attributeList = element.GetAttributeDescriptions();

            foreach (var attribute in attributeList)
            {
                if (attribute?.name != null && attribute.name.Equals(attributeName))
                {
                    s_EditedTextAttribute = attribute;
                    break;
                }
            }

            if (s_EditedTextAttribute == null)
                return;

            s_Viewport = viewport;
            s_EditedElement = element;
            s_EditedTextElement = textElement;

            var value = s_EditedElement.GetValueByReflection(s_EditedTextAttribute.name) as string;

            // To ensure the text element is visible
            if (string.IsNullOrEmpty(value))
            {
                s_EditedElement.SetValueByReflection(s_EditedTextAttribute.name, s_DummyText);
            }

            editorLayer.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);

            var textInput = textEditor.Q(TextField.textInputUssName);
            textEditor.value = value;
            textInput.style.unityTextAlign = textElement.resolvedStyle.unityTextAlign;
            textInput.style.fontSize = textElement.resolvedStyle.fontSize;
            textInput.style.unityFontStyleAndWeight = textElement.resolvedStyle.unityFontStyleAndWeight;

            textEditor.multiline = value != null && value.Contains("\n");

            GetAlignmentFromTextAnchor(textElement.resolvedStyle.unityTextAlign, out var alignItems, out var justifyContent);

            var textEditorContainer = textEditor.parent;
            textEditorContainer.style.paddingLeft = s_EditedTextElement.resolvedStyle.paddingLeft;
            textEditorContainer.style.paddingTop = s_EditedTextElement.resolvedStyle.paddingTop;
            textEditorContainer.style.paddingRight = s_EditedTextElement.resolvedStyle.paddingRight;
            textEditorContainer.style.paddingBottom = s_EditedTextElement.resolvedStyle.paddingBottom;
            textEditorContainer.style.alignItems = alignItems;
            textEditorContainer.style.justifyContent = justifyContent;
            UpdateTextEditorGeometry();
            textElement.RegisterCallback<GeometryChangedEvent>(OnTextElementGeometryChanged);

            textEditor.schedule.Execute(a => textInput.Focus());
            textEditor.SelectAll();

            textInput.RegisterCallback<FocusOutEvent>(OnFocusOutEvent);
            textEditor.RegisterCallback<ChangeEvent<string>>(OnTextChanged);
        }

        static void OnTextElementGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateTextEditorGeometry();
        }

        static void UpdateTextEditorGeometry()
        {
            // The text element may have been removed from its hierarchy if the text is emoty. This is the case with Fields.
            if (s_EditedTextElement.panel == null)
                return;

            var textElementPos =
                s_EditedTextElement.parent.ChangeCoordinatesTo(s_Viewport.documentRootElement, new Vector2(s_EditedTextElement.layout.x, s_EditedTextElement.layout.y));

            var textEditorContainer = s_Viewport.textEditor.parent;

            // Note: Set the font here it because the font maybe still null of empty label of field in OpenEditor()
            s_Viewport.textEditor.Q(TextField.textInputUssName).style.unityFont = s_EditedTextElement.resolvedStyle.unityFont;

            textEditorContainer.style.left = textElementPos.x;
            textEditorContainer.style.top = textElementPos.y;
            textEditorContainer.style.width = Mathf.Max(s_TextEditorMinWidth, s_EditedTextElement.layout.width);
            textEditorContainer.style.height = Mathf.Max(s_TextEditorMinHeight, s_EditedTextElement.layout.height);
        }

        public static void CloseEditor()
        {
            if (s_Viewport == null)
                return;

            s_Viewport.textEditor.Q(TextField.textInputUssName).UnregisterCallback<FocusOutEvent>(OnFocusOutEvent);
            s_Viewport.textEditor.UnregisterCallback<ChangeEvent<string>>(OnTextChanged);
            s_Viewport.editorLayer.AddToClassList(BuilderConstants.HiddenStyleClassName);
            s_EditedTextElement.UnregisterCallback<GeometryChangedEvent>(OnTextElementGeometryChanged);
            s_Viewport = null;
            s_EditedElement = null;
            s_EditedTextAttribute = null;
        }

        static void OnTextChanged(ChangeEvent<string> evt)
        {
            s_EditedElement.SetValueByReflection(s_EditedTextAttribute.name, string.IsNullOrEmpty(evt.newValue) ? s_DummyText : evt.newValue);
        }

        static void OnFocusOutEvent(FocusOutEvent evt)
        {
            OnEditTextFinished();
        }

        static void OnEditTextFinished()
        {
            if (s_EditedElement == null)
                return;

            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(s_Viewport.paneWindow.document.visualTreeAsset, BuilderConstants.ChangeAttributeValueUndoMessage);

            var newValue = s_Viewport.textEditor.value;
            var type = s_EditedElement.GetType();
            var vea = s_EditedElement.GetVisualElementAsset();
            var oldValue = vea.GetAttributeValue(s_EditedTextAttribute.name);

            if (newValue != oldValue)
            {
                // Set value in asset.
                vea.SetAttributeValue(s_EditedTextAttribute.name, newValue);

                var fullTypeName = type.ToString();

                if (VisualElementFactoryRegistry.TryGetValue(fullTypeName, out var factoryList))
                {
                    var traits = factoryList[0].GetTraits();

                    if (traits == null)
                    {
                        CloseEditor();
                        return;
                    }

                    var context = new CreationContext();

                    try
                    {
                        traits.Init(s_EditedElement, vea, context);
                    }
                    catch
                    {
                        // HACK: This throws in 2019.3.0a4 because usageHints property throws when set after the element has already been added to the panel.
                    }
                }

                s_Viewport.selection.NotifyOfHierarchyChange(s_Viewport);
            }
            CloseEditor();
        }
    }
}
