// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;
using UnityEditor.Build;
using System.Collections.Generic;
using UnityEditor.Modules;

namespace UnityEditor
{
    internal abstract class BaseTextureImportPlatformSettings
    {
        static class Styles
        {
            static public readonly GUIContent defaultPlatform = EditorGUIUtility.TrTextContent("Default");
            static public readonly GUIContent overrideFor = EditorGUIUtility.TrTextContent("Override For {0}");
        }

        public abstract bool textureTypeHasMultipleDifferentValues { get; }
        public abstract TextureImporterType textureType { get; }
        public abstract SpriteImportMode spriteImportMode { get; }
        public abstract int GetTargetCount();
        public abstract bool ShowPresetSettings();
        public abstract TextureImporterSettings GetImporterSettings(int i);
        public abstract bool IsSourceTextureHDR(int i);
        public abstract TextureImportPlatformSettingsData model { get; }
        public abstract bool DoesSourceTextureHaveAlpha(int i);
        public abstract TextureImporterPlatformSettings GetPlatformTextureSettings(int i, string name);
        public abstract BaseTextureImportPlatformSettings GetDefaultImportSettings();
        public abstract void SetPlatformTextureSettings(int i, TextureImporterPlatformSettings platformSettings);

        public BaseTextureImportPlatformSettings(string name, BuildTarget target)
        {
            model.platformTextureSettings.name = name;
            model.buildTarget = target;
            model.platformTextureSettings.overridden = false;
        }

        protected void Init()
        {
            for (int i = 0; i < GetTargetCount(); i++)
            {
                TextureImporterPlatformSettings curPlatformSettings = GetPlatformTextureSettings(i, model.platformTextureSettings.name);
                if (i == 0)
                {
                    model.platformTextureSettings = curPlatformSettings;
                }
                else
                {
                    if (curPlatformSettings.overridden != model.platformTextureSettings.overridden)
                        model.overriddenIsDifferent = true;
                    if (curPlatformSettings.format != model.platformTextureSettings.format)
                        model.textureFormatIsDifferent = true;
                    if (curPlatformSettings.maxTextureSize != model.platformTextureSettings.maxTextureSize)
                        model.maxTextureSizeIsDifferent = true;
                    if (curPlatformSettings.resizeAlgorithm != model.platformTextureSettings.resizeAlgorithm)
                        model.resizeAlgorithmIsDifferent = true;
                    if (curPlatformSettings.textureCompression != model.platformTextureSettings.textureCompression)
                        model.textureCompressionIsDifferent = true;
                    if (curPlatformSettings.compressionQuality != model.platformTextureSettings.compressionQuality)
                        model.compressionQualityIsDifferent = true;
                    if (curPlatformSettings.crunchedCompression != model.platformTextureSettings.crunchedCompression)
                        model.crunchedCompressionIsDifferent = true;
                    if (curPlatformSettings.allowsAlphaSplitting != model.platformTextureSettings.allowsAlphaSplitting)
                        model.allowsAlphaSplitIsDifferent = true;
                    if (curPlatformSettings.androidETC2FallbackOverride !=
                        model.platformTextureSettings.androidETC2FallbackOverride)
                        model.androidETC2FallbackOverrideIsDifferent = true;
                }
            }

            Sync();
        }

