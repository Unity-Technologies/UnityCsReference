// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("SceneView/Layers", typeof(SceneView))]
    sealed class LayersDropdown : EditorToolbarDropdown
    {
        public LayersDropdown()
        {
            name = "LayersDropdown";
            tooltip = L10n.Tr("Which layers are visible in the Scene views");
            icon = EditorGUIUtility.LoadIconRequired("Icons/Overlays/SceneLayersToggle.png");

            clicked += () => LayerVisibilityWindow.ShowAtPosition(worldBound);

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            EditorApplication.delayCall += CheckAvailability; //Immediately after a domain reload, calling check availability sometimes returns the wrong value
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            ModeService.modeChanged += OnModeChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ModeService.modeChanged -= OnModeChanged;
        }

        void OnModeChanged(ModeService.ModeChangedArgs args)
        {
            CheckAvailability();
        }

        void CheckAvailability()
        {
            style.display = ModeService.HasCapability(ModeCapability.Layers, true) ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
