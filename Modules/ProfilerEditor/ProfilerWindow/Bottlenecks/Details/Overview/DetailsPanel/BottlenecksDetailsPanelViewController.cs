// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    // The BottlenecksDetailsPanelViewController displays additional details in a side panel.
    // It accepts a generic VisualElement to display contextual information.
    class BottlenecksDetailsPanelViewController : ViewController
    {
        static class Content
        {
            public static readonly string k_AskAssistantButtonText = L10n.Tr("Ask Assistant");
            public static readonly string k_NoSelectionLabelText = L10n.Tr("Nothing is selected or no details available");
        }

        // Model.
        readonly IProfilerCaptureDataService m_DataService;
        readonly IProfilerPersistentSettingsService m_SettingsService;
        readonly ProfilerWindow m_ProfilerWindow;
        IDetailsProvider m_DetailsProvider;
        ViewController m_DetailsContentViewController;
        IDetailsProvider m_CachedDetailsProviderBeforeViewLoaded;

        // View.
        VisualElement m_DataContainer;
        Label m_NoSelectionLabel;
        Button m_AskAssistantButton;

        public BottlenecksDetailsPanelViewController(
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            ProfilerWindow profilerWindow)
        {
            m_DataService = dataService;
            m_SettingsService = settingsService;
            m_ProfilerWindow = profilerWindow;
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("BottlenecksDetailsPanelView.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "bottlenecks-details-panel-view__dark";
            const string k_UssClass_Light = "bottlenecks-details-panel-view__light";
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
                // If there are no implementations of IProfilerAssistantService, hide the "Ask Assistant" container.
                UIUtility.SetElementDisplay(m_AskAssistantButton.parent, false);
            }
            else
            {
                // Otherwise, show the button and set up its click handler.
                UIUtility.SetElementDisplay(m_AskAssistantButton.parent, true);
                m_AskAssistantButton.clicked += OnAskAssistantButtonClicked;
            }

            // Apply cached provider if one was set before view loaded, otherwise show "no selection" state.
            SetDetailsProvider(m_CachedDetailsProviderBeforeViewLoaded);
            m_CachedDetailsProviderBeforeViewLoaded = null;
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_DataContainer = view.Q<VisualElement>("bottlenecks-details-panel-view__-container");
            m_NoSelectionLabel = view.Q<Label>("bottlenecks-details-panel-view__no-selection-label");
            m_AskAssistantButton = view.Q<Button>("bottlenecks-details-panel-view__ask-assistant-button");

            // Set localized text
            if (m_NoSelectionLabel != null)
                m_NoSelectionLabel.text = Content.k_NoSelectionLabelText;

            // The button contains a label as a child, find and set its text
            var buttonLabel = m_AskAssistantButton.Q<Label>();
            if (buttonLabel != null)
                buttonLabel.text = Content.k_AskAssistantButtonText;
        }

        public void SetDetailsProvider(IDetailsProvider detailsProvider)
        {
            if (!IsViewLoaded)
            {
                m_CachedDetailsProviderBeforeViewLoaded = detailsProvider;
                return;
            }

            // Clear any existing view container content.
            if (m_DetailsContentViewController != null)
            {
                RemoveChild(m_DetailsContentViewController);
                m_DetailsContentViewController.Dispose();
                m_DetailsContentViewController = null;
            }

            m_DetailsProvider = null;
            m_DataContainer.Clear();

            // Create a view for the details content if possible.
            if (detailsProvider != null)
            {
                var detailsContentViewController = detailsProvider.GetDetailsViewController(m_DataService);
                if (detailsContentViewController != null)
                {
                    AddChild(detailsContentViewController);
                    m_DataContainer.Add(detailsContentViewController.View);

                    // Store provider and the view controller.
                    m_DetailsProvider = detailsProvider;
                    m_DetailsContentViewController = detailsContentViewController;

                    UIUtility.SetElementDisplay(m_NoSelectionLabel, false);
                    UIUtility.SetElementDisplay(m_DataContainer.parent, true);

                    return;
                }
            }

            // Otherwise show "no selection" state.
            UIUtility.SetElementDisplay(m_NoSelectionLabel, true);
            UIUtility.SetElementDisplay(m_DataContainer.parent, false);
        }

        void OnAskAssistantButtonClicked()
        {
            IDetailsProvider.AssistantRequestContext context = m_DetailsProvider.GetAssistantContext(m_DataService);

            // Invoke profiler assistant
            var layout = m_AskAssistantButton.localBound;
            var worldPos = m_AskAssistantButton.LocalToWorld(new Vector2());
            var screenPos = GUIUtility.GUIToScreenPoint(worldPos);
            var screenRect = new Rect(screenPos, layout.size);

            var attachment = new CpuProfilerAssistantController.CpuProfilerContext(m_ProfilerWindow.CurrentLoadedCaptureFile,
                context.Attachment.FrameRange, context.Attachment.ThreadName, context.Attachment.MarkerIdPath, context.Attachment.MarkerName);
            string prompt = context.Prompt;

            ((UnityEditorInternal.IProfilerWindowController)m_ProfilerWindow).RequestCpuProfilerAssistance(screenRect, attachment, prompt);
        }
    }
}
