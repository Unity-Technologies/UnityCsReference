// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Profiling;
using UnityEditorInternal.Profiling;
using UnityEngine.Profiling;
using System;

namespace UnityEditor.Profiling
{
    public static class ProfilerEditorUtility
    {
        // old API redirects, removed from their interfaces to keep them clean
        // TODO: Once Profile Analyter 1.0.5 is verified, remove this and consolidate on the new API
        internal static int GetActiveVisibleFrameIndex(this IProfilerWindowController controller) => (int)controller.selectedFrameIndex;
        internal static void SetActiveVisibleFrameIndex(this IProfilerWindowController controller, int frame) => controller.selectedFrameIndex = frame;
        // used by Tests/PerformanceTests/Profiler to avoid brittle tests due to reflection
        internal static T GetProfilerModuleByType<T>(this IProfilerWindowController controller) where T : ProfilerModuleBase => controller.GetProfilerModuleByType(typeof(T)) as T;


        // selects first occurence of this sample in the given frame and thread (and optionally given the markerNamePath leading up to it or a (grand, (grand)...) parent of it)
        public static bool SetSelection(this IProfilerFrameTimeViewSampleSelectionController controller, long frameIndex, string threadGroupName, string threadName, string sampleName, string markerNamePath = null, ulong threadId = FrameDataView.invalidThreadId)
        {
            var iController = controller as IProfilerFrameTimeViewSampleSelectionControllerInternal;
            if (controller == null || iController == null)
                throw new ArgumentNullException($"{nameof(controller)}", $"The IProfilerFrameTimeViewSampleSelectionController you are setting a selection on can't be null.");

            ProfilerTimeSampleSelection selection;
            List<int> markerIdPath;
            using (CPUOrGPUProfilerModule.setSelectionIntegrityCheckMarker.Auto())
            {
                // this could've come from anywhere, check the inputs first
                if (string.IsNullOrEmpty(sampleName))
                    throw new ArgumentException($"{nameof(sampleName)} can't be null or empty. Hint: To clear a selection, use {nameof(IProfilerFrameTimeViewSampleSelectionController.ClearSelection)} instead.");


                var threadIndex = CPUOrGPUProfilerModule.IntegrityCheckFrameAndThreadDataOfSelection(frameIndex, threadGroupName, threadName, ref threadId);

                int selectedSampleRawIndex = iController.FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView((int)frameIndex, threadIndex, sampleName, out markerIdPath, markerNamePath);

                if (selectedSampleRawIndex < 0)
                    return false;

                selection = new ProfilerTimeSampleSelection(frameIndex, threadGroupName, threadName, threadId, selectedSampleRawIndex, sampleName);
            }
            using (CPUOrGPUProfilerModule.setSelectionApplyMarker.Auto())
            {
                // looks good, apply
                selection.frameIndexIsSafe = true;
                iController.SetSelectionWithoutIntegrityChecks(selection, markerIdPath);
                return true;
            }
        }

        // selects first occurence of this sample in the given frame and thread and markerId path leading up to it or a (grand, (grand)...) parent of it
        public static bool SetSelection(this IProfilerFrameTimeViewSampleSelectionController controller, long frameIndex, string threadGroupName, string threadName, int sampleMarkerId, List<int> markerIdPath = null, ulong threadId = FrameDataView.invalidThreadId)
        {
            var iController = controller as IProfilerFrameTimeViewSampleSelectionControllerInternal;
            if (controller == null || iController == null)
                throw new ArgumentNullException($"{nameof(controller)}", $"The IProfilerFrameTimeViewSampleSelectionController you are setting a selection on can't be null.");

            ProfilerTimeSampleSelection selection;
            using (CPUOrGPUProfilerModule.setSelectionIntegrityCheckMarker.Auto())
            {
                // this could've come from anywhere, check the inputs first
                if (sampleMarkerId == FrameDataView.invalidMarkerId)
                    throw new ArgumentException($"{nameof(sampleMarkerId)} can't invalid ({FrameDataView.invalidMarkerId}). Hint: To clear a selection, use {nameof(IProfilerFrameTimeViewSampleSelectionController.ClearSelection)} instead.");

                var threadIndex = CPUOrGPUProfilerModule.IntegrityCheckFrameAndThreadDataOfSelection(frameIndex, threadGroupName, threadName, ref threadId);

                string sampleName = null;
                int selectedSampleRawIndex = iController.FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView((int)frameIndex, threadIndex, ref sampleName, ref markerIdPath, sampleMarkerId);

                if (selectedSampleRawIndex < 0)
                    return false;

                selection = new ProfilerTimeSampleSelection(frameIndex, threadGroupName, threadName, threadId, selectedSampleRawIndex, sampleName);
            }
            using (CPUOrGPUProfilerModule.setSelectionApplyMarker.Auto())
            {
                // looks good, apply
                selection.frameIndexIsSafe = true;
                iController.SetSelectionWithoutIntegrityChecks(selection, markerIdPath);
                return true;
            }
        }

