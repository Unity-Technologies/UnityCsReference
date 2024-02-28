// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TextCore.Text;

using UnityEngine.TextCore.LowLevel;
using UnityEditor.TextCore.LowLevel;
using Object = UnityEngine.Object;
using TextAsset = UnityEngine.TextAsset;

using FaceInfo = UnityEngine.TextCore.FaceInfo;
using Glyph = UnityEngine.TextCore.Glyph;
using GlyphRect = UnityEngine.TextCore.GlyphRect;
using GlyphMetrics = UnityEngine.TextCore.GlyphMetrics;


namespace UnityEditor.TextCore.Text
{
    public class FontAssetCreatorWindow : EditorWindow
    {
        [MenuItem("Window/Text/Font Asset Creator", false, 2025)]
        internal static void ShowFontAtlasCreatorWindow()
        {
            var window = GetWindow<FontAssetCreatorWindow>();
            window.titleContent = new GUIContent("Font Asset Creator");
            window.Focus();

            // Make sure TMP Essential Resources have been imported.
            window.CheckEssentialResources();
        }

        public static void ShowFontAtlasCreatorWindow(Font font)
        {
            var window = GetWindow<FontAssetCreatorWindow>();

            window.titleContent = new GUIContent("Font Asset Creator");
            window.Focus();

            window.ClearGeneratedData();
            window.m_LegacyFontAsset = null;
            window.m_SelectedFontAsset = null;

            // Override selected font asset
            window.m_SourceFont = font;

            // Make sure TMP Essential Resources have been imported.
            window.CheckEssentialResources();
        }

        public static void ShowFontAtlasCreatorWindow(FontAsset fontAsset)
        {
            var window = GetWindow<FontAssetCreatorWindow>();

            window.titleContent = new GUIContent("Font Asset Creator");
            window.Focus();

            // Clear any previously generated data
            window.ClearGeneratedData();
            window.m_LegacyFontAsset = null;

            // Load font asset creation settings if we have valid settings
            if (string.IsNullOrEmpty(fontAsset.fontAssetCreationEditorSettings.sourceFontFileGUID) == false)
            {
                window.LoadFontCreationSettings(fontAsset.fontAssetCreationEditorSettings);

                // Override settings to inject character list from font asset
                // window.m_CharacterSetSelectionMode = 6;
                // window.m_CharacterSequence = TextCoreEditorUtilities.GetUnicodeCharacterSequence(FontAsset.GetCharactersArray(fontAsset));


                window.m_ReferencedFontAsset = fontAsset;
                window.m_SavedFontAtlas = fontAsset.atlasTexture;
            }
            else
            {
                window.m_WarningMessage = "Font Asset [" + fontAsset.name + "] does not contain any previous \"Font Asset Creation Settings\". This usually means [" + fontAsset.name + "] was created before this new functionality was added.";
                window.m_SourceFont = null;
                window.m_LegacyFontAsset = fontAsset;
            }

            // Even if we don't have any saved generation settings, we still want to pre-select the source font file.
            window.m_SelectedFontAsset = fontAsset;

            // Make sure TMP Essential Resources have been imported.
            window.CheckEssentialResources();
        }

        [System.Serializable]
        class FontAssetCreationSettingsContainer
        {
            public List<FontAssetCreationEditorSettings> fontAssetCreationSettings;
        }

        FontAssetCreationSettingsContainer m_FontAssetCreationSettingsContainer;

        //static readonly string[] m_FontCreationPresets = new string[] { "Recent 1", "Recent 2", "Recent 3", "Recent 4" };
        int m_FontAssetCreationSettingsCurrentIndex = 0;

        const string k_FontAssetCreationSettingsContainerKey = "TextMeshPro.FontAssetCreator.RecentFontAssetCreationSettings.Container";
        const string k_FontAssetCreationSettingsCurrentIndexKey = "TextMeshPro.FontAssetCreator.RecentFontAssetCreationSettings.CurrentIndex";
        const float k_TwoColumnControlsWidth = 335f;

        // Diagnostics
        System.Diagnostics.Stopwatch m_StopWatch;
        double m_GlyphPackingGenerationTime;
        double m_GlyphRenderingGenerationTime;

