// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;

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

        ProfilerMemoryView m_ShowDetailedMemoryPane;

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
                DrawOverviewText(ProfilerArea.Memory, position);
            else
                DrawDetailedMemoryPane(m_ViewSplit);
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
