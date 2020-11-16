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

        ScrollView m_ScrollView;

        VisualElement m_Container;
        VisualElement m_CodeContainer;
        VisualElement m_CodeOuterContainer;
        TextField m_Code;
        Label m_LineNumbers;

        private Button m_OpenTargetAssetSourceButton;
        ScriptableObject m_TargetAsset;
        BuilderPaneWindow m_PaneWindow;

        public BuilderCodePreview(BuilderPaneWindow paneWindow)
        {
            m_PaneWindow = paneWindow;
            
            m_ScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            m_ScrollView.AddToClassList(s_CodeScrollViewClassName);
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
            m_Code.isReadOnly = true;
            m_Code.name = s_CodeName;
            m_Code.RemoveFromClassList(TextField.ussClassName);
            m_Code.AddToClassList(s_CodeClassName);
            m_Code.AddToClassList(s_CodeTextClassName);
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
            m_Code.Q(TextInputBaseField<string>.textInputUssName).RegisterCallback<KeyDownEvent>(BlockEvent);
            m_Code.Q(TextInputBaseField<string>.textInputUssName).RegisterCallback<KeyUpEvent>(BlockEvent);

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
        }

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

            AssetDatabase.OpenAsset(m_TargetAsset);
        }

        void BlockEvent(KeyUpEvent evt)
        {
            // Allow copy/paste.
            if (evt.keyCode == KeyCode.C)
                return;

            evt.PreventDefault();
            evt.StopImmediatePropagation();
        }

        void BlockEvent(KeyDownEvent evt)
        {
            // Allow copy/paste.
            if (evt.keyCode == KeyCode.C)
                return;

            evt.PreventDefault();
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

            foreach (var c in text)
            {
                if (!char.IsControl(c))
                    printableCharCount++;

                if (printableCharCount > BuilderConstants.MaxTextPrintableCharCount)
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
                 clampedText += BuilderConstants.EllipsisText + $"\n{BuilderConstants.EllipsisText}\n({BuilderConstants.CodePreviewTruncatedTextMessage})";
            m_Code.value = clampedText;
        }
    }
}
