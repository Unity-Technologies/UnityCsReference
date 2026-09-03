// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: SceneTooling not yet converted
using UnityEditor.Overlays;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Toolbars
{
    static partial class ModesDropdown
    {
        const string k_Path = "Editor Controls/Modes";

        [OnCodeLoaded]
        static void Initialize()
        {
            EditorApplication.delayCall += RebuildContent; //Immediately after a domain reload, calling check availability sometimes returns the wrong value
            ModeService.modeChanged += OnModeChanged;
        }

        [OnCodeUnloading]
        static void Shutdown()
        {
            ModeService.modeChanged -= OnModeChanged;
        }

        static void OnModeChanged(ModeService.ModeChangedArgs args) => RebuildContent();

        static void RebuildContent()
        {
            MainToolbar.Refresh(k_Path);
        }
        
        [MainToolbarElement(k_Path, defaultDockIndex = 2, defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarElement CreateButton()
        {
            return new MainToolbarDropdown(
                new MainToolbarContent(ModeService.modeNames[ModeService.currentIndex],
                    L10n.Tr("Select which layers display in the Scene view.", null)),
                (buttonRect) => OpenModesDropdown(buttonRect));
        }
        
        [MainToolbarElementAvailability(k_Path)]
        static bool IsAvailable()
        {
            return (Unsupported.IsDeveloperBuild() && Unsupported.IsDeveloperMode()) && ModeService.hasSwitchableModes;
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
