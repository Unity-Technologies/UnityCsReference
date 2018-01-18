// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEditor.Modules;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

using Object = UnityEngine.Object;

namespace UnityEditor
{
    [System.Serializable]
    internal class TextureImportPlatformSettings
    {
        [SerializeField] private TextureImporterPlatformSettings m_PlatformSettings = new TextureImporterPlatformSettings();
        public TextureImporterPlatformSettings platformTextureSettings { get { return m_PlatformSettings; } }

        public string name { get { return m_PlatformSettings.name; } }

        // Is Overridden
        [SerializeField] private bool m_OverriddenIsDifferent = false;
        public bool overridden { get { return m_PlatformSettings.overridden; } }
        public bool overriddenIsDifferent { get { return m_OverriddenIsDifferent; } }
        public bool allAreOverridden { get { return isDefault || (overridden && !m_OverriddenIsDifferent); } }
        public void SetOverriddenForAll(bool overridden)
        {
            m_PlatformSettings.overridden = overridden;
            m_OverriddenIsDifferent = false;
            SetChanged();
        }

        // Maximum texture size
        [SerializeField] private bool m_MaxTextureSizeIsDifferent = false;
        public int maxTextureSize { get { return m_PlatformSettings.maxTextureSize; } }
        public bool maxTextureSizeIsDifferent { get { return m_MaxTextureSizeIsDifferent; } }
        public void SetMaxTextureSizeForAll(int maxTextureSize)
        {
            Debug.Assert(allAreOverridden, "Attempting to set max texture size for all platforms even though settings are not overridden for all platforms.");
            m_PlatformSettings.maxTextureSize = maxTextureSize;
            m_MaxTextureSizeIsDifferent = false;
            SetChanged();
        }

        // Resize Algorithm
        [SerializeField]
        private bool m_ResizeAlgorithmIsDifferent = false;
        public TextureResizeAlgorithm resizeAlgorithm { get { return m_PlatformSettings.resizeAlgorithm; } }
        public bool resizeAlgorithmIsDifferent { get { return m_ResizeAlgorithmIsDifferent; } }
        public void SetResizeAlgorithmForAll(TextureResizeAlgorithm algorithm)
        {
            Debug.Assert(allAreOverridden, "Attempting to set resize algorithm for all platforms even though settings are not overridden for all platforms.");
            m_PlatformSettings.resizeAlgorithm = algorithm;
            m_ResizeAlgorithmIsDifferent = false;
            SetChanged();
        }

        // Texture compression
        [SerializeField] private bool m_TextureCompressionIsDifferent = false;
        public TextureImporterCompression textureCompression { get { return m_PlatformSettings.textureCompression; } }
        public bool textureCompressionIsDifferent { get { return m_TextureCompressionIsDifferent; } }
        public void SetTextureCompressionForAll(TextureImporterCompression textureCompression)
        {
            Debug.Assert(allAreOverridden, "Attempting to set texture compression for all platforms even though settings are not overridden for all platforms.");
            m_PlatformSettings.textureCompression = textureCompression;
            m_TextureCompressionIsDifferent = false;
            m_HasChanged = true;
        }

        // Compression rate
        [SerializeField] private bool m_CompressionQualityIsDifferent = false;
        public int compressionQuality { get { return m_PlatformSettings.compressionQuality; } }
        public bool compressionQualityIsDifferent { get { return m_CompressionQualityIsDifferent; } }
        public void SetCompressionQualityForAll(int quality)
        {
            Debug.Assert(allAreOverridden, "Attempting to set texture compression quality for all platforms even though settings are not overridden for all platforms.");
            m_PlatformSettings.compressionQuality = quality;
            m_CompressionQualityIsDifferent = false;
            SetChanged();
        }

