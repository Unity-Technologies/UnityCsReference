// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace UnityEditor.TextCore.Text
{
    internal static class TM_EditorStyles
    {
        public static GUIStyle panelTitle;
        public static GUIStyle sectionHeader;
        public static GUIStyle textAreaBoxWindow;

        public static GUIStyle label;
        public static GUIStyle leftLabel;
        public static GUIStyle centeredLabel;
        public static GUIStyle rightLabel;

        public static Texture2D sectionHeaderStyleTexture;

        static TM_EditorStyles()
        {
            // Section Header
            CreateSectionHeaderStyle();

            // Labels
            panelTitle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
            label = leftLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft, richText = true, wordWrap = true, stretchWidth = true };
            centeredLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = true, stretchWidth = true };
            rightLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight, richText = true, wordWrap = true, stretchWidth = true };

            textAreaBoxWindow = new GUIStyle(EditorStyles.textArea) { richText = true };
        }

        internal static void CreateSectionHeaderStyle()
        {
            sectionHeader = new GUIStyle(EditorStyles.textArea) { fixedHeight = 22, richText = true, overflow = new RectOffset(9, 0, 0, 0), padding = new RectOffset(0, 0, 4, 0) };
            sectionHeaderStyleTexture = new Texture2D(1, 1);

            if (EditorGUIUtility.isProSkin)
                sectionHeaderStyleTexture.SetPixel(1, 1, new Color(0.4f, 0.4f, 0.4f, 0.5f));
            else
                sectionHeaderStyleTexture.SetPixel(1, 1, new Color(0.6f, 0.6f, 0.6f, 0.5f));

            sectionHeaderStyleTexture.Apply();
            sectionHeader.normal.background = sectionHeaderStyleTexture;
        }

        internal static void RefreshEditorStyles()
        {
            if (sectionHeader.normal.background == null)
            {
                Texture2D sectionHeaderStyleTexture = new Texture2D(1, 1);

                if (EditorGUIUtility.isProSkin)
                    sectionHeaderStyleTexture.SetPixel(1, 1, new Color(0.4f, 0.4f, 0.4f, 0.5f));
                else
                    sectionHeaderStyleTexture.SetPixel(1, 1, new Color(0.6f, 0.6f, 0.6f, 0.5f));

                sectionHeaderStyleTexture.Apply();
                sectionHeader.normal.background = sectionHeaderStyleTexture;
            }
        }
    }


    internal class TextCoreEditorUtilities
    {
        /// <summary>
        /// Function which returns a string containing a sequence of Decimal character ranges.
        /// </summary>
        /// <param name="characterSet"></param>
        /// <returns></returns>
        internal static string GetDecimalCharacterSequence(int[] characterSet)
        {
            if (characterSet == null || characterSet.Length == 0)
                return string.Empty;

            string characterSequence = string.Empty;
            int count = characterSet.Length;
            int first = characterSet[0];
            int last = first;

            for (int i = 1; i < count; i++)
            {
                if (characterSet[i - 1] + 1 == characterSet[i])
                {
                    last = characterSet[i];
                }
                else
                {
                    if (first == last)
                        characterSequence += first + ",";
                    else
                        characterSequence += first + "-" + last + ",";

                    first = last = characterSet[i];
                }
            }

            // handle the final group
            if (first == last)
                characterSequence += first;
            else
                characterSequence += first + "-" + last;

            return characterSequence;
        }

        /// <summary>
        /// Function which returns a string containing a sequence of Unicode (Hex) character ranges.
        /// </summary>
        /// <param name="characterSet"></param>
        /// <returns></returns>
        internal static string GetUnicodeCharacterSequence(int[] characterSet)
        {
            if (characterSet == null || characterSet.Length == 0)
                return string.Empty;

            string characterSequence = string.Empty;
            int count = characterSet.Length;
            int first = characterSet[0];
            int last = first;

            for (int i = 1; i < count; i++)
            {
                if (characterSet[i - 1] + 1 == characterSet[i])
                {
                    last = characterSet[i];
                }
                else
                {
                    if (first == last)
                        characterSequence += first.ToString("X2") + ",";
                    else
                        characterSequence += first.ToString("X2") + "-" + last.ToString("X2") + ",";

                    first = last = characterSet[i];
                }
            }

            // handle the final group
            if (first == last)
                characterSequence += first.ToString("X2");
            else
                characterSequence += first.ToString("X2") + "-" + last.ToString("X2");

            return characterSequence;
        }

        internal static Material[] FindMaterialReferences(FontAsset fontAsset)
        {
            List<Material> refs = new List<Material>();
            Material mat = fontAsset.material;
            refs.Add(mat);

            // Get materials matching the search pattern.
            string searchPattern = "t:Material" + " " + fontAsset.name.Split(new char[] { ' ' })[0];
            string[] materialAssetGUIDs = AssetDatabase.FindAssets(searchPattern);

            for (int i = 0; i < materialAssetGUIDs.Length; i++)
            {
                string materialPath = AssetDatabase.GUIDToAssetPath(materialAssetGUIDs[i]);
                Material targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (targetMaterial.HasProperty(TextShaderUtilities.ID_MainTex) && targetMaterial.GetTexture(TextShaderUtilities.ID_MainTex) != null && mat.GetTexture(TextShaderUtilities.ID_MainTex) != null && targetMaterial.GetTexture(TextShaderUtilities.ID_MainTex).GetInstanceID() == mat.GetTexture(TextShaderUtilities.ID_MainTex).GetInstanceID())
                {
                    if (!refs.Contains(targetMaterial))
                        refs.Add(targetMaterial);
                }
                else
                {
                    // TODO: Find a more efficient method to unload resources.
                    //Resources.UnloadAsset(targetMaterial.GetTexture(ShaderUtilities.ID_MainTex));
                }
            }

            return refs.ToArray();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="thickness"></param>
        /// <param name="color"></param>
        internal static void DrawBox(Rect rect, float thickness, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.y + thickness, rect.width + thickness * 2, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.y + thickness, thickness, rect.height - thickness * 2), color);
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.y + rect.height - thickness * 2, rect.width + thickness * 2, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x + rect.width, rect.y + thickness, thickness, rect.height - thickness * 2), color);
        }

        internal static void DrawColorProperty(Rect rect, SerializedProperty property)
        {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            if (EditorGUIUtility.wideMode)
            {
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, 50f, rect.height), property, GUIContent.none);
                rect.x += 50f;
                rect.width = Mathf.Min(100f, rect.width - 55f);
            }
            else
            {
                rect.height /= 2f;
                rect.width = Mathf.Min(100f, rect.width - 5f);
                EditorGUI.PropertyField(rect, property, GUIContent.none);
                rect.y += rect.height;
            }

            EditorGUI.BeginChangeCheck();
            string colorString = EditorGUI.TextField(rect, string.Format("#{0}", ColorUtility.ToHtmlStringRGBA(property.colorValue)));
            if (EditorGUI.EndChangeCheck())
            {
                Color color;
                if (ColorUtility.TryParseHtmlString(colorString, out color))
                {
                    property.colorValue = color;
                }
            }
            EditorGUI.indentLevel = oldIndent;
        }

        internal static bool EditorToggle(Rect position, bool value, GUIContent content, GUIStyle style)
        {
            var id = GUIUtility.GetControlID(content, FocusType.Keyboard, position);
            var evt = Event.current;

            // Toggle selected toggle on space or return key
            if (GUIUtility.keyboardControl == id && evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
            {
                value = !value;
                evt.Use();
                GUI.changed = true;
            }

            if (evt.type == EventType.MouseDown && position.Contains(Event.current.mousePosition))
            {
                GUIUtility.keyboardControl = id;
                EditorGUIUtility.editingTextField = false;
                HandleUtility.Repaint();
            }

            return GUI.Toggle(position, id, value, content, style);
        }
    }
}
