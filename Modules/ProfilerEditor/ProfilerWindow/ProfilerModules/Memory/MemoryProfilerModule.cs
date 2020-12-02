// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class MemoryProfilerModule : ProfilerModuleBase
    {
        internal static class Styles
        {
            public static readonly GUIContent gatherObjectReferences = EditorGUIUtility.TrTextContent("Gather object references", "Collect reference information to see where objects are referenced from. Disable this to save memory");
            public static readonly GUIContent takeSample = EditorGUIUtility.TrTextContent("Take Sample {0}", "Warning: this may freeze the Editor and the connected Player for a moment!");
            public static readonly GUIContent memoryUsageInEditorDisclaimer = EditorGUIUtility.TrTextContent("Memory usage in the Editor is not the same as it would be in a Player.");
        }

        const string k_IconName = "Profiler.Memory";
        const int k_DefaultOrderIndex = 3;
        static readonly string k_UnLocalizedName = "Memory";
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString(k_UnLocalizedName);

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

        MemoryTreeList m_ReferenceListView;
        MemoryTreeListClickable m_MemoryListView;
        bool m_GatherObjectReferences = true;

        public MemoryProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow, k_UnLocalizedName, k_Name, k_IconName) {}

        public override ProfilerArea area => ProfilerArea.Memory;
        protected override int defaultOrderIndex => k_DefaultOrderIndex;
        protected override string legacyPreferenceKey => "ProfilerChartMemory";

        bool wantsMemoryRefresh { get { return m_MemoryListView.RequiresRefresh; } }

        public override void OnEnable()
        {
            base.OnEnable();

            instance = new WeakReference(this);

            if (m_ReferenceListView == null)
                m_ReferenceListView = new MemoryTreeList(m_ProfilerWindow, null);
            if (m_MemoryListView == null)
                m_MemoryListView = new MemoryTreeListClickable(m_ProfilerWindow, m_ReferenceListView);
            if (m_ViewSplit == null || !m_ViewSplit.IsValid())
                m_ViewSplit = SplitterState.FromRelative(new[] { EditorPrefs.GetFloat(k_SplitterRelative0SettingsKey, 70f), EditorPrefs.GetFloat(k_SplitterRelative1SettingsKey, 30f) }, k_SplitterMinSizes, null);

            m_ShowDetailedMemoryPane = (ProfilerMemoryView)EditorPrefs.GetInt(k_ViewTypeSettingsKey, (int)ProfilerMemoryView.Simple);
            m_GatherObjectReferences = EditorPrefs.GetBool(k_GatherObjectReferencesSettingsKey, true);
        }

        public override void SaveViewSettings()
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

        public override void OnNativePlatformSupportModuleChanged()
        {
            base.OnNativePlatformSupportModuleChanged();

            var chartCounters = CollectDefaultChartCounters();
            SetCounters(chartCounters, chartCounters);
        }

        public override void DrawToolbar(Rect position)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            m_ShowDetailedMemoryPane = (ProfilerMemoryView)EditorGUILayout.EnumPopup(m_ShowDetailedMemoryPane, EditorStyles.toolbarDropDownLeft, GUILayout.Width(70f));

            GUILayout.Space(5f);

            if (m_ShowDetailedMemoryPane == ProfilerMemoryView.Detailed)
            {
                if (GUILayout.Button(GUIContent.Temp(UnityString.Format(Styles.takeSample.text, m_ProfilerWindow.ConnectedTargetName), Styles.takeSample.tooltip), EditorStyles.toolbarButton))
                    RefreshMemoryData();

                m_GatherObjectReferences = GUILayout.Toggle(m_GatherObjectReferences, Styles.gatherObjectReferences, EditorStyles.toolbarButton);

                if (m_ProfilerWindow.ConnectedToEditor)
                    GUILayout.Label(Styles.memoryUsageInEditorDisclaimer, EditorStyles.toolbarButton);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        public override void DrawDetailsView(Rect position)
        {
            if (m_ShowDetailedMemoryPane == ProfilerMemoryView.Simple)
                DrawSimpleMemoryPane(position);
            else
                DrawDetailedMemoryPane(m_ViewSplit);
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

        void DrawSimpleMemoryPane(Rect position)
        {
            string activeText = string.Empty;

            using (var f = ProfilerDriver.GetRawFrameDataView(m_ProfilerWindow.GetActiveVisibleFrameIndex(), 0))
            {
                if (f.valid)
                {
                    var totalUsedMemory = GetCounterValue(f, "Total Used Memory");
                    if (totalUsedMemory != -1)
                    {
                        var stringBuilder = new StringBuilder(1024);
                        stringBuilder.Append($"Total Used Memory: {EditorUtility.FormatBytes(totalUsedMemory)}   ");
                        stringBuilder.Append($"GC: {GetCounterValueAsBytes(f, "GC Used Memory")}   ");
                        stringBuilder.Append($"Gfx: {GetCounterValueAsBytes(f, "Gfx Used Memory")}   ");
                        stringBuilder.Append($"Audio: {GetCounterValueAsBytes(f, "Audio Used Memory")}   ");
                        stringBuilder.Append($"Video: {GetCounterValueAsBytes(f, "Video Used Memory")}   ");
                        stringBuilder.Append($"Profiler: {GetCounterValueAsBytes(f, "Profiler Used Memory")}   ");

                        stringBuilder.Append($"\nTotal Reserved Memory: {GetCounterValueAsBytes(f, "Total Reserved Memory")}   ");
                        stringBuilder.Append($"GC: {GetCounterValueAsBytes(f, "GC Reserved Memory")}   ");
                        stringBuilder.Append($"Gfx: {GetCounterValueAsBytes(f, "Gfx Reserved Memory")}   ");
                        stringBuilder.Append($"Audio: {GetCounterValueAsBytes(f, "Audio Reserved Memory")}   ");
                        stringBuilder.Append($"Video: {GetCounterValueAsBytes(f, "Video Reserved Memory")}   ");
                        stringBuilder.Append($"Profiler: {GetCounterValueAsBytes(f, "Profiler Reserved Memory")}   ");

                        stringBuilder.Append($"\nSystem Used Memory: {GetCounterValueAsBytes(f, "System Used Memory")}   ");

                        stringBuilder.Append($"\n\nTextures: {GetCounterValue(f, "Texture Count")} / {GetCounterValueAsBytes(f, "Texture Memory")}   ");
                        stringBuilder.Append($"\nMeshes: {GetCounterValue(f, "Mesh Count")} / {GetCounterValueAsBytes(f, "Mesh Memory")}   ");
                        stringBuilder.Append($"\nMaterials: {GetCounterValue(f, "Material Count")} / {GetCounterValueAsBytes(f, "Material Memory")}   ");
                        stringBuilder.Append($"\nAnimationClips: {GetCounterValue(f, "AnimationClip Count")} / {GetCounterValueAsBytes(f, "AnimationClip Memory")}   ");
                        stringBuilder.Append($"\nAsset Count: {GetCounterValue(f, "Asset Count")}   ");
                        stringBuilder.Append($"\nGame Object Count: {GetCounterValue(f, "Game Object Count")}   ");
                        stringBuilder.Append($"\nScene Object Count: {GetCounterValue(f, "Scene Object Count")}   ");
                        stringBuilder.Append($"\nObject Count: {GetCounterValue(f, "Object Count")}   ");

                        stringBuilder.Append($"\n\nGC Allocation In Frame: {GetCounterValue(f, "GC Allocation In Frame Count")} / {GetCounterValueAsBytes(f, "GC Allocated In Frame")}   ");

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
                        }

                        activeText = stringBuilder.ToString();
                    }
                    else
                    {
                        // Old data compatibility.
                        activeText = ProfilerDriver.GetOverviewText(ProfilerArea.Memory, m_ProfilerWindow.GetActiveVisibleFrameIndex());
                    }
                }
            }
            float height = EditorStyles.wordWrappedLabel.CalcHeight(GUIContent.Temp(activeText), position.width);

            m_PaneScroll = GUILayout.BeginScrollView(m_PaneScroll, ProfilerWindow.Styles.background);
            EditorGUILayout.SelectableLabel(activeText, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(height));
            GUILayout.EndScrollView();
        }

        void DrawDetailedMemoryPane(SplitterState splitter)
        {
            SplitterGUILayout.BeginHorizontalSplit(splitter);

            m_MemoryListView.OnGUI();
            m_ReferenceListView.OnGUI();

            SplitterGUILayout.EndHorizontalSplit();
        }

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
