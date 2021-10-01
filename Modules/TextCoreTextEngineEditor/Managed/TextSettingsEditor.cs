// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEditorInternal;


namespace UnityEditor.TextCore.Text
{
    [CustomEditor(typeof(TextSettings))]
    public class TextSettingsEditor : Editor
    {
        internal class Styles
        {
            public static readonly GUIContent defaultFontAssetLabel = new GUIContent("Default Font Asset", "The Font Asset that will be assigned by default to newly created text objects when no Font Asset is specified.");
            public static readonly GUIContent defaultFontAssetPathLabel = new GUIContent("Path:        Resources/", "The relative path to a Resources folder where the Font Assets and Material Presets are located.\nExample \"Fonts & Materials/\"");

            public static readonly GUIContent fallbackFontAssetsLabel = new GUIContent("Font Assets Fallback", "The Font Assets that will be searched recursively to locate characters missing from the current font asset.");
            public static readonly GUIContent fallbackFontAssetsListLabel = new GUIContent("Font Assets Fallback List", "The Font Assets that will be searched recursively to locate characters missing from the current font asset.");

            public static readonly GUIContent fallbackMaterialSettingsLabel = new GUIContent("Fallback Material Settings");
            public static readonly GUIContent matchMaterialPresetLabel = new GUIContent("Match Material Presets");

            public static readonly GUIContent containerDefaultSettingsLabel = new GUIContent("Text Container Default Settings");

            public static readonly GUIContent textMeshProLabel = new GUIContent("TextMeshPro");
            public static readonly GUIContent textMeshProUiLabel = new GUIContent("TextMeshPro UI");
            public static readonly GUIContent enableRaycastTarget = new GUIContent("Enable Raycast Target");
            public static readonly GUIContent autoSizeContainerLabel = new GUIContent("Auto Size Text Container", "Set the size of the text container to match the text.");

            public static readonly GUIContent textComponentDefaultSettingsLabel = new GUIContent("Text Component Default Settings");
            public static readonly GUIContent defaultFontSize = new GUIContent("Default Font Size");
            public static readonly GUIContent autoSizeRatioLabel = new GUIContent("Text Auto Size Ratios");
            public static readonly GUIContent minLabel = new GUIContent("Min");
            public static readonly GUIContent maxLabel = new GUIContent("Max");

            public static readonly GUIContent textWrappingModeLabel = new GUIContent("Text Wrapping Mode");
            public static readonly GUIContent kerningLabel = new GUIContent("Kerning");
            public static readonly GUIContent extraPaddingLabel = new GUIContent("Extra Padding");
            public static readonly GUIContent tintAllSpritesLabel = new GUIContent("Tint All Sprites");
            public static readonly GUIContent parseEscapeCharactersLabel = new GUIContent("Parse Escape Sequence");

            public static readonly GUIContent dynamicFontSystemSettingsLabel = new GUIContent("Dynamic Font System Settings");
            public static readonly GUIContent getFontFeaturesAtRuntime = new GUIContent("Get Font Features at Runtime", "Determines if Glyph Adjustment Data will be retrieved from font files at runtime when new characters and glyphs are added to font assets.");
            public static readonly GUIContent dynamicAtlasTextureGroup = new GUIContent("Dynamic Atlas Texture Group");

            public static readonly GUIContent missingGlyphLabel = new GUIContent("Missing Character Unicode", "The character to be displayed when the requested character is not found in any font asset or fallbacks.");
            public static readonly GUIContent clearDynamicDataOnBuildLabel = new GUIContent("Clear Dynamic Data On Build", "Determines if the \"Clear Dynamic Data on Build\" property will be set to true or false on newly created dynamic font assets.");
            public static readonly GUIContent disableWarningsLabel = new GUIContent("Disable warnings", "Disable warning messages in the Console.");

            public static readonly GUIContent defaultSpriteAssetLabel = new GUIContent("Default Sprite Asset", "The Sprite Asset that will be assigned by default when using the <sprite> tag when no Sprite Asset is specified.");

            public static readonly GUIContent fallbackSpriteAssetsLabel = new GUIContent("Sprite Asset Fallback", "The Sprite Assets that will be searched recursively to locate sprite characters missing from the current sprite asset.");
            public static readonly GUIContent fallbackSpriteAssetsListLabel = new GUIContent("Sprite Asset Fallback List", "The Sprite Assets that will be searched recursively to locate sprite characters missing from the current sprite asset.");