        // Crunched compression
        [SerializeField] private bool m_CrunchedCompressionIsDifferent = false;
        public bool crunchedCompression { get { return m_PlatformSettings.crunchedCompression; } }
        public bool crunchedCompressionIsDifferent { get { return m_CrunchedCompressionIsDifferent; } }
        public void SetCrunchedCompressionForAll(bool crunched)
        {
            Debug.Assert(allAreOverridden, "Attempting to set texture crunched compression for all platforms even though settings are not overridden for all platforms.");
            m_PlatformSettings.crunchedCompression = crunched;
            m_CrunchedCompressionIsDifferent = false;
            SetChanged();
        }

        // Texture format
        [SerializeField] private bool m_TextureFormatIsDifferent = false;
        public TextureImporterFormat format { get { return m_PlatformSettings.format; } }
        public bool textureFormatIsDifferent { get { return m_TextureFormatIsDifferent; } }
        public void SetTextureFormatForAll(TextureImporterFormat format)
        {
            Debug.Assert(allAreOverridden, "Attempting to set texture format for all platforms even though settings are not overridden for all platforms.");
            m_PlatformSettings.format = format;
            m_TextureFormatIsDifferent = false;
            SetChanged();
        }

        // Alpha splitting
        [SerializeField]
        private bool m_AlphaSplitIsDifferent = false;
        public bool allowsAlphaSplitting { get { return m_PlatformSettings.allowsAlphaSplitting; } }
        public bool allowsAlphaSplitIsDifferent { get { return m_AlphaSplitIsDifferent; } }
        public void SetAllowsAlphaSplitForAll(bool value)
        {
            Debug.Assert(allAreOverridden, "Attempting to set alpha splitting for all platforms even though settings are not overridden for all platforms.");
            m_PlatformSettings.allowsAlphaSplitting = value;
            m_AlphaSplitIsDifferent = false;
            SetChanged();
        }

        // Android fallback format in case ETC2 is not supported
        [SerializeField]
        private bool m_AndroidETC2FallbackOverrideIsDifferent = false;
        public AndroidETC2FallbackOverride androidETC2FallbackOverride { get { return m_PlatformSettings.androidETC2FallbackOverride; } }
        public bool androidETC2FallbackOverrideIsDifferent { get { return m_AndroidETC2FallbackOverrideIsDifferent; } }
        public void SetAndroidETC2FallbackOverrideForAll(AndroidETC2FallbackOverride value)
        {
            Debug.Assert(allAreOverridden, "Attempting to set android ETC2 fallback format for all platforms even though settings are not overridden for all platforms.");
            m_PlatformSettings.androidETC2FallbackOverride = value;
            m_AndroidETC2FallbackOverrideIsDifferent = false;
            SetChanged();
        }

        [SerializeField] public BuildTarget m_Target;
        [SerializeField] TextureImporter[] m_Importers;
        public TextureImporter[] importers { get { return m_Importers; } }

        [SerializeField] bool m_HasChanged = false;
        [SerializeField] TextureImporterInspector m_Inspector;
        public bool isDefault { get { return name == TextureImporterInspector.s_DefaultPlatformName; } }

        public TextureImportPlatformSettings(string name, BuildTarget target, TextureImporterInspector inspector)
        {
            m_PlatformSettings.name = name;

            m_Target = target;
            m_Inspector = inspector;
            m_PlatformSettings.overridden = false;
            m_Importers = inspector.targets.Select(x => x as TextureImporter).ToArray();
            for (int i = 0; i < importers.Length; i++)
            {
                TextureImporter imp = importers[i];
                TextureImporterPlatformSettings curPlatformSettings = imp.GetPlatformTextureSettings(name);

                if (i == 0)
                {
                    m_PlatformSettings = curPlatformSettings;
                }
                else
                {
                    if (curPlatformSettings.overridden != m_PlatformSettings.overridden)
                        m_OverriddenIsDifferent = true;
                    if (curPlatformSettings.format != m_PlatformSettings.format)
                        m_TextureFormatIsDifferent = true;
                    if (curPlatformSettings.maxTextureSize != m_PlatformSettings.maxTextureSize)
                        m_MaxTextureSizeIsDifferent = true;
                    if (curPlatformSettings.resizeAlgorithm != m_PlatformSettings.resizeAlgorithm)
                        m_ResizeAlgorithmIsDifferent = true;
                    if (curPlatformSettings.textureCompression != m_PlatformSettings.textureCompression)
                        m_TextureCompressionIsDifferent = true;
                    if (curPlatformSettings.compressionQuality != m_PlatformSettings.compressionQuality)
                        m_CompressionQualityIsDifferent = true;
                    if (curPlatformSettings.crunchedCompression != m_PlatformSettings.crunchedCompression)
                        m_CrunchedCompressionIsDifferent = true;
                    if (curPlatformSettings.allowsAlphaSplitting != m_PlatformSettings.allowsAlphaSplitting)
                        m_AlphaSplitIsDifferent = true;
                    if (curPlatformSettings.androidETC2FallbackOverride != m_PlatformSettings.androidETC2FallbackOverride)
                        m_AndroidETC2FallbackOverrideIsDifferent = true;
                }
            }

            Sync();
        }

