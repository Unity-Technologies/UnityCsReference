// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor.Experimental;

namespace UnityEditor
{
    internal class BrushList
    {
        int m_SelectedBrush = 0;
        Brush[] m_BrushList = null;
        Hash128[] m_BrushHashes = null;
        UnityEngine.Object[] m_FoldoutContent = new UnityEngine.Object[1];
        GUIContent[] m_Thumnails;
        Editor m_BrushEditor = null;
        bool m_ShowBrushSettings = false;

        // UI
        Vector2 m_ScrollPos;

        public int selectedIndex { get { return m_SelectedBrush; } }
        internal static class Styles
        {
            public static GUIStyle gridList = "GridList";
            public static GUIContent brushes = EditorGUIUtility.TrTextContent("Brushes");
        }

        public BrushList()
        {
            if (m_BrushList == null)
            {
                LoadBrushes();
                UpdateSelection(0);
            }
        }

        public void LoadBrushes()
        {
            // Load the textures;
            var arr = new List<Brush>();
            var hashes = new List<Hash128>();

            int idx = 1;
            Texture2D t = null;
            Brush brush = null;

            // Load builtin brushes from editor resources
            do
            {
                brush = (Brush)EditorGUIUtility.Load(EditorResources.brushesPath + "builtin_brush_" + idx + ".brush");
                if (brush != null && brush.m_Mask != null)
                {
                    brush.readOnly = true;
                    arr.Add(brush);
                    hashes.Add(brush.thumbnail.imageContentsHash);
                }

                idx++;
            }
            while (brush);

            // Load user created brushes from the Assets/Gizmos folder
            idx = 0;
            do
            {
                t = EditorGUIUtility.FindTexture("brush_" + idx + ".png");
                if (t)
                {
                    Brush b = Brush.CreateInstance(t, AnimationCurve.Constant(0, 1, 1), Brush.kMaxRadiusScale, true);
                    arr.Add(b);
                    hashes.Add(b.thumbnail.imageContentsHash);
                }
                idx++;
            }
            while (t);

            // Load .brush files
            foreach (string assetPath in AssetDatabase.FindAssets($"t:{typeof(Brush).Name}"))
            {
                var b = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assetPath), typeof(Brush)) as Brush;
                // We do not need to test whether b.texture is non-null because for Brush objects it is always true;
                // if no mask is assigned then the default one is used.
                if (b != null)
                {
                    arr.Add(b);
                    hashes.Add(b.thumbnail.imageContentsHash);
                }
            }