            public static readonly GUIContent missingSpriteCharacterUnicodeLabel = new GUIContent("Missing Sprite Unicode", "The unicode value for the sprite character to be displayed when the requested sprite character is not found in any sprite assets or fallbacks.");
            public static readonly GUIContent enableEmojiSupportLabel = new GUIContent("iOS Emoji Support", "Enables Emoji support for Touch Screen Keyboards on target devices.");
            //public static readonly GUIContent spriteRelativeScale = new GUIContent("Relative Scaling", "Determines if the sprites will be scaled relative to the primary font asset assigned to the text object or relative to the current font asset.");

            public static readonly GUIContent spriteAssetsPathLabel = new GUIContent("Path:        Resources/", "The relative path to a Resources folder where the Sprite Assets are located.\nExample \"Sprite Assets/\"");

            public static readonly GUIContent defaultStyleSheetLabel = new GUIContent("Default Style Sheet", "The Style Sheet that will be used for all text objects in this project.");
            public static readonly GUIContent styleSheetResourcePathLabel = new GUIContent("Path:        Resources/", "The relative path to a Resources folder where the Style Sheets are located.\nExample \"Style Sheets/\"");

            public static readonly GUIContent colorGradientPresetsLabel = new GUIContent("Color Gradient Presets", "The relative path to a Resources folder where the Color Gradient Presets are located.\nExample \"Color Gradient Presets/\"");
            public static readonly GUIContent colorGradientsPathLabel = new GUIContent("Path:        Resources/", "The relative path to a Resources folder where the Color Gradient Presets are located.\nExample \"Color Gradient Presets/\"");

            public static readonly GUIContent lineBreakingLabel = new GUIContent("Line Breaking for Asian languages", "The text assets that contain the Leading and Following characters which define the rules for line breaking with Asian languages.");
            public static readonly GUIContent koreanSpecificRules = new GUIContent("Korean Language Options");
        }

        SerializedProperty m_PropFontAsset;
        SerializedProperty m_PropDefaultFontAssetPath;
        ReorderableList m_FontAssetFallbackList;
        SerializedProperty m_PropDefaultFontSize;
        SerializedProperty m_PropDefaultAutoSizeMinRatio;
        SerializedProperty m_PropDefaultAutoSizeMaxRatio;
        SerializedProperty m_PropDefaultTextMeshProTextContainerSize;
        SerializedProperty m_PropDefaultTextMeshProUITextContainerSize;
        SerializedProperty m_PropAutoSizeTextContainer;
        SerializedProperty m_PropEnableRaycastTarget;

        SerializedProperty m_PropSpriteAsset;
        SerializedProperty m_PropSpriteAssetPath;
        ReorderableList m_SpriteAssetFallbackList;
        SerializedProperty m_PropMissingSpriteCharacterUnicode;
        //SerializedProperty m_PropSpriteRelativeScaling;
        SerializedProperty m_PropEnableEmojiSupport;

        SerializedProperty m_PropStyleSheet;
        SerializedProperty m_PropStyleSheetsResourcePath;

        SerializedProperty m_PropColorGradientPresetsPath;

        SerializedProperty m_PropMatchMaterialPreset;
        SerializedProperty m_PropTextWrappingMode;
        SerializedProperty m_PropKerning;
        SerializedProperty m_PropExtraPadding;
        SerializedProperty m_PropTintAllSprites;
        SerializedProperty m_PropParseEscapeCharacters;
        SerializedProperty m_PropMissingGlyphCharacter;
        SerializedProperty m_PropClearDynamicDataOnBuild;

        //SerializedProperty m_DynamicAtlasTextureManager;
        SerializedProperty m_GetFontFeaturesAtRuntime;

        SerializedProperty m_PropDisplayWarnings;

        SerializedProperty m_PropUnicodeLineBreakingRules;
        //SerializedProperty m_PropLeadingCharacters;
        //SerializedProperty m_PropFollowingCharacters;
        //SerializedProperty m_PropUseModernHangulLineBreakingRules;

        private const string k_UndoRedo = "UndoRedoPerformed";

        public void OnEnable()
        {
            if (target == null)
                return;

            m_PropFontAsset = serializedObject.FindProperty("m_DefaultFontAsset");
            m_PropDefaultFontAssetPath = serializedObject.FindProperty("m_DefaultFontAssetPath");

            m_FontAssetFallbackList = new ReorderableList(serializedObject, serializedObject.FindProperty("m_FallbackFontAssets"), true, true, true, true);
            m_FontAssetFallbackList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = m_FontAssetFallbackList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            };