        public void Sync()
        {
            // Use settings from default if any of the targets are not overridden
            if (!model.isDefault && (!model.platformTextureSettings.overridden || model.overriddenIsDifferent))
            {
                BaseTextureImportPlatformSettings defaultSettings = GetDefaultImportSettings();
                model.platformTextureSettings.maxTextureSize = defaultSettings.model.platformTextureSettings.maxTextureSize;
                model.maxTextureSizeIsDifferent = defaultSettings.model.maxTextureSizeIsDifferent;
                model.platformTextureSettings.resizeAlgorithm = defaultSettings.model.platformTextureSettings.resizeAlgorithm;
                model.resizeAlgorithmIsDifferent = defaultSettings.model.resizeAlgorithmIsDifferent;
                model.platformTextureSettings.textureCompression = defaultSettings.model.platformTextureSettings.textureCompression;
                model.textureCompressionIsDifferent = defaultSettings.model.textureCompressionIsDifferent;
                model.platformTextureSettings.format = defaultSettings.model.platformTextureSettings.format;
                model.textureFormatIsDifferent = defaultSettings.model.textureFormatIsDifferent;
                model.platformTextureSettings.compressionQuality = defaultSettings.model.platformTextureSettings.compressionQuality;
                model.compressionQualityIsDifferent = defaultSettings.model.compressionQualityIsDifferent;
                model.platformTextureSettings.crunchedCompression = defaultSettings.model.platformTextureSettings.crunchedCompression;
                model.crunchedCompressionIsDifferent = defaultSettings.model.crunchedCompressionIsDifferent;
                model.platformTextureSettings.allowsAlphaSplitting = defaultSettings.model.platformTextureSettings.allowsAlphaSplitting;
                model.allowsAlphaSplitIsDifferent = defaultSettings.model.allowsAlphaSplitIsDifferent;
                model.androidETC2FallbackOverrideIsDifferent = defaultSettings.model.androidETC2FallbackOverrideIsDifferent;
            }

            if ((model.platformTextureSettings.overridden || model.overriddenIsDifferent) && model.platformTextureSettings.format < 0)
            {
                var showSettingsForPreset = ShowPresetSettings();

                model.platformTextureSettings.format = TextureImporter.DefaultFormatFromTextureParameters(
                    GetImporterSettings(0),
                    model.platformTextureSettings,
                    showSettingsForPreset || DoesSourceTextureHaveAlpha(0),
                    showSettingsForPreset || IsSourceTextureHDR(0),
                    model.buildTarget
                );
                model.textureFormatIsDifferent = false;

                for (int i = 1; i < GetTargetCount(); i++)
                {
                    TextureImporterSettings settings = GetImporterSettings(i);

                    TextureImporterFormat format = TextureImporter.DefaultFormatFromTextureParameters(settings,
                        model.platformTextureSettings,
                        showSettingsForPreset || DoesSourceTextureHaveAlpha(i),
                        showSettingsForPreset || IsSourceTextureHDR(i),
                        model.buildTarget
                    );
                    if (format != model.platformTextureSettings.format)
                        model.textureFormatIsDifferent = true;
                }
            }
        }

        public void Apply()
        {
            for (int i = 0; i < GetTargetCount(); i++)
            {
                TextureImporterPlatformSettings platformSettings = GetPlatformTextureSettings(i, model.platformTextureSettings.name);

                // Overwrite with inspector properties if same for all targets
                if (!model.overriddenIsDifferent)
                    platformSettings.overridden = model.platformTextureSettings.overridden;
                if (!model.textureFormatIsDifferent)
                    platformSettings.format = model.platformTextureSettings.format;
                if (!model.maxTextureSizeIsDifferent)
                    platformSettings.maxTextureSize = model.platformTextureSettings.maxTextureSize;
                if (!model.resizeAlgorithmIsDifferent)
                    platformSettings.resizeAlgorithm = model.platformTextureSettings.resizeAlgorithm;
                if (!model.textureCompressionIsDifferent)
                    platformSettings.textureCompression = model.platformTextureSettings.textureCompression;
                if (!model.compressionQualityIsDifferent)
                    platformSettings.compressionQuality = model.platformTextureSettings.compressionQuality;
                if (!model.crunchedCompressionIsDifferent)
                    platformSettings.crunchedCompression = model.platformTextureSettings.crunchedCompression;
                if (!model.allowsAlphaSplitIsDifferent)
                    platformSettings.allowsAlphaSplitting = model.platformTextureSettings.allowsAlphaSplitting;
                if (!model.androidETC2FallbackOverrideIsDifferent)
                    platformSettings.androidETC2FallbackOverride = model.platformTextureSettings.androidETC2FallbackOverride;

                platformSettings.forceMaximumCompressionQuality_BC6H_BC7 = model.platformTextureSettings.forceMaximumCompressionQuality_BC6H_BC7;

                SetPlatformTextureSettings(i, platformSettings);
            }

            model.SetChanged(false);
        }

        public static BuildPlatform[] GetBuildPlayerValidPlatforms()
        {
            List<BuildPlatform> validPlatforms = BuildPlatforms.instance.GetValidPlatforms();
            return validPlatforms.ToArray();
        }