        public bool SupportsFormat(TextureImporterFormat format, TextureImporter importer)
        {
            TextureImporterSettings settings = GetSettings(importer);
            int[] testValues;
            switch (m_Target)
            {
                case BuildTarget.WiiU:
                    testValues = kTextureFormatsValueWiiU;
                    break;
                case BuildTarget.PSP2:
                    testValues = kTextureFormatsValuePSP2;
                    break;
                case BuildTarget.Switch:
                    testValues = kTextureFormatsValueSwitch;
                    break;
                // on gles mobile targets we use rgb normal maps, so we can use whatever format we want
                case BuildTarget.iOS:
                case BuildTarget.tvOS:
                    testValues = kTextureFormatsValueApplePVR;
                    break;
                case BuildTarget.Android:
                    testValues = kTextureFormatsValueAndroid;
                    break;
                case BuildTarget.Tizen:
                    testValues = kTextureFormatsValueTizen;
                    break;

                default:
                    testValues = settings.textureType == TextureImporterType.NormalMap ? kNormalFormatsValueDefault : kTextureFormatsValueDefault;
                    break;
            }

            if ((testValues as IList).Contains((int)format))
                return true;
            return false;
        }

        public TextureImporterSettings GetSettings(TextureImporter importer)
        {
            TextureImporterSettings settings = new TextureImporterSettings();
            // Get import settings for this importer
            importer.ReadTextureSettings(settings);
            // Get settings that have been changed in the inspector
            m_Inspector.GetSerializedPropertySettings(settings);
            return settings;
        }

        public virtual void SetChanged()
        {
            m_HasChanged = true;
        }

        public virtual bool HasChanged()
        {
            return m_HasChanged;
        }

