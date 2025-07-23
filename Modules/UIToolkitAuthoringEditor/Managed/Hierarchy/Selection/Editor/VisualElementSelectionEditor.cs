// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[CustomEditor(typeof(VisualElementSelection))]
class VisualElementSelectionEditor : UnityEditor.Editor
{
    private VisualElementSelection Target => (VisualElementSelection)target;

    protected override void OnHeaderGUI()
    {
        // Intentionally left empty to override the header.
    }

    public override VisualElement CreateInspectorGUI()
    {
        var inspector = new VisualElementInspector
        {
            Element = Target.Element
        };
        inspector.SetBinding(VisualElementInspector.ElementProperty, new DataBinding
        {
            dataSource = Target,
            dataSourcePath = VisualElementSelection.ElementProperty,
            bindingMode = BindingMode.ToTarget
        });
        return inspector;
    }
}
