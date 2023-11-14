// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor
{
    [CustomEditor(typeof(ShaderVariantCollection))]
    internal class ShaderVariantCollectionInspector : Editor
    {
        private class Styles
        {
            public static readonly GUIContent iconAdd    = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add variant");
            public static readonly GUIContent iconRemove = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove entry");
            public static readonly GUIStyle invisibleButton = "InvisibleButton";
        }

        SerializedProperty m_Shaders;


        public virtual void OnEnable()
        {
            m_Shaders = serializedObject.FindProperty("m_Shaders");
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (serializedObject == null)
                return null;
            var root = new VisualElement();
            var monoBehaviour = target as MonoBehaviour;
            var scriptableObject = target as ScriptableObject;
            if ((monoBehaviour != null || scriptableObject != null) && SerializationUtility.HasManagedReferencesWithMissingTypes(target))
            {
                string missingSerializeMessage = L10n.Tr("This object contains SerializeReference types which are missing.\nFor more information see SerializationUtility.HasManagedReferencesWithMissingTypes.");
                root.Add(new HelpBox(missingSerializeMessage, HelpBoxMessageType.Warning));
            }

            UIElements.InspectorElement.FillDefaultInspector(root, serializedObject, this);

            return root;
        }

        static Rect GetAddRemoveButtonRect(Rect r)
        {
            var buttonSize = Styles.invisibleButton.CalcSize(Styles.iconRemove);
            return new Rect(r.xMax - buttonSize.x, r.y + (int)(r.height / 2 - buttonSize.y / 2), buttonSize.x, buttonSize.y);
        }

        // Show window to select shader variants
        void DisplayAddVariantsWindow(Shader shader, ShaderVariantCollection collection)
        {
            var data = new AddShaderVariantWindow.PopupData();
            data.shader = shader;
            data.collection = collection;
            AddShaderVariantWindow.ShowAddVariantWindow(data);
            GUIUtility.ExitGUI();
        }

        void DrawShaderEntry(int shaderIndex)
        {
            var entryProp = m_Shaders.GetArrayElementAtIndex(shaderIndex);
            Shader shader = (Shader)entryProp.FindPropertyRelative("first").objectReferenceValue;

            // Shader name and button to remove it
            var variantsProp = entryProp.FindPropertyRelative("second.variants");
            using (new GUILayout.HorizontalScope())
            {
                Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.boldLabel);
                Rect minusRect = GetAddRemoveButtonRect(rowRect);
                rowRect.xMax = minusRect.x;

                GUI.Label(rowRect, shader == null ? "<MISSING SHADER>" : shader.name, EditorStyles.boldLabel);

                if (GUI.Button(minusRect, Styles.iconRemove, Styles.invisibleButton))
                {
                    m_Shaders.DeleteArrayElementAtIndex(shaderIndex);
                    return;
                }
            }

            // Variants for this shader
            for (var i = 0; i < variantsProp.arraySize; ++i)
            {
                var prop = variantsProp.GetArrayElementAtIndex(i);
                var keywords = prop.FindPropertyRelative("keywords").stringValue;
                if (string.IsNullOrEmpty(keywords))
                    keywords = "<no keywords>";
                var passType = (UnityEngine.Rendering.PassType)prop.FindPropertyRelative("passType").intValue;
                Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.miniLabel);
                Rect minusRect = GetAddRemoveButtonRect(rowRect);


                // Variant entry with button to remove it
                rowRect.xMax = minusRect.x;
                GUI.Label(rowRect, passType + " " + keywords, EditorStyles.miniLabel);
                if (GUI.Button(minusRect, Styles.iconRemove, Styles.invisibleButton))
                {
                    variantsProp.DeleteArrayElementAtIndex(i);
                }
            }

            // Add variant button
            Rect addRowRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.miniLabel);
            Rect plusRect = GetAddRemoveButtonRect(addRowRect);

            if (shader != null && GUI.Button(plusRect, Styles.iconAdd, Styles.invisibleButton))
            {
                DisplayAddVariantsWindow(shader, target as ShaderVariantCollection);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            for (var i = 0; i < m_Shaders.arraySize; ++i)
            {
                DrawShaderEntry(i);
            }

            // Button to add a new shader to collection
            if (GUILayout.Button("Add shader"))
            {
                // Show object selector
                ObjectSelector.get.Show(null, typeof(Shader), null, false);
                ObjectSelector.get.objectSelectorID = "ShaderVariantSelector".GetHashCode();
                GUIUtility.ExitGUI();
            }
            if (Event.current.type == EventType.ExecuteCommand)
            {
                // New shader picked in object selector; add it to the collection
                if (Event.current.commandName == ObjectSelector.ObjectSelectorClosedCommand && ObjectSelector.get.objectSelectorID == "ShaderVariantSelector".GetHashCode())
                {
                    var newShader = ObjectSelector.GetCurrentObject() as Shader;
                    if (newShader != null)
                    {
                        ShaderUtil.AddNewShaderToCollection(newShader, target as ShaderVariantCollection);
                    }
                    Event.current.Use();
                    GUIUtility.ExitGUI();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }


    // Utility window for selecting shader variants to add.
    // Some shaders (Standard, SpeedTree etc.) have massive amounts of variants
    // (e.g. about 30000), and approaches like "display a popup menu with all of them"
    // don't work.
    internal class AddShaderVariantWindow : EditorWindow
    {
        internal class PopupData
        {
            public Shader shader;
            public ShaderVariantCollection collection;
        }

        class Styles
        {
            public static readonly GUIStyle sMenuItem = "MenuItem";
            public static readonly GUIStyle sSeparator = "sv_iconselector_sep";
        }

        const float kMargin = 2;
        const float kSpaceHeight = 6;
        const float kSeparatorHeight = 3;

        private const float kMinWindowWidth = 400;
        static readonly float kMiscUIHeight =
            5 * EditorGUI.kSingleLineHeight +
            kSeparatorHeight * 2 +
            kSpaceHeight * 5 +
            kMargin * 2;

        private static readonly float kMinWindowHeight = 9 * EditorGUI.kSingleLineHeight + kMiscUIHeight;

        PopupData m_Data;
        List<string> m_SelectedKeywords;
        List<string> m_AvailableKeywords;
        List<int> m_SelectedVariants; // Indices of variants currently selected for adding

        int[] m_FilteredVariantTypes;
        string[][] m_FilteredVariantKeywords;

        int m_MaxVisibleVariants;
        int m_NumFilteredVariants;

        public AddShaderVariantWindow()
        {
            position = new Rect(100, 100, kMinWindowWidth * 1.5f, kMinWindowHeight * 1.5f);
            minSize = new Vector2(kMinWindowWidth, kMinWindowHeight);
            wantsMouseMove = true;
        }

        private void Initialize(PopupData data)
        {
            m_Data = data;
            m_SelectedKeywords = new List<string>();
            m_AvailableKeywords = new List<string>();
            m_SelectedVariants = new List<int>();
            ApplyKeywordFilter();
        }

        public static void ShowAddVariantWindow(PopupData data)
        {
            var w = EditorWindow.GetWindow<AddShaderVariantWindow>(true, "Add shader " + data.shader.name + " variants to collection");
            w.Initialize(data);
            w.m_Parent.window.m_DontSaveToLayout = true;
        }

        void ApplyKeywordFilter()
        {
            m_MaxVisibleVariants = (int)(CalcVerticalSpaceForVariants() / EditorGUI.kSingleLineHeight);
            string[] keywordLists, remainingKeywords;
            m_FilteredVariantTypes = new int[m_MaxVisibleVariants];

            ShaderUtil.GetShaderVariantEntriesFiltered(m_Data.shader,
                m_MaxVisibleVariants + 1,                                         // query one more to know if we're truncating
                m_SelectedKeywords.ToArray(),
                m_Data.collection,
                out m_FilteredVariantTypes,
                out keywordLists,
                out remainingKeywords);

            m_NumFilteredVariants = m_FilteredVariantTypes.Length;
            m_FilteredVariantKeywords = new string[m_NumFilteredVariants][];
            for (var i = 0; i < m_NumFilteredVariants; ++i)
            {
                m_FilteredVariantKeywords[i] = keywordLists[i].Split(' ');
            }

            m_AvailableKeywords.Clear();
            m_AvailableKeywords.InsertRange(0, remainingKeywords);
            m_AvailableKeywords.Sort();
        }

        public void OnGUI()
        {
            // Objects became deleted while our window was showing? Close.
            if (m_Data == null || m_Data.shader == null || m_Data.collection == null)
            {
                Close();
                return;
            }

            // We do not use the layout event
            if (Event.current.type == EventType.Layout)
                return;

            Rect rect = new Rect(0, 0, position.width, position.height);
            Draw(rect);

            // Repaint on mouse move so we get hover highlights in menu item rows
            if (Event.current.type == EventType.MouseMove)
                Repaint();
        }

        private bool KeywordButton(Rect buttonRect, string k, Vector2 areaSize)
        {
            // If we can't fit all buttons (shader has *a lot* of keywords) and would start clipping,
            // do display the partially clipped ones with some transparency.
            var oldColor = GUI.color;
            if (buttonRect.yMax > areaSize.y)
                GUI.color = new Color(1, 1, 1, 0.4f);

            var result = GUI.Button(buttonRect, EditorGUIUtility.TempContent(k), EditorStyles.miniButton);
            GUI.color = oldColor;
            return result;
        }

        float CalcVerticalSpaceForKeywords()
        {
            return Mathf.Floor((position.height - kMiscUIHeight) / 4);
        }

        float CalcVerticalSpaceForVariants()
        {
            return (position.height - kMiscUIHeight) / 2;
        }

        void DrawKeywordsList(ref Rect rect, List<string> keywords, bool clickingAddsToSelected)
        {
            rect.height = CalcVerticalSpaceForKeywords();
            var displayKeywords = keywords.Select(k => k.ToLowerInvariant()).ToList();

            GUI.BeginGroup(rect);
            Rect indentRect = new Rect(4, 0, rect.width, rect.height);
            var layoutRects = EditorGUIUtility.GetFlowLayoutedRects(indentRect, EditorStyles.miniButton, 2, 2, displayKeywords);
            for (var i = 0; i < displayKeywords.Count; ++i)
            {
                if (KeywordButton(layoutRects[i], displayKeywords[i], rect.size))
                {
                    if (clickingAddsToSelected)
                    {
                        if (!m_SelectedKeywords.Contains(keywords[i]))
                        {
                            m_SelectedKeywords.Add(keywords[i]);
                            m_SelectedKeywords.Sort();
                            m_AvailableKeywords.Remove(keywords[i]);
                        }
                    }
                    else
                    {
                        m_AvailableKeywords.Add(keywords[i]);
                        m_SelectedKeywords.Remove(keywords[i]);
                    }
                    ApplyKeywordFilter();
                    GUIUtility.ExitGUI();
                }
            }
            GUI.EndGroup();
            rect.y += rect.height;
        }

        void DrawSectionHeader(ref Rect rect, string titleString, bool separator)
        {
            // space
            rect.y += kSpaceHeight;
            // separator
            if (separator)
            {
                rect.height = kSeparatorHeight;
                GUI.Label(rect, GUIContent.none, Styles.sSeparator);
                rect.y += rect.height;
            }
            // label
            rect.height = EditorGUI.kSingleLineHeight;
            GUI.Label(rect, titleString);
            rect.y += rect.height;
        }

        private void Draw(Rect windowRect)
        {
            var rect = new Rect(kMargin, kMargin, windowRect.width - kMargin * 2, EditorGUI.kSingleLineHeight);

            DrawSectionHeader(ref rect, "Pick shader keywords to narrow down variant list:", false);
            DrawKeywordsList(ref rect, m_AvailableKeywords, true);

            DrawSectionHeader(ref rect, "Selected keywords:", true);
            DrawKeywordsList(ref rect, m_SelectedKeywords, false);

            DrawSectionHeader(ref rect, "Shader variants with these keywords (click to select):", true);

            if (m_NumFilteredVariants > 0)
            {
                int maxFilteredLength = (int)(CalcVerticalSpaceForVariants() / EditorGUI.kSingleLineHeight);

                if (maxFilteredLength > m_MaxVisibleVariants) // Query data again if we have bigger window than at last query
                    ApplyKeywordFilter();

                // Display first N variants (don't want to display thousands of them if filter is not narrow)
                for (var i = 0; i < Mathf.Min(m_NumFilteredVariants, maxFilteredLength); ++i)
                {
                    var passType = (UnityEngine.Rendering.PassType)m_FilteredVariantTypes[i];
                    var wasSelected = m_SelectedVariants.Contains(i);
                    var keywordString = string.IsNullOrEmpty(m_FilteredVariantKeywords[i][0]) ? "<no keywords>" : string.Join(" ", m_FilteredVariantKeywords[i]);
                    var displayString = passType + " " + keywordString.ToLowerInvariant();
                    var isSelected = GUI.Toggle(rect, wasSelected, displayString, Styles.sMenuItem);
                    rect.y += rect.height;

                    if (isSelected && !wasSelected)
                        m_SelectedVariants.Add(i);
                    else if (!isSelected && wasSelected)
                        m_SelectedVariants.Remove(i);
                }

                // show how many variants we skipped due to filter not being narrow enough
                if (m_NumFilteredVariants > maxFilteredLength)
                {
                    GUI.Label(rect, "List of variants was cropped. Pick further keywords to narrow the selection.", EditorStyles.miniLabel);
                    rect.y += rect.height;
                }
            }
            else
            {
                GUI.Label(rect, "No variants with these keywords");
                rect.y += rect.height;
            }

            // Button to add them at the bottom of popup
            rect.y = windowRect.height - kMargin - kSpaceHeight - EditorGUI.kSingleLineHeight;
            rect.height = EditorGUI.kSingleLineHeight;
            // Disable button if no variants selected
            using (new EditorGUI.DisabledScope(m_SelectedVariants.Count == 0))
            {
                if (GUI.Button(rect, string.Format("Add {0} selected variants", m_SelectedVariants.Count)))
                {
                    // Add the selected variants
                    Undo.RecordObject(m_Data.collection, "Add variant");
                    for (var i = 0; i < m_SelectedVariants.Count; ++i)
                    {
                        var index = m_SelectedVariants[i];
                        var variant = new ShaderVariantCollection.ShaderVariant(m_Data.shader, (UnityEngine.Rendering.PassType)m_FilteredVariantTypes[index], m_FilteredVariantKeywords[index]);
                        m_Data.collection.Add(variant);
                    }
                    // Close our popup
                    Close();
                    GUIUtility.ExitGUI();
                }
            }
        }
    }
}
