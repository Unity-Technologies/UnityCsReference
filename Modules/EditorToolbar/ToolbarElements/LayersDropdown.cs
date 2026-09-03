// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: SceneTooling not yet converted
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("SceneView/Layers", typeof(SceneView))]
    sealed class LayersDropdown : EditorToolbarDropdown
    {
        public LayersDropdown()
        {
            name = "LayersDropdown";
            tooltip = L10n.Tr("Select which layers display in the Scene view.", null);
            icon = EditorGUIUtility.LoadIconRequired("Icons/Overlays/SceneLayersToggle.png");

            #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
            clicked += () => LayerVisibilityWindow.ShowAtPosition(worldBound);
            #pragma warning restore UAL0015

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
            EditorApplication.delayCall += CheckAvailability; //Immediately after a domain reload, calling check availability sometimes returns the wrong value
            #pragma warning restore UAL0015
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