        public void Sync()
        {
            // Use settings from default if any of the targets are not overridden
            if (!isDefault && (!overridden || m_OverriddenIsDifferent))
            {
                TextureImportPlatformSettings defaultSettings = m_Inspector.m_PlatformSettings[0];
                m_PlatformSettings.maxTextureSize = defaultSettings.maxTextureSize;
                m_MaxTextureSizeIsDifferent = defaultSettings.m_MaxTextureSizeIsDifferent;
                m_PlatformSettings.resizeAlgorithm = defaultSettings.resizeAlgorithm;
                m_ResizeAlgorithmIsDifferent = defaultSettings.m_ResizeAlgorithmIsDifferent;
                m_PlatformSettings.textureCompression = defaultSettings.textureCompression;
                m_TextureCompressionIsDifferent = defaultSettings.m_TextureCompressionIsDifferent;
                m_PlatformSettings.format = defaultSettings.format;
                m_TextureFormatIsDifferent = defaultSettings.m_TextureFormatIsDifferent;
                m_PlatformSettings.compressionQuality = defaultSettings.compressionQuality;
                m_CompressionQualityIsDifferent = defaultSettings.m_CompressionQualityIsDifferent;
                m_PlatformSettings.crunchedCompression = defaultSettings.crunchedCompression;
                m_CrunchedCompressionIsDifferent = defaultSettings.m_CrunchedCompressionIsDifferent;
                m_PlatformSettings.allowsAlphaSplitting = defaultSettings.allowsAlphaSplitting;
                m_AlphaSplitIsDifferent = defaultSettings.m_AlphaSplitIsDifferent;
                m_AndroidETC2FallbackOverrideIsDifferent = defaultSettings.m_AndroidETC2FallbackOverrideIsDifferent;
            }

            if ((overridden || m_OverriddenIsDifferent) && m_PlatformSettings.format < 0)
            {
                m_PlatformSettings.format = TextureImporter.FormatFromTextureParameters(GetSettings(importers[0]),
                        m_PlatformSettings,
                        importers[0].DoesSourceTextureHaveAlpha(),
                        importers[0].IsSourceTextureHDR(),
                        m_Target
                        );
                m_TextureFormatIsDifferent = false;

                for (int i = 1; i < importers.Length; i++)
                {
                    TextureImporter imp = importers[i];
                    TextureImporterSettings settings = GetSettings(imp);

                    TextureImporterFormat format = TextureImporter.FormatFromTextureParameters(settings,
                            m_PlatformSettings,
                            imp.DoesSourceTextureHaveAlpha(),
                            imp.IsSourceTextureHDR(),
                            m_Target
                            );
                    if (format != m_PlatformSettings.format)
                        m_TextureFormatIsDifferent = true;
                }
            }
        }

        private bool GetOverridden(TextureImporter importer)
        {
            if (!m_OverriddenIsDifferent)
                return overridden;
            return importer.GetPlatformTextureSettings(name).overridden;
        }

        public void Apply()
        {
            for (int i = 0; i < importers.Length; i++)
            {
                TextureImporter imp = importers[i];

                TextureImporterPlatformSettings platformSettings = imp.GetPlatformTextureSettings(name);

                // Overwrite with inspector properties if same for all targets
                if (!m_OverriddenIsDifferent)
                    platformSettings.overridden = m_PlatformSettings.overridden;
                if (!m_TextureFormatIsDifferent)
                    platformSettings.format = m_PlatformSettings.format;
                if (!m_MaxTextureSizeIsDifferent)
                    platformSettings.maxTextureSize = m_PlatformSettings.maxTextureSize;
                if (!m_ResizeAlgorithmIsDifferent)
                    platformSettings.resizeAlgorithm = m_PlatformSettings.resizeAlgorithm;
                if (!m_TextureCompressionIsDifferent)
                    platformSettings.textureCompression = m_PlatformSettings.textureCompression;
                if (!m_CompressionQualityIsDifferent)
                    platformSettings.compressionQuality = m_PlatformSettings.compressionQuality;
                if (!m_CrunchedCompressionIsDifferent)
                    platformSettings.crunchedCompression = m_PlatformSettings.crunchedCompression;
                if (!m_AlphaSplitIsDifferent)
                    platformSettings.allowsAlphaSplitting = m_PlatformSettings.allowsAlphaSplitting;
                if (!m_AndroidETC2FallbackOverrideIsDifferent)
                    platformSettings.androidETC2FallbackOverride = m_PlatformSettings.androidETC2FallbackOverride;

                imp.SetPlatformTextureSettings(platformSettings);
            }
        }

        public static readonly int[] kTextureFormatsValueWiiU =
        {
            (int)TextureImporterFormat.DXT1,
            (int)TextureImporterFormat.DXT5,
            (int)TextureImporterFormat.RGB16, // R5G6B5
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.RGBA32,
            (int)TextureImporterFormat.RGBA16, // R4G4B4A4
        };

        public static readonly int[] kTextureFormatsValuePSP2 =
        {
            (int)TextureImporterFormat.DXT1,
            (int)TextureImporterFormat.DXT5,
            (int)TextureImporterFormat.RGB16,
            (int)TextureImporterFormat.RGB24,
            (int)TextureImporterFormat.RGBA16,
            (int)TextureImporterFormat.RGBA32,
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.RGBAHalf,
        };

