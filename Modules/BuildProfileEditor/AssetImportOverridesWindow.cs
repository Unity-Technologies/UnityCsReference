// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: BuildSettingsWindow not yet converted
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Asset Import Overrides Window used for modifing texture compression and max texture
    /// size.
    /// </summary>
    internal class AssetImportOverridesWindow : EditorWindow
    {
        const string k_Uxml = "BuildProfile/UXML/AssetImportOverridesWindow.uxml";

        [NoAutoStaticsCleanup] // static dropdown labels, allocated once, only localized string literals
        static readonly List<string> k_MaxTextureSizeLabels = new()
        {
            L10n.Tr("No Override", null),
            L10n.Tr("Max 2048", null),
            L10n.Tr("Max 1024", null),
            L10n.Tr("Max 512", null),
            L10n.Tr("Max 256", null),
            L10n.Tr("Max 128", null),
            L10n.Tr("Max 64", null),
        };

        [NoAutoStaticsCleanup] // static dropdown labels, allocated once, only localized string literals
        static readonly List<string> k_TextureCompressionLabels = new()
        {
            L10n.Tr("No Override", null),
            L10n.Tr("Force Fast Compressor", null),
            L10n.Tr("Force Uncompressed", null),
            L10n.Tr("Force No Crunch", null)
        };

        [NoAutoStaticsCleanup] // static dropdown values, allocated once, only int literals
        static readonly List<int> k_MaxTextureSizeValues = new()
        {
            0,
            2048,
            1024,
            512,
            256,
            128,
            64,
        };

        [NoAutoStaticsCleanup] // static dropdown values, allocated once, only enum-backed int literals
        static readonly List<int> k_TextureCompressionValues = new()
        {
            (int)OverrideTextureCompression.NoOverride,
            (int)OverrideTextureCompression.ForceFastCompressor,
            (int)OverrideTextureCompression.ForceUncompressed,
            (int)OverrideTextureCompression.ForceNoCrunchCompression
        };

        // We set the initial value to -1 since it's possible to apply the override values
        // before the window is opened when switching profiles. In that case we will need to
        // fetch the initial values from EditorUserBuildSettings
        int m_CurrOverrideMaxTextureSize = -1;
        OverrideTextureCompression m_CurrOverrideTextureCompression;
        Action m_UpdateState;
        Button m_ApplyButton;

        bool HasAssetImportOverrideChanges =>
            m_CurrOverrideMaxTextureSize != EditorUserBuildSettings.overrideMaxTextureSize ||
            m_CurrOverrideTextureCompression != EditorUserBuildSettings.overrideTextureCompression;

        internal static bool IsAssetImportOverrideEnabled => EditorUserBuildSettings.overrideMaxTextureSize != 0 ||
            EditorUserBuildSettings.overrideTextureCompression != OverrideTextureCompression.NoOverride;

        internal void ShowUtilityWindow(Action updateState)
        {
            titleContent.text = TrText.assetImportOverrides;
            var windowSize = new Vector2(332, 209);
            minSize = windowSize;
            maxSize = windowSize;
            m_UpdateState = updateState;
            ShowUtility();
        }

        internal void CreateGUI()
        {
            var windowUxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            rootVisualElement.styleSheets.Add(windowUss);
            windowUxml.CloneTree(rootVisualElement);

            var maxTextureSizeDropdown = rootVisualElement.Q<DropdownField>("max-texture-size-dropdown");
            maxTextureSizeDropdown.label = TrText.maxTextureSizeLabel;
            maxTextureSizeDropdown.choices = k_MaxTextureSizeLabels;
            maxTextureSizeDropdown.RegisterCallback<ChangeEvent<string>>(OnMaxTextureSizeChanged);

            var textureCompression = rootVisualElement.Q<DropdownField>("texture-compression-dropdown");
            textureCompression.label = TrText.textureCompressionLabel;
            textureCompression.choices = k_TextureCompressionLabels;
            textureCompression.RegisterCallback<ChangeEvent<string>>(OnTextureCompressionChanged);

            var titleLabel = rootVisualElement.Q<Label>("asset-override-title-label");
            titleLabel.text = TrText.assetImportOverrideTitle;

            var descriptionLabel = rootVisualElement.Q<Label>("asset-override-description-label");
            descriptionLabel.text = TrText.assetImportOverrideDescription;

            m_ApplyButton = rootVisualElement.Q<Button>("apply-asset-override-button");
            m_ApplyButton.clicked += ApplyAssetImportOverridesAction;

            m_CurrOverrideMaxTextureSize = EditorUserBuildSettings.overrideMaxTextureSize;
            m_CurrOverrideTextureCompression = EditorUserBuildSettings.overrideTextureCompression;

            int maxTextureIndex = k_MaxTextureSizeValues.FindIndex(item => item == m_CurrOverrideMaxTextureSize);
            int texCompression = k_TextureCompressionValues.FindIndex(item => item == (int)m_CurrOverrideTextureCompression);
            maxTextureSizeDropdown.value = k_MaxTextureSizeLabels[maxTextureIndex];
            textureCompression.value = k_TextureCompressionLabels[texCompression];
        }

        void ApplyCurrentAssetImportOverrides()
        {
            if (m_CurrOverrideMaxTextureSize < 0)
            {
                // Fetch initial values
                m_CurrOverrideMaxTextureSize = EditorUserBuildSettings.overrideMaxTextureSize;
                m_CurrOverrideTextureCompression = EditorUserBuildSettings.overrideTextureCompression;
            }
            else
            {
                EditorUserBuildSettings.overrideMaxTextureSize = m_CurrOverrideMaxTextureSize;
                EditorUserBuildSettings.overrideTextureCompression = m_CurrOverrideTextureCompression;

                UpdateApplyOverrideVisibility();
            }
        }

        void OnMaxTextureSizeChanged(ChangeEvent<string> evt)
        {
            int index = k_MaxTextureSizeLabels.FindIndex(item => item == evt.newValue);
            if (index >= 0)
                m_CurrOverrideMaxTextureSize = k_MaxTextureSizeValues[index];

            UpdateApplyOverrideVisibility();
        }

        void OnTextureCompressionChanged(ChangeEvent<string> evt)
        {
            int index = k_TextureCompressionLabels.FindIndex(item => item == evt.newValue);
            if (index >= 0)
                m_CurrOverrideTextureCompression = (OverrideTextureCompression)k_TextureCompressionValues[index];

            UpdateApplyOverrideVisibility();
        }

        void ApplyAssetImportOverridesAction()
        {
            if (HasAssetImportOverrideChanges)
            {
                ApplyCurrentAssetImportOverrides();

                m_UpdateState?.Invoke();

                AssetDatabase.Refresh();
            }
        }

        void UpdateApplyOverrideVisibility()
        {
            m_ApplyButton?.SetEnabled(HasAssetImportOverrideChanges);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
