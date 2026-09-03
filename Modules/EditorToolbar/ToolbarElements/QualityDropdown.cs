// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: SceneTooling not yet converted
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Toolbars
{
    static partial class QualityDropdown
    {
        const string k_Path = "Editor Utility/Quality";
        [NoAutoStaticsCleanup] // Transient UI-displayed flag, re-set every time the toolbar element is rebuilt; safe to persist.
        static bool s_Displayed = false;
        [NoAutoStaticsCleanup] // Scratch quality-level index, consumed and reset to -1 on each rebuild; safe to persist.
        static int s_TemporaryNewQualityLevel = -1;

        [OnCodeLoaded]
        static void Initialize()
        {
            ModeService.modeChanged += OnModeChanged;
            QualitySettings.activeQualityLevelIndexChanged += OnActiveQualityLevelIndexChanged;
            QualitySettings.activeQualityLevelRenamed += OnActiveQualityLevelRenamed;
            Undo.undoRedoEvent += OnUndoRedo;
        }

        [OnCodeUnloading]
        static void Shutdown()
        {
            ModeService.modeChanged -= OnModeChanged;
            QualitySettings.activeQualityLevelIndexChanged -= OnActiveQualityLevelIndexChanged;
            QualitySettings.activeQualityLevelRenamed -= OnActiveQualityLevelRenamed;
            Undo.undoRedoEvent -= OnUndoRedo;
        }

        static void OnModeChanged(ModeService.ModeChangedArgs args) => RebuildContent();

        static void OnActiveQualityLevelIndexChanged(int prev, int current, int currentInOld) => RebuildContent(currentInOld);

        static void OnActiveQualityLevelRenamed(string prev, string current) => RebuildContent();

        static void RebuildContent(int newQualityLevel = -1)
        {
            s_Displayed = false;
            s_TemporaryNewQualityLevel = newQualityLevel;
            MainToolbar.Refresh(k_Path);
        }

        static void OnUndoRedo(in UndoRedoInfo info)
        {
            if (s_Displayed)
                RebuildContent();
        }

        [MainToolbarElement(k_Path, defaultDockIndex = 1, defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarElement CreateButton()
        {
            s_Displayed = true;
            int qualityLevelIndex = s_TemporaryNewQualityLevel == -1 ? QualitySettings.GetQualityLevel() : s_TemporaryNewQualityLevel;
            s_TemporaryNewQualityLevel = -1;
            string currentQualityName = QualitySettings.names[qualityLevelIndex];

            return new MainToolbarDropdown(new MainToolbarContent(
                    currentQualityName,
                    L10n.Tr("Select current quality level", null)),
                     (buttonRect) => {
                    OpenQualityWindow(buttonRect);
                })
            {
                displayed = ModeService.HasCapability(ModeCapability.LayoutWindowMenu, true)
            };
        }

        static void OpenQualityWindow(Rect buttonRect)
        {
            var menu = new GenericMenu();

            string currentQualityName = QualitySettings.names[QualitySettings.GetQualityLevel()];

            QualitySettings.ForEach((index, name) =>
            {
                bool isSelected = name == currentQualityName;
                menu.AddItem(
                    new GUIContent(name),
                    isSelected,
                    SetQualityLevel,
                    index
                );
            });

            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent(L10n.Tr("Open Quality Settings...", null)), false, OpenQualitySettings);

            menu.DropDown(buttonRect);
        }

        static void SetQualityLevel(object userData)
        {
            int qualityIndex = (int)userData;
            QualitySettings.SetQualityLevel(qualityIndex, true);
        }

        static void OpenQualitySettings()
        {
            SettingsService.OpenProjectSettings("Project/Quality");
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