        public static readonly int[] kTextureFormatsValueSwitch =
        {
            (int)TextureImporterFormat.DXT1,
            (int)TextureImporterFormat.DXT5,
            (int)TextureImporterFormat.DXT1Crunched,
            (int)TextureImporterFormat.DXT5Crunched,
            (int)TextureImporterFormat.RGB16,
            (int)TextureImporterFormat.RGB24,
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.ARGB16,
            (int)TextureImporterFormat.RGBA32,
            (int)TextureImporterFormat.RGBAHalf,
            (int)TextureImporterFormat.BC4,
            (int)TextureImporterFormat.BC5,
            (int)TextureImporterFormat.BC6H,
            (int)TextureImporterFormat.BC7,

            (int)TextureImporterFormat.ASTC_RGB_4x4,
            (int)TextureImporterFormat.ASTC_RGB_5x5,
            (int)TextureImporterFormat.ASTC_RGB_6x6,
            (int)TextureImporterFormat.ASTC_RGB_8x8,
            (int)TextureImporterFormat.ASTC_RGB_10x10,
            (int)TextureImporterFormat.ASTC_RGB_12x12,
            (int)TextureImporterFormat.ASTC_RGBA_4x4,
            (int)TextureImporterFormat.ASTC_RGBA_5x5,
            (int)TextureImporterFormat.ASTC_RGBA_6x6,
            (int)TextureImporterFormat.ASTC_RGBA_8x8,
            (int)TextureImporterFormat.ASTC_RGBA_10x10,
            (int)TextureImporterFormat.ASTC_RGBA_12x12,
        };

        public static readonly int[] kTextureFormatsValueApplePVR =
        {
            (int)TextureImporterFormat.PVRTC_RGB2,
            (int)TextureImporterFormat.PVRTC_RGBA2,
            (int)TextureImporterFormat.PVRTC_RGB4,
            (int)TextureImporterFormat.PVRTC_RGBA4,

            (int)TextureImporterFormat.ASTC_RGB_4x4,
            (int)TextureImporterFormat.ASTC_RGB_5x5,
            (int)TextureImporterFormat.ASTC_RGB_6x6,
            (int)TextureImporterFormat.ASTC_RGB_8x8,
            (int)TextureImporterFormat.ASTC_RGB_10x10,
            (int)TextureImporterFormat.ASTC_RGB_12x12,
            (int)TextureImporterFormat.ASTC_RGBA_4x4,
            (int)TextureImporterFormat.ASTC_RGBA_5x5,
            (int)TextureImporterFormat.ASTC_RGBA_6x6,
            (int)TextureImporterFormat.ASTC_RGBA_8x8,
            (int)TextureImporterFormat.ASTC_RGBA_10x10,
            (int)TextureImporterFormat.ASTC_RGBA_12x12,

            (int)TextureImporterFormat.ETC_RGB4,
            (int)TextureImporterFormat.ETC_RGB4Crunched,
            (int)TextureImporterFormat.ETC2_RGBA8,
            (int)TextureImporterFormat.ETC2_RGBA8Crunched,

            (int)TextureImporterFormat.RGB16,
            (int)TextureImporterFormat.RGB24,
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.RGBA16,
            (int)TextureImporterFormat.RGBA32
        };

