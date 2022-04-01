// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Editor Utility/Modes", typeof(DefaultMainToolbar))]
    sealed class ModesDropdown : EditorToolbarDropdown
    {
        public ModesDropdown()
        {
            name = "ModesDropdown";

            clicked += OpenModesDropdown;

            style.display = DisplayStyle.None;

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

        void CheckAvailability()
        {
            style.display = Unsupported.IsDeveloperBuild() && ModeService.hasSwitchableModes
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            UpdateContent();
        }

        void OnModeChanged(ModeService.ModeChangedArgs args)
        {
            CheckAvailability();
            UpdateContent();
        }

        void UpdateContent()
        {
            text = ModeService.modeNames[ModeService.currentIndex];
        }

        void OpenModesDropdown()
        {
            GenericMenu menu = new GenericMenu();
            var modes = ModeService.modeNames;
            for (var i = 0; i < modes.Length; i++)
            {
                var modeName = ModeService.modeNames[i];
                int selected = i;
                menu.AddItem(
                    new GUIContent(modeName),
                    ModeService.currentIndex == i,
                    () =>
                    {
                        EditorApplication.delayCall += () => ModeService.ChangeModeByIndex(selected);
                    });
            }
            menu.DropDown(worldBound, true);
        }
    }
}
