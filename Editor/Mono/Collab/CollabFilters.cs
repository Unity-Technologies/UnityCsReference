// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Collaboration
{
    internal abstract class AbstractFilters
    {
        [SerializeField]
        private List<string[]> m_Filters;

        public List<string[]> filters { get { return m_Filters; } set { m_Filters = value; } }

        public abstract void InitializeFilters();

        public bool ContainsSearchFilter(string name, string searchString)
        {
            foreach (var filter in filters)
            {
                if (filter[0] == name && filter[1] == searchString)
                    return true;
            }
            return false;
        }

        public void ShowInFavoriteSearchFilters()
        {
            if (SavedSearchFilters.GetRootInstanceID() == 0)
            {
                SavedSearchFilters.AddInitializedListener(ShowInFavoriteSearchFilters);
                return;
            }

            SavedSearchFilters.RemoveInitializedListener(ShowInFavoriteSearchFilters);
            int prevInstanceID = 0;
            foreach (var filter in filters)
            {
                int instanceID = SavedSearchFilters.GetFilterInstanceID(filter[0], filter[1]);
                if (instanceID == 0)
                {
                    SearchFilter searchFilter = SearchFilter.CreateSearchFilterFromString(filter[1]);
                    if (prevInstanceID == 0)
                        prevInstanceID = SavedSearchFilters.AddSavedFilter(filter[0], searchFilter, 64);
                    else
                        prevInstanceID = SavedSearchFilters.AddSavedFilterAfterInstanceID(filter[0], searchFilter, 64, prevInstanceID, false);
                }
            }

            SavedSearchFilters.RefreshSavedFilters();

            foreach (ProjectBrowser pb in ProjectBrowser.GetAllProjectBrowsers())
            {
                pb.Repaint();
            }
        }

        public void HideFromFavoriteSearchFilters()
        {
            SavedSearchFilters.RefreshSavedFilters();

            foreach (ProjectBrowser pb in ProjectBrowser.GetAllProjectBrowsers())
            {
                pb.Repaint();
            }
        }
    }

    internal class CollabFilters : AbstractFilters
    {
        [SerializeField]
        private bool m_SearchFilterWasSet = false;

        public override void InitializeFilters()
        {
            filters = new List<string[]>() {
                new string[] { "All Modified", "v:any" },
                new string[] { "All Conflicts", "v:conflicted" },
                new string[] { "All Excluded" , "v:ignored"},
            };
        }

        public CollabFilters()
        {
            InitializeFilters();
        }

        public void ShowInProjectBrowser(string filterString)
        {
            ProjectBrowser browser = ProjectBrowser.s_LastInteractedProjectBrowser;
            if (browser == null)
            {
                List<ProjectBrowser> browsers = ProjectBrowser.GetAllProjectBrowsers();
                if (browsers != null && browsers.Count > 0)
                {
                    browser = browsers.First();
                }
            }

            if (!string.IsNullOrEmpty(filterString))
            {
                if (browser == null)
                {
                    browser = EditorWindow.GetWindow<ProjectBrowser>() as ProjectBrowser;
                    ShowInFavoriteSearchFilters();
                    browser.RepaintImmediately();
                }

                m_SearchFilterWasSet = true;

                string filterSearchString = "v:" + filterString;
                if (browser.IsTwoColumns())
                {
                    foreach (var filter in filters)
                    {
                        if (filterSearchString == filter[1])
                        {
                            int instanceID = SavedSearchFilters.GetFilterInstanceID(filter[0], filterSearchString);
                            if (instanceID > ProjectWindowUtil.k_FavoritesStartInstanceID)
                            {
                                browser.SetFolderSelection(new int[] { instanceID }, true);
                                break;
                            }
                        }
                    }
                }

                browser.SetSearch(filterSearchString);
                browser.Repaint();
                browser.Focus();
            }
            else
            {
                if (m_SearchFilterWasSet)
                {
                    if (browser != null)
                    {
                        if (browser.IsTwoColumns())
                        {
                            int instanceID = AssetDatabase.GetMainAssetInstanceID("assets");
                            browser.SetFolderSelection(new int[] { instanceID }, true);
                        }
                        browser.SetSearch("");
                        browser.Repaint();
                    }
                }
                m_SearchFilterWasSet = false;
            }
        }

        public void OnCollabStateChanged(CollabInfo info)
        {
            if (!info.ready || info.inProgress || info.maintenance)
                return;

            foreach (ProjectBrowser pb in ProjectBrowser.GetAllProjectBrowsers())
            {
                pb.RefreshSearchIfFilterContains("v:");
            }
        }
    }
}