        public static readonly int[] kTextureFormatsValueAndroid =
        {
            (int)TextureImporterFormat.DXT1,
            (int)TextureImporterFormat.DXT1Crunched,
            (int)TextureImporterFormat.DXT5,
            (int)TextureImporterFormat.DXT5Crunched,

            (int)TextureImporterFormat.ETC_RGB4,
            (int)TextureImporterFormat.ETC_RGB4Crunched,
            (int)TextureImporterFormat.ETC2_RGB4,
            (int)TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA,
            (int)TextureImporterFormat.ETC2_RGBA8,
            (int)TextureImporterFormat.ETC2_RGBA8Crunched,

            (int)TextureImporterFormat.PVRTC_RGB2,
            (int)TextureImporterFormat.PVRTC_RGBA2,
            (int)TextureImporterFormat.PVRTC_RGB4,
            (int)TextureImporterFormat.PVRTC_RGBA4,

            (int)TextureImporterFormat.ATC_RGB4,
            (int)TextureImporterFormat.ATC_RGBA8,

            (int)TextureImporterFormat.ASTC_RGB_4x4,
            (int)TextureImporterFormat.ASTC_RGB_5x5,
            (int)TextureImporterFormat.ASTC_RGB_6x6,
            (int)TextureImporterFormat.ASTC_RGB_8x8,
            (int)TextureImporterFormat.ASTC_RGB_10x10,
            (int)TextureImporterFormat.ASTC_RGB_12x12,
            (int)TextureImporterFormat.ASTC_RGBA_4x4,
            (int)TextureImporterFormat.ASTC_RGBA_5x5,
            (int)TextureImporterFormat.ASTC_RGBA_6x6,
            (int)TextureImporterFormat.ASTC_RGBA_8x8,
            (int)TextureImporterFormat.ASTC_RGBA_10x10,
            (int)TextureImporterFormat.ASTC_RGBA_12x12,

            (int)TextureImporterFormat.RGB16,
            (int)TextureImporterFormat.RGB24,
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.RGBA16,
            (int)TextureImporterFormat.RGBA32
        };

        public static readonly int[] kTextureFormatsValueTizen =
        {
            (int)TextureImporterFormat.ETC_RGB4,

            (int)TextureImporterFormat.RGB16,
            (int)TextureImporterFormat.RGB24,
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.RGBA16,
            (int)TextureImporterFormat.RGBA32,
        };

        public static readonly int[] kTextureFormatsValueWebGL =
        {
            (int)TextureImporterFormat.DXT1,
            (int)TextureImporterFormat.DXT5,
            (int)TextureImporterFormat.DXT1Crunched,
            (int)TextureImporterFormat.DXT5Crunched,
            (int)TextureImporterFormat.RGB16,
            (int)TextureImporterFormat.RGB24,
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.ARGB16,
            (int)TextureImporterFormat.RGBA32
        };

        public static readonly int[] kNormalFormatsValueDefault =
        {
            (int)TextureImporterFormat.BC5,
            (int)TextureImporterFormat.BC7,
            (int)TextureImporterFormat.DXT5,
            (int)TextureImporterFormat.DXT5Crunched,
            (int)TextureImporterFormat.ARGB16,
            (int)TextureImporterFormat.RGBA32,
        };
        public static readonly int[] kTextureFormatsValueDefault =
        {
            (int)TextureImporterFormat.DXT1,
            (int)TextureImporterFormat.DXT5,
            (int)TextureImporterFormat.DXT1Crunched,
            (int)TextureImporterFormat.DXT5Crunched,
            (int)TextureImporterFormat.RGB16,
            (int)TextureImporterFormat.RGB24,
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.ARGB16,
            (int)TextureImporterFormat.RGBA32,
            (int)TextureImporterFormat.RGBAHalf,
            (int)TextureImporterFormat.BC4,
            (int)TextureImporterFormat.BC5,
            (int)TextureImporterFormat.BC6H,
            (int)TextureImporterFormat.BC7,
        };
        public static readonly int[] kTextureFormatsValueSingleChannel =
        {
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.BC4,
        };

        public static readonly int[] kAndroidETC2FallbackOverrideValues =
        {
            (int)AndroidETC2FallbackOverride.UseBuildSettings,
            (int)AndroidETC2FallbackOverride.Quality32Bit,
            (int)AndroidETC2FallbackOverride.Quality16Bit,
            (int)AndroidETC2FallbackOverride.Quality32BitDownscaled,
        };
    }
}
