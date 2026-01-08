// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;

namespace UnityEditorInternal.Profiling
{
    internal abstract class ProfilerDetailedView
    {
        protected static readonly string kNoneText = LocalizationDatabase.GetLocalizedString("None");

        protected static class Styles
        {
            public static readonly GUIContent emptyText = new GUIContent("");
            public static readonly GUIContent selectLineText = EditorGUIUtility.TrTextContent("Select Line for the detailed information");

            public static readonly GUIContent askAssistantTooltip = EditorGUIUtility.TrTextContent("Ask Assistant", "Ask the Profiler Assistant for help understanding this sample");

            public static readonly GUIStyle expandedArea = new GUIStyle();
            public static readonly GUIStyle callstackScroll = new GUIStyle("CN Box");
            public static readonly GUIStyle callstackTextArea = new GUIStyle("CN Message");

            static Styles()
            {
                expandedArea.stretchWidth = true;
                expandedArea.stretchHeight = true;
                expandedArea.padding = new RectOffset(0, 0, 0, 0);

                callstackScroll.padding = new RectOffset(5, 5, 5, 5);

                callstackTextArea.margin = new RectOffset(0, 0, 0, 0);
                callstackTextArea.padding = new RectOffset(3, 3, 3, 3);
                callstackTextArea.wordWrap = false;
                callstackTextArea.stretchWidth = true;
                callstackTextArea.stretchHeight = true;
            }
        }

        protected HierarchyFrameDataView m_FrameDataView;

        protected CPUOrGPUProfilerModule m_CPUOrGPUProfilerModule;
        [NonSerialized]
        protected ProfilerFrameDataHierarchyView m_ProfilerFrameDataHierarchyView;

        [SerializeField]
        protected int m_SelectedID = -1;

        protected void DrawEmptyPane(GUIStyle headerStyle)
        {
            GUILayout.Box(Styles.emptyText, headerStyle);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label(Styles.selectLineText, EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        protected void DrawAssistantButton(int selectedId)
        {
            if (selectedId == HierarchyFrameDataView.invalidSampleId || !m_ProfilerFrameDataHierarchyView.CpuProfilerAssistantSupported)
                return;

            var rect = EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(Styles.askAssistantTooltip))
            {
                var markerName = m_FrameDataView.GetItemName(selectedId);

                var markerIdPath = new List<int>();
                m_FrameDataView.GetItemMarkerIDPath(selectedId, markerIdPath);
                var markerIdPathString = string.Join("/", markerIdPath);

                m_ProfilerFrameDataHierarchyView.LaunchCpuProfilerAssistant(rect, m_FrameDataView.frameIndex, m_FrameDataView.threadName, markerIdPathString, markerName);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        public abstract void SaveViewSettings();
        public virtual void OnEnable(CPUOrGPUProfilerModule cpuOrGpuProfilerModule, ProfilerFrameDataHierarchyView profilerFrameDataHierarchyView)
        {
            m_CPUOrGPUProfilerModule = cpuOrGpuProfilerModule;
            m_ProfilerFrameDataHierarchyView = profilerFrameDataHierarchyView;
        }

        public abstract void OnDisable();
    }
}
