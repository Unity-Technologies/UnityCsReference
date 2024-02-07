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
            PanelSettings.s_AssignICUData += SetICUDataAsset;

            DropdownUtility.MakeDropdownFunc = CreateGenericOSMenu;

            UIToolkitProjectSettings.onEnableAdvancedTextChanged += SetICUdataAssetOnAllPanelSettings;
        }

        private static GenericOSMenu CreateGenericOSMenu()
        {
            return new GenericOSMenu();
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