        /// <summary>
        /// Search for a sample fitting the '/' seperated path to it and select it
        /// </summary>
        /// <param name="markerNameOrMarkerNamePath">'/' seperated path to the marker </param>
        /// <param name="frameIndex"> The frame to make the selection in, or -1 to select in currently active frame. </param>
        /// <param name="threadIndex"> The index of the thread to find the sample in. </param>
        /// <returns></returns>
        public static bool SetSelection(this IProfilerFrameTimeViewSampleSelectionController controller, string markerNameOrMarkerNamePath, long frameIndex = FrameDataView.invalidOrCurrentFrameIndex, string threadGroupName = CPUProfilerModule.mainThreadGroupName, string threadName = CPUProfilerModule.mainThreadName, ulong threadId = FrameDataView.invalidThreadId)
        {
            var iController = controller as IProfilerFrameTimeViewSampleSelectionControllerInternal;
            if (controller == null || iController == null)
                throw new ArgumentNullException($"{nameof(controller)}", $"The IProfilerFrameTimeViewSampleSelectionController you are setting a selection on can't be null.");

            ProfilerTimeSampleSelection selection;
            List<int> markerIdPath;
            using (CPUOrGPUProfilerModule.setSelectionIntegrityCheckMarker.Auto())
            {
                // this could've come from anywhere, check the inputs first
                if (string.IsNullOrEmpty(markerNameOrMarkerNamePath))
                    throw new ArgumentException($"{nameof(markerNameOrMarkerNamePath)} can't be null or empty. Hint: To clear a selection, use {nameof(IProfilerFrameTimeViewSampleSelectionController.ClearSelection)} instead.");

                if (frameIndex == FrameDataView.invalidOrCurrentFrameIndex)
                {
                    frameIndex = iController.GetActiveVisibleFrameIndexOrLatestFrameForSettingTheSelection();
                }

                var threadIndex = CPUOrGPUProfilerModule.IntegrityCheckFrameAndThreadDataOfSelection(frameIndex, threadGroupName, threadName, ref threadId);

                int lastSlashIndex = markerNameOrMarkerNamePath.LastIndexOf('/');
                string sampleName = lastSlashIndex == -1 ? markerNameOrMarkerNamePath : markerNameOrMarkerNamePath.Substring(lastSlashIndex + 1, markerNameOrMarkerNamePath.Length - (lastSlashIndex + 1));

                if (lastSlashIndex == -1)// no path provided? just find the first sample
                    markerNameOrMarkerNamePath = null;

                int selectedSampleRawIndex = iController.FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView((int)frameIndex, 0, sampleName, out markerIdPath, markerNameOrMarkerNamePath);

                if (selectedSampleRawIndex < 0)
                    return false;

                selection = new ProfilerTimeSampleSelection(frameIndex, threadGroupName, threadName, threadId, selectedSampleRawIndex, sampleName);
            }
            using (CPUOrGPUProfilerModule.setSelectionApplyMarker.Auto())
            {
                // looks good, apply
                selection.frameIndexIsSafe = true;
                iController.SetSelectionWithoutIntegrityChecks(selection, markerIdPath);
                return true;
            }
        }
    }
}
