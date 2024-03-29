// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal abstract class BuilderCodePreview : BuilderPaneContent
    {
        static readonly string s_UssClassName = "unity-builder-code-preview";
        static readonly string s_CodeScrollViewClassName = "unity-builder-code__scroll-view";
        static readonly string s_CodeContainerClassName = "unity-builder-code__container";
        static readonly string s_CodeName = "unity-builder-code__code";
        static readonly string s_CodeClassName = "unity-builder-code__code";
        static readonly string s_CodeLineNumbersClassName = "unity-builder-code__code-line-numbers";
        static readonly string s_CodeTextClassName = "unity-builder-code__code-text";
        static readonly string s_CodeInputClassName = "unity-builder-code__input";
        static readonly string s_CodeCodeOuterContainerClassName = "unity-builder-code__code_outer_container";
        static readonly string s_CodeCodeContainerClassName = "unity-builder-code__code_container";
        static readonly string s_CodeOpenSourceFileClassName = "unity-builder-code__open-source-file-button";

        static readonly string s_TruncatedPreviewTextMessage = BuilderConstants.EllipsisText + $"\n({BuilderConstants.CodePreviewTruncatedTextMessage})";

        ScrollView m_ScrollView;

        VisualElement m_Container;
        VisualElement m_CodeContainer;
        VisualElement m_CodeOuterContainer;
        TextField m_Code;
        Label m_LineNumbers;

        bool m_NeedsRefreshOnResize;

        private Button m_OpenTargetAssetSourceButton;
        ScriptableObject m_TargetAsset;
        BuilderPaneWindow m_PaneWindow;

        public BuilderCodePreview(BuilderPaneWindow paneWindow)
        {
            m_PaneWindow = paneWindow;

            m_ScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            m_ScrollView.AddToClassList(s_CodeScrollViewClassName);
            m_ScrollView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            Add(m_ScrollView);

            AddToClassList(s_UssClassName);

            m_Container = new VisualElement();
            m_Container.AddToClassList(s_CodeContainerClassName);

            m_LineNumbers = new Label();
            m_LineNumbers.RemoveFromClassList(TextField.ussClassName);
            m_LineNumbers.AddToClassList(s_CodeClassName);
            m_LineNumbers.AddToClassList(s_CodeLineNumbersClassName);
            m_LineNumbers.AddToClassList(s_CodeInputClassName);

            m_Code = new TextField(TextField.kMaxLengthNone, true, false, char.MinValue);
            m_Code.textSelection.isSelectable = true;
            m_Code.isReadOnly = true;
            m_Code.name = s_CodeName;
            m_Code.RemoveFromClassList(TextField.ussClassName);
            m_Code.AddToClassList(s_CodeClassName);
            m_Code.AddToClassList(s_CodeTextClassName);
            m_Code.AddToClassList(disabledUssClassName);
            var codeInput = m_Code.Q(className: TextField.inputUssClassName);
            codeInput.AddToClassList(s_CodeInputClassName);

            m_CodeOuterContainer = new VisualElement();
            m_CodeOuterContainer.AddToClassList(s_CodeCodeOuterContainerClassName);
            m_Container.Add(m_CodeOuterContainer);

            m_CodeContainer = new VisualElement();
            m_CodeContainer.AddToClassList(s_CodeCodeContainerClassName);
            m_CodeOuterContainer.Add(m_CodeContainer);

            m_CodeContainer.Add(m_LineNumbers);
            m_CodeContainer.Add(m_Code);

            m_ScrollView.Add(m_Container);

            // Make sure the Hierarchy View gets focus when the pane gets focused.
            primaryFocusable = m_Code;

            // Make sure no key events get through to the code field.
            m_Code.RegisterCallback<KeyDownEvent>(BlockEvent, TrickleDown.TrickleDown);
            m_Code.RegisterCallback<KeyUpEvent>(BlockEvent, TrickleDown.TrickleDown);

            SetText(string.Empty);
        }

        protected override void OnAttachToPanelDefaultAction()
        {
            base.OnAttachToPanelDefaultAction();

            if (m_OpenTargetAssetSourceButton == null)
            {
                m_OpenTargetAssetSourceButton = new Button(OnOpenSourceFileButtonClick);
                m_OpenTargetAssetSourceButton.AddToClassList(s_CodeOpenSourceFileClassName);
                pane.toolbar.Add(m_OpenTargetAssetSourceButton);
            }

            RefreshPreviewIfVisible();
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!m_NeedsRefreshOnResize)
                return;

            RefreshPreview();
            m_NeedsRefreshOnResize = false;
        }

        protected void RefreshPreviewIfVisible()
        {
            RefreshHeader();

            if (m_ScrollView.contentViewport.resolvedStyle.height <= 0 ||
                m_ScrollView.contentViewport.resolvedStyle.width <= 0)
            {
                m_NeedsRefreshOnResize = true;
                return;
            }

            RefreshPreview();
        }

        protected abstract void RefreshPreview();
        protected abstract void RefreshHeader();

        protected abstract string previewAssetExtension { get; }

        string targetAssetPath
        {
            get
            {
                if (m_TargetAsset == null)
                    return string.Empty;

                return AssetDatabase.GetAssetPath(m_TargetAsset);
            }
        }

        private bool isTargetAssetAvailableOnDisk => !string.IsNullOrEmpty(targetAssetPath);
        protected bool hasDocument => m_PaneWindow != null && m_PaneWindow.document != null;
        protected BuilderDocument document => m_PaneWindow.document;

        void OnOpenSourceFileButtonClick()
        {
            if(!isTargetAssetAvailableOnDisk)
                return;

            AssetDatabase.OpenAsset(m_TargetAsset, BuilderConstants.OpenInIDELineNumber);
        }

        void BlockEvent(KeyUpEvent evt)
        {
            // Allow copy/paste.
            if (evt.keyCode == KeyCode.C)
                return;

            evt.StopImmediatePropagation();
        }

        void BlockEvent(KeyDownEvent evt)
        {
            // Allow copy/paste.
            if (evt.keyCode == KeyCode.C)
                return;

            evt.StopImmediatePropagation();
        }

        protected void SetTargetAsset(ScriptableObject targetAsset, bool hasUnsavedChanges)
        {
            if (pane == null)
                return;

            m_TargetAsset = targetAsset;
            pane.subTitle = BuilderAssetUtilities.GetAssetName(targetAsset, previewAssetExtension, hasUnsavedChanges);
            m_OpenTargetAssetSourceButton.style.display = isTargetAssetAvailableOnDisk ? DisplayStyle.Flex : DisplayStyle.None;
        }

        internal static string GetClampedText(string text, out bool truncated)
        {
            var clippedCharIndex = 0;
            var printableCharCount = 0;

            truncated = false;

            var maxTextPrintableCharCount = BuilderConstants.MaxTextPrintableCharCount;
            foreach (var c in text)
            {
                if (!char.IsControl(c))
                    printableCharCount++;

                if (printableCharCount > maxTextPrintableCharCount)
                {
                    truncated = true;
                    break;
                }
                clippedCharIndex++;
            }

            if (truncated)
            {
                return text.Substring(0, clippedCharIndex);
            }

            return text;
        }

        protected void SetText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                m_LineNumbers.text = string.Empty;
                m_Code.value = string.Empty;
                return;
            }

            var clampedText = GetClampedText(text, out var truncated);
            if (truncated)
                clampedText = clampedText.Substring(0, clampedText.Length - s_TruncatedPreviewTextMessage.Length);

            var lineCount = clampedText.Count(x => x == '\n') + 1;
            string lineNumbersText = "";
            for (int i = 1; i <= lineCount; ++i)
            {
                if (!string.IsNullOrEmpty(lineNumbersText))
                    lineNumbersText += "\n";

                lineNumbersText += i.ToString();
            }

            m_LineNumbers.text = lineNumbersText;

            if (truncated)
                clampedText += s_TruncatedPreviewTextMessage;

            m_Code.value = clampedText;
        }
    }
}
