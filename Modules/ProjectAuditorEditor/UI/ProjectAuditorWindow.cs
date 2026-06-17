// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define PA_WELCOME_VIEW_OPTIONS
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
using UnityEditor;

using UnityEngine;
using UnityEngine.Profiling;

using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal class ProjectAuditorWindow : EditorWindow, IHasCustomMenu, IIssueFilter
    {
        enum AnalysisState
        {
            Initializing,
            Initialized,
            InProgress,
            Completed,
            Valid
        }

        static readonly string[] AreaNames = Enum.GetNames(typeof(Areas)).Where(a => a != "None" && a != "All").ToArray();
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
        bool m_ShouldRefresh;
        AnalyticsReporter.Analytic m_AnalyzeButtonAnalytic;
        AnalyticsReporter.Analytic m_LoadButtonAnalytic;

        // UI
        TreeViewSelection m_AreaSelection;
        TreeViewSelection m_AssemblySelection;
        List<string> m_DefaultAssemblies = new List<string>();

        internal List<string> DefaultAssemblies => m_DefaultAssemblies;

        Draw2D m_Draw2D;

        internal Draw2D Draw2D => m_Draw2D;

        Areas m_SelectedAreas;

        // Serialized fields
        [SerializeField] string m_AreaSelectionSummary;
        [SerializeField] string[] m_AssemblyNames;
        [SerializeField] string m_AssemblySelectionSummary;
        [SerializeField] Report m_Report;
        [SerializeField] AnalysisState m_AnalysisState = AnalysisState.Initializing;
        [SerializeField] ViewStates m_ViewStates = new ViewStates();
        [SerializeField] ViewManager m_ViewManager;


        static readonly string k_ReportAutoSaveFilename = "projectauditor-report-autosave.projectauditor";

        [SerializeField] private Tab[] m_Tabs = GetDefaultTabs();

        AnalysisView activeView => m_ViewManager.GetActiveView();

        [SerializeField] int m_ActiveTabIndex = 0;
        int m_TabButtonControlID;

        [SerializeField] TreeViewState m_ViewSelectionTreeState;
        ViewSelectionTreeView m_ViewSelectionTreeView;

        [SerializeField] bool m_IsNonAnalyzedViewSelected;
        [SerializeField] Tab m_SelectedNonAnalyzedTab;

        Vector2 m_PreviousWindowSize;

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
#pragma warning disable UA2001
            foreach (var p in args.added.Concat(args.changedTo))
#pragma warning restore UA2001
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

        private static Tab[] GetDefaultTabs()
        {
            Tab[] tabs = {
                new Tab
                {
                    id = TabId.Summary, name = "Summary", categories = new SerializableEnum<IssueCategory>[] { IssueCategory.Metadata }
                },
                new Tab
                {
                    id = TabId.Code, name = "Code",
                    categories = new SerializableEnum<IssueCategory>[]
                    {
                        IssueCategory.Code, IssueCategory.Assembly, IssueCategory.PrecompiledAssembly,
                        IssueCategory.CodeCompilerMessage, IssueCategory.DomainReload
                    }
                },
                new Tab
                {
                    id = TabId.Assets, name = "Assets",
                    categories = new SerializableEnum<IssueCategory>[]
                    {
                        IssueCategory.AssetIssue, IssueCategory.Texture, IssueCategory.SpriteAtlas, IssueCategory.Mesh,
                        IssueCategory.AudioClip, IssueCategory.AnimatorController, IssueCategory.AnimationClip,
                        IssueCategory.Avatar, IssueCategory.AvatarMask
                    }
                },
                new Tab
                {
                    id = TabId.Shaders, name = "Shaders",
                    categories = new SerializableEnum<IssueCategory>[]
                    {
                        IssueCategory.Shader, IssueCategory.ShaderVariant, /*IssueCategory.ComputeShaderVariant,*/
                        IssueCategory.ShaderCompilerMessage, IssueCategory.Material
                    }
                },
                new Tab
                {
                    id = TabId.Settings, name = "Project",
                    categories = new SerializableEnum<IssueCategory>[] { IssueCategory.ProjectSetting, IssueCategory.Package }
                },
                new Tab
                {
                    id = TabId.Build, name = "Build",
                    categories = new SerializableEnum<IssueCategory>[] { IssueCategory.BuildFile, IssueCategory.BuildStep }
                },
            };

            return tabs;
        }

        // return true if:
        // - this issue is not in a package
        // - we're analyzing package for issues
        // return false otherwise
        public bool PackageFilterMatch(ReportItem issue)
        {
            if (UserPreferences.AnalyzePackagesForIssues == true)
                return true;

            if (issue.Location == null)
                return false;

            if ((issue.Location.Path.IndexOf("packages/", StringComparison.OrdinalIgnoreCase) >= 0)
                || (issue.Location.Path.IndexOf("Unity.SourceGenerators/", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return false;
            }

            var isDiagnostic = issue.IsIssue();
            if (!isDiagnostic)
                return true;

            return true;
        }

        public bool Match(ReportItem issue)
        {
            // return false if the issue does not match one of these criteria:
            // - assembly name, if applicable
            // - area
            // - is not muted, if enabled
            // - critical context, if enabled/applicable

            var viewDesc = activeView.Desc;

            if (!PackageFilterMatch(issue))
                return false;

            Profiler.BeginSample("MatchAssembly");
            var matchAssembly = !viewDesc.ShowAssemblySelection ||
                m_AssemblySelection != null &&
                (m_AssemblySelection.Contains(viewDesc.GetAssemblyName(issue)) ||
                    m_AssemblySelection.ContainsGroup("All"));
            Profiler.EndSample();
            if (!matchAssembly)
                return false;

            var isDiagnostic = issue.IsIssue();
            if (!isDiagnostic)
                return true;

            // TODO: the rest of this logic is common to all diagnostic views. It should be moved to the AnalysisView
            if (activeView.IsDiagnostic()) // Only checking matching areas on views that support Area filtering
            {
                Profiler.BeginSample("MatchArea");
                var matchArea = issue.Id.IsValid() && issue.Id.GetDescriptor().MatchesAnyAreas(m_SelectedAreas);

                Profiler.EndSample();
                if (!matchArea)
                    return false;
            }

            if (m_ViewStates.onlyCriticalIssues &&
                !issue.IsMajorOrCritical())
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

            if (m_Report != null && !m_Report.IsValid())
            {
                IssueCategory[] categories = (IssueCategory[])Enum.GetValues(typeof(IssueCategory));
                var requestedModules = categories.SelectMany(m_ProjectAuditor.GetModules).Distinct().ToArray();
                m_Report.PostSerializeLayoutUpdate(requestedModules);
            }

            UserPreferences.OnAnalyzePackageIsChanged = i =>
            {
                UpdateAssemblyNames();
                UpdateAssemblySelection(true);
                m_ViewManager.GetActiveView().MarkDirty();
                Repaint();
            };

            var currentState = m_AnalysisState;
            m_AnalysisState = AnalysisState.Initializing;
            m_Tabs = GetDefaultTabs();

            AnalyticsReporter.EnableAnalytics();

            Profiler.BeginSample("Update Selections");
            UpdateAreaSelection();
            UpdateAssemblySelection();
            Profiler.EndSample();

            InitializeViews(ProjectAuditorSettings.instance.Rules, true);

            // are we reloading from a valid state?
            if (currentState == AnalysisState.Valid &&
                m_Report != null &&
                m_Report.IsValid())
            {
                Profiler.BeginSample("Views Update");
                m_ViewManager.OnAnalysisRestored(m_Report);
                m_AnalysisState = currentState;
                Profiler.EndSample();
            }
            else
            {
                if (m_tryingFallback == false)
                {
                    m_tryingFallback = true;
                    m_ActiveTabIndex = 0;
                    m_AnalysisState = AnalysisState.Initialized;

                    TryLoadAutosavedReport();
                }

                m_tryingFallback = false;
            }

            m_Draw2D = new Draw2D(ProjectAuditor.s_DataPath + "/Shaders/ProjectAuditor.shader");

            Profiler.BeginSample("Refresh");
            RefreshWindow();
            Profiler.EndSample();

            wantsMouseMove = true;
        }

        void InitializeViews(SeverityRules rules, bool reload)
        {
            var initialize = m_ViewManager == null || !reload;

            if (initialize)
            {
                // Get all supported categories
                List<IssueCategory> supportedCategories = new List<IssueCategory>();
                foreach (var tab in m_Tabs)
                {
                    supportedCategories.AddRange(GetTabCategories(tab));
                }

                var categories = supportedCategories.Distinct();

                // Get all the ViewDescriptors that match the supported categories, and sort them by MenuOrder
                var viewDescriptors = ViewDescriptor.GetAll()
                    .Where(descriptor => categories.Contains(descriptor.Category)).ToArray();
                Array.Sort(viewDescriptors, (a, b) => a.MenuOrder.CompareTo(b.MenuOrder));

                m_ViewManager = new ViewManager(viewDescriptors.Select(d => d.Category).ToArray()); // view manager needs sorted categories
            }

            m_ViewManager.OnActiveViewChanged += i =>
            {
                var viewDesc = m_ViewManager.GetView(i).Desc;
                AnalyticsReporter.SendEvent(
                    (AnalyticsReporter.UIButton)viewDesc.AnalyticsEventId,
                    AnalyticsReporter.BeginAnalytic());

                SyncTabOnViewChange(viewDesc.Category);

                m_IsNonAnalyzedViewSelected = false;

                m_ViewSelectionTreeView.SelectItemByCategory(viewDesc.Category);

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

                m_ViewManager.GetView(IssueCategory.Metadata)?.MarkDirty();
                m_Report.NeedsSaving = true;

                var summary = m_ViewManager.GetView(IssueCategory.Metadata);
                if (summary is SummaryView summaryView)
                    summaryView.MarkDirty();
            };

            m_ViewManager.OnSelectedIssuesDisplayRequested = issues =>
            {
                var analytic = AnalyticsReporter.BeginAnalytic();

                AnalyticsReporter.SendEventWithSelectionSummary(
                    AnalyticsReporter.UIButton.Unmute, analytic, issues);

                m_ViewManager.GetView(IssueCategory.Metadata)?.MarkDirty();
                m_Report.NeedsSaving = true;
            };

            m_ViewManager.OnSelectedIssuesQuickFixRequested = issues =>
            {
                m_ViewManager.GetView(IssueCategory.Metadata)?.MarkDirty();
            };

            m_ViewManager.OnAnalysisRequested += category =>
            {
                AuditCategories(new[] { category });
                GUIUtility.ExitGUI();
            };

            m_ViewManager.OnViewExportCompleted += () =>
            {
                AnalyticsReporter.SendEvent(AnalyticsReporter.UIButton.Export,
                    AnalyticsReporter.BeginAnalytic());
            };

            Profiler.BeginSample("Views Creation");

            m_ViewManager.Create(rules, m_ViewStates, null, this);

            InitializeTabs(!initialize);

            InitializeViewSelection(!initialize);

            var summary = m_ViewManager.GetView(IssueCategory.Metadata);
            if (summary is SummaryView summaryView)
                summaryView.m_ProjectAuditorWindow = this;

            Profiler.EndSample();
        }

        void InitializeTabs(bool reload)
        {
            if (!reload)
                m_ActiveTabIndex = 0;

            foreach (var tab in m_Tabs)
            {
                RefreshTabCategories(tab, reload);
            }
        }

        void RefreshTabCategories(Tab tab, bool reload)
        {
            if (!reload)
                tab.currentCategoryIndex = 0;
        }

        void SyncTabOnViewChange(IssueCategory newCategory)
        {
            for (int tabIndex = 0; tabIndex < m_Tabs.Length; ++tabIndex)
            {
                for (int categoryIndex = 0; categoryIndex < m_Tabs[tabIndex].categories.Length; ++categoryIndex)
                {
                    if (m_Tabs[tabIndex].categories[categoryIndex] == newCategory)
                    {
                        m_ActiveTabIndex = tabIndex;
                        m_Tabs[m_ActiveTabIndex].currentCategoryIndex = categoryIndex;
                        return;
                    }
                }
            }
        }

        internal void GotoNonAnalyzedCategory(IssueCategory category)
        {
            m_ViewSelectionTreeView.SelectNonAnalyzedCategory(category);
        }

        public void OnSelectedNonAnalyzedTab(Tab selectedTab)
        {
            foreach (var tab in m_Tabs)
            {
                // If this tab's categories haven't been analyzed, override view with analyze info/button
                if (tab == selectedTab)
                {
                    bool hasAnyAnalyzedCategory = false;
                    foreach (var cat in tab.categories)
                    {
                        if (m_Report.HasCategory(cat))
                        {
                            hasAnyAnalyzedCategory = true;
                            break;
                        }
                    }

                    if (!hasAnyAnalyzedCategory)
                    {
                        // Change view anyway, even if overridden, to get into a proper view state, not the previous view
                        m_ViewManager.ChangeView(tab.categories[0]);

                        // Override view to show info and analyze button
                        m_IsNonAnalyzedViewSelected = true;
                        m_SelectedNonAnalyzedTab = selectedTab;
                    }
                }
            }
        }

        void OnDisable()
        {
            AutosaveReport();
            // Make sure 'dirty' scriptable objects are saved to their corresponding assets
            AssetDatabase.SaveAssets();

            m_ViewManager?.SaveSettings();
        }

        void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                DrawToolbar();

                if (m_AnalysisState != AnalysisState.Initializing && m_AnalysisState != AnalysisState.Initialized)
                {
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawViewSelection();
                    if (IsAnalysisValid())
                    {
                        if (!m_IsNonAnalyzedViewSelected)
                        {
                            using (new EditorGUILayout.VerticalScope())
                            {
                                DrawPanels();

                                if (m_ViewManager.GetActiveView().Desc.Category != IssueCategory.Metadata)
                                {
                                    DrawStatusBar();
                                }
                            }
                        }
                        else
                        {
                            DrawAnalysisPanel();
                        }
                    }
                    else
                    {
                        DrawHome();
                    }
                }
            }
        }

        // Draw the panel that appears when you click on a tab that has not yet been analyzed.
        void DrawAnalysisPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandHeight(true)))
            {
                var tabName = m_SelectedNonAnalyzedTab.name;

                using (new EditorGUILayout.HorizontalScope())
                {
                    var info = string.Format(Contents.AnalyzeInfoText, tabName);

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.HelpBox(info, MessageType.Info);
                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(10);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(string.Format(Contents.AnalyzeButtonText, tabName), GUILayout.Width(200)))
                    {
                        var categories = GetTabCategories(m_SelectedNonAnalyzedTab);
                        AuditCategories(categories.ToArray());

                        m_ViewSelectionTreeView.Reload();
                        m_ViewSelectionTreeView.SelectItemByCategory(categories[0]);

                        m_IsNonAnalyzedViewSelected = false;
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        void InitializeViewSelection(bool reload)
        {
            if (!reload)
                m_ViewSelectionTreeState = null;

            m_ViewSelectionTreeView = null;
            m_IsNonAnalyzedViewSelected = false;
        }

        void DrawViewSelection()
        {
            using (new EditorGUI.DisabledScope(m_AnalysisState != AnalysisState.Valid))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    if (m_ViewSelectionTreeState == null)
                    {
                        m_ViewSelectionTreeState = new TreeViewState();
                    }

                    if (m_ViewSelectionTreeView == null)
                    {
                        m_ViewSelectionTreeView = new ViewSelectionTreeView(m_ViewSelectionTreeState, m_Tabs,
                            m_ViewManager,
                            m_Report);

                        m_ViewSelectionTreeView.OnSelectedNonAnalyzedTab += OnSelectedNonAnalyzedTab;
                    }

                    var rect = EditorGUILayout.GetControlRect(GUILayout.Width(180), GUILayout.ExpandHeight(true));

                    m_ViewSelectionTreeView.OnGUI(rect);
                }
            }
        }


        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Metadata,
                DisplayName = "Summary",
                MenuOrder = -1,
                ShowInfoPanel = false,
                Type = typeof(SummaryView),
                ShowAssemblySelection = true,
                GetAssemblyName = issue => issue.GetCustomProperty(CodeProperty.Assembly),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Summary
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.AssetIssue,
                DisplayName = "Asset Issues",
                MenuLabel = "Assets/Issues",
                MenuOrder = 1,
                DescriptionWithIcon = true,
                ShowDependencyView = true,
                ShowFilters = true,
                ShowInfoPanel = true,
                DependencyViewGuiContent = new GUIContent("Asset Dependencies"),
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Assets,
                Type = typeof(DiagnosticView),
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Shader,
                DisplayName = "Shaders",
                MenuOrder = 1,
                MenuLabel = "Assets/Shaders/Shaders",
                DescriptionWithIcon = true,
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
                MenuLabel = "Assets/Shaders/Materials",
                MenuOrder = 2,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Materials
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.ShaderCompilerMessage,
                DisplayName = "Compiler Messages",
                MenuLabel = "Assets/Shaders/Compiler Messages",
                MenuOrder = 4,
                DescriptionWithIcon = true,
                OnOpenIssue = EditorInterop.OpenTextFile<Shader>,
                Type = typeof(ShaderCompilerMessagesView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.ShaderCompilerMessages
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.ShaderVariant,
                DisplayName = "Shader Variants",
                MenuOrder = 3,
                MenuLabel = "Assets/Shaders/Variants",
                ShowFilters = true,
                ShowInfoPanel = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                OnDrawToolbar = (viewManager) =>
                {
                    GUILayout.FlexibleSpace();

                    AnalysisView.DrawToolbarButton(Contents.Refresh, () => Instance.AnalyzeShaderVariants());
                    AnalysisView.DrawToolbarButton(Contents.Clear, () => Instance.ClearShaderVariants());

                    GUILayout.FlexibleSpace();
                },
                Type = typeof(ShaderVariantsView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.ShaderVariants
            });

            /*ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.ComputeShaderVariant,
                DisplayName = "Compute Shader Variants",
                MenuOrder = 3,
                MenuLabel = "Assets/Shaders/Compute Variants",
                ShowFilters = true,
                ShowInfoPanel = true,
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
                MenuLabel = "Project/Packages/Installed",
                MenuOrder = 105,
                OnOpenIssue = EditorInterop.OpenPackage,
                ShowDependencyView = true,
                DependencyViewGuiContent = new GUIContent("Package Dependencies"),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Packages
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.AudioClip,
                DisplayName = "Audio Clips",
                MenuLabel = "Assets/Audio Clips",
                MenuOrder = 107,
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.AudioClip
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Mesh,
                DisplayName = "Meshes",
                MenuLabel = "Assets/Meshes/Meshes",
                MenuOrder = 7,
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Meshes
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Texture,
                DisplayName = "Textures",
                MenuLabel = "Assets/Textures/Textures",
                MenuOrder = 6,
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Textures
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.SpriteAtlas,
                DisplayName = "Sprite Atlases",
                MenuLabel = "Assets/Sprite Atlases/Sprite Atlases",
                MenuOrder = 12,
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.SpriteAtlases
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.AnimatorController,
                DisplayName = "Animator Controllers",
                MenuLabel = "Assets/Animation/Animator Controllers",
                MenuOrder = 8,
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.AnimatorControllers
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.AnimationClip,
                DisplayName = "Animation Clips",
                MenuLabel = "Assets/Animation/Animation Clips",
                MenuOrder = 9,
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.AnimationClips
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Avatar,
                DisplayName = "Avatars",
                MenuLabel = "Assets/Animation/Avatars",
                MenuOrder = 10,
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Avatars
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.AvatarMask,
                DisplayName = "Avatar Masks",
                MenuLabel = "Assets/Animation/Avatar Masks",
                MenuOrder = 11,
                DescriptionWithIcon = true,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.AvatarMasks
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.PrecompiledAssembly,
                DisplayName = "Precompiled Assemblies",
                MenuLabel = "Experimental/Precompiled Assemblies",
                MenuOrder = 91,
                ShowFilters = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.PrecompiledAssemblies
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Assembly,
                DisplayName = "Assemblies",
                MenuLabel = "Code/Assemblies",
                MenuOrder = 98,
                ShowFilters = true,
                ShowDependencyView = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.Assemblies
            });
            int assemblyProperty = Convert.ToInt32(CodeProperty.Assembly);
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.Code,
                DisplayName = "Code",
                MenuLabel = "Code/Issues",
                MenuOrder = 0,
                ShowAssemblySelection = true,
                ShowDependencyView = true,
                ShowFilters = true,
                ShowInfoPanel = true,
                DependencyViewGuiContent = new GUIContent("Inverted Call Hierarchy", "Expand the tree to see all of the methods which lead to the call site of a selected issue."),
                GetAssemblyName = issue => issue.GetCustomProperty(assemblyProperty),
                OnOpenIssue = EditorInterop.OpenTextFile<TextAsset>,
                OnOpenManual = EditorInterop.OpenCodeDescriptor,
                Type = typeof(CodeDiagnosticView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.ApiCalls
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.CodeCompilerMessage,
                DisplayName = "Compiler Messages",
                MenuOrder = 98,
                MenuLabel = "Code/C# Compiler Messages",
                ShowFilters = true,
                ShowInfoPanel = true,
                OnOpenIssue = EditorInterop.OpenTextFile<TextAsset>,
                OnOpenManual = EditorInterop.OpenCompilerMessageDescriptor,
                Type = typeof(CompilerMessagesView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.CodeCompilerMessages
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.ProjectSetting,
                DisplayName = "Project Settings",
                MenuLabel = "Project/Settings/Issues",
                MenuOrder = 1,
                ShowFilters = true,
                ShowInfoPanel = true,
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
                MenuLabel = "Build Report/Steps",
                MenuOrder = 100,
                ShowFilters = true,
                ShowInfoPanel = true,
                Type = typeof(BuildStepsView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.BuildSteps
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.BuildFile,
                DisplayName = "Build Size",
                MenuLabel = "Build Report/Size",
                MenuOrder = 101,
                DescriptionWithIcon = true,
                ShowFilters = true,
                ShowInfoPanel = true,
                ShowAdditionalInfoPanel = true,
                OnOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                Type = typeof(BuildSizeView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.BuildFiles
            });
            int domainReloadAssemblyProperty = Convert.ToInt32(CompilerMessageProperty.Assembly);
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = IssueCategory.DomainReload,
                DisplayName = "Domain Reload",
                MenuLabel = "Code/Domain Reload",
                MenuOrder = 50,
                ShowAssemblySelection = true,
                ShowFilters = true,
                ShowInfoPanel = true,
                GetAssemblyName = issue => issue.GetCustomProperty(domainReloadAssemblyProperty),
                OnOpenIssue = EditorInterop.OpenTextFile<TextAsset>,
                OnOpenManual = EditorInterop.OpenCodeDescriptor,
                Type = typeof(CodeDomainReloadView),
                AnalyticsEventId = (int)AnalyticsReporter.UIButton.DomainReload
            });
        }

        bool IsAnalysisValid()
        {
            return m_AnalysisState != AnalysisState.Initializing && m_AnalysisState != AnalysisState.Initialized && m_AnalysisState != AnalysisState.InProgress;
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
                CompilationMode = GetSelectedCompilationMode(),

                OnIncomingIssues = issues =>
                {
                    // add batch of issues
                    m_ViewManager.AddIssues(issues.ToList());
                },
                OnCompleted = report =>
                {
                    if (!report.IsValid())
                    {
                        m_AnalysisState = AnalysisState.Initialized;
                        return;
                    }
                    m_ViewManager.OnAnalysisCompleted(report);

                    m_ShouldRefresh = true;
                    m_AnalysisState = AnalysisState.Completed;

                    m_Report = report;
                    m_Report.DisplayName = reportDisplayName;
                    m_Report.NeedsSaving = true;

                    EditorApplication.delayCall += AutosaveReport;

                    InitializeViewSelection(true);
                }
            };

            InitializeViews(analysisParams.Rules, false);

            m_ProjectAuditor.AuditAsync(analysisParams, new ProgressBar());
        }

        void Update()
        {
            if (m_ShouldRefresh)
                Repaint();
            if (m_AnalysisState == AnalysisState.InProgress)
                Repaint();
        }

        internal void AuditCategories(IssueCategory[] categories)
        {
            if (m_ProjectAuditor == null)
                m_ProjectAuditor = new ProjectAuditor();

            // a module might report more categories than requested so we need to make sure we clean up the views accordingly
            var modules = categories.SelectMany(m_ProjectAuditor.GetModules).ToArray();
            var actualCategories = modules.SelectMany(m => m.Categories).Distinct().ToArray();

            var views = actualCategories
                .Select(c => m_ViewManager.GetView(c))
                .Where(v => v != null)
                .ToArray();

            foreach (var view in views)
            {
                view.Clear();
            }

            var analysisParams = new AnalysisParams
            {
                Categories = actualCategories.ToSerializableArray(),
                Platform = m_Report.SessionInfo.Platform,
                CompilationMode = GetSelectedCompilationMode(),
                ExistingReport = m_Report,
                OnIncomingIssues = issues =>
                {
                    foreach (var view in views)
                    {
                        view.AddIssues(issues);
                    }
                },
                OnCompleted = report =>
                {
                    if (!report.IsValid())
                    {
                        m_AnalysisState = AnalysisState.Initialized;
                        return;
                    }
                    m_ViewManager.OnAnalysisCompleted(report);

                    m_ShouldRefresh = true;
                    m_AnalysisState = AnalysisState.Completed;

                    m_Report.NeedsSaving = true;

                    EditorApplication.delayCall += AutosaveReport;
                }
            };

            m_ProjectAuditor.Audit(analysisParams, new ProgressBar());

            var summaryView = m_ViewManager.GetView(IssueCategory.Metadata);
            if (summaryView != null)
            {
                summaryView.Clear();
                summaryView.AddIssues(m_Report.GetAllIssues());
            }
        }

        public void AnalyzeShaderVariants()
        {
            AuditCategories(GetTabCategories(TabId.Shaders));
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

        CompilationMode GetSelectedCompilationMode()
        {
            return UserPreferences.CompilationMode;
        }

        IssueCategory[] GetSelectedCategories()
        {
            var selectedCategories = UserPreferences.ProjectAreasToAnalyze;

            var requestedCategories = new List<IssueCategory>(new[] { IssueCategory.Metadata });
            ProjectAreaFlags categories = selectedCategories;
            if (categories.HasFlag(ProjectAreaFlags.Code))
                requestedCategories.AddRange(GetTabCategories(TabId.Code));
            if (categories.HasFlag(ProjectAreaFlags.ProjectSettings))
                requestedCategories.AddRange(GetTabCategories(TabId.Settings));
            if (categories.HasFlag(ProjectAreaFlags.Assets))
                requestedCategories.AddRange(GetTabCategories(TabId.Assets));
            if (categories.HasFlag(ProjectAreaFlags.Shaders))
                requestedCategories.AddRange(GetTabCategories(TabId.Shaders));
            if (categories.HasFlag(ProjectAreaFlags.Build))
                requestedCategories.AddRange(GetTabCategories(TabId.Build));

            return requestedCategories.ToArray();
        }

        IssueCategory[] GetTabCategories(TabId tabId)
        {
            return GetTabCategories(m_Tabs.First(t => t.id == tabId));
        }

        IssueCategory[] GetTabCategories(Tab tab)
        {
            return tab.categories.ToValuesArray();
        }

        void DrawAssemblyFilter()
        {
            if (!activeView.Desc.ShowAssemblySelection)
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Contents.AssemblyFilter, GUILayout.Width(LayoutSize.FilterOptionsLeftLabelWidth));

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
                                        payload["numUnityAssemblies"] = selectedAsmNames.Count(assemblyName => assemblyName.Contains("Unity")).ToString();

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
                EditorGUILayout.LabelField(Contents.AreaFilter,
                    GUILayout.Width(LayoutSize.FilterOptionsLeftLabelWidth));

                if (AreaNames.Length > 0)
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
                                    AreaNames, selection =>
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
                    Utility.DrawSelectedText(m_AreaSelectionSummary);

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

                    EditorGUI.indentLevel--;
                }
            }
        }

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


                    EditorGUILayout.Space(k_SpacingHeight);


                    var projectAreas = UserPreferences.ProjectAreasToAnalyze;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        const int k_ButtonWidth = 140;

                        // Preferences button
                        if (GUILayout.Button("Preferences", SharedStyles.LinkLabel, GUILayout.Width(k_ButtonWidth), GUILayout.Height(30)))
                        {
                            EditorInterop.OpenProjectAuditorPreferences();
                        }

                        // Analyze button
                        using (new EditorGUI.DisabledScope((m_AnalysisState == AnalysisState.InProgress) || (ProjectAuditorRulesPackage.IsInstalled == false) || (RulesPackageInstallRequest != null)))
                        {
                            var content = ProjectAuditorRulesPackage.IsInstalled ? Contents.AnalyzeButton : Contents.AnalyzeButtonDisabled;
                            if (GUILayout.Button(content, GUILayout.Width(k_ButtonWidth), GUILayout.Height(30)))
                            {
                                if (projectAreas == ProjectAreaFlags.None)
                                {
                                    if (EditorUtility.DisplayDialog(k_EnableAreas, k_EnableAreasQuestion, "Ok", "Cancel"))
                                    {
                                        UserPreferences.ProjectAreasToAnalyze.Set(ProjectAreaFlags.All);
                                        Analyze();
                                        GUIUtility.ExitGUI();
                                    }
                                }
                                else
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

                        GUILayout.FlexibleSpace();
                    }

                    EditorGUILayout.Space(k_SpacingHeight);

                    DrawHorizontalLine();

                    GUILayout.FlexibleSpace();
                    GUILayout.FlexibleSpace();
                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndVertical();
        }

        void DrawHorizontalLine()
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(1));

            if (m_Draw2D.DrawStart(rect))
            {
                m_Draw2D.DrawLine(0, 0, rect.width, 0, Color.black);
                m_Draw2D.DrawEnd();
            }
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

                    if (activeView != null && activeView.Desc.Category == IssueCategory.Metadata && m_Report != null)
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

                activeView.DrawContent(true);
            }
        }

        internal void SetAreaSelection(TreeViewSelection selection)
        {
            var selectedStrings = selection.GetSelectedStrings(AreaNames, true);

            m_SelectedAreas = Areas.None;
            foreach (var areaString in selectedStrings)
            {
                m_SelectedAreas |= (Areas)Enum.Parse(typeof(Areas), areaString);
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
                        m_AreaSelection.SetAll(AreaNames);
                        m_SelectedAreas = Areas.All;
                    }
                    else if (m_AreaSelectionSummary != "None")
                    {
                        var areas = Formatting.SplitStrings(m_AreaSelectionSummary);
                        m_AreaSelection.selection.AddRange(areas);
                        m_SelectedAreas = (Areas)Enum.Parse(typeof(Areas), m_AreaSelectionSummary);
                    }
                }
                else
                {
                    m_AreaSelection.SetAll(AreaNames);
                    m_SelectedAreas = Areas.All;
                }
            }
        }

        void UpdateAssemblyNames()
        {
            if (m_Report == null)
                return;
            // update list of assembly names
            if (UserPreferences.AnalyzePackagesForIssues)
            {
                var assemblyNames = m_Report.FindByCategory(IssueCategory.Assembly).Select(i => i.Description)
                    .ToArray();
                m_AssemblyNames = assemblyNames.Distinct().OrderBy(str => str).ToArray();
            }
            else
            {
                var assemblyNames = m_Report.FindByCategory(IssueCategory.Assembly).Select(i => i.Description)
                    .ToArray();
                string[] allAssemblies = assemblyNames.Distinct().OrderBy(str => str).ToArray();

                if (m_Report.IsForCurrentProject())
                    m_AssemblyNames = allAssemblies.Where(a => !AssemblyInfoProvider.IsPackageAssembly(a)).ToArray();
                else
                    m_AssemblyNames = allAssemblies;
            }
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
                    var assemblies = Formatting.SplitStrings(m_AssemblySelectionSummary)
                        .Where(assemblyName => m_AssemblyNames.Contains(assemblyName));
                    m_AssemblySelection.selection.AddRange(assemblies);
                }
            }

            if (m_Report.IsForCurrentProject())
            {
                if (forceRefresh || !m_AssemblySelection.selection.Any())
                {
                    // initial selection setup:
                    // - assemblies from user scripts or editable packages, or
                    // - default assembly, or,
                    // - all generated assemblies
                    IEnumerable<string> compiledAssemblies = null;

                    if (UserPreferences.AnalyzePackagesForIssues)
                        compiledAssemblies = m_AssemblyNames.Where(a => !AssemblyInfoProvider.IsUnityEngineAssembly(a));
                    else
                        compiledAssemblies = m_AssemblyNames.Where(a => !AssemblyInfoProvider.IsPackageAssembly(a));

                    compiledAssemblies = compiledAssemblies.Where(a =>
                        !AssemblyInfoProvider.IsReadOnlyAssembly(a)).ToArray();
                    m_AssemblySelection.selection.AddRange(compiledAssemblies);

                    if (m_DefaultAssemblies == null)
                        m_DefaultAssemblies = new List<string>(m_AssemblySelection.selection.Count);

                    m_DefaultAssemblies.AddRange(m_AssemblySelection.selection);

                    if (!m_AssemblySelection.selection.Any())
                    {
                        if (m_AssemblyNames.Contains(AssemblyInfo.DefaultAssemblyName))
                            m_AssemblySelection.Set(AssemblyInfo.DefaultAssemblyName);
                        else
                            m_AssemblySelection.SetAll(m_AssemblyNames);
                    }
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
                {
                    GUILayout.Label(Utility.GetIcon(Utility.IconType.StatusWheel), SharedStyles.IconLabel, GUILayout.Width(AnalysisView.ToolbarIconSize));
                }

                GUILayout.FlexibleSpace();

                // right-end buttons
                const int discardButtonWidth = 110;
                const int loadSaveButtonWidth = 40;

                using (new EditorGUI.DisabledScope(m_AnalysisState != AnalysisState.Valid))
                {
                    if (GUILayout.Button(Contents.DiscardButton, EditorStyles.toolbarButton, GUILayout.Width(discardButtonWidth)))
                    {
                        if (!m_Report.NeedsSaving ||
                            EditorUtility.DisplayDialog(k_Discard, k_DiscardQuestion, "Discard", "Cancel"))
                        {
                            m_ActiveTabIndex = 0;
                            m_AnalysisState = AnalysisState.Initialized;
                            // Reset the auditor so that the module analysers can be reinitialised for a new analysis.
                            // The list of Descriptors in DescriptorLibrary gets overwritten when loading in a saved
                            // analysis; not a problem for the loaded analysis, but new analyses will be generated with data
                            // missing, since not all of the DescriptorLibrary is written out to json (DescriptorJsonConverter).
                            // Plus, we might update its content in future versions and we don't want to mix that with
                            // data from an old capture. COPT-3262
                            // TODO: Does DescriptorLibrary need to get written out at all? Seems like all the relevant info is stored in the issues list...
                            m_ProjectAuditor = null;

                            DeleteAutosave();
                        }
                    }
                }

                using (new EditorGUI.DisabledScope(!ProjectAuditorRulesPackage.IsInstalled))
                {
                    var loadContent = ProjectAuditorRulesPackage.IsInstalled ? Contents.LoadButton : Contents.LoadButtonDisabled;
                    if (GUILayout.Button(loadContent, EditorStyles.toolbarButton, GUILayout.Width(loadSaveButtonWidth)))
                    {
                        LoadReport();
                    }
                }

                using (new EditorGUI.DisabledScope(m_AnalysisState != AnalysisState.Valid))
                {
                    if (GUILayout.Button(Contents.SaveButton, EditorStyles.toolbarButton,
                        GUILayout.Width(loadSaveButtonWidth)))
                    {
                        SaveReport();
                    }
                }

                Utility.DrawHelpButton(Contents.HelpButton, Documentation.GetPageUrl("index"));
            }
        }

        void DrawTabs()
        {
            int tabToAudit = -1;

            using (new EditorGUILayout.VerticalScope(SharedStyles.TabBackground))
            {
                const int tabButtonWidth = 80;
                const int tabButtonHeight = 27;

                EditorGUILayout.BeginHorizontal(SharedStyles.TabBackground);

                for (var i = 0; i < m_Tabs.Length; i++)
                {
                    GUI.enabled = m_Report != null;

                    if (DrawTabButton(new GUIContent(m_Tabs[i].name), m_ActiveTabIndex == i,
                        tabButtonWidth, tabButtonHeight))
                    {
                        var tab = m_Tabs[i];

                        var hasAnyCategories = false;
                        foreach (var category in tab.categories)
                        {
                            if (m_Report.HasCategory(category))
                            {
                                hasAnyCategories = true;
                            }
                        }

                        if (!hasAnyCategories)
                        {
                            tabToAudit = i;
                        }
                        else
                        {
                            if (tab.categories.Length > tab.currentCategoryIndex)
                                m_ViewManager.ChangeView(tab.categories[tab.currentCategoryIndex]);
                        }
                    }

                    GUI.enabled = true;
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            if (tabToAudit != -1)
            {
                var tab = m_Tabs[tabToAudit];

                // Change view anyway, even if overridden, to get into a proper view state, not the previous view
                m_ViewManager.ChangeView(tab.categories[0]);

                // Override view to show info and analyze button
                m_IsNonAnalyzedViewSelected = true;
                m_SelectedNonAnalyzedTab = tab;
            }
        }

        bool DrawTabButton(GUIContent content, bool isActive, int width, int height)
        {
            EditorGUILayout.BeginVertical();

            bool wasButtonClicked = GUILayout.Button(content, SharedStyles.TabButton, GUILayout.Width(width), GUILayout.Height(height));
            int id = GUIUtility.GetControlID(content, FocusType.Passive);
            var isHoverState = GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.MouseMove)
            {
                if (isHoverState)
                {
                    if (m_TabButtonControlID != id)
                    {
                        m_TabButtonControlID = id;
                        if (mouseOverWindow != null)
                        {
                            mouseOverWindow.Repaint();
                        }
                    }
                }
                else
                {
                    if (m_TabButtonControlID == id)
                    {
                        m_TabButtonControlID = 0;
                        if (mouseOverWindow != null)
                        {
                            mouseOverWindow.Repaint();
                        }
                    }
                }
            }

            var rect = EditorGUILayout.GetControlRect(false, 3, SharedStyles.TabBackground, GUILayout.Width(width), GUILayout.Height(2));
            if ((isActive || isHoverState) && m_Draw2D.DrawStart(rect))
            {
                var color = isActive ? SharedStyles.TabBottomActiveColor : SharedStyles.TabBottomHoverColor;

                m_Draw2D.DrawFilledBox(0, 0, width, 2.5f, color);
                m_Draw2D.DrawEnd();
            }

            EditorGUILayout.EndVertical();

            return wasButtonClicked;
        }

        void SaveReport()
        {
            // Avoid unsupported save name characters from the report's displayname (project name)
            var invalidChars = Path.GetInvalidFileNameChars();
            var reportDisplayName = new StringBuilder(m_Report.DisplayName);
            foreach (var c in invalidChars)
            {
                reportDisplayName.Replace(c, '_');
            }

            var path = EditorUtility.SaveFilePanel(k_SaveToFile, UserPreferences.LoadSavePath, reportDisplayName.ToString(), "projectauditor");
            if (path.Length != 0)
            {
                m_Report.NeedsSaving = false;
                m_Report.DisplayName = Path.GetFileNameWithoutExtension(path);

                m_Report.Save(path);
                AutosaveReport();

                UserPreferences.LoadSavePath = Path.GetDirectoryName(path);

                EditorUtility.RevealInFinder(path);
                AnalyticsReporter.SendEvent(AnalyticsReporter.UIButton.Save, AnalyticsReporter.BeginAnalytic());
            }

            GUIUtility.ExitGUI();
        }

        void LoadReport()
        {
            var path = EditorUtility.OpenFilePanel(k_LoadFromFile, UserPreferences.LoadSavePath, "projectauditor");
            if (path.Length != 0)
            {
                LoadReportFromFile(path);
            }

            GUIUtility.ExitGUI();
        }

        void LoadReportFromFile(string path)
        {
            Report newReport = Report.Load(path, out var errorMessage);
            var fileWasManuallySaved = path != GetAutosaveFilename();

            if (newReport == null)
            {
                if (fileWasManuallySaved)
                    EditorUtility.DisplayDialog(k_LoadFromFile, k_LoadingFailedVersion + "\n" + errorMessage, "Ok");
                else
                    Debug.LogWarning(k_LoadingAutosaveFailedVersion + "\n" + errorMessage);
                return;
            }

            if (newReport.NumTotalIssues == 0)
            {
                if (fileWasManuallySaved)
                    EditorUtility.DisplayDialog(k_LoadFromFile, k_LoadingFailed, "Ok");
                else
                    Debug.LogWarning(k_LoadingAutosaveFailed);
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
            m_ViewManager.ChangeView(IssueCategory.Metadata);
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
                m_Report?.Save(GetAutosaveFilename());
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
        const string k_EnableAreasQuestion =
            "Enable all analysis areas and continue?\n\nAreas can be individually toggled in the Project Auditor section of Preferences.";

        // UI styles and layout
        static class LayoutSize
        {
            public static readonly int MinWindowWidth = 410;
            public static readonly int MinWindowHeight = 640;
            public static readonly int FilterOptionsLeftLabelWidth = 100;
            public static readonly int FilterOptionsEnumWidth = 50;
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
            public static readonly GUIContent DiscardButton = EditorGUIUtility.TrTextContentWithIcon("New Analysis", "Discard the current report and return to the Welcome view.", "Refresh");

            public static readonly GUIContent HelpButton = Utility.GetIcon(Utility.IconType.Help, "Open Manual (in a web browser)");
            public static readonly GUIContent PreferencesMenuItem = EditorGUIUtility.TrTextContent("Preferences", $"Open User Preferences for {ProjectAuditor.DisplayName}");

            public static readonly GUIContent AssemblyFilter =
                new GUIContent("Assembly : ", "Select assemblies to examine");

            public static readonly GUIContent AssemblyFilterSelect =
                new GUIContent("Select", "Select assemblies to examine");

            public static readonly GUIContent AreaFilter =
                new GUIContent("Areas : ", "Select performance areas to display");

            public static readonly GUIContent AreaFilterSelect =
                new GUIContent("Select", "Select performance areas to display");

            public static readonly GUIContent FiltersFoldout = new GUIContent("Filters", "Filtering Criteria");


            public static readonly GUIContent WelcomeTextTitle = new GUIContent($"Welcome to {ProjectAuditor.DisplayName}");

            public static readonly GUIContent WelcomeText = new GUIContent(
                $@"{ProjectAuditor.DisplayName} is a suite of static analysis tools that examine assets, settings and scripts to enable users to optimize their Unity Project. It produces a report that highlights issues in Code and Settings, insights about the latest Build Report, information about Assets, and provides recommendations on how to improve."
            );

            public static readonly GUIContent Clear = new GUIContent("Clear");
            public static readonly GUIContent Refresh = new GUIContent("Refresh");

            public static readonly GUIContent ShaderVariants = new GUIContent("Variants", "Inspect Shader Variants");

            public static readonly string AnalyzeInfoText = "{0} analysis is not yet included in this report. Run analysis now?";
            public static readonly string AnalyzeButtonText = "Start {0} Analysis";


            static Contents()
            {
                UpdateRulesButtonInProgress = new GUIContent[12];
                for (int i = 0; i < 12; i++)
                    UpdateRulesButtonInProgress[i] = EditorGUIUtility.TrTextContentWithIcon(" Installing Rules...", "WaitSpin" + i.ToString("00"));
            }
        }
    }
}
