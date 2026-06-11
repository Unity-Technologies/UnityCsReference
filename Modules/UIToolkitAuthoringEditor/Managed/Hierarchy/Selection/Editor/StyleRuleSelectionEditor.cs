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
    StyleRuleHeader m_Header;

    private StyleRuleSelection Target => (StyleRuleSelection)target;

    void OnEnable() => StageNavigationManager.instance.afterSuccessfullySwitchedToStage += OnStageChanged;

    void OnDisable() => StageNavigationManager.instance.afterSuccessfullySwitchedToStage -= OnStageChanged;

    void OnStageChanged(Stage _) => m_Inspector?.ResetSearch();

    public override bool UseDefaultMargins()
    {
        // We don't want to have an artificial padding
        return false;
    }

    internal override bool isHeaderSticky => true;

    internal override VisualElement CreateInspectorHeaderGUI()
    {
        m_Header = new StyleRuleHeader { name = "Header" };
        m_Header.Rule = Target.StyleRule;
        m_Header.SetBinding(StyleRuleHeader.RuleProperty, new DataBinding
        {
            dataSource = Target,
            dataSourcePath = StyleRuleSelection.StyleRuleProperty,
            bindingMode = BindingMode.ToTarget
        });
        return m_Header;
    }

    public override VisualElement CreateInspectorGUI()
    {
        m_Inspector = new StyleRuleInspector { StyleRule = Target.StyleRule };
        m_Inspector.SetBinding(StyleRuleInspector.StyleRuleProperty, new DataBinding
        {
            dataSource = Target,
            dataSourcePath = StyleRuleSelection.StyleRuleProperty,
            bindingMode = BindingMode.ToTarget
        });
        m_Inspector.InitializeSearchField(m_Header.SearchField);
        return m_Inspector;
    }

    void OnDestroy() => m_Inspector?.Dispose();
}
