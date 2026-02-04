// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    [Serializable]
    internal sealed class ViewManager
    {
        class NullFilter : IIssueFilter
        {
            public bool Match(ReportItem issue)
            {
                return true;
            }
        }

        Report m_Report;
        AnalysisView[] m_Views;

        [SerializeField] SerializableEnum<IssueCategory>[] m_Categories;
        [SerializeField] int m_ActiveViewIndex;
        [SerializeField] HashSet<IssueCategory> m_PendingCategories;
        [SerializeField] HashSet<string> m_PendingModuleNames;

        public Report Report => m_Report;

        public int NumViews => m_Views != null ? m_Views.Length : 0;

        // user interactions
        public Action<int> OnActiveViewChanged { get; set; }
        public Action<bool> OnIgnoredIssuesVisibilityChanged { get; set; }

        // events that trigger future operations
        public Action<IssueCategory> OnAnalysisRequested { get; set; }
        public Action<ReportItem[]>  OnSelectedIssuesIgnoreRequested { get; set; }
        public Action<ReportItem[]>  OnSelectedIssuesDisplayRequested { get; set; }
        public Action<ReportItem[]>  OnSelectedIssuesQuickFixRequested { get; set; }
        public Action<ReportItem[]>  OnSelectedIssuesDocumentationRequested { get; set; }

        // events based on past operations
        public Action OnViewExportCompleted { get; set; }

        public ViewManager()
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            : this(ViewDescriptor.GetAll().Select(d => d.Category).ToArray())
#pragma warning restore UA2001
        {
        }

        public ViewManager(IssueCategory[] categories)
        {
            m_Categories = categories.ToSerializableArray();
            m_ActiveViewIndex = 0;
        }

        public bool IsValid()
        {
            return m_Views != null && m_Views.Length > 0;
        }

        public void AddIssues(IEnumerable<ReportItem> issues)
        {
            foreach (var view in m_Views)
                view.AddIssues(issues);
        }

        public void Clear()
        {
            foreach (var view in m_Views)
            {
                if (view != null)
                    view.Clear();
            }
        }

        public void Create(SeverityRules rules, ViewStates viewStates, Action<ViewDescriptor, bool> onCreateView = null, IIssueFilter filter = null)
        {
            if (filter == null)
                filter = new NullFilter();

            var views = new List<AnalysisView>();
            foreach (var category in m_Categories)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var desc = ViewDescriptor.GetAll().FirstOrDefault(d => d.Category == category);
#pragma warning restore UA2001
                if (desc == null)
                {
                    Debug.LogWarning($"[{ProjectAuditor.DisplayName}] Descriptor for " + ProjectAuditor.GetCategoryName(category) + " was not registered.");
                    continue;
                }
                var layout = IssueLayout.GetLayout(category);
                var isSupported = layout != null;

                if (onCreateView != null)
                    onCreateView(desc, isSupported);

                if (!isSupported)
                {
                    Debug.LogWarning($"[{ProjectAuditor.DisplayName}] Layout for category " + ProjectAuditor.GetCategoryName(category) + " was not found.");
                    continue;
                }

                var view = desc.Type != null ? (AnalysisView)Activator.CreateInstance(desc.Type, this) : new AnalysisView(this);
                view.Create(desc, layout, rules, viewStates, filter);
                view.OnEnable();
                views.Add(view);
            }

            m_Views = views.ToArray();
        }

        public void ClearView(IssueCategory category)
        {
            var view = GetView(category);
            if (view != null)
            {
                view.Clear();
            }
        }

        public AnalysisView GetActiveView()
        {
            return m_Views[m_ActiveViewIndex];
        }

        public AnalysisView GetView(int index)
        {
            return m_Views[index];
        }

        public bool HasView(IssueCategory category)
        {
            return GetView(category) != null;
        }

        public AnalysisView GetView(IssueCategory category)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return m_Views.FirstOrDefault(v => v.Desc.Category == category);
#pragma warning restore UA2001
        }

        public void ChangeView(IssueCategory category)
        {
            var activeView = GetActiveView();
            if (activeView.Desc.Category == category)
            {
                return;
            }

            var newView = GetView(category);
            if (newView == null)
                return; // assume the view was not registered

            ChangeView(Array.IndexOf(m_Views, newView));
        }

        void ChangeView(int index)
        {
            var changeViewRequired = (m_ActiveViewIndex != index);
            if (changeViewRequired)
            {
                m_ActiveViewIndex = index;

                if (OnActiveViewChanged != null)
                    OnActiveViewChanged(m_ActiveViewIndex);
            }
        }

        /// <summary>
        /// Mark all views as dirty. Use this to reload their tables.
        /// </summary>
        public void MarkViewsAsDirty()
        {
            foreach (var view in m_Views)
            {
                view.MarkDirty();
            }
        }

        /// <summary>
        /// Mark all views as dirty. Use this to reload their tables.
        /// </summary>
        public void MarkViewColumnWidthsAsDirty()
        {
            foreach (var view in m_Views)
            {
                view.MarkColumnWidthsDirty();
            }
        }

        public void OnAnalysisCompleted()
        {
            m_PendingCategories = null;
            m_PendingModuleNames = null;
            MarkViewColumnWidthsAsDirty();
        }

        public void OnAnalysisRestored(Report report)
        {
            AddIssues(report.GetAllIssues());
            m_PendingCategories = null;
            m_PendingModuleNames = null;
            m_Report = report;

            ClearSearch();
        }

        public void OnAnalysisStarted(Report report, string[] moduleNames, IssueCategory[] pendingCategories)
        {
            m_PendingCategories = new HashSet<IssueCategory>(pendingCategories);
            m_PendingModuleNames = new HashSet<string>(moduleNames);
            m_Report = report;
        }

        public void ClearSearch()
        {
            foreach (var view in m_Views)
            {
                view.SetSearch("");
            }
        }

        public void LoadSettings()
        {
            if (!IsValid())
                return;

            foreach (var view in m_Views)
            {
                view.LoadSettings();
            }
        }

        public HashSet<IssueCategory> PendingCategories
        {
            set => m_PendingCategories = value;
        }

        public HashSet<string> PendingModuleNames
        {
            get => m_PendingModuleNames;
        }

        public bool HasPendingCategories()
        {
            return m_PendingCategories != null;
        }

        public bool HasPendingCategory(IssueCategory category)
        {
            return m_PendingCategories != null && m_PendingCategories.Contains(category);
        }

        public void SaveSettings()
        {
            if (!IsValid())
                return;

            foreach (var view in m_Views)
            {
                view.SaveSettings();
            }
        }
    }
}
