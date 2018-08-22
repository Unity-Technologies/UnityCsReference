// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;


namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class ProfilerFrameDataViewBase
    {
        protected static class BaseStyles
        {
            public static readonly GUIContent noData = EditorGUIUtility.TrTextContent("No frame data available");
            public static GUIContent disabledSearchText = EditorGUIUtility.TrTextContent("Showing search results are disabled while recording with deep profiling.\nStop recording to view search results.");
            public static GUIContent cpuGPUTime = EditorGUIUtility.TrTextContent("CPU:{0}ms   GPU:{1}ms");

            public static readonly GUIStyle header = "OL title";
            public static readonly GUIStyle label = "OL label";
            public static readonly GUIStyle toolbar = EditorStyles.toolbar;
            public static readonly GUIStyle tooltip = "AnimationEventTooltip";
            public static readonly GUIStyle tooltipArrow = "AnimationEventTooltipArrow";
            public static readonly GUIStyle viewTypeToolbarDropDown = new GUIStyle(EditorStyles.toolbarDropDown);
            public static readonly GUIStyle detailedViewTypeToolbarDropDown = new GUIStyle(EditorStyles.toolbarDropDown);

            static BaseStyles()
            {
                viewTypeToolbarDropDown.fixedWidth = Chart.kSideWidth - 1f - viewTypeToolbarDropDown.padding.left;
                viewTypeToolbarDropDown.stretchWidth = false;

                detailedViewTypeToolbarDropDown.fixedWidth = 130f;
            }
        }

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

        public bool gpuView { get; set; }

        public delegate void ViewTypeChangedCallback(ProfilerViewType viewType);
        public event ViewTypeChangedCallback viewTypeChanged;

        protected ProfilerFrameDataViewBase()
        {
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

        protected void DrawCPUGPUTime(string cpuTime, string gpuTime)
        {
            GUILayout.Label(string.Format(BaseStyles.cpuGPUTime.text, cpuTime, gpuTime), EditorStyles.miniLabel);
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

        public virtual FrameViewFilteringModes GetFilteringMode()
        {
            return 0;
        }
    }
}
