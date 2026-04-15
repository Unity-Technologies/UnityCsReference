// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class SystemImpactDetailsProvider : IDetailsProvider
    {
        readonly ProfilerWindow m_ProfilerWindow;
        readonly SystemsImpactModel.SystemImpact m_SystemImpact;
        readonly Range m_FrameRange;

        public SystemImpactDetailsProvider(
            ProfilerWindow profilerWindow,
            SystemsImpactModel.SystemImpact systemImpact,
            Range frameRange)
        {
            m_ProfilerWindow = profilerWindow;
            m_SystemImpact = systemImpact;
            m_FrameRange = frameRange;
        }

        public IDetailsProvider.AssistantRequestContext GetAssistantContext(IProfilerCaptureDataService dataService)
        {
            var prompt = $"Provide detailed analysis of the system '{m_SystemImpact.Name}' impact on CPU time";
            var attachment = new CpuProfilerAssistantController.CpuProfilerContext(
                m_ProfilerWindow.CurrentLoadedCaptureFile,
                m_FrameRange,
                threadName: "",
                markerIdPath: "",
                markerName: m_SystemImpact.Name);
            return new IDetailsProvider.AssistantRequestContext(prompt, attachment);
        }

        public ViewController GetDetailsViewController(IProfilerCaptureDataService dataService)
        {
            return new SystemImpactDetailsPanelViewController(m_SystemImpact);
        }

        class SystemImpactDetailsPanelViewController : ViewController
        {
            readonly SystemsImpactModel.SystemImpact m_SystemImpact;

            public SystemImpactDetailsPanelViewController(SystemsImpactModel.SystemImpact systemImpact)
            {
                m_SystemImpact = systemImpact;
            }

            protected override VisualElement LoadView()
            {
                var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("FrameBottleneckBarDetailsView.uxml");
                if (view == null)
                    throw new InvalidViewDefinedInUxmlException();

                return view;
            }

            protected override void ViewLoaded()
            {
                base.ViewLoaded();
                SetupContent();
            }

            void SetupContent()
            {
                var nameLabel = View.Q<Label>("frame-bottleneck-bar-details__name");
                if (nameLabel != null)
                    nameLabel.text = m_SystemImpact.Name;

                var valueLabel = View.Q<Label>("frame-bottleneck-bar-details__value");
                if (valueLabel != null)
                    valueLabel.text = TimeFormatterUtility.FormatTimeNsToMs(m_SystemImpact.DurationNs);

                // Hide description and view button — they are not applicable for system impact items.
                var descriptionLabel = View.Q<Label>("frame-bottleneck-bar-details__description");
                if (descriptionLabel != null)
                    UIUtility.SetElementDisplay(descriptionLabel, false);

                var viewButton = View.Q<Button>("frame-bottleneck-bar-details__view-button");
                if (viewButton != null)
                    UIUtility.SetElementDisplay(viewButton, false);
            }
        }
    }
}
