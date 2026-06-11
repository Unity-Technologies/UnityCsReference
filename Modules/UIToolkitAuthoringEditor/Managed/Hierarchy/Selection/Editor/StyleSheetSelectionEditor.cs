// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[CustomEditor(typeof(StyleSheetSelection))]
class StyleSheetSelectionEditor : UnityEditor.Editor
{
    StyleSheetHeader m_Header;

    private StyleSheetSelection Target => (StyleSheetSelection)target;

    public override bool UseDefaultMargins()
    {
        // We don't want to have an artificial padding
        return false;
    }

    internal override bool isHeaderSticky => true;

    internal override VisualElement CreateInspectorHeaderGUI()
    {
        m_Header = new StyleSheetHeader { name = "Header" };
        m_Header.StyleSheet = Target.StyleSheet;
        m_Header.SetBinding(StyleSheetHeader.StyleSheetProperty, new DataBinding
        {
            dataSource = Target,
            dataSourcePath = StyleSheetSelection.StyleSheetProperty,
            bindingMode = BindingMode.ToTarget
        });
        return m_Header;
    }

    public override VisualElement CreateInspectorGUI()
    {
        var inspector = new StyleSheetInspector() { StyleSheet = Target.StyleSheet };
        inspector.SetBinding(StyleSheetInspector.StyleSheetProperty, new DataBinding
        {
            dataSource = Target,
            dataSourcePath = StyleSheetSelection.StyleSheetProperty,
            bindingMode = BindingMode.ToTarget
        });
        return inspector;
    }
}
