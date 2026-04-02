// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine.UIElements;
using static Unity.Profiling.Editor.UI.TopMarkersModel;

namespace Unity.Profiling.Editor.UI
{
    class TopMarkerDetailsProvider : IDetailsProvider
    {
        private readonly ProfilerWindow m_ProfilerWindow;
        private readonly Marker m_Marker;
        private readonly TopMarkersViewController.IResponder m_Responder;

        public TopMarkerDetailsProvider(
            ProfilerWindow profilerWindow,
            Marker marker,
            TopMarkersViewController.IResponder responder)
        {
            m_ProfilerWindow = profilerWindow;
            m_Marker = marker;
            m_Responder = responder;
        }

        public IDetailsProvider.AssistantRequestContext GetAssistantContext(IProfilerCaptureDataService dataService)
        {
            var gcAnalysis = m_Marker.Units != Marker.Unit.TimeNanoseconds;
            var prompt = gcAnalysis
                ? $"Provide detailed analysis of the GC Allocations usage for the marker"
                : $"Provide detailed analysis of the CPU time for the marker";

            // Get the full marker id path to the marker.
            using var threadData = dataService.GetHierarchyFrameDataView(m_Marker.FrameIndex, m_Marker.ThreadIndex,
                HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName | HierarchyFrameDataView.ViewModes.InvertHierarchy,
                gcAnalysis ? HierarchyFrameDataView.columnGcMemory : HierarchyFrameDataView.columnSelfTime,
                false);

            // Find the marker itself
            var children = new List<int>();
            var currentItem = HierarchyFrameDataView.invalidSampleId;
            threadData.GetItemChildren(threadData.GetRootItemID(), children);
            foreach (var child in children)
            {
                if (threadData.GetItemMarkerID(child) == m_Marker.MarkerId)
                {
                    currentItem = child;
                    break;
                }
            }

            if (currentItem == HierarchyFrameDataView.invalidSampleId)
            {
                throw new InvalidOperationException("Could not find marker in hierarchy data view.");
            }

            var threadName = threadData.threadName;
            // Iterate to find the marker and build the path.
            var markerIdPath = new List<int>();
            while (currentItem != HierarchyFrameDataView.invalidSampleId)
            {
                markerIdPath.Add(threadData.GetItemMarkerID(currentItem));
                threadData.GetItemChildren(currentItem, children);
                currentItem = (children.Count > 0) ? children[0] : HierarchyFrameDataView.invalidSampleId;
            }
            // Invert the path to go from root to leaf.
            markerIdPath.Reverse();
            var markerPathString = string.Join("/", markerIdPath);

            var attachement = new CpuProfilerAssistantController.CpuProfilerContext(
                m_ProfilerWindow.CurrentLoadedCaptureFile,
                m_Marker.FrameIndex..m_Marker.FrameIndex,
                threadName,
                markerPathString,
                m_Marker.Name);

            return new IDetailsProvider.AssistantRequestContext(prompt, attachement);
        }

        public ViewController GetDetailsViewController(IProfilerCaptureDataService dataService)
        {
            return new TopMarkerDetailsPanelViewController(m_Marker, dataService, m_Responder);
        }

        private class TopMarkerDetailsPanelViewController : ViewController
        {
            private readonly Marker m_Marker;
            private readonly IProfilerCaptureDataService m_ProfilerCaptureDataService;
            private readonly TopMarkersViewController.IResponder m_Responder;

            private MarkerDetailsPanelViewController m_DetailsPanelViewController;

            public TopMarkerDetailsPanelViewController(
                Marker marker,
                IProfilerCaptureDataService dataService,
                TopMarkersViewController.IResponder responder)
            {
                m_Marker = marker;
                m_ProfilerCaptureDataService = dataService;
                m_Responder = responder;
            }

            protected override VisualElement LoadView()
            {
                // Create OWN root view (distinct from child's view)
                var container = new VisualElement { style = { flexGrow = 1 } };
                return container;
            }

            protected override void ViewLoaded()
            {
                base.ViewLoaded();

                // Create child and add its view to the container
                m_DetailsPanelViewController = new MarkerDetailsPanelViewController(m_Marker, m_ProfilerCaptureDataService, OnViewButtonClicked);
                AddChild(m_DetailsPanelViewController);
                View.Add(m_DetailsPanelViewController.View);
            }

            void OnViewButtonClicked()
            {
                m_Responder?.OnMarkerSelected(m_Marker, TopMarkersViewController.Action.SwitchToCpuModule);
            }
        }
    }
}
