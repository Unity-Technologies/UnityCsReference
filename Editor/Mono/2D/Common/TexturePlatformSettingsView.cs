// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.U2D.Interface;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.U2D.Common
{
    internal class TexturePlatformSettingsView : ITexturePlatformSettingsView
    {
        class Styles
        {
            public readonly GUIContent textureFormatLabel = L10n.TextContent("Format", null, null, null);
            public readonly GUIContent maxTextureSizeLabel = L10n.TextContent("Max Texture Size", "Maximum size of the packed texture.", null, null);
            public readonly GUIContent compressionLabel = L10n.TextContent("Compression", "How will this texture be compressed?", null, null);
            public readonly GUIContent useCrunchedCompressionLabel = L10n.TextContent("Use Crunch Compression", "Texture is crunch-compressed to save space on disk when applicable.", null, null);
            public readonly GUIContent useAlphaSplitLabel = L10n.TextContent("Split Alpha Channel", "Alpha for this texture will be preserved by splitting the alpha channel to another texture, and both resulting textures will be compressed using ETC1.", null, null);
            public readonly GUIContent compressionQualityLabel = L10n.TextContent("Compressor Quality", null, null, null);
            public readonly GUIContent compressionQualitySliderLabel = L10n.TextContent("Compressor Quality", "Use the slider to adjust compression quality from 0 (Fastest) to 100 (Best)", null, null);

            public readonly int[] kMaxTextureSizeValues = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 };
            public readonly GUIContent[] kMaxTextureSizeStrings;

            public readonly GUIContent[] kTextureCompressionOptions =
            {
                L10n.TextContent("None", "Texture is not compressed.", null, null),
                L10n.TextContent("Low Quality", "Texture compressed with low quality but high performance, high compression format.", null, null),
                L10n.TextContent("Normal Quality", "Texture is compressed with a standard format.", null, null),
                L10n.TextContent("High Quality", "Texture compressed with a high quality format.", null, null),
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
                L10n.TextContent("Fast", null, null, null),
                L10n.TextContent("Normal", null, null, null),
                L10n.TextContent("Best", null, null, null)
            };

            public Styles()
            {
                kMaxTextureSizeStrings = new GUIContent[kMaxTextureSizeValues.Length];
                for (var i = 0; i < kMaxTextureSizeValues.Length; ++i)
                    kMaxTextureSizeStrings[i] = EditorGUIUtility.TextContent(string.Format("{0}", kMaxTextureSizeValues[i]));
            }
        }

        [NoAutoStaticsCleanup] // Lazy GUIContent cache loaded by fixed name; safe to persist across reloads.
        private static Styles s_Styles;

        public string buildPlatformTitle { get; set; }

        internal TexturePlatformSettingsView()
        {
            s_Styles = s_Styles ?? new Styles();
        }

        public virtual TextureImporterCompression DrawCompression(TextureImporterCompression defaultValue, bool isMixedValue, bool isDisabled, out bool changed)
        {
            using (new EditorGUI.DisabledScope(isDisabled))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = isMixedValue;
                defaultValue = (TextureImporterCompression)EditorGUILayout.IntPopup(s_Styles.compressionLabel,
                    (int)defaultValue, s_Styles.kTextureCompressionOptions, s_Styles.kTextureCompressionValues);
                EditorGUI.showMixedValue = false;
                changed = EditorGUI.EndChangeCheck();
                return defaultValue;
            }
        }

        public virtual bool DrawUseCrunchedCompression(bool defaultValue, bool isMixedValue, bool isDisabled, out bool changed)
        {
            using (new EditorGUI.DisabledScope(isDisabled))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = isMixedValue;
                defaultValue = EditorGUILayout.Toggle(s_Styles.useCrunchedCompressionLabel, defaultValue);
                EditorGUI.showMixedValue = false;
                changed = EditorGUI.EndChangeCheck();
                return defaultValue;
            }
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

        public virtual int DrawMaxSize(int defaultValue, bool isMixedValue, bool isDisabled, out bool changed)
        {
            using (new EditorGUI.DisabledScope(isDisabled))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = isMixedValue;
                defaultValue = EditorGUILayout.IntPopup(s_Styles.maxTextureSizeLabel, defaultValue,
                    s_Styles.kMaxTextureSizeStrings, s_Styles.kMaxTextureSizeValues);
                EditorGUI.showMixedValue = false;
                changed = EditorGUI.EndChangeCheck();
                return defaultValue;
            }
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

        public virtual int DrawCompressionQualityPopup(int defaultValue, bool isMixedValue, bool isDisabled, out bool changed)
        {
            using (new EditorGUI.DisabledScope(isDisabled))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = isMixedValue;
                defaultValue = EditorGUILayout.Popup(s_Styles.compressionQualityLabel, defaultValue,
                    s_Styles.kMobileCompressionQualityOptions);
                EditorGUI.showMixedValue = false;
                changed = EditorGUI.EndChangeCheck();
                return defaultValue;
            }
        }

        public virtual int DrawCompressionQualitySlider(int defaultValue, bool isMixedValue, bool isDisabled, out bool changed)
        {
            using (new EditorGUI.DisabledScope(isDisabled))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = isMixedValue;
                defaultValue = EditorGUILayout.IntSlider(s_Styles.compressionQualitySliderLabel, defaultValue, 0, 100);
                EditorGUI.showMixedValue = false;
                changed = EditorGUI.EndChangeCheck();
                return defaultValue;
            }
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
