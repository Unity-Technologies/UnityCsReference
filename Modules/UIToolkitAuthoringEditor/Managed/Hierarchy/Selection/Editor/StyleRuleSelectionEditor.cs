// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[CustomEditor(typeof(StyleRuleSelection))]
class StyleRuleSelectionEditor : UnityEditor.Editor
{
    StyleRuleInspector m_Inspector;

    private StyleRuleSelection Target => (StyleRuleSelection)target;

    void OnEnable() => StageNavigationManager.instance.afterSuccessfullySwitchedToStage += OnStageChanged;

    void OnDisable() => StageNavigationManager.instance.afterSuccessfullySwitchedToStage -= OnStageChanged;

    void OnStageChanged(Stage _) => m_Inspector?.ResetSearch();

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
        m_Inspector = new StyleRuleInspector() { StyleRule = Target.StyleRule };
        m_Inspector.SetBinding(StyleRuleInspector.StyleRuleProperty, new DataBinding
        {
            dataSource = Target,
            dataSourcePath = StyleRuleSelection.StyleRuleProperty,
            bindingMode = BindingMode.ToTarget
        });
        return m_Inspector;
    }

    void OnDestroy() => m_Inspector?.Dispose();
}
