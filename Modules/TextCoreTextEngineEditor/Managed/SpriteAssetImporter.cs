// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

using TextAsset = UnityEngine.TextAsset;
using GlyphRect = UnityEngine.TextCore.GlyphRect;
using GlyphMetrics = UnityEngine.TextCore.GlyphMetrics;


namespace UnityEditor.TextCore.Text
{
    internal class SpriteAssetImporter : EditorWindow
    {
        // Create Sprite Asset Editor Window
        [MenuItem("Window/Text/Sprite Importer", false, 2026)]
        internal static void ShowFontAtlasCreatorWindow()
        {
            var window = GetWindow<SpriteAssetImporter>();
            window.titleContent = new GUIContent("Sprite Importer");
            window.Focus();
        }

        Texture2D m_SpriteAtlas;
        SpriteAssetImportFormats m_SpriteDataFormat = SpriteAssetImportFormats.TexturePackerJsonArray;
        TextAsset m_JsonFile;

        string m_CreationFeedback;

        SpriteAsset m_SpriteAsset;

        /// <summary>
        ///
        /// </summary>
        void OnEnable()
        {
            // Set Editor Window Size
            SetEditorWindowSize();
        }

        /// <summary>
        ///
        /// </summary>
        public void OnGUI()
        {
            DrawEditorPanel();
        }

        /// <summary>
        ///
        /// </summary>
        private void OnDisable()
        {
            // Clean up sprite asset object that may have been created and not saved.
            if (m_SpriteAsset != null && !EditorUtility.IsPersistent(m_SpriteAsset))
                DestroyImmediate(m_SpriteAsset);
        }

