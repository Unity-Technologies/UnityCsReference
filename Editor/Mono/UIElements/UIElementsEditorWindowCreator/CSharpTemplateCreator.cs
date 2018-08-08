// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

static partial class UIElementsTemplate
{
    public static string CreateCSharpTemplate(string cSharpName, string uxmlName, string ussName, string folder)
    {
        string csTemplate = string.Format(@"using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;


public class {0} : EditorWindow
{{
    [MenuItem(""Window/UIElements/{0}"")]
    public static void ShowExample()
    {{
        {0} wnd = GetWindow<{0}>();
        wnd.titleContent = new GUIContent(""{0}"");
    }}

    public void OnEnable()
    {{
        // Each editor window contains a root VisualElement object
        VisualElement root = this.GetRootVisualContainer();

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement label = new Label(""Hello World! From C#"");
        root.Add(label);", cSharpName);

        if (uxmlName != String.Empty)
        {
            csTemplate = csTemplate + string.Format(@"

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath(""{0}/{1}.uxml"", typeof(VisualTreeAsset)) as VisualTreeAsset;
        VisualElement labelFromUXML = visualTree.CloneTree(null);
        root.Add(labelFromUXML);", folder, uxmlName);
        }

        if (ussName != String.Empty)
        {
            csTemplate += string.Format(@"

        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        VisualElement labelWithStyle = new Label(""Hello World! With Style"");
        labelWithStyle.AddStyleSheetPath(""{0}/{1}.uss"");
        root.Add(labelWithStyle);", folder, ussName);
        }

        csTemplate += @"
    }
}";
        return csTemplate;
    }
}
