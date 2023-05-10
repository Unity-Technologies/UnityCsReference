// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System;

namespace UnityEditor
{
    public class SearchableEditorWindow : EditorWindow
    {
        public enum SearchMode { All, Name, Type, Label, AssetBundleName }
        public enum SearchModeHierarchyWindow { All, Name, Type }

        internal static SearchFilter CreateFilter(string searchString, SearchMode searchMode)
        {
            SearchFilter filter = new SearchFilter();
            if (string.IsNullOrEmpty(searchString))
                return filter;

            switch (searchMode)
            {
                case SearchMode.All:
                    if (!SearchUtility.ParseSearchString(searchString, filter))
                    {
                        // Use search string for all SearchModes: name, types and labels
                        filter.nameFilter = searchString;
                        filter.classNames = new[] { searchString };
                        filter.assetLabels = new[] { searchString };
                        filter.assetBundleNames = new[] { searchString };
                        filter.showAllHits = true;
                    }
                    break;
                case SearchMode.Name:
                    filter.nameFilter = searchString;
                    break;
                case SearchMode.Type:
                    filter.classNames = new[] { searchString };
                    break;
                case SearchMode.Label:
                    filter.assetLabels = new[] { searchString };
                    break;
                case SearchMode.AssetBundleName:
                    filter.assetBundleNames = new[] { searchString };
                    break;
            }

            return filter;
        }

        static List<SearchableEditorWindow> searchableWindows = new List<SearchableEditorWindow>();
        private static int s_SearchableEditorWindowSearchField = "SearchableEditorWindowSearchField".GetHashCode();

        internal HierarchyType m_HierarchyType = HierarchyType.Assets;
        internal string m_SearchFilter = "";
        internal SearchMode m_SearchMode = SearchMode.All;
        bool m_FocusSearchField = false;
        bool m_HasSearchFilterFocus = false;
        protected int m_SearchGroup;

        internal const float k_SearchTimerDelaySecs = 0.250f;
        private double m_NextSearch = double.MaxValue;
        internal bool m_SyncSearch;
        internal string m_OldSearch;
        string m_SearchStringDebounced;
        Action m_DeregisterDebounceCall;

        [MenuItem("CONTEXT/Component/Find References In Scene", secondaryPriority = 15)]
        private static void OnSearchForReferencesToComponent(MenuCommand command)
        {
            var component = command.context as Component;
            if (component)
                SearchForReferencesToInstanceID(component.GetInstanceID());
        }

        [MenuItem("CONTEXT/Component/Properties...", priority = 99999)]
        private static void OnOpenPropertiesToComponent(MenuCommand command)
        {
            var component = command.context as Component;
            if (!component)
                return;

            PropertyEditor.OpenPropertyEditor(component);
        }

        [MenuItem("Assets/Find References In Scene", false, 25)]
        private static void OnSearchForReferences()
        {
            SearchForReferencesToInstanceID(Selection.activeInstanceID);
        }

        [MenuItem("Assets/Find References In Scene", true)]
        private static bool OnSearchForReferencesValidate()
        {
            Object obj = Selection.activeObject;
            if (obj != null)
            {
                if (AssetDatabase.Contains(obj))
                {
                    string path = AssetDatabase.GetAssetPath(obj);
                    return !System.IO.Directory.Exists(path);
                }
            }

            return false;
        }

        virtual public void OnEnable()
        {
            SearchService.SearchService.syncSearchChanged += OnSyncSearchChanged;
            searchableWindows.Add(this);
        }

        virtual public void OnDisable()
        {
            SearchService.SearchService.syncSearchChanged -= OnSyncSearchChanged;
            searchableWindows.Remove(this);
            m_DeregisterDebounceCall = null;
        }

