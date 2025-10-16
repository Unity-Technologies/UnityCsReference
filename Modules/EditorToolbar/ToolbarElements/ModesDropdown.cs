// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Overlays;
using UnityEngine;

namespace UnityEditor.Toolbars
{
    static class ModesDropdown
    {
        const string k_Path = "Editor Controls/Modes";

        static ModesDropdown()
        {
            EditorApplication.delayCall += RebuildContent; //Immediately after a domain reload, calling check availability sometimes returns the wrong value
            ModeService.modeChanged += (args) => RebuildContent();
        }

        static void RebuildContent()
        {
            MainToolbar.Refresh(k_Path);
        }

        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement(k_Path, defaultDockIndex = 2, defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarElement CreateButton()
        {
            return new MainToolbarDropdown(new MainToolbarContent(ModeService.modeNames[ModeService.currentIndex], L10n.Tr("Select which layers display in the Scene view.")), (buttonRect) => OpenModesDropdown(buttonRect))
            {
                displayed = Unsupported.IsDeveloperBuild() && ModeService.hasSwitchableModes
            };
        }

        static void OpenModesDropdown(Rect buttonRect)
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
            menu.DropDown(buttonRect, true);
        }
    }
}
