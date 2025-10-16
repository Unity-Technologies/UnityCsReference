// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Toolbars
{
    static class QualityDropdown
    {
        const string k_Path = "Editor Utility/Quality";

        static QualityDropdown()
        {
            ModeService.modeChanged += (args) => RebuildContent();
            QualitySettings.activeQualityLevelChanged += (prev, current) => RebuildContent();
            QualitySettings.activeQualityLevelRenamed += (prev, current) => RebuildContent();
        }

        static void RebuildContent()
        {
            MainToolbar.Refresh(k_Path);
        }

        [MainToolbarElement(k_Path, defaultDockIndex = 1, defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarElement CreateButton()
        {
            string currentQualityName = QualitySettings.names[QualitySettings.GetQualityLevel()];
            Texture2D icon = EditorGUIUtility.LoadIcon("BuildSettings.DedicatedServer.Small");

            return new MainToolbarDropdown(new MainToolbarContent(
                    currentQualityName,
                    icon,
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
