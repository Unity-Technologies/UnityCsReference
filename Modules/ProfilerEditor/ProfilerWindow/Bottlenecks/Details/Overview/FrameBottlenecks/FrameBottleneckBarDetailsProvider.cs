// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class FrameBottleneckBarDetailsProvider : IDetailsProvider
    {
        readonly ProfilerWindow m_ProfilerWindow;
        readonly FrameBottleneckBarType m_BarType;
        readonly int m_FrameIndex;
        readonly ulong m_DurationNs;
        readonly ulong m_TargetFrameDurationNs;

        public FrameBottleneckBarDetailsProvider(
            ProfilerWindow profilerWindow,
            FrameBottleneckBarType barType,
            int frameIndex,
            ulong durationNs,
            ulong targetFrameDurationNs)
        {
            m_ProfilerWindow = profilerWindow;
            m_BarType = barType;
            m_FrameIndex = frameIndex;
            m_DurationNs = durationNs;
            m_TargetFrameDurationNs = targetFrameDurationNs;
        }

        public IDetailsProvider.AssistantRequestContext GetAssistantContext(IProfilerCaptureDataService dataService)
        {
            var prompt = m_BarType switch
            {
                FrameBottleneckBarType.Cpu => "Provide detailed analysis of the CPU Active Time for this frame",
                FrameBottleneckBarType.Gpu => "Provide detailed analysis of the GPU Time for this frame",
                _ => throw new ArgumentOutOfRangeException()
            };

            var attachment = new CpuProfilerAssistantController.CpuProfilerContext(
                m_ProfilerWindow.CurrentLoadedCaptureFile,
                m_FrameIndex..m_FrameIndex,
                threadName: "",
                markerIdPath: "",
                markerName: "");

            return new IDetailsProvider.AssistantRequestContext(prompt, attachment);
        }

        public ViewController GetDetailsViewController(IProfilerCaptureDataService dataService)
        {
            return new FrameBottleneckBarDetailsPanelViewController(m_BarType, m_ProfilerWindow, m_DurationNs);
        }

        class FrameBottleneckBarDetailsPanelViewController : ViewController
        {
            static class Content
            {
                public static readonly string k_BarName_Cpu = L10n.Tr("CPU Active Time");
                public static readonly string k_BarName_Gpu = L10n.Tr("GPU Time");
            }

            readonly FrameBottleneckBarType m_BarType;
            readonly ProfilerWindow m_ProfilerWindow;
            readonly ulong m_DurationNs;

            public FrameBottleneckBarDetailsPanelViewController(
                FrameBottleneckBarType barType,
                ProfilerWindow profilerWindow,
                ulong durationNs)
            {
                m_BarType = barType;
                m_ProfilerWindow = profilerWindow;
                m_DurationNs = durationNs;
            }

            protected override VisualElement LoadView()
            {
                var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("FrameBottleneckBarDetailsView.uxml");
                if (view == null)
                    throw new InvalidViewDefinedInUxmlException();

                const string k_UssClass_Dark = "frame-bottleneck-bar-details__dark";
                const string k_UssClass_Light = "frame-bottleneck-bar-details__light";
                var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
                view.AddToClassList(themeUssClass);

                return view;
            }

            protected override void ViewLoaded()
            {
                base.ViewLoaded();

                SetupContent();
            }

            void SetupContent()
            {
                // Set bar name
                var nameLabel = View.Q<Label>("frame-bottleneck-bar-details__name");
                if (nameLabel != null)
                {
                    nameLabel.text = m_BarType switch
                    {
                        FrameBottleneckBarType.Cpu => Content.k_BarName_Cpu,
                        FrameBottleneckBarType.Gpu => Content.k_BarName_Gpu,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }

                // Set bar value
                var valueLabel = View.Q<Label>("frame-bottleneck-bar-details__value");
                if (valueLabel != null)
                {
                    valueLabel.text = TimeFormatterUtility.FormatTimeNsToMs(m_DurationNs);
                }

                // Set description
                var descriptionLabel = View.Q<Label>("frame-bottleneck-bar-details__description");
                if (descriptionLabel != null)
                {
                    descriptionLabel.text = m_BarType switch
                    {
                        FrameBottleneckBarType.Cpu => FrameBottlenecksViewController.Content.k_CpuActiveTimeTooltip,
                        FrameBottleneckBarType.Gpu => FrameBottlenecksViewController.Content.k_GpuTimeTooltip,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }

                // Set up view button
                var viewButton = View.Q<Button>("frame-bottleneck-bar-details__view-button");
                if (viewButton != null)
                {
                    viewButton.clicked += OnViewButtonClicked;
                }
            }

            void OnViewButtonClicked()
            {
                switch (m_BarType)
                {
                    case FrameBottleneckBarType.Cpu:
                    {
                        // Open the CPU module's Timeline view.
                        var cpuModule = m_ProfilerWindow.GetProfilerModule<UnityEditorInternal.Profiling.CPUProfilerModule>(UnityEngine.Profiling.ProfilerArea.CPU);
                        m_ProfilerWindow.selectedModule = cpuModule;
                        cpuModule.ViewType = UnityEditorInternal.ProfilerViewType.Timeline;

                        const string k_LinkDescription_OpenCpuTimeline = "Open CPU Timeline";
                        UnityEditor.Profiling.Analytics.ProfilerWindowAnalytics.SendBottleneckLinkSelectedEvent(
                            k_LinkDescription_OpenCpuTimeline);

                        break;
                    }
                    case FrameBottleneckBarType.Gpu:
                    {
                        // Open the Frame Debugger.
                        FrameDebuggerWindow.OpenWindow();

                        const string k_LinkDescription_OpenFrameDebugger = "Open Frame Debugger";
                        UnityEditor.Profiling.Analytics.ProfilerWindowAnalytics.SendBottleneckLinkSelectedEvent(
                            k_LinkDescription_OpenFrameDebugger);

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    enum FrameBottleneckBarType
    {
        Cpu,
        Gpu
    }
}
