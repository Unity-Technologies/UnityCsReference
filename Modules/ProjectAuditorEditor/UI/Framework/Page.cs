// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    // Stable identifier for a navigation page. Used to map project-area flags to pages and back.
    enum PageId
    {
        None,               // Leaf/category pages that don't need a stable lookup id.
        Home,
        Optimization,
        Code,
        Assets,
        Shaders,
        GameObjects,
        ProjectSettings,
        Build,
        Upgrade,
        MigrationToURP,
        MigrationToCoreCLR,
    }

    // A node in the view-selection navigation tree. A page is either:
    //  - the Home page (IsHome): shows the preferences / Start Analysis panel, or
    //  - a view page: selecting it activates the view registered for Category.
    // The hierarchy is expressed directly by nesting child pages.
    // The same category can appear under more than one page; IssueFilter lets each such page show a
    // different subset of that category's issues (the active view is re-filtered on selection).
    class Page
    {
        public PageId id;
        public string name;
        public bool isHome;
        public SerializableEnum<IssueCategory> category;
        public Page[] children = Array.Empty<Page>();

        // Optional extra filter applied to the view's issues while this page is selected. Null = no
        // extra filtering.
        public Func<ReportItem, bool> issueFilter;

        // Optional custom filter UI drawn in the Filters panel while this page is selected, letting a
        // page add its own filter controls on top of the active view's. Null = no extra UI.
        public Action<ViewStates> drawFilters;

        // All categories represented by this page: its own (if not Home) plus those of its
        // descendants, own first. Computed lazily and cached on first access. Page instances are
        // built once and live for the window's lifetime, so this avoids per-call list/iterator
        // allocations in hot paths (e.g. TreeView row rendering).
        IssueCategory[] m_AllCategories;

        public IReadOnlyList<IssueCategory> AllCategories => m_AllCategories ??= BuildAllCategories();

        IssueCategory[] BuildAllCategories()
        {
            var categories = new List<IssueCategory>();
            Collect(this, categories);
            return categories.ToArray();

            static void Collect(Page page, List<IssueCategory> categories)
            {
                if (!page.isHome)
                    categories.Add(page.category);

                if (page.children != null)
                {
                    foreach (var child in page.children)
                        Collect(child, categories);
                }
            }
        }
    }
}
