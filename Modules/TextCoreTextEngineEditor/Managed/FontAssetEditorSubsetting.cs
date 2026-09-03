// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.TextCore.Text;

#pragma warning disable CS0618 // AtlasPopulationMode.Static is obsolete; this section migrates away from it

namespace UnityEditor.TextCore.Text
{
    // Font Subsetting inspector section and the static-to-dynamic conversion flow.
    internal partial class FontAssetEditor
    {
        enum SubsetPreset
        {
            Ascii,
            ExtendedAscii,
            AsciiLowercase,
            AsciiUppercase,
            NumbersAndSymbols,
            FromBakedAtlas,
            Custom,
        }

        static readonly GUIContent[] k_SubsetPresetLabels =
        {
            new GUIContent("ASCII"),
            new GUIContent("Extended ASCII"),
            new GUIContent("ASCII Lowercase"),
            new GUIContent("ASCII Uppercase"),
            new GUIContent("Numbers + Symbols"),
            new GUIContent("From Baked Atlas"),
            new GUIContent("Custom"),
        };

        // The Font Asset Creator character sets, as canonical hex ranges.
        static readonly string[] k_SubsetPresetRanges =
        {
            "20-7e,a0,200b,2026,25a1",
            "20-7e,a0-ff,2000-206f,20ac,2122,25a1",
            "20-40,5b-7e,a0",
            "20-60,7b-7e,a0",
            "20-40,5b-60,7b-7e,a0",
            null,
            null,
        };

        const int k_SubsetInputMaxLength = 4096;
        const int k_MissingCodePointsListedMax = 512;
        const float k_SubsetCharactersAreaHeight = 48; // three wrapped lines; taller content scrolls
        const float k_MissingCodePointsMaxHeight = 150; // list hugs its content, then scrolls

        string m_SubsetCharactersInput;
        Vector2 m_SubsetCharactersScroll;
        SubsetPreset m_SubsetPreset = SubsetPreset.Custom;
        bool m_SubsetSectionInitialized;
        // Probing the cmap and previewing the subset size load the source font, so cache per input rather than per repaint.
        string m_MissingCodePointsFor;
        string m_MissingCodePoints;
        string m_MissingCodePointsDisplay;
        int m_MissingCodePointsCount;
        bool m_MissingCodePointsFoldout;
        Vector2 m_MissingCodePointsScroll;
        long m_SubsetSizePreview;
        // Disabled until the conversion flow ships with its legal notice.
        /*
        // Loads a font face, so computed once per inspector instance rather than per repaint.
        bool? m_StaleGlyphIndices;

        void DrawStaticMigrationSection()
        {
            if (targets.Length != 1 || m_AtlasPopulationMode_prop.intValue != (int)AtlasPopulationMode.Static)
                return;

            EditorGUILayout.HelpBox(FontAssetStaticMigrator.StaticNotSupportedMessage, MessageType.Warning, true);

            bool canMigrate = FontAssetStaticMigrator.CanMigrate(m_fontAsset, out string reason);
            bool outOfSync = canMigrate && (m_StaleGlyphIndices ??= FontAssetStaticMigrator.HasStaleGlyphIndices(m_fontAsset));

            if (outOfSync)
                EditorGUILayout.HelpBox(FontAssetStaticMigrator.OutOfSyncMessage, MessageType.Warning);
            else if (!canMigrate)
                EditorGUILayout.HelpBox(reason, MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(!canMigrate || outOfSync))
                {
                    if (GUILayout.Button("Convert to Dynamic", GUILayout.Width(160)))
                        ConvertStaticToDynamic();
                }
            }

            EditorGUILayout.Space();
        }

        void ConvertStaticToDynamic()
        {
            if (FontAssetStaticMigrator.Convert(m_fontAsset, out string error))
            {
                serializedObject.Update();
                m_SubsetSectionInitialized = false;
                UI_PanelState.fontSubsettingPanel = true;
            }
            else
            {
                Debug.LogError($"Font asset conversion failed: {error}", m_fontAsset);
            }

            GUIUtility.ExitGUI();
        }
        */