        /// <summary>
        ///
        /// </summary>
        void DrawEditorPanel()
        {
            // label
            GUILayout.Label("Import Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            // Sprite Texture Selection
            m_JsonFile = EditorGUILayout.ObjectField("Sprite Data Source", m_JsonFile, typeof(TextAsset), false) as TextAsset;

            m_SpriteDataFormat = (SpriteAssetImportFormats)EditorGUILayout.EnumPopup("Import Format", m_SpriteDataFormat);

            // Sprite Texture Selection
            m_SpriteAtlas = EditorGUILayout.ObjectField("Sprite Texture Atlas", m_SpriteAtlas, typeof(Texture2D), false) as Texture2D;

            if (EditorGUI.EndChangeCheck())
            {
                m_CreationFeedback = string.Empty;
            }

            GUILayout.Space(10);

            GUI.enabled = m_JsonFile != null && m_SpriteAtlas != null && m_SpriteDataFormat != SpriteAssetImportFormats.None;

            // Create Sprite Asset
            if (GUILayout.Button("Create Sprite Asset"))
            {
                m_CreationFeedback = string.Empty;

                // Clean up sprite asset object that may have been previously created.
                if (m_SpriteAsset != null && !EditorUtility.IsPersistent(m_SpriteAsset))
                    DestroyImmediate(m_SpriteAsset);

                // Read json data file
                if (m_JsonFile != null)
                {
                    switch (m_SpriteDataFormat)
                    {
                        case SpriteAssetImportFormats.TexturePackerJsonArray:
                            TexturePacker_JsonArray.SpriteDataObject jsonData = null;
                            try
                            {
                                jsonData = JsonUtility.FromJson<TexturePacker_JsonArray.SpriteDataObject>(m_JsonFile.text);
                            }
                            catch
                            {
                                m_CreationFeedback = "The Sprite Data Source file [" + m_JsonFile.name + "] appears to be invalid or incorrectly formatted.";
                            }

                            if (jsonData != null && jsonData.frames != null && jsonData.frames.Count > 0)
                            {
                                int spriteCount = jsonData.frames.Count;

                                // Update import results
                                m_CreationFeedback = "<b>Import Results</b>\n--------------------\n";
                                m_CreationFeedback += "<color=#C0ffff><b>" + spriteCount + "</b></color> Sprites were imported from file.";

                                // Create new Sprite Asset
                                m_SpriteAsset = CreateInstance<SpriteAsset>();

                                // Assign sprite sheet / atlas texture to sprite asset
                                m_SpriteAsset.spriteSheet = m_SpriteAtlas;

                                List<SpriteGlyph> spriteGlyphTable = new List<SpriteGlyph>();
                                List<SpriteCharacter> spriteCharacterTable = new List<SpriteCharacter>();

                                PopulateSpriteTables(jsonData, spriteCharacterTable, spriteGlyphTable);

                                m_SpriteAsset.spriteCharacterTable = spriteCharacterTable;
                                m_SpriteAsset.spriteGlyphTable = spriteGlyphTable;
                            }
                            break;
                    }
                }
            }

            GUI.enabled = true;

            // Creation Feedback
            GUILayout.Space(5);
            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(60));
            {
                EditorGUILayout.TextArea(m_CreationFeedback, TM_EditorStyles.label);
            }
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUI.enabled = m_JsonFile != null && m_SpriteAtlas && m_SpriteAsset != null;
            if (GUILayout.Button("Save Sprite Asset") && m_JsonFile != null)
            {
                string filePath = EditorUtility.SaveFilePanel("Save Sprite Asset File", new FileInfo(AssetDatabase.GetAssetPath(m_JsonFile)).DirectoryName, m_JsonFile.name, "asset");

                if (filePath.Length == 0)
                    return;

                SaveSpriteAsset(filePath);
            }
            GUI.enabled = true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="spriteDataObject"></param>
        /// <param name="spriteCharacterTable"></param>
        /// <param name="spriteGlyphTable"></param>
        private static void PopulateSpriteTables(TexturePacker_JsonArray.SpriteDataObject spriteDataObject, List<SpriteCharacter> spriteCharacterTable, List<SpriteGlyph> spriteGlyphTable)
        {
            List<TexturePacker_JsonArray.Frame> importedSprites = spriteDataObject.frames;

            float atlasHeight = spriteDataObject.meta.size.h;

            for (int i = 0; i < importedSprites.Count; i++)
            {
                TexturePacker_JsonArray.Frame spriteData = importedSprites[i];

                SpriteGlyph spriteGlyph = new SpriteGlyph();
                spriteGlyph.index = (uint)i;

                spriteGlyph.metrics = new GlyphMetrics((int)spriteData.frame.w, (int)spriteData.frame.h, -spriteData.frame.w * spriteData.pivot.x, spriteData.frame.h * spriteData.pivot.y, (int)spriteData.frame.w);
                spriteGlyph.glyphRect = new GlyphRect((int)spriteData.frame.x, (int)(atlasHeight - spriteData.frame.h - spriteData.frame.y), (int)spriteData.frame.w, (int)spriteData.frame.h);
                spriteGlyph.scale = 1.0f;

                spriteGlyphTable.Add(spriteGlyph);

                SpriteCharacter spriteCharacter = new SpriteCharacter(0, spriteGlyph);
                spriteCharacter.name = spriteData.filename.Split('.')[0];
                spriteCharacter.unicode = 0xFFFE;
                spriteCharacter.scale = 1.0f;

                spriteCharacterTable.Add(spriteCharacter);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="filePath"></param>
        void SaveSpriteAsset(string filePath)
        {
            filePath = filePath.Substring(0, filePath.Length - 6); // Trim file extension from filePath.

            string dataPath = Application.dataPath;

            if (filePath.IndexOf(dataPath, System.StringComparison.InvariantCultureIgnoreCase) == -1)
            {
                Debug.LogError("You're saving the font asset in a directory outside of this project folder. This is not supported. Please select a directory under \"" + dataPath + "\"");
                return;
            }

            string relativeAssetPath = filePath.Substring(dataPath.Length - 6);
            string dirName = Path.GetDirectoryName(relativeAssetPath);
            string fileName = Path.GetFileNameWithoutExtension(relativeAssetPath);
            string pathNoExt = dirName + "/" + fileName;

            // Save Sprite Asset
            AssetDatabase.CreateAsset(m_SpriteAsset, pathNoExt + ".asset");

            // Set version number
            m_SpriteAsset.version = "1.1.0";

            // Add new default material for sprite asset.
            AddDefaultMaterial(m_SpriteAsset);
        }

        /// <summary>
        /// Create and add new default material to sprite asset.
        /// </summary>
        /// <param name="spriteAsset"></param>
        static void AddDefaultMaterial(SpriteAsset spriteAsset)
        {
            Shader shader = Shader.Find("Text/Sprite");
            Material material = new Material(shader);
            material.SetTexture(TextShaderUtilities.ID_MainTex, spriteAsset.spriteSheet);

            spriteAsset.material = material;
            material.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(material, spriteAsset);
        }

        /// <summary>
        /// Limits the minimum size of the editor window.
        /// </summary>
        void SetEditorWindowSize()
        {
            EditorWindow editorWindow = this;

            Vector2 currentWindowSize = editorWindow.minSize;

            editorWindow.minSize = new Vector2(Mathf.Max(230, currentWindowSize.x), Mathf.Max(300, currentWindowSize.y));
        }
    }
}
