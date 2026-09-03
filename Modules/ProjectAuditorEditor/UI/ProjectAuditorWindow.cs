// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Profiling not yet converted
//#define PA_DRAW_LOGO

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal partial class ProjectAuditorWindow : EditorWindow, IHasCustomMenu, IIssueFilter
    {
        enum AnalysisState
        {
            Initializing,
            Initialized,
            InProgress,
            Completed,
            Valid
        }

        static readonly string[] s_AreaNames = Array.ConvertAll(AreasExtensions.AlphabeticalAreas, (a) => a.ToString());

        static string[] NicifiedAreaNames
        {
            get
            {
                if (s_NicifiedAreaNames == null)
                    s_NicifiedAreaNames = Array.ConvertAll(AreasExtensions.AlphabeticalAreas, (a) => a.ToFrontendString());
                return s_NicifiedAreaNames;
            }
        }

        [NoAutoStaticsCleanup]
        static string[] s_NicifiedAreaNames;

        [AutoStaticsCleanupOnCodeReload]
        static ProjectAuditorWindow s_Instance;

        public static ProjectAuditorWindow Instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = ShowWindow();
                return s_Instance;
            }
        }

        ProjectAuditor m_ProjectAuditor;
        IProgress m_Progress;
        bool m_ShouldRefresh;
        AnalyticsReporter.Analytic m_AnalyzeButtonAnalytic;
        AnalyticsReporter.Analytic m_LoadButtonAnalytic;

        // UI
        TreeViewSelection m_AreaSelection;
        TreeViewSelection m_AssemblySelection;

        Draw2D m_Draw2D;

        internal Draw2D Draw2D => m_Draw2D;

        Areas m_SelectedAreas;

        // Serialized fields
        [SerializeField] string m_AreaSelectionSummary;
        [SerializeField] string[] m_AssemblyNames;
        [SerializeField] bool[] m_AssemblyReadOnlyFlags;
        [SerializeField] string m_AssemblySelectionSummary;
        [SerializeField] internal Report m_Report;
        [SerializeField] AnalysisState m_AnalysisState = AnalysisState.Initializing;
        [SerializeField] ViewStates m_ViewStates = new ViewStates();
        [SerializeField] ViewManager m_ViewManager;

        static readonly string k_ReportAutoSaveFilename = "projectauditor-report-autosave.projectauditor";

        // The navigation tree shown in the view selection tree view.
        // Rebuilt from code in OnEnable, so it doesn't need to be serialized.
        private Page[] m_Pages = GetDefaultPages();

        AnalysisView activeView => m_ViewManager.GetActiveView();

        [SerializeField] TreeViewState m_ViewSelectionTreeState;
        ViewSelectionTreeView m_ViewSelectionTreeView;

        [SerializeField] bool m_IsNonAnalyzedViewSelected;
        [SerializeField] bool m_IsPendingAnalysisViewSelected;
        [SerializeField] PageId m_SelectedNonAnalyzedPageId;

        // True when the Home page (preferences / Start Analysis) is shown; otherwise the normal
        // analysis panels are shown. Defaults to Home on open.
        [SerializeField] bool m_ShowHomePage = true;

        // The page currently shown, and the extra issue filter it applies to the active view. Both are
        // derived from the page tree (built in code), so they're not serialized.
        Page m_CurrentPage;
        Func<ReportItem, bool> m_CurrentPageIssueFilter;

        Vector2 m_PreviousWindowSize;

        [AutoStaticsCleanupOnCodeReload]
        static AddRequest RulesPackageInstallRequest;

        static void RulesPackageInstallProgressCallback()
        {
            var wnd = GetWindow(typeof(ProjectAuditorWindow)) as ProjectAuditorWindow;
            if (wnd != null)
                wnd.Repaint();

            if (RulesPackageInstallRequest.IsCompleted)
            {
                if (RulesPackageInstallRequest.Status == StatusCode.Success)
                {
                    Debug.Log("Installed: " + RulesPackageInstallRequest.Result.packageId);
                    Events.registeredPackages += OnRulesPackageRegistered;
                }
                else if (RulesPackageInstallRequest.Status >= StatusCode.Failure)
                {
                    Debug.Log(RulesPackageInstallRequest.Error.message);
                }

                EditorApplication.update -= RulesPackageInstallProgressCallback;
                RulesPackageInstallRequest = null;
            }
        }

        static void OnRulesPackageRegistered(PackageRegistrationEventArgs args)
        {
#pragma warning disable UAC2001
            foreach (var p in args.added.Concat(args.changedTo))
#pragma warning restore UAC2001
            {
                if (p.name == ProjectAuditorRulesPackage.Name)
                {
                    Events.registeredPackages -= OnRulesPackageRegistered;
                    ProjectAuditorRulesPackage.Initialize();
                    Instance?.m_ProjectAuditor?.InitModules();
                    return;
                }
            }
        }

        private static Page[] GetDefaultPages()
        {
            // A category-backed leaf page. Its display name matches the registered view's DisplayName.
            Page Leaf(string name, IssueCategory category) =>
                new Page { name = name, category = category };

            Page[] pages =
            [
                new Page
                {
                    id = PageId.Home,
                    name = "Home",
                    isHome = true
                },
                new Page
                {
                    id = PageId.Optimization,
                    name = "Optimization",
                    category = IssueCategory.OptimizationSummary,
                    children =
                    [
                        new Page
                        {
                            id = PageId.Code,
                            name = "Code",
                            category = IssueCategory.Code,
                            children =
                            [
                                Leaf("Assemblies", IssueCategory.Assembly),
                                Leaf("Precompiled Assemblies", IssueCategory.PrecompiledAssembly),
                                Leaf("Compiler Messages", IssueCategory.CodeCompilerMessage),
                                Leaf("Domain Reload", IssueCategory.DomainReload),
                                Leaf("Obsolete API", IssueCategory.ObsoleteAPI),
                            ]
                        },
                        new Page
                        {
                            id = PageId.Assets,
                            name = "Assets",
                            category = IssueCategory.AssetIssue,
                            children =
                            [
                                Leaf("Textures", IssueCategory.Texture),
                                Leaf("Sprite Atlases", IssueCategory.SpriteAtlas),
                                Leaf("Meshes", IssueCategory.Mesh),
                                Leaf("Audio Clips", IssueCategory.AudioClip),
                                Leaf("Animator Controllers", IssueCategory.AnimatorController),
                                Leaf("Animation Clips", IssueCategory.AnimationClip),
                                Leaf("Avatars", IssueCategory.Avatar),
                                Leaf("Avatar Masks", IssueCategory.AvatarMask),
                            ]
                        },
                        new Page
                        {
                            id = PageId.Shaders,
                            name = "Shaders",
                            category = IssueCategory.Shader,
                            children =
                            [
                                Leaf("Shader Variants", IssueCategory.ShaderVariant),
                                /*Leaf("Compute Shader Variants", IssueCategory.ComputeShaderVariant),*/
                                Leaf("Compiler Messages", IssueCategory.ShaderCompilerMessage),
                                Leaf("Materials", IssueCategory.Material),
                            ]
                        },
                        new Page
                        {
                            id = PageId.GameObjects,
                            name = "Game Objects",
                            category = IssueCategory.GameObject,
                            children =
                            [
                                Leaf("Mesh Colliders", IssueCategory.MeshCollider),
                            ]
                        },
                        new Page
                        {
                            id = PageId.ProjectSettings,
                            name = "Project Settings",
                            category = IssueCategory.ProjectSetting,
                            children = [Leaf("Packages", IssueCategory.Package)]
                        },
                        new Page
                        {
                            id = PageId.Build,
                            name = "Build",
                            category = IssueCategory.BuildFile,
                            children =
                            [
                                Leaf("Build Steps", IssueCategory.BuildStep),
                            ]
                        },
                    ]
                },
                new Page
                {
                    id = PageId.Upgrade,
                    name = "Upgrade",
                    category = IssueCategory.UpgradeSummary,
                    children =
                    [
                        new Page
                        {
                            id = PageId.Code,
                            name = "Code",
                            category = IssueCategory.Code
                        },
                        new Page
                        {
                            id = PageId.Assets,
                            name = "Assets",
                            category = IssueCategory.AssetIssue
                        },
                        new Page
                        {
                            id = PageId.GameObjects,
                            name = "Game Objects",
                            category = IssueCategory.GameObject
                        },
                        new Page
                        {
                            id = PageId.ProjectSettings,
                            name = "Project Settings",
                            category = IssueCategory.ProjectSetting
                        },
                    ]
                },
                new Page
                {
                    id = PageId.MigrationToURP,
                    name = "Migrate to URP",
                    category = IssueCategory.MigrateToURPSummary,
                    children =
                    [
                        new Page
                        {
                            id = PageId.Code,
                            name = "Code",
                            category = IssueCategory.Code
                        },
                        new Page
                        {
                            id = PageId.Assets,
                            name = "Assets",
                            category = IssueCategory.AssetIssue
                        },
                        new Page
                        {
                            id = PageId.GameObjects,
                            name = "Game Objects",
                            category = IssueCategory.GameObject
                        },
                        new Page
                        {
                            id = PageId.ProjectSettings,
                            name = "Project Settings",
                            category = IssueCategory.ProjectSetting
                        },
                    ]
                },
                new Page
                {
                    id = PageId.MigrationToCoreCLR,
                    name = "Migrate to CoreCLR",
                    category = IssueCategory.MigrateToCoreCLRSummary,
                    children =
                    [
                        new Page
                        {
                            id = PageId.Code,
                            name = "Code",
                            category = IssueCategory.Code
                        }
                    ]
                },
            ];

            // Apply page filters.
            ApplyGroupFilter(pages, PageId.Optimization, issue => !HasAnyAreas(issue, Areas.Upgrade));
            ApplyGroupFilter(pages, PageId.Upgrade, issue => HasAnyAreas(issue, Areas.Upgrade));
            ApplyGroupFilter(pages, PageId.MigrationToURP, issue => HasAnyAreas(issue, Areas.MigrationToURP));
            ApplyGroupFilter(pages, PageId.MigrationToCoreCLR, issue => HasAnyAreas(issue, Areas.MigrationToCoreCLR));

            // Upgrade pages additionally offer a target-version selector in the Filters panel.
            ApplyGroupDrawFilters(pages, PageId.Upgrade, DiagnosticView.DrawUpgradeTargetVersionFilter);

            return pages;
        }

        // Sets issueFilter on the page with the given id and all of its descendants.
        static void ApplyGroupFilter(Page[] pages, PageId groupId, Func<ReportItem, bool> filter)
        {
            var group = FindPage(pages, p => p.id == groupId);
            if (group != null)
                ForEachInSubtree(group, page => page.issueFilter = filter);
        }

        // Sets drawFilters on the page with the given id and all of its descendants.
        static void ApplyGroupDrawFilters(Page[] pages, PageId groupId, Action<ViewStates> drawFilters)
        {
            var group = FindPage(pages, p => p.id == groupId);
            if (group != null)
                ForEachInSubtree(group, page => page.drawFilters = drawFilters);
        }

        static void ForEachInSubtree(Page page, Action<Page> action)
        {
            action(page);

            if (page.children != null)
            {
                foreach (var child in page.children)
                    ForEachInSubtree(child, action);
            }
        }

        // True if the issue is flagged with the specified areas.
        static bool HasAnyAreas(ReportItem issue, Areas areas)
        {
            return issue.Id.IsValid() && (issue.Id.GetDescriptor().Areas & areas) != 0;
        }

        public bool Match(ReportItem issue)
        {
            // return false if the issue does not match one of these criteria:
            // - the current page's filter (e.g. the Optimization/Upgrade area split)
            // - assembly name, if applicable
            // - area
            // - is not muted, if enabled
            // - critical context, if enabled/applicable

            // The selected page can restrict which of its category's issues are shown, letting the same
            // view appear under more than one page with a different subset each.
            if (m_CurrentPageIssueFilter != null && !m_CurrentPageIssueFilter(issue))
                return false;

            var viewDesc = activeView.Desc;
            var matchAssembly = !viewDesc.ShowAssemblySelection ||
                m_AssemblySelection != null &&
                (m_AssemblySelection.Contains(viewDesc.GetAssemblyName(issue)) ||
                    m_AssemblySelection.ContainsGroup("All"));
            if (!matchAssembly)
                return false;

            var isDiagnostic = issue.IsIssue();
            if (!isDiagnostic)
                return true;

            // TODO: the rest of this logic is common to all diagnostic views. It should be moved to the AnalysisView
            if (activeView.IsDiagnostic()) // Only checking matching areas on views that support Area filtering
            {
                var matchArea = issue.Id.IsValid() && issue.Id.GetDescriptor().MatchesAnyAreas(m_SelectedAreas);
                if (!matchArea)
                    return false;
            }

            if (activeView.OnlyCriticalIssues() && !issue.IsMajorOrCritical())
                return false;
            if (activeView.OnlyPerfCriticalIssues() && !issue.IsPerformanceCritical())
                return false;
            if (activeView.OnlyFixableIssues() && issue.Id.GetDescriptor().Fixer == null)
                return false;

            return true;
        }

        bool m_tryingFallback = false;

        void OnEnable()
        {
            ProjectAuditorSettings.instance.DiagnosticParams.RegisterParameters();
            ProjectAuditorSettings.instance.Save();

            if (m_ProjectAuditor == null)
                m_ProjectAuditor = new ProjectAuditor();

            // Throw away old version, if restored from serialized window state (these code paths skip the version checking we get during Report.Load)
            // e.g. after a domain reload, or from a previous editor session via the window layout.
            if (m_Report != null && m_Report.ReportVersion != Report.k_CurrentVersion)
                m_Report = null;

            if (m_Report != null && !m_Report.IsValid())
            {
                IssueCategory[] categories = (IssueCategory[])Enum.GetValues(typeof(IssueCategory));
                #pragma warning disable UAC2001 // Avoid Linq
                var requestedModules = categories.SelectMany(m_ProjectAuditor.GetModules).Distinct().ToArray();
#pragma warning restore UAC2001
                m_Report.PostSerializeLayoutUpdate(requestedModules);
            }

            var currentState = m_AnalysisState;
            m_AnalysisState = AnalysisState.Initializing;
            m_Pages = GetDefaultPages();

            AnalyticsReporter.EnableAnalytics();

            UpdateAreaSelection();
            UpdateAssemblySelection();

            InitializeViews(ProjectAuditorSettings.instance.Rules, true);

            // are we reloading from a valid state?
            if (currentState == AnalysisState.Valid &&
                m_Report != null &&
                m_Report.IsValid())
            {
                m_ViewManager.OnAnalysisRestored(m_Report);
                m_AnalysisState = currentState;
            }
            else
            {
                if (m_tryingFallback == false)
                {
                    m_tryingFallback = true;
                    m_AnalysisState = AnalysisState.Initialized;

                    TryLoadAutosavedReport();
                }

                m_tryingFallback = false;
            }

            m_Draw2D = new Draw2D(ProjectAuditor.s_DataPath + "/Shaders/ProjectAuditor.shader");

            RefreshWindow();

            wantsMouseMove = true;
        }

        void InitializeViews(SeverityRules rules, bool reload)
        {
            var initialize = m_ViewManager == null || !reload;

            if (initialize)
            {
                // Every category that has a page in the navigation tree needs a view. Collected in
                // page-tree (display) order; the ViewManager keeps that order, so the first category
                // (the Optimization summary) is the default active view. A category can appear under
                // more than one page (e.g. Code under both Optimization and Upgrade), so deduplicate
                // to avoid creating redundant view instances for the same category.
                var supportedCategories = new List<IssueCategory>();
                var seenCategories = new HashSet<IssueCategory>();
                foreach (var page in m_Pages)
                {
                    foreach (var category in page.AllCategories)
                    {
                        if (seenCategories.Add(category))
                            supportedCategories.Add(category);
                    }
                }

                m_ViewManager = new ViewManager(supportedCategories);
            }

            m_ViewManager.OnActiveViewChanged += i =>
            {
                var viewDesc = m_ViewManager.GetView(i).Desc;
                AnalyticsReporter.SendEvent(
                    (AnalyticsReporter.UIButton)viewDesc.AnalyticsEventId,
                    AnalyticsReporter.BeginAnalytic());

                // Selecting any real analysis view means we are showing the normal panels, not the
                // Home page.
                m_ShowHomePage = false;
                m_IsNonAnalyzedViewSelected = false;
                m_IsPendingAnalysisViewSelected = false;

                m_ViewSelectionTreeView?.SelectItemByCategory(viewDesc.Category);
                m_ViewManager.GetView(i)?.MarkDirty();

                Repaint();
            };

            m_ViewManager.OnIgnoredIssuesVisibilityChanged += showIgnoredIssues =>
            {
                var analytic = AnalyticsReporter.BeginAnalytic();
                var payload = new Dictionary<string, string>
                {
                    ["selected"] = showIgnoredIssues ? "true" : "false"
                };
                AnalyticsReporter.SendEventWithKeyValues(
                    AnalyticsReporter.UIButton.ShowMuted,
                    analytic, payload);
            };

            m_ViewManager.OnSelectedIssuesIgnoreRequested = issues =>
            {
                var analytic = AnalyticsReporter.BeginAnalytic();

                AnalyticsReporter.SendEventWithSelectionSummary(AnalyticsReporter.UIButton.Mute,
                    analytic, issues);

                MarkSummaryViewsDirty();
                m_Report.NeedsSaving = true;
            };

            m_ViewManager.OnSelectedIssuesDisplayRequested = issues =>
            {
                var analytic = AnalyticsReporter.BeginAnalytic();

                AnalyticsReporter.SendEventWithSelectionSummary(
                    AnalyticsReporter.UIButton.Unmute, analytic, issues);

                MarkSummaryViewsDirty();
                m_Report.NeedsSaving = true;
            };

            m_ViewManager.OnSelectedIssuesQuickFixRequested = issues =>
            {
                MarkSummaryViewsDirty();
            };

            m_ViewManager.OnAnalysisRequested += category =>
            {
                AuditCategories(ProjectAreaFlags.None, [category]);
                var page = FindPageForCategory(category);
                if (page != null)
                    OnSelectedNonAnalyzedPage(page);
                GUIUtility.ExitGUI();
            };

            m_ViewManager.OnViewExportCompleted += () =>
            {
                AnalyticsReporter.SendEvent(AnalyticsReporter.UIButton.Export,
                    AnalyticsReporter.BeginAnalytic());
            };

            m_ViewManager.Create(rules, m_ViewStates, null, this);

            InitializeViewSelection(!initialize);
        }

        // Finds the page with the given stable id anywhere in the navigation tree.
        Page FindPage(PageId id)
        {
            return FindPage(m_Pages, p => p.id == id);
        }

        // Finds the "tab" page (a direct child of a top-level page) whose subtree contains the category.
        Page FindPageForCategory(IssueCategory category)
        {
            foreach (var topLevel in m_Pages)
            {
                if (topLevel.children == null)
                    continue;
                foreach (var tab in topLevel.children)
                {
                    foreach (var cat in tab.AllCategories)
                    {
                        if (cat == category)
                            return tab;
                    }
                }
            }

            return null;
        }

        static Page FindPage(Page[] pages, Func<Page, bool> predicate)
        {
            foreach (var page in pages)
            {
                if (predicate(page))
                    return page;

                if (page.children != null)
                {
                    var found = FindPage(page.children, predicate);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        // The category a page activates when selected: its own, or its first descendant's.
        static IssueCategory GetPrimaryCategory(Page page)
        {
            return page.AllCategories[0];
        }

        // Marks every summary page's view dirty so its stats refresh (e.g. after muting/fixing issues).
        void MarkSummaryViewsDirty()
        {
            for (int i = 0; i < m_ViewManager.NumViews; i++)
            {
                if (m_ViewManager.GetView(i) is SummaryView summaryView)
                    summaryView.MarkDirty();
            }
        }

        // Invoked whenever a module finishes during analysis. Updates the set of pending
        // modules/categories and refreshes the summary views. Crucially, if the user is currently
        // looking at a page whose data has just arrived (its module finished mid-analysis), it
        // re-evaluates that page so the populated view replaces the "analysis running" prompt
        // immediately, instead of only refreshing once the whole analysis completes. (UUM-144826)
        void HandleModuleCompleted(string moduleName)
        {
            m_ViewManager.PendingModuleNames.Remove(moduleName);

#pragma warning disable UAC2001 // Avoid Linq
            var remainingModules = m_ProjectAuditor.GetModules().Where(m => m_ViewManager.PendingModuleNames.Contains(m.Name));
            var remainingCategories = remainingModules.SelectMany(m => m.Categories).ToHashSet();
#pragma warning restore UAC2001
            m_ViewManager.PendingCategories = remainingCategories;

            MarkSummaryViewsDirty();

            // If a page is currently showing the "analysis running" prompt, re-check whether its
            // data has now arrived so it can switch to the populated view without waiting for the
            // entire analysis to finish.
            if (m_IsNonAnalyzedViewSelected)
            {
                var selectedPage = FindPage(m_SelectedNonAnalyzedPageId);
                if (selectedPage != null)
                {
                    OnSelectedNonAnalyzedPage(selectedPage);
                    Repaint();
                }
            }
        }

        // Navigate to the page for a category, preferring one in the same top-level group as the
        // currently shown page (so e.g. the Upgrade summary's "Go to Code" lands on Upgrade > Code).
        internal void GotoCategory(IssueCategory category)
        {
            var page = FindCategoryPageInCurrentGroup(category)
                ?? FindPage(m_Pages, p => !p.isHome && p.category == category);
            if (page == null)
                return;

            ShowPage(page);

            EvaluateAnalyzePrompt(page);

            m_ViewSelectionTreeView?.SelectPage(page);
        }

        Page FindCategoryPageInCurrentGroup(IssueCategory category)
        {
            var top = GetTopLevelPage(m_CurrentPage);
            if (top == null)
                return null;

            return FindPage(new[] { top }, p => !p.isHome && p.category == category);
        }

        // The top-level page (Home / Optimization / Upgrade) whose subtree contains the given page.
        Page GetTopLevelPage(Page page)
        {
            if (page == null)
                return null;

            foreach (var top in m_Pages)
            {
                if (FindPage(new[] { top }, p => ReferenceEquals(p, page)) != null)
                    return top;
            }

            return null;
        }

        // Shows the analyze prompt for a page when the category being viewed has no results yet.
        void OnSelectedNonAnalyzedPage(Page selectedPage)
        {
            
            var activeCategory = m_ViewManager.GetActiveView().Desc.Category;
            bool activePending = m_ViewManager.HasPendingCategory(activeCategory);

            bool activeHasData = (m_ViewManager.Report?.HasCategory(activeCategory) ?? false)
                || (activeCategory.IsPopulatedByPlayerBuild() && !activePending);

            if (activeHasData)
            {
                m_IsNonAnalyzedViewSelected = false;
                m_IsPendingAnalysisViewSelected = false;
                
            }
            else
            {
                m_IsNonAnalyzedViewSelected = true;
                m_IsPendingAnalysisViewSelected = activePending;
                m_SelectedNonAnalyzedPageId = selectedPage.id;
            }
        }

        // Called when the user selects a page node in the view selection tree.
        void OnSelectedPage(Page page)
        {
            if (page.isHome)
            {
                m_ShowHomePage = true;
                m_CurrentPage = page;
                return;
            }

            ShowPage(page);

            EvaluateAnalyzePrompt(page);
        }

        // Only "tab" pages map to a project area, so a leaf page (e.g. Shaders > Materials) drives
        // its prompt from the tab that contains it - that area is what the Analyze button re-runs.
        void EvaluateAnalyzePrompt(Page page)
        {
            var promptPage = GetPageProjectArea(page.id) != ProjectAreaFlags.None
                ? page
                : FindPageForCategory(page.category);
            if (promptPage != null)
                OnSelectedNonAnalyzedPage(promptPage);
        }

        // Activates a page's view, applying that page's issue filter. The view instance is shared
        // between pages of the same category, so it is marked dirty to re-filter for this page even
        // when the category didn't change.
        void ShowPage(Page page)
        {
            m_ShowHomePage = false;
            m_CurrentPage = page;
            m_CurrentPageIssueFilter = page.issueFilter;

            m_ViewManager.ChangeView(page.category);
            m_ViewManager.GetView(page.category)?.MarkDirty();
        }

        void OnDisable()
        {
            CancelAnalysis();
            AutosaveReport();

            // Make sure 'dirty' scriptable objects are saved to their corresponding assets
            AssetDatabase.SaveAssets();

            m_ViewManager?.OnDisable();
        }

        // Called when the EditorWindow is closed
        void OnDestroy()
        {
            CancelAnalysis();
        }

        void CancelAnalysis()
        {
            if (m_AnalysisState == AnalysisState.InProgress)
                m_Progress.Cancel();
        }

            void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                DrawToolbar();

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawViewSelection();
                    if (m_ShowHomePage || !IsAnalysisValid())
                    {
                        DrawHome();
                    }
                    else
                    {
                        if (!m_IsNonAnalyzedViewSelected)
                        {
                            using (new EditorGUILayout.VerticalScope())
                            {
                                DrawPanels();

                                // Summary pages (Optimization, Upgrade) have no table, so skip the
                                // selection/status bar.
                                if (!(m_ViewManager.GetActiveView() is SummaryView))
                                {
                                    DrawStatusBar();
                                }
                            }
                        }
                        else
                        {
                            DrawAnalysisPanel(m_IsPendingAnalysisViewSelected);
                        }
                    }
                }
            }
        }

        // Draw the panel that appears when you select a page that has not yet been analyzed.
        void DrawAnalysisPanel(bool analysisPending)
        {
            var selectedPage = FindPage(m_SelectedNonAnalyzedPageId);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandHeight(true)))
            {
                var tabName = selectedPage.name;

                if (analysisPending)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    var boxWidth = Mathf.Min(650.0f, EditorGUIUtility.currentViewWidth - LayoutSize.kTreeViewWidth - 20.0f);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(boxWidth));

                    EditorGUILayout.BeginHorizontal();
                    var info = string.Format(Contents.PendingAnalyzeInfoText, tabName);
                    GUILayout.Label(EditorGUIUtility.GetHelpIcon(MessageType.Info), GUILayout.ExpandWidth(false));
                    GUILayout.Space(5);
                    GUILayout.Label(info, EditorStyles.wordWrappedLabel, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(5);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(Contents.OpenBackgroundTasks))
                        Progress.ShowDetails(false);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();

                    var info = string.Format(Contents.AnalyzeInfoText, tabName);

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.HelpBox(info, MessageType.Info);
                    GUILayout.FlexibleSpace();

                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(10);

                    EditorGUILayout.BeginHorizontal();

                    GUILayout.FlexibleSpace();
                    GUI.enabled = !m_ViewManager.HasPendingCategories();

                    if (GUILayout.Button(string.Format(Contents.AnalyzeButtonText, tabName), GUILayout.Width(200)))
                    {
                        bool validPreferences = true;
                        if (selectedPage.id == PageId.Code)
                            validPreferences = ValidateCodeAnalysisWithPopup();

                        if (validPreferences)
                        {
                            var area = GetPageProjectArea(selectedPage.id);
                            AuditCategories(area, selectedPage.AllCategories);
                            OnSelectedNonAnalyzedPage(selectedPage);
                        }
                    }

                    GUI.enabled = true;
                    GUILayout.FlexibleSpace();

                    EditorGUILayout.EndHorizontal();

                    if (selectedPage.id == PageId.Code)
                    {
                        const int k_SpacingHeight = 12;

                        EditorGUILayout.BeginHorizontal();

                        GUILayout.FlexibleSpace();
                        using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(350)))
                        {
                            GUILayout.Space(k_SpacingHeight);
                            UserPreferences.CodeAnalysisGUI();
                            GUILayout.Space(k_SpacingHeight);
                        }
                        GUILayout.FlexibleSpace();

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
        }

        void InitializeViewSelection(bool reload)
        {
            if (!reload)
                m_ViewSelectionTreeState = null;

            m_ViewSelectionTreeView = null;
            m_IsNonAnalyzedViewSelected = false;
            m_IsPendingAnalysisViewSelected = false;
        }

        // Selects the tree node that matches the current page state, so the highlight stays
        // consistent with the panel being shown (e.g. after the tree is rebuilt).
        void SyncTreeSelection()
        {
            if (m_ViewSelectionTreeView == null)
                return;

            if (m_ShowHomePage)
            {
                m_ViewSelectionTreeView.SelectPage(FindPage(PageId.Home));
                return;
            }

            // Restore the exact page that was selected and re-show it, so the active view's per-page issue filter is reapplied.
            if (m_CurrentPage != null)
            {
                m_ViewSelectionTreeView.SelectPage(m_CurrentPage);
                return;
            }

            // After a domain reload m_CurrentPage is lost (not serialized) but the selected tree node
            // id survives in m_ViewSelectionTreeState. Restore the exact page that was selected and
            // re-show it, so the active view's per-page issue filter is reapplied.
            var restored = ResolveSelectedPage();
            if (restored != null)
            {
                OnSelectedPage(restored);
                m_ViewSelectionTreeView.SelectPage(restored);
                return;
            }

            // Fallback: sync to the tree node mapped to the active view's category.
            m_ViewSelectionTreeView.SelectItemByCategory(m_ViewManager.GetActiveView().Desc.Category);
        }

        // Resolves the page referenced by the serialized tree selection, or null if none.
        Page ResolveSelectedPage()
        {
            var selectedIds = m_ViewSelectionTreeState?.selectedIDs;
            if (selectedIds == null)
                return null;

            foreach (var id in selectedIds)
            {
                if (m_ViewSelectionTreeView.TryGetPage(id, out var page))
                    return page;
            }

            return null;
        }

        void DrawViewSelection()
        {
            using (new EditorGUI.DisabledScope(m_AnalysisState == AnalysisState.Initializing))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    if (m_ViewSelectionTreeState == null)
                    {
                        m_ViewSelectionTreeState = new TreeViewState();
                    }

                    if (m_ViewSelectionTreeView == null)
                    {
                        m_ViewSelectionTreeView = new ViewSelectionTreeView(m_ViewSelectionTreeState, m_Pages, m_ViewManager);
                        m_ViewSelectionTreeView.OnSelectedPage += OnSelectedPage;

                        // Keep the tree highlight in sync with the current page/view after a rebuild.
                        SyncTreeSelection();
                    }

                    var rect = EditorGUILayout.GetControlRect(GUILayout.Width(LayoutSize.kTreeViewWidth), GUILayout.ExpandHeight(true));
                    m_ViewSelectionTreeView.OnGUI(rect);
                }
            }
        }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            // UUM-139591: Force ProjectAuditorSettings to load now, during InitializeOnLoad (which runs
            // before the editor restores its window layout). Otherwise the ScriptableSingleton's backing
            // asset is loaded for the first time from inside ProjectAuditorWindow.OnEnable() while the
            // layout is still being deserialized. That nested LoadSerializedFileAndForget call results in
            // a FallbackEditorWindow that later fails to save.
            _ = ProjectAuditorSettings.instance;

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.OptimizationSummary,
                DisplayName = "Optimization",
                Type = typeof(OptimizationSummaryView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Summary
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.UpgradeSummary,
                DisplayName = "Upgrade",
                Type = typeof(UpgradeSummaryView),
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.MigrateToURPSummary,
                DisplayName = "Migrate to URP",
                Type = typeof(MigrateToURPSummaryView),
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.MigrateToCoreCLRSummary,
                DisplayName = "Migrate to CoreCLR",
                Type = typeof(MigrateToCoreCLRSummaryView),
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.AssetIssue,
                DisplayName = "Asset Issues",
                DescriptionWithIcon = true,
                ShowDependencyView = true,
                ShowFilters = true,
                ShowInfoPanel = true,
                ShowDetails = true,
                DependencyViewGuiContent = new GUIContent("Asset Dependencies"),
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Assets,
                Type = typeof(DiagnosticView),
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Shader,
                DisplayName = "Shaders",
                DescriptionWithIcon = true,
                ShowDependencyView = true,
                ShowFilters = true,
                OnContextMenu = (menu, viewManager, issue) =>
                {
                    menu.AddItem(Contents.ShaderVariants, false, () =>
                    {
                        viewManager.ChangeView(IssueCategory.ShaderVariant);
                        viewManager.GetActiveView().SetSearch(issue.Description);
                    });
                },
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Shaders
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Material,
                DisplayName = "Materials",
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Materials
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.ShaderCompilerMessage,
                DisplayName = "Compiler Messages",
                DescriptionWithIcon = true,
                ShowDetails = true,
                OnOpenIssue = EditorInterop.OpenTextFile<Shader>,
                Type = typeof(ShaderCompilerMessagesView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.ShaderCompilerMessages
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.ShaderVariant,
                DisplayName = "Shader Variants",
                ShowFilters = true,
                ShowInfoPanel = true,
                ShowDetails = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                OnDrawToolbar = (viewManager) =>
                {
                    GUILayout.FlexibleSpace();

                    using (new EditorGUI.DisabledScope(Instance.IsAnalysisInProgress()))
                    {
                        AnalysisView.DrawToolbarButton(Contents.Refresh, () => Instance.AnalyzeShaderVariants());
                        AnalysisView.DrawToolbarButton(Contents.Clear, () => Instance.ClearShaderVariants());
                    }

                    GUILayout.FlexibleSpace();
                },
                Type = typeof(ShaderVariantsView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.ShaderVariants
            });

            /*ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.ComputeShaderVariant,
                DisplayName = "Compute Shader Variants",
                ShowFilters = true,
                ShowInfoPanel = true,
                ShowDetails = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                OnDrawToolbar = (viewManager) =>
                {
                    GUILayout.FlexibleSpace();

                    AnalysisView.DrawToolbarButton(Contents.Refresh, () => Instance.AnalyzeShaderVariants());
                    AnalysisView.DrawToolbarButton(Contents.Clear, () => Instance.ClearShaderVariants());

                    GUILayout.FlexibleSpace();
                },
                Type = typeof(ShaderVariantsView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.ComputeShaderVariants
            });*/

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Package,
                DisplayName = "Packages",
                OnOpenIssue = EditorInterop.OpenPackage,
                ShowDependencyView = true,
                DependencyViewGuiContent = new GUIContent("Package Dependencies"),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Packages
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.AudioClip,
                DisplayName = "Audio Clips",
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.AudioClip
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Mesh,
                DisplayName = "Meshes",
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Meshes
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Texture,
                DisplayName = "Textures",
                DescriptionWithIcon = true,
                ShowDependencyView = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Textures
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.SpriteAtlas,
                DisplayName = "Sprite Atlases",
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.SpriteAtlases
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.AnimatorController,
                DisplayName = "Animator Controllers",
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.AnimatorControllers
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.AnimationClip,
                DisplayName = "Animation Clips",
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.AnimationClips
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Avatar,
                DisplayName = "Avatars",
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Avatars
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.AvatarMask,
                DisplayName = "Avatar Masks",
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.AvatarMasks
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.PrecompiledAssembly,
                DisplayName = "Precompiled Assemblies",
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.PrecompiledAssemblies
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Assembly,
                DisplayName = "Assemblies",
                ShowFilters = true,
                ShowDependencyView = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Assemblies
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Code,
                DisplayName = "Code Issues",
                ShowAssemblySelection = true,
                ShowDependencyView = true,
                ShowFilters = true,
                ShowInfoPanel = true,
                ShowDetails = true,
                ShowQuickFixes = false, // Remove this if we add support for quick-fixing code issues
                ShowPerformanceCritical = true,
                DependencyViewGuiContent = new GUIContent("Inverted Call Hierarchy", "Expand the tree to see all of the methods which lead to the call site of a selected issue."),
                GetAssemblyName = issue => issue.GetCustomProperty(CodeProperty.Assembly),
                OnOpenIssue = EditorInterop.OpenTextFile<TextAsset>,
                OnOpenManual = EditorInterop.OpenCodeDescriptor,
                Type = typeof(CodeDiagnosticView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.ApiCalls
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.CodeCompilerMessage,
                DisplayName = "Compiler Messages",
                ShowFilters = true,
                ShowInfoPanel = true,
                ShowDetails = true,
                OnOpenIssue = EditorInterop.OpenTextFile<TextAsset>,
                OnOpenManual = EditorInterop.OpenCompilerMessageDescriptor,
                Type = typeof(CompilerMessagesView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.CodeCompilerMessages
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.ProjectSetting,
                DisplayName = "Project Settings Issues",
                ShowFilters = true,
                ShowInfoPanel = true,
                ShowDetails = true,
                OnOpenIssue = (location) =>
                {
                    if (location.Path.StartsWith("Packages/"))
                    {
                        EditorInterop.OpenPackage(location);
                        return;
                    }

                    var guid = AssetDatabase.AssetPathToGUID(location.Path);
                    if (string.IsNullOrEmpty(guid))
                    {
                        EditorInterop.OpenProjectSettings(location);
                        return;
                    }

                    EditorInterop.FocusOnAssetInProjectWindow(location);
                },
                Type = typeof(DiagnosticView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.ProjectSettings
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.BuildStep,
                DisplayName = "Build Steps",
                ShowFilters = true,
                ShowInfoPanel = true,
                ShowDetails = true,
                Type = typeof(BuildStepsView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.BuildSteps
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.BuildFile,
                DisplayName = "Build Size",
                DescriptionWithIcon = true,
                ShowFilters = true,
                ShowInfoPanel = true,
                ShowDetails = true,
                ShowAdditionalInfoPanel = BuildSizeView.ShowAdditionalInfo,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                Type = typeof(BuildSizeView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.BuildFiles
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.DomainReload,
                DisplayName = "Domain Reload",
                ShowAssemblySelection = true,
                ShowFilters = true,
                ShowInfoPanel = true,
                ShowDetails = true,
                GetAssemblyName = issue => issue.GetCustomProperty(CompilerMessageProperty.Assembly),
                OnOpenIssue = EditorInterop.OpenTextFile<TextAsset>,
                OnOpenManual = EditorInterop.OpenCodeDescriptor,
                Type = typeof(CodeDomainReloadView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.DomainReload
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.ObsoleteAPI,
                DisplayName = "Obsolete API Database",
                ShowFilters = true,
                ShowInfoPanel = true,
                ShowDetails = true,
                Type = typeof(ObsoleteApiView)
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.GameObject,
                DisplayName = "Game Object Issues",
                ShowFilters = true,
                ShowInfoPanel = true,
                ShowDetails = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInHierarchyWindow,
                //AnalyticsEventId = (int)AnalyticsReporter.UIButton.ApiCalls,
                Type = typeof(DiagnosticView)
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.MeshCollider,
                DisplayName = "Mesh Colliders",
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                //AnalyticsEventId = (int)AnalyticsReporter.UIButton.MeshColliders
            });
        }

        bool IsAnalysisValid()
        {
            return m_AnalysisState != AnalysisState.Initializing && m_AnalysisState != AnalysisState.Initialized;
        }

        bool IsAnalysisInProgress()
        {
            return m_AnalysisState == AnalysisState.InProgress;
        }

        void Analyze()
        {
            m_AnalyzeButtonAnalytic = AnalyticsReporter.BeginAnalytic();

            m_ShouldRefresh = true;
            m_AnalysisState = AnalysisState.InProgress;
            m_Report = null;

            var reportDisplayName = Application.productName + "_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

            if (m_ProjectAuditor == null)
                m_ProjectAuditor = new ProjectAuditor();

            var analysisParams = new AnalysisParams
            {
                Categories = GetSelectedCategories().ToSerializableArray(),
                Platform = GetSelectedAnalysisPlatform(),
                CodeAnalysisFlags = GetSelectedCompilationFlags(),
                CodeOwnerFlags = GetSelectedCodeOwnerFlags(),

                OnIncomingIssues = issues =>
                {
                    // add batch of issues
                    m_ViewManager.AddIssues(issues);
                },
                OnStarted = (report, moduleNames, categories) =>
                {
                    m_ViewManager.OnAnalysisStarted(report, moduleNames, categories);
                    m_ViewManager.ClearSearch();
                },
                OnModuleCompleted = (moduleName, analysisResult, extraAnalysisTimeMs) =>
                {
                    HandleModuleCompleted(moduleName);
                },
                OnCompleted = report =>
                {
                    if (!report.IsValid())
                    {
                        m_AnalysisState = AnalysisState.Initialized;
                        return;
                    }
                    m_ViewManager.OnAnalysisCompleted();

                    m_ShouldRefresh = true;
                    m_AnalysisState = AnalysisState.Completed;
                    m_Progress = null;

                    m_Report = report;
                    m_Report.DisplayName = reportDisplayName;
                    m_Report.NeedsSaving = true;

                    EditorApplication.delayCall += AutosaveReport;

                    InitializeViewSelection(true);
                }
            };

            InitializeViews(analysisParams.Rules, false);

            // Leave the Home page and show the Summary (Optimization) while analysis runs.
            ShowPage(FindPage(PageId.Optimization));

            m_Progress = new ProgressBar();
            m_ProjectAuditor.AuditAsync(analysisParams, m_Progress);
        }

        void Update()
        {
            if (m_ShouldRefresh)
                Repaint();
            if (m_AnalysisState == AnalysisState.InProgress)
                Repaint();
        }

        internal void AuditCategories(ProjectAreaFlags areas, IReadOnlyList<IssueCategory> categories)
        {
            if (m_ProjectAuditor == null)
                m_ProjectAuditor = new ProjectAuditor();

            // a module might report more categories than requested so we need to make sure we clean up the views accordingly
            #pragma warning disable UAC2001 // Avoid Linq
            var modules = categories.SelectMany(m_ProjectAuditor.GetModules).ToArray();
            var actualCategories = modules.SelectMany(m => m.Categories).Distinct().ToArray();

            var views = actualCategories
                .Select(c => m_ViewManager.GetView(c))
                .Where(v => v != null)
                .ToArray();
#pragma warning restore UAC2001

            foreach (var view in views)
            {
                view.Clear();
            }

            m_AnalysisState = AnalysisState.InProgress;

            var analysisParams = new AnalysisParams
            {
                Categories = actualCategories.ToSerializableArray(),
                Platform = m_Report.SessionInfo.Platform,
                CodeAnalysisFlags = GetSelectedCompilationFlags(),
                CodeOwnerFlags = GetSelectedCodeOwnerFlags(),
                ExistingReport = m_Report,
                ExistingReportProjectAreas = areas,
                OnIncomingIssues = issues =>
                {
                    foreach (var view in views)
                    {
                        view.AddIssues(issues);
                    }
                },
                OnStarted = (report, moduleNames, categories) =>
                {
                    m_ViewManager.OnAnalysisStarted(report, moduleNames, categories);
                },
                OnModuleCompleted = (moduleName, analysisResult, extraAnalysisTimeMs) =>
                {
                    HandleModuleCompleted(moduleName);
                },
                OnCompleted = report =>
                {
                    if (!report.IsValid())
                    {
                        m_AnalysisState = AnalysisState.Initialized;
                        return;
                    }
                    m_ViewManager.OnAnalysisCompleted();

                    m_ShouldRefresh = true;
                    m_AnalysisState = AnalysisState.Completed;
                    m_Progress = null;

                    m_Report.NeedsSaving = true;

                    EditorApplication.delayCall += AutosaveReport;

                    InitializeViewSelection(true);
                }
            };

            m_Progress = new ProgressBar();
            m_ProjectAuditor.AuditAsync(analysisParams, m_Progress);
        }

        public void AnalyzeShaderVariants()
        {
            var shadersPage = FindPage(PageId.Shaders);
            AuditCategories(GetPageProjectArea(PageId.Shaders), shadersPage.AllCategories);
            OnSelectedNonAnalyzedPage(shadersPage);
            GUIUtility.ExitGUI();
        }

        public void ClearShaderVariants()
        {
            m_Report.ClearIssues(IssueCategory.ShaderVariant);

            m_ViewManager.ClearView(IssueCategory.ShaderVariant);

            ShadersModule.ClearBuildData();
        }

        void RefreshWindow()
        {
            if (!IsAnalysisValid())
                return;

            m_ViewManager.MarkViewsAsDirty();

            if (m_AnalysisState == AnalysisState.Completed)
            {
                UpdateAssemblyNames();
                UpdateAssemblySelection();

                m_AnalysisState = AnalysisState.Valid;

                if (m_LoadButtonAnalytic != null)
                    AnalyticsReporter.SendEvent(AnalyticsReporter.UIButton.Load, m_LoadButtonAnalytic);
                if (m_AnalyzeButtonAnalytic != null)
                    AnalyticsReporter.SendEventWithAnalyzeSummary(AnalyticsReporter.UIButton.Analyze, m_AnalyzeButtonAnalytic, m_Report);

                // repaint once more to make status wheel disappear
                Repaint();
            }
        }

        string GetSelectedAssembliesSummary()
        {
            if (m_AssemblyNames != null && m_AssemblyNames.Length > 0)
                return Utility.GetTreeViewSelectedSummary(m_AssemblySelection, m_AssemblyNames);
            return string.Empty;
        }

        internal string GetSelectedAreasSummary()
        {
            if (m_SelectedAreas == AreasExtensions.All)
                return "All";
            return m_SelectedAreas.ToString();
        }

        BuildTarget GetSelectedAnalysisPlatform()
        {
            BuildTarget platform = UserPreferences.AnalysisTargetPlatform;

            // if platform is not selected or supported, fallback to active build target
            if (platform == BuildTarget.NoTarget ||
                !BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(platform), platform))
                platform = EditorUserBuildSettings.activeBuildTarget;

            return platform;
        }

        CodeAnalysisFlags GetSelectedCompilationFlags()
        {
            return UserPreferences.CodeAnalysisFlags;
        }

        CodeOwnerFlags GetSelectedCodeOwnerFlags()
        {
            if (Unsupported.IsDeveloperMode())
                return UserPreferences.CodeOwnerFlags;
            return CodeOwnerFlags.User;
        }

        IssueCategory[] GetSelectedCategories()
        {
            var selectedCategories = UserPreferences.ProjectAreasToAnalyze;
            var requestedCategories = new List<IssueCategory>();
            ProjectAreaFlags categories = selectedCategories;

            if (categories.HasFlag(ProjectAreaFlags.Code))
                requestedCategories.AddRange(FindPage(PageId.Code).AllCategories);
            if (categories.HasFlag(ProjectAreaFlags.ProjectSettings))
                requestedCategories.AddRange(FindPage(PageId.ProjectSettings).AllCategories);
            if (categories.HasFlag(ProjectAreaFlags.Assets))
                requestedCategories.AddRange(FindPage(PageId.Assets).AllCategories);
            if (categories.HasFlag(ProjectAreaFlags.GameObjects))
                requestedCategories.AddRange(FindPage(PageId.GameObjects).AllCategories);
            if (categories.HasFlag(ProjectAreaFlags.Shaders))
                requestedCategories.AddRange(FindPage(PageId.Shaders).AllCategories);
            if (categories.HasFlag(ProjectAreaFlags.Build))
                requestedCategories.AddRange(FindPage(PageId.Build).AllCategories);

            return requestedCategories.ToArray();
        }

        ProjectAreaFlags GetPageProjectArea(PageId id)
        {
            switch (id)
            {
                case PageId.Code: return ProjectAreaFlags.Code;
                case PageId.Assets: return ProjectAreaFlags.Assets;
                case PageId.Shaders: return ProjectAreaFlags.Shaders;
                case PageId.GameObjects: return ProjectAreaFlags.GameObjects;
                case PageId.ProjectSettings: return ProjectAreaFlags.ProjectSettings;
                case PageId.Build: return ProjectAreaFlags.Build;
                default:
                    return ProjectAreaFlags.None;
            }
        }

        void DrawAssemblyFilter()
        {
            if (!activeView.Desc.ShowAssemblySelection)
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Contents.AssemblyFilter, LayoutSize.FilterOptionsLabelWidth);

                using (new EditorGUI.DisabledScope(!IsAnalysisValid() || SelectionWindow.IsOpen<AssemblySelectionWindow>()))
                {
                    if (GUILayout.Button(Contents.AssemblyFilterSelect, EditorStyles.miniButton,
                        GUILayout.Width(LayoutSize.FilterOptionsEnumWidth)))
                    {
                        if (m_AssemblyNames != null && m_AssemblyNames.Length > 0)
                        {
                            var analytic = AnalyticsReporter.BeginAnalytic();

                            // Note: Window auto closes as it loses focus so this isn't strictly required
                            if (SelectionWindow.IsOpen<AssemblySelectionWindow>())
                            {
                                SelectionWindow.CloseAll<AssemblySelectionWindow>();
                            }
                            else
                            {
                                var windowPosition =
                                    new Vector2(Event.current.mousePosition.x + LayoutSize.FilterOptionsEnumWidth,
                                        Event.current.mousePosition.y + GUI.skin.label.lineHeight);
                                var screenPosition = GUIUtility.GUIToScreenPoint(windowPosition);

                                SelectionWindow.Open<AssemblySelectionWindow>("Assemblies", screenPosition.x, screenPosition.y, m_AssemblySelection,
                                    m_AssemblyNames, selection =>
                                    {
                                        var selectEvent = AnalyticsReporter.BeginAnalytic();
                                        SetAssemblySelection(selection);

                                        var payload = new Dictionary<string, string>();
                                        var selectedAsmNames = selection.selection;

                                        payload["numSelected"] = selectedAsmNames.Count.ToString();
                                        #pragma warning disable UAC2001 // Avoid Linq
                                        payload["numUnityAssemblies"] = selectedAsmNames.Count(assemblyName => assemblyName.Contains("Unity")).ToString();
#pragma warning restore UAC2001

                                        AnalyticsReporter.SendEventWithKeyValues(AnalyticsReporter.UIButton.AssemblySelectApply, selectEvent, payload);
                                    });
                            }

                            AnalyticsReporter.SendEvent(AnalyticsReporter.UIButton.AssemblySelect,
                                analytic);
                        }
                    }
                }

                m_AssemblySelectionSummary = GetSelectedAssembliesSummary();
                Utility.DrawSelectedText(m_AssemblySelectionSummary);

                GUILayout.FlexibleSpace();
            }
        }

        // stephenm TODO - if AssemblySelectionWindow and AreaSelectionWindow end up sharing a common base class then
        // DrawAssemblyFilter() and DrawAreaFilter() can be made to call a common method and just pass the selection, names
        // and the type of window we want.
        void DrawAreaFilter()
        {
            if (!activeView.IsDiagnostic())
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Contents.AreaFilter, LayoutSize.FilterOptionsLabelWidth);

                if (NicifiedAreaNames.Length > 0)
                {
                    using (new EditorGUI.DisabledScope(!IsAnalysisValid() || SelectionWindow.IsOpen<AreaSelectionWindow>()))
                    {
                        if (GUILayout.Button(Contents.AreaFilterSelect, EditorStyles.miniButton,
                            GUILayout.Width(LayoutSize.FilterOptionsEnumWidth)))
                        {
                            var analytic = AnalyticsReporter.BeginAnalytic();

                            // Note: Window auto closes as it loses focus so this isn't strictly required
                            if (SelectionWindow.IsOpen<AreaSelectionWindow>())
                            {
                                SelectionWindow.CloseAll<AreaSelectionWindow>();
                            }
                            else
                            {
                                var windowPosition =
                                    new Vector2(Event.current.mousePosition.x + LayoutSize.FilterOptionsEnumWidth,
                                        Event.current.mousePosition.y + GUI.skin.label.lineHeight);
                                var screenPosition = GUIUtility.GUIToScreenPoint(windowPosition);

                                SelectionWindow.Open<AreaSelectionWindow>("Areas", screenPosition.x, screenPosition.y, m_AreaSelection,
                                    NicifiedAreaNames, selection =>
                                    {
                                        var selectEvent = AnalyticsReporter.BeginAnalytic();
                                        SetAreaSelection(selection);

                                        var payload = new Dictionary<string, string>();
                                        payload["areas"] = GetSelectedAreasSummary();
                                        AnalyticsReporter.SendEventWithKeyValues(AnalyticsReporter.UIButton.AreaSelectApply, selectEvent, payload);
                                    });
                            }

                            AnalyticsReporter.SendEvent(AnalyticsReporter.UIButton.AreaSelect, analytic);
                        }
                    }

                    m_AreaSelectionSummary = GetSelectedAreasSummary();
                    Utility.DrawSelectedText(ObjectNames.NicifyVariableName(m_AreaSelectionSummary));

                    GUILayout.FlexibleSpace();
                }
            }
        }

        void DrawFilters()
        {
            if (!activeView.Desc.ShowFilters)
            {
                // Clear search, just in case: Older versions of Profile Auditor let users apply search filters via
                // context menu without giving an option to clear it. Ideally, we'd simply stop the filtering from
                // happening at all, but the class/method structure makes that a bit awkward.
                activeView.SetSearch("");
                return;
            }

            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                m_ViewStates.filters = Utility.BoldFoldout(m_ViewStates.filters, Contents.FiltersFoldout);
                if (m_ViewStates.filters)
                {
                    EditorGUI.indentLevel++;

                    DrawAssemblyFilter();
                    DrawAreaFilter();

                    activeView.DrawSearch();

                    activeView.DrawFilters();

                    // The selected page may contribute its own filter controls (e.g. the Upgrade
                    // pages' target-version selector). Changing them re-filters the active view.
                    if (m_CurrentPage?.drawFilters != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        m_CurrentPage.drawFilters(m_ViewStates);
                        if (EditorGUI.EndChangeCheck())
                        {
                            activeView.MarkDirty();
                            activeView.ClearSelection();
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }

        // Draws the Home page: preferences, rules install and the Start Analysis button.
        void DrawHome()
        {

            const int k_SpacingHeight = 24;

            // Darkish grey box filling the window
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            // Draw centered in the window, with equal space to the left and right
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                // Begin drawing top to bottom
                using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(512), GUILayout.ExpandWidth(true)))
                {
                    GUILayout.FlexibleSpace();


                    // Title
                    using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
                    {
                        GUILayout.FlexibleSpace();
                        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                        {
                            EditorGUILayout.LabelField(Contents.WelcomeTextTitle, SharedStyles.TitleLabel, GUILayout.ExpandWidth(true));
                            EditorGUILayout.Space(k_SpacingHeight);
                            EditorGUILayout.LabelField(Contents.WelcomeText, SharedStyles.WelcomeTextArea, GUILayout.MaxWidth(512));
                        }
                        GUILayout.FlexibleSpace();
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(350)))
                        {
                            GUILayout.Space(k_SpacingHeight);
                            using (new EditorGUI.DisabledScope(RulesPackageInstallRequest != null))
                                UserPreferences.SharedPreferencesGUI();
                            GUILayout.Space(k_SpacingHeight);
                        }
                        GUILayout.FlexibleSpace();
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        const int k_ButtonWidth = 140;
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(k_ButtonWidth)))
                        {
                            // Analyze button
                            using (new EditorGUI.DisabledScope((m_AnalysisState == AnalysisState.InProgress) || (ProjectAuditorRulesPackage.IsInstalled == false) || (RulesPackageInstallRequest != null)))
                            {
                                var content = ProjectAuditorRulesPackage.IsInstalled ? Contents.AnalyzeButton : Contents.AnalyzeButtonDisabled;
                                if (GUILayout.Button(content, GUILayout.Width(k_ButtonWidth), GUILayout.Height(30)))
                                {
                                    bool canAnalyze = true;

                                    // m_Report can be null here (e.g. after cancelling an analysis)
                                    // In this case, there is nothing to save/discard.
                                    if (m_Report != null && m_Report.NeedsSaving)
                                    {
                                        DialogResult response = DialogResult.DefaultAction;
                                        if (m_AnalysisState == AnalysisState.Valid)
                                            response = EditorDialog.DisplayComplexDecisionDialog(k_Discard, k_DiscardQuestion, "Discard", "Save", "Cancel");
                                        else
                                            response = EditorUtility.DisplayDialog(k_Discard, k_DiscardQuestion, "Discard", "Cancel") ? DialogResult.DefaultAction : DialogResult.Cancel;

                                        if (response == DialogResult.AlternateAction)
                                        {
                                            if (!SaveReport(out var _))
                                                canAnalyze = false;
                                        }
                                        else if (response == DialogResult.Cancel)
                                        {
                                            canAnalyze = false;
                                        }
                                    }

                                    if (canAnalyze)
                                    {
                                        var projectAreas = UserPreferences.ProjectAreasToAnalyze;
                                        if (projectAreas == ProjectAreaFlags.None)
                                        {
                                            canAnalyze = false;
                                            if (EditorUtility.DisplayDialog(k_EnableAreas, k_EnableAreasQuestion, "Ok", "Cancel"))
                                            {
                                                UserPreferences.ProjectAreasToAnalyze.Set(ProjectAreaFlags.All);
                                                projectAreas.Set(ProjectAreaFlags.All);
                                                canAnalyze = true;
                                            }
                                        }

                                        if ((projectAreas & ProjectAreaFlags.Code) != 0)
                                        {
                                            if (canAnalyze)
                                                canAnalyze = ValidateCodeAnalysisWithPopup();
                                        }
                                    }

                                    if (canAnalyze)
                                    {
                                        Analyze();
                                        GUIUtility.ExitGUI();
                                    }
                                }
                            }

                            // Install rules
                            using (new EditorGUI.DisabledScope((m_AnalysisState == AnalysisState.InProgress) || (ProjectAuditorRulesPackage.IsLatest) || (RulesPackageInstallRequest != null)))
                            {
                                var content = Contents.InstallRulesButton;
                                if (RulesPackageInstallRequest != null)
                                {
                                    int frame = Utility.GetStatusWheelFrame();
                                    content = Contents.UpdateRulesButtonInProgress[frame];
                                }
                                else if (ProjectAuditorRulesPackage.IsLatest)
                                {
                                    content = Contents.UpdateRulesButtonDisabled;
                                }
                                else if (ProjectAuditorRulesPackage.IsInstalled)
                                {
                                    content = Contents.UpdateRulesButton;
                                }

                                if (GUILayout.Button(content, GUILayout.Width(k_ButtonWidth), GUILayout.Height(30)))
                                {
                                    RulesPackageInstallRequest = Client.Add(ProjectAuditorRulesPackage.Name);
                                    EditorApplication.update += RulesPackageInstallProgressCallback;
                                }
                            }

                            // Preferences button
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("All Preferences", SharedStyles.LinkLabel, GUILayout.Height(30)))
                                {
                                    EditorInterop.OpenProjectAuditorPreferences();
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.FlexibleSpace();
                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndVertical();
        }

        bool ValidateCodeAnalysisWithPopup()
        {
            bool validPreferences = true;
            var codeAnalysisFlags = UserPreferences.CodeAnalysisFlags;
            var codeOwnerFlags = UserPreferences.CodeOwnerFlags;

            if (codeOwnerFlags == CodeOwnerFlags.None)
            {
                validPreferences = false;
                if (EditorUtility.DisplayDialog(k_EnableCodeOwners, k_EnableCodeOwnersQuestion, "Ok", "Cancel"))
                {
                    UserPreferences.CodeOwnerFlags.Set(CodeOwnerFlags.User);
                    codeOwnerFlags.Set(CodeOwnerFlags.User);
                    validPreferences = true;
                }
            }

            if (validPreferences && (codeAnalysisFlags & (CodeAnalysisFlags.Player | CodeAnalysisFlags.Editor)) == 0)
            {
                validPreferences = false;
                EditorUtility.DisplayDialog(k_NoCodeSelected, k_NoCodeSelectedMessage, "Ok");
            }

            return validPreferences;
        }

        void DrawPanels()
        {
            if (activeView.ShowVerticalScrollView)
            {
                float widthDifference = 0f;
                if (Event.current.type == EventType.Repaint)
                {
                    // If window size changes and user still holds the mouse button we won't generally get another
                    // following repaint event. "LastVerticalScrollViewSize" is one repaint behind, so here we correct
                    // that width to correctly clip GL rendering within the scroll view area.

                    if (m_PreviousWindowSize != position.size)
                        widthDifference = position.size.x - m_PreviousWindowSize.x;
                    m_PreviousWindowSize = position.size;
                }

                activeView.VerticalScrollViewPos = EditorGUILayout.BeginScrollView(activeView.VerticalScrollViewPos,
                    false, false, GUIStyle.none,
                    GUI.skin.verticalScrollbar, GUI.skin.scrollView);

                Rect clipRect = new Rect(activeView.VerticalScrollViewPos.x, activeView.VerticalScrollViewPos.y,
                    activeView.LastVerticalScrollViewSize.x - GUI.skin.verticalScrollbar.fixedWidth + widthDifference - 1f,
                    activeView.LastVerticalScrollViewSize.y);
                m_Draw2D.SetClipRect(clipRect);
            }

            DrawReport();

            if (activeView.ShowVerticalScrollView)
            {
                EditorGUILayout.EndScrollView();
                m_Draw2D.ClearClipRect();

                if (Event.current.type == EventType.Repaint)
                {
                    var rectSize = GUILayoutUtility.GetLastRect().size;
                    activeView.LastVerticalScrollViewSize = new Vector2(rectSize.x, rectSize.y);
                }
            }
        }

        void DrawStatusBar()
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(20)))
            {
                var selectedIssues = activeView.GetSelectionCount();
                int selectionSize = Math.Min(selectedIssues, activeView.NumFilteredIssues);
                var info = selectionSize + " / " + activeView.NumFilteredIssues + " Item(s) selected";
                EditorGUILayout.LabelField(info, GUILayout.ExpandWidth(true), GUILayout.Width(200));

                GUILayout.FlexibleSpace();

                // Disable zoom option for now since it doesn't behave very well (and there doesn't seem to be any similar
                // functionality in the rest of Unity). COPT-3412
                // Allow the size-setting code to still run, in case non-default values were stored from a previous version.
                var fontSize = ViewStates.DefaultMinFontSize;
                if (fontSize != m_ViewStates.fontSize)
                {
                    m_ViewStates.fontSize = fontSize;
                    SharedStyles.SetFontDynamicSize(m_ViewStates.fontSize);
                }

                EditorGUILayout.LabelField("Rules Version: " + ProjectAuditorRulesPackage.Version, EditorStyles.label, GUILayout.Width(120));
            }
        }

        void DrawReport()
        {
            GUILayout.Space(2);

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(4);

                    GUILayout.Label(activeView.Desc.DisplayName, SharedStyles.MediumTitleLabel);

                    if (activeView is SummaryView && m_Report != null)
                    {
                        GUILayout.Label(" | ", SharedStyles.MediumTitleLabel);

                        GUILayout.Label(m_Report.DisplayName, SharedStyles.MediumTitleLabel);

                        if (m_Report != null && m_Report.NeedsSaving)
                        {
                            GUILayout.Label("*", SharedStyles.MediumTitleLabel);
                        }
                    }

                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(8);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(4);
                    GUILayout.Label(activeView.Description, GUILayout.MinWidth(360), GUILayout.ExpandWidth(true));
                    GUILayout.FlexibleSpace();
                }
            }

            activeView.DrawTopPanel();

            if (activeView.IsValid())
            {
                DrawFilters();

                if (m_ShouldRefresh || m_AnalysisState == AnalysisState.Completed)
                {
                    RefreshWindow();
                    m_ShouldRefresh = false;
                }

                activeView.DrawContent();
            }
        }

        internal void SetAreaSelection(TreeViewSelection selection)
        {
            m_SelectedAreas = Areas.None;
            if (selection.selection != null)
            {
                foreach (var areaName in selection.selection)
                {
                    // Selection entries are nicified area names, e.g. "Build Size"
                    if (Enum.TryParse(areaName.Replace(" ", ""), out Areas area))
                        m_SelectedAreas |= area;
                }
            }

            m_AreaSelection = selection;
            RefreshWindow();
        }

        internal void SetAssemblySelection(TreeViewSelection selection)
        {
            m_AssemblySelection = selection;
            RefreshWindow();
        }

        void UpdateAreaSelection()
        {
            if (m_AreaSelection == null)
            {
                m_AreaSelection = new TreeViewSelection();
                if (!string.IsNullOrEmpty(m_AreaSelectionSummary))
                {
                    if (m_AreaSelectionSummary == "All")
                    {
                        m_AreaSelection.SetAll(NicifiedAreaNames);
                        m_SelectedAreas = AreasExtensions.All;
                    }
                    else if (m_AreaSelectionSummary != "None")
                    {
                        var areas = Formatting.SplitStrings(m_AreaSelectionSummary);
                        foreach (var area in areas)
                            m_AreaSelection.selection.Add(ObjectNames.NicifyVariableName(area));
                        m_SelectedAreas = (Areas)Enum.Parse(typeof(Areas), m_AreaSelectionSummary);
                    }
                }
                else
                {
                    m_AreaSelection.SetAll(NicifiedAreaNames);
                    m_SelectedAreas = AreasExtensions.All;
                }
            }
        }

        void UpdateAssemblyNames()
        {
            if (m_Report == null || m_ViewManager.HasPendingCategory(IssueCategory.Assembly))
                return;

#pragma warning disable UAC2001, UAC2010 // Avoid Linq
            var assemblyNames = m_Report.FindByCategory(IssueCategory.Assembly).Select(i => new System.Tuple<string, bool>(i.Description, i.GetCustomPropertyBool(AssemblyProperty.ReadOnly)));
            var allAssemblies = assemblyNames.GroupBy(i => i.Item1).Select(g => g.First()).OrderBy(i => i.Item1).ToArray();
#pragma warning restore UAC2001, UAC2010

            var codeOwnerFlags = m_Report.SessionInfo.CodeOwnerFlags;
            bool allowPackages = (m_Report.SessionInfo.CodeAnalysisFlags & CodeAnalysisFlags.Packages) != 0;
            bool allowUnityCode = (codeOwnerFlags & CodeOwnerFlags.Unity) != 0;
            bool allowUserCode = (codeOwnerFlags & CodeOwnerFlags.User) != 0;

#pragma warning disable UAC2001 // Avoid Linq
            // update list of assembly names
            if (m_Report.IsForCurrentProject())
                allAssemblies = allAssemblies.Where(a => !AssemblyInfoProvider.FilterAssembly(a.Item1, allowPackages, allowUnityCode, allowUserCode)).ToArray();
#pragma warning restore UAC2001

            m_AssemblyNames = Array.ConvertAll(allAssemblies, a => a.Item1);
            m_AssemblyReadOnlyFlags = Array.ConvertAll(allAssemblies, a => a.Item2);
        }

        void UpdateAssemblySelection(bool forceRefresh = false)
        {
            if (m_AssemblyNames == null)
                return;

            if (m_AssemblySelection == null)
                m_AssemblySelection = new TreeViewSelection();

            m_AssemblySelection.selection.Clear();
            if (!forceRefresh && !string.IsNullOrEmpty(m_AssemblySelectionSummary))
            {
                if (m_AssemblySelectionSummary == "All")
                {
                    m_AssemblySelection.SetAll(m_AssemblyNames);
                }
                else if (m_AssemblySelectionSummary != "None")
                {
                    #pragma warning disable UAC2001 // Avoid Linq
                    var assemblies = Formatting.SplitStrings(m_AssemblySelectionSummary)
                        .Where(assemblyName => Array.IndexOf(m_AssemblyNames, assemblyName) != -1);
#pragma warning restore UAC2001
                    m_AssemblySelection.selection.AddRange(assemblies);
                }
            }

            if (m_Report != null && m_Report.IsForCurrentProject())
            {
                if (forceRefresh || m_AssemblySelection.selection.Count == 0)
                {
                    var codeOwnerFlags = m_Report.SessionInfo.CodeOwnerFlags;
                    bool allowPackages = (m_Report.SessionInfo.CodeAnalysisFlags & CodeAnalysisFlags.Packages) != 0;
                    bool allowUnityCode = (codeOwnerFlags & CodeOwnerFlags.Unity) != 0;
                    bool allowUserCode = (codeOwnerFlags & CodeOwnerFlags.User) != 0;

                    var compiledAssemblies = new List<string>(m_AssemblyNames.Length);
                    for (int i = 0; i < m_AssemblyNames.Length; i++)
                    {
                        if (!m_AssemblyReadOnlyFlags[i])
                            compiledAssemblies.Add(m_AssemblyNames[i]);
                    }

                    m_AssemblySelection.selection.AddRange(compiledAssemblies);

                    if (m_AssemblySelection.selection.Count == 0)
                        m_AssemblySelection.SetAll(m_AssemblyNames);
                }
            }

            if (forceRefresh)
                m_AssemblySelection.SetAll(m_AssemblyNames);

            // update assembly selection summary
            m_AssemblySelectionSummary = GetSelectedAssembliesSummary();
        }

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var analysisTarget = BuildTarget.NoTarget;

                if (m_Report != null && m_Report.SessionInfo != null)
                {
                    analysisTarget = m_Report.SessionInfo.Platform;
                }

                if (analysisTarget == BuildTarget.NoTarget)
                {
                    analysisTarget = GetSelectedAnalysisPlatform();
                }

                GUILayout.Label("Platform: ", SharedStyles.Label, GUILayout.Width(55));
                GUILayout.Label(Utility.GetPlatformIconWithName(analysisTarget), SharedStyles.IconLabelLeft);

                if (m_AnalysisState == AnalysisState.InProgress)
                    GUILayout.Label(Utility.GetIcon(Utility.IconType.StatusWheel), SharedStyles.IconLabel, GUILayout.Width(AnalysisView.ToolbarIconSize));

                GUILayout.FlexibleSpace();

                // right-end buttons
                const int discardButtonWidth = 120;
                const int loadSaveButtonWidth = 40;

                using (new EditorGUI.DisabledScope(m_AnalysisState != AnalysisState.Valid && m_AnalysisState != AnalysisState.InProgress))
                {
                    if (m_AnalysisState == AnalysisState.InProgress)
                    {
                        if (GUILayout.Button(Contents.CancelButton, EditorStyles.toolbarButton, GUILayout.Width(discardButtonWidth)))
                            m_Progress.Cancel();
                    }
                    else if (GUILayout.Button(Contents.NewAnalysisButton, EditorStyles.toolbarButton, GUILayout.Width(discardButtonWidth)))
                    {
                        m_ShowHomePage = true;
                        m_ViewSelectionTreeView?.SelectPage(FindPage(PageId.Home), true);
                        GUIUtility.ExitGUI();
                    }
                }

                using (new EditorGUI.DisabledScope(m_AnalysisState == AnalysisState.InProgress || !ProjectAuditorRulesPackage.IsInstalled))
                {
                    var loadContent = ProjectAuditorRulesPackage.IsInstalled ? Contents.LoadButton : Contents.LoadButtonDisabled;
                    if (GUILayout.Button(loadContent, EditorStyles.toolbarButton, GUILayout.Width(loadSaveButtonWidth)))
                    {
                        // Defer native dialogs past the current IMGUI frame: on macOS they drive the Cocoa event loop,
                        // causing a re-entrant IMGUI frame that corrupts the layout group stack (UUM-139712).
                        EditorApplication.delayCall += LoadReport;
                        GUIUtility.ExitGUI();
                    }
                }

                using (new EditorGUI.DisabledScope(m_AnalysisState != AnalysisState.Valid))
                {
                    if (GUILayout.Button(Contents.SaveButton, EditorStyles.toolbarButton,
                        GUILayout.Width(loadSaveButtonWidth)))
                    {
                        // See comment above on DiscardButton for why delayCall + ExitGUI are used here (UUM-139712).
                        EditorApplication.delayCall += SaveCurrentReport;
                        GUIUtility.ExitGUI();
                    }
                }

                Utility.DrawHelpButton(Contents.HelpButton, Documentation.GetPageUrl("index"));
            }
        }

        bool SaveReport(out string path)
        {
            // Avoid unsupported save name characters from the report's displayname (project name)
            var invalidChars = Path.GetInvalidFileNameChars();
            var reportDisplayName = new StringBuilder(m_Report.DisplayName);
            foreach (var c in invalidChars)
            {
                reportDisplayName.Replace(c, '_');
            }

            path = EditorUtility.SaveFilePanel(k_SaveToFile, UserPreferences.LoadSavePath, reportDisplayName.ToString(), "projectauditor");
            if (path.Length != 0)
            {
                m_Report.NeedsSaving = false;
                m_Report.DisplayName = Path.GetFileNameWithoutExtension(path);

                m_Report.Save(path);
                AutosaveReport();

                UserPreferences.LoadSavePath = Path.GetDirectoryName(path);

                return true;
            }

            return false;
        }

        void SaveCurrentReport()
        {
            if (SaveReport(out var path))
            {
                EditorUtility.RevealInFinder(path);
                AnalyticsReporter.SendEvent(AnalyticsReporter.UIButton.Save, AnalyticsReporter.BeginAnalytic());
            }
        }

        void LoadReport()
        {
            var path = EditorUtility.OpenFilePanel(k_LoadFromFile, UserPreferences.LoadSavePath, "projectauditor");
            if (path.Length != 0)
            {
                LoadReportFromFile(path);
            }
        }

        void LoadReportFromFile(string path)
        {
            Report newReport = Report.Load(path, out var errorMessage);
            var fileWasManuallySaved = path != GetAutosaveFilename();

            if (newReport == null)
            {
                if (fileWasManuallySaved)
                {
                    EditorUtility.DisplayDialog(k_LoadFromFile, k_LoadingFailedVersion + "\n" + errorMessage, "Ok");
                }
                else
                {
                    Debug.LogWarning(k_LoadingAutosaveFailedVersion + "\n" + errorMessage);
                    DeleteAutosave();
                }
                return;
            }

            if (newReport.NumTotalIssues == 0)
            {
                if (fileWasManuallySaved)
                {
                    EditorUtility.DisplayDialog(k_LoadFromFile, k_LoadingFailed, "Ok");
                }
                else
                {
                    Debug.LogWarning(k_LoadingAutosaveFailed);
                    DeleteAutosave();
                }
                return;
            }

            m_Report = newReport;
            if (fileWasManuallySaved)
                m_Report.DisplayName = Path.GetFileNameWithoutExtension(path);

            if (m_Report.IsForCurrentProject() == false)
            {
                if (fileWasManuallySaved || !String.IsNullOrEmpty(m_Report.SessionInfo.ProjectId))
                    EditorUtility.DisplayDialog(k_ReportMismatch, k_ReportMismatchDetail, "Ok");
            }

            if (m_ProjectAuditor == null)
                m_ProjectAuditor = new ProjectAuditor();

            m_LoadButtonAnalytic = AnalyticsReporter.BeginAnalytic();
            m_AnalysisState = AnalysisState.Valid;
            UserPreferences.LoadSavePath = Path.GetDirectoryName(path);
            m_ViewManager = null; // make sure ViewManager is reinitialized

            OnEnable();

            UpdateAssemblyNames();
            UpdateAssemblySelection();

            m_ViewManager.MarkViewColumnWidthsAsDirty();

            // switch to summary view after loading
            ShowPage(FindPage(PageId.Optimization));
            m_ViewManager.GetActiveView().SetSearch("");
        }

        string GetAutosaveFilename()
        {
            var projectPath = ProjectAuditor.ProjectPath;
            var libraryPath = Path.Combine(projectPath, "Library");

            return Path.Combine(libraryPath, k_ReportAutoSaveFilename);
        }

        void AutosaveReport()
        {
            if (m_Report?.IsValid() ?? false)
                m_Report.Save(GetAutosaveFilename());
        }

        void TryLoadAutosavedReport()
        {
            var filename = GetAutosaveFilename();

            if (!File.Exists(filename))
            {
                return;
            }

            LoadReportFromFile(filename);
        }

        void DeleteAutosave()
        {
            var filename = GetAutosaveFilename();

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(Contents.PreferencesMenuItem, false, OpenPreferences);
        }

        static void OpenPreferences()
        {
            var preferencesWindow = SettingsService.OpenUserPreferences(UserPreferences.Path);
            if (preferencesWindow == null)
            {
                Debug.LogError($"Could not find Preferences for 'Analysis/{ProjectAuditor.DisplayName}'");
            }
        }

        [MenuItem("Window/Analysis/" + ProjectAuditor.DisplayName)]
        public static ProjectAuditorWindow ShowWindow()
        {
            var wnd = GetWindow(typeof(ProjectAuditorWindow)) as ProjectAuditorWindow;
            if (wnd != null)
            {
                wnd.minSize = new Vector2(LayoutSize.MinWindowWidth, LayoutSize.MinWindowHeight);
                wnd.titleContent = Contents.WindowTitle;
            }

            return wnd;
        }

        const string k_LoadFromFile = "Load from file";
        const string k_LoadingFailedVersion = "Report file is not compatible with this version of Project Auditor.  Please start a new analysis.";
        const string k_LoadingAutosaveFailedVersion = "Autosaved report file is not compatible with this version of Project Auditor.  Please start a new analysis.";
        const string k_LoadingFailed = "Loading report from file was unsuccessful.";
        const string k_LoadingAutosaveFailed = "Loading autosaved report from file was unsuccessful.";
        const string k_ReportMismatch = "Report is from another project";
        const string k_ReportMismatchDetail = "This report does not match the currently loaded project.  Some features may be unavailable.";
        const string k_SaveToFile = "Save report to projectauditor file";
        const string k_Discard = "Start New Analysis";
        const string k_DiscardQuestion = "If you start a new analysis, the current report will be discarded.";
        const string k_EnableAreas = "No Project Areas selected";
        const string k_EnableCodeOwners = "No Code Owners selected";
        const string k_EnableAreasQuestion = "Enable all analysis areas and continue?\n\nAreas can be individually toggled in the Project Auditor section of Preferences.";
        const string k_EnableCodeOwnersQuestion = "Enable user code analysis and continue?\n\nCode owners can be individually toggled in the Project Auditor section of Preferences.";
        const string k_NoCodeSelected = "Invalid Code Analysis Areas";
        const string k_NoCodeSelectedMessage = "Please select either Editor, Player or both.";

        // UI styles and layout
        internal static class LayoutSize
        {
            const int kFilterContentsWidth = 320;

            public static readonly int MinWindowWidth = 410;
            public static readonly int MinWindowHeight = 640;
            public static readonly GUILayoutOption FilterOptionsLabelWidth = GUILayout.Width(104);
            public static readonly GUILayoutOption FilterOptionsContentsWidth = GUILayout.Width(kFilterContentsWidth);
            public static readonly GUILayoutOption FilterOptionsContentsHalfWidth = GUILayout.Width(kFilterContentsWidth / 2);
            public static readonly int FilterOptionsEnumWidth = 50;
            public const float kTreeViewWidth = 190.0f;
        }

        static class Contents
        {
            public static readonly GUIContent WindowTitle = new GUIContent(ProjectAuditor.DisplayName);

            public static readonly GUIContent AnalyzeButton =
                new GUIContent("Start Analysis", "Analyze Project and list all issues found.");
            public static readonly GUIContent AnalyzeButtonDisabled =
                new GUIContent("Start Analysis", $"Please install the rules package to analyze your project ({ProjectAuditorRulesPackage.Name}).");

            public static readonly GUIContent InstallRulesButton =
                new GUIContent("Install Rules", $"Please install the rules package to analyze your project ({ProjectAuditorRulesPackage.Name}).");
            public static readonly GUIContent UpdateRulesButton =
                new GUIContent("Update Rules", $"Please update your rules package to the latest version ({ProjectAuditorRulesPackage.Name}@{ProjectAuditorRulesPackage.LatestVersion}).");
            public static readonly GUIContent UpdateRulesButtonDisabled =
                new GUIContent("Update Rules", "Everything is up to date!");
            public static readonly GUIContent[] UpdateRulesButtonInProgress;

            public static readonly GUIContent SaveButton = Utility.GetIcon(Utility.IconType.Save, "Save current report to projectauditor file");
            public static readonly GUIContent LoadButton = Utility.GetIcon(Utility.IconType.Load, "Load report from projectauditor file");
            public static readonly GUIContent LoadButtonDisabled = Utility.GetIcon(Utility.IconType.Load, $"Please install the rules package to load reports ({ProjectAuditorRulesPackage.Name}).");
            public static readonly GUIContent NewAnalysisButton = EditorGUIUtility.TrTextContentWithIcon("New Analysis", "Return to the Home page to start a new analysis. If you start a new analysis, the current report will be discarded.", "Refresh");
            public static readonly GUIContent CancelButton = EditorGUIUtility.TrTextContentWithIcon("Cancel Analysis", "Cancel the in-progress analysis", "Clear");

            public static readonly GUIContent HelpButton = Utility.GetIcon(Utility.IconType.Help, "Open Manual (in a web browser)");
            public static readonly GUIContent PreferencesMenuItem = EditorGUIUtility.TrTextContent("Preferences", $"Open User Preferences for {ProjectAuditor.DisplayName}");

            public static readonly GUIContent AssemblyFilter = EditorGUIUtility.TrTextContent("Assembly:", "Select assemblies to examine");
            public static readonly GUIContent AssemblyFilterSelect = EditorGUIUtility.TrTextContent("Select", "Select assemblies to examine");
            public static readonly GUIContent AreaFilter = EditorGUIUtility.TrTextContent("Areas:", "Select performance areas to display");
            public static readonly GUIContent AreaFilterSelect = EditorGUIUtility.TrTextContent("Select", "Select performance areas to display");
            public static readonly GUIContent FiltersFoldout = EditorGUIUtility.TrTextContent("Filters", "Filtering Criteria");


            public static readonly GUIContent WelcomeTextTitle = new GUIContent($"Welcome to {ProjectAuditor.DisplayName}");

            public static readonly GUIContent WelcomeText = new GUIContent(
@"Select <b>Install Rules</b> to install the Rules package and enable project analysis. 
To generate a report, select the project area, platform, and code to analyze then select <b>Start Analysis</b>."
            );

            public static readonly GUIContent Clear = new GUIContent("Clear");
            public static readonly GUIContent Refresh = new GUIContent("Refresh");

            public static readonly GUIContent ShaderVariants = new GUIContent("Variants", "Inspect Shader Variants");

            public static readonly string PendingAnalyzeInfoText = L10n.Tr("{0} analysis is still running in the background… (see more in Window > General > Progress)", null);
            public static readonly string AnalyzeInfoText = L10n.Tr("{0} analysis is not yet included in this report. Run analysis now?", null);
            public static readonly string AnalyzeButtonText = L10n.Tr("Start {0} Analysis", null);

            public static readonly GUIContent OpenBackgroundTasks = EditorGUIUtility.TrTextContent("Open Background Tasks");
            public static readonly GUIContent ProjectAreaSelection = new GUIContent("Project Areas", "Select project areas to analyze.");
            public static readonly GUIContent PlatformSelection = new GUIContent("Platform", "Select the target platform.");
            public static readonly GUIContent CompilationModeSelection = new GUIContent("Compilation Mode", "Select the compilation mode.");

            static Contents()
            {
                UpdateRulesButtonInProgress = new GUIContent[12];
                for (int i = 0; i < 12; i++)
                    UpdateRulesButtonInProgress[i] = EditorGUIUtility.TrTextContentWithIcon(" Installing Rules...", "WaitSpin" + i.ToString("00"));
            }
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