        void DrawFontSubsettingSection()
        {
            if (m_fontAsset == null || targets.Length > 1 || m_fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic)
                return;

            bool hasImporter = FontSubsetterManager.TryGetSourceFontImporter(m_fontAsset, out _, out string fontPath);
            bool active = FontSubsetterManager.TryGetActiveRecipe(m_fontAsset, out var activeRecipe);

            Rect rect = EditorGUILayout.GetControlRect(false, 24);
            if (GUI.Button(rect, new GUIContent("<b>Font Subsetting</b>", "Subsets the source font so only the selected characters ship in builds."), TM_EditorStyles.sectionHeader))
                UI_PanelState.fontSubsettingPanel = !UI_PanelState.fontSubsettingPanel;

            GUI.Label(rect, active ? "Active" : (UI_PanelState.fontSubsettingPanel ? "" : s_UiStateLabel[1]), TM_EditorStyles.rightLabel);

            if (!UI_PanelState.fontSubsettingPanel)
                return;

            EditorGUI.indentLevel = 1;

            if (!hasImporter)
            {
                EditorGUILayout.HelpBox(FontSubsetterManager.NoSourceFontImporterMessage, MessageType.Info);
                EditorGUI.indentLevel = 0;
                EditorGUILayout.Space();
                return;
            }

            if (!m_SubsetSectionInitialized)
            {
                m_SubsetSectionInitialized = true;
                if (active)
                {
                    int presetIndex = Array.IndexOf(k_SubsetPresetRanges, activeRecipe.characters);
                    m_SubsetPreset = presetIndex >= 0 ? (SubsetPreset)presetIndex : SubsetPreset.Custom;
                    if (m_SubsetPreset == SubsetPreset.Custom)
                        m_SubsetCharactersInput = UnicodeRanges.ToCharacters(activeRecipe.characters, k_SubsetInputMaxLength) ?? string.Empty;
                }
            }

            if (active)
                EditorGUILayout.LabelField(new GUIContent("Active Subset"), new GUIContent($"{UnicodeRanges.CountCodePoints(activeRecipe.characters)} code points", activeRecipe.characters));

            EditorGUI.BeginChangeCheck();
            m_SubsetPreset = (SubsetPreset)EditorGUILayout.Popup(new GUIContent("Preset"), (int)m_SubsetPreset, k_SubsetPresetLabels);
            if (EditorGUI.EndChangeCheck() && m_SubsetPreset == SubsetPreset.FromBakedAtlas)
            {
                string presetCharacters = CharactersFromBakedAtlas(m_fontAsset);
                if (presetCharacters != null)
                {
                    m_SubsetCharactersInput = presetCharacters;
                    m_SubsetCharactersScroll = Vector2.zero;
                }
                else
                {
                    m_SubsetPreset = SubsetPreset.Custom;
                    Debug.LogWarning($"The baked atlas has more characters than the Characters field can hold ({k_SubsetInputMaxLength}).", m_fontAsset);
                }
            }

            string presetRanges = k_SubsetPresetRanges[(int)m_SubsetPreset];
            if (presetRanges == null)
            {
                EditorGUILayout.LabelField(new GUIContent("Characters", "Characters to keep in the subset. All other glyphs are removed from the font data embedded in builds."));
                // Line breaks are not subset characters, so keep Return from inserting one.
                var evt = Event.current;
                if (evt.type == EventType.KeyDown && (evt.character == '\n' || evt.character == '\r'))
                    evt.character = '\0';
                EditorGUI.BeginChangeCheck();
                Rect areaRect = EditorGUILayout.GetControlRect(false, k_SubsetCharactersAreaHeight);
                m_SubsetCharactersInput = EditorGUI.ScrollableTextAreaInternal(areaRect, m_SubsetCharactersInput, ref m_SubsetCharactersScroll, EditorStyles.textArea);
                if (EditorGUI.EndChangeCheck())
                {
                    m_SubsetCharactersInput = m_SubsetCharactersInput.Replace("\n", string.Empty).Replace("\r", string.Empty);
                    m_SubsetPreset = SubsetPreset.Custom;
                }
            }

            string ranges = presetRanges ?? UnicodeRanges.FromCharacters(m_SubsetCharactersInput);

            if (ranges != m_MissingCodePointsFor)
            {
                m_MissingCodePointsFor = ranges;
                int sourceFaceIndex = active ? activeRecipe.faceIndex : m_fontAsset.faceInfo.faceIndex;
                m_MissingCodePoints = FontSubsetterManager.GetMissingCodePoints(m_fontAsset, ranges, sourceFaceIndex);
                m_MissingCodePointsCount = UnicodeRanges.CountCodePoints(m_MissingCodePoints);
                m_MissingCodePointsDisplay = FormatMissingCodePoints(m_MissingCodePoints, k_MissingCodePointsListedMax);
                // The applied sub-asset already carries the real size, so only preview pending input.
                m_SubsetSizePreview = active && ranges == activeRecipe.characters
                    ? 0
                    : FontSubsetterManager.GetSubsetSizePreview(m_fontAsset, ranges, sourceFaceIndex);
            }

            if (m_MissingCodePointsCount > 0)
            {
                m_MissingCodePointsFoldout = EditorGUILayout.Foldout(m_MissingCodePointsFoldout,
                    new GUIContent($"Missing Characters ({m_MissingCodePointsCount})", "Code points the source font has no glyph for. They are left out of the subset."), true);
                if (m_MissingCodePointsFoldout)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        m_MissingCodePointsScroll = EditorGUILayout.BeginScrollView(m_MissingCodePointsScroll, GUILayout.MaxHeight(k_MissingCodePointsMaxHeight));
                        Rect textRect = GUILayoutUtility.GetRect(new GUIContent(m_MissingCodePointsDisplay), EditorStyles.wordWrappedLabel);
                        EditorGUI.SelectableLabel(textRect, m_MissingCodePointsDisplay, EditorStyles.wordWrappedLabel);
                        EditorGUILayout.EndScrollView();
                    }
                }
            }

