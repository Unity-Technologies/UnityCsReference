// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[CustomEditor(typeof(VisualTreeAssetSelection))]
internal class VisualTreeAssetSelectionEditor : UnityEditor.Editor
{
    private VisualTreeAssetSelection Target => (VisualTreeAssetSelection)target;

    protected override void OnHeaderGUI()
    {
        // Intentionally left empty to override the header.
    }

    public override VisualElement CreateInspectorGUI()
    {
        var inspector = new VisualTreeAssetInspector
        {
            VisualTreeAsset = Target.panelComponent.visualTreeAsset
        };

        var binding = new DataBinding
        {
            dataSource = Target,
            dataSourcePath = VisualTreeAssetSelection.PanelComponentProperty,
            updateTrigger = BindingUpdateTrigger.EveryUpdate,
            bindingMode = BindingMode.ToTarget
        };

        binding.sourceToUiConverters.AddConverter((ref UIDocument document) => document.visualTreeAsset);
        binding.sourceToUiConverters.AddConverter((ref PanelRenderer renderer) => renderer.visualTreeAsset);
        binding.sourceToUiConverters.AddConverter((ref IPanelComponent panelComponent) => panelComponent.visualTreeAsset);

        inspector.SetBinding(VisualTreeAssetInspector.VisualTreeAssetProperty, binding);
        return inspector;
    }
}
