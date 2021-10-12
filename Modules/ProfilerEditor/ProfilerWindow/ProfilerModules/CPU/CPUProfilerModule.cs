// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    // TODO: refactor: rename to CpuProfilerModule
    // together with CPUOrGPUProfilerModule and GpuProfilerModule
    // in a PR that doesn't affect performance so that the sample names can be fixed as well without loosing comparability in Performance tests.
    [ProfilerModuleMetadata("CPU Usage", typeof(LocalizationResource), IconPath = "Profiler.CPU")]
    internal class CPUProfilerModule : CPUOrGPUProfilerModule
    {
        static class Styles
        {
            public static readonly GUIStyle whiteLabel = "ProfilerBadge";
            public static readonly float labelDropShadowOpacity = 0.3f;
        }

        const string k_SettingsKeyPrefix = "Profiler.CPUProfilerModule.";
        const int k_DefaultOrderIndex = 0;

        // ProfilerWindow exposes this as a const value via ProfilerWindow.cpuModuleName, so we need to define it as const.
        internal const string k_Identifier = "UnityEditorInternal.Profiling.CPUProfilerModule, UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";

        static class Content
        {
            public static readonly GUIContent selectionHighlightLabelBaseText = EditorGUIUtility.TrTextContent("Selected: {0}", "Selected Sample Stack: {0}");
            public static readonly GUIContent selectionHighlightNonMainThreadLabelBaseText = EditorGUIUtility.TrTextContent("Selected: {0} (Thread: {1})", "Selected Sample Stack: {0} (Thread: {1})");
        }

        GUIContent selectionHighlightLabel = new GUIContent();

        [SerializeField]
        ProfilerTimelineGUI m_TimelineGUI;

        internal override ProfilerArea area => ProfilerArea.CPU;
        public override bool usesCounters => false;

        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartCPU";
        protected override string SettingsKeyPrefix => k_SettingsKeyPrefix;
        protected override ProfilerViewType DefaultViewTypeSetting => ProfilerViewType.Timeline;

        internal const string mainThreadName = "Main Thread";
        internal const string mainThreadGroupName = "";
        internal const string renderThreadName = "Render Thread";
        internal const string renderThreadGroupName = "";
        internal const string loadingThreadNamePrefix = "Loading";
        internal const string jobThreadNamePrefix = "Job";
        internal const string scriptingThreadNamePrefix = "Scripting Thread";


        [NonSerialized]
        string m_LastThreadName = "";

        internal override void OnEnable()
        {
            base.OnEnable();

            m_TimelineGUI = new ProfilerTimelineGUI();
            m_TimelineGUI.OnEnable(this, ProfilerWindow, false);
            // safety guarding against event registration leaks due to an imbalance of OnEnable/OnDisable Calls, by deregistering first
            m_TimelineGUI.viewTypeChanged -= CPUOrGPUViewTypeChanged;
            m_TimelineGUI.viewTypeChanged += CPUOrGPUViewTypeChanged;
            m_TimelineGUI.selectionChanged -= SetSelectionWithoutIntegrityChecksOnSelectionChangeInDetailedView;
            m_TimelineGUI.selectionChanged += SetSelectionWithoutIntegrityChecksOnSelectionChangeInDetailedView;
            UpdateSelectionHighlightLabel();
        }

        internal override void OnDisable()
        {
            base.OnDisable();
            if (m_TimelineGUI != null)
            {
                m_TimelineGUI.viewTypeChanged -= CPUOrGPUViewTypeChanged;
                m_TimelineGUI.selectionChanged -= SetSelectionWithoutIntegrityChecksOnSelectionChangeInDetailedView;
            }
        }

        internal override void Clear()
        {
            base.Clear();
            m_TimelineGUI?.Clear();
        }

        private protected override void DrawChartOverlay(Rect chartRect)
        {
            // Show selected property name
            if (selection == null)
                return;

            var content = selectionHighlightLabel;
            var size = EditorStyles.whiteLabel.CalcSize(content);
            const float marginRight = 3;
            const float textMarginRight = 2;
            // the FPS label could get in the way, "0.1ms (10000FPS)" being the longest.
            const float marginLeft = 120;
            var maxWidth = chartRect.width - (marginRight + marginLeft);
            if (Event.current.type == EventType.Repaint && size.x > maxWidth)
            {
                var length = selectionHighlightLabel.text.Length - 3;
                content = GUIContent.Temp(selectionHighlightLabel.text, selectionHighlightLabel.tooltip);
                while (length > 0 && size.x > maxWidth)
                {
                    content.text = content.text.Substring(0, --length) + "...";
                    size = EditorStyles.whiteLabel.CalcSize(content);
                }
            }
            var r = new Rect(chartRect.x + chartRect.width - size.x - marginRight - textMarginRight, chartRect.y + marginRight, size.x + textMarginRight, size.y);
            EditorGUI.DoDropShadowLabel(r, content, Styles.whiteLabel, Styles.labelDropShadowOpacity);
        }

        public override void DrawDetailsView(Rect position)
        {
            if (m_TimelineGUI != null && m_ViewType == ProfilerViewType.Timeline)
            {
                if (Event.current.isKey)
                    ProfilerWindowAnalytics.RecordViewKeyboardEvent(ProfilerWindowAnalytics.profilerCPUModuleTimeline);
                if (Event.current.isMouse && position.Contains(Event.current.mousePosition))
                    ProfilerWindowAnalytics.RecordViewMouseEvent(ProfilerWindowAnalytics.profilerCPUModuleTimeline);
                CurrentFrameIndex = (int)ProfilerWindow.selectedFrameIndex;
                m_TimelineGUI.DoGUI(CurrentFrameIndex, position, fetchData, ref updateViewLive);
            }
            else
            {
                if (Event.current.isKey)
                    ProfilerWindowAnalytics.RecordViewKeyboardEvent(ProfilerWindowAnalytics.profilerCPUModuleHierarchy);
                if (Event.current.isMouse && position.Contains(Event.current.mousePosition))
                    ProfilerWindowAnalytics.RecordViewMouseEvent(ProfilerWindowAnalytics.profilerCPUModuleHierarchy);
                base.DrawDetailsView(position);
            }
        }

        internal override void Rebuild()
        {
            base.Rebuild();
            m_TimelineGUI.ReInitialize();
        }

        protected override HierarchyFrameDataView.ViewModes GetFilteringMode()
        {
            return (((int)ViewOptions & (int)ProfilerViewFilteringOptions.CollapseEditorBoundarySamples) != 0) ? HierarchyFrameDataView.ViewModes.HideEditorOnlySamples : HierarchyFrameDataView.ViewModes.Default;
        }

        internal override void SetOption(ProfilerViewFilteringOptions option, bool on)
        {
            base.SetOption(option, on);
            m_TimelineGUI?.ReInitialize();
        }

        protected override void ToggleOption(ProfilerViewFilteringOptions option)
        {
            base.ToggleOption(option);
            m_TimelineGUI?.ReInitialize();
        }

        // Used for testing
        internal override void GetSelectedSampleIdsForCurrentFrameAndView(ref List<int> ids)
        {
            if (ViewType == ProfilerViewType.Timeline)
            {
                ids.Clear();
                if (selection != null)
                {
                    m_TimelineGUI.GetSelectedSampleIdsForCurrentFrameAndView(ref ids);
                }
            }
            else
            {
                base.GetSelectedSampleIdsForCurrentFrameAndView(ref ids);
            }
        }

        protected override void FrameThread(int threadIndex)
        {
            base.FrameThread(threadIndex);
            if (ViewType == ProfilerViewType.Timeline)
            {
                m_TimelineGUI.FrameThread(threadIndex);
            }
        }

        protected override void ApplySelection(bool viewChanged, bool frameSelection)
        {
            if (ViewType == ProfilerViewType.Timeline)
            {
                if (selection != null)
                {
                    using (k_ApplyValidSelectionMarker.Auto())
                    {
                        var threadIndex = GetThreadIndexInCurrentFrameToApplySelectionFromAnotherFrame(selection);
                        m_TimelineGUI.SetSelection(selection, threadIndex, frameSelection);
                    }
                }
                else
                {
                    using (k_ApplySelectionClearMarker.Auto())
                    {
                        m_TimelineGUI.ClearSelection();
                    }
                }
            }
            else
            {
                base.ApplySelection(viewChanged, frameSelection);
            }
        }

        static readonly ProfilerMarker k_SetSelectedPropertyPathMarker = new ProfilerMarker($"{nameof(CPUProfilerModule)}.{nameof(SetSelectedPropertyPath)} Apply Selection");
        static readonly ProfilerMarker k_SetSelectedPropertyPathUpdateChartsMarker = new ProfilerMarker($"{nameof(CPUProfilerModule)}.{nameof(SetSelectedPropertyPath)} Update Charts");
        static readonly ProfilerMarker k_ClearSelectedPropertyPathMarker = new ProfilerMarker($"{nameof(CPUProfilerModule)}.{nameof(ClearSelectedPropertyPath)} Apply Clear");
        static readonly ProfilerMarker k_ClearSelectedPropertyPathUpdateChartsMarker = new ProfilerMarker($"{nameof(CPUProfilerModule)}.{nameof(ClearSelectedPropertyPath)} Update Charts");

        protected override void SetSelectedPropertyPath(string path, string threadName)
        {
            if (ProfilerDriver.selectedPropertyPath != path)
            {
                using (k_SetSelectedPropertyPathMarker.Auto())
                {
                    ProfilerDriver.selectedPropertyPath = path;
                }
                using (k_SetSelectedPropertyPathUpdateChartsMarker.Auto())
                {
                    Update();
                    UpdateSelectionHighlightLabel();
                    m_LastThreadName = threadName;
                }
            }
            else if (threadName != m_LastThreadName)
            {
                m_LastThreadName = threadName;
                UpdateSelectionHighlightLabel();
            }
        }

        protected override void ClearSelectedPropertyPath()
        {
            if (ProfilerDriver.selectedPropertyPath != string.Empty)
            {
                using (k_ClearSelectedPropertyPathMarker.Auto())
                {
                    ProfilerDriver.selectedPropertyPath = string.Empty;
                }
                using (k_ClearSelectedPropertyPathUpdateChartsMarker.Auto())
                {
                    Update();
                    UpdateSelectionHighlightLabel();
                    m_LastThreadName = string.Empty;
                }
            }
            else if (string.Empty != m_LastThreadName)
            {
                m_LastThreadName = string.Empty;
                UpdateSelectionHighlightLabel();
            }
        }

        void UpdateSelectionHighlightLabel()
        {
            if (selection != null)
            {
                System.Text.StringBuilder sampleStack = new System.Text.StringBuilder();
                if (selection.markerPathDepth > 0)
                {
                    var markerNamePath = selection.markerNamePath;
                    for (int i = selection.markerPathDepth - 1; i >= 0; i--)
                    {
                        sampleStack.AppendFormat("\n{0}", markerNamePath[i]);
                    }
                }
                if (selection.threadName == k_MainThreadName)
                {
                    selectionHighlightLabel = new GUIContent(
                        string.Format(Content.selectionHighlightLabelBaseText.text, selection.sampleDisplayName),
                        string.Format(Content.selectionHighlightLabelBaseText.tooltip, sampleStack.ToString()));
                }
                else
                {
                    selectionHighlightLabel = new GUIContent(
                        string.Format(Content.selectionHighlightNonMainThreadLabelBaseText.text, selection.sampleDisplayName, selection.threadName),
                        string.Format(Content.selectionHighlightNonMainThreadLabelBaseText.tooltip, sampleStack.ToString(), selection.threadName));
                }
            }
            else
                selectionHighlightLabel = GUIContent.none;
        }

        protected override int FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(int frameIndex, int threadIndex, string sampleName, out List<int> markerIdPath, string markerNamePath = null)
        {
            if (ViewType == ProfilerViewType.Timeline)
            {
                markerIdPath = new List<int>();
                return FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(frameIndex, threadIndex, ref sampleName, ref markerIdPath, markerNamePath, FrameDataView.invalidMarkerId);
            }
            return base.FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(frameIndex, threadIndex, sampleName, out markerIdPath, markerNamePath);
        }

        protected override int FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(int frameIndex, int threadIndex, ref string sampleName, ref List<int> markerIdPath, int sampleMarkerId)
        {
            if (ViewType == ProfilerViewType.Timeline)
            {
                Debug.Assert(sampleMarkerId != FrameDataView.invalidMarkerId);
                return FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(frameIndex, threadIndex, ref sampleName, ref markerIdPath, null, sampleMarkerId);
            }
            return base.FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(frameIndex, threadIndex, ref sampleName, ref markerIdPath, sampleMarkerId);
        }

        int FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(int frameIndex, int threadIndex, ref string sampleName, ref List<int> markerIdPath, string markerNamePath = null, int sampleMarkerId = FrameDataView.invalidMarkerId)
        {
            using (var frameData = new RawFrameDataView(frameIndex, threadIndex))
            {
                List<int> tempMarkerIdPath = null;
                if (markerIdPath != null && markerIdPath.Count > 0)
                    tempMarkerIdPath = markerIdPath;

                var foundSampleIndex = RawFrameDataView.invalidSampleIndex;
                var sampleIndexPath = new List<int>();

                if (sampleMarkerId == FrameDataView.invalidMarkerId)
                    sampleMarkerId = frameData.GetMarkerId(sampleName);

                int pathLength = 0;
                string lastSampleInPath = null;
                Func<int, int, RawFrameDataView, bool> sampleIdFitsMarkerPathIndex = null;

                if (tempMarkerIdPath != null && tempMarkerIdPath.Count > 0)
                {
                    pathLength = tempMarkerIdPath.Count;
                }
                else if (!string.IsNullOrEmpty(markerNamePath))
                {
                    var path = markerNamePath.Split('/');
                    if (path != null && path.Length > 0)
                    {
                        pathLength = path.Length;
                        using (var iterator = new RawFrameDataView(frameIndex, threadIndex))
                        {
                            tempMarkerIdPath = new List<int>(pathLength);
                            for (int i = 0; i < pathLength; i++)
                            {
                                tempMarkerIdPath.Add(iterator.GetMarkerId(path[i]));
                            }
                        }
                        sampleIdFitsMarkerPathIndex = (sampleIndex, markerPathIndex, iterator) =>
                        {
                            return tempMarkerIdPath[markerPathIndex] == FrameDataView.invalidMarkerId && GetItemName(iterator, sampleIndex) == path[markerPathIndex];
                        };
                    }
                }

                if (pathLength > 0)
                {
                    var enclosingScopeSampleIndex = RawFrameDataView.invalidSampleIndex;
                    if (sampleIndexPath.Capacity < pathLength)
                        sampleIndexPath.Capacity = pathLength + 1; // +1 for the presumably often case of the searched sample being part of the path or in the last scope of it

                    enclosingScopeSampleIndex = ProfilerTimelineGUI.FindNextSampleThroughMarkerPath(
                        frameData, this, tempMarkerIdPath, pathLength, ref lastSampleInPath, ref sampleIndexPath,
                        sampleIdFitsMarkerPathIndex: sampleIdFitsMarkerPathIndex);

                    if (enclosingScopeSampleIndex == RawFrameDataView.invalidSampleIndex)
                    {
                        //enclosing scope not found
                        return RawFrameDataView.invalidSampleIndex;
                    }
                    foundSampleIndex = FindFirstMatchingRawSampleIndexInScopeRecursively(frameData, ref sampleIndexPath, sampleName, sampleMarkerId);
                    while (foundSampleIndex == RawFrameDataView.invalidSampleIndex && enclosingScopeSampleIndex != RawFrameDataView.invalidSampleIndex)
                    {
                        // sample wasn't found in the current scope, find the next scope ...
                        enclosingScopeSampleIndex = ProfilerTimelineGUI.FindNextSampleThroughMarkerPath(
                            frameData, this, tempMarkerIdPath, pathLength, ref lastSampleInPath, ref sampleIndexPath,
                            sampleIdFitsMarkerPathIndex: sampleIdFitsMarkerPathIndex);

                        if (enclosingScopeSampleIndex == RawFrameDataView.invalidSampleIndex)
                            return RawFrameDataView.invalidSampleIndex;
                        // ... and search there
                        foundSampleIndex = FindFirstMatchingRawSampleIndexInScopeRecursively(frameData, ref sampleIndexPath, sampleName, sampleMarkerId);
                    }
                }
                else
                {
                    if (sampleMarkerId == FrameDataView.invalidMarkerId)
                        foundSampleIndex = FindFirstMatchingRawSampleIndexInScopeRecursively(frameData, ref sampleIndexPath, sampleName);
                    else
                        foundSampleIndex = FindFirstMatchingRawSampleIndexInScopeRecursively(frameData, ref sampleIndexPath, null, sampleMarkerId);
                }

                if (foundSampleIndex != RawFrameDataView.invalidSampleIndex)
                {
                    if (string.IsNullOrEmpty(sampleName))
                        sampleName = GetItemName(frameData, foundSampleIndex);
                    if (markerIdPath == null)
                        markerIdPath = new List<int>(sampleIndexPath.Count);
                    // populate marker id path with missing markers
                    for (int i = markerIdPath.Count; i < sampleIndexPath.Count; i++)
                    {
                        markerIdPath.Add(frameData.GetSampleMarkerId(sampleIndexPath[i]));
                    }
                }
                return foundSampleIndex;
            }
        }

        struct RawSampleIterationInfo { public int sampleIndex;  public int lastSampleIndexInScope; }

        int FindFirstMatchingRawSampleIndexInScopeRecursively(RawFrameDataView frameData, ref List<int> sampleIndexPath, string sampleName, int sampleMarkerId = FrameDataView.invalidMarkerId)
        {
            if (sampleMarkerId == FrameDataView.invalidMarkerId)
                sampleMarkerId = frameData.GetMarkerId(sampleName);
            var firstIndex = sampleIndexPath != null && sampleIndexPath.Count > 0 ? sampleIndexPath[sampleIndexPath.Count - 1] : 0;
            var lastSampleInSearchScope = firstIndex == 0 ? frameData.sampleCount - 1 : firstIndex + frameData.GetSampleChildrenCountRecursive(firstIndex);

            // Check if the enclosing scope matches the searched sample
            if (sampleIndexPath != null && sampleIndexPath.Count > 0 && (sampleMarkerId == FrameDataView.invalidMarkerId && GetItemName(frameData, firstIndex) == sampleName || frameData.GetSampleMarkerId(firstIndex) == sampleMarkerId))
            {
                return firstIndex;
            }

            var samplePathAndLastSampleInScope = new List<RawSampleIterationInfo>() {};
            for (int i = firstIndex; i <= lastSampleInSearchScope; i++)
            {
                samplePathAndLastSampleInScope.Add(new RawSampleIterationInfo { sampleIndex = i, lastSampleIndexInScope = i + frameData.GetSampleChildrenCountRecursive(i) });
                if (sampleMarkerId == FrameDataView.invalidMarkerId && (sampleName != null && GetItemName(frameData, i) == sampleName) || frameData.GetSampleMarkerId(i) == sampleMarkerId)
                {
                    // ignore the first sample if it's the enclosing sample (which is already in the list)
                    for (int j = sampleIndexPath.Count > 0 ? 1 : 0; j < samplePathAndLastSampleInScope.Count; j++)
                    {
                        // ignore the first sample if it's either the thread root sample.
                        // we can't always assume that there is a thread root sample, as the data may be mal formed, but we need to check if this is one by checking the name
                        if (j == 0 && frameData.GetSampleName(samplePathAndLastSampleInScope[j].sampleIndex) == frameData.threadName)
                            continue;
                        sampleIndexPath.Add(samplePathAndLastSampleInScope[j].sampleIndex);
                    }
                    return i;
                }
                while (samplePathAndLastSampleInScope.Count > 0 && i + 1 > samplePathAndLastSampleInScope[samplePathAndLastSampleInScope.Count - 1].lastSampleIndexInScope)
                {
                    samplePathAndLastSampleInScope.RemoveAt(samplePathAndLastSampleInScope.Count - 1);
                }
            }
            return RawFrameDataView.invalidSampleIndex;
        }

        private protected override ProfilerChart InstantiateChart(float defaultChartScale, float chartMaximumScaleInterpolationValue)
        {
            var chart = base.InstantiateChart(defaultChartScale, chartMaximumScaleInterpolationValue);
            chart.SetOnSeriesToggleCallback(OnChartSeriesToggled);
            return chart;
        }

        private protected override void ApplyActiveState()
        {
            // Opening/closing CPU chart should not set the CPU area as that would set Profiler.enabled.
        }

        void OnChartSeriesToggled(bool wasToggled)
        {
            if (wasToggled)
            {
                int firstEmptyFrame = firstFrameIndexWithHistoryOffset;
                int firstFrame = Mathf.Max(ProfilerDriver.firstFrameIndex, firstEmptyFrame);
                int frameCount = ProfilerUserSettings.frameCount;
                m_Chart.ComputeChartScaleValue(firstEmptyFrame, firstFrame, frameCount);
            }
        }

        private protected override void UpdateChartOverlay(int firstEmptyFrame, int firstFrame, int frameCount)
        {
            base.UpdateChartOverlay(firstEmptyFrame, firstFrame, frameCount);

            string selectedName = ProfilerDriver.selectedPropertyPath;
            var selectedModule = ProfilerWindow.selectedModule;
            bool hasCPUOverlay = (selectedName != string.Empty) && this.Equals(selectedModule);
            if (hasCPUOverlay)
            {
                m_Chart.UpdateOverlayData(firstEmptyFrame);
            }
            else
            {
                m_Chart.m_Data.hasOverlay = false;
            }
        }
    }
}
