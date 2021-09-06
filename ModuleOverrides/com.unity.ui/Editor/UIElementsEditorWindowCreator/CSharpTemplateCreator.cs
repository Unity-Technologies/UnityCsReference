// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text;

namespace UnityEditor.UIElements
{
    static partial class UIElementsTemplate
    {
        public static string CreateCSharpTemplate(string cSharpName, bool addUXMLReference, bool addUSSLabel)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat(@"using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class {0} : EditorWindow
{{", cSharpName);

            if (addUXMLReference)
            {
                stringBuilder.AppendLine(@"
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;");
            }

            if (addUSSLabel)
            {
                stringBuilder.AppendLine(@"
    [SerializeField]
    private StyleSheet m_StyleSheet = default;");
            }

            stringBuilder.AppendFormat(@"
    [MenuItem(""Window/UI Toolkit/{0}"")]
    public static void ShowExample()
    {{
        {0} wnd = GetWindow<{0}>();
        wnd.titleContent = new GUIContent(""{0}"");
    }}

    public void CreateGUI()
    {{
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement label = new Label(""Hello World! From C#"");
        root.Add(label);
", cSharpName);
            if (addUXMLReference)
            {
                stringBuilder.Append(@"
        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);");
            }

            if (addUSSLabel)
            {
                stringBuilder.Append(@"
        // Add label
        VisualElement labelWithStyle = new Label(""Hello World! With Style"");
        labelWithStyle.AddToClassList(""custom-label"");
        labelWithStyle.styleSheets.Add(m_StyleSheet);
        root.Add(labelWithStyle);");
            }

            stringBuilder.AppendLine(@"
    }
}");
            var template = stringBuilder.ToString();

            // Normalize line endings
            template = ProjectWindowUtil.SetLineEndings(template, EditorSettings.lineEndingsForNewScripts);

            return template;
        }
    }
}
