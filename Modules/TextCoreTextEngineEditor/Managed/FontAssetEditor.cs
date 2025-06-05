// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEngine.TextCore.Text;
using UnityEngine.TextCore.LowLevel;
using UnityEditor.TextCore.LowLevel;

using Glyph = UnityEngine.TextCore.Glyph;
using GlyphRect = UnityEngine.TextCore.GlyphRect;
using GlyphMetrics = UnityEngine.TextCore.GlyphMetrics;


namespace UnityEditor.TextCore.Text
{
    [CustomPropertyDrawer(typeof(FontWeightPair))]
    internal class FontWeightDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty prop_regular = property.FindPropertyRelative("regularTypeface");
            SerializedProperty prop_italic = property.FindPropertyRelative("italicTypeface");

            float width = position.width;

            position.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(position, label);

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // NORMAL TYPEFACE
            if (label.text[0] == '4') GUI.enabled = false;
            position.x += position.width; position.width = (width - position.width) / 2;
            EditorGUI.PropertyField(position, prop_regular, GUIContent.none);

            // ITALIC TYPEFACE
            GUI.enabled = true;
            position.x += position.width;
            EditorGUI.PropertyField(position, prop_italic, GUIContent.none);

            EditorGUI.indentLevel = oldIndent;
        }
    }

    [CustomEditor(typeof(FontAsset))]
    internal class FontAssetEditor : Editor
    {
        internal struct UI_PanelState
        {
            public static bool generationSettingsPanel = true;
            public static bool fontAtlasInfoPanel = true;
            public static bool fontWeightPanel = true;
            public static bool fallbackFontAssetPanel = true;
            public static bool glyphTablePanel = false;
            public static bool characterTablePanel = false;
            public static bool LigatureSubstitutionTablePanel;
            public static bool PairAdjustmentTablePanel = false;
            public static bool MarkToBaseTablePanel = false;
            public static bool MarkToMarkTablePanel = false;
        }

        internal struct GenerationSettings
        {
            public Font sourceFont;
            public int faceIndex;
            public GlyphRenderMode glyphRenderMode;
            public float pointSize;
            public int padding;
            public int atlasWidth;
            public int atlasHeight;
        }

        /// <summary>
        /// Material used to display SDF glyphs in the Character and Glyph tables.
        /// </summary>
        internal static Material internalSDFMaterial
        {
            get
            {
                if (s_InternalSDFMaterial == null)
                {
                    Shader shader = TextShaderUtilities.ShaderRef_MobileSDF;

                    if (shader != null)
                        s_InternalSDFMaterial = new Material(shader);
                }

                return s_InternalSDFMaterial;
            }
        }
        static Material s_InternalSDFMaterial;

        /// <summary>
        /// Material used to display Bitmap glyphs in the Character and Glyph tables.
        /// </summary>
        internal static Material internalBitmapMaterial
        {
            get
            {
                if (s_InternalBitmapMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/Internal-GUITextureClipText");

                    if (shader != null)
                        s_InternalBitmapMaterial = new Material(shader);
                }

                return s_InternalBitmapMaterial;
            }
        }
        static Material s_InternalBitmapMaterial;

        /// <summary>
        /// Material used to display color glyphs in the Character and Glyph tables.
        /// </summary>
        internal static Material internalRGBABitmapMaterial
        {
            get
            {
                if (s_Internal_Bitmap_RGBA_Material == null)
                {
                    Shader shader = Shader.Find("Hidden/Internal-GUITextureClip");

                    if (shader != null)
                        s_Internal_Bitmap_RGBA_Material = new Material(shader);
                }

                return s_Internal_Bitmap_RGBA_Material;
            }
        }
        static Material s_Internal_Bitmap_RGBA_Material;



        private static string[] s_UiStateLabel = new string[] { "<i>(Click to collapse)</i> ", "<i>(Click to expand)</i> " };
        public static readonly GUIContent getFontFeaturesLabel = new GUIContent("Get Font Features", "Determines if OpenType font features should be retrieved from the source font file as new characters and glyphs are added to the font asset.");
        private GUIContent[] m_AtlasResolutionLabels = { new GUIContent("8"), new GUIContent("16"), new GUIContent("32"), new GUIContent("64"), new GUIContent("128"), new GUIContent("256"), new GUIContent("512"), new GUIContent("1024"), new GUIContent("2048"), new GUIContent("4096"), new GUIContent("8192") };
        private int[] m_AtlasResolutions = { 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };

        private struct Warning
        {
            public bool isEnabled;
            public double expirationTime;
        }

        private int m_CurrentGlyphPage = 0;
        private int m_CurrentCharacterPage = 0;
        private int m_CurrentLigaturePage = 0;
        private int m_CurrentAdjustmentPairPage = 0;
        private int m_CurrentMarkToBasePage = 0;
        private int m_CurrentMarkToMarkPage = 0;

        internal int m_SelectedGlyphRecord = -1;
        internal int m_SelectedCharacterRecord = -1;
        internal int m_SelectedLigatureRecord = -1;
        internal int m_SelectedAdjustmentRecord = -1;
        internal int m_SelectedMarkToBaseRecord = -1;
        internal int m_SelectedMarkToMarkRecord = -1;

        enum RecordSelectionType { CharacterRecord, GlyphRecord, LigatureSubstitutionRecord, AdjustmentPairRecord, MarkToBaseRecord, MarkToMarkRecord }

        private string m_dstGlyphID;
        private string m_dstUnicode;
        private const string k_placeholderUnicodeHex = "<i>New Unicode (Hex)</i>";
        private string m_unicodeHexLabel = k_placeholderUnicodeHex;
        private const string k_placeholderGlyphID = "<i>New Glyph ID</i>";
        private string m_GlyphIDLabel = k_placeholderGlyphID;

        private Warning m_AddGlyphWarning;
        private Warning m_AddCharacterWarning;
        private bool m_DisplayDestructiveChangeWarning;
        private GenerationSettings m_GenerationSettings;
        private bool m_MaterialPresetsRequireUpdate;

        private static readonly string[] k_InvalidFontFaces = { string.Empty };
        private string[] m_FontFaces;
        private bool m_FaceInfoDirty;

        private string m_GlyphSearchPattern;
        private List<int> m_GlyphSearchList;

        private string m_LigatureTableSearchPattern;
        private List<int> m_LigatureTableSearchList;

        private string m_CharacterSearchPattern;
        private List<int> m_CharacterSearchList;

        private string m_KerningTableSearchPattern;
        private List<int> m_KerningTableSearchList;

        private string m_MarkToBaseTableSearchPattern;
        private List<int> m_MarkToBaseTableSearchList;

        private string m_MarkToMarkTableSearchPattern;
        private List<int> m_MarkToMarkTableSearchList;

        private HashSet<uint> m_GlyphsToAdd;

        private bool m_isSearchDirty;

        private const string k_UndoRedo = "UndoRedoPerformed";

        private SerializedProperty m_AtlasPopulationMode_prop;
        private SerializedProperty font_atlas_prop;
        private SerializedProperty font_material_prop;

        private SerializedProperty m_FontFaceIndex_prop;
        private SerializedProperty m_AtlasRenderMode_prop;
        private SerializedProperty m_SamplingPointSize_prop;
        private SerializedProperty m_AtlasPadding_prop;
        private SerializedProperty m_AtlasWidth_prop;
        private SerializedProperty m_AtlasHeight_prop;
        private SerializedProperty m_IsMultiAtlasTexturesEnabled_prop;
        private SerializedProperty m_ClearDynamicDataOnBuild_prop;
        private SerializedProperty m_GetFontFeatures_prop;

        private SerializedProperty fontWeights_prop;

        //private SerializedProperty fallbackFontAssets_prop;
        private ReorderableList m_FallbackFontAssetList;

        private SerializedProperty font_normalStyle_prop;
        private SerializedProperty font_normalSpacing_prop;

        private SerializedProperty font_boldStyle_prop;
        private SerializedProperty font_boldSpacing_prop;

        private SerializedProperty font_italicStyle_prop;
        private SerializedProperty font_tabSize_prop;

        private SerializedProperty m_FaceInfo_prop;
        private SerializedProperty m_GlyphTable_prop;
        private SerializedProperty m_CharacterTable_prop;

        private FontFeatureTable m_FontFeatureTable;
        private SerializedProperty m_FontFeatureTable_prop;
        private SerializedProperty m_GlyphPairAdjustmentRecords_prop;
        private SerializedProperty m_LigatureSubstitutionRecords_prop;
        private SerializedProperty m_MarkToBaseAdjustmentRecords_prop;
        private SerializedProperty m_MarkToMarkAdjustmentRecords_prop;

        private SerializedPropertyHolder m_SerializedPropertyHolder;
        private SerializedProperty m_EmptyGlyphPairAdjustmentRecord_prop;
        private SerializedProperty m_FirstCharacterUnicode_prop;
        private SerializedProperty m_SecondCharacterUnicode_prop;


        // private string m_SecondCharacter;
        // private uint m_SecondGlyphIndex;

        private FontAsset m_fontAsset;

        private Material[] m_materialPresets;

        private bool isAssetDirty = false;
        private bool m_IsFallbackGlyphCacheDirty;

        private int errorCode;

        private System.DateTime timeStamp;

        public void OnEnable()
        {
            m_FaceInfo_prop = serializedObject.FindProperty("m_FaceInfo");

            font_atlas_prop = serializedObject.FindProperty("m_AtlasTextures").GetArrayElementAtIndex(0);
            font_material_prop = serializedObject.FindProperty("m_Material");

            m_FontFaceIndex_prop = m_FaceInfo_prop.FindPropertyRelative("m_FaceIndex");
            m_AtlasPopulationMode_prop = serializedObject.FindProperty("m_AtlasPopulationMode");
            m_AtlasRenderMode_prop = serializedObject.FindProperty("m_AtlasRenderMode");
            m_SamplingPointSize_prop = m_FaceInfo_prop.FindPropertyRelative("m_PointSize");
            m_AtlasPadding_prop = serializedObject.FindProperty("m_AtlasPadding");
            m_AtlasWidth_prop = serializedObject.FindProperty("m_AtlasWidth");
            m_AtlasHeight_prop = serializedObject.FindProperty("m_AtlasHeight");
            m_IsMultiAtlasTexturesEnabled_prop = serializedObject.FindProperty("m_IsMultiAtlasTexturesEnabled");
            m_ClearDynamicDataOnBuild_prop = serializedObject.FindProperty("m_ClearDynamicDataOnBuild");
            m_GetFontFeatures_prop = serializedObject.FindProperty("m_GetFontFeatures");

            fontWeights_prop = serializedObject.FindProperty("m_FontWeightTable");

            m_FallbackFontAssetList = PrepareReorderableList(serializedObject.FindProperty("m_FallbackFontAssetTable"), "Fallback Font Assets");

            // Clean up fallback list in the event if contains null elements.
            CleanFallbackFontAssetTable();

            font_normalStyle_prop = serializedObject.FindProperty("m_RegularStyleWeight");
            font_normalSpacing_prop = serializedObject.FindProperty("m_RegularStyleSpacing");

            font_boldStyle_prop = serializedObject.FindProperty("m_BoldStyleWeight");
            font_boldSpacing_prop = serializedObject.FindProperty("m_BoldStyleSpacing");

            font_italicStyle_prop = serializedObject.FindProperty("m_ItalicStyleSlant");
            font_tabSize_prop = serializedObject.FindProperty("m_TabMultiple");

            m_CharacterTable_prop = serializedObject.FindProperty("m_CharacterTable");
            m_GlyphTable_prop = serializedObject.FindProperty("m_GlyphTable");

            m_FontFeatureTable_prop = serializedObject.FindProperty("m_FontFeatureTable");
            m_LigatureSubstitutionRecords_prop = m_FontFeatureTable_prop.FindPropertyRelative("m_LigatureSubstitutionRecords");
            m_GlyphPairAdjustmentRecords_prop = m_FontFeatureTable_prop.FindPropertyRelative("m_GlyphPairAdjustmentRecords");
            m_MarkToBaseAdjustmentRecords_prop = m_FontFeatureTable_prop.FindPropertyRelative("m_MarkToBaseAdjustmentRecords");
            m_MarkToMarkAdjustmentRecords_prop = m_FontFeatureTable_prop.FindPropertyRelative("m_MarkToMarkAdjustmentRecords");

            m_fontAsset = target as FontAsset;
            m_FontFeatureTable = m_fontAsset.fontFeatureTable;

            // Get Font Faces and Styles
            m_FontFaces = GetFontFaces();

            // Create serialized object to allow us to use a serialized property of an empty kerning pair.
            m_SerializedPropertyHolder = CreateInstance<SerializedPropertyHolder>();
            m_SerializedPropertyHolder.fontAsset = m_fontAsset;
            SerializedObject internalSerializedObject = new SerializedObject(m_SerializedPropertyHolder);
            m_FirstCharacterUnicode_prop = internalSerializedObject.FindProperty("firstCharacter");
            m_SecondCharacterUnicode_prop = internalSerializedObject.FindProperty("secondCharacter");
            m_EmptyGlyphPairAdjustmentRecord_prop = internalSerializedObject.FindProperty("glyphPairAdjustmentRecord");

            m_materialPresets = TextCoreEditorUtilities.FindMaterialReferences(m_fontAsset);

            m_GlyphSearchList = new List<int>();
            m_KerningTableSearchList = new List<int>();

            // Sort Font Asset Tables
            m_fontAsset.SortAllTables();

            // Clear glyph proxy lookups
            TextCorePropertyDrawerUtilities.ClearGlyphProxyLookups();
        }

        private ReorderableList PrepareReorderableList(SerializedProperty property, string label)
        {
            SerializedObject so = property.serializedObject;

            ReorderableList list = new ReorderableList(so, property, true, true, true, true);

            list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, label);
            };

            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            };

            list.onChangedCallback = itemList => { };

            return list;
        }


        public void OnDisable()
        {
            // Revert changes if user closes or changes selection without having made a choice.
            if (m_DisplayDestructiveChangeWarning)
            {
                m_DisplayDestructiveChangeWarning = false;
                RestoreGenerationSettings();
                GUIUtility.keyboardControl = 0;

                serializedObject.ApplyModifiedProperties();
            }
        }

        internal static Func<bool> IsAdvancedTextEnabled;

        public override void OnInspectorGUI()
        {
            //Debug.Log("OnInspectorGUI Called.");
            if (IsAdvancedTextEnabled.Invoke())
            {
                EditorGUILayout.HelpBox("Enabling the Advanced Text Generator restricts customization of font metrics and static font assets. Additionally, some properties are still in development and may not be available.", MessageType.Warning, true);
            }

            Event currentEvent = Event.current;

            serializedObject.Update();

            Rect rect = EditorGUILayout.GetControlRect(false, 24);
            float labelWidth = EditorGUIUtility.labelWidth;
            float fieldWidth = EditorGUIUtility.fieldWidth;

            // FACE INFO PANEL
            #region Face info
            GUI.Label(rect, new GUIContent("<b>Face Info</b> - v" + m_fontAsset.version), TM_EditorStyles.sectionHeader);

            rect.x += rect.width - 132f;
            rect.y += 2;
            rect.width = 130f;
            rect.height = 18f;
            if (GUI.Button(rect, new GUIContent("Update Atlas Texture")))
            {
                FontAssetCreatorWindow.ShowFontAtlasCreatorWindow(target as FontAsset);
            }

            EditorGUI.indentLevel = 1;
            GUI.enabled = false; // Lock UI

            // TODO : Consider creating a property drawer for these.
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_FamilyName"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_StyleName"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_PointSize"));

            GUI.enabled = true;

            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_Scale"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_LineHeight"));

            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_AscentLine"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_CapLine"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_MeanLine"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_Baseline"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_DescentLine"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_UnderlineOffset"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_UnderlineThickness"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_StrikethroughOffset"));
            //EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("strikethroughThickness"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_SuperscriptOffset"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_SuperscriptSize"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_SubscriptOffset"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_SubscriptSize"));
            EditorGUILayout.PropertyField(m_FaceInfo_prop.FindPropertyRelative("m_TabWidth"));
            // TODO : Add clamping for some of these values.
            //subSize_prop.floatValue = Mathf.Clamp(subSize_prop.floatValue, 0.25f, 1f);

            EditorGUILayout.Space();
            #endregion

            // GENERATION SETTINGS
            #region Generation Settings
            rect = EditorGUILayout.GetControlRect(false, 24);

            if (GUI.Button(rect, new GUIContent("<b>Generation Settings</b>"), TM_EditorStyles.sectionHeader))
                UI_PanelState.generationSettingsPanel = !UI_PanelState.generationSettingsPanel;

            GUI.Label(rect, (UI_PanelState.generationSettingsPanel ? "" : s_UiStateLabel[1]), TM_EditorStyles.rightLabel);

            if (UI_PanelState.generationSettingsPanel)
            {
                EditorGUI.indentLevel = 1;

                EditorGUI.BeginChangeCheck();
                Font sourceFont = (Font)EditorGUILayout.ObjectField("Source Font File", m_fontAsset.SourceFont_EditorRef, typeof(Font), false);
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateSourceFontFile(sourceFont);
                }

                EditorGUI.BeginDisabledGroup(sourceFont == null);
                {
                    EditorGUI.BeginChangeCheck();
                    m_FontFaceIndex_prop.intValue = EditorGUILayout.Popup(new GUIContent("Font Face"), m_FontFaceIndex_prop.intValue, m_FontFaces);
                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdateFontFaceIndex(m_FontFaceIndex_prop.intValue);
                    }

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_AtlasPopulationMode_prop, new GUIContent("Atlas Population Mode"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdateAtlasPopulationMode(m_AtlasPopulationMode_prop.intValue);
                    }

                    // Save state of atlas settings
                    if (m_DisplayDestructiveChangeWarning == false)
                    {
                        SavedGenerationSettings();
                        //Undo.RegisterCompleteObjectUndo(m_fontAsset, "Font Asset Changes");
                    }

                    EditorGUI.BeginDisabledGroup(m_AtlasPopulationMode_prop.intValue == (int)AtlasPopulationMode.Static);
                    {
                        EditorGUI.BeginChangeCheck();
                        // TODO: Switch shaders depending on GlyphRenderMode.
                        var glyphRenderValues = (GlyphRenderMode[])Enum.GetValues(typeof(GlyphRenderMode));
                        GlyphRenderMode currentValue = glyphRenderValues[m_AtlasRenderMode_prop.enumValueIndex];
                        GlyphRenderModeUI selectedUI = (GlyphRenderModeUI)currentValue;

                        selectedUI = (GlyphRenderModeUI)EditorGUILayout.EnumPopup("Render Mode", selectedUI);
                        GlyphRenderMode updatedValue = (GlyphRenderMode)selectedUI;
                        if (updatedValue != currentValue)
                        {
                            int updatedIndex = Array.IndexOf(glyphRenderValues, updatedValue);
                            m_AtlasRenderMode_prop.enumValueIndex = updatedIndex;
                            m_DisplayDestructiveChangeWarning = true;
                        }
                        EditorGUILayout.PropertyField(m_SamplingPointSize_prop, new GUIContent("Sampling Point Size"));
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_DisplayDestructiveChangeWarning = true;
                        }

                        // Changes to these properties require updating Material Presets for this font asset.
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(m_AtlasPadding_prop, new GUIContent("Padding"));
                        EditorGUILayout.IntPopup(m_AtlasWidth_prop, m_AtlasResolutionLabels, m_AtlasResolutions, new GUIContent("Atlas Width"));
                        EditorGUILayout.IntPopup(m_AtlasHeight_prop, m_AtlasResolutionLabels, m_AtlasResolutions, new GUIContent("Atlas Height"));
                        EditorGUILayout.PropertyField(m_IsMultiAtlasTexturesEnabled_prop, new GUIContent("Multi Atlas Textures", "Determines if the font asset will store glyphs in multiple atlas textures."));
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (m_AtlasPadding_prop.intValue < 0)
                            {
                                m_AtlasPadding_prop.intValue = 0;
                                serializedObject.ApplyModifiedProperties();
                            }

                            m_MaterialPresetsRequireUpdate = true;
                            m_DisplayDestructiveChangeWarning = true;
                        }
                        EditorGUILayout.PropertyField(m_ClearDynamicDataOnBuild_prop, new GUIContent("Clear Dynamic Data On Build", "Clears all dynamic data restoring the font asset back to its default creation and empty state."));
                        EditorGUILayout.PropertyField(m_GetFontFeatures_prop, getFontFeaturesLabel);

                        EditorGUILayout.Space();

                        if (m_DisplayDestructiveChangeWarning)
                        {
                            bool guiEnabledState = GUI.enabled;
                            GUI.enabled = true;

                            // These changes are destructive on the font asset
                            rect = EditorGUILayout.GetControlRect(false, 60);
                            rect.x += 15;
                            rect.width -= 15;
                            EditorGUI.HelpBox(rect, "Changing these settings will clear the font asset's character, glyph and texture data.", MessageType.Warning);

                            if (GUI.Button(new Rect(rect.width - 140, rect.y + 36, 80, 18), new GUIContent("Apply")))
                            {
                                ApplyDestructiveChanges();
                            }

                            if (GUI.Button(new Rect(rect.width - 56, rect.y + 36, 80, 18), new GUIContent("Revert")))
                            {
                                RevertDestructiveChanges();
                            }

                            GUI.enabled = guiEnabledState;
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Space();
            }
            #endregion

            // ATLAS & MATERIAL PANEL
            #region Atlas & Material
            rect = EditorGUILayout.GetControlRect(false, 24);

            if (GUI.Button(rect, new GUIContent("<b>Atlas & Material</b>"), TM_EditorStyles.sectionHeader))
                UI_PanelState.fontAtlasInfoPanel = !UI_PanelState.fontAtlasInfoPanel;

            GUI.Label(rect, (UI_PanelState.fontAtlasInfoPanel ? "" : s_UiStateLabel[1]), TM_EditorStyles.rightLabel);

            if (UI_PanelState.fontAtlasInfoPanel)
            {
                EditorGUI.indentLevel = 1;

                GUI.enabled = false;
                EditorGUILayout.PropertyField(font_atlas_prop, new GUIContent("Font Atlas"));
                EditorGUILayout.PropertyField(font_material_prop, new GUIContent("Font Material"));
                GUI.enabled = true;
                EditorGUILayout.Space();
            }
            #endregion

            string evt_cmd = Event.current.commandName; // Get Current Event CommandName to check for Undo Events

            // FONT WEIGHT PANEL
            #region Font Weights
            rect = EditorGUILayout.GetControlRect(false, 24);

            if (GUI.Button(rect, new GUIContent("<b>Font Weights</b>", "The Font Assets that will be used for different font weights and the settings used to simulate a typeface when no asset is available."), TM_EditorStyles.sectionHeader))
                UI_PanelState.fontWeightPanel = !UI_PanelState.fontWeightPanel;

            GUI.Label(rect, (UI_PanelState.fontWeightPanel ? "" : s_UiStateLabel[1]), TM_EditorStyles.rightLabel);

            if (UI_PanelState.fontWeightPanel)
            {
                EditorGUIUtility.labelWidth *= 0.75f;
                EditorGUIUtility.fieldWidth *= 0.25f;

                EditorGUILayout.BeginVertical();
                EditorGUI.indentLevel = 1;
                rect = EditorGUILayout.GetControlRect(true);
                rect.x += EditorGUIUtility.labelWidth;
                rect.width = (rect.width - EditorGUIUtility.labelWidth) / 2f;
                GUI.Label(rect, "Regular Typeface", EditorStyles.label);
                rect.x += rect.width;
                GUI.Label(rect, "Italic Typeface", EditorStyles.label);

                EditorGUI.indentLevel = 1;

                EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(1), new GUIContent("100 - Thin"));
                EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(2), new GUIContent("200 - Extra-Light"));
                EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(3), new GUIContent("300 - Light"));
                EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(4), new GUIContent("400 - Regular"));
                EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(5), new GUIContent("500 - Medium"));
                EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(6), new GUIContent("600 - Semi-Bold"));
                EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(7), new GUIContent("700 - Bold"));
                EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(8), new GUIContent("800 - Heavy"));
                EditorGUILayout.PropertyField(fontWeights_prop.GetArrayElementAtIndex(9), new GUIContent("900 - Black"));

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(font_normalStyle_prop, new GUIContent("Regular Weight"));
                font_normalStyle_prop.floatValue = Mathf.Clamp(font_normalStyle_prop.floatValue, -3.0f, 3.0f);
                if (GUI.changed || evt_cmd == k_UndoRedo)
                {
                    GUI.changed = false;

                    // Modify the material property on matching material presets.
                    for (int i = 0; i < m_materialPresets.Length; i++)
                        m_materialPresets[i].SetFloat("_WeightNormal", font_normalStyle_prop.floatValue);
                }

                EditorGUILayout.PropertyField(font_boldStyle_prop, new GUIContent("Bold Weight"));
                font_boldStyle_prop.floatValue = Mathf.Clamp(font_boldStyle_prop.floatValue, -3.0f, 3.0f);
                if (GUI.changed || evt_cmd == k_UndoRedo)
                {
                    GUI.changed = false;

                    // Modify the material property on matching material presets.
                    for (int i = 0; i < m_materialPresets.Length; i++)
                        m_materialPresets[i].SetFloat("_WeightBold", font_boldStyle_prop.floatValue);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(font_normalSpacing_prop, new GUIContent("Regular Spacing"));
                font_normalSpacing_prop.floatValue = Mathf.Clamp(font_normalSpacing_prop.floatValue, -100, 100);
                if (GUI.changed || evt_cmd == k_UndoRedo)
                {
                    GUI.changed = false;
                }

                EditorGUILayout.PropertyField(font_boldSpacing_prop, new GUIContent("Bold Spacing"));
                font_boldSpacing_prop.floatValue = Mathf.Clamp(font_boldSpacing_prop.floatValue, 0, 100);
                if (GUI.changed || evt_cmd == k_UndoRedo)
                {
                    GUI.changed = false;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(font_italicStyle_prop, new GUIContent("Italic Slant"));
                font_italicStyle_prop.intValue = Mathf.Clamp(font_italicStyle_prop.intValue, 15, 60);

                EditorGUILayout.PropertyField(font_tabSize_prop, new GUIContent("Tab Multiple"));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUIUtility.labelWidth = 0;
            EditorGUIUtility.fieldWidth = 0;
            #endregion

            // FALLBACK FONT ASSETS
            #region Fallback Font Asset
            rect = EditorGUILayout.GetControlRect(false, 24);
            EditorGUI.indentLevel = 0;
            if (GUI.Button(rect, new GUIContent("<b>Fallback Font Assets</b>", "Select the Font Assets that will be searched and used as fallback when characters are missing from this font asset."), TM_EditorStyles.sectionHeader))
                UI_PanelState.fallbackFontAssetPanel = !UI_PanelState.fallbackFontAssetPanel;

            GUI.Label(rect, (UI_PanelState.fallbackFontAssetPanel ? "" : s_UiStateLabel[1]), TM_EditorStyles.rightLabel);

            if (UI_PanelState.fallbackFontAssetPanel)
            {
                EditorGUIUtility.labelWidth = 120;
                EditorGUI.indentLevel = 0;
                EditorGUI.BeginChangeCheck();
                m_FallbackFontAssetList.DoLayoutList();
                if (EditorGUI.EndChangeCheck())
                {
                    m_IsFallbackGlyphCacheDirty = true;
                }
                EditorGUILayout.Space();
            }
            #endregion

            // CHARACTER TABLE TABLE
            #region Character Table
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
            EditorGUI.indentLevel = 0;
            rect = EditorGUILayout.GetControlRect(false, 24);

            int characterCount = m_fontAsset.characterTable.Count;

            if (GUI.Button(rect, new GUIContent("<b>Character Table</b>   [" + characterCount + "]" + (rect.width > 320 ? " Characters" : ""), "List of characters contained in this font asset."), TM_EditorStyles.sectionHeader))
                UI_PanelState.characterTablePanel = !UI_PanelState.characterTablePanel;

            GUI.Label(rect, (UI_PanelState.characterTablePanel ? "" : s_UiStateLabel[1]), TM_EditorStyles.rightLabel);

            if (UI_PanelState.characterTablePanel)
            {
                int arraySize = m_CharacterTable_prop.arraySize;
                int itemsPerPage = 15;

                // Display Glyph Management Tools
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // Search Bar implementation
                    #region DISPLAY SEARCH BAR
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUIUtility.labelWidth = 130f;
                        EditorGUI.BeginChangeCheck();
                        string searchPattern = EditorGUILayout.TextField("Character Search", m_CharacterSearchPattern, "SearchTextField");
                        if (EditorGUI.EndChangeCheck() || m_isSearchDirty)
                        {
                            if (string.IsNullOrEmpty(searchPattern) == false)
                            {
                                m_CharacterSearchPattern = searchPattern;

                                // Search Character Table for potential matches
                                SearchCharacterTable(m_CharacterSearchPattern, ref m_CharacterSearchList);
                            }
                            else
                                m_CharacterSearchPattern = null;

                            m_isSearchDirty = false;
                        }

                        string styleName = string.IsNullOrEmpty(m_CharacterSearchPattern) ? "SearchCancelButtonEmpty" : "SearchCancelButton";
                        if (GUILayout.Button(GUIContent.none, styleName))
                        {
                            GUIUtility.keyboardControl = 0;
                            m_CharacterSearchPattern = string.Empty;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    #endregion

                    // Display Page Navigation
                    if (!string.IsNullOrEmpty(m_CharacterSearchPattern))
                        arraySize = m_CharacterSearchList.Count;

                    DisplayPageNavigation(ref m_CurrentCharacterPage, arraySize, itemsPerPage);
                }
                EditorGUILayout.EndVertical();

                // Display Character Table Elements
                if (arraySize > 0)
                {
                    // Display each character entry using the CharacterPropertyDrawer.
                    for (int i = itemsPerPage * m_CurrentCharacterPage; i < arraySize && i < itemsPerPage * (m_CurrentCharacterPage + 1); i++)
                    {
                        // Define the start of the selection region of the element.
                        Rect elementStartRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        int elementIndex = i;
                        if (!string.IsNullOrEmpty(m_CharacterSearchPattern))
                            elementIndex = m_CharacterSearchList[i];

                        SerializedProperty characterProperty = m_CharacterTable_prop.GetArrayElementAtIndex(elementIndex);

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        EditorGUI.BeginDisabledGroup(i != m_SelectedCharacterRecord);
                        {
                            EditorGUILayout.PropertyField(characterProperty);
                        }
                        EditorGUI.EndDisabledGroup();

                        EditorGUILayout.EndVertical();

                        // Define the end of the selection region of the element.
                        Rect elementEndRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        // Check for Item selection
                        Rect selectionArea = new Rect(elementStartRegion.x, elementStartRegion.y, elementEndRegion.width, elementEndRegion.y - elementStartRegion.y);
                        if (DoSelectionCheck(selectionArea))
                        {
                            if (m_SelectedCharacterRecord == i)
                                m_SelectedCharacterRecord = -1;
                            else
                            {
                                m_SelectedCharacterRecord = i;
                                m_AddCharacterWarning.isEnabled = false;
                                m_unicodeHexLabel = k_placeholderUnicodeHex;
                                GUIUtility.keyboardControl = 0;
                            }
                        }

                        // Draw Selection Highlight and Glyph Options
                        if (m_SelectedCharacterRecord == i)
                        {
                            // Reset other selections
                            ResetSelections(RecordSelectionType.CharacterRecord);

                            TextCoreEditorUtilities.DrawBox(selectionArea, 2f, new Color32(40, 192, 255, 255));

                            // Draw Glyph management options
                            Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 1f);
                            float optionAreaWidth = controlRect.width * 0.6f;
                            float btnWidth = optionAreaWidth / 3;

                            Rect position = new Rect(controlRect.x + controlRect.width * .4f, controlRect.y, btnWidth, controlRect.height);

                            // Copy Selected Glyph to Target Glyph ID
                            GUI.enabled = !string.IsNullOrEmpty(m_dstUnicode);
                            if (GUI.Button(position, new GUIContent("Copy to")))
                            {
                                GUIUtility.keyboardControl = 0;

                                // Convert Hex Value to Decimal
                                int dstGlyphID = (int)TextUtilities.StringHexToInt(m_dstUnicode);

                                //Add new glyph at target Unicode hex id.
                                if (!AddNewCharacter(elementIndex, dstGlyphID))
                                {
                                    m_AddCharacterWarning.isEnabled = true;
                                    m_AddCharacterWarning.expirationTime = EditorApplication.timeSinceStartup + 1;
                                }

                                m_dstUnicode = string.Empty;
                                m_isSearchDirty = true;

                                TextEventManager.ON_FONT_PROPERTY_CHANGED(true, m_fontAsset);
                            }

                            // Target Glyph ID
                            GUI.enabled = true;
                            position.x += btnWidth;

                            GUI.SetNextControlName("CharacterID_Input");
                            m_dstUnicode = EditorGUI.TextField(position, m_dstUnicode);

                            // Placeholder text
                            EditorGUI.LabelField(position, new GUIContent(m_unicodeHexLabel, "The Unicode (Hex) ID of the duplicated Character"), TM_EditorStyles.label);

                            // Only filter the input when the destination glyph ID text field has focus.
                            if (GUI.GetNameOfFocusedControl() == "CharacterID_Input")
                            {
                                m_unicodeHexLabel = string.Empty;

                                //Filter out unwanted characters.
                                char chr = Event.current.character;
                                if ((chr < '0' || chr > '9') && (chr < 'a' || chr > 'f') && (chr < 'A' || chr > 'F'))
                                {
                                    Event.current.character = '\0';
                                }
                            }
                            else
                            {
                                m_unicodeHexLabel = k_placeholderUnicodeHex;
                                //m_dstUnicode = string.Empty;
                            }


                            // Remove Glyph
                            position.x += btnWidth;
                            if (GUI.Button(position, "Remove"))
                            {
                                GUIUtility.keyboardControl = 0;

                                RemoveCharacterFromList(elementIndex);

                                isAssetDirty = true;
                                m_SelectedCharacterRecord = -1;
                                m_isSearchDirty = true;
                                break;
                            }

                            if (m_AddCharacterWarning.isEnabled && EditorApplication.timeSinceStartup < m_AddCharacterWarning.expirationTime)
                            {
                                EditorGUILayout.HelpBox("The Destination Character ID already exists", MessageType.Warning);
                            }
                        }
                    }
                }

                DisplayPageNavigation(ref m_CurrentCharacterPage, arraySize, itemsPerPage);

                EditorGUILayout.Space();
            }
            #endregion

            // GLYPH TABLE
            #region Glyph Table
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
            EditorGUI.indentLevel = 0;
            rect = EditorGUILayout.GetControlRect(false, 24);

            GUIStyle glyphPanelStyle = new GUIStyle(EditorStyles.helpBox);

            int glyphCount = m_fontAsset.glyphTable.Count;

            if (GUI.Button(rect, new GUIContent("<b>Glyph Table</b>   [" + glyphCount + "]" + (rect.width > 275 ? " Glyphs" : ""), "List of glyphs contained in this font asset."), TM_EditorStyles.sectionHeader))
                UI_PanelState.glyphTablePanel = !UI_PanelState.glyphTablePanel;

            GUI.Label(rect, (UI_PanelState.glyphTablePanel ? "" : s_UiStateLabel[1]), TM_EditorStyles.rightLabel);

            if (UI_PanelState.glyphTablePanel)
            {
                int arraySize = m_GlyphTable_prop.arraySize;
                int itemsPerPage = 15;

                // Display Glyph Management Tools
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // Search Bar implementation
                    #region DISPLAY SEARCH BAR
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUIUtility.labelWidth = 130f;
                        EditorGUI.BeginChangeCheck();
                        string searchPattern = EditorGUILayout.TextField("Glyph Search", m_GlyphSearchPattern, "SearchTextField");
                        if (EditorGUI.EndChangeCheck() || m_isSearchDirty)
                        {
                            if (string.IsNullOrEmpty(searchPattern) == false)
                            {
                                m_GlyphSearchPattern = searchPattern;

                                // Search Glyph Table for potential matches
                                SearchGlyphTable(m_GlyphSearchPattern, ref m_GlyphSearchList);
                            }
                            else
                                m_GlyphSearchPattern = null;

                            m_isSearchDirty = false;
                        }

                        string styleName = string.IsNullOrEmpty(m_GlyphSearchPattern) ? "SearchCancelButtonEmpty" : "SearchCancelButton";
                        if (GUILayout.Button(GUIContent.none, styleName))
                        {
                            GUIUtility.keyboardControl = 0;
                            m_GlyphSearchPattern = string.Empty;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    #endregion

                    // Display Page Navigation
                    if (!string.IsNullOrEmpty(m_GlyphSearchPattern))
                        arraySize = m_GlyphSearchList.Count;

                    DisplayPageNavigation(ref m_CurrentGlyphPage, arraySize, itemsPerPage);
                }
                EditorGUILayout.EndVertical();

                // Display Glyph Table Elements

                if (arraySize > 0)
                {
                    // Display each GlyphInfo entry using the GlyphInfo property drawer.
                    for (int i = itemsPerPage * m_CurrentGlyphPage; i < arraySize && i < itemsPerPage * (m_CurrentGlyphPage + 1); i++)
                    {
                        // Define the start of the selection region of the element.
                        Rect elementStartRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        int elementIndex = i;
                        if (!string.IsNullOrEmpty(m_GlyphSearchPattern))
                            elementIndex = m_GlyphSearchList[i];

                        SerializedProperty glyphProperty = m_GlyphTable_prop.GetArrayElementAtIndex(elementIndex);

                        EditorGUILayout.BeginVertical(glyphPanelStyle);

                        using (new EditorGUI.DisabledScope(i != m_SelectedGlyphRecord))
                        {
                            EditorGUILayout.PropertyField(glyphProperty);
                        }

                        EditorGUILayout.EndVertical();

                        // Define the end of the selection region of the element.
                        Rect elementEndRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        // Check for Item selection
                        Rect selectionArea = new Rect(elementStartRegion.x, elementStartRegion.y, elementEndRegion.width, elementEndRegion.y - elementStartRegion.y);
                        if (DoSelectionCheck(selectionArea))
                        {
                            if (m_SelectedGlyphRecord == i)
                                m_SelectedGlyphRecord = -1;
                            else
                            {
                                m_SelectedGlyphRecord = i;
                                m_AddGlyphWarning.isEnabled = false;
                                m_unicodeHexLabel = k_placeholderUnicodeHex;
                                GUIUtility.keyboardControl = 0;
                            }
                        }

                        // Draw Selection Highlight and Glyph Options
                        if (m_SelectedGlyphRecord == i)
                        {
                            // Reset other selections
                            ResetSelections(RecordSelectionType.GlyphRecord);

                            TextCoreEditorUtilities.DrawBox(selectionArea, 2f, new Color32(40, 192, 255, 255));

                            // Draw Glyph management options
                            Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 1f);
                            float optionAreaWidth = controlRect.width * 0.6f;
                            float btnWidth = optionAreaWidth / 3;

                            Rect position = new Rect(controlRect.x + controlRect.width * .4f, controlRect.y, btnWidth, controlRect.height);

                            // Copy Selected Glyph to Target Glyph ID
                            GUI.enabled = !string.IsNullOrEmpty(m_dstGlyphID);
                            if (GUI.Button(position, new GUIContent("Copy to")))
                            {
                                GUIUtility.keyboardControl = 0;
                                int dstGlyphID;

                                // Convert Hex Value to Decimal
                                int.TryParse(m_dstGlyphID, out dstGlyphID);

                                //Add new glyph at target Unicode hex id.
                                if (!AddNewGlyph(elementIndex, dstGlyphID))
                                {
                                    m_AddGlyphWarning.isEnabled = true;
                                    m_AddGlyphWarning.expirationTime = EditorApplication.timeSinceStartup + 1;
                                }

                                m_dstGlyphID = string.Empty;
                                m_isSearchDirty = true;

                                TextEventManager.ON_FONT_PROPERTY_CHANGED(true, m_fontAsset);
                            }

                            // Target Glyph ID
                            GUI.enabled = true;
                            position.x += btnWidth;

                            GUI.SetNextControlName("GlyphID_Input");
                            m_dstGlyphID = EditorGUI.TextField(position, m_dstGlyphID);

                            // Placeholder text
                            EditorGUI.LabelField(position, new GUIContent(m_GlyphIDLabel, "The Glyph ID of the duplicated Glyph"), TM_EditorStyles.label);

                            // Only filter the input when the destination glyph ID text field has focus.
                            if (GUI.GetNameOfFocusedControl() == "GlyphID_Input")
                            {
                                m_GlyphIDLabel = string.Empty;

                                //Filter out unwanted characters.
                                char chr = Event.current.character;
                                if ((chr < '0' || chr > '9'))
                                {
                                    Event.current.character = '\0';
                                }
                            }
                            else
                            {
                                m_GlyphIDLabel = k_placeholderGlyphID;
                                //m_dstGlyphID = string.Empty;
                            }

                            // Remove Glyph
                            position.x += btnWidth;
                            if (GUI.Button(position, "Remove"))
                            {
                                GUIUtility.keyboardControl = 0;

                                RemoveGlyphFromList(elementIndex);

                                isAssetDirty = true;
                                m_SelectedGlyphRecord = -1;
                                m_isSearchDirty = true;
                                break;
                            }

                            if (m_AddGlyphWarning.isEnabled && EditorApplication.timeSinceStartup < m_AddGlyphWarning.expirationTime)
                            {
                                EditorGUILayout.HelpBox("The Destination Glyph ID already exists", MessageType.Warning);
                            }
                        }
                    }
                }

                //DisplayAddRemoveButtons(m_GlyphTable_prop, m_SelectedGlyphRecord, glyphRecordCount);

                DisplayPageNavigation(ref m_CurrentGlyphPage, arraySize, itemsPerPage);

                EditorGUILayout.Space();
            }
            #endregion

            // FONT FEATURE TABLES

            // LIGATURE SUBSTITUTION TABLE
            #region LIGATURE
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
            EditorGUI.indentLevel = 0;
            rect = EditorGUILayout.GetControlRect(false, 24);

            int ligatureSubstitutionRecordCount = m_fontAsset.fontFeatureTable.ligatureRecords.Count;

            if (GUI.Button(rect, new GUIContent("<b>Ligature Table</b>   [" + ligatureSubstitutionRecordCount + "]" + (rect.width > 340 ? " Records" : ""), "List of Ligature substitution records."), TM_EditorStyles.sectionHeader))
                UI_PanelState.LigatureSubstitutionTablePanel = !UI_PanelState.LigatureSubstitutionTablePanel;

            GUI.Label(rect, (UI_PanelState.LigatureSubstitutionTablePanel ? "" : s_UiStateLabel[1]), TM_EditorStyles.rightLabel);

            if (UI_PanelState.LigatureSubstitutionTablePanel)
            {
                int arraySize = m_LigatureSubstitutionRecords_prop.arraySize;
                int itemsPerPage = 20;

                // Display Mark Adjust Records Management Tools
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // Search Bar implementation
                    #region DISPLAY SEARCH BAR
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUIUtility.labelWidth = 150f;
                        EditorGUI.BeginChangeCheck();
                        string searchPattern = EditorGUILayout.TextField("Ligature Search", m_LigatureTableSearchPattern, "SearchTextField");
                        if (EditorGUI.EndChangeCheck() || m_isSearchDirty)
                        {
                            if (string.IsNullOrEmpty(searchPattern) == false)
                            {
                                m_LigatureTableSearchPattern = searchPattern;

                                // Search Glyph Table for potential matches
                                SearchLigatureTable(m_LigatureTableSearchPattern, ref m_LigatureTableSearchList);
                            }
                            else
                                m_LigatureTableSearchPattern = null;

                            m_isSearchDirty = false;
                        }

                        string styleName = string.IsNullOrEmpty(m_LigatureTableSearchPattern) ? "SearchCancelButtonEmpty" : "SearchCancelButton";
                        if (GUILayout.Button(GUIContent.none, styleName))
                        {
                            GUIUtility.keyboardControl = 0;
                            m_LigatureTableSearchPattern = string.Empty;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    #endregion

                    // Display Page Navigation
                    if (!string.IsNullOrEmpty(m_LigatureTableSearchPattern))
                        arraySize = m_LigatureTableSearchList.Count;

                    DisplayPageNavigation(ref m_CurrentLigaturePage, arraySize, itemsPerPage);
                }
                EditorGUILayout.EndVertical();

                if (arraySize > 0)
                {
                    // Display each GlyphInfo entry using the GlyphInfo property drawer.
                    for (int i = itemsPerPage * m_CurrentLigaturePage; i < arraySize && i < itemsPerPage * (m_CurrentLigaturePage + 1); i++)
                    {
                        // Define the start of the selection region of the element.
                        Rect elementStartRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        int elementIndex = i;
                        if (!string.IsNullOrEmpty(m_LigatureTableSearchPattern))
                            elementIndex = m_LigatureTableSearchList[i];

                        SerializedProperty ligaturePropertyRecord = m_LigatureSubstitutionRecords_prop.GetArrayElementAtIndex(elementIndex);

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        using (new EditorGUI.DisabledScope(i != m_SelectedLigatureRecord))
                        {
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(ligaturePropertyRecord, new GUIContent("Selectable"));

                            if (EditorGUI.EndChangeCheck())
                            {
                                UpdateLigatureSubstitutionRecordLookup(ligaturePropertyRecord);
                            }
                        }

                        EditorGUILayout.EndVertical();

                        // Define the end of the selection region of the element.
                        Rect elementEndRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        // Check for Item selection
                        Rect selectionArea = new Rect(elementStartRegion.x, elementStartRegion.y, elementEndRegion.width, elementEndRegion.y - elementStartRegion.y);
                        if (DoSelectionCheck(selectionArea))
                        {
                            if (m_SelectedLigatureRecord == i)
                            {
                                m_SelectedLigatureRecord = -1;
                            }
                            else
                            {
                                m_SelectedLigatureRecord = i;
                                GUIUtility.keyboardControl = 0;
                            }
                        }

                        // Draw Selection Highlight and Kerning Pair Options
                        if (m_SelectedLigatureRecord == i)
                        {
                            // Reset other selections
                            ResetSelections(RecordSelectionType.LigatureSubstitutionRecord);

                            TextCoreEditorUtilities.DrawBox(selectionArea, 2f, new Color32(40, 192, 255, 255));

                            // Draw Glyph management options
                            Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 1f);
                            float optionAreaWidth = controlRect.width;
                            float btnWidth = optionAreaWidth / 4;

                            Rect position = new Rect(controlRect.x + controlRect.width - btnWidth, controlRect.y, btnWidth, controlRect.height);

                            // Move record up
                            bool guiEnabled = GUI.enabled;
                            if (m_SelectedLigatureRecord == 0) { GUI.enabled = false; }
                            if (GUI.Button(new Rect(controlRect.x, controlRect.y, btnWidth, controlRect.height), "Up"))
                            {
                                SwapCharacterElements(m_LigatureSubstitutionRecords_prop, m_SelectedLigatureRecord, m_SelectedLigatureRecord - 1);
                                serializedObject.ApplyModifiedProperties();
                                m_SelectedLigatureRecord -= 1;
                                isAssetDirty = true;
                                m_isSearchDirty = true;
                                m_fontAsset.InitializeLigatureSubstitutionLookupDictionary();
                            }
                            GUI.enabled = guiEnabled;

                            // Move record down
                            if (m_SelectedLigatureRecord == arraySize - 1) { GUI.enabled = false; }
                            if (GUI.Button(new Rect(controlRect.x + btnWidth, controlRect.y, btnWidth, controlRect.height), "Down"))
                            {
                                SwapCharacterElements(m_LigatureSubstitutionRecords_prop, m_SelectedLigatureRecord, m_SelectedLigatureRecord + 1);
                                serializedObject.ApplyModifiedProperties();
                                m_SelectedLigatureRecord += 1;
                                isAssetDirty = true;
                                m_isSearchDirty = true;
                                m_fontAsset.InitializeLigatureSubstitutionLookupDictionary();
                            }
                            GUI.enabled = guiEnabled;

                            // Remove record
                            GUI.enabled = true;
                            if (GUI.Button(position, "Remove"))
                            {
                                GUIUtility.keyboardControl = 0;

                                RemoveRecord(m_LigatureSubstitutionRecords_prop, m_SelectedLigatureRecord);

                                isAssetDirty = true;
                                m_SelectedLigatureRecord = -1;
                                m_isSearchDirty = true;
                                break;
                            }
                        }
                    }
                }

                DisplayAddRemoveButtons(m_LigatureSubstitutionRecords_prop, m_SelectedLigatureRecord, ligatureSubstitutionRecordCount);

                DisplayPageNavigation(ref m_CurrentLigaturePage, arraySize, itemsPerPage);

                GUILayout.Space(5);
            }
            #endregion

            // PAIR ADJUSTMENT TABLE
            #region Pair Adjustment Table
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
            EditorGUI.indentLevel = 0;
            rect = EditorGUILayout.GetControlRect(false, 24);

            int adjustmentPairCount = m_fontAsset.fontFeatureTable.glyphPairAdjustmentRecords.Count;

            if (GUI.Button(rect, new GUIContent("<b>Glyph Adjustment Table</b>   [" + adjustmentPairCount + "]" + (rect.width > 340 ? " Records" : ""), "List of glyph adjustment / advanced kerning pairs."), TM_EditorStyles.sectionHeader))
                UI_PanelState.PairAdjustmentTablePanel = !UI_PanelState.PairAdjustmentTablePanel;

            GUI.Label(rect, (UI_PanelState.PairAdjustmentTablePanel ? "" : s_UiStateLabel[1]), TM_EditorStyles.rightLabel);

            if (UI_PanelState.PairAdjustmentTablePanel)
            {
                int arraySize = m_GlyphPairAdjustmentRecords_prop.arraySize;
                int itemsPerPage = 20;

                // Display Kerning Pair Management Tools
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // Search Bar implementation
                    #region DISPLAY SEARCH BAR
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUIUtility.labelWidth = 150f;
                        EditorGUI.BeginChangeCheck();
                        string searchPattern = EditorGUILayout.TextField("Adjustment Pair Search", m_KerningTableSearchPattern, "SearchTextField");
                        if (EditorGUI.EndChangeCheck() || m_isSearchDirty)
                        {
                            if (string.IsNullOrEmpty(searchPattern) == false)
                            {
                                m_KerningTableSearchPattern = searchPattern;

                                // Search Glyph Table for potential matches
                                SearchKerningTable(m_KerningTableSearchPattern, ref m_KerningTableSearchList);
                            }
                            else
                                m_KerningTableSearchPattern = null;

                            m_isSearchDirty = false;
                        }

                        string styleName = string.IsNullOrEmpty(m_KerningTableSearchPattern) ? "SearchCancelButtonEmpty" : "SearchCancelButton";
                        if (GUILayout.Button(GUIContent.none, styleName))
                        {
                            GUIUtility.keyboardControl = 0;
                            m_KerningTableSearchPattern = string.Empty;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    #endregion

                    // Display Page Navigation
                    if (!string.IsNullOrEmpty(m_KerningTableSearchPattern))
                        arraySize = m_KerningTableSearchList.Count;

                    DisplayPageNavigation(ref m_CurrentAdjustmentPairPage, arraySize, itemsPerPage);
                }
                EditorGUILayout.EndVertical();

                if (arraySize > 0)
                {
                    // Display each GlyphInfo entry using the GlyphInfo property drawer.
                    for (int i = itemsPerPage * m_CurrentAdjustmentPairPage; i < arraySize && i < itemsPerPage * (m_CurrentAdjustmentPairPage + 1); i++)
                    {
                        // Define the start of the selection region of the element.
                        Rect elementStartRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        int elementIndex = i;
                        if (!string.IsNullOrEmpty(m_KerningTableSearchPattern))
                            elementIndex = m_KerningTableSearchList[i];

                        SerializedProperty pairAdjustmentRecordProperty = m_GlyphPairAdjustmentRecords_prop.GetArrayElementAtIndex(elementIndex);

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        using (new EditorGUI.DisabledScope(i != m_SelectedAdjustmentRecord))
                        {
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(pairAdjustmentRecordProperty, new GUIContent("Selectable"));
                            if (EditorGUI.EndChangeCheck())
                            {
                                UpdatePairAdjustmentRecordLookup(pairAdjustmentRecordProperty);
                            }
                        }

                        EditorGUILayout.EndVertical();

                        // Define the end of the selection region of the element.
                        Rect elementEndRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        // Check for Item selection
                        Rect selectionArea = new Rect(elementStartRegion.x, elementStartRegion.y, elementEndRegion.width, elementEndRegion.y - elementStartRegion.y);
                        if (DoSelectionCheck(selectionArea))
                        {
                            if (m_SelectedAdjustmentRecord == i)
                            {
                                m_SelectedAdjustmentRecord = -1;
                            }
                            else
                            {
                                m_SelectedAdjustmentRecord = i;
                                GUIUtility.keyboardControl = 0;
                            }
                        }

                        // Draw Selection Highlight and Kerning Pair Options
                        if (m_SelectedAdjustmentRecord == i)
                        {
                            // Reset other selections
                            ResetSelections(RecordSelectionType.AdjustmentPairRecord);

                            TextCoreEditorUtilities.DrawBox(selectionArea, 2f, new Color32(40, 192, 255, 255));

                            // Draw Glyph management options
                            Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 1f);
                            float optionAreaWidth = controlRect.width;
                            float btnWidth = optionAreaWidth / 4;

                            Rect position = new Rect(controlRect.x + controlRect.width - btnWidth, controlRect.y, btnWidth, controlRect.height);

                            // Remove Kerning pair
                            GUI.enabled = true;
                            if (GUI.Button(position, "Remove"))
                            {
                                GUIUtility.keyboardControl = 0;

                                RemoveRecord(m_GlyphPairAdjustmentRecords_prop, i);

                                isAssetDirty = true;
                                m_SelectedAdjustmentRecord = -1;
                                m_isSearchDirty = true;
                                break;
                            }
                        }
                    }
                }

                DisplayPageNavigation(ref m_CurrentAdjustmentPairPage, arraySize, itemsPerPage);

                GUILayout.Space(5);

                // Add new kerning pair
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_EmptyGlyphPairAdjustmentRecord_prop);
                    if (EditorGUI.EndChangeCheck())
                    {
                        SetPropertyHolderGlyphIndexes();
                    }
                }
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Add New Glyph Adjustment Record"))
                {
                    SerializedProperty firstAdjustmentRecordProperty = m_EmptyGlyphPairAdjustmentRecord_prop.FindPropertyRelative("m_FirstAdjustmentRecord");
                    SerializedProperty secondAdjustmentRecordProperty = m_EmptyGlyphPairAdjustmentRecord_prop.FindPropertyRelative("m_SecondAdjustmentRecord");

                    uint firstGlyphIndex = (uint)firstAdjustmentRecordProperty.FindPropertyRelative("m_GlyphIndex").intValue;
                    uint secondGlyphIndex = (uint)secondAdjustmentRecordProperty.FindPropertyRelative("m_GlyphIndex").intValue;

                    GlyphValueRecord firstValueRecord = GetValueRecord(firstAdjustmentRecordProperty.FindPropertyRelative("m_GlyphValueRecord"));
                    GlyphValueRecord secondValueRecord = GetValueRecord(secondAdjustmentRecordProperty.FindPropertyRelative("m_GlyphValueRecord"));

                    errorCode = -1;
                    uint pairKey = secondGlyphIndex << 16 | firstGlyphIndex;
                    if (m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.ContainsKey(pairKey) == false)
                    {
                        GlyphPairAdjustmentRecord adjustmentRecord = new GlyphPairAdjustmentRecord(new GlyphAdjustmentRecord(firstGlyphIndex, firstValueRecord), new GlyphAdjustmentRecord(secondGlyphIndex, secondValueRecord));
                        m_FontFeatureTable.glyphPairAdjustmentRecords.Add(adjustmentRecord);
                        m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.Add(pairKey, adjustmentRecord);
                        errorCode = 0;
                    }

                    // Add glyphs and characters
                    Character character;

                    uint firstCharacter = m_SerializedPropertyHolder.firstCharacter;
                    if (!m_fontAsset.characterLookupTable.ContainsKey(firstCharacter))
                        m_fontAsset.TryAddCharacterInternal(firstCharacter, FontStyles.Normal, TextFontWeight.Regular, out character);

                    uint secondCharacter = m_SerializedPropertyHolder.secondCharacter;
                    if (!m_fontAsset.characterLookupTable.ContainsKey(secondCharacter))
                        m_fontAsset.TryAddCharacterInternal(secondCharacter, FontStyles.Normal, TextFontWeight.Regular, out character);

                    // Sort Kerning Pairs & Reload Font Asset if new kerning pair was added.
                    if (errorCode != -1)
                    {
                        m_FontFeatureTable.SortGlyphPairAdjustmentRecords();
                        serializedObject.ApplyModifiedProperties();
                        isAssetDirty = true;
                        m_isSearchDirty = true;
                    }
                    else
                    {
                        timeStamp = System.DateTime.Now.AddSeconds(5);
                    }

                    // Clear Add Kerning Pair Panel
                    // TODO
                }

                if (errorCode == -1)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Kerning Pair already <color=#ffff00>exists!</color>", TM_EditorStyles.label);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    if (System.DateTime.Now > timeStamp)
                        errorCode = 0;
                }
            }
            #endregion

            // MARK TO BASE Font Feature Table
            #region MARK TO BASE
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
            EditorGUI.indentLevel = 0;
            rect = EditorGUILayout.GetControlRect(false, 24);

            int markToBaseAdjustmentRecordCount = m_fontAsset.fontFeatureTable.MarkToBaseAdjustmentRecords.Count;

            if (GUI.Button(rect, new GUIContent("<b>Mark To Base Adjustment Table</b>   [" + markToBaseAdjustmentRecordCount + "]" + (rect.width > 340 ? " Records" : ""), "List of Mark to Base adjustment records."), TM_EditorStyles.sectionHeader))
                UI_PanelState.MarkToBaseTablePanel = !UI_PanelState.MarkToBaseTablePanel;

            GUI.Label(rect, (UI_PanelState.MarkToBaseTablePanel ? "" : s_UiStateLabel[1]), TM_EditorStyles.rightLabel);

            if (UI_PanelState.MarkToBaseTablePanel)
            {
                int arraySize = m_MarkToBaseAdjustmentRecords_prop.arraySize;
                int itemsPerPage = 20;

                // Display Mark Adjust Records Management Tools
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // Search Bar implementation
                    #region DISPLAY SEARCH BAR
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUIUtility.labelWidth = 150f;
                        EditorGUI.BeginChangeCheck();
                        string searchPattern = EditorGUILayout.TextField("Mark to Base Search", m_MarkToBaseTableSearchPattern, "SearchTextField");
                        if (EditorGUI.EndChangeCheck() || m_isSearchDirty)
                        {
                            if (string.IsNullOrEmpty(searchPattern) == false)
                            {
                                m_MarkToBaseTableSearchPattern = searchPattern;

                                // Search Glyph Table for potential matches
                                SearchMarkToBaseTable(m_MarkToBaseTableSearchPattern, ref m_MarkToBaseTableSearchList);
                            }
                            else
                                m_MarkToBaseTableSearchPattern = null;

                            m_isSearchDirty = false;
                        }

                        string styleName = string.IsNullOrEmpty(m_MarkToBaseTableSearchPattern) ? "SearchCancelButtonEmpty" : "SearchCancelButton";
                        if (GUILayout.Button(GUIContent.none, styleName))
                        {
                            GUIUtility.keyboardControl = 0;
                            m_MarkToBaseTableSearchPattern = string.Empty;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    #endregion

                    // Display Page Navigation
                    if (!string.IsNullOrEmpty(m_MarkToBaseTableSearchPattern))
                        arraySize = m_MarkToBaseTableSearchList.Count;

                    DisplayPageNavigation(ref m_CurrentMarkToBasePage, arraySize, itemsPerPage);
                }
                EditorGUILayout.EndVertical();

                if (arraySize > 0)
                {
                    // Display each GlyphInfo entry using the GlyphInfo property drawer.
                    for (int i = itemsPerPage * m_CurrentMarkToBasePage; i < arraySize && i < itemsPerPage * (m_CurrentMarkToBasePage + 1); i++)
                    {
                        // Define the start of the selection region of the element.
                        Rect elementStartRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        int elementIndex = i;
                        if (!string.IsNullOrEmpty(m_MarkToBaseTableSearchPattern))
                            elementIndex = m_MarkToBaseTableSearchList[i];

                        SerializedProperty markToBasePropertyRecord = m_MarkToBaseAdjustmentRecords_prop.GetArrayElementAtIndex(elementIndex);

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        using (new EditorGUI.DisabledScope(i != m_SelectedMarkToBaseRecord))
                        {
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(markToBasePropertyRecord, new GUIContent("Selectable"));

                            if (EditorGUI.EndChangeCheck())
                            {
                                UpdateMarkToBaseAdjustmentRecordLookup(markToBasePropertyRecord);
                            }
                        }

                        EditorGUILayout.EndVertical();

                        // Define the end of the selection region of the element.
                        Rect elementEndRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        // Check for Item selection
                        Rect selectionArea = new Rect(elementStartRegion.x, elementStartRegion.y, elementEndRegion.width, elementEndRegion.y - elementStartRegion.y);
                        if (DoSelectionCheck(selectionArea))
                        {
                            if (m_SelectedMarkToBaseRecord == i)
                            {
                                m_SelectedMarkToBaseRecord = -1;
                            }
                            else
                            {
                                m_SelectedMarkToBaseRecord = i;
                                GUIUtility.keyboardControl = 0;
                            }
                        }

                        // Draw Selection Highlight and Kerning Pair Options
                        if (m_SelectedMarkToBaseRecord == i)
                        {
                            // Reset other selections
                            ResetSelections(RecordSelectionType.MarkToBaseRecord);

                            TextCoreEditorUtilities.DrawBox(selectionArea, 2f, new Color32(40, 192, 255, 255));

                            // Draw Glyph management options
                            Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 1f);
                            float optionAreaWidth = controlRect.width;
                            float btnWidth = optionAreaWidth / 4;

                            Rect position = new Rect(controlRect.x + controlRect.width - btnWidth, controlRect.y, btnWidth, controlRect.height);

                            // Remove Mark to Base Record
                            GUI.enabled = true;
                            if (GUI.Button(position, "Remove"))
                            {
                                GUIUtility.keyboardControl = 0;

                                RemoveRecord(m_MarkToBaseAdjustmentRecords_prop, i);

                                isAssetDirty = true;
                                m_SelectedMarkToBaseRecord = -1;
                                m_isSearchDirty = true;
                                break;
                            }
                        }
                    }
                }

                DisplayAddRemoveButtons(m_MarkToBaseAdjustmentRecords_prop, m_SelectedMarkToBaseRecord, markToBaseAdjustmentRecordCount);

                DisplayPageNavigation(ref m_CurrentMarkToBasePage, arraySize, itemsPerPage);

                GUILayout.Space(5);
            }
            #endregion

            // MARK TO MARK Font Feature Table
            #region MARK TO MARK
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
            EditorGUI.indentLevel = 0;
            rect = EditorGUILayout.GetControlRect(false, 24);

            int markToMarkAdjustmentRecordCount = m_fontAsset.fontFeatureTable.MarkToMarkAdjustmentRecords.Count;

            if (GUI.Button(rect, new GUIContent("<b>Mark To Mark Adjustment Table</b>   [" + markToMarkAdjustmentRecordCount + "]" + (rect.width > 340 ? " Records" : ""), "List of Mark to Mark adjustment records."), TM_EditorStyles.sectionHeader))
                UI_PanelState.MarkToMarkTablePanel = !UI_PanelState.MarkToMarkTablePanel;

            GUI.Label(rect, (UI_PanelState.MarkToMarkTablePanel ? "" : s_UiStateLabel[1]), TM_EditorStyles.rightLabel);

            if (UI_PanelState.MarkToMarkTablePanel)
            {
                int arraySize = m_MarkToMarkAdjustmentRecords_prop.arraySize;
                int itemsPerPage = 20;

                // Display Kerning Pair Management Tools
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // Search Bar implementation
                    #region DISPLAY SEARCH BAR
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUIUtility.labelWidth = 150f;
                        EditorGUI.BeginChangeCheck();
                        string searchPattern = EditorGUILayout.TextField("Mark to Mark Search", m_MarkToMarkTableSearchPattern, "SearchTextField");
                        if (EditorGUI.EndChangeCheck() || m_isSearchDirty)
                        {
                            if (string.IsNullOrEmpty(searchPattern) == false)
                            {
                                m_MarkToMarkTableSearchPattern = searchPattern;

                                // Search Glyph Table for potential matches
                                SearchMarkToMarkTable(m_MarkToMarkTableSearchPattern, ref m_MarkToMarkTableSearchList);
                            }
                            else
                                m_MarkToMarkTableSearchPattern = null;

                            m_isSearchDirty = false;
                        }

                        string styleName = string.IsNullOrEmpty(m_MarkToMarkTableSearchPattern) ? "SearchCancelButtonEmpty" : "SearchCancelButton";
                        if (GUILayout.Button(GUIContent.none, styleName))
                        {
                            GUIUtility.keyboardControl = 0;
                            m_MarkToMarkTableSearchPattern = string.Empty;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    #endregion

                    // Display Page Navigation
                    if (!string.IsNullOrEmpty(m_MarkToMarkTableSearchPattern))
                        arraySize = m_MarkToMarkTableSearchList.Count;

                    DisplayPageNavigation(ref m_CurrentMarkToMarkPage, arraySize, itemsPerPage);
                }
                EditorGUILayout.EndVertical();

                if (arraySize > 0)
                {
                    // Display each GlyphInfo entry using the GlyphInfo property drawer.
                    for (int i = itemsPerPage * m_CurrentMarkToMarkPage; i < arraySize && i < itemsPerPage * (m_CurrentMarkToMarkPage + 1); i++)
                    {
                        // Define the start of the selection region of the element.
                        Rect elementStartRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        int elementIndex = i;
                        if (!string.IsNullOrEmpty(m_MarkToMarkTableSearchPattern))
                            elementIndex = m_MarkToMarkTableSearchList[i];

                        SerializedProperty markToMarkPropertyRecord = m_MarkToMarkAdjustmentRecords_prop.GetArrayElementAtIndex(elementIndex);

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        using (new EditorGUI.DisabledScope(i != m_SelectedMarkToMarkRecord))
                        {
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(markToMarkPropertyRecord, new GUIContent("Selectable"));

                            if (EditorGUI.EndChangeCheck())
                            {
                                UpdateMarkToMarkAdjustmentRecordLookup(markToMarkPropertyRecord);
                            }
                        }

                        EditorGUILayout.EndVertical();

                        // Define the end of the selection region of the element.
                        Rect elementEndRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                        // Check for Item selection
                        Rect selectionArea = new Rect(elementStartRegion.x, elementStartRegion.y, elementEndRegion.width, elementEndRegion.y - elementStartRegion.y);
                        if (DoSelectionCheck(selectionArea))
                        {
                            if (m_SelectedMarkToMarkRecord == i)
                            {
                                m_SelectedMarkToMarkRecord = -1;
                            }
                            else
                            {
                                m_SelectedMarkToMarkRecord = i;
                                GUIUtility.keyboardControl = 0;
                            }
                        }

                        // Draw Selection Highlight and Kerning Pair Options
                        if (m_SelectedMarkToMarkRecord == i)
                        {
                            // Reset other selections
                            ResetSelections(RecordSelectionType.MarkToMarkRecord);

                            TextCoreEditorUtilities.DrawBox(selectionArea, 2f, new Color32(40, 192, 255, 255));

                            // Draw Glyph management options
                            Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 1f);
                            float optionAreaWidth = controlRect.width;
                            float btnWidth = optionAreaWidth / 4;

                            Rect position = new Rect(controlRect.x + controlRect.width - btnWidth, controlRect.y, btnWidth, controlRect.height);

                            // Remove Mark to Base Record
                            GUI.enabled = true;
                            if (GUI.Button(position, "Remove"))
                            {
                                GUIUtility.keyboardControl = 0;

                                RemoveRecord(m_MarkToMarkAdjustmentRecords_prop, i);

                                isAssetDirty = true;
                                m_SelectedMarkToMarkRecord = -1;
                                m_isSearchDirty = true;
                                break;
                            }
                        }
                    }
                }

                DisplayAddRemoveButtons(m_MarkToMarkAdjustmentRecords_prop, m_SelectedMarkToMarkRecord, markToMarkAdjustmentRecordCount);

                DisplayPageNavigation(ref m_CurrentMarkToMarkPage, arraySize, itemsPerPage);

                GUILayout.Space(5);
            }
            #endregion

            if (serializedObject.ApplyModifiedProperties() || evt_cmd == k_UndoRedo || isAssetDirty || m_IsFallbackGlyphCacheDirty)
            {
                // Delay callback until user has decided to Apply or Revert the changes.
                if (m_DisplayDestructiveChangeWarning == false)
                {
                    TextResourceManager.RebuildFontAssetCache();
                    TextEventManager.ON_FONT_PROPERTY_CHANGED(true, m_fontAsset);
                    m_IsFallbackGlyphCacheDirty = false;
                }

                if (m_fontAsset.IsFontAssetLookupTablesDirty || evt_cmd == k_UndoRedo)
                    m_fontAsset.ReadFontAssetDefinition();

                isAssetDirty = false;
                EditorUtility.SetDirty(target);
            }


            // Clear selection if mouse event was not consumed.
            GUI.enabled = true;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                m_SelectedAdjustmentRecord = -1;
                m_SelectedMarkToBaseRecord = -1;
                m_SelectedMarkToMarkRecord = -1;
            }
        }

        /// <summary>
        /// Overrided method from the Editor class.
        /// </summary>
        /// <returns></returns>
        [UnityEngine.Internal.ExcludeFromDocs]
        public override bool HasPreviewGUI()
        {
            return true;
        }

        /// <summary>
        /// Overrided method to implement custom preview inspector.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="background"></param>
        [UnityEngine.Internal.ExcludeFromDocs]
        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (m_SelectedMarkToBaseRecord != -1)
                DrawMarkToBasePreview(m_SelectedMarkToBaseRecord, rect);

            if (m_SelectedMarkToMarkRecord != -1)
                DrawMarkToMarkPreview(m_SelectedMarkToMarkRecord, rect);

        }

        void ResetSelections(RecordSelectionType type)
        {
            switch (type)
            {
             case RecordSelectionType.CharacterRecord:
                 m_SelectedGlyphRecord = -1;
                 m_SelectedLigatureRecord = -1;
                 m_SelectedAdjustmentRecord = -1;
                 m_SelectedMarkToBaseRecord = -1;
                 m_SelectedMarkToMarkRecord = -1;
                 break;
             case RecordSelectionType.GlyphRecord:
                 m_SelectedCharacterRecord = -1;
                 m_SelectedLigatureRecord = -1;
                 m_SelectedAdjustmentRecord = -1;
                 m_SelectedMarkToBaseRecord = -1;
                 m_SelectedMarkToMarkRecord = -1;
                 break;
             case RecordSelectionType.LigatureSubstitutionRecord:
                 m_SelectedCharacterRecord = -1;
                 m_SelectedGlyphRecord = -1;
                 m_SelectedAdjustmentRecord = -1;
                 m_SelectedMarkToBaseRecord = -1;
                 m_SelectedMarkToMarkRecord = -1;
                 break;
             case RecordSelectionType.AdjustmentPairRecord:
                 m_SelectedCharacterRecord = -1;
                 m_SelectedGlyphRecord = -1;
                 m_SelectedLigatureRecord = -1;
                 m_SelectedMarkToBaseRecord = -1;
                 m_SelectedMarkToMarkRecord = -1;
                 break;
             case RecordSelectionType.MarkToBaseRecord:
                 m_SelectedCharacterRecord = -1;
                 m_SelectedGlyphRecord = -1;
                 m_SelectedLigatureRecord = -1;
                 m_SelectedAdjustmentRecord = -1;
                 m_SelectedMarkToMarkRecord = -1;
                 break;
             case RecordSelectionType.MarkToMarkRecord:
                 m_SelectedCharacterRecord = -1;
                 m_SelectedGlyphRecord = -1;
                 m_SelectedLigatureRecord = -1;
                 m_SelectedAdjustmentRecord = -1;
                 m_SelectedMarkToBaseRecord = -1;
                 break;
            }
        }

        string[] GetFontFaces()
        {
            return GetFontFaces(m_FontFaceIndex_prop.intValue);
        }

        string[] GetFontFaces(int faceIndex)
        {
            if (LoadFontFace(m_SamplingPointSize_prop.floatValue, faceIndex) == FontEngineError.Success)
                return FontEngine.GetFontFaces();

            return k_InvalidFontFaces;
        }

        FontEngineError LoadFontFace(float pointSize, int faceIndex)
        {
            if (m_fontAsset.SourceFont_EditorRef != null)
            {
                if (FontEngine.LoadFontFace(m_fontAsset.SourceFont_EditorRef, pointSize, faceIndex) == FontEngineError.Success)
                    return FontEngineError.Success;
            }

            return FontEngine.LoadFontFace(m_fontAsset.faceInfo.familyName, m_fontAsset.faceInfo.styleName, pointSize);
        }

        void CleanFallbackFontAssetTable()
        {
            SerializedProperty m_FallbackFontAsseTable = serializedObject.FindProperty("m_FallbackFontAssetTable");

            bool isListDirty = false;

            int elementCount = m_FallbackFontAsseTable.arraySize;

            for (int i = 0; i < elementCount; i++)
            {
                SerializedProperty element = m_FallbackFontAsseTable.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null)
                {
                    m_FallbackFontAsseTable.DeleteArrayElementAtIndex(i);
                    elementCount -= 1;
                    i -= 1;

                    isListDirty = true;
                }
            }

            if (isListDirty)
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }

        internal void SavedGenerationSettings()
        {
            m_GenerationSettings.faceIndex = m_FontFaceIndex_prop.intValue;
            m_GenerationSettings.glyphRenderMode = (GlyphRenderMode)m_AtlasRenderMode_prop.intValue;
            m_GenerationSettings.pointSize       = m_SamplingPointSize_prop.floatValue;
            m_GenerationSettings.padding         = m_AtlasPadding_prop.intValue;
            m_GenerationSettings.atlasWidth      = m_AtlasWidth_prop.intValue;
            m_GenerationSettings.atlasHeight     = m_AtlasHeight_prop.intValue;
        }

        internal void RestoreGenerationSettings()
        {
            m_fontAsset.SourceFont_EditorRef = m_GenerationSettings.sourceFont;
            m_FontFaceIndex_prop.intValue = m_GenerationSettings.faceIndex;
            m_SamplingPointSize_prop.floatValue = m_GenerationSettings.pointSize;
            m_FontFaces = GetFontFaces();

            m_AtlasRenderMode_prop.intValue = (int)m_GenerationSettings.glyphRenderMode;
            m_AtlasPadding_prop.intValue = m_GenerationSettings.padding;
            m_AtlasWidth_prop.intValue = m_GenerationSettings.atlasWidth;
            m_AtlasHeight_prop.intValue = m_GenerationSettings.atlasHeight;
        }

        void UpdateFontAssetCreationSettings()
        {
            m_fontAsset.m_fontAssetCreationEditorSettings.faceIndex = m_FontFaceIndex_prop.intValue;
            m_fontAsset.m_fontAssetCreationEditorSettings.pointSize = m_SamplingPointSize_prop.floatValue;
            m_fontAsset.m_fontAssetCreationEditorSettings.renderMode = m_AtlasRenderMode_prop.intValue;
            m_fontAsset.m_fontAssetCreationEditorSettings.padding = m_AtlasPadding_prop.intValue;
            m_fontAsset.m_fontAssetCreationEditorSettings.atlasWidth = m_AtlasWidth_prop.intValue;
            m_fontAsset.m_fontAssetCreationEditorSettings.atlasHeight = m_AtlasHeight_prop.intValue;
        }

        void UpdateCharacterData(SerializedProperty property, int index)
        {
            Character character = m_fontAsset.characterTable[index];

            character.unicode = (uint)property.FindPropertyRelative("m_Unicode").intValue;
            character.scale = property.FindPropertyRelative("m_Scale").floatValue;

            SerializedProperty glyphProperty = property.FindPropertyRelative("m_Glyph");
            character.glyph.index = (uint)glyphProperty.FindPropertyRelative("m_Index").intValue;

            SerializedProperty glyphRectProperty = glyphProperty.FindPropertyRelative("m_GlyphRect");
            character.glyph.glyphRect = new GlyphRect(glyphRectProperty.FindPropertyRelative("m_X").intValue, glyphRectProperty.FindPropertyRelative("m_Y").intValue, glyphRectProperty.FindPropertyRelative("m_Width").intValue, glyphRectProperty.FindPropertyRelative("m_Height").intValue);

            SerializedProperty glyphMetricsProperty = glyphProperty.FindPropertyRelative("m_Metrics");
            character.glyph.metrics = new UnityEngine.TextCore.GlyphMetrics(glyphMetricsProperty.FindPropertyRelative("m_Width").floatValue, glyphMetricsProperty.FindPropertyRelative("m_Height").floatValue, glyphMetricsProperty.FindPropertyRelative("m_HorizontalBearingX").floatValue, glyphMetricsProperty.FindPropertyRelative("m_HorizontalBearingY").floatValue, glyphMetricsProperty.FindPropertyRelative("m_HorizontalAdvance").floatValue);

            character.glyph.scale = glyphProperty.FindPropertyRelative("m_Scale").floatValue;

            character.glyph.atlasIndex = glyphProperty.FindPropertyRelative("m_AtlasIndex").intValue;
        }

        void UpdateGlyphData(SerializedProperty property, int index)
        {
            UnityEngine.TextCore.Glyph glyph = m_fontAsset.glyphTable[index];

            glyph.index = (uint)property.FindPropertyRelative("m_Index").intValue;

            SerializedProperty glyphRect = property.FindPropertyRelative("m_GlyphRect");
            glyph.glyphRect = new GlyphRect(glyphRect.FindPropertyRelative("m_X").intValue, glyphRect.FindPropertyRelative("m_Y").intValue, glyphRect.FindPropertyRelative("m_Width").intValue, glyphRect.FindPropertyRelative("m_Height").intValue);

            SerializedProperty glyphMetrics = property.FindPropertyRelative("m_Metrics");
            glyph.metrics = new GlyphMetrics(glyphMetrics.FindPropertyRelative("m_Width").floatValue, glyphMetrics.FindPropertyRelative("m_Height").floatValue, glyphMetrics.FindPropertyRelative("m_HorizontalBearingX").floatValue, glyphMetrics.FindPropertyRelative("m_HorizontalBearingY").floatValue, glyphMetrics.FindPropertyRelative("m_HorizontalAdvance").floatValue);

            glyph.scale = property.FindPropertyRelative("m_Scale").floatValue;
        }

        void DisplayAddRemoveButtons(SerializedProperty property, int selectedRecord, int recordCount)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 20);

            rect.width /= 6;
            // Add Style
            rect.x = rect.width * 4 + 15;
            if (GUI.Button(rect, "+"))
            {
                int index = selectedRecord == -1 ? 0 : selectedRecord;

                if (index > recordCount)
                    index = recordCount;

                // Copy selected element
                property.InsertArrayElementAtIndex(index);

                // Select newly inserted element
                selectedRecord = index + 1;

                serializedObject.ApplyModifiedProperties();

                m_fontAsset.ReadFontAssetDefinition();

            }

            // Delete style
            rect.x += rect.width;
            if (selectedRecord == -1 || selectedRecord >= recordCount) GUI.enabled = false;
            if (GUI.Button(rect, "-"))
            {
                int index = selectedRecord == -1 ? 0 : selectedRecord;

                property.DeleteArrayElementAtIndex(index);

                selectedRecord = -1;
                serializedObject.ApplyModifiedProperties();

                m_fontAsset.ReadFontAssetDefinition();
                return;
            }

            GUI.enabled = true;
        }

        void DisplayPageNavigation(ref int currentPage, int arraySize, int itemsPerPage)
        {
            Rect pagePos = EditorGUILayout.GetControlRect(false, 20);
            pagePos.width /= 3;

            int shiftMultiplier = Event.current.shift ? 10 : 1; // Page + Shift goes 10 page forward

            // Previous Page
            GUI.enabled = currentPage > 0;

            if (GUI.Button(pagePos, "Previous Page"))
                currentPage -= 1 * shiftMultiplier;


            // Page Counter
            GUI.enabled = true;
            pagePos.x += pagePos.width;
            int totalPages = (int)(arraySize / (float)itemsPerPage + 0.999f);
            GUI.Label(pagePos, "Page " + (currentPage + 1) + " / " + totalPages, TM_EditorStyles.centeredLabel);

            // Next Page
            pagePos.x += pagePos.width;
            GUI.enabled = itemsPerPage * (currentPage + 1) < arraySize;

            if (GUI.Button(pagePos, "Next Page"))
                currentPage += 1 * shiftMultiplier;

            // Clamp page range
            currentPage = Mathf.Clamp(currentPage, 0, arraySize / itemsPerPage);

            GUI.enabled = true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="srcGlyphID"></param>
        /// <param name="dstGlyphID"></param>
        bool AddNewGlyph(int srcIndex, int dstGlyphID)
        {
            // Make sure Destination Glyph ID doesn't already contain a Glyph
            if (m_fontAsset.glyphLookupTable.ContainsKey((uint)dstGlyphID))
                return false;

            // Add new element to glyph list.
            m_GlyphTable_prop.arraySize += 1;

            // Get a reference to the source glyph.
            SerializedProperty sourceGlyph = m_GlyphTable_prop.GetArrayElementAtIndex(srcIndex);

            int dstIndex = m_GlyphTable_prop.arraySize - 1;

            // Get a reference to the target / destination glyph.
            SerializedProperty targetGlyph = m_GlyphTable_prop.GetArrayElementAtIndex(dstIndex);

            CopyGlyphSerializedProperty(sourceGlyph, ref targetGlyph);

            // Update the ID of the glyph
            targetGlyph.FindPropertyRelative("m_Index").intValue = dstGlyphID;

            serializedObject.ApplyModifiedProperties();

            m_fontAsset.SortGlyphTable();

            m_fontAsset.ReadFontAssetDefinition();

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="glyphID"></param>
        void RemoveGlyphFromList(int index)
        {
            if (index > m_GlyphTable_prop.arraySize)
                return;

            int targetGlyphIndex = m_GlyphTable_prop.GetArrayElementAtIndex(index).FindPropertyRelative("m_Index").intValue;

            m_GlyphTable_prop.DeleteArrayElementAtIndex(index);

            // Remove all characters referencing this glyph.
            for (int i = 0; i < m_CharacterTable_prop.arraySize; i++)
            {
                int glyphIndex = m_CharacterTable_prop.GetArrayElementAtIndex(i).FindPropertyRelative("m_GlyphIndex").intValue;

                if (glyphIndex == targetGlyphIndex)
                {
                    // Remove character
                    m_CharacterTable_prop.DeleteArrayElementAtIndex(i);
                }
            }

            serializedObject.ApplyModifiedProperties();

            m_fontAsset.ReadFontAssetDefinition();
        }

        bool AddNewCharacter(int srcIndex, int dstGlyphID)
        {
            // Make sure Destination Glyph ID doesn't already contain a Glyph
            if (m_fontAsset.characterLookupTable.ContainsKey((uint)dstGlyphID))
                return false;

            // Add new element to glyph list.
            m_CharacterTable_prop.arraySize += 1;

            // Get a reference to the source glyph.
            SerializedProperty sourceCharacter = m_CharacterTable_prop.GetArrayElementAtIndex(srcIndex);

            int dstIndex = m_CharacterTable_prop.arraySize - 1;

            // Get a reference to the target / destination glyph.
            SerializedProperty targetCharacter = m_CharacterTable_prop.GetArrayElementAtIndex(dstIndex);

            CopyCharacterSerializedProperty(sourceCharacter, ref targetCharacter);

            // Update the ID of the glyph
            targetCharacter.FindPropertyRelative("m_Unicode").intValue = dstGlyphID;

            serializedObject.ApplyModifiedProperties();

            m_fontAsset.SortCharacterTable();

            m_fontAsset.ReadFontAssetDefinition();

            return true;
        }

        void RemoveCharacterFromList(int index)
        {
            if (index > m_CharacterTable_prop.arraySize)
                return;

            m_CharacterTable_prop.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();

            m_fontAsset.ReadFontAssetDefinition();
        }

        void AddNewGlyphsFromProperty(SerializedProperty property)
        {
            if (m_GlyphsToAdd == null)
                m_GlyphsToAdd = new HashSet<uint>();
            else
                m_GlyphsToAdd.Clear();

            string propertyType = property.type;

            switch (propertyType)
            {
                case "LigatureSubstitutionRecord":
                    int componentCount = property.FindPropertyRelative("m_ComponentGlyphIDs").arraySize;
                    for (int i = 0; i < componentCount; i++)
                    {
                        uint glyphIndex = (uint)property.FindPropertyRelative("m_ComponentGlyphIDs").GetArrayElementAtIndex(i).intValue;
                        m_GlyphsToAdd.Add(glyphIndex);
                    }

                    m_GlyphsToAdd.Add((uint)property.FindPropertyRelative("m_LigatureGlyphID").intValue);

                    foreach (uint glyphIndex in m_GlyphsToAdd)
                    {
                        if (glyphIndex != 0)
                            m_fontAsset.TryAddGlyphInternal(glyphIndex, out _);
                    }

                    break;
            }

        }

        // Check if any of the Style elements were clicked on.
        private bool DoSelectionCheck(Rect selectionArea)
        {
            Event currentEvent = Event.current;

            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (selectionArea.Contains(currentEvent.mousePosition) && currentEvent.button == 0)
                    {
                        currentEvent.Use();
                        return true;
                    }

                    break;
            }

            return false;
        }

        private void UpdateLigatureSubstitutionRecordLookup(SerializedProperty property)
        {
            serializedObject.ApplyModifiedProperties();
            AddNewGlyphsFromProperty(property);
            m_fontAsset.InitializeLigatureSubstitutionLookupDictionary();
            isAssetDirty = true;
        }

        void SetPropertyHolderGlyphIndexes()
        {
            uint firstCharacterUnicode = (uint)m_FirstCharacterUnicode_prop.intValue;
            if (firstCharacterUnicode != 0)
            {
                uint glyphIndex = m_fontAsset.GetGlyphIndex(firstCharacterUnicode, out bool success);
                if (glyphIndex != 0)
                    m_EmptyGlyphPairAdjustmentRecord_prop.FindPropertyRelative("m_FirstAdjustmentRecord").FindPropertyRelative("m_GlyphIndex").intValue = (int)glyphIndex;
            }

            uint secondCharacterUnicode = (uint)m_SecondCharacterUnicode_prop.intValue;
            if (secondCharacterUnicode != 0)
            {
                uint glyphIndex = m_fontAsset.GetGlyphIndex(secondCharacterUnicode, out bool success);
                if (glyphIndex != 0)
                    m_EmptyGlyphPairAdjustmentRecord_prop.FindPropertyRelative("m_SecondAdjustmentRecord").FindPropertyRelative("m_GlyphIndex").intValue = (int)glyphIndex;
            }
        }

        private void UpdatePairAdjustmentRecordLookup(SerializedProperty property)
        {
            GlyphPairAdjustmentRecord pairAdjustmentRecord = GetGlyphPairAdjustmentRecord(property);

            uint firstGlyphIndex = pairAdjustmentRecord.firstAdjustmentRecord.glyphIndex;
            uint secondGlyphIndex = pairAdjustmentRecord.secondAdjustmentRecord.glyphIndex;

            uint key = secondGlyphIndex << 16 | firstGlyphIndex;

            // Lookup dictionary entry and update it
            if (m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.ContainsKey(key))
                m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup[key] = pairAdjustmentRecord;
        }

        GlyphPairAdjustmentRecord GetGlyphPairAdjustmentRecord(SerializedProperty property)
        {
            GlyphPairAdjustmentRecord pairAdjustmentRecord = new GlyphPairAdjustmentRecord();

            SerializedProperty firstAdjustmentRecordProperty = property.FindPropertyRelative("m_FirstAdjustmentRecord");
            uint firstGlyphIndex = (uint)firstAdjustmentRecordProperty.FindPropertyRelative("m_GlyphIndex").intValue;
            GlyphValueRecord firstValueRecord = GetValueRecord(firstAdjustmentRecordProperty.FindPropertyRelative("m_GlyphValueRecord"));

            pairAdjustmentRecord.firstAdjustmentRecord = new GlyphAdjustmentRecord(firstGlyphIndex, firstValueRecord);

            SerializedProperty secondAdjustmentRecordProperty = property.FindPropertyRelative("m_SecondAdjustmentRecord");
            uint secondGlyphIndex = (uint)secondAdjustmentRecordProperty.FindPropertyRelative("m_GlyphIndex").intValue;
            GlyphValueRecord secondValueRecord = GetValueRecord(secondAdjustmentRecordProperty.FindPropertyRelative("m_GlyphValueRecord"));

            pairAdjustmentRecord.secondAdjustmentRecord = new GlyphAdjustmentRecord(secondGlyphIndex, secondValueRecord);

            // TODO : Need to revise how Everything is handled in the event more enum values are added.
            int flagValue = property.FindPropertyRelative("m_FeatureLookupFlags").intValue;
            //pairAdjustmentRecord.featureLookupFlags = flagValue == -1 ? FontFeatureLookupFlags.IgnoreLigatures | FontFeatureLookupFlags.IgnoreSpacingAdjustments : (FontFeatureLookupFlags) flagValue;

            return pairAdjustmentRecord;
        }

        void SwapCharacterElements(SerializedProperty property, int selectedIndex, int newIndex)
        {
            property.MoveArrayElement(selectedIndex, newIndex);
        }

        void RemoveRecord(SerializedProperty property, int index)
        {
            if (index > property.arraySize)
                return;

            property.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();

            m_fontAsset.ReadFontAssetDefinition();
        }

        GlyphValueRecord GetValueRecord(SerializedProperty property)
        {
            GlyphValueRecord record = new GlyphValueRecord();
            record.xPlacement = property.FindPropertyRelative("m_XPlacement").floatValue;
            record.yPlacement = property.FindPropertyRelative("m_YPlacement").floatValue;
            record.xAdvance = property.FindPropertyRelative("m_XAdvance").floatValue;
            record.yAdvance = property.FindPropertyRelative("m_YAdvance").floatValue;

            return record;
        }

        private void UpdateMarkToBaseAdjustmentRecordLookup(SerializedProperty property)
        {
            MarkToBaseAdjustmentRecord adjustmentRecord = GetMarkToBaseAdjustmentRecord(property);

            uint firstGlyphIndex = adjustmentRecord.baseGlyphID;
            uint secondGlyphIndex = adjustmentRecord.markGlyphID;

            uint key = secondGlyphIndex << 16 | firstGlyphIndex;

            // Lookup dictionary entry and update it
            if (m_FontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.ContainsKey(key))
                m_FontFeatureTable.m_MarkToBaseAdjustmentRecordLookup[key] = adjustmentRecord;
        }

        MarkToBaseAdjustmentRecord GetMarkToBaseAdjustmentRecord(SerializedProperty property)
        {
            MarkToBaseAdjustmentRecord adjustmentRecord = new MarkToBaseAdjustmentRecord();

            adjustmentRecord.baseGlyphID = (uint)property.FindPropertyRelative("m_BaseGlyphID").intValue;
            SerializedProperty baseAnchorPointProperty = property.FindPropertyRelative("m_BaseGlyphAnchorPoint");

            GlyphAnchorPoint baseAnchorPoint = new GlyphAnchorPoint();
            baseAnchorPoint.xCoordinate = baseAnchorPointProperty.FindPropertyRelative("m_XCoordinate").floatValue;
            baseAnchorPoint.yCoordinate = baseAnchorPointProperty.FindPropertyRelative("m_YCoordinate").floatValue;
            adjustmentRecord.baseGlyphAnchorPoint = baseAnchorPoint;

            adjustmentRecord.markGlyphID = (uint)property.FindPropertyRelative("m_MarkGlyphID").intValue;
            SerializedProperty markAdjustmentRecordProperty = property.FindPropertyRelative("m_MarkPositionAdjustment");

            MarkPositionAdjustment markAdjustmentRecord = new MarkPositionAdjustment();
            markAdjustmentRecord.xPositionAdjustment = markAdjustmentRecordProperty.FindPropertyRelative("m_XPositionAdjustment").floatValue;
            markAdjustmentRecord.yPositionAdjustment = markAdjustmentRecordProperty.FindPropertyRelative("m_YPositionAdjustment").floatValue;
            adjustmentRecord.markPositionAdjustment = markAdjustmentRecord;

            return adjustmentRecord;
        }

        private void UpdateMarkToMarkAdjustmentRecordLookup(SerializedProperty property)
        {
            MarkToMarkAdjustmentRecord adjustmentRecord = GetMarkToMarkAdjustmentRecord(property);

            uint firstGlyphIndex = adjustmentRecord.baseMarkGlyphID;
            uint secondGlyphIndex = adjustmentRecord.combiningMarkGlyphID;

            uint key = secondGlyphIndex << 16 | firstGlyphIndex;

            // Lookup dictionary entry and update it
            if (m_FontFeatureTable.m_MarkToMarkAdjustmentRecordLookup.ContainsKey(key))
                m_FontFeatureTable.m_MarkToMarkAdjustmentRecordLookup[key] = adjustmentRecord;
        }

        MarkToMarkAdjustmentRecord GetMarkToMarkAdjustmentRecord(SerializedProperty property)
        {
            MarkToMarkAdjustmentRecord adjustmentRecord = new MarkToMarkAdjustmentRecord();

            adjustmentRecord.baseMarkGlyphID = (uint)property.FindPropertyRelative("m_BaseMarkGlyphID").intValue;
            SerializedProperty baseAnchorPointProperty = property.FindPropertyRelative("m_BaseMarkGlyphAnchorPoint");

            GlyphAnchorPoint baseAnchorPoint = new GlyphAnchorPoint();
            baseAnchorPoint.xCoordinate = baseAnchorPointProperty.FindPropertyRelative("m_XCoordinate").floatValue;
            baseAnchorPoint.yCoordinate = baseAnchorPointProperty.FindPropertyRelative("m_YCoordinate").floatValue;
            adjustmentRecord.baseMarkGlyphAnchorPoint = baseAnchorPoint;

            adjustmentRecord.combiningMarkGlyphID = (uint)property.FindPropertyRelative("m_CombiningMarkGlyphID").intValue;
            SerializedProperty markAdjustmentRecordProperty = property.FindPropertyRelative("m_CombiningMarkPositionAdjustment");

            MarkPositionAdjustment markAdjustment = new MarkPositionAdjustment();
            markAdjustment.xPositionAdjustment = markAdjustmentRecordProperty.FindPropertyRelative("m_XPositionAdjustment").floatValue;
            markAdjustment.yPositionAdjustment = markAdjustmentRecordProperty.FindPropertyRelative("m_YPositionAdjustment").floatValue;
            adjustmentRecord.combiningMarkPositionAdjustment = markAdjustment;

            return adjustmentRecord;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="srcGlyph"></param>
        /// <param name="dstGlyph"></param>
        void CopyGlyphSerializedProperty(SerializedProperty srcGlyph, ref SerializedProperty dstGlyph)
        {
            // TODO : Should make a generic function which copies each of the properties.
            dstGlyph.FindPropertyRelative("m_Index").intValue = srcGlyph.FindPropertyRelative("m_Index").intValue;

            // Glyph -> GlyphMetrics
            SerializedProperty srcGlyphMetrics = srcGlyph.FindPropertyRelative("m_Metrics");
            SerializedProperty dstGlyphMetrics = dstGlyph.FindPropertyRelative("m_Metrics");

            dstGlyphMetrics.FindPropertyRelative("m_Width").floatValue = srcGlyphMetrics.FindPropertyRelative("m_Width").floatValue;
            dstGlyphMetrics.FindPropertyRelative("m_Height").floatValue = srcGlyphMetrics.FindPropertyRelative("m_Height").floatValue;
            dstGlyphMetrics.FindPropertyRelative("m_HorizontalBearingX").floatValue = srcGlyphMetrics.FindPropertyRelative("m_HorizontalBearingX").floatValue;
            dstGlyphMetrics.FindPropertyRelative("m_HorizontalBearingY").floatValue = srcGlyphMetrics.FindPropertyRelative("m_HorizontalBearingY").floatValue;
            dstGlyphMetrics.FindPropertyRelative("m_HorizontalAdvance").floatValue = srcGlyphMetrics.FindPropertyRelative("m_HorizontalAdvance").floatValue;

            // Glyph -> GlyphRect
            SerializedProperty srcGlyphRect = srcGlyph.FindPropertyRelative("m_GlyphRect");
            SerializedProperty dstGlyphRect = dstGlyph.FindPropertyRelative("m_GlyphRect");

            dstGlyphRect.FindPropertyRelative("m_X").intValue = srcGlyphRect.FindPropertyRelative("m_X").intValue;
            dstGlyphRect.FindPropertyRelative("m_Y").intValue = srcGlyphRect.FindPropertyRelative("m_Y").intValue;
            dstGlyphRect.FindPropertyRelative("m_Width").intValue = srcGlyphRect.FindPropertyRelative("m_Width").intValue;
            dstGlyphRect.FindPropertyRelative("m_Height").intValue = srcGlyphRect.FindPropertyRelative("m_Height").intValue;

            dstGlyph.FindPropertyRelative("m_Scale").floatValue = srcGlyph.FindPropertyRelative("m_Scale").floatValue;
            dstGlyph.FindPropertyRelative("m_AtlasIndex").intValue = srcGlyph.FindPropertyRelative("m_AtlasIndex").intValue;
        }

        void CopyCharacterSerializedProperty(SerializedProperty source, ref SerializedProperty target)
        {
            // TODO : Should make a generic function which copies each of the properties.
            int unicode = source.FindPropertyRelative("m_Unicode").intValue;
            target.FindPropertyRelative("m_Unicode").intValue = unicode;

            int srcGlyphIndex = source.FindPropertyRelative("m_GlyphIndex").intValue;
            target.FindPropertyRelative("m_GlyphIndex").intValue = srcGlyphIndex;

            target.FindPropertyRelative("m_Scale").floatValue = source.FindPropertyRelative("m_Scale").floatValue;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        void SearchGlyphTable(string searchPattern, ref List<int> searchResults)
        {
            if (searchResults == null) searchResults = new List<int>();

            searchResults.Clear();

            int arraySize = m_GlyphTable_prop.arraySize;

            for (int i = 0; i < arraySize; i++)
            {
                SerializedProperty sourceGlyph = m_GlyphTable_prop.GetArrayElementAtIndex(i);

                int id = sourceGlyph.FindPropertyRelative("m_Index").intValue;

                // Check for potential match against a character.
                //if (searchPattern.Length == 1 && id == searchPattern[0])
                //    searchResults.Add(i);

                // Check for potential match against decimal id
                if (id.ToString().Contains(searchPattern))
                    searchResults.Add(i);

                //if (id.ToString("x").Contains(searchPattern))
                //    searchResults.Add(i);

                //if (id.ToString("X").Contains(searchPattern))
                //    searchResults.Add(i);
            }
        }

        void SearchCharacterTable(string searchPattern, ref List<int> searchResults)
        {
            if (searchResults == null) searchResults = new List<int>();

            searchResults.Clear();

            int arraySize = m_CharacterTable_prop.arraySize;

            for (int i = 0; i < arraySize; i++)
            {
                SerializedProperty sourceCharacter = m_CharacterTable_prop.GetArrayElementAtIndex(i);

                int id = sourceCharacter.FindPropertyRelative("m_Unicode").intValue;

                // Check for potential match against a character.
                if (searchPattern.Length == 1 && id == searchPattern[0])
                    searchResults.Add(i);
                else if (id.ToString("x").Contains(searchPattern))
                    searchResults.Add(i);
                else if (id.ToString("X").Contains(searchPattern))
                    searchResults.Add(i);

                // Check for potential match against decimal id
                //if (id.ToString().Contains(searchPattern))
                //    searchResults.Add(i);
            }
        }

        void SearchLigatureTable(string searchPattern, ref List<int> searchResults)
        {
            if (searchResults == null) searchResults = new List<int>();

            searchResults.Clear();

            // Lookup glyph index of potential characters contained in the search pattern.
            uint firstGlyphIndex = 0;
            Character firstCharacterSearch;

            if (searchPattern.Length > 0 && m_fontAsset.characterLookupTable.TryGetValue(searchPattern[0], out firstCharacterSearch))
                firstGlyphIndex = firstCharacterSearch.glyphIndex;

            uint secondGlyphIndex = 0;
            Character secondCharacterSearch;

            if (searchPattern.Length > 1 && m_fontAsset.characterLookupTable.TryGetValue(searchPattern[1], out secondCharacterSearch))
                secondGlyphIndex = secondCharacterSearch.glyphIndex;

            int arraySize = m_MarkToBaseAdjustmentRecords_prop.arraySize;

            for (int i = 0; i < arraySize; i++)
            {
                SerializedProperty record = m_MarkToBaseAdjustmentRecords_prop.GetArrayElementAtIndex(i);

                int baseGlyphIndex = record.FindPropertyRelative("m_BaseGlyphID").intValue;
                int markGlyphIndex = record.FindPropertyRelative("m_MarkGlyphID").intValue;

                if (firstGlyphIndex == baseGlyphIndex && secondGlyphIndex == markGlyphIndex)
                    searchResults.Add(i);
                else if (searchPattern.Length == 1 && (firstGlyphIndex == baseGlyphIndex || firstGlyphIndex == markGlyphIndex))
                    searchResults.Add(i);
                else if (baseGlyphIndex.ToString().Contains(searchPattern))
                    searchResults.Add(i);
                else if (markGlyphIndex.ToString().Contains(searchPattern))
                    searchResults.Add(i);
            }
        }

        void SearchKerningTable(string searchPattern, ref List<int> searchResults)
        {
            if (searchResults == null) searchResults = new List<int>();

            searchResults.Clear();

            // Lookup glyph index of potential characters contained in the search pattern.
            uint firstGlyphIndex = 0;
            Character firstCharacterSearch;

            if (searchPattern.Length > 0 && m_fontAsset.characterLookupTable.TryGetValue(searchPattern[0], out firstCharacterSearch))
                firstGlyphIndex = firstCharacterSearch.glyphIndex;

            uint secondGlyphIndex = 0;
            Character secondCharacterSearch;

            if (searchPattern.Length > 1 && m_fontAsset.characterLookupTable.TryGetValue(searchPattern[1], out secondCharacterSearch))
                secondGlyphIndex = secondCharacterSearch.glyphIndex;

            int arraySize = m_GlyphPairAdjustmentRecords_prop.arraySize;

            for (int i = 0; i < arraySize; i++)
            {
                SerializedProperty record = m_GlyphPairAdjustmentRecords_prop.GetArrayElementAtIndex(i);

                SerializedProperty firstAdjustmentRecord = record.FindPropertyRelative("m_FirstAdjustmentRecord");
                SerializedProperty secondAdjustmentRecord = record.FindPropertyRelative("m_SecondAdjustmentRecord");

                int firstGlyph = firstAdjustmentRecord.FindPropertyRelative("m_GlyphIndex").intValue;
                int secondGlyph = secondAdjustmentRecord.FindPropertyRelative("m_GlyphIndex").intValue;

                if (firstGlyphIndex == firstGlyph && secondGlyphIndex == secondGlyph)
                    searchResults.Add(i);
                else if (searchPattern.Length == 1 && (firstGlyphIndex == firstGlyph || firstGlyphIndex == secondGlyph))
                    searchResults.Add(i);
                else if (firstGlyph.ToString().Contains(searchPattern))
                    searchResults.Add(i);
                else if (secondGlyph.ToString().Contains(searchPattern))
                    searchResults.Add(i);
            }
        }

        void SearchMarkToBaseTable(string searchPattern, ref List<int> searchResults)
        {
            if (searchResults == null) searchResults = new List<int>();

            searchResults.Clear();

            // Lookup glyph index of potential characters contained in the search pattern.
            uint firstGlyphIndex = 0;
            Character firstCharacterSearch;

            if (searchPattern.Length > 0 && m_fontAsset.characterLookupTable.TryGetValue(searchPattern[0], out firstCharacterSearch))
                firstGlyphIndex = firstCharacterSearch.glyphIndex;

            uint secondGlyphIndex = 0;
            Character secondCharacterSearch;

            if (searchPattern.Length > 1 && m_fontAsset.characterLookupTable.TryGetValue(searchPattern[1], out secondCharacterSearch))
                secondGlyphIndex = secondCharacterSearch.glyphIndex;

            int arraySize = m_MarkToBaseAdjustmentRecords_prop.arraySize;

            for (int i = 0; i < arraySize; i++)
            {
                SerializedProperty record = m_MarkToBaseAdjustmentRecords_prop.GetArrayElementAtIndex(i);

                int baseGlyphIndex = record.FindPropertyRelative("m_BaseGlyphID").intValue;
                int markGlyphIndex = record.FindPropertyRelative("m_MarkGlyphID").intValue;

                if (firstGlyphIndex == baseGlyphIndex && secondGlyphIndex == markGlyphIndex)
                    searchResults.Add(i);
                else if (searchPattern.Length == 1 && (firstGlyphIndex == baseGlyphIndex || firstGlyphIndex == markGlyphIndex))
                    searchResults.Add(i);
                else if (baseGlyphIndex.ToString().Contains(searchPattern))
                    searchResults.Add(i);
                else if (markGlyphIndex.ToString().Contains(searchPattern))
                    searchResults.Add(i);
            }
        }

        void SearchMarkToMarkTable(string searchPattern, ref List<int> searchResults)
        {
            if (searchResults == null) searchResults = new List<int>();

            searchResults.Clear();

            // Lookup glyph index of potential characters contained in the search pattern.
            uint firstGlyphIndex = 0;
            Character firstCharacterSearch;

            if (searchPattern.Length > 0 && m_fontAsset.characterLookupTable.TryGetValue(searchPattern[0], out firstCharacterSearch))
                firstGlyphIndex = firstCharacterSearch.glyphIndex;

            uint secondGlyphIndex = 0;
            Character secondCharacterSearch;

            if (searchPattern.Length > 1 && m_fontAsset.characterLookupTable.TryGetValue(searchPattern[1], out secondCharacterSearch))
                secondGlyphIndex = secondCharacterSearch.glyphIndex;

            int arraySize = m_MarkToMarkAdjustmentRecords_prop.arraySize;

            for (int i = 0; i < arraySize; i++)
            {
                SerializedProperty record = m_MarkToMarkAdjustmentRecords_prop.GetArrayElementAtIndex(i);

                int baseGlyphIndex = record.FindPropertyRelative("m_BaseMarkGlyphID").intValue;
                int markGlyphIndex = record.FindPropertyRelative("m_CombiningMarkGlyphID").intValue;

                if (firstGlyphIndex == baseGlyphIndex && secondGlyphIndex == markGlyphIndex)
                    searchResults.Add(i);
                else if (searchPattern.Length == 1 && (firstGlyphIndex == baseGlyphIndex || firstGlyphIndex == markGlyphIndex))
                    searchResults.Add(i);
                else if (baseGlyphIndex.ToString().Contains(searchPattern))
                    searchResults.Add(i);
                else if (markGlyphIndex.ToString().Contains(searchPattern))
                    searchResults.Add(i);
            }
        }

        void DrawMarkToBasePreview(int selectedRecord, Rect rect)
        {
            MarkToBaseAdjustmentRecord adjustmentRecord = m_fontAsset.fontFeatureTable.m_MarkToBaseAdjustmentRecords[selectedRecord];

            uint baseGlyphIndex = adjustmentRecord.baseGlyphID;
            uint markGlyphIndex = adjustmentRecord.markGlyphID;

            if (baseGlyphIndex == 0 || markGlyphIndex == 0)
                return;

            float lineHeight = m_fontAsset.faceInfo.ascentLine - m_fontAsset.faceInfo.descentLine;
            float scale = rect.width < rect.height ? rect.width / lineHeight : rect.height / lineHeight;
            scale *= 0.9f;

            Glyph baseGlyph;
            m_fontAsset.glyphLookupTable.TryGetValue(baseGlyphIndex, out baseGlyph);

            if (baseGlyph == null)
                return;

            Rect center = new Rect(rect.x + rect.width / 2, rect.y + rect.height / 2, rect.width, rect.height);

            Vector2 origin = new Vector2(center.x, center.y);
            origin.x = origin.x - (baseGlyph.metrics.horizontalBearingX + baseGlyph.metrics.width / 2) * scale;
            origin.y = origin.y + (baseGlyph.metrics.horizontalBearingY - baseGlyph.metrics.height / 2) * scale;

            // Draw Baseline
            DrawBaseline(origin, rect.width, Color.grey);

            // Draw Origin
            DrawAnchorPoint(origin, Color.yellow);

            Rect baseGlyphPosition = new Rect(origin.x + baseGlyph.metrics.horizontalBearingX * scale, origin.y - baseGlyph.metrics.horizontalBearingY * scale , rect.width, rect.height);

            DrawGlyph(baseGlyph, baseGlyphPosition, scale);

            Vector2 baseAnchorPosition = new Vector2(origin.x + adjustmentRecord.baseGlyphAnchorPoint.xCoordinate * scale, origin.y - adjustmentRecord.baseGlyphAnchorPoint.yCoordinate * scale);

            DrawAnchorPoint(baseAnchorPosition, Color.green);

            // Draw Mark
            if (m_fontAsset.glyphLookupTable.ContainsKey(markGlyphIndex))
            {
                Glyph markGlyph = m_fontAsset.glyphLookupTable[markGlyphIndex];

                Rect markGlyphPosition = new Rect(baseAnchorPosition.x + (markGlyph.metrics.horizontalBearingX - adjustmentRecord.markPositionAdjustment.xPositionAdjustment) * scale, baseAnchorPosition.y + (adjustmentRecord.markPositionAdjustment.yPositionAdjustment - markGlyph.metrics.horizontalBearingY) * scale, markGlyph.metrics.width, markGlyph.metrics.height);

                // Draw Mark Origin
                DrawGlyph(markGlyph, markGlyphPosition, scale);
            }
        }

        void DrawMarkToMarkPreview(int selectedRecord, Rect rect)
        {
            MarkToMarkAdjustmentRecord adjustmentRecord = m_fontAsset.fontFeatureTable.m_MarkToMarkAdjustmentRecords[selectedRecord];

            uint baseGlyphIndex = adjustmentRecord.baseMarkGlyphID;
            uint markGlyphIndex = adjustmentRecord.combiningMarkGlyphID;

            if (baseGlyphIndex == 0 || markGlyphIndex == 0)
                return;

            float lineHeight = m_fontAsset.faceInfo.ascentLine - m_fontAsset.faceInfo.descentLine;
            float scale = rect.width < rect.height ? rect.width / lineHeight : rect.height / lineHeight;
            scale *= 0.9f;

            Glyph baseGlyph;
            m_fontAsset.glyphLookupTable.TryGetValue(baseGlyphIndex, out baseGlyph);

            if (baseGlyph == null)
                return;

            Rect center = new Rect(rect.x + rect.width / 2, rect.y + rect.height / 2, rect.width, rect.height);

            Vector2 origin = new Vector2(center.x, center.y);
            origin.x = origin.x - (baseGlyph.metrics.horizontalBearingX + baseGlyph.metrics.width / 2) * scale;
            origin.y = origin.y + (baseGlyph.metrics.horizontalBearingY - baseGlyph.metrics.height / 2) * scale;

            // Draw Baseline
            DrawBaseline(origin, rect.width, Color.grey);

            // Draw Origin
            DrawAnchorPoint(origin, Color.yellow);

            Rect baseGlyphPosition = new Rect(origin.x + baseGlyph.metrics.horizontalBearingX * scale, origin.y - baseGlyph.metrics.horizontalBearingY * scale , rect.width, rect.height);

            DrawGlyph(baseGlyph, baseGlyphPosition, scale);

            Vector2 baseAnchorPosition = new Vector2(origin.x + adjustmentRecord.baseMarkGlyphAnchorPoint.xCoordinate * scale, origin.y - adjustmentRecord.baseMarkGlyphAnchorPoint.yCoordinate * scale);

            DrawAnchorPoint(baseAnchorPosition, Color.green);

            // Draw Mark Glyph
            if (m_fontAsset.glyphLookupTable.ContainsKey(markGlyphIndex))
            {
                Glyph markGlyph = m_fontAsset.glyphLookupTable[markGlyphIndex];

                Rect markGlyphPosition = new Rect(baseAnchorPosition.x + (markGlyph.metrics.horizontalBearingX - adjustmentRecord.combiningMarkPositionAdjustment.xPositionAdjustment) * scale, baseAnchorPosition.y + (adjustmentRecord.combiningMarkPositionAdjustment.yPositionAdjustment - markGlyph.metrics.horizontalBearingY) * scale, markGlyph.metrics.width, markGlyph.metrics.height);

                DrawGlyph(markGlyph, markGlyphPosition, scale);
            }
        }

        void DrawBaseline(Vector2 position, float width, Color color)
        {
            Handles.color = color;

            // Horizontal line
            Handles.DrawLine(new Vector2(0f, position.y), new Vector2(width, position.y));
        }

        void DrawAnchorPoint(Vector2 position, Color color)
        {
            Handles.color = color;

            // Horizontal line
            Handles.DrawLine(new Vector2(position.x - 25, position.y), new Vector2(position.x + 25, position.y));

            // Vertical line
            Handles.DrawLine(new Vector2(position.x, position.y - 25), new Vector2(position.x, position.y + 25));
        }

        void DrawGlyph(Glyph glyph, Rect position, float scale)
        {
            // Get the atlas index of the glyph and lookup its atlas texture
            int atlasIndex = glyph.atlasIndex;
            Texture2D atlasTexture = m_fontAsset.atlasTextures.Length > atlasIndex ? m_fontAsset.atlasTextures[atlasIndex] : null;

            if (atlasTexture == null)
                return;

            Material mat;
            if (((GlyphRasterModes)m_fontAsset.atlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP)
            {
                if (m_fontAsset.atlasRenderMode == GlyphRenderMode.COLOR || m_fontAsset.atlasRenderMode == GlyphRenderMode.COLOR_HINTED)
                    mat = internalRGBABitmapMaterial;
                else
                    mat = internalBitmapMaterial;

                if (mat == null)
                    return;

                mat.mainTexture = atlasTexture;
            }
            else
            {
                mat = EditorShaderUtilities.internalSDFMaterial;

                if (mat == null)
                    return;

                mat.mainTexture = atlasTexture;
                mat.SetFloat(TextShaderUtilities.ID_GradientScale, m_fontAsset.atlasPadding + 1);
            }

            GlyphRect glyphRect = glyph.glyphRect;

            int padding = m_fontAsset.atlasPadding;

            int glyphOriginX = glyphRect.x - padding;
            int glyphOriginY = glyphRect.y - padding;
            int glyphWidth = glyphRect.width + padding * 2;
            int glyphHeight = glyphRect.height + padding * 2;

            // Compute the normalized texture coordinates
            Rect texCoords = new Rect((float)glyphOriginX / atlasTexture.width, (float)glyphOriginY / atlasTexture.height, (float)glyphWidth / atlasTexture.width, (float)glyphHeight / atlasTexture.height);

            if (Event.current.type == EventType.Repaint)
            {
                // Draw glyph from atlas texture.
                Rect glyphDrawPosition = new Rect(position.x - padding * scale, position.y - padding * scale, position.width, position.height);

                //glyphDrawPosition.x += (glyphDrawPosition.width - glyphWidth * scale); // / 2;
                //glyphDrawPosition.y += (glyphDrawPosition.height - glyphHeight * scale); // / 2;
                glyphDrawPosition.width = glyphWidth * scale;
                glyphDrawPosition.height = glyphHeight * scale;

                // Could switch to using the default material of the font asset which would require passing scale to the shader.
                Graphics.DrawTexture(glyphDrawPosition, atlasTexture, texCoords, 0, 0, 0, 0, new Color(0.8f, 0.8f, 0.8f), mat);
            }
        }


        internal void UpdateSourceFontFile(Font sourceFont)
        {
            m_GenerationSettings.sourceFont = m_fontAsset.SourceFont_EditorRef;
            m_fontAsset.SourceFont_EditorRef = sourceFont;
            m_FontFaces = GetFontFaces(0);
            m_FaceInfoDirty = true;
            m_DisplayDestructiveChangeWarning = true;
        }

        internal void UpdateFontFaceIndex(int index)
        {
            var faceInfo = m_fontAsset.faceInfo;
            faceInfo.faceIndex = index;
            m_fontAsset.faceInfo = faceInfo;
            m_MaterialPresetsRequireUpdate = true;
            m_DisplayDestructiveChangeWarning = true;
            m_FaceInfoDirty = true;
        }

        // 0 = Static, 1 = Dynamic, 2 = Dynamic OS
        internal void UpdateAtlasPopulationMode(int populationMode)
        {
            serializedObject.ApplyModifiedProperties();
            m_fontAsset.atlasPopulationMode = (AtlasPopulationMode)populationMode;

            // Static font asset
            if (populationMode == 0)
            {
                m_fontAsset.sourceFontFile = null;

                //Set atlas textures to non readable.
                for (int i = 0; i < m_fontAsset.atlasTextures.Length; i++)
                {
                    Texture2D tex = m_fontAsset.atlasTextures[i];

                    if (tex != null && tex.isReadable)
                        FontEngineEditorUtilities.SetAtlasTextureIsReadable(tex, false);
                }

                //Debug.Log("Atlas Population mode set to [Static].");
            }
            else // Dynamic font asset
            {
                if (m_fontAsset.m_SourceFontFile_EditorRef.dynamic == false)
                {
                    Debug.LogWarning("Please set the [" + m_fontAsset.name + "] font to dynamic mode as this is required for Dynamic SDF support.", m_fontAsset.m_SourceFontFile_EditorRef);
                    m_AtlasPopulationMode_prop.intValue = 0;
                    m_fontAsset.atlasPopulationMode = (AtlasPopulationMode)populationMode;

                    serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    m_fontAsset.sourceFontFile = m_fontAsset.m_SourceFontFile_EditorRef;

                    // Set atlas textures to readable.
                    for (int i = 0; i < m_fontAsset.atlasTextures.Length; i++)
                    {
                        Texture2D tex = m_fontAsset.atlasTextures[i];

                        if (tex != null && tex.isReadable == false)
                            FontEngineEditorUtilities.SetAtlasTextureIsReadable(tex, true);
                    }

                    //Debug.Log("Atlas Population mode set to [" + (m_AtlasPopulationMode_prop.intValue == 1 ? "Dynamic" : "Dynamic OS") + "].");
                }

                // Dynamic OS font asset
                if (populationMode == 2)
                    m_fontAsset.sourceFontFile = null;
            }

            serializedObject.Update();
            isAssetDirty = true;
        }

        internal void ApplyDestructiveChanges()
        {
            m_DisplayDestructiveChangeWarning = false;

            // Update face info if  sampling point size was changed.
            if (m_GenerationSettings.pointSize != m_SamplingPointSize_prop.floatValue || m_FaceInfoDirty)
            {
                LoadFontFace(m_SamplingPointSize_prop.floatValue, m_FontFaceIndex_prop.intValue);
                m_fontAsset.faceInfo = FontEngine.GetFaceInfo();
                m_FaceInfoDirty = false;
            }

            Material mat = m_fontAsset.material;

            // Update material
            mat.SetFloat(TextShaderUtilities.ID_TextureWidth, m_AtlasWidth_prop.intValue);
            mat.SetFloat(TextShaderUtilities.ID_TextureHeight, m_AtlasHeight_prop.intValue);

            if (mat.HasProperty(TextShaderUtilities.ID_GradientScale))
                mat.SetFloat(TextShaderUtilities.ID_GradientScale, m_AtlasPadding_prop.intValue + 1);

            // Update material presets if any of the relevant properties have been changed.
            if (m_MaterialPresetsRequireUpdate)
            {
                m_MaterialPresetsRequireUpdate = false;

                Material[] materialPresets = TextCoreEditorUtilities.FindMaterialReferences(m_fontAsset);
                for (int i = 0; i < materialPresets.Length; i++)
                {
                    mat = materialPresets[i];

                    mat.SetFloat(TextShaderUtilities.ID_TextureWidth, m_AtlasWidth_prop.intValue);
                    mat.SetFloat(TextShaderUtilities.ID_TextureHeight, m_AtlasHeight_prop.intValue);

                    if (mat.HasProperty(TextShaderUtilities.ID_GradientScale))
                        mat.SetFloat(TextShaderUtilities.ID_GradientScale, m_AtlasPadding_prop.intValue + 1);
                }
            }

            m_fontAsset.UpdateFontAssetData();
            GUIUtility.keyboardControl = 0;
            isAssetDirty = true;

            // Update Font Asset Creation Settings to reflect new changes.
            UpdateFontAssetCreationSettings();

            // TODO: Clear undo buffers.
            //Undo.ClearUndo(m_fontAsset);
        }

        internal void RevertDestructiveChanges()
        {
            m_DisplayDestructiveChangeWarning = false;
            RestoreGenerationSettings();
            GUIUtility.keyboardControl = 0;

            // TODO: Clear undo buffers.
            //Undo.ClearUndo(m_fontAsset);
        }
    }
}
