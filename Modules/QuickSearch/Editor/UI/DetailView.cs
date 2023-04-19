// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    class DetailView : IDisposable
    {
        private readonly ISearchView m_SearchView;
        private Editor[] m_Editors;
        private int m_EditorsHash = 0;
        private Vector2 m_ScrollPosition;
        private double m_LastPreviewStamp = 0;
        private Texture2D m_PreviewTexture;
        private bool m_DisposedValue;

        public DetailView(ISearchView searchView)
        {
            m_SearchView = searchView;
        }

        ~DetailView()
        {
            Dispose(disposing: false);
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

                if (Event.current.type == EventType.Layout)
                    SetupEditors(selection, showOptions);

                GUILayout.Label(Styles.previewInspectorContent, Styles.panelHeader);

                if (selectionCount == 0)
                    return;

                if (selectionCount > 1)
                {
                    // Do not render anything else if the selection is composed of items with different providers
                    if (selection.GroupBy(item => item.provider.id).Count() > 1)
                    {
                        GUILayout.Label($"Selected {selectionCount} items from different types.", Styles.panelHeader);
                        return;
                    }
                    else
                    {
                        GUILayout.Label($"Selected {selectionCount} items", Styles.panelHeader);
                    }
                }

                using (var s = new EditorGUILayout.VerticalScope(Styles.inspector))
                {
                    if (showOptions.HasAny(ShowDetailsOptions.Actions) && !m_SearchView.IsPicker())
                        DrawActions(context);

                    if (selectionCount == 1)
                    {
                        if (showOptions.HasAny(ShowDetailsOptions.Preview) && lastItem != null)
                            DrawPreview(context, lastItem, width);

                        if (showOptions.HasAny(ShowDetailsOptions.Description) && lastItem != null)
                            DrawDescription(context, lastItem);
                    }

                    if (showOptions.HasAny(ShowDetailsOptions.Inspector))
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
            var remainingActions = actions.Where(a => !fixedActions.Contains(a.id)).ToArray();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.inspectorFullWidthMargins))
            {
                DrawActions(selection, actions.Where(a => fixedActions.Contains(a.id)));
                if (remainingActions.Length > 3)
                    DrawMoreMenu(selection, remainingActions);
            }
            if (remainingActions.Length <= 3)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.inspectorFullWidthMargins))
                    DrawActions(selection, remainingActions);
            }

            GUILayout.Space(2);
        }

        private void DrawActions(SearchSelection selection, IEnumerable<SearchAction> actions)
        {
            foreach (var action in actions)
            {
                if (action == null || action.content == null)
                    continue;

                if (selection.Count > 1 && action.execute == null)
                    continue;

                if (GUILayout.Button(action.content, GUILayout.Height(20f), GUILayout.ExpandWidth(true)))
                {
                    m_SearchView.ExecuteAction(action, selection.ToArray(), false);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void DrawMoreMenu(SearchSelection selection, IEnumerable<SearchAction> actions)
        {
            if (!actions.Any())
                return;
            if (GUILayout.Button(Styles.moreActionsContent, GUILayout.Height(20f), GUILayout.Width(24f)))
            {
                var menu = new GenericMenu();
                foreach (var action in actions)
                {
                    if (action == null || action.content == null)
                        continue;
                    if (selection.Count > 1 && action.execute == null)
                        continue;

                    var itemName = !string.IsNullOrWhiteSpace(action.content.text) ? action.content.text : action.content.tooltip;
                    menu.AddItem(new GUIContent(itemName, action.content.image), false, () => m_SearchView.ExecuteAction(action, selection.ToArray(), false));
                }

                menu.ShowAsContext();
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
            m_PreviewTexture = null;
        }

        private void DrawInspector(float width)
        {
            if (m_Editors == null)
                return;

            for (int i = 0; i < m_Editors.Length; ++i)
            {
                var e = m_Editors[i];
                if (!Utils.IsEditorValid(e))
                    continue;

                try
                {
                    var collapsed = IsEditorCollapsed(e);
                    var newCollapsed = !EditorGUILayout.InspectorTitlebar(!collapsed, e);
                    if (newCollapsed != collapsed)
                        SetEditorCollapsed(e, newCollapsed);
                    if (!newCollapsed)
                    {
                        var wideMode = width > Editor.k_WideModeMinWidth;
                        using (new EditorGUILayout.VerticalScope(wideMode ? Styles.inpsectorWideMargins : Styles.inpsectorMargins))
                        {
                            if (e.HasPreviewGUI())
                            {
                                var previewRect = EditorGUILayout.GetControlRect(false, 256,
                                    GUIStyle.none, GUILayout.MaxWidth(width), GUILayout.Height(256));
                                if (previewRect.width > 0 && previewRect.height > 0)
                                    e.OnInteractivePreviewGUI(previewRect, Styles.largePreview);
                            }

                            GUI.changed = false;
                            Utils.SetCurrentViewWidth(width);
                            EditorGUIUtility.wideMode = wideMode;
                            EditorGUIUtility.hierarchyMode = wideMode;
                            EditorGUIUtility.labelWidth = Mathf.Max((wideMode ? 0.4f : 0.2f) * width, 150f);
                            e.OnInspectorGUI();
                            Utils.SetCurrentViewWidth(-1f);
                        }
                    }
                }
                catch
                {
                    // Skip item
                }
            }
        }


        private void SetEditorCollapsed(Editor e, bool collapsed)
        {
            SessionState.SetBool($"collapsed_{e.target.GetType().Name}", collapsed);
        }

        private bool IsEditorCollapsed(Editor e)
        {
            return SessionState.GetBool($"collapsed_{e.target.GetType().Name}", false);
        }

        private void SetupEditors(SearchSelection selection, ShowDetailsOptions showOptions)
        {
            int selectionHash = 0;
            foreach (var s in selection)
                selectionHash ^= s.id.GetHashCode();

            if (selectionHash == m_EditorsHash)
                return;

            ResetEditors();

            if (!showOptions.HasAny(ShowDetailsOptions.Inspector))
                return;

            var targets = new List<UnityEngine.Object>();
            foreach (var s in selection)
            {
                var item = s;
                LoadEditor(item, targets);

                if (item.GetFieldCount() > 0)
                {
                    targets.Add(new SearchServiceItem()
                    {
                        name = item.label ?? item.value.ToString(),
                        item = item
                    });
                }
            }

            if (targets.Count > 0)
            {
                int maxGroupCount = targets.GroupBy(t => t.GetType()).Max(g => g.Count());
                m_Editors = targets.GroupBy(t => t.GetType()).Where(g => g.Count() == maxGroupCount).Select(g =>
                {
                    var editor = Editor.CreateEditor(g.ToArray());
                    Utils.SetFirstInspectedEditor(editor);
                    return editor;
                }).ToArray();
            }
            m_EditorsHash = selectionHash;
        }

        private bool LoadEditor(SearchItem item, List<UnityEngine.Object> targets)
        {
            item.options |= SearchItemOptions.FullDescription;
            var itemObject = item.ToObject();
            item.options &= ~SearchItemOptions.FullDescription;
            if (!itemObject)
                return false;

            if (itemObject is GameObject go)
            {
                var components = go.GetComponents<Component>();
                foreach (var c in components)
                {
                    if (!c || (c.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
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

            return true;
        }

        private static void DrawDescription(SearchContext context, SearchItem item)
        {
            item.options |= SearchItemOptions.FullDescription;
            var description = item.GetDescription(context, false);
            GUILayout.Label(description, Styles.previewDescription);
            item.options &= ~SearchItemOptions.FullDescription;
        }

        private void DrawPreview(SearchContext context, SearchItem item, float size)
        {
            if (item.provider.fetchPreview == null)
                return;

            if (SkipGeneratedPreview())
                return;

            var now = EditorApplication.timeSinceStartup;
            if (!m_PreviewTexture || (now - m_LastPreviewStamp > 2.5 && m_PreviewTexture?.GetInstanceID() < 0))
            {
                var maxHeight = Mathf.Min(256, size);
                var previewFlags = FetchPreviewOptions.Preview2D | FetchPreviewOptions.Large;
                var newPreview = item.provider.fetchPreview(item, context, new Vector2(size, maxHeight), previewFlags);
                if (newPreview && newPreview.height >= 64)
                {
                    m_PreviewTexture = newPreview;
                    m_LastPreviewStamp = now;
                }
            }

            if (m_PreviewTexture)
            {
                if (IsBuiltInIcon(m_PreviewTexture))
                    return;
                var previewHeight = Math.Min(m_PreviewTexture.height, 256);
                var textureRect = EditorGUILayout.GetControlRect(false, previewHeight,
                    Styles.largePreview, GUILayout.MaxWidth(size), GUILayout.Height(previewHeight));
                if (Event.current.type == EventType.Repaint)
                    GUI.Label(Styles.largePreview.margin.Remove(textureRect), m_PreviewTexture, Styles.largePreview);
            }
        }

        private static bool IsBuiltInIcon(Texture icon)
        {
            return Utils.IsBuiltInResource(icon);
        }

        private bool SkipGeneratedPreview()
        {
            if (m_Editors == null || m_Editors.Length == 0)
                return false;
            return m_Editors.Any(e => Utils.IsEditorValid(e) && e.HasPreviewGUI() &&
                (typeof(Texture).IsAssignableFrom(e.target.GetType()) ||
                    typeof(Material).IsAssignableFrom(e.target.GetType()) ||
                    typeof(AudioClip).IsAssignableFrom(e.target.GetType())));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_DisposedValue)
                return;

            if (disposing)
                m_PreviewTexture = null;

            ResetEditors();
            m_DisposedValue = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Refresh(RefreshFlags flags = RefreshFlags.Default)
        {
            if ((flags & RefreshFlags.StructureChanged) == RefreshFlags.StructureChanged)
                ResetEditors();
        }
    }
}
