// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.TextCore.Text;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using TextUtilities = UnityEngine.UIElements.TextUtilities;

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
            TextUtilities.IsAdvancedTextEnabled = () => UIToolkitProjectSettings.enableAdvancedText;
            FontAssetEditor.IsAdvancedTextEnabled = () => UIToolkitProjectSettings.enableAdvancedText;

            UIDocument.IsEditorPlaying = () => EditorApplication.isPlaying;
            UIDocument.IsEditorPlayingOrWillChangePlaymode = () => EditorApplication.isPlayingOrWillChangePlaymode;

            PanelSettings.CreateRuntimePanelDebug = UIElementsEditorRuntimeUtility.CreateRuntimePanelDebug;
            PanelSettings.GetOrCreateDefaultTheme = PanelSettingsCreator.GetOrCreateDefaultTheme;
            PanelSettings.GetGameViewResolution = (int display) =>
                {
                    foreach (var playModeView in PlayModeView.GetAllPlayModeViewWindows())
                    {
                        if (playModeView.targetDisplay == display)
                            return playModeView.targetSize;
                    }
                    return new(Display.main.renderingWidth, Display.main.renderingHeight);
                };
            PanelSettings.SetPanelSettingsAssetDirty = EditorUtility.SetDirty;
            PanelSettings.IsAdvancedTextEnabled = () => UIToolkitProjectSettings.enableAdvancedText;
            PanelSettings.s_OnValidateCallback += SetICUDataAsset;

            DropdownUtility.MakeDropdownFunc = CreateGenericMenu;
            DropdownUtility.ShowDropdownFunc = ShowGenericMenu;

            UIToolkitProjectSettings.onEnableAdvancedTextChanged += SetICUdataAssetOnAllPanelSettings;

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

        private static void SetICUdataAssetOnAllPanelSettings(bool _)
        {
            try
            {
                foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(PanelSettings).FullName))
                {
                    SetICUDataAsset( AssetDatabase.LoadMainAssetAtGUID(new GUID(guid))  as PanelSettings);
                }
            }
            finally
            {
                AssetDatabase.SaveAssets();
            }
        }


        private static void SetICUDataAsset(PanelSettings target)
        {
            Debug.Assert(target != null, "target PanelSetting is null");
            if (UIToolkitProjectSettings.enableAdvancedText)
            {
                var asset = ICUDataAssetUtilities.GetICUAsset();
                if (asset == null)
                {
                    ICUDataAssetUtilities.CreateAsset();
                    asset = ICUDataAssetUtilities.GetICUAsset();
                }

                if (asset != null && target.m_ICUDataAsset != asset)
                {
                    target.m_ICUDataAsset = asset;
                    target.MarkDirty();
                }
            }
            else if( target.m_ICUDataAsset != null)
            {
                target.m_ICUDataAsset = null;
                target.MarkDirty();
            }
        }
    }
}
