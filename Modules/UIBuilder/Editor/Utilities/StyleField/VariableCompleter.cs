// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal class VariableCompleter : FieldSearchCompleter<VariableInfo>
    {
        public static readonly string s_ItemUssClassName = "unity-field-search-completer-popup__item";
        public static readonly string s_ItemNameLabelName = "nameLabel";
        public static readonly string s_ItemNameLabelUssClassName = "unity-field-search-completer-popup__item__name-label";
        public static readonly string s_ItemEditorOnlyLabelName = "editorOnlyLabel";
        public static readonly string s_ItemEditorOnlyLabelUssClassName = "unity-field-search-completer-popup__item__editor-only-label";

        VariableEditingHandler m_Handler;
        VariableInfoView m_DetailsView;

        public VariableCompleter(VariableEditingHandler handler)
            : base(handler.variableField != null ? handler.variableField.textField : null)
        {
            m_Handler = handler;
            getFilterFromTextCallback = (text) => text != null ? text.TrimStart('-') : null;
            dataSourceCallback = () =>
            {
                return StyleVariableUtilities.GetAllAvailableVariables(handler.inspector.currentVisualElement, GetCompatibleStyleValueTypes(handler), handler.inspector.document.fileSettings.editorExtensionMode);
            };
            makeItem = () =>
            {
                var item = new VisualElement();

                item.AddToClassList(s_ItemUssClassName);
                var nameLabel = new Label();
                var editorOnlyLabel = new Label(BuilderConstants.EditorOnlyTag);
                nameLabel.AddToClassList(s_ItemNameLabelUssClassName);
                nameLabel.name = s_ItemNameLabelName;
                nameLabel.style.textOverflow = TextOverflow.Ellipsis;
                editorOnlyLabel.AddToClassList(s_ItemEditorOnlyLabelUssClassName);
                editorOnlyLabel.AddToClassList("unity-builder-tag-pill");
                editorOnlyLabel.name = s_ItemEditorOnlyLabelName;
                item.Add(nameLabel);
                item.Add(editorOnlyLabel);
                return item;
            };
            bindItem = (e, i) =>
            {
                var res = results[i];

                e.Q<Label>(s_ItemNameLabelName).text = res.name;
                e.Q<Label>(s_ItemEditorOnlyLabelName).EnableInClassList(BuilderConstants.HiddenStyleClassName, !res.isEditorVar);
            };

            m_DetailsView = new VariableInfoView();
            m_DetailsView.AddToClassList(BuilderConstants.HiddenStyleClassName);
            detailsContent = m_DetailsView;
            selectionChanged += data =>
            {
                m_DetailsView.SetInfo(data);
                if (data.IsValid())
                {
                    m_DetailsView.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);
                }
                else
                {
                    m_DetailsView.AddToClassList(BuilderConstants.HiddenStyleClassName);
                }
            };

            matcherCallback = Matcher;
            getTextFromDataCallback = GetVarName;
        }

        static string GetVarName(VariableInfo data)
        {
            return data.name;
        }

        public static StyleValueType[] GetCompatibleStyleValueTypes(VariableEditingHandler handler)
        {
            var styleType = StyleDebug.GetComputedStyleType(handler.styleName);
            if (styleType == null)
                return new[] { StyleValueType.Invalid };

            var val = StyleDebug.GetComputedStyleValue(handler.inspector.currentVisualElement.computedStyle, handler.styleName);

            if (BuilderInspectorStyleFields.IsComputedStyleFloat(val) ||
                BuilderInspectorStyleFields.IsComputedStyleInt(val) ||
                BuilderInspectorStyleFields.IsComputedStyleLength(val) ||
                BuilderInspectorStyleFields.IsComputedStyleList<TimeValue>(val))
            {
                return new[] { StyleValueType.Float, StyleValueType.Dimension };
            }

            if (BuilderInspectorStyleFields.IsComputedStyleColor(val))
            {
                return new[] { StyleValueType.Color };
            }

            if (BuilderInspectorStyleFields.IsComputedStyleFont(val, handler.styleName))
            {
                return new[] { StyleValueType.AssetReference, StyleValueType.ResourcePath };
            }

            if (BuilderInspectorStyleFields.IsComputedStyleBackground(val))
            {
                return new[] { StyleValueType.ScalableImage, StyleValueType.AssetReference, StyleValueType.ResourcePath };
            }

            if (BuilderInspectorStyleFields.IsComputedStyleCursor(val) ||
                BuilderInspectorStyleFields.IsComputedStyleList<StylePropertyName>(val))
            {
                return new[] { StyleValueType.Enum, StyleValueType.ScalableImage, StyleValueType.AssetReference, StyleValueType.ResourcePath };
            }

            if (BuilderInspectorStyleFields.IsComputedStyleEnum(val, styleType) ||
                BuilderInspectorStyleFields.IsComputedStyleList<EasingFunction>(val))
            {
                return new[] { StyleValueType.Enum };
            }

            return new[] { StyleValueType.Invalid };
        }

        bool Matcher(string filter, VariableInfo data)
        {
            var text = data.name;
            return string.IsNullOrEmpty(text) ? false : text.Contains(filter);
        }

        protected override bool IsValidText(string text)
        {
            if (m_Handler.variableField != null && m_Handler.variableField.textField == textField)
            {
                return true;
            }
            else
            {
                return text.StartsWith(BuilderConstants.UssVariablePrefix);
            }
        }
    }
}
