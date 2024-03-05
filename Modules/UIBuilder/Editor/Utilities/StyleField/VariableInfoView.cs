// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    class VariableInfoView : VisualElement
    {
        static readonly string s_UssClassName = "unity-builder-inspector__varinfo-view";
        static readonly string s_ValueAndPreviewUssClassName = s_UssClassName + "__value-preview-container";
        static readonly string s_PreviewThumbnailUssClassName = s_ValueAndPreviewUssClassName + "--thumbnail";
        static readonly string s_EmptyText = "None";

        Label m_NameLabel;
        Label m_ValueLabel;
        Label m_StyleSheetLabel;
        Label m_DescriptionLabel;
        VisualElement m_DescriptionContainer;
        VisualElement m_ValueAndPreviewContainer;
        VisualElement m_Preview;
        Image m_Thumbnail;

        public string variableName
        {
            get => m_NameLabel.text;
            set => m_NameLabel.text = value;
        }

        public string variableValue
        {
            get => m_ValueLabel.text;
            set
            {
                m_ValueLabel.text = value;
                m_ValueLabel.EnableInClassList(BuilderConstants.HiddenStyleClassName, string.IsNullOrEmpty(value));
            }
        }

        public string sourceStyleSheet
        {
            get => m_StyleSheetLabel.text;
            set => m_StyleSheetLabel.text = value;
        }

        public string description
        {
            get => m_DescriptionLabel.text;
            set
            {
                m_DescriptionLabel.text = value;
                m_DescriptionContainer.EnableInClassList(BuilderConstants.HiddenStyleClassName, string.IsNullOrEmpty(value));
            }
        }

        public VariableInfoView()
        {
            AddToClassList(s_UssClassName);

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Inspector/VariableInfoView.uxml");
            template.CloneTree(this);
            m_NameLabel = this.Q<Label>("name-label");
            m_ValueLabel = this.Q<Label>("value-label");
            m_StyleSheetLabel = this.Q<Label>("stylesheet-label");
            m_DescriptionLabel = this.Q<Label>("description-label");
            m_DescriptionContainer = this.Q("description-container");
            m_ValueAndPreviewContainer = this.Q("value-preview-container");
            m_ValueAndPreviewContainer.AddToClassList(s_ValueAndPreviewUssClassName);
            m_Preview = this.Q("preview");
            m_Thumbnail = this.Q<Image>("thumbnail");
            m_NameLabel.style.textOverflow = TextOverflow.Ellipsis;
            m_NameLabel.displayTooltipWhenElided = true;
            m_ValueLabel.style.textOverflow = TextOverflow.Ellipsis;
            m_ValueLabel.style.unityTextOverflowPosition = TextOverflowPosition.Middle;
            m_ValueLabel.displayTooltipWhenElided = true;
            m_StyleSheetLabel.style.textOverflow = TextOverflow.Ellipsis;
            m_StyleSheetLabel.displayTooltipWhenElided = true;

            ClearUI();
        }

        void ClearUI()
        {
            variableName = s_EmptyText;
            variableValue = s_EmptyText;
            sourceStyleSheet = s_EmptyText;
            description = null;
            m_Preview.AddToClassList(BuilderConstants.HiddenStyleClassName);
            m_Preview.style.backgroundColor = Color.clear;
            m_ValueAndPreviewContainer.RemoveFromClassList(s_PreviewThumbnailUssClassName);
            m_Thumbnail.image = null;
            m_Thumbnail.vectorImage = null;
        }

        public void SetInfo(in VariableInfo info)
        {
            ClearUI();

            if (!info.IsValid())
                return;

            if (info.sheet)
            {
                var varStyleSheetOrigin = info.sheet;
                var fullPath = AssetDatabase.GetAssetPath(varStyleSheetOrigin);
                string displayPath = null;

                if (string.IsNullOrEmpty(fullPath))
                {
                    displayPath = varStyleSheetOrigin.name;
                }
                else
                {
                    if (fullPath == "Library/unity editor resources")
                        displayPath = varStyleSheetOrigin.name;
                    else
                        displayPath = Path.GetFileName(fullPath);
                }

                var valueText = StyleSheetToUss.ValueHandleToUssString(info.sheet, new UssExportOptions(), "",
                    info.styleVariable.handles[0].valueType != StyleValueType.Function ? info.styleVariable.handles[0] : info.styleVariable.handles[2]);

                variableValue = valueText;
                sourceStyleSheet = displayPath;
            }

            variableName = info.name;
            description = info.description;

            if (info.styleVariable.handles[0].valueType == StyleValueType.Color)
            {
                m_Preview.style.backgroundColor = info.sheet.ReadColor(info.styleVariable.handles[0]);
                m_Preview.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);
            }
            else if (info.styleVariable.handles[0].valueType == StyleValueType.Enum)
            {
                var colorName = info.sheet.ReadAsString(info.styleVariable.handles[0]);
                if (StyleSheetColor.TryGetColor(colorName.ToLower(), out var color))
                {
                    m_Preview.style.backgroundColor = color;
                    m_Preview.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);
                }
            }
            else if (info.styleVariable.handles[0].valueType == StyleValueType.ResourcePath || info.styleVariable.handles[0].valueType == StyleValueType.AssetReference)
            {
                var source = new ImageSource();
                var dpiScaling = 1.0f;
                if (StylePropertyReader.TryGetImageSourceFromValue(new StylePropertyValue{ sheet = info.sheet, handle = info.styleVariable.handles[0]}, dpiScaling, out source) == false)
                {
                    // Load a stand-in picture to make it easier to identify which image element is missing its picture
                    source.texture = Panel.LoadResource("d_console.warnicon", typeof(Texture2D), dpiScaling) as Texture2D;
                }

                m_Thumbnail.image = source.texture;
                m_Thumbnail.vectorImage = source.vectorImage;
                m_Preview.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);
                m_ValueAndPreviewContainer.AddToClassList(s_PreviewThumbnailUssClassName);
            }
        }
    }
}
