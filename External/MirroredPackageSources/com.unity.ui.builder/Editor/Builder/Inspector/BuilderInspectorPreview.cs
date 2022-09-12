using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorPreview : BuilderPaneContent, IBuilderSelectionNotifier
    {
        static readonly string s_UssClassName = "unity-builder-selector-preview";
        static readonly string s_CheckerboardBackgroundClassName = s_UssClassName + "__checkerboard-background";
        static readonly string s_ToggleIconClassName = s_UssClassName + "__toggle-icon";
        static readonly string s_DefaultBackgroundClassName = s_UssClassName + "__default-layer";
        static readonly string s_TextClassName = s_UssClassName + "__selector-text";
        static readonly string s_ContainerClassName = s_UssClassName + "__text-container";
        static readonly string s_PreviewTextContent = "Unity\n0123";

        Label m_Text;
        VisualElement m_CheckerboardBackgroundElement;
        VisualElement m_DefaultBackgroundElement;
        ToolbarToggle m_BackgroundToggle;
        BuilderInspector m_Inspector;

        VisualElement currentVisualElement => m_Inspector.currentVisualElement;
        public VisualElement defaultBackgroundElement => m_DefaultBackgroundElement;
        public VisualElement checkerboardBackgroundElement => m_CheckerboardBackgroundElement;
        public ToolbarToggle backgroundToggle => m_BackgroundToggle;

        public BuilderInspectorPreview(BuilderInspector inspector)
        {
            m_Inspector = inspector;

            AddToClassList(s_UssClassName);

            // checkered and default backgrounds
            m_CheckerboardBackgroundElement = new CheckerboardBackground();
            m_CheckerboardBackgroundElement.AddToClassList(s_CheckerboardBackgroundClassName);

            m_DefaultBackgroundElement = new VisualElement();
            m_DefaultBackgroundElement.AddToClassList(s_DefaultBackgroundClassName);
            m_DefaultBackgroundElement.style.display = DisplayStyle.None;
            
            // preview text
            m_Text = new Label(s_PreviewTextContent);
            m_Text.AddToClassList(s_TextClassName);
            m_Text.RemoveFromClassList(Label.ussClassName);
            m_Text.RemoveFromClassList(TextElement.ussClassName);
            
            // transparency toggle
            m_BackgroundToggle = new ToolbarToggle();
            m_BackgroundToggle.tooltip = BuilderConstants.PreviewTransparencyToggleTooltip;
            m_BackgroundToggle.AddToClassList(s_ToggleIconClassName);
            m_BackgroundToggle.RegisterValueChangedCallback(ToggleBackground);

            // preview container
            var container = new VisualElement();
            container.AddToClassList(s_ContainerClassName);
            container.Add(m_CheckerboardBackgroundElement);
            container.Add(m_DefaultBackgroundElement);
            container.Add(m_Text);
            
            Add(container);
        }

        protected override void OnAttachToPanelDefaultAction()
        {
            base.OnAttachToPanelDefaultAction();
            RefreshPreview();
        }

        protected override void InitEllipsisMenu()
        {
            base.InitEllipsisMenu();

            if (pane == null)
                return;

            pane.AppendActionToEllipsisMenu(
                BuilderConstants.PreviewConvertToFloatingWindow,
                a => m_Inspector.OpenPreviewWindow(),
                a => DropdownMenuAction.Status.Normal);

            pane.AppendActionToEllipsisMenu(BuilderConstants.PreviewMinimizeInInspector,
                a => m_Inspector.TogglePreviewInInspector(),
                a => !m_Inspector.showingPreview ? 
                    DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }

        private void RefreshPreview()
        {
            if (currentVisualElement == null)
                return;
            
            SetTextStyles();
        }

        public void ToggleBackground(ChangeEvent<bool> evt)
        {
            var value = evt.newValue;
            
            m_CheckerboardBackgroundElement.style.display = value ? DisplayStyle.None : DisplayStyle.Flex; 
            m_DefaultBackgroundElement.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        private void SetTextStyles()
        {
            m_Text.style.unityFont = currentVisualElement.resolvedStyle.unityFont;
            m_Text.style.unityFontDefinition = currentVisualElement.resolvedStyle.unityFontDefinition;
            m_Text.style.unityFontStyleAndWeight = currentVisualElement.resolvedStyle.unityFontStyleAndWeight;
            m_Text.style.fontSize = currentVisualElement.resolvedStyle.fontSize;
            m_Text.style.color = currentVisualElement.resolvedStyle.color;
            m_Text.style.unityTextAlign = currentVisualElement.resolvedStyle.unityTextAlign;
            m_Text.style.whiteSpace = currentVisualElement.resolvedStyle.whiteSpace;
            m_Text.style.textOverflow = currentVisualElement.resolvedStyle.textOverflow;
            m_Text.style.unityTextOutlineWidth = currentVisualElement.resolvedStyle.unityTextOutlineWidth;
            m_Text.style.unityTextOutlineColor = currentVisualElement.resolvedStyle.unityTextOutlineColor;
            m_Text.style.textShadow = currentVisualElement.computedStyle.textShadow;
            m_Text.style.letterSpacing = currentVisualElement.resolvedStyle.letterSpacing;
            m_Text.style.wordSpacing = currentVisualElement.resolvedStyle.wordSpacing;
            m_Text.style.unityParagraphSpacing = currentVisualElement.resolvedStyle.unityParagraphSpacing;
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            if (changeType.HasFlag(BuilderHierarchyChangeType.FullRefresh))
            {
                RefreshPreview();
            }
        }

        public void SelectionChanged()
        {
            RefreshPreview();
        }

        public void StylingChanged(List<string> styles, BuilderStylingChangeType changeType)
        {
            RefreshPreview();
        }
    }
}
