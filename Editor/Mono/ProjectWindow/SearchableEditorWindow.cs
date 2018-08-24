// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public class SearchableEditorWindow : EditorWindow
    {
        public enum SearchMode { All, Name, Type, Label, AssetBundleName };
        public enum SearchModeHierarchyWindow { All, Name, Type };

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
        int m_SearchFieldControlId;

        internal const float k_SearchTimerDelaySecs = 0.250f;
        private double m_NextSearch = double.MaxValue;


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
            searchableWindows.Add(this);
        }

        virtual public void OnDisable()
        {
            searchableWindows.Remove(this);
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

            // only main assets have unique paths (remove "Assets" to make string simpler)
            string path = AssetDatabase.GetAssetPath(instanceID).Substring(7);
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
                    if (sw != this && sw.m_HierarchyType == m_HierarchyType &&
                        sw.m_HierarchyType != HierarchyType.Assets)
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

        internal void SearchFieldGUI(float maxWidth)
        {
            Rect rect = GUILayoutUtility.GetRect(EditorGUILayout.kLabelFloatMaxW * 0.2f, maxWidth, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField);

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
            m_SearchFieldControlId = GUIUtility.GetControlID(s_SearchableEditorWindowSearchField, FocusType.Keyboard, rect);

            EditorGUI.BeginChangeCheck();
            string searchFilter = EditorGUI.ToolbarSearchField(m_SearchFieldControlId, rect, enumStrings, ref searchMode, m_SearchFilter);
            if (EditorGUI.EndChangeCheck())
                SetSearchFilter(searchFilter, (SearchMode)searchMode, true, true);

            m_HasSearchFilterFocus = GUIUtility.keyboardControl == m_SearchFieldControlId;

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape && m_SearchFilter != "" && GUIUtility.hotControl == 0)
            {
                m_SearchFilter = "";
                SetSearchFilter(searchFilter, (SearchMode)searchMode, true, true);
                Event.current.Use();
                m_HasSearchFilterFocus = false;
            }
        }
    }
}