        string[] m_FontSizingOptions = { "Auto Sizing", "Custom Size" };
        int m_PointSizeSamplingMode;
        string[] m_FontResolutionLabels = { "8", "16", "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" };
        int[] m_FontAtlasResolutions = { 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
        string[] m_FontCharacterSets = { "ASCII", "Extended ASCII", "ASCII Lowercase", "ASCII Uppercase", "Numbers + Symbols", "Custom Range", "Unicode Range (Hex)", "Custom Characters", "Characters from File" };
        enum FontPackingModes { Fast = 0, Optimum = 4 };
        FontPackingModes m_PackingMode = FontPackingModes.Fast;

        int m_CharacterSetSelectionMode;

        string m_CharacterSequence = "";
        string m_OutputFeedback = "";
        string m_WarningMessage;
        int m_CharacterCount;
        Vector2 m_ScrollPosition;
        Vector2 m_OutputScrollPosition;

        bool m_IsRepaintNeeded;

        float m_AtlasGenerationProgress;
        string m_AtlasGenerationProgressLabel = string.Empty;
        bool m_IsGlyphPackingDone;
        bool m_IsGlyphRenderingDone;
        bool m_IsRenderingDone;
        bool m_IsProcessing;
        bool m_IsGenerationDisabled;
        bool m_IsGenerationCancelled;

        bool m_IsFontAtlasInvalid;
        Font m_SourceFont;
        int m_SourceFontFaceIndex;
        private string[] m_SourceFontFaces = new string[0];
        FontAsset m_SelectedFontAsset;
        FontAsset m_LegacyFontAsset;
        FontAsset m_ReferencedFontAsset;

        TextAsset m_CharactersFromFile;
        int m_PointSize;
        float m_PaddingFieldValue = 10;
        int m_Padding;

        enum PaddingMode { Undefined = 0, Percentage = 1, Pixel = 2 };

        string[] k_PaddingOptionLabels = { "%", "px" };
        private PaddingMode m_PaddingMode = PaddingMode.Percentage;

        GlyphRenderMode m_GlyphRenderMode = GlyphRenderMode.SDFAA;
        int m_AtlasWidth = 512;
        int m_AtlasHeight = 512;
        byte[] m_AtlasTextureBuffer;
        Texture2D m_FontAtlasTexture;
        Texture2D m_GlyphRectPreviewTexture;
        Texture2D m_SavedFontAtlas;

        //
        List<Glyph> m_FontGlyphTable = new List<Glyph>();
        List<Character> m_FontCharacterTable = new List<Character>();

        Dictionary<uint, uint> m_CharacterLookupMap = new Dictionary<uint, uint>();
        Dictionary<uint, List<uint>> m_GlyphLookupMap = new Dictionary<uint, List<uint>>();

        List<Glyph> m_GlyphsToPack = new List<Glyph>();
        List<Glyph> m_GlyphsPacked = new List<Glyph>();
        List<GlyphRect> m_FreeGlyphRects = new List<GlyphRect>();
        List<GlyphRect> m_UsedGlyphRects = new List<GlyphRect>();
        List<Glyph> m_GlyphsToRender = new List<Glyph>();
        List<uint> m_AvailableGlyphsToAdd = new List<uint>();
        List<uint> m_MissingCharacters = new List<uint>();
        List<uint> m_ExcludedCharacters = new List<uint>();

        private FaceInfo m_FaceInfo;

        bool m_IncludeFontFeatures;


        public void OnEnable()
        {
            // Used for Diagnostics
            m_StopWatch = new System.Diagnostics.Stopwatch();

            // Set Editor window size.
            minSize = new Vector2(315, minSize.y);

            // Initialize & Get shader property IDs.
            TextShaderUtilities.GetShaderPropertyIDs();

            // Load last selected preset if we are not already in the process of regenerating an existing font asset (via the Context menu)
            if (EditorPrefs.HasKey(k_FontAssetCreationSettingsContainerKey))
            {
                if (m_FontAssetCreationSettingsContainer == null)
                    m_FontAssetCreationSettingsContainer = JsonUtility.FromJson<FontAssetCreationSettingsContainer>(EditorPrefs.GetString(k_FontAssetCreationSettingsContainerKey));

                if (m_FontAssetCreationSettingsContainer.fontAssetCreationSettings != null && m_FontAssetCreationSettingsContainer.fontAssetCreationSettings.Count > 0)
                {
                    // Load Font Asset Creation Settings preset.
                    if (EditorPrefs.HasKey(k_FontAssetCreationSettingsCurrentIndexKey))
                        m_FontAssetCreationSettingsCurrentIndex = EditorPrefs.GetInt(k_FontAssetCreationSettingsCurrentIndexKey);

                    LoadFontCreationSettings(m_FontAssetCreationSettingsContainer.fontAssetCreationSettings[m_FontAssetCreationSettingsCurrentIndex]);
                }
            }

            // Get potential font face and styles for the current font.
            if (m_SourceFont != null)
                m_SourceFontFaces = GetFontFaces();

            ClearGeneratedData();
        }

        public void OnDisable()
        {
            //Debug.Log("TextMeshPro Editor Window has been disabled.");

            // Destroy Engine only if it has been initialized already
            FontEngine.DestroyFontEngine();

            ClearGeneratedData();

            // Remove Glyph Report if one was created.
            if (File.Exists("Assets/TextMesh Pro/Glyph Report.txt"))
            {
                File.Delete("Assets/TextMesh Pro/Glyph Report.txt");
                File.Delete("Assets/TextMesh Pro/Glyph Report.txt.meta");

                AssetDatabase.Refresh();
            }

            // Save Font Asset Creation Settings Index
            SaveCreationSettingsToEditorPrefs(SaveFontCreationSettings());
            EditorPrefs.SetInt(k_FontAssetCreationSettingsCurrentIndexKey, m_FontAssetCreationSettingsCurrentIndex);

            // Unregister to event
            TextEventManager.RESOURCE_LOAD_EVENT.Remove(ON_RESOURCES_LOADED);

            Resources.UnloadUnusedAssets();
        }

        // Event received when TMP resources have been loaded.
        void ON_RESOURCES_LOADED()
        {
            TextEventManager.RESOURCE_LOAD_EVENT.Remove(ON_RESOURCES_LOADED);

            m_IsGenerationDisabled = false;
        }

        // Make sure TMP Essential Resources have been imported.
        void CheckEssentialResources()
        {
            // TODO: Update this
            // if (TMP_Settings.instance == null)
            // {
            //     if (m_IsGenerationDisabled == false)
            //         TMPro_EventManager.RESOURCE_LOAD_EVENT.Add(ON_RESOURCES_LOADED);
            //
            //     m_IsGenerationDisabled = true;
            // }
        }

        public void OnGUI()
        {
            GUILayout.BeginHorizontal();

            DrawControls();

            if (position.width > position.height && position.width > k_TwoColumnControlsWidth)
                DrawPreview();

            GUILayout.EndHorizontal();
        }

        public void Update()
        {
            if (m_IsRepaintNeeded)
            {
                //Debug.Log("Repainting...");
                m_IsRepaintNeeded = false;
                Repaint();
            }

            // Update Progress bar is we are Rendering a Font.
            if (m_IsProcessing)
            {
                m_AtlasGenerationProgress = FontEngine.generationProgress;

                m_IsRepaintNeeded = true;
            }

            if (m_IsGlyphPackingDone)
            {
                UpdateRenderFeedbackWindow();

                if (m_IsGenerationCancelled == false)
                {
                    DrawGlyphRectPreviewTexture();
                    Debug.Log("Glyph packing completed in: " + m_GlyphPackingGenerationTime.ToString("0.000 ms."));
                }

                m_IsGlyphPackingDone = false;
            }

            if (m_IsGlyphRenderingDone)
            {
                Debug.Log("Font Atlas generation completed in: " + m_GlyphRenderingGenerationTime.ToString("0.000 ms."));
                m_IsGlyphRenderingDone = false;
            }

            // Update Feedback Window & Create Font Texture once Rendering is done.
            if (m_IsRenderingDone)
            {
                m_IsProcessing = false;
                m_IsRenderingDone = false;

                if (m_IsGenerationCancelled == false)
                {
                    m_AtlasGenerationProgress = FontEngine.generationProgress;
                    m_AtlasGenerationProgressLabel = "Generation completed in: " + (m_GlyphPackingGenerationTime + m_GlyphRenderingGenerationTime).ToString("0.00 ms.");

                    UpdateRenderFeedbackWindow();
                    CreateFontAtlasTexture();

                    // If dynamic make readable ...
                    m_FontAtlasTexture.Apply(false, false);
                }
                Repaint();
            }
        }

        /// <summary>
        /// Method which returns the character corresponding to a decimal value.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        static uint[] ParseNumberSequence(string sequence)
        {
            List<uint> unicodeList = new List<uint>();
            string[] sequences = sequence.Split(',');

            foreach (string seq in sequences)
            {
                string[] s1 = seq.Split('-');

                if (s1.Length == 1)
                    try
                    {
                        unicodeList.Add(uint.Parse(s1[0]));
                    }
                    catch
                    {
                        Debug.Log("No characters selected or invalid format.");
                    }
                else
                {
                    for (uint j = uint.Parse(s1[0]); j < uint.Parse(s1[1]) + 1; j++)
                    {
                        unicodeList.Add(j);
                    }
                }
            }

            return unicodeList.ToArray();
        }

        /// <summary>
        /// Method which returns the character (decimal value) from a hex sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        static uint[] ParseHexNumberSequence(string sequence)
        {
            List<uint> unicodeList = new List<uint>();
            string[] sequences = sequence.Split(',');

            foreach (string seq in sequences)
            {
                string[] s1 = seq.Split('-');

                if (s1.Length == 1)
                    try
                    {
                        unicodeList.Add(uint.Parse(s1[0], NumberStyles.AllowHexSpecifier));
                    }
                    catch
                    {
                        Debug.Log("No characters selected or invalid format.");
                    }
                else
                {
                    for (uint j = uint.Parse(s1[0], NumberStyles.AllowHexSpecifier); j < uint.Parse(s1[1], NumberStyles.AllowHexSpecifier) + 1; j++)
                    {
                        unicodeList.Add(j);
                    }
                }
            }

            return unicodeList.ToArray();
        }

        void DrawControls()
        {
            GUILayout.Space(5f);

            if (position.width > position.height && position.width > k_TwoColumnControlsWidth)
            {
                m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Width(315));
            }
            else
            {
                m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            }

            GUILayout.Space(5f);

            GUILayout.Label(m_SelectedFontAsset != null ? string.Format("Font Settings [{0}]", m_SelectedFontAsset.name) : "Font Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUIUtility.labelWidth = 125f;
            EditorGUIUtility.fieldWidth = 5f;

            // Disable Options if already generating a font atlas texture.
            EditorGUI.BeginDisabledGroup(m_IsProcessing);
            {
                // FONT SELECTION
                EditorGUI.BeginChangeCheck();
                m_SourceFont = EditorGUILayout.ObjectField("Source Font", m_SourceFont, typeof(Font), false) as Font;
                if (EditorGUI.EndChangeCheck())
                {
                    m_SelectedFontAsset = null;
                    m_IsFontAtlasInvalid = true;
                    m_SourceFontFaces = GetFontFaces();
                    m_SourceFontFaceIndex = 0;
                }

                // FONT FACE AND STYLE SELECTION
                EditorGUI.BeginChangeCheck();
                GUI.enabled = m_SourceFont != null;
                m_SourceFontFaceIndex = EditorGUILayout.Popup("Font Face", m_SourceFontFaceIndex, m_SourceFontFaces);
                if (EditorGUI.EndChangeCheck())
                {
                    m_SelectedFontAsset = null;
                    m_IsFontAtlasInvalid = true;
                }
                GUI.enabled = true;

                // FONT SIZING
                EditorGUI.BeginChangeCheck();
                if (m_PointSizeSamplingMode == 0)
                {
                    m_PointSizeSamplingMode = EditorGUILayout.Popup("Sampling Point Size", m_PointSizeSamplingMode, m_FontSizingOptions);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    m_PointSizeSamplingMode = EditorGUILayout.Popup("Sampling Point Size", m_PointSizeSamplingMode, m_FontSizingOptions, GUILayout.Width(225));
                    m_PointSize = EditorGUILayout.IntField(m_PointSize);
                    GUILayout.EndHorizontal();
                }
                if (EditorGUI.EndChangeCheck())
                {
                    m_IsFontAtlasInvalid = true;
                }

                // FONT PADDING
                GUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();

                m_PaddingFieldValue = EditorGUILayout.FloatField("Padding", m_PaddingFieldValue);

                int selection = m_PaddingMode == PaddingMode.Undefined || m_PaddingMode == PaddingMode.Pixel ? 1 : 0;
                selection = GUILayout.SelectionGrid(selection, k_PaddingOptionLabels, 2);

                if (m_PaddingMode == PaddingMode.Percentage)
                    m_PaddingFieldValue = (int)(m_PaddingFieldValue + 0.5f);

                if (EditorGUI.EndChangeCheck())
                {
                    m_PaddingMode = (PaddingMode)selection + 1;
                    m_IsFontAtlasInvalid = true;
                }
                GUILayout.EndHorizontal();

                // FONT PACKING METHOD SELECTION
                EditorGUI.BeginChangeCheck();
                m_PackingMode = (FontPackingModes)EditorGUILayout.EnumPopup("Packing Method", m_PackingMode);
                if (EditorGUI.EndChangeCheck())
                {
                    m_IsFontAtlasInvalid = true;
                }

                // FONT ATLAS RESOLUTION SELECTION
                GUILayout.BeginHorizontal();
                GUI.changed = false;

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PrefixLabel("Atlas Resolution");
                m_AtlasWidth = EditorGUILayout.IntPopup(m_AtlasWidth, m_FontResolutionLabels, m_FontAtlasResolutions);
                m_AtlasHeight = EditorGUILayout.IntPopup(m_AtlasHeight, m_FontResolutionLabels, m_FontAtlasResolutions);
                if (EditorGUI.EndChangeCheck())
                {
                    m_IsFontAtlasInvalid = true;
                }

                GUILayout.EndHorizontal();


                // FONT CHARACTER SET SELECTION
                EditorGUI.BeginChangeCheck();
                bool hasSelectionChanged = false;
                m_CharacterSetSelectionMode = EditorGUILayout.Popup("Character Set", m_CharacterSetSelectionMode, m_FontCharacterSets);
                if (EditorGUI.EndChangeCheck())
                {
                    m_CharacterSequence = "";
                    hasSelectionChanged = true;
                    m_IsFontAtlasInvalid = true;
                }

                switch (m_CharacterSetSelectionMode)
                {
                    case 0: // ASCII
                        //characterSequence = "32 - 126, 130, 132 - 135, 139, 145 - 151, 153, 155, 161, 166 - 167, 169 - 174, 176, 181 - 183, 186 - 187, 191, 8210 - 8226, 8230, 8240, 8242 - 8244, 8249 - 8250, 8252 - 8254, 8260, 8286";
                        m_CharacterSequence = "32 - 126, 160, 8203, 8230, 9633";
                        break;

                    case 1: // EXTENDED ASCII
                        m_CharacterSequence = "32 - 126, 160 - 255, 8192 - 8303, 8364, 8482, 9633";
                        // Could add 9632 for missing glyph
                        break;

                    case 2: // Lowercase
                        m_CharacterSequence = "32 - 64, 91 - 126, 160";
                        break;

                    case 3: // Uppercase
                        m_CharacterSequence = "32 - 96, 123 - 126, 160";
                        break;

                    case 4: // Numbers & Symbols
                        m_CharacterSequence = "32 - 64, 91 - 96, 123 - 126, 160";
                        break;

                    case 5: // Custom Range
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.Label("Enter a sequence of decimal values to define the characters to be included in the font asset or retrieve one from another font asset.", TM_EditorStyles.label);
                        GUILayout.Space(10f);

                        EditorGUI.BeginChangeCheck();
                        m_ReferencedFontAsset = EditorGUILayout.ObjectField("Select Font Asset", m_ReferencedFontAsset, typeof(FontAsset), false) as FontAsset;
                        if (EditorGUI.EndChangeCheck() || hasSelectionChanged)
                        {
                            if (m_ReferencedFontAsset != null)
                                m_CharacterSequence = TextCoreEditorUtilities.GetDecimalCharacterSequence(FontAsset.GetCharactersArray(m_ReferencedFontAsset));

                            m_IsFontAtlasInvalid = true;
                        }

                        // Filter out unwanted characters.
                        char chr = Event.current.character;
                        if ((chr < '0' || chr > '9') && (chr < ',' || chr > '-'))
                        {
                            Event.current.character = '\0';
                        }
                        GUILayout.Label("Character Sequence (Decimal)", EditorStyles.boldLabel);
                        EditorGUI.BeginChangeCheck();
                        m_CharacterSequence = EditorGUILayout.TextArea(m_CharacterSequence, TM_EditorStyles.textAreaBoxWindow, GUILayout.Height(120), GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_IsFontAtlasInvalid = true;
                        }

                        EditorGUILayout.EndVertical();
                        break;

                    case 6: // Unicode HEX Range
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.Label("Enter a sequence of Unicode (hex) values to define the characters to be included in the font asset or retrieve one from another font asset.", TM_EditorStyles.label);
                        GUILayout.Space(10f);

                        EditorGUI.BeginChangeCheck();
                        m_ReferencedFontAsset = EditorGUILayout.ObjectField("Select Font Asset", m_ReferencedFontAsset, typeof(FontAsset), false) as FontAsset;
                        if (EditorGUI.EndChangeCheck() || hasSelectionChanged)
                        {
                            if (m_ReferencedFontAsset != null)
                                m_CharacterSequence = TextCoreEditorUtilities.GetUnicodeCharacterSequence(FontAsset.GetCharactersArray(m_ReferencedFontAsset));

                            m_IsFontAtlasInvalid = true;
                        }

                        // Filter out unwanted characters.
                        chr = Event.current.character;
                        if ((chr < '0' || chr > '9') && (chr < 'a' || chr > 'f') && (chr < 'A' || chr > 'F') && (chr < ',' || chr > '-'))
                        {
                            Event.current.character = '\0';
                        }
                        GUILayout.Label("Character Sequence (Hex)", EditorStyles.boldLabel);
                        EditorGUI.BeginChangeCheck();
                        m_CharacterSequence = EditorGUILayout.TextArea(m_CharacterSequence, TM_EditorStyles.textAreaBoxWindow, GUILayout.Height(120), GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_IsFontAtlasInvalid = true;
                        }

                        EditorGUILayout.EndVertical();
                        break;

                    case 7: // Characters from Font Asset
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.Label("Type the characters to be included in the font asset or retrieve them from another font asset.", TM_EditorStyles.label);
                        GUILayout.Space(10f);

                        EditorGUI.BeginChangeCheck();
                        m_ReferencedFontAsset = EditorGUILayout.ObjectField("Select Font Asset", m_ReferencedFontAsset, typeof(FontAsset), false) as FontAsset;
                        if (EditorGUI.EndChangeCheck() || hasSelectionChanged)
                        {
                            if (m_ReferencedFontAsset != null)
                                m_CharacterSequence = FontAsset.GetCharacters(m_ReferencedFontAsset);

                            m_IsFontAtlasInvalid = true;
                        }

                        EditorGUI.indentLevel = 0;

                        GUILayout.Label("Custom Character List", EditorStyles.boldLabel);
                        EditorGUI.BeginChangeCheck();
                        m_CharacterSequence = EditorGUILayout.TextArea(m_CharacterSequence, TM_EditorStyles.textAreaBoxWindow, GUILayout.Height(120), GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_IsFontAtlasInvalid = true;
                        }
                        EditorGUILayout.EndVertical();
                        break;

                    case 8: // Character List from File
                        EditorGUI.BeginChangeCheck();
                        m_CharactersFromFile = EditorGUILayout.ObjectField("Character File", m_CharactersFromFile, typeof(TextAsset), false) as TextAsset;
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_IsFontAtlasInvalid = true;
                        }

                        if (m_CharactersFromFile != null)
                        {
                            Regex rx = new Regex(@"(?<!\\)(?:\\u[0-9a-fA-F]{4}|\\U[0-9a-fA-F]{8})");

                            m_CharacterSequence = rx.Replace(m_CharactersFromFile.text,
                                match =>
                                {
                                    if (match.Value.StartsWith("\\U"))
                                        return char.ConvertFromUtf32(int.Parse(match.Value.Replace("\\U", ""), NumberStyles.HexNumber));

                                    return char.ConvertFromUtf32(int.Parse(match.Value.Replace("\\u", ""), NumberStyles.HexNumber));
                                });
                        }
                        break;
                }

                // FONT STYLE SELECTION
                //GUILayout.BeginHorizontal();
                //EditorGUI.BeginChangeCheck();
                ////m_FontStyle = (FaceStyles)EditorGUILayout.EnumPopup("Font Style", m_FontStyle, GUILayout.Width(225));
                ////m_FontStyleValue = EditorGUILayout.IntField((int)m_FontStyleValue);
                //if (EditorGUI.EndChangeCheck())
                //{
                //    m_IsFontAtlasInvalid = true;
                //}
                //GUILayout.EndHorizontal();

                // Render Mode Selection
                CheckForLegacyGlyphRenderMode();

                EditorGUI.BeginChangeCheck();
                m_GlyphRenderMode = (GlyphRenderMode)EditorGUILayout.EnumPopup("Render Mode", m_GlyphRenderMode);
                if (EditorGUI.EndChangeCheck())
                {
                    m_IsFontAtlasInvalid = true;
                }

                m_IncludeFontFeatures = EditorGUILayout.Toggle("Get Font Features", m_IncludeFontFeatures);

                EditorGUILayout.Space();
            }

            EditorGUI.EndDisabledGroup();

            if (!string.IsNullOrEmpty(m_WarningMessage))
            {
                EditorGUILayout.HelpBox(m_WarningMessage, MessageType.Warning);
            }

            GUI.enabled = m_SourceFont != null && !m_IsProcessing && !m_IsGenerationDisabled; // Enable Preview if we are not already rendering a font.
            if (GUILayout.Button("Generate Font Atlas") && GUI.enabled)
            {
                if (!m_IsProcessing && m_SourceFont != null)
                {
                    DestroyImmediate(m_FontAtlasTexture);
                    DestroyImmediate(m_GlyphRectPreviewTexture);
                    m_FontAtlasTexture = null;
                    m_SavedFontAtlas = null;
                    m_OutputFeedback = string.Empty;

                    // Initialize font engine
                    FontEngineError errorCode = FontEngine.InitializeFontEngine();
                    if (errorCode != FontEngineError.Success)
                    {
                        Debug.Log("Font Asset Creator - Error [" + errorCode + "] has occurred while Initializing the FreeType Library.");
                    }

                    if (errorCode == FontEngineError.Success)
                    {
                        errorCode = FontEngine.LoadFontFace(m_SourceFont, 0, m_SourceFontFaceIndex);

                        if (errorCode != FontEngineError.Success)
                        {
                            Debug.Log("Font Asset Creator - Error Code [" + errorCode + "] has occurred trying to load the [" + m_SourceFont.name + "] font file. This typically results from the use of an incompatible or corrupted font file.", m_SourceFont);
                        }
                    }


                    // Define an array containing the characters we will render.
                    if (errorCode == FontEngineError.Success)
                    {
                        uint[] characterSet = null;

                        // Get list of characters that need to be packed and rendered to the atlas texture.
                        if (m_CharacterSetSelectionMode == 7 || m_CharacterSetSelectionMode == 8)
                        {
                            List<uint> char_List = new List<uint>();

                            for (int i = 0; i < m_CharacterSequence.Length; i++)
                            {
                                uint unicode = m_CharacterSequence[i];

                                // Handle surrogate pairs
                                if (i < m_CharacterSequence.Length - 1 && char.IsHighSurrogate((char)unicode) && char.IsLowSurrogate(m_CharacterSequence[i + 1]))
                                {
                                    unicode = (uint)char.ConvertToUtf32(m_CharacterSequence[i], m_CharacterSequence[i + 1]);
                                    i += 1;
                                }

                                // Check to make sure we don't include duplicates
                                if (char_List.FindIndex(item => item == unicode) == -1)
                                    char_List.Add(unicode);
                            }

                            characterSet = char_List.ToArray();
                        }
                        else if (m_CharacterSetSelectionMode == 6)
                        {
                            characterSet = ParseHexNumberSequence(m_CharacterSequence);
                        }
                        else
                        {
                            characterSet = ParseNumberSequence(m_CharacterSequence);
                        }

                        m_CharacterCount = characterSet.Length;

                        m_AtlasGenerationProgress = 0;
                        m_IsProcessing = true;
                        m_IsGenerationCancelled = false;

                        GlyphLoadFlags glyphLoadFlags = ((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_HINTED) == GlyphRasterModes.RASTER_MODE_HINTED
                            ? GlyphLoadFlags.LOAD_RENDER
                            : GlyphLoadFlags.LOAD_RENDER | GlyphLoadFlags.LOAD_NO_HINTING;

                        glyphLoadFlags = ((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_MONO) == GlyphRasterModes.RASTER_MODE_MONO
                            ? glyphLoadFlags | GlyphLoadFlags.LOAD_MONOCHROME
                            : glyphLoadFlags;

                        glyphLoadFlags = ((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_COLOR) == GlyphRasterModes.RASTER_MODE_COLOR
                            ? glyphLoadFlags | GlyphLoadFlags.LOAD_COLOR
                            : glyphLoadFlags;


                        //
                        AutoResetEvent autoEvent = new AutoResetEvent(false);

                        // Worker thread to pack glyphs in the given texture space.
                        ThreadPool.QueueUserWorkItem(PackGlyphs =>
                        {
                            // Start Stop Watch
                            m_StopWatch = System.Diagnostics.Stopwatch.StartNew();

                            // Clear the various lists used in the generation process.
                            m_AvailableGlyphsToAdd.Clear();
                            m_MissingCharacters.Clear();
                            m_ExcludedCharacters.Clear();
                            m_CharacterLookupMap.Clear();
                            m_GlyphLookupMap.Clear();
                            m_GlyphsToPack.Clear();
                            m_GlyphsPacked.Clear();

                            // Check if requested characters are available in the source font file.
                            for (int i = 0; i < characterSet.Length; i++)
                            {
                                uint unicode = characterSet[i];
                                uint glyphIndex;

                                if (FontEngine.TryGetGlyphIndex(unicode, out glyphIndex))
                                {
                                    // Skip over potential duplicate characters.
                                    if (m_CharacterLookupMap.ContainsKey(unicode))
                                        continue;

                                    // Add character to character lookup map.
                                    m_CharacterLookupMap.Add(unicode, glyphIndex);

                                    // Skip over potential duplicate glyph references.
                                    if (m_GlyphLookupMap.ContainsKey(glyphIndex))
                                    {
                                        // Add additional glyph reference for this character.
                                        m_GlyphLookupMap[glyphIndex].Add(unicode);
                                        continue;
                                    }

                                    // Add glyph reference to glyph lookup map.
                                    m_GlyphLookupMap.Add(glyphIndex, new List<uint>() { unicode });

                                    // Add glyph index to list of glyphs to add to texture.
                                    m_AvailableGlyphsToAdd.Add(glyphIndex);
                                }
                                else
                                {
                                    // Add Unicode to list of missing characters.
                                    m_MissingCharacters.Add(unicode);
                                }
                            }

                            // Pack available glyphs in the provided texture space.
                            if (m_AvailableGlyphsToAdd.Count > 0)
                            {
                                int packingModifier = ((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP ? 0 : 1;

                                if (m_PointSizeSamplingMode == 0) // Auto-Sizing Point Size Mode
                                {
                                    // Estimate min / max range for auto sizing of point size.
                                    int minPointSize = 0;
                                    int maxPointSize = (int)Mathf.Sqrt((m_AtlasWidth * m_AtlasHeight) / m_AvailableGlyphsToAdd.Count) * 3;

                                    m_PointSize = (maxPointSize + minPointSize) / 2;

                                    bool optimumPointSizeFound = false;
                                    for (int iteration = 0; iteration < 15 && optimumPointSizeFound == false && m_PointSize > 0; iteration++)
                                    {
                                        m_AtlasGenerationProgressLabel = "Packing glyphs - Pass (" + iteration + ")";

                                        FontEngine.SetFaceSize(m_PointSize);

                                        m_Padding = (int)(m_PaddingMode == PaddingMode.Percentage ? m_PointSize * m_PaddingFieldValue / 100f : m_PaddingFieldValue);

                                        m_GlyphsToPack.Clear();
                                        m_GlyphsPacked.Clear();

                                        m_FreeGlyphRects.Clear();
                                        m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier, m_AtlasHeight - packingModifier));
                                        m_UsedGlyphRects.Clear();

                                        for (int i = 0; i < m_AvailableGlyphsToAdd.Count; i++)
                                        {
                                            uint glyphIndex = m_AvailableGlyphsToAdd[i];
                                            Glyph glyph;

                                            if (FontEngine.TryGetGlyphWithIndexValue(glyphIndex, glyphLoadFlags, out glyph))
                                            {
                                                if (glyph.glyphRect.width > 0 && glyph.glyphRect.height > 0)
                                                {
                                                    m_GlyphsToPack.Add(glyph);
                                                }
                                                else
                                                {
                                                    m_GlyphsPacked.Add(glyph);
                                                }
                                            }
                                        }

                                        FontEngine.TryPackGlyphsInAtlas(m_GlyphsToPack, m_GlyphsPacked, m_Padding, (GlyphPackingMode)m_PackingMode, m_GlyphRenderMode, m_AtlasWidth, m_AtlasHeight, m_FreeGlyphRects, m_UsedGlyphRects);

                                        if (m_IsGenerationCancelled)
                                        {
                                            DestroyImmediate(m_FontAtlasTexture);
                                            m_FontAtlasTexture = null;
                                            return;
                                        }

                                        //Debug.Log("Glyphs remaining to add [" + m_GlyphsToAdd.Count + "]. Glyphs added [" + m_GlyphsAdded.Count + "].");

                                        if (m_GlyphsToPack.Count > 0)
                                        {
                                            if (m_PointSize > minPointSize)
                                            {
                                                maxPointSize = m_PointSize;
                                                m_PointSize = (m_PointSize + minPointSize) / 2;

                                                //Debug.Log("Decreasing point size from [" + maxPointSize + "] to [" + m_PointSize + "].");
                                            }
                                        }
                                        else
                                        {
                                            if (maxPointSize - minPointSize > 1 && m_PointSize < maxPointSize)
                                            {
                                                minPointSize = m_PointSize;
                                                m_PointSize = (m_PointSize + maxPointSize) / 2;

                                                //Debug.Log("Increasing point size from [" + minPointSize + "] to [" + m_PointSize + "].");
                                            }
                                            else
                                            {
                                                //Debug.Log("[" + iteration + "] iterations to find the optimum point size of : [" + m_PointSize + "].");
                                                optimumPointSizeFound = true;
                                            }
                                        }
                                    }
                                }
                                else // Custom Point Size Mode
                                {
                                    m_AtlasGenerationProgressLabel = "Packing glyphs...";

                                    // Set point size
                                    FontEngine.SetFaceSize(m_PointSize);

                                    m_Padding = (int)(m_PaddingMode == PaddingMode.Percentage ? m_PointSize * m_PaddingFieldValue / 100 : m_PaddingFieldValue);

                                    m_GlyphsToPack.Clear();
                                    m_GlyphsPacked.Clear();

                                    m_FreeGlyphRects.Clear();
                                    m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier, m_AtlasHeight - packingModifier));
                                    m_UsedGlyphRects.Clear();

                                    for (int i = 0; i < m_AvailableGlyphsToAdd.Count; i++)
                                    {
                                        uint glyphIndex = m_AvailableGlyphsToAdd[i];
                                        Glyph glyph;

                                        if (FontEngine.TryGetGlyphWithIndexValue(glyphIndex, glyphLoadFlags, out glyph))
                                        {
                                            if (glyph.glyphRect.width > 0 && glyph.glyphRect.height > 0)
                                            {
                                                m_GlyphsToPack.Add(glyph);
                                            }
                                            else
                                            {
                                                m_GlyphsPacked.Add(glyph);
                                            }
                                        }
                                    }

                                    FontEngine.TryPackGlyphsInAtlas(m_GlyphsToPack, m_GlyphsPacked, m_Padding, (GlyphPackingMode)m_PackingMode, m_GlyphRenderMode, m_AtlasWidth, m_AtlasHeight, m_FreeGlyphRects, m_UsedGlyphRects);

                                    if (m_IsGenerationCancelled)
                                    {
                                        DestroyImmediate(m_FontAtlasTexture);
                                        m_FontAtlasTexture = null;
                                        return;
                                    }
                                    //Debug.Log("Glyphs remaining to add [" + m_GlyphsToAdd.Count + "]. Glyphs added [" + m_GlyphsAdded.Count + "].");
                                }
                            }
                            else
                            {
                                int packingModifier = ((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP ? 0 : 1;

                                FontEngine.SetFaceSize(m_PointSize);

                                m_Padding = (int)(m_PaddingMode == PaddingMode.Percentage ? m_PointSize * m_PaddingFieldValue / 100 : m_PaddingFieldValue);

                                m_GlyphsToPack.Clear();
                                m_GlyphsPacked.Clear();

                                m_FreeGlyphRects.Clear();
                                m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier, m_AtlasHeight - packingModifier));
                                m_UsedGlyphRects.Clear();
                            }

                            //Stop StopWatch
                            m_StopWatch.Stop();
                            m_GlyphPackingGenerationTime = m_StopWatch.Elapsed.TotalMilliseconds;
                            m_IsGlyphPackingDone = true;
                            m_StopWatch.Reset();

                            m_FontCharacterTable.Clear();
                            m_FontGlyphTable.Clear();
                            m_GlyphsToRender.Clear();

                            // Handle Results and potential cancellation of glyph rendering
                            if (m_GlyphRenderMode == GlyphRenderMode.SDF32 && m_PointSize > 512 || m_GlyphRenderMode == GlyphRenderMode.SDF16 && m_PointSize > 1024 || m_GlyphRenderMode == GlyphRenderMode.SDF8 && m_PointSize > 2048)
                            {
                                int upSampling = 1;
                                switch (m_GlyphRenderMode)
                                {
                                    case GlyphRenderMode.SDF8:
                                        upSampling = 8;
                                        break;
                                    case GlyphRenderMode.SDF16:
                                        upSampling = 16;
                                        break;
                                    case GlyphRenderMode.SDF32:
                                        upSampling = 32;
                                        break;
                                }

                                Debug.Log("Glyph rendering has been aborted due to sampling point size of [" + m_PointSize + "] x SDF [" + upSampling + "] up sampling exceeds 16,384 point size. Please revise your generation settings to make sure the sampling point size x SDF up sampling mode does not exceed 16,384.");

                                m_IsRenderingDone = true;
                                m_AtlasGenerationProgress = 0;
                                m_IsGenerationCancelled = true;
                            }

                            // Add glyphs and characters successfully added to texture to their respective font tables.
                            foreach (Glyph glyph in m_GlyphsPacked)
                            {
                                uint glyphIndex = glyph.index;

                                m_FontGlyphTable.Add(glyph);

                                // Add glyphs to list of glyphs that need to be rendered.
                                if (glyph.glyphRect.width > 0 && glyph.glyphRect.height > 0)
                                    m_GlyphsToRender.Add(glyph);

                                foreach (uint unicode in m_GlyphLookupMap[glyphIndex])
                                {
                                    // Create new Character
                                    m_FontCharacterTable.Add(new Character(unicode, glyph));
                                }
                            }

                            //
                            foreach (Glyph glyph in m_GlyphsToPack)
                            {
                                foreach (uint unicode in m_GlyphLookupMap[glyph.index])
                                {
                                    m_ExcludedCharacters.Add(unicode);
                                }
                            }

                            // Get the face info for the current sampling point size.
                            m_FaceInfo = FontEngine.GetFaceInfo();

                            autoEvent.Set();
                        });

                        // Worker thread to render glyphs in texture buffer.
                        ThreadPool.QueueUserWorkItem(RenderGlyphs =>
                        {
                            autoEvent.WaitOne();

                            if (m_IsGenerationCancelled == false)
                            {
                                // Start Stop Watch
                                m_StopWatch = System.Diagnostics.Stopwatch.StartNew();

                                m_IsRenderingDone = false;

                                // Allocate texture data

                                if (m_GlyphRenderMode == GlyphRenderMode.COLOR || m_GlyphRenderMode == GlyphRenderMode.COLOR_HINTED)
                                    m_AtlasTextureBuffer = new byte[m_AtlasWidth * m_AtlasHeight * 4];
                                else
                                    m_AtlasTextureBuffer = new byte[m_AtlasWidth * m_AtlasHeight];


                                m_AtlasGenerationProgressLabel = "Rendering glyphs...";

                                // Render and add glyphs to the given atlas texture.
                                if (m_GlyphsToRender.Count > 0)
                                {
                                    FontEngine.RenderGlyphsToTexture(m_GlyphsToRender, m_Padding, m_GlyphRenderMode, m_AtlasTextureBuffer, m_AtlasWidth, m_AtlasHeight);
                                }

                                m_IsRenderingDone = true;

                                // Stop StopWatch
                                m_StopWatch.Stop();
                                m_GlyphRenderingGenerationTime = m_StopWatch.Elapsed.TotalMilliseconds;
                                m_IsGlyphRenderingDone = true;
                                m_StopWatch.Reset();
                            }
                        });
                    }

                    SaveCreationSettingsToEditorPrefs(SaveFontCreationSettings());
                }
            }

            // FONT RENDERING PROGRESS BAR
            GUILayout.Space(1);
            Rect progressRect = EditorGUILayout.GetControlRect(false, 20);

            GUI.enabled = true;
            progressRect.width -= 22;
            EditorGUI.ProgressBar(progressRect, Mathf.Max(0.01f, m_AtlasGenerationProgress), m_AtlasGenerationProgressLabel);
            progressRect.x = progressRect.x + progressRect.width + 2;
            progressRect.y -= 1;
            progressRect.width = 20;
            progressRect.height = 20;

            GUI.enabled = m_IsProcessing;
            if (GUI.Button(progressRect, "X"))
            {
                FontEngine.SendCancellationRequest();
                m_AtlasGenerationProgress = 0;
                m_IsProcessing = false;
                m_IsGenerationCancelled = true;
            }
            GUILayout.Space(5);

            // FONT STATUS & INFORMATION
            GUI.enabled = true;

            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(200));
            m_OutputScrollPosition = EditorGUILayout.BeginScrollView(m_OutputScrollPosition);
            EditorGUILayout.LabelField(m_OutputFeedback, TM_EditorStyles.label);
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            // SAVE TEXTURE & CREATE and SAVE FONT XML FILE
            GUI.enabled = m_FontAtlasTexture != null && !m_IsProcessing;    // Enable Save Button if font_Atlas is not Null.

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Save") && GUI.enabled)
            {
                if (m_SelectedFontAsset == null)
                {
                    if (m_LegacyFontAsset != null)
                        SaveNewFontAssetWithSameName(m_LegacyFontAsset);
                    else
                        SaveNewFontAsset(m_SourceFont);
                }
                else
                {
                    // Save over exiting Font Asset
                    string filePath = Path.GetFullPath(AssetDatabase.GetAssetPath(m_SelectedFontAsset)).Replace('\\', '/');

                    if (((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP)
                        Save_Bitmap_FontAsset(filePath);
                    else
                        Save_SDF_FontAsset(filePath);
                }
            }
            if (GUILayout.Button("Save as...") && GUI.enabled)
            {
                if (m_SelectedFontAsset == null)
                {
                    SaveNewFontAsset(m_SourceFont);
                }
                else
                {
                    SaveNewFontAssetWithSameName(m_SelectedFontAsset);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();

            GUI.enabled = true; // Re-enable GUI

            if (position.height > position.width || position.width < k_TwoColumnControlsWidth)
            {
                DrawPreview();
                GUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();

            if (m_IsFontAtlasInvalid)
                ClearGeneratedData();
        }

        /// <summary>
        /// Clear the previously generated data.
        /// </summary>
        void ClearGeneratedData()
        {
            m_IsFontAtlasInvalid = false;

            if (m_FontAtlasTexture != null && !EditorUtility.IsPersistent(m_FontAtlasTexture))
            {
                DestroyImmediate(m_FontAtlasTexture);
                m_FontAtlasTexture = null;
            }

            if (m_GlyphRectPreviewTexture != null)
            {
                DestroyImmediate(m_GlyphRectPreviewTexture);
                m_GlyphRectPreviewTexture = null;
            }

            m_AtlasGenerationProgressLabel = string.Empty;
            m_AtlasGenerationProgress = 0;
            m_SavedFontAtlas = null;

            m_OutputFeedback = string.Empty;
            m_WarningMessage = string.Empty;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        string[] GetFontFaces()
        {
            FontEngine.LoadFontFace(m_SourceFont, 0, 0);
            return FontEngine.GetFontFaces();
        }

        /// <summary>
        /// Function to update the feedback window showing the results of the latest generation.
        /// </summary>
        void UpdateRenderFeedbackWindow()
        {
            m_PointSize = m_FaceInfo.pointSize;

            string missingGlyphReport = string.Empty;

            //string colorTag = m_FontCharacterTable.Count == m_CharacterCount ? "<color=#C0ffff>" : "<color=#ffff00>";
            string colorTag2 = "<color=#C0ffff>";

            missingGlyphReport = "Font: <b>" + colorTag2 + m_FaceInfo.familyName + "</color></b>  Style: <b>" + colorTag2 + m_FaceInfo.styleName + "</color></b>";

            missingGlyphReport += "\nPoint Size: <b>" + colorTag2 + m_FaceInfo.pointSize + "</color></b>   Padding: <b>" + colorTag2 + m_Padding + "</color></b>   SP/PD Ratio: <b>" + colorTag2 + ((float)m_Padding / m_FaceInfo.pointSize).ToString("0.0%" + "</color></b>");

            missingGlyphReport += "\n\nCharacters included: <color=#ffff00><b>" + m_FontCharacterTable.Count + "/" + m_CharacterCount + "</b></color>";
            missingGlyphReport += "\nMissing characters: <color=#ffff00><b>" + m_MissingCharacters.Count + "</b></color>";
            missingGlyphReport += "\nExcluded characters: <color=#ffff00><b>" + m_ExcludedCharacters.Count + "</b></color>";

            // Report characters missing from font file
            missingGlyphReport += "\n\n<b><color=#ffff00>Characters missing from font file:</color></b>";
            missingGlyphReport += "\n----------------------------------------";

            m_OutputFeedback = missingGlyphReport;

            for (int i = 0; i < m_MissingCharacters.Count; i++)
            {
                missingGlyphReport += "\nID: <color=#C0ffff>" + m_MissingCharacters[i] + "\t</color>Hex: <color=#C0ffff>" + m_MissingCharacters[i].ToString("X") + "\t</color>Char [<color=#C0ffff>" + (char)m_MissingCharacters[i] + "</color>]";

                if (missingGlyphReport.Length < 16300)
                    m_OutputFeedback = missingGlyphReport;
            }

            // Report characters that did not fit in the atlas texture
            missingGlyphReport += "\n\n<b><color=#ffff00>Characters excluded from packing:</color></b>";
            missingGlyphReport += "\n----------------------------------------";

            for (int i = 0; i < m_ExcludedCharacters.Count; i++)
            {
                missingGlyphReport += "\nID: <color=#C0ffff>" + m_ExcludedCharacters[i] + "\t</color>Hex: <color=#C0ffff>" + m_ExcludedCharacters[i].ToString("X") + "\t</color>Char [<color=#C0ffff>" + (char)m_ExcludedCharacters[i] + "</color>]";

                if (missingGlyphReport.Length < 16300)
                    m_OutputFeedback = missingGlyphReport;
            }

            if (missingGlyphReport.Length > 16300)
                m_OutputFeedback += "\n\n<color=#ffff00>Report truncated.</color>\n<color=#c0ffff>See</color> \"TextMesh Pro\\Glyph Report.txt\"";

            // Save Missing Glyph Report file
            if (Directory.Exists("Assets/TextMesh Pro"))
            {
                missingGlyphReport = System.Text.RegularExpressions.Regex.Replace(missingGlyphReport, @"<[^>]*>", string.Empty);
                File.WriteAllText("Assets/TextMesh Pro/Glyph Report.txt", missingGlyphReport);
                AssetDatabase.Refresh();
            }
        }

        void DrawGlyphRectPreviewTexture()
        {
            if (m_GlyphRectPreviewTexture != null)
                DestroyImmediate(m_GlyphRectPreviewTexture);

            m_GlyphRectPreviewTexture = new Texture2D(m_AtlasWidth, m_AtlasHeight, TextureFormat.RGBA32, false, true);

            FontEngine.ResetAtlasTexture(m_GlyphRectPreviewTexture);

            foreach (Glyph glyph in m_GlyphsPacked)
            {
                GlyphRect glyphRect = glyph.glyphRect;

                Color c = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 0.5f, 1.0f, 1.0f);

                int x0 = glyphRect.x;
                int x1 = x0 + glyphRect.width;

                int y0 = glyphRect.y;
                int y1 = y0 + glyphRect.height;

                // Draw glyph rectangle.
                for (int x = x0; x < x1; x++)
                {
                    for (int y = y0; y < y1; y++)
                        m_GlyphRectPreviewTexture.SetPixel(x, y, c);
                }
            }

            m_GlyphRectPreviewTexture.Apply(false);
        }

        void CreateFontAtlasTexture()
        {
            if (m_FontAtlasTexture != null)
                DestroyImmediate(m_FontAtlasTexture);

            Color32[] colors = new Color32[m_AtlasWidth * m_AtlasHeight];


            switch (m_GlyphRenderMode)
            {
                case GlyphRenderMode.COLOR:
                case GlyphRenderMode.COLOR_HINTED:
                    m_FontAtlasTexture = new Texture2D(m_AtlasWidth, m_AtlasHeight, TextureFormat.RGBA32, false, true);

                    for (int i = 0; i < colors.Length; i++)
                    {
                        int readIndex = i * 4;
                        byte r = m_AtlasTextureBuffer[readIndex + 0];
                        byte g = m_AtlasTextureBuffer[readIndex + 1];
                        byte b = m_AtlasTextureBuffer[readIndex + 2];
                        byte a = m_AtlasTextureBuffer[readIndex + 3];
                        colors[i] = new Color32(r, g, b, a);
                    }
                    break;
                default:
                    m_FontAtlasTexture = new Texture2D(m_AtlasWidth, m_AtlasHeight, TextureFormat.Alpha8, false, true);

                    for (int i = 0; i < colors.Length; i++)
                    {
                        byte c = m_AtlasTextureBuffer[i];
                        colors[i] = new Color32(c, c, c, c);
                    }
                    break;
            }

            // Clear allocation of
            m_AtlasTextureBuffer = null;

            if ((m_GlyphRenderMode & GlyphRenderMode.RASTER) == GlyphRenderMode.RASTER || (m_GlyphRenderMode & GlyphRenderMode.RASTER_HINTED) == GlyphRenderMode.RASTER_HINTED)
                m_FontAtlasTexture.filterMode = FilterMode.Point;

            m_FontAtlasTexture.SetPixels32(colors, 0);
            m_FontAtlasTexture.Apply(false, false);

            // Saving File for Debug
            //var pngData = m_FontAtlasTexture.EncodeToPNG();
            //File.WriteAllBytes("Assets/Textures/Debug Font Texture.png", pngData);
        }

        /// <summary>
        /// Open Save Dialog to provide the option save the font asset using the name of the source font file. This also appends SDF to the name if using any of the SDF Font Asset creation modes.
        /// </summary>
        /// <param name="sourceObject"></param>
        void SaveNewFontAsset(Object sourceObject)
        {
            string filePath;

            // Save new Font Asset and open save file requester at Source Font File location.
            string saveDirectory = new FileInfo(AssetDatabase.GetAssetPath(sourceObject)).DirectoryName;

            if (((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP)
            {
                filePath = EditorUtility.SaveFilePanel("Save TextMesh Pro! Font Asset File", saveDirectory, sourceObject.name, "asset");

                if (filePath.Length == 0)
                    return;

                Save_Bitmap_FontAsset(filePath);
            }
            else
            {
                filePath = EditorUtility.SaveFilePanel("Save TextMesh Pro! Font Asset File", saveDirectory, sourceObject.name + " SDF", "asset");

                if (filePath.Length == 0)
                    return;

                Save_SDF_FontAsset(filePath);
            }
        }

        /// <summary>
        /// Open Save Dialog to provide the option to save the font asset under the same name.
        /// </summary>
        /// <param name="sourceObject"></param>
        void SaveNewFontAssetWithSameName(Object sourceObject)
        {
            string filePath;

            // Save new Font Asset and open save file requester at Source Font File location.
            string saveDirectory = new FileInfo(AssetDatabase.GetAssetPath(sourceObject)).DirectoryName;

            filePath = EditorUtility.SaveFilePanel("Save TextMesh Pro! Font Asset File", saveDirectory, sourceObject.name, "asset");

            if (filePath.Length == 0)
                return;

            if (((GlyphRasterModes)m_GlyphRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP)
            {
                Save_Bitmap_FontAsset(filePath);
            }
            else
            {
                Save_SDF_FontAsset(filePath);
            }
        }

        void Save_Bitmap_FontAsset(string filePath)
        {
            filePath = filePath.Substring(0, filePath.Length - 6); // Trim file extension from filePath.

            string dataPath = Application.dataPath;

            // if (filePath.IndexOf(dataPath, System.StringComparison.InvariantCultureIgnoreCase) == -1)
            // {
            //     Debug.LogError("You're saving the font asset in a directory outside of this project folder. This is not supported. Please select a directory under \"" + dataPath + "\"");
            //     return;
            // }

            string relativeAssetPath = filePath.Substring(dataPath.Length - 6);
            string tex_DirName = Path.GetDirectoryName(relativeAssetPath);
            string tex_FileName = Path.GetFileNameWithoutExtension(relativeAssetPath);
            string tex_Path_NoExt = tex_DirName + "/" + tex_FileName;

            // Check if TextMeshPro font asset already exists. If not, create a new one. Otherwise update the existing one.
            FontAsset fontAsset = AssetDatabase.LoadAssetAtPath(tex_Path_NoExt + ".asset", typeof(FontAsset)) as FontAsset;
            if (fontAsset == null)
            {
                //Debug.Log("Creating TextMeshPro font asset!");
                fontAsset = ScriptableObject.CreateInstance<FontAsset>(); // Create new TextMeshPro Font Asset.
                AssetDatabase.CreateAsset(fontAsset, tex_Path_NoExt + ".asset");

                // Set version number of font asset
                fontAsset.version = "1.1.0";

                //Set Font Asset Type
                fontAsset.atlasRenderMode = m_GlyphRenderMode;

                // Reference to the source font file GUID.
                fontAsset.m_SourceFontFile_EditorRef = m_SourceFont;
                fontAsset.m_SourceFontFileGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_SourceFont));

                // Add FaceInfo to Font Asset
                fontAsset.faceInfo = m_FaceInfo;

                // Add GlyphInfo[] to Font Asset
                fontAsset.glyphTable = m_FontGlyphTable;

                // Add CharacterTable[] to font asset.
                fontAsset.characterTable = m_FontCharacterTable;

                // Sort glyph and character tables.
                fontAsset.SortAllTables();

                // Get and Add Kerning Pairs to Font Asset
                if (m_IncludeFontFeatures)
                    fontAsset.fontFeatureTable = GetAllFontFeatures();


                // Add Font Atlas as Sub-Asset
                fontAsset.atlasTextures = new Texture2D[] { m_FontAtlasTexture };
                m_FontAtlasTexture.name = tex_FileName + " Atlas";
                fontAsset.atlasWidth = m_AtlasWidth;
                fontAsset.atlasHeight = m_AtlasHeight;
                fontAsset.atlasPadding = m_Padding;

                AssetDatabase.AddObjectToAsset(m_FontAtlasTexture, fontAsset);

                // Create new Material and Add it as Sub-Asset
                Shader default_Shader = TextShaderUtilities.ShaderRef_MobileBitmap;
                Material tmp_material = new Material(default_Shader);
                tmp_material.name = tex_FileName + " Material";
                tmp_material.SetTexture(TextShaderUtilities.ID_MainTex, m_FontAtlasTexture);
                fontAsset.material = tmp_material;

                AssetDatabase.AddObjectToAsset(tmp_material, fontAsset);
            }
            else
            {
                // Find all Materials referencing this font atlas.
                Material[] material_references = TextCoreEditorUtilities.FindMaterialReferences(fontAsset);

                // Set version number of font asset
                fontAsset.version = "1.1.0";

                //Set Font Asset Type
                fontAsset.atlasRenderMode = m_GlyphRenderMode;

                // Add FaceInfo to Font Asset
                fontAsset.faceInfo = m_FaceInfo;

                // Add GlyphInfo[] to Font Asset
                fontAsset.glyphTable = m_FontGlyphTable;

                // Add CharacterTable[] to font asset.
                fontAsset.characterTable = m_FontCharacterTable;

                // Sort glyph and character tables.
                fontAsset.SortAllTables();

                // Get and Add Kerning Pairs to Font Asset
                if (m_IncludeFontFeatures)
                    fontAsset.fontFeatureTable = GetAllFontFeatures();

                // Destroy Assets that will be replaced.
                if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
                {
                    for (int i = 1; i < fontAsset.atlasTextures.Length; i++)
                        DestroyImmediate(fontAsset.atlasTextures[i], true);
                }

                fontAsset.m_AtlasTextureIndex = 0;
                fontAsset.atlasWidth = m_AtlasWidth;
                fontAsset.atlasHeight = m_AtlasHeight;
                fontAsset.atlasPadding = m_Padding;

                // Make sure remaining atlas texture is of the correct size
                Texture2D tex = fontAsset.atlasTextures[0];
                tex.name = tex_FileName + " Atlas";

                // Make texture readable to allow resizing
                bool isReadableState = tex.isReadable;
                if (isReadableState == false)
                    FontEngineEditorUtilities.SetAtlasTextureIsReadable(tex, true);

                if (tex.width != m_AtlasWidth || tex.height != m_AtlasHeight)
                {
                    tex.Reinitialize(m_AtlasWidth, m_AtlasHeight);
                    tex.Apply(false);
                }

                // Copy new texture data to existing texture
                Graphics.CopyTexture(m_FontAtlasTexture, tex);

                // Apply changes to the texture.
                tex.Apply(false);

                // Special handling due to a bug in earlier versions of Unity.
                m_FontAtlasTexture.hideFlags = HideFlags.None;
                fontAsset.material.hideFlags = HideFlags.None;

                // Update the Texture reference on the Material
                //for (int i = 0; i < material_references.Length; i++)
                //{
                //    material_references[i].SetFloat(TextShaderUtilities.ID_TextureWidth, tex.width);
                //    material_references[i].SetFloat(TextShaderUtilities.ID_TextureHeight, tex.height);

                //    int spread = m_Padding;
                //    material_references[i].SetFloat(TextShaderUtilities.ID_GradientScale, spread);

                //    material_references[i].SetFloat(TextShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
                //    material_references[i].SetFloat(TextShaderUtilities.ID_WeightBold, fontAsset.boldStyle);
                //}
            }

            // Set texture to non readable
            if (fontAsset.atlasPopulationMode == AtlasPopulationMode.Static)
                FontEngineEditorUtilities.SetAtlasTextureIsReadable(fontAsset.atlasTexture, false);

            // Add list of GlyphRects to font asset.
            fontAsset.freeGlyphRects = m_FreeGlyphRects;
            fontAsset.usedGlyphRects = m_UsedGlyphRects;

            // Save Font Asset creation settings
            m_SelectedFontAsset = fontAsset;
            m_LegacyFontAsset = null;
            fontAsset.fontAssetCreationEditorSettings = SaveFontCreationSettings();

            AssetDatabase.SaveAssets();

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(fontAsset));  // Re-import font asset to get the new updated version.

            //EditorUtility.SetDirty(font_asset);
            fontAsset.ReadFontAssetDefinition();

            AssetDatabase.Refresh();

            m_FontAtlasTexture = null;

            // NEED TO GENERATE AN EVENT TO FORCE A REDRAW OF ANY TEXTMESHPRO INSTANCES THAT MIGHT BE USING THIS FONT ASSET
            TextEventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);
        }

        void Save_SDF_FontAsset(string filePath)
        {
            filePath = filePath.Substring(0, filePath.Length - 6); // Trim file extension from filePath.

            string dataPath = Application.dataPath;

            // if (filePath.IndexOf(dataPath, System.StringComparison.InvariantCultureIgnoreCase) == -1)
            // {
            //     Debug.LogError("You're saving the font asset in a directory outside of this project folder. This is not supported. Please select a directory under \"" + dataPath + "\"");
            //     return;
            // }

            string relativeAssetPath = filePath.Substring(dataPath.Length - 6);
            string tex_DirName = Path.GetDirectoryName(relativeAssetPath);
            string tex_FileName = Path.GetFileNameWithoutExtension(relativeAssetPath);
            string tex_Path_NoExt = tex_DirName + "/" + tex_FileName;


            // Check if TextMeshPro font asset already exists. If not, create a new one. Otherwise update the existing one.
            FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<FontAsset>(tex_Path_NoExt + ".asset");
            if (fontAsset == null)
            {
                //Debug.Log("Creating TextMeshPro font asset!");
                fontAsset = ScriptableObject.CreateInstance<FontAsset>(); // Create new TextMeshPro Font Asset.
                AssetDatabase.CreateAsset(fontAsset, tex_Path_NoExt + ".asset");

                // Set version number of font asset
                fontAsset.version = "1.1.0";

                // Reference to source font file GUID.
                fontAsset.m_SourceFontFile_EditorRef = m_SourceFont;
                fontAsset.m_SourceFontFileGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_SourceFont));

                //Set Font Asset Type
                fontAsset.atlasRenderMode = m_GlyphRenderMode;

                // Add FaceInfo to Font Asset
                fontAsset.faceInfo = m_FaceInfo;

                // Add GlyphInfo[] to Font Asset
                fontAsset.glyphTable = m_FontGlyphTable;

                // Add CharacterTable[] to font asset.
                fontAsset.characterTable = m_FontCharacterTable;

                // Sort glyph and character tables.
                fontAsset.SortAllTables();

                // Get and Add Kerning Pairs to Font Asset
                if (m_IncludeFontFeatures)
                    fontAsset.fontFeatureTable = GetAllFontFeatures();

                // Add Font Atlas as Sub-Asset
                fontAsset.atlasTextures = new Texture2D[] { m_FontAtlasTexture };
                m_FontAtlasTexture.name = tex_FileName + " Atlas";
                fontAsset.atlasWidth = m_AtlasWidth;
                fontAsset.atlasHeight = m_AtlasHeight;
                fontAsset.atlasPadding = m_Padding;

                AssetDatabase.AddObjectToAsset(m_FontAtlasTexture, fontAsset);

                // Create new Material and Add it as Sub-Asset
                Shader default_Shader = TextShaderUtilities.ShaderRef_MobileSDF;
                Material tmp_material = new Material(default_Shader);

                tmp_material.name = tex_FileName + " Material";
                tmp_material.SetTexture(TextShaderUtilities.ID_MainTex, m_FontAtlasTexture);
                tmp_material.SetFloat(TextShaderUtilities.ID_TextureWidth, m_FontAtlasTexture.width);
                tmp_material.SetFloat(TextShaderUtilities.ID_TextureHeight, m_FontAtlasTexture.height);

                int spread = m_Padding + 1;
                tmp_material.SetFloat(TextShaderUtilities.ID_GradientScale, spread); // Spread = Padding for Brute Force SDF.

                tmp_material.SetFloat(TextShaderUtilities.ID_WeightNormal, fontAsset.regularStyleWeight);
                tmp_material.SetFloat(TextShaderUtilities.ID_WeightBold, fontAsset.boldStyleWeight);

                fontAsset.material = tmp_material;

                AssetDatabase.AddObjectToAsset(tmp_material, fontAsset);
            }
            else
            {
                // Find all Materials referencing this font atlas.
                Material[] material_references = TextCoreEditorUtilities.FindMaterialReferences(fontAsset);

                // Set version number of font asset
                fontAsset.version = "1.1.0";

                //Set Font Asset Type
                fontAsset.atlasRenderMode = m_GlyphRenderMode;

                // Add FaceInfo to Font Asset
                fontAsset.faceInfo = m_FaceInfo;

                // Add GlyphInfo[] to Font Asset
                fontAsset.glyphTable = m_FontGlyphTable;

                // Add CharacterTable[] to font asset.
                fontAsset.characterTable = m_FontCharacterTable;

                // Sort glyph and character tables.
                fontAsset.SortAllTables();

                // Get and Add Kerning Pairs to Font Asset
                // TODO: Check and preserve existing adjustment pairs.
                if (m_IncludeFontFeatures)
                    fontAsset.fontFeatureTable = GetAllFontFeatures();

                // Destroy Assets that will be replaced.
                if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
                {
                    for (int i = 1; i < fontAsset.atlasTextures.Length; i++)
                        DestroyImmediate(fontAsset.atlasTextures[i], true);
                }

                fontAsset.m_AtlasTextureIndex = 0;
                fontAsset.atlasWidth = m_AtlasWidth;
                fontAsset.atlasHeight = m_AtlasHeight;
                fontAsset.atlasPadding = m_Padding;

                // Make sure remaining atlas texture is of the correct size
                Texture2D tex = fontAsset.atlasTextures[0];
                tex.name = tex_FileName + " Atlas";

                // Make texture readable to allow resizing
                bool isReadableState = tex.isReadable;
                if (isReadableState == false)
                    FontEngineEditorUtilities.SetAtlasTextureIsReadable(tex, true);

                if (tex.width != m_AtlasWidth || tex.height != m_AtlasHeight)
                {
                    tex.Reinitialize(m_AtlasWidth, m_AtlasHeight);
                    tex.Apply(false);
                }

                // Copy new texture data to existing texture
                Graphics.CopyTexture(m_FontAtlasTexture, tex);

                // Apply changes to the texture.
                tex.Apply(false);

                // Special handling due to a bug in earlier versions of Unity.
                m_FontAtlasTexture.hideFlags = HideFlags.None;
                fontAsset.material.hideFlags = HideFlags.None;

                // Update the Texture reference on the Material
                for (int i = 0; i < material_references.Length; i++)
                {
                    material_references[i].SetFloat(TextShaderUtilities.ID_TextureWidth, tex.width);
                    material_references[i].SetFloat(TextShaderUtilities.ID_TextureHeight, tex.height);

                    int spread = m_Padding + 1;
                    material_references[i].SetFloat(TextShaderUtilities.ID_GradientScale, spread);

                    material_references[i].SetFloat(TextShaderUtilities.ID_WeightNormal, fontAsset.regularStyleWeight);
                    material_references[i].SetFloat(TextShaderUtilities.ID_WeightBold, fontAsset.boldStyleWeight);
                }
            }

            // Saving File for Debug
            //var pngData = destination_Atlas.EncodeToPNG();
            //File.WriteAllBytes("Assets/Textures/Debug Distance Field.png", pngData);

            // Set texture to non readable
            if (fontAsset.atlasPopulationMode == AtlasPopulationMode.Static)
                FontEngineEditorUtilities.SetAtlasTextureIsReadable(fontAsset.atlasTexture, false);

            // Add list of GlyphRects to font asset.
            fontAsset.freeGlyphRects = m_FreeGlyphRects;
            fontAsset.usedGlyphRects = m_UsedGlyphRects;

            // Save Font Asset creation settings
            m_SelectedFontAsset = fontAsset;
            m_LegacyFontAsset = null;
            fontAsset.fontAssetCreationEditorSettings = SaveFontCreationSettings();

            AssetDatabase.SaveAssets();

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(fontAsset));  // Re-import font asset to get the new updated version.

            fontAsset.ReadFontAssetDefinition();

            AssetDatabase.Refresh();

            m_FontAtlasTexture = null;

            // NEED TO GENERATE AN EVENT TO FORCE A REDRAW OF ANY TEXTMESHPRO INSTANCES THAT MIGHT BE USING THIS FONT ASSET
            TextEventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);
        }