            m_FontAssetFallbackList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, Styles.fallbackFontAssetsListLabel);
            };

            m_PropDefaultFontSize = serializedObject.FindProperty("m_DefaultFontSize");
            m_PropDefaultAutoSizeMinRatio = serializedObject.FindProperty("m_defaultAutoSizeMinRatio");
            m_PropDefaultAutoSizeMaxRatio = serializedObject.FindProperty("m_defaultAutoSizeMaxRatio");
            m_PropDefaultTextMeshProTextContainerSize = serializedObject.FindProperty("m_defaultTextMeshProTextContainerSize");
            m_PropDefaultTextMeshProUITextContainerSize = serializedObject.FindProperty("m_defaultTextMeshProUITextContainerSize");
            m_PropAutoSizeTextContainer = serializedObject.FindProperty("m_autoSizeTextContainer");
            m_PropEnableRaycastTarget = serializedObject.FindProperty("m_EnableRaycastTarget");

            m_PropSpriteAsset = serializedObject.FindProperty("m_DefaultSpriteAsset");

            m_SpriteAssetFallbackList = new ReorderableList(serializedObject, serializedObject.FindProperty("m_FallbackSpriteAssets"), true, true, true, true);
            m_SpriteAssetFallbackList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = m_SpriteAssetFallbackList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            };

            m_SpriteAssetFallbackList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, Styles.fallbackSpriteAssetsListLabel);
            };

            m_PropMissingSpriteCharacterUnicode = serializedObject.FindProperty("m_MissingSpriteCharacterUnicode");
            //m_PropSpriteRelativeScaling = serializedObject.FindProperty("m_SpriteRelativeScaling");
            m_PropEnableEmojiSupport = serializedObject.FindProperty("m_enableEmojiSupport");
            m_PropSpriteAssetPath = serializedObject.FindProperty("m_DefaultSpriteAssetPath");

            m_PropStyleSheet = serializedObject.FindProperty("m_DefaultStyleSheet");
            m_PropStyleSheetsResourcePath = serializedObject.FindProperty("m_StyleSheetsResourcePath");


            m_PropColorGradientPresetsPath = serializedObject.FindProperty("m_DefaultColorGradientPresetsPath");


            m_PropMatchMaterialPreset = serializedObject.FindProperty("m_MatchMaterialPreset");

            m_PropTextWrappingMode = serializedObject.FindProperty("m_TextWrappingMode");
            m_PropKerning = serializedObject.FindProperty("m_enableKerning");
            m_PropExtraPadding = serializedObject.FindProperty("m_enableExtraPadding");
            m_PropTintAllSprites = serializedObject.FindProperty("m_enableTintAllSprites");
            m_PropParseEscapeCharacters = serializedObject.FindProperty("m_enableParseEscapeCharacters");
            m_PropMissingGlyphCharacter = serializedObject.FindProperty("m_MissingCharacterUnicode");
            m_PropClearDynamicDataOnBuild = serializedObject.FindProperty("m_ClearDynamicDataOnBuild");

            m_PropDisplayWarnings = serializedObject.FindProperty("m_DisplayWarnings");

            //m_DynamicAtlasTextureManager = serializedObject.FindProperty("m_DynamicAtlasTextureGroup");
            m_GetFontFeaturesAtRuntime = serializedObject.FindProperty("m_GetFontFeaturesAtRuntime");

            m_PropUnicodeLineBreakingRules = serializedObject.FindProperty("m_UnicodeLineBreakingRules");
            //m_PropLeadingCharacters = m_PropUnicodeLineBreakingRules.FindPropertyRelative("m_LeadingCharacters");
            //m_PropFollowingCharacters = m_PropUnicodeLineBreakingRules.FindPropertyRelative("m_FollowingCharacters");
            //m_PropUseModernHangulLineBreakingRules = m_PropUnicodeLineBreakingRules.FindPropertyRelative("m_UseModernHangulLineBreakingRules");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            string evt_cmd = Event.current.commandName;

            float labelWidth = EditorGUIUtility.labelWidth;
            float fieldWidth = EditorGUIUtility.fieldWidth;

            // TextMeshPro Font Info Panel
            EditorGUI.indentLevel = 0;

            // FONT ASSET
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.defaultFontAssetLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropFontAsset, Styles.defaultFontAssetLabel);
            EditorGUILayout.PropertyField(m_PropDefaultFontAssetPath, Styles.defaultFontAssetPathLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // FONT ASSET FALLBACK(s)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.fallbackFontAssetsLabel, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            m_FontAssetFallbackList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
            {
                TextResourceManager.RebuildFontAssetCache();
            }

            GUILayout.Label(Styles.fallbackMaterialSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropMatchMaterialPreset, Styles.matchMaterialPresetLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // MISSING GLYPHS
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.dynamicFontSystemSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            //EditorGUILayout.PropertyField(m_GetFontFeaturesAtRuntime, Styles.getFontFeaturesAtRuntime);
            EditorGUILayout.PropertyField(m_PropMissingGlyphCharacter, Styles.missingGlyphLabel);
            EditorGUILayout.PropertyField(m_PropClearDynamicDataOnBuild, Styles.clearDynamicDataOnBuildLabel);
            EditorGUILayout.PropertyField(m_PropDisplayWarnings, Styles.disableWarningsLabel);
            //EditorGUILayout.PropertyField(m_DynamicAtlasTextureManager, Styles.dynamicAtlasTextureManager);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // TEXT OBJECT DEFAULT PROPERTIES
            /*
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.containerDefaultSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;

            EditorGUILayout.PropertyField(m_PropDefaultTextMeshProTextContainerSize, Styles.textMeshProLabel);
            EditorGUILayout.PropertyField(m_PropDefaultTextMeshProUITextContainerSize, Styles.textMeshProUiLabel);
            EditorGUILayout.PropertyField(m_PropEnableRaycastTarget, Styles.enableRaycastTarget);
            EditorGUILayout.PropertyField(m_PropAutoSizeTextContainer, Styles.autoSizeContainerLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();

            GUILayout.Label(Styles.textComponentDefaultSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropDefaultFontSize, Styles.defaultFontSize);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel(Styles.autoSizeRatioLabel);
                EditorGUIUtility.labelWidth = 32;
                EditorGUIUtility.fieldWidth = 10;

                EditorGUI.indentLevel = 0;
                EditorGUILayout.PropertyField(m_PropDefaultAutoSizeMinRatio, Styles.minLabel);
                EditorGUILayout.PropertyField(m_PropDefaultAutoSizeMaxRatio, Styles.maxLabel);
                EditorGUI.indentLevel = 1;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;

            EditorGUILayout.PropertyField(m_PropWordWrapping, Styles.wordWrappingLabel);
            EditorGUILayout.PropertyField(m_PropKerning, Styles.kerningLabel);

            EditorGUILayout.PropertyField(m_PropExtraPadding, Styles.extraPaddingLabel);
            EditorGUILayout.PropertyField(m_PropTintAllSprites, Styles.tintAllSpritesLabel);

            EditorGUILayout.PropertyField(m_PropParseEscapeCharacters, Styles.parseEscapeCharactersLabel);

            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            */

            // SPRITE ASSET
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.defaultSpriteAssetLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropSpriteAsset, Styles.defaultSpriteAssetLabel);
            EditorGUILayout.PropertyField(m_PropMissingSpriteCharacterUnicode, Styles.missingSpriteCharacterUnicodeLabel);
            //EditorGUILayout.PropertyField(m_PropEnableEmojiSupport, Styles.enableEmojiSupportLabel);
            //EditorGUILayout.PropertyField(m_PropSpriteRelativeScaling, Styles.spriteRelativeScale);
            EditorGUILayout.PropertyField(m_PropSpriteAssetPath, Styles.spriteAssetsPathLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // SPRITE ASSET FALLBACK(s)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.fallbackSpriteAssetsLabel, EditorStyles.boldLabel);
            m_SpriteAssetFallbackList.DoLayoutList();

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // STYLE SHEET
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.defaultStyleSheetLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_PropStyleSheet, Styles.defaultStyleSheetLabel);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                TextStyleSheet styleSheet = m_PropStyleSheet.objectReferenceValue as TextStyleSheet;
                if (styleSheet != null)
                    styleSheet.RefreshStyles();
            }
            EditorGUILayout.PropertyField(m_PropStyleSheetsResourcePath, Styles.styleSheetResourcePathLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // COLOR GRADIENT PRESETS
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.colorGradientPresetsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropColorGradientPresetsPath, Styles.colorGradientsPathLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // LINE BREAKING RULE
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.lineBreakingLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropUnicodeLineBreakingRules);
            //EditorGUILayout.Space();
            //GUILayout.Label(Styles.koreanSpecificRules, EditorStyles.boldLabel);
            //EditorGUILayout.PropertyField(m_PropUseModernHangulLineBreakingRules, new GUIContent("Use Modern Line Breaking", "Determines if traditional or modern line breaking rules will be used to control line breaking. Traditional line breaking rules use the Leading and Following Character rules whereas Modern uses spaces for line breaking."));

            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            if (serializedObject.ApplyModifiedProperties() || evt_cmd == k_UndoRedo)
            {
                EditorUtility.SetDirty(target);
                TextEventManager.ON_TMP_SETTINGS_CHANGED();
            }
        }
    }
}
