// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [InitializeOnLoad]
    internal static class EditorDelegateRegistration
    {
        static EditorDelegateRegistration()
        {
            DefaultEventSystem.IsEditorRemoteConnected = () => EditorApplication.isRemoteConnected;
            PanelTextSettings.EditorGUIUtilityLoad = path => EditorGUIUtility.Load(path);
            PanelTextSettings.GetCurrentLanguage = () => LocalizationDatabase.currentEditorLanguage;

            UIDocument.IsEditorPlaying = () => EditorApplication.isPlaying;
            UIDocument.IsEditorPlayingOrWillChangePlaymode = () => EditorApplication.isPlayingOrWillChangePlaymode;

            PanelSettings.CreateRuntimePanelDebug = UIElementsEditorRuntimeUtility.CreateRuntimePanelDebug;
            PanelSettings.GetOrCreateDefaultTheme = PanelSettingsCreator.GetFirstThemeOrCreateDefaultTheme;
            PanelSettings.GetGameViewResolution = (int display) =>
                {
                    foreach (var playModeView in PlayModeView.GetAllPlayModeViewWindows())
                    {
                        if (playModeView.targetDisplay == display)
                            return playModeView.targetSize;
                    }
                    return new(Display.main.renderingWidth, Display.main.renderingHeight);
                };

            DropdownUtility.MakeDropdownFunc = CreateGenericOSMenu;
            PanelSettings.SetPanelSettingsAssetDirty = EditorUtility.SetDirty;
        }

        private static GenericOSMenu CreateGenericOSMenu()
        {
            return new GenericOSMenu();
        }
    }
}