        private void OnSyncSearchChanged(SearchService.SearchService.SyncSearchEvent evt, string syncViewId, string searchQuery)
        {
            if (SearchService.SceneSearch.HasEngineOverride())
            {
                if (evt == SearchService.SearchService.SyncSearchEvent.StartSession)
                {
                    // When starting a synced session, back the current search to restore it
                    m_OldSearch = m_SearchFilter;
                }

                if (syncViewId == SearchService.SceneSearch.GetActiveSearchEngine().GetType().FullName)
                {
                    SetSearchFilter(searchQuery, (SearchMode)searchMode, true, true);
                    m_SyncSearch = true;
                }
                else if (m_SyncSearch)
                {
                    // When changing the source id, restore old search so user is not affected
                    m_SyncSearch = false;
                    if (string.IsNullOrEmpty(m_OldSearch))
                    {
                        ClearSearchFilter();
                    }
                    else
                    {
                        SetSearchFilter(m_OldSearch, (SearchMode)searchMode, true, true);
                    }
                }

                if (evt == SearchService.SearchService.SyncSearchEvent.EndSession)
                {
                    m_SyncSearch = false;
                }
            }
        }

        void OnInspectorUpdate()
        {
            // if it's time for a search we do it
            if (EditorApplication.timeSinceStartup > m_NextSearch)
            {
                m_NextSearch = double.MaxValue;
                Repaint();
                EditorApplication.Internal_CallSearchHasChanged();
            }
        }

        internal bool hasSearchFilter
        {
            get { return m_SearchFilter != ""; }
        }

        internal bool hasSearchFilterFocus
        {
            get { return m_HasSearchFilterFocus; }
            set { m_HasSearchFilterFocus = value; }
        }

        internal SearchMode searchMode
        {
            get { return m_SearchMode; }
            set { m_SearchMode = value; }
        }

        internal static void SearchForReferencesToInstanceID(int instanceID)
        {
            string searchFilter;

            // Don't remove "Assets" prefix, we need to support Packages as well (https://fogbugz.unity3d.com/f/cases/1161019/)
            string path = AssetDatabase.GetAssetPath(instanceID);
            if (path.IndexOf(' ') != -1)
                path = '"' + path + '"';

            if (AssetDatabase.IsMainAsset(instanceID))
                searchFilter = "ref:" + path;
            else
                searchFilter = "ref:" + instanceID + ":" + path;

            foreach (SearchableEditorWindow sw in searchableWindows)
            {
                if (sw.m_HierarchyType == HierarchyType.GameObjects)
                {
                    sw.SetSearchFilter(searchFilter, SearchMode.All, false, false);
                    sw.m_HasSearchFilterFocus = true;
                    sw.Repaint();
                }
            }
        }

        internal void UnfocusSearchField()
        {
            if (hasSearchFilterFocus)
                GUIUtility.keyboardControl = 0;
        }

        internal void FocusSearchField()
        {
            m_FocusSearchField = true;
        }

        internal void ClearSearchFilter()
        {
            SetSearchFilter("", m_SearchMode, true, false);
            // Reset current editor. This is needed, so if the user types into a search field, and then
            // a new object is selected, which is not in the filter, with the editor still having keyboard focus,
            // the search field gets properly cleared.

            if (EditorGUI.s_RecycledEditor != null)
                EditorGUI.s_RecycledEditor.EndEditing();
        }

        internal void SelectPreviousSearchResult()
        {
            foreach (SearchableEditorWindow sw in searchableWindows)
            {
                if (sw is SceneHierarchyWindow)
                {
                    ((SceneHierarchyWindow)sw).SelectPrevious();
                    return;
                }
            }
        }

        internal void SelectNextSearchResult()
        {
            foreach (SearchableEditorWindow sw in searchableWindows)
            {
                if (sw is SceneHierarchyWindow)
                {
                    ((SceneHierarchyWindow)sw).SelectNext();
                    return;
                }
            }
        }

