// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[CustomEditor(typeof(VisualElementSelection))]
class VisualElementSelectionEditor : UnityEditor.Editor
{
    VisualElementInspector m_Inspector;

    private VisualElementSelection Target => (VisualElementSelection)target;

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
        m_Inspector = new VisualElementInspector
        {
            Element = Target.Element,
            EditFlags = Target.EditFlags
        };
        m_Inspector.SetBinding(VisualElementInspector.ElementProperty, new DataBinding
        {
            dataSource = Target,
            dataSourcePath = VisualElementSelection.ElementProperty,
            bindingMode = BindingMode.ToTarget
        });
        return m_Inspector;
    }

    void OnDestroy() => m_Inspector?.Dispose();
}
