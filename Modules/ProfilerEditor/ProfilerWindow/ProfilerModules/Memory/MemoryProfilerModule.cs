// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
using UnityEditor.Profiling;
using System.Text;

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

        static readonly float[] k_SplitterMinSizes = new[] { 450f, 50f };

        [SerializeField]
        SplitterState m_ViewSplit;

        ProfilerMemoryView m_ShowDetailedMemoryPane = (ProfilerMemoryView)EditorPrefs.GetInt(k_ViewTypeSettingsKey, (int)ProfilerMemoryView.Simple);

        private MemoryTreeList m_ReferenceListView;
        private MemoryTreeListClickable m_MemoryListView;
        private bool m_GatherObjectReferences = true;
        bool wantsMemoryRefresh { get { return m_MemoryListView.RequiresRefresh; } }

        static WeakReference instance;

        const string k_ViewTypeSettingsKey = "Profiler.MemoryProfilerModule.ViewType";
        const string k_GatherObjectReferencesSettingsKey = "Profiler.MemoryProfilerModule.GatherObjectReferences";
        const string k_SplitterRelative0SettingsKey = "Profiler.MemoryProfilerModule.Splitter.Relative[0]";
        const string k_SplitterRelative1SettingsKey = "Profiler.MemoryProfilerModule.Splitter.Relative[1]";

        public override void OnEnable(IProfilerWindowController profilerWindow)
        {
            base.OnEnable(profilerWindow);

            instance = new WeakReference(this);

            if (m_ReferenceListView == null)
                m_ReferenceListView = new MemoryTreeList(profilerWindow, null);
            if (m_MemoryListView == null)
                m_MemoryListView = new MemoryTreeListClickable(profilerWindow, m_ReferenceListView);
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

        public override void DrawView(Rect position)
        {
            if (m_ShowDetailedMemoryPane == ProfilerMemoryView.Simple)
                DrawSimpleMemoryPane(position);
            else
                DrawDetailedMemoryPane(m_ViewSplit);
        }

        static long GetCounterValue(FrameDataView frameData, string name)
        {
            var id = frameData.GetMarkerId(name);
            if (id == FrameDataView.invalidMarkerId)
                return -1;

            return frameData.GetCounterValueAsLong(id);
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
                        stringBuilder.Append($"Used Total: {EditorUtility.FormatBytes(totalUsedMemory)}   ");
                        stringBuilder.Append($"Mono/il2cpp: {EditorUtility.FormatBytes(GetCounterValue(f, "GC Used Memory"))}   ");
                        stringBuilder.Append($"Gfx: {EditorUtility.FormatBytes(GetCounterValue(f, "Gfx Used Memory"))}   ");
                        stringBuilder.Append($"Audio: {EditorUtility.FormatBytes(GetCounterValue(f, "Audio Used Memory"))}   ");
                        stringBuilder.Append($"Video: {EditorUtility.FormatBytes(GetCounterValue(f, "Video Used Memory"))}   ");
                        stringBuilder.Append($"Profiler: {EditorUtility.FormatBytes(GetCounterValue(f, "Profiler Used Memory"))}   ");

                        stringBuilder.Append($"\nReserved Total: {EditorUtility.FormatBytes(GetCounterValue(f, "Total Reserved Memory"))}   ");
                        stringBuilder.Append($"Mono/il2cpp: {EditorUtility.FormatBytes(GetCounterValue(f, "GC Reserved Memory"))}   ");
                        stringBuilder.Append($"Gfx: {EditorUtility.FormatBytes(GetCounterValue(f, "Gfx Reserved Memory"))}   ");
                        stringBuilder.Append($"Audio: {EditorUtility.FormatBytes(GetCounterValue(f, "Audio Reserved Memory"))}   ");
                        stringBuilder.Append($"Video: {EditorUtility.FormatBytes(GetCounterValue(f, "Video Reserved Memory"))}   ");
                        stringBuilder.Append($"Profiler: {EditorUtility.FormatBytes(GetCounterValue(f, "Profiler Reserved Memory"))}   ");

                        stringBuilder.Append($"\nTotal System Memory Usage: {EditorUtility.FormatBytes(GetCounterValue(f, "System Used Memory"))}   ");

                        stringBuilder.Append($"\n\nTextures: {GetCounterValue(f, "Texture Count")} / {EditorUtility.FormatBytes(GetCounterValue(f, "Texture Memory"))}   ");
                        stringBuilder.Append($"\nMeshes: {GetCounterValue(f, "Mesh Count")} / {EditorUtility.FormatBytes(GetCounterValue(f, "Mesh Memory"))}   ");
                        stringBuilder.Append($"\nMaterials: {GetCounterValue(f, "Material Count")} / {EditorUtility.FormatBytes(GetCounterValue(f, "Material Memory"))}   ");
                        stringBuilder.Append($"\nAnimationClips: {GetCounterValue(f, "AnimationClip Count")} / {EditorUtility.FormatBytes(GetCounterValue(f, "AnimationClip Memory"))}   ");
                        stringBuilder.Append($"\nAssets: {GetCounterValue(f, "Asset Count")}   ");
                        stringBuilder.Append($"\nGameObjects in Scenes: {GetCounterValue(f, "Game Object Count")}   ");
                        stringBuilder.Append($"\nTotal Objects in Scenes: {GetCounterValue(f, "Scene Object Count")}   ");
                        stringBuilder.Append($"\nTotal Unity Object Count: {GetCounterValue(f, "Object Count")}   ");

                        stringBuilder.Append($"\n\nGC Allocations per Frame: {GetCounterValue(f, "GC Allocation In Frame Count")} / {EditorUtility.FormatBytes(GetCounterValue(f, "GC Allocated In Frame"))}   ");

                        var garlicHeapUsedMemory = GetCounterValue(f, "GARLIC heap used");
                        if (garlicHeapUsedMemory != -1)
                        {
                            var garlicHeapAvailable = GetCounterValue(f, "GARLIC heap available");
                            stringBuilder.Append($"\n\nGARLIC heap used: {EditorUtility.FormatBytes(garlicHeapUsedMemory)}/{EditorUtility.FormatBytes(garlicHeapAvailable + garlicHeapUsedMemory)}   ");
                            stringBuilder.Append($"({EditorUtility.FormatBytes(garlicHeapAvailable)} available)   ");
                            stringBuilder.Append($"peak used: {EditorUtility.FormatBytes(GetCounterValue(f, "GARLIC heap peak used"))}   ");
                            stringBuilder.Append($"num allocs: {GetCounterValue(f, "GARLIC heap allocs")}\n");

                            stringBuilder.Append($"ONION heap used: {EditorUtility.FormatBytes(GetCounterValue(f, "ONION heap used"))}   ");
                            stringBuilder.Append($"peak used: {EditorUtility.FormatBytes(GetCounterValue(f, "ONION heap peak used"))}   ");
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
