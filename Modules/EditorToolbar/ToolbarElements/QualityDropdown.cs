// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Toolbars
{
    static class QualityDropdown
    {
        const string k_Path = "Editor Utility/Quality";
        static bool s_Displayed = false;
        static int s_TemporaryNewQualityLevel = -1;

        static QualityDropdown()
        {
            ModeService.modeChanged += (args) => RebuildContent();
            QualitySettings.activeQualityLevelIndexChanged += (prev, current, currentInOld) => RebuildContent(currentInOld);
            QualitySettings.activeQualityLevelRenamed += (prev, current) => RebuildContent();
            Undo.undoRedoEvent += OnUndoRedo;
        }

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
                    L10n.Tr("Select current quality level")),
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

            menu.AddItem(new GUIContent(L10n.Tr("Open Quality Settings...")), false, OpenQualitySettings);

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
