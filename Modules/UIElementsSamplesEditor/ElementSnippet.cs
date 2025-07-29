// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Text.RegularExpressions;
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

        // Dark Colors
        private const string s_KeywordColorDark = "#569cd6";
        private const string s_NameColorDark = "#92caf4";
        private const string s_CommentColorDark = "#57a64a";
        private const string s_MethodColorDark = "#b8d7a3";
        private const string s_TypeColorDark = "#4ec9b0";
        private const string s_StringColorDark = "#d69d85";

        // Light Colors
        private const string s_KeywordColorLight = "#1800ff";
        private const string s_NameColorLight = "#ff2702";
        private const string s_CommentColorLight = "#008000";
        private const string s_MethodColorLight = "#74531f";
        private const string s_TypeColorLight = "#008080";
        private const string s_StringColorLight = "#a3152e";

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
            {
                text = ProcessCSharp(text);
                text = addSyntaxColors_CSharp(text);
            }
            else if (path.EndsWith("_uxml.asset")) // UXML
            {
                text = addSyntaxColors_UXML(text);
            }
            else if (path.EndsWith("_uss.asset")) // USS
            {
                text = addSyntaxColors_USS(text);
            }

            var lineCount = text.Count(x => x == '\n') + 1;
            string lineNumbersText = "";
            for (int i = 1; i <= lineCount; ++i)
            {
                if (!string.IsNullOrEmpty(lineNumbersText))
                    lineNumbersText += "\n";

                lineNumbersText += i.ToString();
            }

            var lineNumbers = new Label(lineNumbersText);
            lineNumbers.AddToClassList(s_CodeClassName);
            lineNumbers.AddToClassList(s_CodeLineNumbersClassName);
            lineNumbers.AddToClassList(s_CodeInputClassName);

            var code = new Label(text);
            code.selection.isSelectable = true;
            code.AddToClassList(s_CodeClassName);
            code.AddToClassList(s_CodeTextClassName);
            code.AddToClassList(s_CodeInputClassName);

            var codeScrollView = new ScrollView(ScrollViewMode.Horizontal);
            codeScrollView.mouseWheelScrollSize = 0; // We want the scroll wheel to only work on the root vertical scroll view.
            codeScrollView.horizontalScroller.lowButton.focusable = true; // UUM-105775 - Prevents scroller clicks from focusing the code area.
            codeScrollView.horizontalScroller.highButton.focusable = true;
            codeScrollView.Add(code);

            var codeOuterContainer = new VisualElement();
            codeOuterContainer.AddToClassList(s_CodeCodeOuterContainerClassName);
            container.Add(codeOuterContainer);

            var codeContainer = new VisualElement();
            codeContainer.AddToClassList(s_CodeCodeContainerClassName);
            codeOuterContainer.Add(codeContainer);

            codeContainer.Add(lineNumbers);
            codeContainer.Add(codeScrollView);

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

        static string addSyntaxColors_CSharp(string code)
        {
            // Define colors for each token type
            string keywordColor = s_KeywordColorDark;
            string typeColor = s_TypeColorDark;
            string stringColor = s_StringColorDark;
            string commentColor = s_CommentColorDark;
            string methodColor = s_MethodColorDark;
            string classColor = s_TypeColorDark;
            if (EditorGUIUtility.isProSkin == false) // Light theme
            {
                keywordColor = s_KeywordColorLight;
                typeColor = s_TypeColorLight;
                stringColor = s_StringColorLight;
                commentColor = s_CommentColorLight;
                methodColor = s_MethodColorLight;
                classColor = s_TypeColorLight;
            }

            // Handle multiline comments (/* ... */)
            code = Regex.Replace(code, @"/\*.*?\*/", m => $"<color={commentColor}>{m.Value}</color>", RegexOptions.Singleline);

            // Handle single-line comments (//...)
            code = Regex.Replace(code, @"(//.*?$)", $"<color={commentColor}>$1</color>", RegexOptions.Multiline);

            // Handle verbatim strings (@"...")
            code = Regex.Replace(code, @"@""([^""]|"""")*""", m => $"<color={stringColor}>{m.Value}</color>");

            // Handle interpolated strings ($"...")
            code = Regex.Replace(code, @"\$""([^""\\]|\\.|"")*""", m => $"<color={stringColor}>{m.Value}</color>");

            // Handle regular strings ("...")
            code = Regex.Replace(code, @"""([^""\\]|\\.)*""", m => $"<color={stringColor}>{m.Value}</color>");

            // Highlight class names after "class" keyword
            code = Regex.Replace(code, @"\bclass\s+([A-Z][a-zA-Z0-9_]*)", m =>
            {
                var className = m.Groups[1].Value;
                return $"class <color={classColor}>{className}</color>";
            });

            // Highlight keywords
            string[] keywords = {
                "using", "namespace", "class", "public", "private", "protected", "internal", "static", "void",
                "int", "float", "string", "bool", "object", "var", "return", "new", "if", "else", "while", "for", "foreach",
                "in", "break", "continue", "switch", "case", "default", "do", "try", "catch", "finally", "throw",
                "true", "false", "null", "this", "base", "override", "abstract", "virtual", "sealed", "as",
                "int", "float", "bool", "string", "object", "readonly", "enum", "typeof", "partial"
            };

            foreach (var keyword in keywords)
            {
                code = Regex.Replace(code, $@"\b{keyword}\b", $"<color={keywordColor}>{keyword}</color>");
            }

            // Highlight common types
            string[] types = {
                "AreaScope",
                "Button",
                "Color",
                "CustomEditor",
                "Editor",
                "EditorGUI",
                "EditorGUILayout",
                "EditorGUIUtility",
                "Event",
                "GUIContent",
                "GUILayout",
                "GUILayoutUtility",
                "GUIStyle",
                "GUIStyleState",
                "HorizontalScope",
                "IMGUIContainer",
                "Label",
                "MouseEnterEvent",
                "MouseLeaveEvent",
                "MouseWheelEvent",
                "PropertyField",
                "Q",
                "ChangeEvent",
                "Object",
                "ObjectField",
                "Query",
                "Rect",
                "RectOffset",
                "Regex",
                "Texture2D",
                "UxmlAttribute",
                "UxmlElement",
                "VerticalScope",
                "VisualElement",
                "VisualTreeAsset",
            };
            foreach (var type in types)
            {
                code = Regex.Replace(code, $@"\b{type}\b", $"<color={typeColor}>{type}</color>");
            }

            // Highlight method names - words followed by (
            code = Regex.Replace(code, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*(?=\()", m =>
            {
                var name = m.Groups[1].Value;
                if (!types.Contains(name) && !keywords.Contains(name))
                    return $"<color={methodColor}>{name}</color>";
                else
                    return name;
            });

            return code;
        }

        static string addSyntaxColors_UXML(string xml)
        {
            // Highlight colors
            string tagColor = s_KeywordColorDark;
            string attrNameColor = s_NameColorDark;
            string attrValueColor = s_KeywordColorDark;
            string commentColor = s_CommentColorDark;
            if (EditorGUIUtility.isProSkin == false) // Light theme
            {
                tagColor = s_KeywordColorLight;
                attrNameColor = s_NameColorLight;
                attrValueColor = s_KeywordColorLight;
                commentColor = s_CommentColorLight;
            }

            // Placeholder-safe color tag helper
            string EncodeColorTag(string content, string color)
            {
                return $"[[LT]]color[[EQ]]{color}[[GT]]{content}[[LT]]/color[[GT]]";
            }

            // 1. Highlight comments: <!-- ... -->
            xml = Regex.Replace(xml, @"<!--(.*?)-->", m =>
                EncodeColorTag($"<!--{m.Groups[1].Value}-->", commentColor),
                RegexOptions.Singleline);

            // 2. Highlight tag names (without coloring angle brackets)
            xml = Regex.Replace(xml, @"(<\/?)([\w:]+)", m =>
                $"{m.Groups[1].Value}{EncodeColorTag(m.Groups[2].Value, tagColor)}");

            // 3. Highlight attribute names before '='
            xml = Regex.Replace(xml, @"\b(\w+)(=)", m =>
                $"{EncodeColorTag(m.Groups[1].Value, attrNameColor)}{m.Groups[2].Value}");

            // 4. Highlight attribute string values
            xml = Regex.Replace(xml, @"""[^""]*""", m =>
                EncodeColorTag(m.Value, attrValueColor));

            // 5. Decode placeholder symbols
            xml = xml.Replace("[[LT]]", "<")
                    .Replace("[[GT]]", ">")
                    .Replace("[[EQ]]", "=");

            return xml;
        }

        static string addSyntaxColors_USS(string uss)
        {
            // Highlight colors
            string selectorColor = s_KeywordColorDark;
            string propertyColor = s_TypeColorDark;
            string stringColor = s_StringColorDark;
            string commentColor = s_CommentColorDark;
            string numberColor = s_MethodColorDark;
            if (EditorGUIUtility.isProSkin == false) // Light theme
            {
                selectorColor = s_KeywordColorLight;
                propertyColor = s_TypeColorLight;
                stringColor = s_StringColorLight;
                commentColor = s_CommentColorLight;
                numberColor = s_MethodColorLight;
            }

            // Placeholder-safe coloring
            string EncodeColorTag(string content, string color)
            {
                return $"[[LT]]color[[EQ]]{color}[[GT]]{content}[[LT]]/color[[GT]]";
            }

            // 1. Highlight comments (protect early)
            uss = Regex.Replace(uss, @"/\*.*?\*/", m =>
                EncodeColorTag(m.Value, commentColor), RegexOptions.Singleline);

            // 2. Highlight strings
            uss = Regex.Replace(uss, @"""[^""]*""", m =>
                EncodeColorTag(m.Value, stringColor));

            // 4. Highlight selectors:
            // - Class: .class
            // - ID: #id
            // - Pseudo: :hover
            uss = Regex.Replace(uss, @"([.#:][a-zA-Z_][\w\-]*)", m =>
                EncodeColorTag(m.Value, selectorColor));

            // 3. Highlight numeric values (e.g., 16px, 100%, 1.5em)
            uss = Regex.Replace(uss, @"\b\d+(\.\d+)?(px|em|rem|%|vh|vw|fr)?\b", m =>
                EncodeColorTag(m.Value, numberColor));

            // 5. Highlight property names before colon (e.g., color:)
            // This runs after selector regex, so selectors inside properties won't match
            uss = Regex.Replace(uss, @"(\b[a-zA-Z_-]+)(\s*:)", m =>
                $"{EncodeColorTag(m.Groups[1].Value, propertyColor)}{m.Groups[2].Value}");

            // Final: decode placeholder tags
            uss = uss.Replace("[[LT]]", "<")
                    .Replace("[[GT]]", ">")
                    .Replace("[[EQ]]", "=");

            return uss;
        }
    }
}
