// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    static class BuilderInPlaceTextEditingUtilities
    {
        struct EditingContext
        {
            public VisualElement editedElement;
            public TextElement targetTextElement;
            public string uxmlAttributeName;
            public string propertyName;

            public bool CanEdit => null != targetTextElement
                                   && null != targetTextElement.panel
                                   && !string.IsNullOrWhiteSpace(uxmlAttributeName);
        }


        const string k_DummyText = " ";
        const string k_TextAttributeName = "text";

        private static bool CheckIfTransformIsUsed(VisualElement element, VisualElement root)
        {
            var parent = element.hierarchy.parent;

            while (parent != null && parent != root)
            {
                var computedStyle = parent.computedStyle;

                if (!computedStyle.translate.IsNone())
                    return true;

                if (!computedStyle.rotate.IsNone())
                    return true;

                if (computedStyle.scale != Scale.Initial())
                    return true;

                if (computedStyle.transformOrigin != TransformOrigin.Initial())
                    return true;

                parent = parent.hierarchy.parent;
            }

            return false;
        }

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

        static EditingContext GetAttributeToEdit(VisualElement editedElement, string uxmlAttributeName)
        {
            var context = new EditingContext
            {
                editedElement = editedElement
            };

            switch (editedElement)
            {
                case IPrefixLabel prefixLabel:
                    context.targetTextElement = prefixLabel.labelElement;
                    context.uxmlAttributeName = nameof(IPrefixLabel.label);
                    context.propertyName = nameof(IPrefixLabel.label);
                    break;
                case Foldout foldout:
                    context.targetTextElement = foldout.toggle.boolFieldLabelElement;
                    context.uxmlAttributeName = nameof(FoldoutField.text);
                    context.propertyName = nameof(FoldoutField.text);
                    break;
                case TextElement textElement:
                    context.targetTextElement = textElement;
                    context.uxmlAttributeName = uxmlAttributeName;
                    context.propertyName = BuilderNameUtilities.ConvertDashToCamel(uxmlAttributeName);;
                    break;
                case BaseListView {showFoldoutHeader: true} listView:
                    context.targetTextElement = listView.headerFoldout.toggle.boolFieldLabelElement;
                    context.uxmlAttributeName = "header-title";
                    context.propertyName = nameof(BaseListView.headerTitle);
                    break;
                case Tab tab:
                    context.targetTextElement = tab.headerLabel;
                    context.uxmlAttributeName = nameof(Tab.label);
                    context.propertyName = nameof(Tab.label);
                    break;
                case GroupBox groupBox:
                    context.targetTextElement = groupBox.titleLabel;
                    context.uxmlAttributeName = nameof(GroupBox.text);
                    context.propertyName = nameof(GroupBox.text);
                    break;
            }

            return context;
        }

        public static void OpenEditor(VisualElement element, Vector2 pos, VisualElement documentRoot)
        {
            var viewport = element.GetFirstAncestorOfType<BuilderViewport>();
            var editorLayer = viewport.editorLayer;
            var textEditor = viewport.textEditor;

            var context = GetAttributeToEdit(element, k_TextAttributeName);

            if (context.targetTextElement == null || string.IsNullOrEmpty(context.uxmlAttributeName))
                return;

            if (CheckIfTransformIsUsed(context.targetTextElement, documentRoot))
            {
                Builder.ShowWarning("In-place editing is not supported when any ancestor is setting a transform related style property.");
                return;
            }

            if (!context.CanEdit)
                return;

            if (viewport.bindingsCache.HasResolvedBinding(element, context.uxmlAttributeName))
            {
                Builder.ShowWarning(string.Format(BuilderConstants.CannotEditBoundPropertyMessage, context.uxmlAttributeName));
                return;
            }

            var value = context.editedElement.GetValueByReflection(context.propertyName) as string;

            // To ensure the text element is visible
            if (string.IsNullOrEmpty(value))
            {
                context.editedElement.SetValueByReflection(context.propertyName, k_DummyText);
            }

            editorLayer.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);

            var textInput = textEditor.Q(TextField.textInputUssName);
            textEditor.SetValueWithoutNotify(value);

            textInput.style.unityTextAlign = context.targetTextElement.computedStyle.unityTextAlign;
            textInput.style.fontSize = context.targetTextElement.computedStyle.fontSize;
            textInput.style.unityFontStyleAndWeight = context.targetTextElement.computedStyle.unityFontStyleAndWeight;
            textInput.style.whiteSpace = context.targetTextElement.computedStyle.whiteSpace;
            textInput.style.width = context.targetTextElement.computedStyle.width;
            textInput.style.height = context.targetTextElement.computedStyle.height;
            textInput.style.borderBottomWidth = context.targetTextElement.computedStyle.borderBottomWidth;
            textInput.style.borderTopWidth = context.targetTextElement.computedStyle.borderTopWidth;
            textInput.style.borderLeftWidth = context.targetTextElement.computedStyle.borderLeftWidth;
            textInput.style.borderRightWidth = context.targetTextElement.computedStyle.borderRightWidth;
            textInput.style.paddingBottom = context.targetTextElement.computedStyle.paddingBottom;
            textInput.style.paddingTop = context.targetTextElement.computedStyle.paddingTop;
            textInput.style.paddingLeft = context.targetTextElement.computedStyle.paddingLeft;
            textInput.style.paddingRight = context.targetTextElement.computedStyle.paddingRight;
            textInput.style.letterSpacing = context.targetTextElement.computedStyle.letterSpacing;
            textInput.style.wordSpacing = context.targetTextElement.computedStyle.wordSpacing;
            textInput.style.unityParagraphSpacing = context.targetTextElement.computedStyle.unityParagraphSpacing;
            textInput.style.textOverflow = context.targetTextElement.computedStyle.textOverflow;
            textInput.style.overflow = context.targetTextElement.computedStyle.overflow == OverflowInternal.Visible ? Overflow.Visible : Overflow.Hidden;

            textEditor.style.translate = context.targetTextElement.computedStyle.translate;
            textEditor.style.transformOrigin = context.targetTextElement.computedStyle.transformOrigin;
            textEditor.style.rotate = context.targetTextElement.computedStyle.rotate;
            textEditor.style.scale = context.targetTextElement.computedStyle.scale;

            GetAlignmentFromTextAnchor(context.targetTextElement.computedStyle.unityTextAlign, out var alignItems, out var justifyContent);

            UpdateTextEditorGeometry(context);
            context.targetTextElement.RegisterCallback<GeometryChangedEvent, EditingContext>(OnTextElementGeometryChanged, context);

            textEditor.schedule.Execute(a => textEditor.Focus());
            textEditor.textSelection.SelectAll();

            textInput.RegisterCallback<FocusOutEvent, EditingContext>(OnFocusOutEvent, context, TrickleDown.TrickleDown);
            textEditor.RegisterCallback<ChangeEvent<string>, EditingContext>(OnTextChanged, context);
        }

        static void OnTextElementGeometryChanged(GeometryChangedEvent evt, EditingContext context)
        {
            UpdateTextEditorGeometry(context);
        }

        static void UpdateTextEditorGeometry(EditingContext context)
        {
            if (!context.CanEdit)
                return;

            var viewport = context.editedElement.GetFirstOfType<BuilderViewport>();
            if (null == viewport)
                return;

            var textElementPos =
                context.targetTextElement.parent.ChangeCoordinatesTo(viewport.documentRootElement, new Vector2(context.targetTextElement.layout.x, context.targetTextElement.layout.y));

            var textEditorContainer = viewport.textEditor.parent;

            // Note: Set the font here it because the font maybe still null of empty label of field in OpenEditor()
            viewport.textEditor.Q(TextField.textInputUssName).style.unityFont = context.targetTextElement.computedStyle.unityFont;
            viewport.textEditor.Q(TextField.textInputUssName).style.unityFontDefinition = context.targetTextElement.computedStyle.unityFontDefinition;

            textEditorContainer.style.left = textElementPos.x;
            textEditorContainer.style.top = textElementPos.y;
            var textInput = textEditorContainer.Q(TextField.textInputUssName);

            textInput.style.width = context.targetTextElement.layout.width;
            textInput.style.height = context.targetTextElement.layout.height;
        }

        private static void CloseEditor(EditingContext context)
        {
            var viewport = context.editedElement.GetFirstAncestorOfType<BuilderViewport>();
            if (null == viewport)
                return;

            viewport.textEditor.UnregisterCallback<FocusOutEvent, EditingContext>(OnFocusOutEvent, TrickleDown.TrickleDown);
            viewport.textEditor.UnregisterCallback<ChangeEvent<string>, EditingContext>(OnTextChanged);
            viewport.editorLayer.AddToClassList(BuilderConstants.HiddenStyleClassName);
            context.targetTextElement.UnregisterCallback<GeometryChangedEvent, EditingContext>(OnTextElementGeometryChanged);
        }

        static void OnTextChanged(ChangeEvent<string> evt, EditingContext context)
        {
            context.editedElement.SetValueByReflection(context.propertyName, string.IsNullOrEmpty(evt.newValue) ? k_DummyText : evt.newValue);
        }

        static void OnFocusOutEvent(FocusOutEvent evt, EditingContext context)
        {
            OnEditTextFinished(context);
        }

        static void OnEditTextFinished(EditingContext context)
        {
            if (!context.CanEdit)
                return;

            var viewport = context.editedElement.GetFirstAncestorOfType<BuilderViewport>();
            if (null == viewport)
                return;

            var editedElement = context.editedElement;
            var uxmlAttributeName = context.uxmlAttributeName;

            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(viewport.paneWindow.document.visualTreeAsset, BuilderConstants.ChangeAttributeValueUndoMessage);

            var newValue = viewport.textEditor.value;
            var type = editedElement.GetType();
            var vea = editedElement.GetVisualElementAsset();
            var oldValue = vea.GetAttributeValue(uxmlAttributeName);

            if (newValue != oldValue)
            {
                // UxmlSerializedData
                if (!BuilderUxmlAttributesView.alwaysUseUxmlTraits && vea.serializedData != null && UxmlSerializedDataRegistry.GetDescription(vea.fullTypeName) is {} description)
                {
                    var attributeDescription = description.FindAttributeWithUxmlName(uxmlAttributeName);
                    if (attributeDescription != null)
                    {
                        attributeDescription.SetSerializedValue(vea.serializedData, newValue);
                        attributeDescription.SetSerializedValueAttributeFlags(vea.serializedData, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);
                        vea.SetAttribute(uxmlAttributeName, newValue);
                    }
                }
                else // Fallback to factory
                {
                    vea.SetAttribute(context.uxmlAttributeName, newValue);

                    var fullTypeName = type.ToString();

                    if (VisualElementFactoryRegistry.TryGetValue(fullTypeName, out var factoryList))
                    {
                        var traits = factoryList[0].GetTraits() as UxmlTraits;

                        if (traits == null)
                        {
                            CloseEditor(context);
                            return;
                        }

                        var creationContext = new CreationContext();

                        try
                        {
                            // We need to clear bindings before calling Init to avoid corrupting the data source.
                            BuilderBindingUtility.ClearUxmlBindings(editedElement);

                            traits.Init(editedElement, vea, creationContext);
                        }
                        catch
                        {
                            // HACK: This throws in 2019.3.0a4 because usageHints property throws when set after the element has already been added to the panel.
                        }
                    }
                }

                viewport.selection.NotifyOfHierarchyChange(viewport);
            }
            CloseEditor(context);
        }
    }
}
