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
        const string k_BuilderCanvas = "Unity.UI.Builder.BuilderCanvas";

        static EditorDelegateRegistration()
        {
            DefaultEventSystem.IsEditorRemoteConnected = () => EditorApplication.isRemoteConnected;

            TextUtilities.getEditorTextSettings = () => EditorTextSettings.defaultTextSettings;

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

            DropdownUtility.MakeDropdownFunc = CreateGenericMenu;
            DropdownUtility.ShowDropdownFunc = ShowGenericMenu;
            PanelSettings.SetPanelSettingsAssetDirty = EditorUtility.SetDirty;
        }

        private static GenericDropdownMenu CreateGenericMenu(bool childrenAllowed)
        {
            return new GenericDropdownMenu(childrenAllowed);
        }

        private static void ShowGenericMenu(GenericDropdownMenu menu, Rect position, VisualElement target, bool parseShortcuts, bool autoClose)
        {
            var genericDropdownMenu = menu as GenericDropdownMenu;

            if (genericDropdownMenu == null || target?.panel.contextType == ContextType.Player)
                menu.DropDown(position, target);
            else
            {
                var contextMenu = genericDropdownMenu.DoDisplayGenericDropdownMenu(position, new DropdownMenuDescriptor()
                {
                    search = DropdownMenuSearch.Auto,
                    parseShortcuts = parseShortcuts,
                    autoClose = autoClose
                });

                if(target != null)
                {
                    contextMenu.rootVisualElement.styleSheets.Clear();
                    InheritStyleSheets(contextMenu.rootVisualElement, target);
                }
            }
        }

        // A hack to inherit stylesheets from parent control. Used in situations where user can set
        // specific skins for UI controls (For example UI Builder with explicitly selected themes).
        static void InheritStyleSheets(VisualElement receiver, VisualElement parent)
        {
            if (receiver == null || parent == null)
                return;

            do
            {
                for (int i = 0; i < parent.styleSheets.count; i++)
                    receiver.styleSheets.Add(parent.styleSheets[i]);

                parent = parent.parent;
            } while (parent != null && parent.GetType().FullName != k_BuilderCanvas);
        }
    }
}