            m_BrushList = arr.ToArray();
            m_BrushHashes = hashes.ToArray();
        }

        public void SelectPrevBrush()
        {
            if (--m_SelectedBrush < 0)
                m_SelectedBrush = m_BrushList.Length - 1;
            UpdateSelection(m_SelectedBrush);
        }

        public void SelectNextBrush()
        {
            if (++m_SelectedBrush >= m_BrushList.Length)
                m_SelectedBrush = 0;
            UpdateSelection(m_SelectedBrush);
        }

        public void UpdateSelection(int newSelectedBrush)
        {
            m_SelectedBrush = newSelectedBrush;
            m_BrushEditor = Editor.CreateEditor(GetActiveBrush());
        }

        public Brush GetCircleBrush()
        {
            return m_BrushList[0];
        }

        public Brush GetActiveBrush()
        {
            if (m_SelectedBrush >= m_BrushList.Length)
                m_SelectedBrush = 0;
            return m_BrushList[m_SelectedBrush];
        }

        public bool ShowGUI()
        {
            bool repaint = false;

            // check if we need to update our thumbnail list
            if (m_Thumnails != null)
            {
                for (int x = 0; x < m_BrushList.Length; x++)
                {
                    Hash128 thumbnailHash = m_BrushList[x].thumbnail.imageContentsHash;
                    if (m_BrushHashes[x] != thumbnailHash)
                    {
                        m_BrushHashes[x] = thumbnailHash;
                        m_Thumnails = null;
                    }
                }
            }

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(Styles.brushes, EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            var b = m_BrushList != null ? GetActiveBrush() : null;
            if (b != null && !b.readOnly)
            {
                if (GUILayout.Button("Delete Brush...")
                    && EditorUtility.DisplayDialog(L10n.Tr("Delete Brush"), L10n.Tr("Deleting this brush will delete the brush asset from disk. You cannot undo this operation. Do you wish to continue?"), L10n.Tr("Yes"), L10n.Tr("No")))
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(b));
                    LoadBrushes();
                    UpdateSelection(m_BrushList != null && m_SelectedBrush < m_BrushList.Length ? m_SelectedBrush : 0);
                }
            }

            if (GUILayout.Button("New Brush..."))
                CreateBrush();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                Rect brushPreviewRect = EditorGUILayout.GetControlRect(true, GUILayout.Width(60), GUILayout.Height(60));
                if (m_BrushList != null)
                {
                    EditorGUI.DrawTextureAlpha(brushPreviewRect, GetActiveBrush().thumbnail);

                    bool dummy;
                    m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, GUILayout.Height(128));
                    var missingBrush = EditorGUIUtility.TrTextContent("No brushes defined.");
                    int newBrush = BrushSelectionGrid(m_SelectedBrush, m_BrushList, 32, Styles.gridList, missingBrush, out dummy);
                    if (newBrush != m_SelectedBrush)
                    {
                        UpdateSelection(newBrush);
                        repaint = true;
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.EndHorizontal();

            return repaint;
        }

        public void ShowEditGUI()
        {
            if (selectedIndex == -1)
                return;

            Brush b = m_BrushList == null ? null : GetActiveBrush();

            // if the brush has been deleted outside unity rebuild the brush list
            if (b == null)
            {
                m_BrushList = null;
                LoadBrushes();
                UpdateSelection(0);
                return;
            }

            if (b.readOnly)
            {
                EditorGUILayout.HelpBox("The brush is read-only.", MessageType.Info);
            }
            else if (m_BrushEditor)
            {
                Rect titleRect = Editor.DrawHeaderGUI(m_BrushEditor, b.name, 10f);
                int id = GUIUtility.GetControlID(78901, FocusType.Passive);

                Rect renderRect = EditorGUI.GetInspectorTitleBarObjectFoldoutRenderRect(titleRect);
                renderRect.y = titleRect.yMax - 17f; // align with bottom
                m_FoldoutContent[0] = b;
                bool newVisible = EditorGUI.DoObjectFoldout(m_ShowBrushSettings, titleRect, renderRect, m_FoldoutContent, id);

                // Toggle visibility
                if (newVisible != m_ShowBrushSettings)
                {
                    m_ShowBrushSettings = newVisible;
                    InternalEditorUtility.SetIsInspectorExpanded(b, newVisible);
                }
                if (m_ShowBrushSettings)
                    m_BrushEditor.OnInspectorGUI();
            }
        }

        int BrushSelectionGrid(int selected, Brush[] brushes, int approxSize, GUIStyle style, GUIContent emptyString, out bool doubleClick)
        {
            GUILayout.BeginVertical("box", GUILayout.MinHeight(approxSize));
            int retval = 0;

            doubleClick = false;

            if (brushes.Length != 0)
            {
                int columns = (int)(EditorGUIUtility.currentViewWidth - 150) / approxSize;
                if (columns <= 0)
                    columns = 1;
                int rows = (int)Mathf.Ceil((brushes.Length + columns - 1) / columns);
                Rect r = GUILayoutUtility.GetAspectRect((float)columns / (float)rows);
                Event evt = Event.current;
                if (evt.type == EventType.MouseDown && evt.clickCount == 2 && r.Contains(evt.mousePosition))
                {
                    doubleClick = true;
                    evt.Use();
                }

                if (m_Thumnails == null || m_Thumnails.Length != brushes.Length)
                {
                    m_Thumnails = GUIContentFromBrush(brushes);
                }
                retval = GUI.SelectionGrid(r, System.Math.Min(selected, brushes.Length - 1), m_Thumnails, (int)columns, style);
            }
            else
                GUILayout.Label(emptyString);

            GUILayout.EndVertical();
            return retval;
        }

        internal void CreateBrush()
        {
            ObjectSelector.get.Show(null, typeof(Texture2D), null, false, null,
                selection =>
                {
                    if (selection == null)
                        return;

                    var brushName = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(ProjectWindowUtil.GetActiveFolderPath(), "NewBrush.brush"));
                    var newBrush = Brush.CreateInstance((Texture2D)selection, AnimationCurve.Linear(0, 0, 1, 1), Brush.kMaxRadiusScale, false);
                    AssetDatabase.CreateAsset(newBrush, brushName);
                    LoadBrushes();
                    int newIndex = m_BrushList != null ? System.Array.IndexOf(m_BrushList, newBrush) : -1;
                    if (newIndex >= 0)
                        UpdateSelection(newIndex);
                }, null);
        }

        internal static GUIContent[] GUIContentFromBrush(Brush[] brushes)
        {
            GUIContent[] retval = new GUIContent[brushes.Length];

            for (int i = 0; i < brushes.Length; i++)
                retval[i] = new GUIContent(brushes[i].thumbnail, brushes[i].name);

            return retval;
        }
    }
} //namespace