        /// <summary>
        /// Internal method to save the Font Asset Creation Settings
        /// </summary>
        /// <returns></returns>
        FontAssetCreationEditorSettings SaveFontCreationSettings()
        {
            FontAssetCreationEditorSettings settings = new FontAssetCreationEditorSettings();

            //settings.sourceFontFileName = m_SourceFontFile.name;
            settings.sourceFontFileGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_SourceFont));
            settings.faceIndex = m_SourceFontFaceIndex;
            settings.pointSizeSamplingMode = m_PointSizeSamplingMode;
            settings.pointSize = m_PointSize;
            settings.padding = m_Padding;
            settings.paddingMode = (int)m_PaddingMode;
            settings.packingMode = (int)m_PackingMode;
            settings.atlasWidth = m_AtlasWidth;
            settings.atlasHeight = m_AtlasHeight;
            settings.characterSetSelectionMode = m_CharacterSetSelectionMode;
            settings.characterSequence = m_CharacterSequence;
            settings.referencedFontAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_ReferencedFontAsset));
            settings.referencedTextAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_CharactersFromFile));
            settings.renderMode = (int)m_GlyphRenderMode;
            settings.includeFontFeatures = m_IncludeFontFeatures;

            return settings;
        }

        /// <summary>
        /// Internal method to load the Font Asset Creation Settings
        /// </summary>
        /// <param name="settings"></param>
        void LoadFontCreationSettings(FontAssetCreationEditorSettings settings)
        {
            m_SourceFont = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(settings.sourceFontFileGUID));
            m_SourceFontFaceIndex = settings.faceIndex;
            m_SourceFontFaces = GetFontFaces();
            m_PointSizeSamplingMode  = settings.pointSizeSamplingMode;
            m_PointSize = settings.pointSize;
            m_Padding = settings.padding;
            m_PaddingMode = settings.paddingMode == 0 ? PaddingMode.Pixel : (PaddingMode)settings.paddingMode;
            m_PaddingFieldValue = m_PaddingMode == PaddingMode.Percentage ? (float)m_Padding / m_PointSize * 100 : m_Padding;
            m_PackingMode = (FontPackingModes)settings.packingMode;
            m_AtlasWidth = settings.atlasWidth;
            m_AtlasHeight = settings.atlasHeight;
            m_CharacterSetSelectionMode = settings.characterSetSelectionMode;
            m_CharacterSequence = settings.characterSequence;
            m_ReferencedFontAsset = AssetDatabase.LoadAssetAtPath<FontAsset>(AssetDatabase.GUIDToAssetPath(settings.referencedFontAssetGUID));
            m_CharactersFromFile = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(settings.referencedTextAssetGUID));
            m_GlyphRenderMode = (GlyphRenderMode)settings.renderMode;
            m_IncludeFontFeatures = settings.includeFontFeatures;
        }

        /// <summary>
        /// Save the latest font asset creation settings to EditorPrefs.
        /// </summary>
        /// <param name="settings"></param>
        void SaveCreationSettingsToEditorPrefs(FontAssetCreationEditorSettings settings)
        {
            // Create new list if one does not already exist
            if (m_FontAssetCreationSettingsContainer == null)
            {
                m_FontAssetCreationSettingsContainer = new FontAssetCreationSettingsContainer();
                m_FontAssetCreationSettingsContainer.fontAssetCreationSettings = new List<FontAssetCreationEditorSettings>();
            }

            // Add new creation settings to the list
            m_FontAssetCreationSettingsContainer.fontAssetCreationSettings.Add(settings);

            // Since list should only contain the most 4 recent settings, we remove the first element if list exceeds 4 elements.
            if (m_FontAssetCreationSettingsContainer.fontAssetCreationSettings.Count > 4)
                m_FontAssetCreationSettingsContainer.fontAssetCreationSettings.RemoveAt(0);

            m_FontAssetCreationSettingsCurrentIndex = m_FontAssetCreationSettingsContainer.fontAssetCreationSettings.Count - 1;

            // Serialize list to JSON
            string serializedSettings = JsonUtility.ToJson(m_FontAssetCreationSettingsContainer, true);

            EditorPrefs.SetString(k_FontAssetCreationSettingsContainerKey, serializedSettings);
        }

        void DrawPreview()
        {
            Rect pixelRect;

            float ratioX = (position.width - k_TwoColumnControlsWidth) / m_AtlasWidth;
            float ratioY = (position.height - 15) / m_AtlasHeight;

            if (position.width < position.height)
            {
                ratioX = (position.width - 15) / m_AtlasWidth;
                ratioY = (position.height - 485) / m_AtlasHeight;
            }

            if (ratioX < ratioY)
            {
                float width = m_AtlasWidth * ratioX;
                float height = m_AtlasHeight * ratioX;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(width), GUILayout.MaxHeight(height));

                pixelRect = GUILayoutUtility.GetRect(width - 5, height, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
            }
            else
            {
                float width = m_AtlasWidth * ratioY;
                float height = m_AtlasHeight * ratioY;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(width), GUILayout.MaxHeight(height));

                pixelRect = GUILayoutUtility.GetRect(width - 5, height, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
            }

            if (m_FontAtlasTexture != null)
            {
                if (m_FontAtlasTexture.format == TextureFormat.Alpha8)
                    EditorGUI.DrawTextureAlpha(pixelRect, m_FontAtlasTexture, ScaleMode.StretchToFill);
                else
                    EditorGUI.DrawPreviewTexture(pixelRect, m_FontAtlasTexture, null, ScaleMode.StretchToFill);

                // Destroy GlyphRect preview texture
                if (m_GlyphRectPreviewTexture != null)
                {
                    DestroyImmediate(m_GlyphRectPreviewTexture);
                    m_GlyphRectPreviewTexture = null;
                }
            }
            else if (m_GlyphRectPreviewTexture != null)
            {
                EditorGUI.DrawPreviewTexture(pixelRect, m_GlyphRectPreviewTexture, null, ScaleMode.StretchToFill);
            }
            else if (m_SavedFontAtlas != null)
            {
                EditorGUI.DrawTextureAlpha(pixelRect, m_SavedFontAtlas, ScaleMode.StretchToFill);
            }

            EditorGUILayout.EndVertical();
        }

        void CheckForLegacyGlyphRenderMode()
        {
            // Special handling for legacy glyph render mode
            if ((int)m_GlyphRenderMode < 0x100)
            {
                switch ((int)m_GlyphRenderMode)
                {
                    case 0:
                        m_GlyphRenderMode = GlyphRenderMode.SMOOTH_HINTED;
                        break;
                    case 1:
                        m_GlyphRenderMode = GlyphRenderMode.SMOOTH;
                        break;
                    case 2:
                        m_GlyphRenderMode = GlyphRenderMode.RASTER_HINTED;
                        break;
                    case 3:
                        m_GlyphRenderMode = GlyphRenderMode.RASTER;
                        break;
                    case 6:
                    case 7:
                        m_GlyphRenderMode = GlyphRenderMode.SDFAA;
                        break;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        FontFeatureTable GetAllFontFeatures()
        {
            FontFeatureTable fontFeatureTable = new FontFeatureTable();

            PopulateGlyphAdjustmentTable(fontFeatureTable);
            PopulateLigatureTable(fontFeatureTable);
            PopulateDiacriticalMarkAdjustmentTables(fontFeatureTable);

            return fontFeatureTable;
        }

        void PopulateGlyphAdjustmentTable(FontFeatureTable fontFeatureTable)
        {
            GlyphPairAdjustmentRecord[] adjustmentRecords = FontEngine.GetPairAdjustmentRecords(m_AvailableGlyphsToAdd);

            if (adjustmentRecords == null)
                return;

            float emScale = (float)m_FaceInfo.pointSize / m_FaceInfo.unitsPerEM;

            for (int i = 0; i < adjustmentRecords.Length && adjustmentRecords[i].firstAdjustmentRecord.glyphIndex != 0; i++)
            {
                GlyphPairAdjustmentRecord record = adjustmentRecords[i];

                // Adjust values currently in Units per EM to make them relative to Sampling Point Size.
                GlyphValueRecord valueRecord = record.firstAdjustmentRecord.glyphValueRecord;
                valueRecord.xAdvance *= emScale;

                GlyphPairAdjustmentRecord newRecord = new GlyphPairAdjustmentRecord { firstAdjustmentRecord = new GlyphAdjustmentRecord { glyphIndex = record.firstAdjustmentRecord.glyphIndex, glyphValueRecord = valueRecord }, secondAdjustmentRecord = record.secondAdjustmentRecord };

                fontFeatureTable.glyphPairAdjustmentRecords.Add(newRecord);
            }

            fontFeatureTable.SortGlyphPairAdjustmentRecords();
        }

        void PopulateLigatureTable(FontFeatureTable fontFeatureTable)
        {
            UnityEngine.TextCore.LowLevel.LigatureSubstitutionRecord[] ligatureRecords = FontEngine.GetLigatureSubstitutionRecords(m_AvailableGlyphsToAdd);
            if (ligatureRecords != null)
                AddLigatureRecords(fontFeatureTable, ligatureRecords);
        }

        void AddLigatureRecords(FontFeatureTable fontFeatureTable, LigatureSubstitutionRecord[] records)
        {
            for (int i = 0; i < records.Length; i++)
            {
                LigatureSubstitutionRecord record = records[i];

                if (records[i].componentGlyphIDs == null || records[i].ligatureGlyphID == 0)
                    return;

                uint firstComponentGlyphIndex = record.componentGlyphIDs[0];

                LigatureSubstitutionRecord newRecord = new LigatureSubstitutionRecord() { componentGlyphIDs = record.componentGlyphIDs, ligatureGlyphID = record.ligatureGlyphID };

                // Add new record to lookup
                if (!fontFeatureTable.m_LigatureSubstitutionRecordLookup.ContainsKey(firstComponentGlyphIndex))
                {
                    fontFeatureTable.m_LigatureSubstitutionRecordLookup.Add(firstComponentGlyphIndex, new List<LigatureSubstitutionRecord> { newRecord });
                }
                else
                {
                    fontFeatureTable.m_LigatureSubstitutionRecordLookup[firstComponentGlyphIndex].Add(newRecord);
                }

                fontFeatureTable.m_LigatureSubstitutionRecords.Add(newRecord);
            }
        }

        void PopulateDiacriticalMarkAdjustmentTables(FontFeatureTable fontFeatureTable)
        {
            MarkToBaseAdjustmentRecord[] markToBaseRecords = FontEngine.GetMarkToBaseAdjustmentRecords(m_AvailableGlyphsToAdd);
            if (markToBaseRecords != null)
                AddMarkToBaseAdjustmentRecords(fontFeatureTable, markToBaseRecords);

            MarkToMarkAdjustmentRecord[] markToMarkRecords = FontEngine.GetMarkToMarkAdjustmentRecords(m_AvailableGlyphsToAdd);
            if (markToMarkRecords != null)
                AddMarkToMarkAdjustmentRecords(fontFeatureTable, markToMarkRecords);

        }

        void AddMarkToBaseAdjustmentRecords(FontFeatureTable fontFeatureTable, MarkToBaseAdjustmentRecord[] records)
        {
            float emScale = (float)m_FaceInfo.pointSize / m_FaceInfo.unitsPerEM;

            for (int i = 0; i < records.Length; i++)
            {
                MarkToBaseAdjustmentRecord record = records[i];

                uint key = record.markGlyphID << 16 | record.baseGlyphID;

                if (fontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.ContainsKey(key))
                    continue;

                MarkToBaseAdjustmentRecord newRecord = new MarkToBaseAdjustmentRecord {
                    baseGlyphID = record.baseGlyphID,
                    baseGlyphAnchorPoint = new GlyphAnchorPoint { xCoordinate = record.baseGlyphAnchorPoint.xCoordinate * emScale, yCoordinate = record.baseGlyphAnchorPoint.yCoordinate * emScale },
                    markGlyphID = record.markGlyphID,
                    markPositionAdjustment = new MarkPositionAdjustment { xPositionAdjustment = record.markPositionAdjustment.xPositionAdjustment * emScale, yPositionAdjustment = record.markPositionAdjustment.yPositionAdjustment * emScale } };

                fontFeatureTable.MarkToBaseAdjustmentRecords.Add(newRecord);
                fontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.Add(key, newRecord);
            }
        }

        void AddMarkToMarkAdjustmentRecords(FontFeatureTable fontFeatureTable, MarkToMarkAdjustmentRecord[] records)
        {
            float emScale = (float)m_FaceInfo.pointSize / m_FaceInfo.unitsPerEM;

            for (int i = 0; i < records.Length; i++)
            {
                MarkToMarkAdjustmentRecord record = records[i];

                uint key = record.combiningMarkGlyphID << 16 | record.baseMarkGlyphID;

                if (fontFeatureTable.m_MarkToMarkAdjustmentRecordLookup.ContainsKey(key))
                    continue;

                MarkToMarkAdjustmentRecord newRecord = new MarkToMarkAdjustmentRecord {
                    baseMarkGlyphID = record.baseMarkGlyphID,
                    baseMarkGlyphAnchorPoint = new GlyphAnchorPoint { xCoordinate = record.baseMarkGlyphAnchorPoint.xCoordinate * emScale, yCoordinate = record.baseMarkGlyphAnchorPoint.yCoordinate * emScale},
                    combiningMarkGlyphID = record.combiningMarkGlyphID,
                    combiningMarkPositionAdjustment = new MarkPositionAdjustment { xPositionAdjustment = record.combiningMarkPositionAdjustment.xPositionAdjustment * emScale, yPositionAdjustment = record.combiningMarkPositionAdjustment.yPositionAdjustment * emScale } };

                fontFeatureTable.MarkToMarkAdjustmentRecords.Add(newRecord);
                fontFeatureTable.m_MarkToMarkAdjustmentRecordLookup.Add(key, newRecord);
            }
        }
    }
}
