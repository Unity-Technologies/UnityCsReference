// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace UnityEditor.Collaboration
{
    internal class CollabFilters
    {
        [SerializeField]
        private bool m_SearchFilterWasSet = false;

        internal static List<string[]> s_Filters;

        public CollabFilters()
        {
            s_Filters = new List<string[]>() {
                new string[] { "All Modified", "v:any" },
                new string[] { "All Conflicts", "v:conflicted" },
                new string[] { "All Excluded" , "v:ignored"}
            };
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
                    browser.RepaintImmediately();
                }

                m_SearchFilterWasSet = true;

                string filterSearchString = "v:" + filterString;
                if (browser.IsTwoColumns())
                {
                    foreach (var filter in s_Filters)
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
                browser.RepaintImmediately();
                browser.Focus();
            }
            else
            {
                if (m_SearchFilterWasSet)
                {
                    int instanceID = AssetDatabase.GetMainAssetInstanceID("assets");
                    if (browser != null)
                    {
                        browser.SetFolderSelection(new int[] { instanceID }, true);
                        browser.SetSearch("");
                        browser.RepaintImmediately();
                    }
                }
                m_SearchFilterWasSet = false;
            }
        }

        public void AddFavoriteSearchFilters()
        {
            if (ProjectBrowser.s_LastInteractedProjectBrowser == null)
            {
                return;
            }

            int prevInstanceID = 0;
            foreach (var filter in s_Filters)
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
        }

        public void RemoveFavoriteSearchFilters()
        {
            if (ProjectBrowser.s_LastInteractedProjectBrowser == null)
            {
                return;
            }

            foreach (var filter in s_Filters)
            {
                int instanceID = SavedSearchFilters.GetFilterInstanceID(filter[0], filter[1]);
                if (instanceID > ProjectWindowUtil.k_FavoritesStartInstanceID)
                {
                    SavedSearchFilters.RemoveSavedFilter(instanceID);
                }
            }
        }
    }
}
