// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditor.MPE;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class ProfilerFrameDataViewBase
    {
        protected static class BaseStyles
        {
            public static readonly GUIContent noData = EditorGUIUtility.TrTextContent("No frame data available. Select a frame from the charts above to see its details here.");
            public static GUIContent disabledSearchText = EditorGUIUtility.TrTextContent("Showing search results are disabled while recording with deep profiling.\nStop recording to view search results.");
            public static GUIContent cpuGPUTime = EditorGUIUtility.TrTextContent("CPU:{0}ms   GPU:{1}ms");

            public static readonly GUIStyle header = "OL title";
            public static readonly GUIStyle label = "OL label";
            public static readonly GUIStyle toolbar = EditorStyles.toolbar;
            public static readonly GUIStyle tooltip = "AnimationEventTooltip";
            public static readonly GUIStyle tooltipArrow = "AnimationEventTooltipArrow";
            public static readonly GUIStyle viewTypeToolbarDropDown = new GUIStyle(EditorStyles.toolbarDropDownLeft);
            public static readonly GUIStyle threadSelectionToolbarDropDown = new GUIStyle(EditorStyles.toolbarDropDown);
            public static readonly GUIStyle detailedViewTypeToolbarDropDown = new GUIStyle(EditorStyles.toolbarDropDown);
            public static readonly GUIContent updateLive = EditorGUIUtility.TrTextContent("Live", "Display the current or selected frame while recording Playmode or Editor. This increases the overhead in the EditorLoop when the Profiler Window is repainted.");
            public static readonly GUIContent liveUpdateMessage = EditorGUIUtility.TrTextContent("Displaying of frame data disabled while recording Playmode or Editor. To see the data, pause recording, or toggle \"Live\" display mode on. " +
                "\n \"Live\" display mode increases the overhead in the EditorLoop when the Profiler Window is repainted.");

            static BaseStyles()
            {
                viewTypeToolbarDropDown.fixedWidth = Chart.kSideWidth;
                viewTypeToolbarDropDown.stretchWidth = false;

                detailedViewTypeToolbarDropDown.fixedWidth = 150f;
            }
        }

        [NonSerialized]
        public string dataAvailabilityMessage = null;

        static readonly GUIContent[] kCPUProfilerViewTypeNames = new GUIContent[]
        {
            EditorGUIUtility.TrTextContent("Timeline"),
            EditorGUIUtility.TrTextContent("Hierarchy"),
            EditorGUIUtility.TrTextContent("Raw Hierarchy")
        };
        static readonly int[] kCPUProfilerViewTypes = new int[]
        {
            (int)ProfilerViewType.Timeline,
            (int)ProfilerViewType.Hierarchy,
            (int)ProfilerViewType.RawHierarchy
        };

        static readonly GUIContent[] kGPUProfilerViewTypeNames = new GUIContent[]
        {
            EditorGUIUtility.TrTextContent("Hierarchy"),
            EditorGUIUtility.TrTextContent("Raw Hierarchy")
        };
        static readonly int[] kGPUProfilerViewTypes = new int[]
        {
            (int)ProfilerViewType.Hierarchy,
            (int)ProfilerViewType.RawHierarchy
        };

        public bool gpuView { get; private set; }

        protected IProfilerWindowController m_ProfilerWindow;

        public CPUorGPUProfilerModule cpuModule { get; private set; }

        public delegate void ViewTypeChangedCallback(ProfilerViewType viewType);
        public event ViewTypeChangedCallback viewTypeChanged;

        public virtual void OnEnable(CPUorGPUProfilerModule cpuOrGpuModule, IProfilerWindowController profilerWindow, bool isGpuView)
        {
            m_ProfilerWindow = profilerWindow;
            cpuModule = cpuOrGpuModule;
            gpuView = isGpuView;
        }

        protected void DrawViewTypePopup(ProfilerViewType viewType)
        {
            ProfilerViewType newViewType;
            if (!gpuView)
            {
                newViewType = (ProfilerViewType)EditorGUILayout.IntPopup((int)viewType, kCPUProfilerViewTypeNames, kCPUProfilerViewTypes, BaseStyles.viewTypeToolbarDropDown, GUILayout.Width(BaseStyles.viewTypeToolbarDropDown.fixedWidth));
            }
            else
            {
                if (viewType == ProfilerViewType.Timeline)
                    viewType = ProfilerViewType.Hierarchy;
                newViewType = (ProfilerViewType)EditorGUILayout.IntPopup((int)viewType, kGPUProfilerViewTypeNames, kGPUProfilerViewTypes, BaseStyles.viewTypeToolbarDropDown, GUILayout.Width(BaseStyles.viewTypeToolbarDropDown.fixedWidth));
            }

            if (newViewType != viewType)
            {
                if (viewTypeChanged != null)
                    viewTypeChanged.Invoke(newViewType);
            }
        }

        protected void DrawLiveUpdateToggle(ref bool updateViewLive)
        {
            using (new EditorGUI.DisabledScope(ProcessService.level != ProcessLevel.Master))
            {
                // This button is only needed in the Master Process
                updateViewLive = GUILayout.Toggle(updateViewLive, BaseStyles.updateLive, EditorStyles.toolbarButton);
            }
        }

        protected void DrawCPUGPUTime(float cpuTimeMs, float gpuTimeMs)
        {
            var cpuTime = cpuTimeMs > 0 ? UnityString.Format("{0:N2}", cpuTimeMs) : "--";
            var gpuTime = gpuTimeMs > 0 ? UnityString.Format("{0:N2}", gpuTimeMs) : "--";
            GUILayout.Label(UnityString.Format(BaseStyles.cpuGPUTime.text, cpuTime, gpuTime), EditorStyles.toolbarLabel);
        }

        protected void ShowLargeTooltip(Vector2 pos, Rect fullRect, string text, float lineHeight)
        {
            var textC = GUIContent.Temp(text);
            var style = BaseStyles.tooltip;
            var size = style.CalcSize(textC);

            // Arrow of tooltip
            var arrowRect = new Rect(pos.x - 32, pos.y, 64, 6);

            // Label box
            var rect = new Rect(pos.x, pos.y + 6, size.x, size.y);

            // Ensure it doesn't go too far right
            if (rect.xMax > fullRect.xMax + 16)
                rect.x = fullRect.xMax - rect.width + 16;
            if (arrowRect.xMax > fullRect.xMax + 20)
                arrowRect.x = fullRect.xMax - arrowRect.width + 20;

            // Adjust left to we can always see giant (STL) names.
            if (rect.xMin < fullRect.xMin + 30)
                rect.x = fullRect.xMin + 30;
            if (arrowRect.xMin < fullRect.xMin - 20)
                arrowRect.x = fullRect.xMin - 20;

            // Flip tooltip if too close to bottom (but do not flip if flipping would mean the tooltip is too high up)
            var flipRectAdjust = (lineHeight + rect.height + 2 * arrowRect.height);
            var flipped = (pos.y + size.y + 6 > fullRect.yMax) && (rect.y - flipRectAdjust > 0);
            if (flipped)
            {
                rect.y -= flipRectAdjust;
                arrowRect.y -= (lineHeight + 2 * arrowRect.height);
            }

            // Draw small arrow
            GUI.BeginClip(arrowRect);
            var oldMatrix = GUI.matrix;
            if (flipped)
                GUIUtility.ScaleAroundPivot(new Vector2(1.0f, -1.0f), new Vector2(arrowRect.width * 0.5f, arrowRect.height));
            GUI.Label(new Rect(0, 0, arrowRect.width, arrowRect.height), GUIContent.none, BaseStyles.tooltipArrow);
            GUI.matrix = oldMatrix;
            GUI.EndClip();

            // Draw tooltip
            GUI.Label(rect, textC, style);
        }

        public virtual void Clear()
        {
        }
    }
}
