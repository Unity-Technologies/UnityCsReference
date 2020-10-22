// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Search
{
    class DetailView
    {
        private readonly ISearchView m_SearchView;
        private string m_LastPreviewItemId;
        private Editor[] m_Editors;
        private int m_EditorsHash = 0;
        private Vector2 m_ScrollPosition;
        private double m_LastPreviewStamp = 0;
        private Texture2D m_PreviewTexture;

        public DetailView(ISearchView searchView)
        {
            m_SearchView = searchView;
        }

        public bool HasDetails(SearchContext context)
        {
            var selection = context.searchView.selection;
            var selectionCount = selection.Count;
            if (selectionCount == 0)
                return false;

            var showDetails = true;
            string sameType = null;
            foreach (var s in selection)
            {
                if (!s.provider.showDetails)
                {
                    showDetails = false;
                    break;
                }

                if (sameType == null)
                    sameType = s.provider.id;
                else if (sameType != s.provider.id)
                {
                    showDetails = false;
                    break;
                }
            }

            if (!showDetails)
                return false;

            return true;
        }

        public void Draw(SearchContext context, float width)
        {
            var selection = context.searchView.selection;

            using (var scrollView = new EditorGUILayout.ScrollViewScope(m_ScrollPosition, Styles.panelBackgroundRight, GUILayout.Width(width), GUILayout.ExpandHeight(true)))
            {
                var selectionCount = selection.Count;

                var lastItem = selection.Last();
                var showOptions = lastItem?.provider.showDetailsOptions ?? ShowDetailsOptions.None;

                GUILayout.Label("Preview Inspector", Styles.panelHeader);

                if (selectionCount == 0)
                    return;

                if (selectionCount > 1)
                    GUILayout.Label($"Selected {selectionCount} items", Styles.panelHeader);

                var padding = Styles.inspector.margin.horizontal;
                using (var s = new EditorGUILayout.VerticalScope(Styles.inspector))
                {
                    if (showOptions.HasFlag(ShowDetailsOptions.Inspector) && Event.current.type == EventType.Layout)
                        SetupEditors(selection);

                    if (showOptions.HasFlag(ShowDetailsOptions.Actions))
                        DrawActions(context);

                    if (selectionCount == 1)
                    {
                        if (showOptions.HasFlag(ShowDetailsOptions.Preview) && lastItem != null)
                            DrawPreview(context, lastItem, width - padding);

                        if (showOptions.HasFlag(ShowDetailsOptions.Description) && lastItem != null)
                            DrawDescription(context, lastItem);
                    }

                    if (showOptions.HasFlag(ShowDetailsOptions.Inspector))
                        DrawInspector(width);
                }

                m_ScrollPosition = scrollView.scrollPosition;
            }
        }

        private void DrawActions(SearchContext context)
        {
            var selection = context.searchView.selection;
            var firstItem = selection.First();
            if (firstItem == null)
                return;

            var fixedActions = new string[] { "select", "open" };
            var actions = firstItem.provider.actions.Where(a => a.enabled(selection));
            using (new EditorGUILayout.HorizontalScope())
                DrawActions(selection, actions.Where(a => fixedActions.Contains(a.id)));
            DrawActions(selection, actions.Where(a => !fixedActions.Contains(a.id)));

            GUILayout.Space(8);
        }

        private void DrawActions(SearchSelection selection, IEnumerable<SearchAction> actions)
        {
            foreach (var action in actions)
            {
                if (action == null || action.content == null)
                    continue;

                if (selection.Count > 1 && action.execute == null)
                    continue;

                if (GUILayout.Button(action.content, GUILayout.ExpandWidth(true)))
                {
                    m_SearchView.ExecuteAction(action, selection.ToArray(), false);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void ResetEditors()
        {
            if (m_Editors != null)
            {
                foreach (var e in m_Editors)
                    UnityEngine.Object.DestroyImmediate(e);
            }
            m_Editors = null;
            m_EditorsHash = 0;
        }

        private void DrawInspector(float width)
        {
            if (m_Editors == null)
                return;

            using (new InspectorWindowUtils.LayoutGroupChecker())
            {
                for (int i = 0; i < m_Editors.Length; ++i)
                {
                    var e = m_Editors[i];
                    if (!e)
                        continue;

                    GUI.changed = false;
                    EditorGUIUtility.wideMode = true;
                    EditorGUIUtility.hierarchyMode = false;
                    EditorGUIUtility.labelWidth = Mathf.Min(150f, (width > 175f ? 0.35f : 0.2f) * width);

                    try
                    {
                        using (new EditorGUIUtility.IconSizeScope(new Vector2(16, 16)))
                        using (new EditorGUILayout.VerticalScope(EditorStyles.inspectorFullWidthMargins))
                            e.OnInspectorGUI();
                    }
                    catch (Exception ex)
                    {
                        EditorGUILayout.HelpBox(new GUIContent($"Failed to display inspector for {e.GetType().Name}", ex.Message));
                    }

                    GUILayout.Space(2);
                }
            }
        }

        private void SetupEditors(SearchSelection selection)
        {
            int selectionHash = 0;
            foreach (var s in selection)
                selectionHash ^= s.id.GetHashCode();

            if (selectionHash == m_EditorsHash)
                return;

            ResetEditors();

            var targets = new List<UnityEngine.Object>();
            foreach (var s in selection)
            {
                var item = s;
                var itemObject = item.provider.toObject?.Invoke(item, typeof(UnityEngine.Object));

                if (!itemObject)
                    continue;

                if (itemObject is GameObject go)
                {
                    var components = go.GetComponents<Component>();
                    foreach (var c in components)
                    {
                        if (!c || c.hideFlags.HasFlag(HideFlags.HideInInspector))
                            continue;

                        targets.Add(c);
                    }
                }
                else
                {
                    targets.Add(itemObject);

                    if (item.provider.id == "asset")
                    {
                        var importer = AssetImporter.GetAtPath(item.id);
                        if (importer && importer.GetType() != typeof(AssetImporter))
                            targets.Add(importer);
                    }
                }
            }

            m_Editors = targets.GroupBy(t => t.GetType()).Select(g =>
            {
                var editor = Editor.CreateEditor(g.ToArray());
                editor.firstInspectedEditor = true;
                return editor;
            }).ToArray();
            m_EditorsHash = selectionHash;
        }

        private static void DrawDescription(SearchContext context, SearchItem item)
        {
            var description = SearchContent.FormatDescription(item, context, 2048);
            GUILayout.Label(description, Styles.previewDescription);
        }

        private void DrawPreview(SearchContext context, SearchItem item, float size)
        {
            if (m_Editors != null && m_Editors.Length > 0)
            {
                var previewEditor = m_Editors.FirstOrDefault(e => e.HasPreviewGUI());
                if (previewEditor != null)
                {
                    var previewRect = EditorGUILayout.GetControlRect(false, 256,
                        GUIStyle.none, GUILayout.MaxWidth(size), GUILayout.Height(256));
                    if (previewRect.width > 0 && previewRect.height > 0)
                        previewEditor.OnPreviewGUI(previewRect, Styles.largePreview);
                    return;
                }
            }
            if (item.provider.fetchPreview == null)
                return;

            var now = EditorApplication.timeSinceStartup;
            if (now - m_LastPreviewStamp > 2.5)
                m_PreviewTexture = null;

            var textureRect = EditorGUILayout.GetControlRect(false, 256,
                GUIStyle.none, GUILayout.MaxWidth(size), GUILayout.Height(256));
            if (Event.current.type == EventType.Repaint)
            {
                if (!m_PreviewTexture || m_LastPreviewItemId != item.id)
                {
                    m_LastPreviewStamp = now;
                    if (textureRect.width > 0 && textureRect.height > 0)
                    {
                        m_PreviewTexture = item.provider.fetchPreview(item, context,
                            new Vector2(textureRect.width, textureRect.height), FetchPreviewOptions.Preview2D | FetchPreviewOptions.Large);
                    }
                    m_LastPreviewItemId = item.id;
                }

                if (m_PreviewTexture == null)
                    m_SearchView.Repaint();

                if (m_PreviewTexture)
                {
                    GUI.Label(textureRect, m_PreviewTexture, Styles.largePreview);
                }
            }

            GUILayout.Space(4);
        }
    }
}
