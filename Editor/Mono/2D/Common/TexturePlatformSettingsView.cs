// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.U2D.Interface;

namespace UnityEditor.U2D.Common
{
    internal class TexturePlatformSettingsView : ITexturePlatformSettingsView
    {
        class Styles
        {
            public readonly GUIContent textureFormatLabel = EditorGUIUtility.TextContent("Format");
            public readonly GUIContent maxTextureSizeLabel = EditorGUIUtility.TextContent("Max Texture Size|Maximum size of the packed texture.");
            public readonly GUIContent compressionLabel = EditorGUIUtility.TextContent("Compression|How will this texture be compressed?");
            public readonly GUIContent useCrunchedCompressionLabel = EditorGUIUtility.TextContent("Use Crunch Compression|Texture is crunch-compressed to save space on disk when applicable.");
            public readonly GUIContent useAlphaSplitLabel = EditorGUIUtility.TrTextContent("Split Alpha Channel", "Alpha for this texture will be preserved by splitting the alpha channel to another texture, and both resulting textures will be compressed using ETC1.");
            public readonly GUIContent compressionQualityLabel = EditorGUIUtility.TextContent("Compressor Quality");
            public readonly GUIContent compressionQualitySliderLabel = EditorGUIUtility.TextContent("Compressor Quality|Use the slider to adjust compression quality from 0 (Fastest) to 100 (Best)");

            public readonly int[] kMaxTextureSizeValues = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
            public readonly GUIContent[] kMaxTextureSizeStrings;

            public readonly GUIContent[] kTextureCompressionOptions =
            {
                EditorGUIUtility.TextContent("None|Texture is not compressed."),
                EditorGUIUtility.TextContent("Low Quality|Texture compressed with low quality but high performance, high compression format."),
                EditorGUIUtility.TextContent("Normal Quality|Texture is compressed with a standard format."),
                EditorGUIUtility.TextContent("High Quality|Texture compressed with a high quality format."),
            };

            public readonly int[] kTextureCompressionValues =
            {
                (int)TextureImporterCompression.Uncompressed,
                (int)TextureImporterCompression.CompressedLQ,
                (int)TextureImporterCompression.Compressed,
                (int)TextureImporterCompression.CompressedHQ
            };

            public readonly GUIContent[] kMobileCompressionQualityOptions =
            {
                EditorGUIUtility.TextContent("Fast"),
                EditorGUIUtility.TextContent("Normal"),
                EditorGUIUtility.TextContent("Best")
            };

            public Styles()
            {
                kMaxTextureSizeStrings = new GUIContent[kMaxTextureSizeValues.Length];
                for (var i = 0; i < kMaxTextureSizeValues.Length; ++i)
                    kMaxTextureSizeStrings[i] = EditorGUIUtility.TextContent(string.Format("{0}", kMaxTextureSizeValues[i]));
            }
        }

        private static Styles s_Styles;

        public string buildPlatformTitle { get; set; }

        internal TexturePlatformSettingsView()
        {
            s_Styles = s_Styles ?? new Styles();
        }

        public virtual TextureImporterCompression DrawCompression(TextureImporterCompression defaultValue, bool isMixedValue, out bool changed)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = isMixedValue;
            defaultValue = (TextureImporterCompression)EditorGUILayout.IntPopup(s_Styles.compressionLabel, (int)defaultValue, s_Styles.kTextureCompressionOptions, s_Styles.kTextureCompressionValues);
            EditorGUI.showMixedValue = false;
            changed = EditorGUI.EndChangeCheck();
            return defaultValue;
        }

        public virtual bool DrawUseCrunchedCompression(bool defaultValue, bool isMixedValue, out bool changed)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = isMixedValue;
            defaultValue = EditorGUILayout.Toggle(s_Styles.useCrunchedCompressionLabel, defaultValue);
            EditorGUI.showMixedValue = false;
            changed = EditorGUI.EndChangeCheck();
            return defaultValue;
        }

        public virtual bool DrawOverride(bool defaultValue, bool isMixedValue, out bool changed)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = isMixedValue;
            defaultValue = EditorGUILayout.ToggleLeft(EditorGUIUtility.TempContent("Override for " + buildPlatformTitle), defaultValue);
            EditorGUI.showMixedValue = false;
            changed = EditorGUI.EndChangeCheck();
            return defaultValue;
        }

        public virtual int DrawMaxSize(int defaultValue, bool isMixedValue, out bool changed)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = isMixedValue;
            defaultValue = EditorGUILayout.IntPopup(s_Styles.maxTextureSizeLabel, defaultValue, s_Styles.kMaxTextureSizeStrings, s_Styles.kMaxTextureSizeValues);
            EditorGUI.showMixedValue = false;
            changed = EditorGUI.EndChangeCheck();
            return defaultValue;
        }

        public virtual TextureImporterFormat DrawFormat(TextureImporterFormat defaultValue, int[] displayValues, string[] displayStrings, bool isMixedValue, bool isDisabled, out bool changed)
        {
            using (new EditorGUI.DisabledScope(isDisabled))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = isMixedValue;
                defaultValue = (TextureImporterFormat)EditorGUILayout.IntPopup(s_Styles.textureFormatLabel, (int)defaultValue, EditorGUIUtility.TempContent(displayStrings), displayValues);
                EditorGUI.showMixedValue = false;
                changed = EditorGUI.EndChangeCheck();
                return defaultValue;
            }
        }

        public virtual int DrawCompressionQualityPopup(int defaultValue, bool isMixedValue, out bool changed)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = isMixedValue;
            defaultValue = EditorGUILayout.Popup(s_Styles.compressionQualityLabel, defaultValue, s_Styles.kMobileCompressionQualityOptions);
            EditorGUI.showMixedValue = false;
            changed = EditorGUI.EndChangeCheck();
            return defaultValue;
        }

        public virtual int DrawCompressionQualitySlider(int defaultValue, bool isMixedValue, out bool changed)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = isMixedValue;
            defaultValue = EditorGUILayout.IntSlider(s_Styles.compressionQualitySliderLabel, defaultValue, 0, 100);
            EditorGUI.showMixedValue = false;
            changed = EditorGUI.EndChangeCheck();
            return defaultValue;
        }

        public virtual bool DrawAlphaSplit(bool defaultValue, bool isMixedValue, bool isDisabled, out bool changed)
        {
            using (new EditorGUI.DisabledScope(isDisabled))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = isMixedValue;
                defaultValue = EditorGUILayout.Toggle(s_Styles.useAlphaSplitLabel, defaultValue);
                EditorGUI.showMixedValue = false;
                changed = EditorGUI.EndChangeCheck();
                return defaultValue;
            }
        }
    }
}
