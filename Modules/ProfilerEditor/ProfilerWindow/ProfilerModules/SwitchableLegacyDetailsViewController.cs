// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditorInternal;
using UnityEditorInternal.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    internal class HybridLegacyDetailsViewController : ProfilerModuleViewController
    {
        readonly ProfilerWindow m_ProfilerWindow;
        readonly ProfilerModuleBase m_Module;
        readonly ProfilerViewType m_InitialViewType;

        VisualElement m_LegacyIMGUIView;
        VisualElement m_JobsProfilerView;

        ProfilerModuleViewController m_JobsProfilerDetailsViewController;

        public HybridLegacyDetailsViewController(
            ProfilerWindow profilerWindow,
            ProfilerModuleBase module,
            ProfilerViewType initialViewType)
            : base(profilerWindow)
        {
            m_ProfilerWindow = profilerWindow;
            m_Module = module;
            m_InitialViewType = initialViewType;
        }

        public void SetViewType(ProfilerViewType newViewType)
        {
            var isJobsProfiler = newViewType == ProfilerViewType.TimelineV2;
            if (isJobsProfiler && m_JobsProfilerDetailsViewController == null)
            {
                var jobsProfilerModule = m_ProfilerWindow.jobsProfilerModule;
                m_JobsProfilerDetailsViewController = jobsProfilerModule.CreateDetailsViewController();
                m_JobsProfilerView.Add(m_JobsProfilerDetailsViewController.View);
            }

            UI.UIUtility.SetElementDisplay(m_LegacyIMGUIView, !isJobsProfiler);
            UI.UIUtility.SetElementDisplay(m_JobsProfilerView, isJobsProfiler);
        }

        protected override VisualElement CreateView()
        {
            var view = new VisualElement()
            {
                style = { flexGrow = 1 }
            };

            // Create the legacy UI view.
            m_LegacyIMGUIView = new IMGUIContainer(DrawDetailsViewViaLegacyIMGUIMethods)
            {
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    right = 0,
                    top = 0,
                    bottom = 0,
                }
            };
            view.Add(m_LegacyIMGUIView);

            // Create a container for the Jobs Profiler view with just the toolbar in it for now.
            m_JobsProfilerView = new VisualElement()
            {
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    right = 0,
                    top = 0,
                    bottom = 0,
                }
            };
            view.Add(m_JobsProfilerView);

            var jobProfilerToolbar = new IMGUIContainer(DrawToolbarViaLegacyIMGUIMethods);
            jobProfilerToolbar.style.flexGrow = 0;
            m_JobsProfilerView.Add(jobProfilerToolbar);

            SetViewType(m_InitialViewType);
            return view;
        }

        protected override void Dispose(bool disposing)
        {
            m_JobsProfilerDetailsViewController?.Dispose();
            m_JobsProfilerDetailsViewController = null;

            base.Dispose(disposing);
        }

        void DrawToolbarViaLegacyIMGUIMethods()
        {
            using (Markers.drawLegacyDetailsView.Auto())
            {
                var detailsViewContainer = ProfilerWindow.DetailsViewContainer;
                var resolvedStyle = detailsViewContainer.resolvedStyle;
                var detailsViewBounds = new Rect(0, 0, resolvedStyle.width, resolvedStyle.height);
                var detailsViewToolbarBounds = detailsViewBounds;
                detailsViewToolbarBounds.height = EditorStyles.contentToolbar.CalcHeight(GUIContent.none, 10.0f);
                m_Module.DrawToolbar(detailsViewToolbarBounds);
            }
        }

        void DrawDetailsViewViaLegacyIMGUIMethods()
        {
            using (Markers.drawLegacyDetailsView.Auto())
            {
                var detailsViewContainer = ProfilerWindow.DetailsViewContainer;
                var resolvedStyle = detailsViewContainer.resolvedStyle;
                var detailsViewBounds = new Rect(0, 0, resolvedStyle.width, resolvedStyle.height);
                var detailsViewToolbarBounds = detailsViewBounds;
                detailsViewToolbarBounds.height = EditorStyles.contentToolbar.CalcHeight(GUIContent.none, 10.0f);
                m_Module.DrawToolbar(detailsViewToolbarBounds);

                detailsViewBounds.yMin += detailsViewToolbarBounds.height;
                m_Module.DrawDetailsView(detailsViewBounds);
            }
        }

        static class Markers
        {
            // The name of the marker is "ProfilerWindow.DrawDetailsView" for comparing with old performance tests.
            public static readonly ProfilerMarker drawLegacyDetailsView = new ProfilerMarker("ProfilerWindow.DrawSwitchableDetailsView");
        }
    }
}
