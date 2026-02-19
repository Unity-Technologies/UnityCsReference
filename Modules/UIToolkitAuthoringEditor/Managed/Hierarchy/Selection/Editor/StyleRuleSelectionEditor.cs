// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[CustomEditor(typeof(StyleRuleSelection))]
class StyleRuleSelectionEditor : UnityEditor.Editor
{
    private StyleRuleSelection Target => (StyleRuleSelection)target;

    protected override void OnHeaderGUI()
    {
        // Intentionally left empty to override the header.
    }

    public override bool UseDefaultMargins()
    {
        // We don't want to have an artificial padding
        return false;
    }

    public override VisualElement CreateInspectorGUI()
    {
        var inspector = new StyleRuleInspector() { StyleRule = Target.StyleRule };
        inspector.SetBinding(StyleRuleInspector.StyleRuleProperty, new DataBinding
        {
            dataSource = Target,
            dataSourcePath = StyleRuleSelection.StyleRuleProperty,
            bindingMode = BindingMode.ToTarget
        });
        return inspector;
    }
}
