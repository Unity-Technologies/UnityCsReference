// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor
{
    [CustomEditor(typeof(TrueTypeFontImporter))]
    [CanEditMultipleObjects]
    internal class TrueTypeFontImporterInspector : AssetImporterEditor
    {
        SerializedProperty m_FontSize;
        SerializedProperty m_TextureCase;
        SerializedProperty m_IncludeFontData;
        SerializedProperty m_FontNamesArraySize;
        SerializedProperty m_CustomCharacters;
        SerializedProperty m_FontRenderingMode;
        SerializedProperty m_AscentCalculationMode;
        SerializedProperty m_UseLegacyBoundsCalculation;
        SerializedProperty m_FallbackFontReferencesArraySize;

        string m_FontNamesString = "";
        string m_DefaultFontNamesString = "";
        bool? m_FormatSupported = null;
        bool m_ReferencesExpanded = false;
        bool m_GotFontNamesFromImporter = false;
        Font[] m_FallbackFontReferences = null;

        public override void OnEnable()
        {
            m_FontSize = serializedObject.FindProperty("m_FontSize");
            m_TextureCase = serializedObject.FindProperty("m_ForceTextureCase");
            m_IncludeFontData = serializedObject.FindProperty("m_IncludeFontData");
            m_FontNamesArraySize = serializedObject.FindProperty("m_FontNames.Array.size");
            m_CustomCharacters = serializedObject.FindProperty("m_CustomCharacters");
            m_FontRenderingMode = serializedObject.FindProperty("m_FontRenderingMode");
            m_AscentCalculationMode = serializedObject.FindProperty("m_AscentCalculationMode");
            m_UseLegacyBoundsCalculation = serializedObject.FindProperty("m_UseLegacyBoundsCalculation");
            m_FallbackFontReferencesArraySize = serializedObject.FindProperty("m_FallbackFontReferences.Array.size");

            // We don't want to expose GUI for setting included fonts when selecting multiple fonts
            if (targets.Length == 1)
            {
                m_DefaultFontNamesString = GetDefaultFontNames();
                m_FontNamesString = GetFontNames();
                var importer = (TrueTypeFontImporter)target;
                m_FallbackFontReferences = importer.fontReferences;
            }
        }

        protected override void Apply()
        {
            m_FallbackFontReferencesArraySize.intValue = m_FallbackFontReferences.Length;
            SerializedProperty fontReferenceProp = m_FallbackFontReferencesArraySize.Copy();

            for (int i = 0; i < m_FallbackFontReferences.Length; i++)
            {
                fontReferenceProp.Next(false);
                fontReferenceProp.objectReferenceValue = m_FallbackFontReferences[i];
            }

            base.Apply();
        }

        string GetDefaultFontNames()
        {
            return ((TrueTypeFontImporter)target).fontTTFName;
        }

        string GetFontNames()
        {
            var importer = (TrueTypeFontImporter)target;

            string joinedFontNames = string.Join(", ", importer.fontNames);
            if (string.IsNullOrEmpty(joinedFontNames))
            {
                // Didn't get font names from importer
                // Fall back to default font name, and lookup names/fallbacks in SetFontNames
                joinedFontNames = m_DefaultFontNamesString;
            }
            else
            {
                // We've already got font names - don't look them up again
                m_GotFontNamesFromImporter = true;
            }
            return joinedFontNames;
        }

        void SetFontNames(string fontNames)
        {
            string[] names;
            if (!m_GotFontNamesFromImporter)
            {
                // The import settings have never been touched.
                names = new string[0];
            }
            else
            {
                // Split into array of font names
                // FIXME: Do we need to split and then trim? Or is the separator always ", "?
                names = fontNames.Split(',');
                for (int i = 0; i < names.Length; i++)
                    names[i] = names[i].Trim();
            }

            m_FontNamesArraySize.intValue = names.Length;
            SerializedProperty fontNameProp = m_FontNamesArraySize.Copy();
            for (int i = 0; i < names.Length; i++)
            {
                fontNameProp.Next(false);
                fontNameProp.stringValue = names[i];
            }

            var importer = (TrueTypeFontImporter)target;
            m_FallbackFontReferences = importer.LookupFallbackFontReferences(names);
        }

        private void ShowFormatUnsupportedGUI()
        {
            GUILayout.Space(5);
            EditorGUILayout.HelpBox("Format of selected font is not supported by Unity.", MessageType.Warning);
        }

        static GUIContent[] kCharacterStrings =
        {
            new GUIContent("Dynamic"),
            new GUIContent("Unicode"),
            new GUIContent("ASCII default set"),
            new GUIContent("ASCII upper case"),
            new GUIContent("ASCII lower case"),
            new GUIContent("Custom set")
        };
        static int[] kCharacterValues =
        {
            (int)FontTextureCase.Dynamic,
            (int)FontTextureCase.Unicode,
            (int)FontTextureCase.ASCII,
            (int)FontTextureCase.ASCIIUpperCase,
            (int)FontTextureCase.ASCIILowerCase,
            (int)FontTextureCase.CustomSet,
        };

        static GUIContent[] kRenderingModeStrings =
        {
            new GUIContent("Smooth"),
            new GUIContent("Hinted Smooth"),
            new GUIContent("Hinted Raster"),
            new GUIContent("OS Default"),
        };
        static int[] kRenderingModeValues =
        {
            (int)FontRenderingMode.Smooth,
            (int)FontRenderingMode.HintedSmooth,
            (int)FontRenderingMode.HintedRaster,
            (int)FontRenderingMode.OSDefault,
        };

        static GUIContent[] kAscentCalculationModeStrings =
        {
            new GUIContent("Legacy version 2 mode (glyph bounding boxes)"),
            new GUIContent("Face ascender metric"),
            new GUIContent("Face bounding box metric"),
        };
        static int[] kAscentCalculationModeValues =
        {
            (int)AscentCalculationMode.Legacy2x,
            (int)AscentCalculationMode.FaceAscender,
            (int)AscentCalculationMode.FaceBoundingBox,
        };

        static string GetUniquePath(string basePath, string extension)
        {
            for (int i = 0; i < 10000; i++)
            {
                string path = string.Format("{0}{1}.{2}", basePath, (i == 0 ? string.Empty : i.ToString()), extension);
                if (!File.Exists(path))
                    return path;
            }
            return "";
        }

        [MenuItem("CONTEXT/TrueTypeFontImporter/Create Editable Copy")]
        static void CreateEditableCopy(MenuCommand command)
        {
            var importer = (TrueTypeFontImporter)command.context;
            if (importer.fontTextureCase == FontTextureCase.Dynamic)
            {
                EditorUtility.DisplayDialog(
                    "Cannot generate editable font asset for dynamic fonts",
                    "Please reimport the font in a different mode.",
                    "Ok");
                return;
            }
            string basePath = Path.Combine(Path.GetDirectoryName(importer.assetPath), Path.GetFileNameWithoutExtension(importer.assetPath));
            EditorGUIUtility.PingObject(importer.GenerateEditableFont(GetUniquePath(basePath + "_copy", "fontsettings")));
        }

        public override void OnInspectorGUI()
        {
            if (!m_FormatSupported.HasValue)
            {
                m_FormatSupported = true;
                foreach (Object target in targets)
                {
                    var importer = target as TrueTypeFontImporter;
                    if (importer == null || !importer.IsFormatSupported())
                        m_FormatSupported = false;
                }
            }

            if (m_FormatSupported == false)
            {
                ShowFormatUnsupportedGUI();
                return;
            }

            EditorGUILayout.PropertyField(m_FontSize);
            if (m_FontSize.intValue < 1)
                m_FontSize.intValue = 1;
            if (m_FontSize.intValue > 500)
                m_FontSize.intValue = 500;

            EditorGUILayout.IntPopup(m_FontRenderingMode, kRenderingModeStrings, kRenderingModeValues, new GUIContent("Rendering Mode"));
            EditorGUILayout.IntPopup(m_TextureCase, kCharacterStrings, kCharacterValues, new GUIContent("Character"));
            EditorGUILayout.IntPopup(m_AscentCalculationMode, kAscentCalculationModeStrings, kAscentCalculationModeValues, new GUIContent("Ascent Calculation Mode"));
            EditorGUILayout.PropertyField(m_UseLegacyBoundsCalculation, new GUIContent("Use Legacy Bounds"));

            if (!m_TextureCase.hasMultipleDifferentValues)
            {
                if ((FontTextureCase)m_TextureCase.intValue != FontTextureCase.Dynamic)
                {
                    if ((FontTextureCase)m_TextureCase.intValue == FontTextureCase.CustomSet)
                    {
                        // Characters included
                        EditorGUI.BeginChangeCheck();
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel("Custom Chars");
                        EditorGUI.showMixedValue = m_CustomCharacters.hasMultipleDifferentValues;
                        string guiChars = EditorGUILayout.TextArea(m_CustomCharacters.stringValue, GUI.skin.textArea, GUILayout.MinHeight(EditorGUI.kSingleLineHeight * 2));
                        EditorGUI.showMixedValue = false;
                        GUILayout.EndHorizontal();
                        if (EditorGUI.EndChangeCheck())
                            m_CustomCharacters.stringValue = new string(guiChars.Distinct().Where(c => c != '\n' && c != '\r').ToArray());
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(m_IncludeFontData, new GUIContent("Incl. Font Data"));
                    // The default font names are different based on font so it'll be a mess if we show
                    // this GUI when multiple fonts are selected.
                    if (targets.Length == 1)
                    {
                        EditorGUI.BeginChangeCheck();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel("Font Names");
                        GUI.SetNextControlName("fontnames");
                        m_FontNamesString = EditorGUILayout.TextArea(m_FontNamesString, "TextArea", GUILayout.MinHeight(EditorGUI.kSingleLineHeight * 2));
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        using (new EditorGUI.DisabledScope(m_FontNamesString == m_DefaultFontNamesString))
                        {
                            if (GUILayout.Button("Reset", "MiniButton"))
                            {
                                GUI.changed = true;
                                if (GUI.GetNameOfFocusedControl() == "fontnames")
                                    GUIUtility.keyboardControl = 0;
                                m_FontNamesString = m_DefaultFontNamesString;
                            }
                        }
                        GUILayout.EndHorizontal();

                        if (EditorGUI.EndChangeCheck())
                            SetFontNames(m_FontNamesString);

                        m_ReferencesExpanded = EditorGUILayout.Foldout(m_ReferencesExpanded, "References to other fonts in project", true);
                        if (m_ReferencesExpanded)
                        {
                            EditorGUILayout.HelpBox("These are automatically generated by the inspector if any of the font names you supplied match fonts present in your project, which will then be used as fallbacks for this font.", MessageType.Info);

                            using (new EditorGUI.DisabledScope(true))
                            {
                                if (m_FallbackFontReferences != null && m_FallbackFontReferences.Length > 0)
                                {
                                    for (int i = 0; i < m_FallbackFontReferences.Length; ++i)
                                        EditorGUILayout.ObjectField(m_FallbackFontReferences[i], typeof(Font), false);
                                }
                                else
                                {
                                    GUILayout.Label("No references to other fonts in project.");
                                }
                            }
                        }
                    }
                }
            }

            ApplyRevertGUI();
        }
    }
}
