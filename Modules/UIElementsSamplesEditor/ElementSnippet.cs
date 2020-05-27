// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class ElementSnippet<T> where T : new()
    {
        private static readonly string s_CodeContainerClassName = "unity-snippet-code__container";
        private static readonly string s_CodeTitleClassName = "unity-snippet-code__title";
        private static readonly string s_CodeClassName = "unity-snippet-code__code";
        private static readonly string s_CodeLineNumbersClassName = "unity-snippet-code__code-line-numbers";
        private static readonly string s_CodeTextClassName = "unity-snippet-code__code-text";
        private static readonly string s_CodeInputClassName = "unity-snippet-code__input";
        private static readonly string s_CodeCodeOuterContainerClassName = "unity-snippet-code__code_outer_container";
        private static readonly string s_CodeCodeContainerClassName = "unity-snippet-code__code_container";

        private static readonly string s_DemoContainerClassName = "unity-samples-explorer__demo-container";
        private static readonly string s_SnippetsContainer = "unity-samples-explorer__snippets-container";

        private static readonly string s_SampleBeginTag = "/// <sample>";
        private static readonly string s_SampleEndTag = "/// </sample>";

        private static readonly string s_TextAssetsPath = "UIPackageResources/Snippets/Generated/";
        private static readonly string s_AssetsPath = "UIPackageResources/Snippets/";

        internal virtual void Apply(VisualElement container)
        {
        }

        private static string ProcessCSharp(string text)
        {
            const string badEndLine = "\r\n";
            const string goodEndLine = "\n";

            text = text.Replace(badEndLine, goodEndLine);

            int startIndex = text.IndexOf(s_SampleBeginTag);
            if (startIndex < 0)
                return text;

            int endIndex = text.IndexOf(s_SampleEndTag);
            if (endIndex < 0)
                return text;

            var actualStartIndex = startIndex + s_SampleBeginTag.Length;

            string leadingWhiteSpace = "";
            for (int i = actualStartIndex; i < endIndex; ++i)
            {
                if (char.IsWhiteSpace(text[i]))
                    continue;

                leadingWhiteSpace = text.Substring(actualStartIndex, i - actualStartIndex);
                actualStartIndex = i;
                break;
            }

            for (int i = endIndex - 1; i > actualStartIndex; --i)
            {
                if (char.IsWhiteSpace(text[i]))
                    continue;

                endIndex = i + 1;
                break;
            }

            text = text.Substring(actualStartIndex, endIndex - actualStartIndex);
            text = text.Replace(leadingWhiteSpace, goodEndLine);

            return text;
        }

        private static VisualElement CreateSnippetCode(string title, string path)
        {
            var container = new VisualElement();
            container.AddToClassList(s_CodeContainerClassName);

            var titleLabel = new Label(title);
            titleLabel.AddToClassList(s_CodeTitleClassName);
            container.Add(titleLabel);

            var asset = EditorGUIUtility.Load(path) as UIElementsSnippetAsset;
            if (asset == null)
                return null;

            var text = asset.text;
            if (string.IsNullOrEmpty(text))
                return null;

            if (path.EndsWith("_cs.asset")) // C#
                text = ProcessCSharp(text);

            var lineCount = text.Count(x => x == '\n') + 1;
            string lineNumbersText = "";
            for (int i = 1; i <= lineCount; ++i)
            {
                if (!string.IsNullOrEmpty(lineNumbersText))
                    lineNumbersText += "\n";

                lineNumbersText += i.ToString();
            }

            var lineNumbers = new Label(lineNumbersText);
            lineNumbers.RemoveFromClassList(TextField.ussClassName);
            lineNumbers.AddToClassList(s_CodeClassName);
            lineNumbers.AddToClassList(s_CodeLineNumbersClassName);
            lineNumbers.AddToClassList(s_CodeInputClassName);

            var code = new TextField(TextField.kMaxLengthNone, true, false, char.MinValue) { value = text };
            code.isReadOnly = true;
            code.RemoveFromClassList(TextField.ussClassName);
            code.AddToClassList(s_CodeClassName);
            code.AddToClassList(s_CodeTextClassName);

            var codeInput = code.Q(className: TextField.inputUssClassName);
            codeInput.AddToClassList(s_CodeInputClassName);

            var codeOuterContainer = new VisualElement();
            codeOuterContainer.AddToClassList(s_CodeCodeOuterContainerClassName);
            container.Add(codeOuterContainer);

            var codeContainer = new VisualElement();
            codeContainer.AddToClassList(s_CodeCodeContainerClassName);
            codeOuterContainer.Add(codeContainer);

            codeContainer.Add(lineNumbers);
            codeContainer.Add(code);

            return container;
        }

        internal static VisualElement Create(UIElementsSamples.SampleTreeItem item)
        {
            var snippet = new T() as ElementSnippet<T>;
            var tname = typeof(T).Name;

            var container = new VisualElement();
            container.AddToClassList(s_DemoContainerClassName);

            var csTextAssetPath = s_TextAssetsPath + "Code/" + tname + "_cs.asset";
            var ussTextAssetPath = s_TextAssetsPath + "StyleSheets/" + tname + "_uss.asset";
            var uxmlTextAssetPath = s_TextAssetsPath + "UXML/" + tname + "_uxml.asset";

            var ussPath = s_AssetsPath + "StyleSheets/" + tname + ".uss";
            var uxmlPath = s_AssetsPath + "UXML/" + tname + ".uxml";

            var csSnippet = CreateSnippetCode("C#", csTextAssetPath);

            var ussSnippet = CreateSnippetCode("USS", ussTextAssetPath);
            var styleSheet = EditorGUIUtility.Load(ussPath) as StyleSheet;
            container.styleSheets.Add(styleSheet);

            var uxmlSnippet = CreateSnippetCode("UXML", uxmlTextAssetPath);
            if (uxmlSnippet != null)
            {
                var visualTree = EditorGUIUtility.Load(uxmlPath) as VisualTreeAsset;
                visualTree.CloneTree(container);
            }

            snippet.Apply(container);

            var scrollView = new ScrollView();
            scrollView.AddToClassList(s_SnippetsContainer);
            scrollView.Add(csSnippet);
            scrollView.Add(ussSnippet);

            if (uxmlSnippet != null)
                scrollView.Add(uxmlSnippet);

            var panel = new VisualElement();
            panel.Add(container);
            panel.Add(scrollView);

            return panel;
        }
    }
}
