using System;
namespace UnityEditor.UIElements
{
    static partial class UIElementsTemplate
    {
        public static string CreateCSharpTemplate(string cSharpName, string uxmlName, string ussName, string folder)
        {
            var csTemplate = string.Format(@"using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;


public class {0} : EditorWindow
{{
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
        root.Add(label);", cSharpName);

            if (uxmlName != string.Empty)
            {
                csTemplate = csTemplate + string.Format(@"

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(""{0}/{1}.uxml"");
        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);", folder, uxmlName);
            }

            if (ussName != string.Empty)
            {
                csTemplate += string.Format(@"

        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(""{0}/{1}.uss"");
        VisualElement labelWithStyle = new Label(""Hello World! With Style"");
        labelWithStyle.styleSheets.Add(styleSheet);
        root.Add(labelWithStyle);", folder, ussName);
            }

            csTemplate += @"
    }
}";
            return csTemplate;
        }
    }
}
