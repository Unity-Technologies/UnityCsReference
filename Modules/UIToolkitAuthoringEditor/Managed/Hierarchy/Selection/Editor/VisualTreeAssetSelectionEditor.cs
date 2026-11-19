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

    public override bool UseDefaultMargins() => false;

    public override VisualElement CreateInspectorGUI()
    {
        var inspector = new VisualTreeAssetInspector
        {
            VisualTreeAsset = VisualElementSceneViewOverlay.IsAlive(Target.PanelComponent) ? Target.PanelComponent.visualTreeAsset : null,
            PanelSettings = Target.PanelSettings,
            PanelComponent = Target.PanelComponent,
        };

        var binding = new DataBinding
        {
            dataSource = Target,
            dataSourcePath = VisualTreeAssetSelection.PanelComponentProperty,
            updateTrigger = BindingUpdateTrigger.EveryUpdate,
            bindingMode = BindingMode.ToTarget
        };

        binding.sourceToUiConverters.AddConverter((ref UIDocument document) => document != null ? document.visualTreeAsset : null);
        binding.sourceToUiConverters.AddConverter((ref PanelRenderer renderer) => renderer != null ? renderer.visualTreeAsset : null);
        binding.sourceToUiConverters.AddConverter((ref IPanelComponent panelComponent) => VisualElementSceneViewOverlay.IsAlive(panelComponent) ? panelComponent.visualTreeAsset : null);

        inspector.SetBinding(VisualTreeAssetInspector.VisualTreeAssetProperty, binding);

        var panelSettingsBinding = new DataBinding
        {
            dataSource = Target,
            dataSourcePath = VisualTreeAssetSelection.PanelSettingsProperty,
            bindingMode = BindingMode.ToTarget
        };

        inspector.SetBinding(VisualTreeAssetInspector.PanelSettingsProperty, panelSettingsBinding);
        return inspector;
    }

    // Make Edit > Frame Selected / F frame the document's host GameObject in the SceneView (the uxml
    // root node has no VisualElement of its own to measure).
    public bool HasFrameBounds() => VisualElementSceneViewOverlay.IsAlive(Target.PanelComponent);

    public UnityEngine.Bounds OnGetFrameBounds()
    {
        if (!VisualElementSceneViewOverlay.IsAlive(Target.PanelComponent))
            return default;
        return VisualElementSceneViewOverlay.FloorBounds(VisualElementSceneViewOverlay.GameObjectWorldBounds(Target.PanelComponent.gameObject));
    }
}