            DrawSubsetSizeLine(active, fontPath);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(ranges)))
                {
                    if (GUILayout.Button("Update Subset", GUILayout.Width(120)))
                    {
                        if (!FontSubsetterManager.ApplySubset(m_fontAsset, ranges, out string error))
                            Debug.LogError($"Font subsetting failed: {error}", m_fontAsset);
                        m_MissingCodePointsFor = null;
                        GUIUtility.ExitGUI();
                    }
                }
                using (new EditorGUI.DisabledScope(!active))
                {
                    if (GUILayout.Button("Remove Subset", GUILayout.Width(120)))
                    {
                        FontSubsetterManager.RemoveSubset(m_fontAsset);
                        m_SubsetSectionInitialized = false;
                        m_MissingCodePointsFor = null;
                        GUIUtility.ExitGUI();
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUI.indentLevel = 0;
        }

        // Returns null when the baked character set exceeds the Characters field limit.
        internal static string CharactersFromBakedAtlas(FontAsset fontAsset)
        {
            var ranges = UnicodeRanges.FromCodePoints(FontAssetStaticMigrator.GetBakedCodePoints(fontAsset));
            return UnicodeRanges.ToCharacters(ranges, k_SubsetInputMaxLength);
        }

        internal static string FormatMissingCodePoints(string missingRanges, int maxEntries)
        {
            var builder = new StringBuilder();
            int count = 0;
            foreach (uint c in UnicodeRanges.EnumerateCodePoints(missingRanges))
            {
                if (count == maxEntries)
                {
                    builder.Append($"\n… (+{UnicodeRanges.CountCodePoints(missingRanges) - maxEntries} more)");
                    break;
                }
                if (count > 0)
                    builder.Append('\n');

                var category = CharUnicodeInfo.GetUnicodeCategory((int)c);
                bool renderable = category != UnicodeCategory.Format
                    && category != UnicodeCategory.Control
                    && category != UnicodeCategory.LineSeparator
                    && category != UnicodeCategory.ParagraphSeparator;
                builder.Append($"ID: {c}\tHex: {c:X}\t");
                builder.Append(renderable ? $"Char [{char.ConvertFromUtf32((int)c)}]" : "Char []");
                count++;
            }
            return builder.ToString();
        }

        void DrawSubsetSizeLine(bool active, string fontPath)
        {
            var fontFile = new System.IO.FileInfo(fontPath);
            if (!fontFile.Exists)
                return;

            long originalSize = fontFile.Length;
            bool preview = m_SubsetSizePreview > 0;
            long subsetSize = preview ? m_SubsetSizePreview : (active ? GetSubsetFontDataSize() : 0);
            string sizeText = subsetSize > 0
                ? $"{EditorUtility.FormatBytes(originalSize)} → {EditorUtility.FormatBytes(subsetSize)} (-{(1f - (float)subsetSize / originalSize) * 100f:0.#} %)"
                : $"{EditorUtility.FormatBytes(originalSize)} (no subset applied)";
            if (preview)
                sizeText += " · preview";

            EditorGUILayout.LabelField("Font File Size", sizeText);
        }

        long GetSubsetFontDataSize()
        {
            var subsetFont = m_fontAsset.sourceFontFile;
            if (subsetFont == null || !FontSubsetterManager.IsSubsetActive(m_fontAsset))
                return 0;

            using var subsetFontObject = new SerializedObject(subsetFont);
            return subsetFontObject.FindProperty("m_FontData")?.arraySize ?? 0;
        }
    }
}
