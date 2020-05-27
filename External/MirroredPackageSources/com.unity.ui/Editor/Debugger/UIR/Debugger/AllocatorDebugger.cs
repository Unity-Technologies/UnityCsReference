using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;
using static UnityEngine.UIElements.UIR.UIRenderDevice.AllocationStatistics;

namespace UnityEditor.UIElements.Debugger
{
    internal class AllocatorDebugger : UIRDebugger
    {
        [MenuItem("Window/Analysis/UIR Allocator Debugger", false, 202, true)]
        public static void Open()
        {
            GetWindow<AllocatorDebugger>().Show();
        }

        private IMGUIContainer m_IMGUIToolbar;
        private ScrollView m_ScrollView;
        private VisualElement m_StatsContainer;

        private AllocatorStatsDisplay m_GlobalStats;
        private PagesStatsDisplay m_PagesStats;

        public new void OnEnable()
        {
            base.OnEnable();
            titleContent = new GUIContent("Allocator Debugger");

            var root =  rootVisualElement;
            root.AddStyleSheetPath("UIPackageResources/StyleSheets/UIElementsDebugger/UIRAllocatorDebugger.uss");
            m_IMGUIToolbar = new IMGUIContainer(OnGUIToolbar);
            m_ScrollView = new ScrollView() { style = { flexGrow = 1 }};
            m_StatsContainer = new VisualElement();

            m_GlobalStats = new AllocatorStatsDisplay();
            m_PagesStats = new PagesStatsDisplay();

            var globalStatsLabel = new Label("Global Stats");
            globalStatsLabel.AddToClassList("section-header");
            m_StatsContainer.Add(globalStatsLabel);
            m_StatsContainer.Add(m_GlobalStats);

            var pageStatsLabel = new Label("Pages Stats");
            pageStatsLabel.AddToClassList("section-header");
            m_StatsContainer.Add(pageStatsLabel);
            m_StatsContainer.Add(m_PagesStats);

            m_ScrollView.Add(m_StatsContainer);

            root.Add(m_IMGUIToolbar);
            root.Add(m_ScrollView);
        }

        protected override void OnSelectVisualTree(VisualTreeDebug vtDebug)
        {
            if (vtDebug != null)
                Refresh();
        }

        public override void Refresh()
        {
            var renderDevice = UIRDebugUtility.GetUIRenderDevice(m_SelectedVisualTree.panel);
            Debug.Assert(renderDevice != null, "Allocator debugger fail to retrieve UIRenderDevice");
            if (renderDevice != null)
                RefreshStats(renderDevice);
        }

        private void RefreshStats(UIRenderDevice renderDevice)
        {
            var statistics = renderDevice.GatherAllocationStatistics();
            m_GlobalStats.UpdateStats(statistics);
            m_PagesStats.UpdateStats(statistics);
        }

        private void OnGUIToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
            OnGUIPanelSelectDropDown();
            EditorGUILayout.EndHorizontal();
        }
    }

    // Global stats
    internal class AllocatorStatsDisplay : VisualElement
    {
        private IMGUIContainer m_IMGUIContainer;
        private UIRenderDevice.AllocationStatistics m_Stats;

        public AllocatorStatsDisplay()
        {
            m_IMGUIContainer = new IMGUIContainer(OnGUI);
            Add(m_IMGUIContainer);
        }

        public void UpdateStats(UIRenderDevice.AllocationStatistics stats)
        {
            m_Stats = stats;
        }

        private void OnGUI()
        {
            int freesDeferred = 0;
            foreach (var freeDeferred in m_Stats.freesDeferred)
                freesDeferred += freeDeferred;

            using (new EditorGUI.DisabledScope(Event.current.type != EventType.Repaint))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.IntField("Fully init", m_Stats.completeInit ? 1 : 0);
                EditorGUILayout.IntField("Pages Count", m_Stats.pages != null ? m_Stats.pages.Length : 0);
                EditorGUILayout.IntField("Frees Deferred", freesDeferred);
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    // Stats per pages
    internal class PagesStatsDisplay : VisualElement
    {
        class HeapStats : VisualElement
        {
            private HeapStatistics m_Stats;

            private IMGUIContainer m_IMGUIStats;

            public HeapStats(HeapStatistics stats)
            {
                m_Stats = stats;

                m_IMGUIStats = new IMGUIContainer(OnGUIStats);
                Add(m_IMGUIStats);
            }

            private void OnGUIStats()
            {
                var stats = m_Stats;
                using (new EditorGUI.DisabledScope(Event.current.type != EventType.Repaint))
                {
                    EditorGUILayout.IntField("Num Allocs", (int)stats.numAllocs);
                    EditorGUILayout.IntField("Alloc Size", (int)stats.allocatedSize);
                    EditorGUILayout.IntField("Free Size", (int)stats.freeSize);
                    EditorGUILayout.IntField("Largest Block", (int)stats.largestAvailableBlock);
                    EditorGUILayout.IntField("Available Block", (int)stats.availableBlocksCount);
                    EditorGUILayout.IntField("Block Count", (int)stats.blockCount);
                    EditorGUILayout.IntField("High Watermark", (int)stats.highWatermark);
                    EditorGUILayout.FloatField("Fragmentation", stats.fragmentation);
                }
            }
        }

        private VisualElement m_GlobalContainer;
        private VisualElement m_PermContainer;
        private VisualElement m_TempContainer;

        public PagesStatsDisplay()
        {
            m_GlobalContainer = new VisualElement();
            m_PermContainer = new VisualElement();
            m_TempContainer = new VisualElement();

            AddToClassList("pages-stats-display");

            m_GlobalContainer.AddToClassList("alloc-stats-container");
            m_PermContainer.AddToClassList("alloc-stats-container");
            m_TempContainer.AddToClassList("alloc-stats-container");

            Add(m_GlobalContainer);
            Add(m_PermContainer);
            Add(m_TempContainer);
        }

        public void UpdateStats(UIRenderDevice.AllocationStatistics stats)
        {
            m_GlobalContainer.Clear();
            m_PermContainer.Clear();
            m_TempContainer.Clear();

            m_GlobalContainer.Add(new Label("Global"));
            m_PermContainer.Add(new Label("Perm"));
            m_TempContainer.Add(new Label("Temp"));

            foreach (var pageStats in stats.pages)
            {
                var globalStats = new VisualElement();
                globalStats.AddToClassList("page-stats");
                globalStats.Add(new Label("Vertices"));
                globalStats.Add(new HeapStats(pageStats.vertices));
                globalStats.Add(new Label("Indices"));
                globalStats.Add(new HeapStats(pageStats.indices));
                m_GlobalContainer.Add(globalStats);

                var permStats = new VisualElement();
                permStats.AddToClassList("page-stats");
                permStats.Add(new Label("Vertices"));
                permStats.Add(new HeapStats(pageStats.vertices.subAllocators[0]));
                permStats.Add(new Label("Indices"));
                permStats.Add(new HeapStats(pageStats.indices.subAllocators[0]));
                m_PermContainer.Add(permStats);

                var tempStats = new VisualElement();
                tempStats.AddToClassList("page-stats");
                tempStats.Add(new Label("Vertices"));
                tempStats.Add(new HeapStats(pageStats.vertices.subAllocators[1]));
                tempStats.Add(new Label("Indices"));
                tempStats.Add(new HeapStats(pageStats.indices.subAllocators[1]));
                m_TempContainer.Add(tempStats);
            }
        }
    }
}
