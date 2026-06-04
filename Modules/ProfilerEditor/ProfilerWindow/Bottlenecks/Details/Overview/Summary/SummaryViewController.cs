// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    abstract class SummaryViewController : ViewController, TopMarkersViewController.IResponder
    {
        const string k_SelectionAssistantPrompt = "Why do I have spikes in the profiler capture?";
        const string k_SingleFrameAssistantPrompt = "Why is the selected frame slow?";

        // Model.
        protected Range m_SelectedRange;
        protected readonly IProfilerCaptureDataService m_DataService;
        protected readonly IProfilerPersistentSettingsService m_SettingsService;
        protected readonly ProfilerWindow m_ProfilerWindow;
        protected readonly IResponder m_Responder;
        protected readonly IDetailsElementBinder m_DetailsBinder;

        // View.
        Button m_AskAssistantButton;
        protected VisualElement m_TopSection;
        protected VisualElement m_BottlenecksContainer;
        protected VisualElement m_SystemsImpactContainer;
        protected VisualElement m_FrameTimesContainer;
        protected VisualElement m_AllocationsContainer;
        protected Label m_NoDataLabel;

        // Children.
        protected SystemsImpactViewController m_SystemsImpactViewController;
        protected FrameTimesSectionViewController m_FrameTimesSectionViewController;
        protected AllocationsSectionViewController m_AllocationsSectionViewController;

        public SummaryViewController(
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            ProfilerWindow profilerWindow,
            IResponder responder,
            IDetailsElementBinder detailsBinder)
        {
            m_DataService = dataService;
            m_SettingsService = settingsService;
            m_ProfilerWindow = profilerWindow;
            m_Responder = responder;
            m_DetailsBinder = detailsBinder;
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("SummaryView.uxml") ?? throw new InvalidViewDefinedInUxmlException();
            const string k_UssClass_Dark = "summary-view__dark";
            const string k_UssClass_Light = "summary-view__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            if (!((UnityEditorInternal.IProfilerWindowController)m_ProfilerWindow).CpuProfilerAssistantSupported)
            {
                // If there are no implementations of IProfilerAssistantService, hide the "Ask Assistant" button.
                m_AskAssistantButton.visible = false;
            }
            else
            {
                // Otherwise, show the button and set up its click handler to launch all assistant services.
                m_AskAssistantButton.visible = true;
                m_AskAssistantButton.clickable.clicked += () =>
                {
                    var layout = m_AskAssistantButton.localBound;
                    var worldPos = m_AskAssistantButton.LocalToWorld(new Vector2());
                    var screenPos = GUIUtility.GUIToScreenPoint(worldPos);
                    var screenRect = new Rect(screenPos, layout.size);

                    var targetFrameTime = ProfilerUserSettings.targetFramesPerSecond > 0 ? 1000f / ProfilerUserSettings.targetFramesPerSecond : -1f;
                    var attachment = new CpuProfilerAssistantController.CpuProfilerContext(m_ProfilerWindow.CurrentLoadedCaptureFile,
                        m_SelectedRange,
                        targetFrameTime: targetFrameTime);

                    string prompt = m_SelectedRange.Start.Value == m_SelectedRange.End.Value
                        ? k_SingleFrameAssistantPrompt
                        : k_SelectionAssistantPrompt;

                    ((UnityEditorInternal.IProfilerWindowController)m_ProfilerWindow).RequestCpuProfilerAssistance(screenRect, attachment, prompt);

                    const string k_LinkDescription_AskAssistant= "Ask Assistant";
                    UnityEditor.Profiling.Analytics.ProfilerWindowAnalytics.SendBottleneckLinkSelectedEvent(k_LinkDescription_AskAssistant);
                };
            }
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_AskAssistantButton = view.Q<Button>("summary-view__ask-assistant-button");
            m_TopSection = view.Q<VisualElement>("summary-view__top-section");
            m_BottlenecksContainer = view.Q<VisualElement>("summary-view__bottlenecks-container");
            m_SystemsImpactContainer = view.Q<VisualElement>("summary-view__systems-impact-container");
            m_FrameTimesContainer = view.Q<VisualElement>("summary-view__frame-times-container");
            m_AllocationsContainer = view.Q<VisualElement>("summary-view__allocations-container");
            m_NoDataLabel = view.Q<Label>("summary-view__no-data-label");
        }

        void TopMarkersViewController.IResponder.OnMarkerSelected(
            TopMarkersModel.Marker marker,
            TopMarkersViewController.Action action)
        {
            m_Responder?.OnTopMarkerSelected(marker, action);
        }

        public interface IResponder
        {
            void OnTopMarkerSelected(TopMarkersModel.Marker marker, TopMarkersViewController.Action action);
        }
    }
}
