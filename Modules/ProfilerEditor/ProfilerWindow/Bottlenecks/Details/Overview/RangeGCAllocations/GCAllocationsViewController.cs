// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;
using static Unity.Profiling.Editor.UI.IDetailsProvider;

namespace Unity.Profiling.Editor.UI
{
    class GCAllocationsViewController : ViewController
    {
        // Model.
        readonly string m_Title;
        readonly ProfilerWindow m_ProfilerWindow;
        private readonly IDetailsElementBinder m_DetailsBinder;
        GCAllocationsModel? m_Model;

        // View.
        Label m_TitleLabel;
        ReadOnlyFloatField m_TotalField;
        ReadOnlyFloatField m_MaximumCallsField;
        ReadOnlyFloatField m_MaximumSizeField;
        Button m_MaximumCallsButton;
        Button m_MaximumSizeButton;
        Label m_NoDataLabel;
        ActivityIndicatorOverlay m_ActivityOverlay;

        public GCAllocationsViewController(
            string title,
            ProfilerWindow profilerWindow,
            IDetailsElementBinder detailsBinder)
        {
            m_Title = title;
            m_ProfilerWindow = profilerWindow;
            m_DetailsBinder = detailsBinder;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_DetailsBinder.UnbindDetailsElement(m_MaximumCallsField);
                m_DetailsBinder.UnbindDetailsElement(m_MaximumSizeField);
            }
            base.Dispose(disposing);
        }

        public void ReloadData(GCAllocationsModel model)
        {
            m_Model = model;
            if (IsViewLoaded)
                RefreshView();
        }

        public void SetActivityIndicatorVisible(bool visible)
        {
            if (visible)
                m_ActivityOverlay.Show();
            else
                m_ActivityOverlay.Hide();
        }

        public void ShowActivityIndicatorAfterDelay(int delayMs)
        {
            m_ActivityOverlay.ShowAfterDelay(delayMs);
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("GCAllocationsView.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "gc-allocations-view__dark";
            const string k_UssClass_Light = "gc-allocations-view__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_TitleLabel.text = m_Title;
            m_TotalField.Label.text = "Total count";
            m_MaximumCallsField.Label.text = "Frame with highest count";
            m_MaximumSizeField.Label.text = "Frame with highest size";
            m_MaximumCallsButton.clickable.clicked += SelectMaximumCallsFrameInProfilerWindow;
            m_MaximumSizeButton.clickable.clicked += SelectMaximumSizeFrameInProfilerWindow;
            m_NoDataLabel.text = "No markers found";

            if (m_Model.HasValue)
                RefreshView();
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_TitleLabel = view.Q<Label>("gc-allocations-view__title-label");
            m_TotalField = view.Q<ReadOnlyFloatField>("gc-allocations-view__total-field");
            m_MaximumCallsField = view.Q<ReadOnlyFloatField>("gc-allocations-view__max-calls-field");
            m_MaximumCallsButton = view.Q<Button>("gc-allocations-view__max-calls-button");
            m_MaximumSizeField = view.Q<ReadOnlyFloatField>("gc-allocations-view__max-size-field");
            m_MaximumSizeButton = view.Q<Button>("gc-allocations-view__max-size-button");
            m_NoDataLabel = view.Q<Label>("gc-allocations-view__no-data-label");
            m_ActivityOverlay = view.Q<ActivityIndicatorOverlay>("gc-allocations-view__activity-overlay");
        }

        void RefreshView()
        {
            var model = m_Model.Value;

            var hasData = !model.Equals(GCAllocationsModel.Empty);
            if (hasData)
            {
                m_TotalField.ValueLabel.text = $"{model.Total:N0}";

                var maximumCallsInFrame = model.MaximumCallsInFrame;
                m_MaximumCallsField.ValueLabel.text = $"{maximumCallsInFrame.Value:N0}";
                m_MaximumCallsButton.text = FrameIndexFormatterUtility.DisplayStringForFrameIndex(maximumCallsInFrame.FrameIndex);
                m_DetailsBinder.BindDetailsElement(
                    m_MaximumCallsField,
                    new FrameWithHighestGcAllocationsDetailsProvider(m_ProfilerWindow, maximumCallsInFrame.FrameIndex));

                var maximumSizeInFrame = model.MaximumSizeInFrame;
                m_MaximumSizeField.ValueLabel.text = EditorUtility.FormatBytes(System.Convert.ToInt64(maximumSizeInFrame.Value));
                m_MaximumSizeButton.text = FrameIndexFormatterUtility.DisplayStringForFrameIndex(maximumSizeInFrame.FrameIndex);
                m_DetailsBinder.BindDetailsElement(
                    m_MaximumSizeField,
                    new FrameWithHighestGcAllocationsDetailsProvider(m_ProfilerWindow, maximumSizeInFrame.FrameIndex));
            }

            SetNoDataLabelVisible(!hasData);
            SetActivityIndicatorVisible(false);
        }

        void SelectMaximumCallsFrameInProfilerWindow()
        {
            if (m_Model.HasValue == false)
                return;

            var model = m_Model.Value;
            m_ProfilerWindow.selectedFrameIndex = model.MaximumCallsInFrame.FrameIndex;
        }

        void SelectMaximumSizeFrameInProfilerWindow()
        {
            if (m_Model.HasValue == false)
                return;

            var model = m_Model.Value;
            m_ProfilerWindow.selectedFrameIndex = model.MaximumSizeInFrame.FrameIndex;
        }

        void SetNoDataLabelVisible(bool visible)
        {
            UIUtility.SetElementDisplay(m_NoDataLabel, visible);
        }

        internal class FrameWithHighestGcAllocationsDetailsProvider : IDetailsProvider
        {
            private readonly ProfilerWindow m_ProfilerWindow;
            private readonly int m_FrameIndex;

            public FrameWithHighestGcAllocationsDetailsProvider(ProfilerWindow profilerWindow, int frameIndex)
            {
                m_ProfilerWindow = profilerWindow;
                m_FrameIndex = frameIndex;
            }

            public AssistantRequestContext GetAssistantContext(IProfilerCaptureDataService dataService)
            {
                var prompt = $"Provide detailed analysis of the GC allocations.";
                var attachment = new CpuProfilerAssistantController.CpuProfilerContext(
                    capturePath: m_ProfilerWindow.CurrentLoadedCaptureFile,
                    frameRange: m_FrameIndex..m_FrameIndex);

                return new AssistantRequestContext(prompt, attachment);
            }

            public ViewController GetDetailsViewController(IProfilerCaptureDataService dataService)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