        internal virtual void SetSearchFilter(string searchFilter, SearchMode mode, bool setAll, bool delayed)
        {
            m_SearchMode = mode;
            m_SearchFilter = searchFilter;

            if (setAll)
            {
                foreach (SearchableEditorWindow sw in searchableWindows)
                {
                    if (sw != this && sw.m_HierarchyType == m_HierarchyType && m_SearchGroup == sw.m_SearchGroup && sw.m_HierarchyType != HierarchyType.Assets)
                        sw.SetSearchFilter(m_SearchFilter, m_SearchMode, false, delayed);
                }
            }

            if (delayed)
            {
                m_NextSearch = EditorApplication.timeSinceStartup + k_SearchTimerDelaySecs;
            }
            else
            {
                Repaint();
                EditorApplication.Internal_CallSearchHasChanged();
            }
        }

        internal virtual void ClickedSearchField()
        {
        }

        internal void SearchFieldGUI()
        {
            SearchFieldGUI(EditorGUILayout.kLabelFloatMaxW * 1.5f);
        }

        void SetSearchFilterDebounced()
        {
            SetSearchFilter(m_SearchStringDebounced, searchMode, true, true);
            m_SearchStringDebounced = "";
        }

        internal void SearchFieldGUI(float maxWidth)
        {
            Rect rect = GUILayoutUtility.GetRect(EditorGUILayout.kLabelFloatMaxW * 0.2f, maxWidth, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchFieldWithJump);

            var drawSearchPopupButton = ModeService.HasCapability(ModeCapability.SearchPopupButton, true);
            if (!drawSearchPopupButton)
            {
                // Add a 2px margin if the search popup button is not drawn; we don't want a margin between the search
                // field and the button if the button is present.
                rect.xMax -= 2f;
            }

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                ClickedSearchField();

            GUI.SetNextControlName("SearchFilter");
            if (m_FocusSearchField)
            {
                EditorGUI.FocusTextInControl("SearchFilter");
                if (Event.current.type == EventType.Repaint)
                    m_FocusSearchField = false;
            }

            int searchMode = (int)m_SearchMode;

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape && GUI.GetNameOfFocusedControl() == "SearchFilter")
                SetSearchFilter("", (SearchMode)searchMode, true, true);

            string[] enumStrings = System.Enum.GetNames(m_HierarchyType == HierarchyType.GameObjects ? typeof(SearchModeHierarchyWindow) : typeof(SearchMode));
            int searchFieldControlId = GUIUtility.GetControlID(s_SearchableEditorWindowSearchField, FocusType.Keyboard, rect);

            EditorGUI.BeginChangeCheck();
            string searchFilter =
                EditorGUI.ToolbarSearchField(
                    searchFieldControlId,
                    rect,
                    enumStrings,
                    ref searchMode,
                    m_SearchFilter,
                    m_SyncSearch ? EditorStyles.toolbarSearchFieldWithJumpPopupSynced : EditorStyles.toolbarSearchFieldWithJumpPopup,
                    m_SyncSearch ? EditorStyles.toolbarSearchFieldWithJumpSynced : EditorStyles.toolbarSearchFieldWithJump,
                    string.IsNullOrEmpty(m_SearchFilter) ? EditorStyles.toolbarSearchFieldCancelButtonWithJumpEmpty : EditorStyles.toolbarSearchFieldCancelButtonWithJump);
            if (EditorGUI.EndChangeCheck())
            {
                m_SearchMode = (SearchMode)searchMode;
                m_SearchStringDebounced = searchFilter;
                m_DeregisterDebounceCall?.Invoke();
                m_DeregisterDebounceCall = EditorApplication.CallDelayed(SetSearchFilterDebounced, SearchUtils.debounceThresholdMs / 1000f);
            }

            m_HasSearchFilterFocus = GUIUtility.keyboardControl == searchFieldControlId;

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape && m_SearchFilter != "" && GUIUtility.hotControl == 0)
            {
                m_SearchFilter = "";
                SetSearchFilter("", (SearchMode)searchMode, true, true);
                Event.current.Use();
                m_HasSearchFilterFocus = false;
            }

            if (m_HasSearchFilterFocus)
            {
                SearchService.SearchService.HandleSearchEvent(this, Event.current, m_SearchFilter);
            }

            if (drawSearchPopupButton)
            {
                SearchService.SearchService.DrawOpenSearchButton(this, m_SearchFilter);
            }
        }
    }
}