        internal static void ShowPlatformSpecificSettings(List<BaseTextureImportPlatformSettings> platformSettings)
        {
            BuildPlatform[] validPlatforms = GetBuildPlayerValidPlatforms();
            GUILayout.Space(10);
            int shownTextureFormatPage = EditorGUILayout.BeginPlatformGrouping(validPlatforms, Styles.defaultPlatform);
            BaseTextureImportPlatformSettings realPS = platformSettings[shownTextureFormatPage + 1];

            if (!realPS.model.isDefault)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = realPS.model.overriddenIsDifferent;

                string title = string.Format(Styles.overrideFor.text, validPlatforms[shownTextureFormatPage].title.text);
                bool newOverride = EditorGUILayout.ToggleLeft(title, realPS.model.platformTextureSettings.overridden);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    realPS.model.SetOverriddenForAll(newOverride);
                    SyncPlatformSettings(platformSettings);
                }
            }

            // Disable size and format GUI if not overwritten for all objects
            bool notAllOverriddenForThisPlatform = (!realPS.model.isDefault && !realPS.model.allAreOverridden);
            using (new EditorGUI.DisabledScope(notAllOverriddenForThisPlatform))
            {
                // acquire the platform support module for this platform, and present the appropriate UI
                ITextureImportSettingsExtension textureSettingsExtension = ModuleManager.GetTextureImportSettingsExtension(realPS.model.buildTarget);
                textureSettingsExtension.ShowImportSettings(realPS);

                //just do this once, regardless of whether things changed
                SyncPlatformSettings(platformSettings);
            }

