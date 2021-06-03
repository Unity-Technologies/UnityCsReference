// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace UnityEditorInternal.Profiling
{
    // Used with via Reflection (see MemoryProfilerModuleBridge.cs) from the Profiler Memory Profiler Package for the Memory Profiler Module UI Override.
    internal static class MemoryProfilerOverrides
    {
        static public Func<ProfilerWindow, ProfilerModuleViewController> CreateDetailsViewController = null;
    }

    [Serializable]
    [ProfilerModuleMetadata("Memory", typeof(LocalizationResource), IconPath = "Profiler.Memory")]
    internal class MemoryProfilerModule : ProfilerModuleBase
    {
        class MemoryProfilerModuleViewController : ProfilerModuleViewController
        {
            // adaptation helper. MemoryProfilerModuleViewController is copied over from the memory profiler package which contains this helper
            static class UIElementsHelper
            {
                public static void SetVisibility(VisualElement element, bool visible)
                {
                    element.visible = visible;
                    element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                }

                public static Rect GetRect(VisualElement element)
                {
                    return new Rect(element.LocalToWorld(element.contentRect.position), element.contentRect.size);
                }
            }

            static class ResourcePaths
            {
                public const string MemoryModuleUxmlPath = "Profiler/Modules/Memory/MemoryModule.uxml";
            }
            static class Content
            {
                public static readonly string NoFrameDataAvailable = L10n.Tr("No frame data available. Select a frame from the charts above to see its details here.");
                public static readonly string Textures = L10n.Tr("Textures");
                public static readonly string Meshes = L10n.Tr("Meshes");
                public static readonly string Materials = L10n.Tr("Materials");
                public static readonly string AnimationClips = L10n.Tr("Animation Clips");
                public static readonly string Assets = L10n.Tr("Assets");
                public static readonly string GameObjects = L10n.Tr("Game Objects");
                public static readonly string SceneObjects = L10n.Tr("Scene Objects");
                public static readonly string GCAlloc = L10n.Tr("GC allocated in frame");
            }

            struct ObjectTableRow
            {
                public Label Count;
                public Label Size;
            }

            MemoryProfilerModule m_MemoryModule;
            // all UI Element and similar references should be grouped into this class.
            // Since the things this reference get recreated every time the module is selected, these references shouldn't linger beyond the Dispose()
            UIState m_UIState = null;
            class UIState
            {
                public VisualElement ViewArea;
                public VisualElement NoDataView;
                public VisualElement SimpleView;
                public VisualElement DetailedView;
                public UnityEngine.UIElements.Button DetailedMenu;
                public Label DetailedMenuLabel;
                public UnityEngine.UIElements.Button InstallPackageButton;
                public VisualElement EditorWarningLabel;
                public VisualElement DetailedToolbarSection;
                public MemoryUsageBreakdown TopLevelBreakdown;
                public MemoryUsageBreakdown Breakdown;
                // if no memory counter data is available (i.e. the recording is from a pre 2020.2 Unity version) this whole section can't be populated with info
                public VisualElement CounterBasedUI;
                public TextField Text;

                public ObjectTableRow TexturesRow;
                public ObjectTableRow MeshesRow;
                public ObjectTableRow MaterialsRow;
                public ObjectTableRow AnimationClipsRow;
                public ObjectTableRow AssetsRow;
                public ObjectTableRow GameObjectsRow;
                public ObjectTableRow SceneObjectsRow;
                public ObjectTableRow GCAllocRow;
            }

            ulong[] m_Used = new ulong[6];
            ulong[] m_Reserved = new ulong[6];

            ulong[] m_TotalUsed = new ulong[1];
            ulong[] m_TotalReserved = new ulong[1];
            StringBuilder m_SimplePaneStringBuilder = new StringBuilder(1024);

            bool m_AddedSimpleDataView = false;


            ulong m_MaxSystemUsedMemory = 0;

            IConnectionState m_ConnectionState;

            bool m_InitiatedPackageSearchQuery;

            public MemoryProfilerModuleViewController(ProfilerWindow profilerWindow, MemoryProfilerModule memoryModule) : base(profilerWindow)
            {
                profilerWindow.SelectedFrameIndexChanged += UpdateContent;
                m_MemoryModule = memoryModule;
                if (!m_MemoryModule.InitiateMemoryProfilerPackageAvailabilityCheck())
                {
                    m_InitiatedPackageSearchQuery = true;
                }
            }

            int[] oneFrameAvailabilityBuffer = new int[1];

            bool CheckMemoryStatsAvailablity(long frameIndex)
            {
                var dataNotAvailable = frameIndex < 0 || frameIndex<ProfilerWindow.firstAvailableFrameIndex || frameIndex> ProfilerWindow.lastAvailableFrameIndex;
                if (!dataNotAvailable)
                {
                    ProfilerDriver.GetStatisticsAvailable(UnityEngine.Profiling.ProfilerArea.Memory, (int)frameIndex, oneFrameAvailabilityBuffer);
                    if (oneFrameAvailabilityBuffer[0] == 0)
                        dataNotAvailable = true;
                }
                return !dataNotAvailable;
            }

            void ViewChanged(ProfilerMemoryView view)
            {
                m_UIState.ViewArea.Clear();
                m_MemoryModule.m_ShowDetailedMemoryPane = view;
                if (view == ProfilerMemoryView.Simple)
                {
                    var frameIndex = ProfilerWindow.selectedFrameIndex;
                    var dataAvailable = CheckMemoryStatsAvailablity(frameIndex);
                    m_UIState.DetailedMenuLabel.text = "Simple";

                    UIElementsHelper.SetVisibility(m_UIState.DetailedToolbarSection, false);
                    if (dataAvailable)
                    {
                        m_UIState.ViewArea.Add(m_UIState.SimpleView);
                        m_AddedSimpleDataView = true;
                        UpdateContent(frameIndex);
                    }
                    else
                    {
                        m_UIState.ViewArea.Add(m_UIState.NoDataView);
                        m_AddedSimpleDataView = false;
                    }
                }
                else
                {
                    m_UIState.DetailedMenuLabel.text = "Detailed";
                    UIElementsHelper.SetVisibility(m_UIState.DetailedToolbarSection, true);
                    // Detailed View doesn't differentiate between there being frame data or not because
                    // 1. Clear doesn't clear out old snapshots so there totally could be data here
                    // 2. Take Snapshot also doesn't require there to be any frame data
                    // this special case will disappear together with the detailed view eventually
                    m_UIState.ViewArea.Add(m_UIState.DetailedView);
                    m_AddedSimpleDataView = false;
                }
            }

            static ProfilerMarker s_UpdateMaxSystemUsedMemoryProfilerMarker = new ProfilerMarker("MemoryProfilerModule.UpdateMaxSystemUsedMemory");
            float[] m_CachedArray;
            // Total System Memory Used
            void UpdateMaxSystemUsedMemory(long firstFrameToCheck, long lastFrameToCheck)
            {
                s_UpdateMaxSystemUsedMemoryProfilerMarker.Begin();
                var frameCountToCheck = lastFrameToCheck - firstFrameToCheck;
                m_MaxSystemUsedMemory = 0;
                var max = m_MaxSystemUsedMemory;
                // try to reuse the array if possible
                if (m_CachedArray == null || m_CachedArray.Length != frameCountToCheck)
                    m_CachedArray = new float[frameCountToCheck];
                float maxValueInRange;
                ProfilerDriver.GetCounterValuesBatch(UnityEngine.Profiling.ProfilerArea.Memory, "System Used Memory", (int)firstFrameToCheck, 1, m_CachedArray, out maxValueInRange);
                if (maxValueInRange > max)
                    max = (ulong)maxValueInRange;
                m_MaxSystemUsedMemory = max;
                s_UpdateMaxSystemUsedMemoryProfilerMarker.End();
            }

            void UpdateContent(long frame)
            {
                if (m_MemoryModule.m_ShowDetailedMemoryPane != ProfilerMemoryView.Simple)
                    return;
                var dataAvailable = CheckMemoryStatsAvailablity(frame);

                if (m_AddedSimpleDataView != dataAvailable)
                {
                    // refresh the view structure
                    ViewChanged(ProfilerMemoryView.Simple);
                    return;
                }
                if (!dataAvailable)
                    return;
                if (m_UIState != null)
                {
                    using (var data = ProfilerDriver.GetRawFrameDataView((int)frame, 0))
                    {
                        m_SimplePaneStringBuilder.Clear();
                        if (data.valid && data.GetMarkerId("Total Reserved Memory") != FrameDataView.invalidMarkerId)
                        {
                            var systemUsedMemoryId = data.GetMarkerId("System Used Memory");

                            var systemUsedMemory = (ulong)data.GetCounterValueAsLong(systemUsedMemoryId);

                            var systemUsedMemoryIsKnown = (systemUsedMemoryId != FrameDataView.invalidMarkerId && systemUsedMemory > 0);

                            var maxSystemUsedMemory = m_MaxSystemUsedMemory = systemUsedMemory;
                            if (!m_MemoryModule.m_Normalized)
                            {
                                UpdateMaxSystemUsedMemory(ProfilerWindow.firstAvailableFrameIndex, ProfilerWindow.lastAvailableFrameIndex);
                                maxSystemUsedMemory = m_MaxSystemUsedMemory;
                            }

                            var totalUsedId = data.GetMarkerId("Total Used Memory");
                            var totalUsed = (ulong)data.GetCounterValueAsLong(totalUsedId);
                            var totalReservedId = data.GetMarkerId("Total Reserved Memory");
                            var totalReserved = (ulong)data.GetCounterValueAsLong(totalReservedId);

                            m_TotalUsed[0] = totalUsed;
                            m_TotalReserved[0] = totalReserved;

                            if (!systemUsedMemoryIsKnown)
                                systemUsedMemory = totalReserved;

                            m_UIState.TopLevelBreakdown.SetVaules(systemUsedMemory, m_TotalReserved, m_TotalUsed, m_MemoryModule.m_Normalized, maxSystemUsedMemory, systemUsedMemoryIsKnown);

                            m_Used[4] = totalUsed;
                            m_Reserved[4] = totalReserved;

                            var gfxReservedId = data.GetMarkerId("Gfx Reserved Memory");
                            m_Reserved[1] = m_Used[1] = (ulong)data.GetCounterValueAsLong(gfxReservedId);

                            var managedUsedId = data.GetMarkerId("GC Used Memory");
                            m_Used[0] = (ulong)data.GetCounterValueAsLong(managedUsedId);
                            var managedReservedId = data.GetMarkerId("GC Reserved Memory");
                            m_Reserved[0] = (ulong)data.GetCounterValueAsLong(managedReservedId);

                            var audioReservedId = data.GetMarkerId("Audio Used Memory");
                            m_Reserved[2] = m_Used[2] = (ulong)data.GetCounterValueAsLong(audioReservedId);

                            var videoReservedId = data.GetMarkerId("Video Used Memory");
                            m_Reserved[3] = m_Used[3] = (ulong)data.GetCounterValueAsLong(videoReservedId);


                            var profilerUsedId = data.GetMarkerId("Profiler Used Memory");
                            m_Used[5] = (ulong)data.GetCounterValueAsLong(profilerUsedId);
                            var profilerReservedId = data.GetMarkerId("Profiler Reserved Memory");
                            m_Reserved[5] = (ulong)data.GetCounterValueAsLong(profilerReservedId);

                            m_Used[4] -= Math.Min(m_Used[0] + m_Used[1] + m_Used[2] + m_Used[3] + m_Used[5], m_Used[4]);
                            m_Reserved[4] -= Math.Min(m_Reserved[0] + m_Reserved[1] + m_Reserved[2] + m_Reserved[3] + m_Reserved[5], m_Reserved[4]);
                            m_UIState.Breakdown.SetVaules(systemUsedMemory, m_Reserved, m_Used, m_MemoryModule.m_Normalized, maxSystemUsedMemory, systemUsedMemoryIsKnown);

                            UpdateObjectRow(data, ref m_UIState.TexturesRow, "Texture Count", "Texture Memory");
                            UpdateObjectRow(data, ref m_UIState.MeshesRow, "Mesh Count", "Mesh Memory");
                            UpdateObjectRow(data, ref m_UIState.MaterialsRow, "Material Count", "Material Memory");
                            UpdateObjectRow(data, ref m_UIState.AnimationClipsRow, "AnimationClip Count", "AnimationClip Memory");
                            UpdateObjectRow(data, ref m_UIState.AssetsRow, "Asset Count");
                            UpdateObjectRow(data, ref m_UIState.GameObjectsRow, "Game Object Count");
                            UpdateObjectRow(data, ref m_UIState.SceneObjectsRow, "Scene Object Count");

                            UpdateObjectRow(data, ref m_UIState.GCAllocRow, "GC Allocation In Frame Count", "GC Allocated In Frame");

                            if (!m_UIState.CounterBasedUI.visible)
                                UIElementsHelper.SetVisibility(m_UIState.CounterBasedUI, true);

                            var platformSpecifics = MemoryProfilerModule.GetPlatformSpecificText(data, ProfilerWindow);
                            if (!string.IsNullOrEmpty(platformSpecifics))
                            {
                                m_SimplePaneStringBuilder.Append(platformSpecifics);
                            }
                        }
                        else
                        {
                            if (m_UIState.CounterBasedUI.visible)
                                UIElementsHelper.SetVisibility(m_UIState.CounterBasedUI, false);
                            m_SimplePaneStringBuilder.Append(MemoryProfilerModule.GetSimpleMemoryPaneText(data, ProfilerWindow, false));
                        }

                        if (m_SimplePaneStringBuilder.Length > 0)
                        {
                            UIElementsHelper.SetVisibility(m_UIState.Text, true);
                            m_UIState.Text.value = m_SimplePaneStringBuilder.ToString();
                        }
                        else
                        {
                            UIElementsHelper.SetVisibility(m_UIState.Text, false);
                        }
                    }
                }
            }

            void UpdateObjectRow(RawFrameDataView data, ref ObjectTableRow row, string countMarkerName, string sizeMarkerName = null)
            {
                row.Count.text = data.GetCounterValueAsLong(data.GetMarkerId(countMarkerName)).ToString();
                if (!string.IsNullOrEmpty(sizeMarkerName))
                    row.Size.text = EditorUtility.FormatBytes(data.GetCounterValueAsLong(data.GetMarkerId(sizeMarkerName)));
            }

            void ConnectionChanged(string playerName)
            {
                if (m_ConnectionState != null)
                    UIElementsHelper.SetVisibility(m_UIState.EditorWarningLabel, m_ConnectionState.connectionName == "Editor");
            }

            void CheckPackageAvailabilityStatus()
            {
                if (m_InitiatedPackageSearchQuery && !m_MemoryModule.MemoryProfilerPackageAvailabilityCheckMoveNext())
                {
                    m_InitiatedPackageSearchQuery = false;
                    UpdatePackageInstallButton();
                }
            }

            void UpdatePackageInstallButton()
            {
                if (m_UIState != null)
                {
                    switch (m_MemoryModule.m_MemoryProfilerPackageStage)
                    {
                        case PackageStage.Experimental:
                            m_UIState.InstallPackageButton.text = Styles.experimentalPackageHint.text;
                            break;
                        case PackageStage.PreviewOrReleased:
                            m_UIState.InstallPackageButton.text = Styles.packageInstallSuggestionButton.text;
                            break;
                        case PackageStage.Installed:
                            UIElementsHelper.SetVisibility(m_UIState.InstallPackageButton, false);
                            break;
                        default:
                            break;
                    }
                }
            }

            void PackageInstallButtonClicked()
            {
                switch (m_MemoryModule.m_MemoryProfilerPackageStage)
                {
                    case PackageStage.Experimental:
                        Application.OpenURL(Styles.memoryProfilerPackageDocumentatinURL);
                        break;
                    case PackageStage.PreviewOrReleased:
                        UnityEditor.PackageManager.Client.Add(m_MemoryModule.m_MemoryProfilerPackageName);
                        break;
                    case PackageStage.Installed:
                        break;
                    default:
                        break;
                }
            }

            protected override VisualElement CreateView()
            {
                VisualTreeAsset memoryModuleViewTree = EditorGUIUtility.Load(ResourcePaths.MemoryModuleUxmlPath) as VisualTreeAsset;

                var root = memoryModuleViewTree.CloneTree();

                m_UIState = new UIState();

                var toolbar = root.Q("memory-module__toolbar");
                m_UIState.DetailedToolbarSection = toolbar.Q("memory-module__toolbar__detailed-controls");

                m_UIState.DetailedMenu = toolbar.Q<UnityEngine.UIElements.Button>("memory-module__toolbar__detail-view-menu");
                m_UIState.DetailedMenuLabel = m_UIState.DetailedMenu.Q<Label>("memory-module__toolbar__detail-view-menu__label");
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Simple"), false, () => ViewChanged(ProfilerMemoryView.Simple));
                menu.AddItem(new GUIContent("Detailed"), false, () => ViewChanged(ProfilerMemoryView.Detailed));
                m_UIState.DetailedMenu.clicked += () =>
                {
                    menu.DropDown(UIElementsHelper.GetRect(m_UIState.DetailedMenu));
                };

                var takeCapture = toolbar.Q<UnityEngine.UIElements.Button>("memory-module__toolbar__take-sample-button");
                takeCapture.clicked += () => m_MemoryModule.RefreshMemoryData();

                var gatherObjectReferencesToggle = toolbar.Q<Toggle>("memory-module__toolbar__gather-references-toggle");
                gatherObjectReferencesToggle.RegisterValueChangedCallback((evt) => m_MemoryModule.m_GatherObjectReferences = evt.newValue);
                gatherObjectReferencesToggle.SetValueWithoutNotify(m_MemoryModule.m_GatherObjectReferences);

                m_UIState.InstallPackageButton = toolbar.Q<UnityEngine.UIElements.Button>("memory-module__toolbar__install-package-button");

                // in the main code base, this button offers to install the memory profiler package, here it is swapped to be one that opens it.
                if (m_InitiatedPackageSearchQuery)
                    m_UIState.InstallPackageButton.schedule.Execute(CheckPackageAvailabilityStatus).Until(() => !m_InitiatedPackageSearchQuery);
                m_UIState.InstallPackageButton.clicked += PackageInstallButtonClicked;
                UpdatePackageInstallButton();

                m_UIState.EditorWarningLabel = toolbar.Q("memory-module__toolbar__editor-warning");

                m_ConnectionState = PlayerConnectionGUIUtility.GetConnectionState(ProfilerWindow, ConnectionChanged);
                UIElementsHelper.SetVisibility(m_UIState.EditorWarningLabel, m_ConnectionState.connectionName == "Editor");

                m_UIState.ViewArea = root.Q("memory-module__view-area");

                m_UIState.SimpleView = m_UIState.ViewArea.Q("memory-module__simple-area");
                m_UIState.CounterBasedUI = m_UIState.SimpleView.Q("memory-module__simple-area__counter-based-ui");

                var normalizedToggle = m_UIState.CounterBasedUI.Q<Toggle>("memory-module__simple-area__breakdown__normalized-toggle");
                normalizedToggle.value = m_MemoryModule.m_Normalized;
                normalizedToggle.RegisterValueChangedCallback((evt) =>
                {
                    m_MemoryModule.m_Normalized = evt.newValue;
                    UpdateContent(ProfilerWindow.selectedFrameIndex);
                });

                m_UIState.TopLevelBreakdown = m_UIState.CounterBasedUI.Q<MemoryUsageBreakdown>("memory-usage-breakdown__top-level");
                m_UIState.TopLevelBreakdown.Setup();
                m_UIState.Breakdown = m_UIState.CounterBasedUI.Q<MemoryUsageBreakdown>("memory-usage-breakdown");
                m_UIState.Breakdown.Setup();

                var m_ObjectStatsTable = m_UIState.CounterBasedUI.Q("memory-usage-breakdown__object-stats_list");

                SetupObjectTableRow(m_ObjectStatsTable.Q("memory-usage-breakdown__object-stats__textures"), ref m_UIState.TexturesRow, Content.Textures);
                SetupObjectTableRow(m_ObjectStatsTable.Q("memory-usage-breakdown__object-stats__meshes"), ref m_UIState.MeshesRow, Content.Meshes);
                SetupObjectTableRow(m_ObjectStatsTable.Q("memory-usage-breakdown__object-stats__materials"), ref m_UIState.MaterialsRow, Content.Materials);
                SetupObjectTableRow(m_ObjectStatsTable.Q("memory-usage-breakdown__object-stats__animation-clips"), ref m_UIState.AnimationClipsRow, Content.AnimationClips);
                SetupObjectTableRow(m_ObjectStatsTable.Q("memory-usage-breakdown__object-stats__assets"), ref m_UIState.AssetsRow, Content.Assets, true);
                SetupObjectTableRow(m_ObjectStatsTable.Q("memory-usage-breakdown__object-stats__game-objects"), ref m_UIState.GameObjectsRow, Content.GameObjects, true);
                SetupObjectTableRow(m_ObjectStatsTable.Q("memory-usage-breakdown__object-stats__scene-objects"), ref m_UIState.SceneObjectsRow, Content.SceneObjects, true);

                var m_GCAllocExtraRow = m_UIState.CounterBasedUI.Q<VisualElement>("memory-usage-breakdown__object-stats__gc");
                SetupObjectTableRow(m_GCAllocExtraRow, ref m_UIState.GCAllocRow, Content.GCAlloc);

                m_UIState.Text = m_UIState.SimpleView.Q<TextField>("memory-module__simple-area__label");

                var detailedView = m_UIState.ViewArea.Q<IMGUIContainer>("memory-module__detaile-snapshot-area");// new IMGUIContainer();
                detailedView.onGUIHandler = () => m_MemoryModule.DrawDetailedMemoryPane();
                m_UIState.DetailedView = detailedView;

                m_UIState.NoDataView = m_UIState.ViewArea.Q("memory-module__no-frame-data__area");
                m_UIState.NoDataView.Q<Label>("memory-module__no-frame-data__label").text = Content.NoFrameDataAvailable;

                ViewChanged(m_MemoryModule.m_ShowDetailedMemoryPane);
                return root;
            }

            void SetupObjectTableRow(VisualElement rowRoot, ref ObjectTableRow row, string name, bool sizesUnknown = false)
            {
                rowRoot.Q<Label>("memory-usage-breakdown__object-table__name").text = name;
                row.Count = rowRoot.Q<Label>("memory-usage-breakdown__object-table__count-column");
                row.Count.text = "0";
                row.Size = rowRoot.Q<Label>("memory-usage-breakdown__object-table__size-column");
                row.Size.text = sizesUnknown ? "-" : "0";
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (ProfilerWindow != null)
                    ProfilerWindow.SelectedFrameIndexChanged -= UpdateContent;

                if (m_ConnectionState != null)
                    m_ConnectionState.Dispose();
                m_ConnectionState = null;

                m_UIState = null;
            }
        }

        public override ProfilerModuleViewController CreateDetailsViewController() =>
            MemoryProfilerOverrides.CreateDetailsViewController == null ? new MemoryProfilerModuleViewController(ProfilerWindow, this) : MemoryProfilerOverrides.CreateDetailsViewController(ProfilerWindow);

        internal static class Styles
        {
            public static readonly GUIContent gatherObjectReferences = EditorGUIUtility.TrTextContent("Gather object references", "Collect reference information to see where objects are referenced from. Disable this to save memory");
            public static readonly GUIContent takeSample = EditorGUIUtility.TrTextContent("Take Sample {0}", "Warning: this may freeze the Editor and the connected Player for a moment!");
            public static readonly GUIContent memoryUsageInEditorDisclaimer = EditorGUIUtility.TrTextContent("Memory usage in the Editor is not the same as it would be in a Player.");
            public const string memoryProfilerPackageDocumentatinURL = "https://docs.unity3d.com/Packages/com.unity.memoryprofiler@latest/";
            public static readonly GUIContent experimentalPackageHint = EditorGUIUtility.TrTextContent("See more details with the experimental Memory Profiler Package.");
            public static readonly string packageInstallSuggestion = L10n.Tr("Install Memory Profiler Package{0}");
            public static readonly string packageInstallSuggestionVersionPart = L10n.Tr(" (Version {0})");
            public static GUIContent packageInstallSuggestionButton = new GUIContent(string.Format(packageInstallSuggestion, ""));
        }

        const int k_DefaultOrderIndex = 3;

        static readonly float[] k_SplitterMinSizes = new[] { 450f, 50f };
        static readonly string[] k_DefaultMemoryAreaCounterNames =
        {
            "Total Used Memory",
            "Texture Memory",
            "Mesh Memory",
            "Material Count",
            "Object Count",
            "GC Used Memory",
            "GC Allocated In Frame",
        };
        static readonly string[] k_PS4MemoryAreaAdditionalCounterNames = new string[]
        {
            "GARLIC heap allocs",
            "ONION heap allocs"
        };
        static readonly string k_MemoryCountersCategoryName = ProfilerCategory.Memory.Name;

        static WeakReference instance;

        const string k_ViewTypeSettingsKey = "Profiler.MemoryProfilerModule.ViewType";
        const string k_GatherObjectReferencesSettingsKey = "Profiler.MemoryProfilerModule.GatherObjectReferences";
        const string k_SplitterRelative0SettingsKey = "Profiler.MemoryProfilerModule.Splitter.Relative[0]";
        const string k_SplitterRelative1SettingsKey = "Profiler.MemoryProfilerModule.Splitter.Relative[1]";

        [SerializeField]
        SplitterState m_ViewSplit;

        ProfilerMemoryView m_ShowDetailedMemoryPane;
        bool m_Normalized;

        MemoryTreeList m_ReferenceListView;
        MemoryTreeListClickable m_MemoryListView;
        // Used with via Reflection (see MemoryProfilerModuleBridge.cs) from the Profiler Memory Profiler Package for the Memory Profiler Module UI Override. (Only when showing old (pre-2020.2 aka pre-memory-counters) profiler data)
        // (Only until the package fully replaces all workflows afforded by this view, at which point the details view will just not be available from the package override UI anymore)
        bool m_GatherObjectReferences = true;

        internal override ProfilerArea area => ProfilerArea.Memory;
        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartMemory";

        enum PackageStage
        {
            Experimental,
            PreviewOrReleased,
            Installed,
        }

        PackageStage m_MemoryProfilerPackageStage = PackageStage.Experimental;
        UnityEditor.PackageManager.Requests.SearchRequest m_MemoryProfilerSearchRequest = null;
        string m_MemoryProfilerPackageName = "com.unity.memoryprofiler";

        bool wantsMemoryRefresh { get { return m_MemoryListView.RequiresRefresh; } }

        internal override void OnEnable()
        {
            base.OnEnable();

            instance = new WeakReference(this);

            InitiateMemoryProfilerPackageAvailabilityCheck();

            if (m_ReferenceListView == null)
                m_ReferenceListView = new MemoryTreeList(ProfilerWindow, null);
            if (m_MemoryListView == null)
                m_MemoryListView = new MemoryTreeListClickable(ProfilerWindow, m_ReferenceListView);
            if (m_ViewSplit == null || !m_ViewSplit.IsValid())
                m_ViewSplit = SplitterState.FromRelative(new[] { EditorPrefs.GetFloat(k_SplitterRelative0SettingsKey, 70f), EditorPrefs.GetFloat(k_SplitterRelative1SettingsKey, 30f) }, k_SplitterMinSizes, null);

            m_ShowDetailedMemoryPane = (ProfilerMemoryView)EditorPrefs.GetInt(k_ViewTypeSettingsKey, (int)ProfilerMemoryView.Simple);
            m_GatherObjectReferences = EditorPrefs.GetBool(k_GatherObjectReferencesSettingsKey, true);
        }

        internal override void SaveViewSettings()
        {
            base.SaveViewSettings();
            EditorPrefs.SetInt(k_ViewTypeSettingsKey, (int)m_ShowDetailedMemoryPane);
            EditorPrefs.SetBool(k_GatherObjectReferencesSettingsKey, m_GatherObjectReferences);
            if (m_ViewSplit != null && m_ViewSplit.relativeSizes != null && m_ViewSplit.relativeSizes.Length >= 2)
            {
                EditorPrefs.SetFloat(k_SplitterRelative0SettingsKey, m_ViewSplit.relativeSizes[0]);
                EditorPrefs.SetFloat(k_SplitterRelative1SettingsKey, m_ViewSplit.relativeSizes[1]);
            }
        }

        internal override void OnNativePlatformSupportModuleChanged()
        {
            base.OnNativePlatformSupportModuleChanged();

            var chartCounters = CollectDefaultChartCounters();
            SetCounters(chartCounters, chartCounters);
        }

        bool InitiateMemoryProfilerPackageAvailabilityCheck()
        {
            var installedPackages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            foreach (var package in installedPackages)
            {
                if (package.name == m_MemoryProfilerPackageName)
                {
                    m_MemoryProfilerPackageStage = PackageStage.Installed;
                    return true;
                }
            }

            m_MemoryProfilerSearchRequest = UnityEditor.PackageManager.Client.Search(m_MemoryProfilerPackageName, true);
            return false;
        }

        bool MemoryProfilerPackageAvailabilityCheckMoveNext()
        {
            if (m_MemoryProfilerSearchRequest != null)
            {
                if (m_MemoryProfilerSearchRequest.IsCompleted)
                {
                    if (m_MemoryProfilerSearchRequest.Result != null)
                    {
                        foreach (var result in m_MemoryProfilerSearchRequest.Result)
                        {
                            if (!result.version.StartsWith("0."))
                            {
                                m_MemoryProfilerPackageStage = PackageStage.PreviewOrReleased;
                                Styles.packageInstallSuggestionButton = new GUIContent(string.Format(Styles.packageInstallSuggestion, string.Format(Styles.packageInstallSuggestionVersionPart, m_MemoryProfilerSearchRequest.Result[0].versions.latestCompatible)));
                                break;
                            }
                        }
                    }
                    m_MemoryProfilerSearchRequest = null;
                    return false;
                }
                return true;
            }
            return false;
        }

        protected override List<ProfilerCounterData> CollectDefaultChartCounters()
        {
            var defaultChartCounters = new List<ProfilerCounterData>(k_DefaultMemoryAreaCounterNames.Length);
            foreach (var defaultCounterName in k_DefaultMemoryAreaCounterNames)
            {
                defaultChartCounters.Add(new ProfilerCounterData()
                {
                    m_Name = defaultCounterName,
                    m_Category = k_MemoryCountersCategoryName,
                });
            }

            // Add any counters specific to native platforms.
            var m_ActiveNativePlatformSupportModule = EditorUtility.GetActiveNativePlatformSupportModuleName();
            if (m_ActiveNativePlatformSupportModule == "PS4")
            {
                var ps4ChartCounters = new List<ProfilerCounterData>(k_PS4MemoryAreaAdditionalCounterNames.Length);
                foreach (var ps4CounterName in k_PS4MemoryAreaAdditionalCounterNames)
                {
                    ps4ChartCounters.Add(new ProfilerCounterData()
                    {
                        m_Name = ps4CounterName,
                        m_Category = k_MemoryCountersCategoryName,
                    });
                }

                defaultChartCounters.AddRange(ps4ChartCounters);
            }

            return defaultChartCounters;
        }

        // Used with via Reflection (see MemoryProfilerModuleBridge.cs) from the Profiler Memory Profiler Package for the Memory Profiler Module UI Override. (Only when showing old (pre-2020.2 aka pre-memory-counters) profiler data)
        static string GetSimpleMemoryPaneText(RawFrameDataView f, IProfilerWindowController profilerWindow, bool summary = true)
        {
            if (f.valid)
            {
                var totalReservedMemoryId = GetCounterValue(f, "Total Reserved Memory");
                if (totalReservedMemoryId != -1)
                {
                    // Counter Data is available, a text form display is not needed
                    return string.Empty;
                }
                else
                {
                    // Old data compatibility.
                    return ProfilerDriver.GetOverviewText(ProfilerArea.Memory, profilerWindow.GetActiveVisibleFrameIndex());
                }
            }
            return string.Empty;
        }

        // Used with via Reflection (see MemoryProfilerModuleBridge.cs) from the Profiler Memory Profiler Package for the Memory Profiler Module UI Override. (To avoid pulling platform specifics into the package)
        static string GetPlatformSpecificText(RawFrameDataView f, IProfilerWindowController profilerWindow)
        {
            if (f.valid)
            {
                var totalReservedMemoryId = GetCounterValue(f, "Total Reserved Memory");
                if (totalReservedMemoryId != -1)
                {
                    StringBuilder stringBuilder = new StringBuilder(1024);
                    var garlicHeapUsedMemory = GetCounterValue(f, "GARLIC heap used");
                    if (garlicHeapUsedMemory != -1)
                    {
                        var garlicHeapAvailable = GetCounterValue(f, "GARLIC heap available");
                        stringBuilder.Append($"\n\nGARLIC heap used: {EditorUtility.FormatBytes(garlicHeapUsedMemory)}/{EditorUtility.FormatBytes(garlicHeapAvailable + garlicHeapUsedMemory)}   ");
                        stringBuilder.Append($"({EditorUtility.FormatBytes(garlicHeapAvailable)} available)   ");
                        stringBuilder.Append($"peak used: {GetCounterValueAsBytes(f, "GARLIC heap peak used")}   ");
                        stringBuilder.Append($"num allocs: {GetCounterValue(f, "GARLIC heap allocs")}\n");

                        stringBuilder.Append($"ONION heap used: {GetCounterValueAsBytes(f, "ONION heap used")}   ");
                        stringBuilder.Append($"peak used: {GetCounterValueAsBytes(f, "ONION heap peak used")}   ");
                        stringBuilder.Append($"num allocs: {GetCounterValue(f, "ONION heap allocs")}");
                        return stringBuilder.ToString();
                    }
                }
            }
            return null;
        }

        // Used with via Reflection (see MemoryProfilerModuleBridge.cs) from the Profiler Memory Profiler Package for the Memory Profiler Module UI Override.
        // (Only until the package fully replaces all workflows afforded by this view, at which point the details view will just not be available from the package override UI anymore)
        void DrawDetailedMemoryPane()
        {
            SplitterGUILayout.BeginHorizontalSplit(m_ViewSplit);

            m_MemoryListView.OnGUI();
            m_ReferenceListView.OnGUI();

            SplitterGUILayout.EndHorizontalSplit();
        }

        // Used with via Reflection (see MemoryProfilerModuleBridge.cs) from the Profiler Memory Profiler Package for the Memory Profiler Module UI Override. (Only when showing old (pre-2020.2 aka pre-memory-counters) profiler data)
        // (Only until the package fully replaces all workflows afforded by this view, at which point the details view will just not be available from the package override UI anymore)
        void RefreshMemoryData()
        {
            m_MemoryListView.RequiresRefresh = true;
            ProfilerDriver.RequestObjectMemoryInfo(m_GatherObjectReferences);
        }

        /// <summary>
        /// Called from Native in ObjectMemoryProfiler.cpp ObjectMemoryProfiler::DeserializeAndApply
        /// </summary>
        /// <param name="memoryInfo"></param>
        /// <param name="referencedIndices"></param>
        static void SetMemoryProfilerInfo(ObjectMemoryInfo[] memoryInfo, int[] referencedIndices)
        {
            if (instance.IsAlive && (instance.Target as MemoryProfilerModule).wantsMemoryRefresh)
            {
                (instance.Target as MemoryProfilerModule).m_MemoryListView.SetRoot(MemoryElementDataManager.GetTreeRoot(memoryInfo, referencedIndices));
            }
        }
    }
}