            EditorGUILayout.EndPlatformGrouping();
        }

        internal static void SyncPlatformSettings(List<BaseTextureImportPlatformSettings> platformSettings)
        {
            foreach (BaseTextureImportPlatformSettings ps in platformSettings)
                ps.Sync();
        }

        internal static void ApplyPlatformSettings(List<BaseTextureImportPlatformSettings> platformSettings)
        {
            SyncPlatformSettings(platformSettings);
            foreach (BaseTextureImportPlatformSettings ps in platformSettings)
                ps.Apply();
        }
    }

    [System.Serializable]
    internal class TextureImportPlatformSettingsData
    {
        [SerializeField]
        private TextureImporterPlatformSettings m_PlatformSettings = new TextureImporterPlatformSettings();

        public TextureImporterPlatformSettings platformTextureSettings
        {
            get { return m_PlatformSettings; }
            set { m_PlatformSettings = value; }
        }

        [SerializeField] private bool m_OverriddenIsDifferent = false;
        public bool overriddenIsDifferent
        {
            get { return m_OverriddenIsDifferent; }
            set { m_OverriddenIsDifferent = value; }
        }

        public bool allAreOverridden
        {
            get { return isDefault || (platformTextureSettings.overridden && !overriddenIsDifferent); }
        }
        public bool isDefault
        {
            get { return platformTextureSettings.name == TextureImporterInspector.s_DefaultPlatformName; }
        }

        // Maximum texture size
        [SerializeField] private bool m_MaxTextureSizeIsDifferent = false;
        public bool maxTextureSizeIsDifferent
        {
            get { return m_MaxTextureSizeIsDifferent; }
            set { m_MaxTextureSizeIsDifferent = value; }
        }

        // Resize Algorithm
        [SerializeField] private bool m_ResizeAlgorithmIsDifferent = false;
        public bool resizeAlgorithmIsDifferent
        {
            get { return m_ResizeAlgorithmIsDifferent; }
            set { m_ResizeAlgorithmIsDifferent = value; }
        }

        // Texture compression
        [SerializeField] private bool m_TextureCompressionIsDifferent = false;
        public bool textureCompressionIsDifferent
        {
            get { return m_TextureCompressionIsDifferent; }
            set { m_TextureCompressionIsDifferent = value; }
        }


        // Compression rate
        [SerializeField] private bool m_CompressionQualityIsDifferent = false;
        public bool compressionQualityIsDifferent
        {
            get { return m_CompressionQualityIsDifferent; }
            set { m_CompressionQualityIsDifferent = value; }
        }

        // Crunched compression
        [SerializeField] private bool m_CrunchedCompressionIsDifferent = false;
        public bool crunchedCompressionIsDifferent
        {
            get { return m_CrunchedCompressionIsDifferent; }
            set { m_CrunchedCompressionIsDifferent = value; }
        }

        // Texture format
        [SerializeField] private bool m_TextureFormatIsDifferent = false;
        public bool textureFormatIsDifferent
        {
            get { return m_TextureFormatIsDifferent; }
            set { m_TextureFormatIsDifferent = value; }
        }


        // Alpha splitting
        [SerializeField] private bool m_AlphaSplitIsDifferent = false;
        public bool allowsAlphaSplitIsDifferent
        {
            get { return m_AlphaSplitIsDifferent; }
            set { m_AlphaSplitIsDifferent = value; }
        }

        // Android fallback format in case ETC2 is not supported
        [SerializeField] private bool m_AndroidETC2FallbackOverrideIsDifferent = false;

        public bool androidETC2FallbackOverrideIsDifferent
        {
            get { return m_AndroidETC2FallbackOverrideIsDifferent; }
            set { m_AndroidETC2FallbackOverrideIsDifferent = value; }
        }

        public void SetAndroidETC2FallbackOverrideForAll(AndroidETC2FallbackOverride value)
        {
            Debug.Assert(allAreOverridden,
                "Attempting to set android ETC2 fallback format for all platforms even though settings are not overridden for all platforms.");
            m_PlatformSettings.androidETC2FallbackOverride = value;
            m_AndroidETC2FallbackOverrideIsDifferent = false;
            SetChanged();
        }

        [SerializeField] public BuildTarget m_Target;
        [SerializeField] bool m_HasChanged = false;


        public void SetChanged(bool value = true)
        {
            m_HasChanged = value;
        }

        public bool HasChanged()
        {
            return m_HasChanged;
        }

        public BuildTarget buildTarget
        {
            get { return m_Target; }
            set { m_Target = value; }
        }

        public void SetOverriddenForAll(bool overridden)
        {
            platformTextureSettings.overridden = overridden;
            overriddenIsDifferent = false;
            SetChanged();
        }

        public void SetMaxTextureSizeForAll(int maxTextureSize)
        {
            Debug.Assert(allAreOverridden,
                "Attempting to set max texture size for all platforms even though settings are not overridden for all platforms.");
            platformTextureSettings.maxTextureSize = maxTextureSize;
            maxTextureSizeIsDifferent = false;
            SetChanged();
        }

        public void SetResizeAlgorithmForAll(TextureResizeAlgorithm algorithm)
        {
            Debug.Assert(allAreOverridden,
                "Attempting to set resize algorithm for all platforms even though settings are not overridden for all platforms.");
            platformTextureSettings.resizeAlgorithm = algorithm;
            resizeAlgorithmIsDifferent = false;
            SetChanged();
        }

        public void SetTextureCompressionForAll(TextureImporterCompression textureCompression)
        {
            Debug.Assert(allAreOverridden,
                "Attempting to set texture compression for all platforms even though settings are not overridden for all platforms.");
            platformTextureSettings.textureCompression = textureCompression;
            textureCompressionIsDifferent = false;
            SetChanged();
        }

        public void SetCompressionQualityForAll(int quality)
        {
            Debug.Assert(allAreOverridden,
                "Attempting to set texture compression quality for all platforms even though settings are not overridden for all platforms.");
            platformTextureSettings.compressionQuality = quality;
            compressionQualityIsDifferent = false;
            platformTextureSettings.forceMaximumCompressionQuality_BC6H_BC7 = 0;
            SetChanged();
        }

        internal bool forceMaximumCompressionQuality_BC6H_BC7
        {
            get { return platformTextureSettings.forceMaximumCompressionQuality_BC6H_BC7 != 0; }
            set { platformTextureSettings.forceMaximumCompressionQuality_BC6H_BC7 = value ? 1 : 0; }
        }

        public void SetCrunchedCompressionForAll(bool crunched)
        {
            Debug.Assert(allAreOverridden,
                "Attempting to set texture crunched compression for all platforms even though settings are not overridden for all platforms.");
            platformTextureSettings.crunchedCompression = crunched;
            crunchedCompressionIsDifferent = false;
            SetChanged();
        }

        public void SetTextureFormatForAll(TextureImporterFormat format)
        {
            Debug.Assert(allAreOverridden,
                "Attempting to set texture format for all platforms even though settings are not overridden for all platforms.");
            platformTextureSettings.format = format;
            textureFormatIsDifferent = false;
            SetChanged();
        }

        public void GetValidTextureFormatsAndStrings(TextureImporterType textureType, out int[] formatValues, out string[] formatStrings)
        {
            if (isDefault)
                TextureImportValidFormats.GetDefaultTextureFormatValuesAndStrings(textureType, out formatValues,
                    out formatStrings);
            else
                TextureImportValidFormats.GetPlatformTextureFormatValuesAndStrings(textureType, buildTarget, out formatValues,
                    out formatStrings);
        }

        public void SetAllowsAlphaSplitForAll(bool value)
        {
            Debug.Assert(allAreOverridden,
                "Attempting to set alpha splitting for all platforms even though settings are not overridden for all platforms.");
            platformTextureSettings.allowsAlphaSplitting = value;
            allowsAlphaSplitIsDifferent = false;
            SetChanged();
        }
    }

    [System.Serializable]
    internal class TextureImportPlatformSettings : BaseTextureImportPlatformSettings
    {
        [SerializeField] TextureImporter[] m_Importers;
        public TextureImporter[] importers
        {
            get { return m_Importers; }
        }

        [SerializeField] TextureImporterInspector m_Inspector;

        [SerializeField] TextureImportPlatformSettingsData m_Data = new TextureImportPlatformSettingsData();
        public override TextureImportPlatformSettingsData model
        {
            get
            {
                return m_Data;
            }
        }

        public override bool textureTypeHasMultipleDifferentValues
        {
            get
            {
                return m_Inspector.textureTypeHasMultipleDifferentValues;
            }
        }

        public override TextureImporterType textureType
        {
            get
            {
                return m_Inspector.textureType;
            }
        }

        public override SpriteImportMode spriteImportMode
        {
            get
            {
                return m_Inspector.spriteImportMode;
            }
        }

        public TextureImportPlatformSettings(string name, BuildTarget target, TextureImporterInspector inspector) : base(name, target)
        {
            m_Inspector = inspector;
            m_Importers = inspector.targets.Select(x => x as TextureImporter).ToArray();
            Init();
        }

        public override int GetTargetCount()
        {
            return m_Importers.Length;
        }

        public override bool ShowPresetSettings()
        {
            return m_Inspector.assetTarget == null;
        }

        public override TextureImporterSettings GetImporterSettings(int i)
        {
            TextureImporterSettings settings = new TextureImporterSettings();
            // Get import settings for this importer
            m_Importers[i].ReadTextureSettings(settings);
            // Get settings that have been changed in the inspector
            m_Inspector.GetSerializedPropertySettings(settings);
            return settings;
        }

        public override bool IsSourceTextureHDR(int i)
        {
            return m_Importers[i].IsSourceTextureHDR();
        }

        public override bool DoesSourceTextureHaveAlpha(int i)
        {
            return m_Importers[i].DoesSourceTextureHaveAlpha();
        }

        public override TextureImporterPlatformSettings GetPlatformTextureSettings(int i, string name)
        {
            return m_Importers[i].GetPlatformTextureSettings(name);
        }

        public override BaseTextureImportPlatformSettings GetDefaultImportSettings()
        {
            return m_Inspector.m_PlatformSettings[0];
        }

        public override void SetPlatformTextureSettings(int i, TextureImporterPlatformSettings platformSettings)
        {
            m_Importers[i].SetPlatformTextureSettings(platformSettings);
        }

        public static readonly int[] kAndroidETC2FallbackOverrideValues =
        {
            (int)AndroidETC2FallbackOverride.UseBuildSettings,
            (int)AndroidETC2FallbackOverride.Quality32Bit,
            (int)AndroidETC2FallbackOverride.Quality16Bit,
            (int)AndroidETC2FallbackOverride.Quality32BitDownscaled,
        };
    }
}
