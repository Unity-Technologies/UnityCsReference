// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    // Displays a box-and-whisker graph alongside its values, as defined by a BoxPlotModel.
    class BoxPlotViewController : ViewController
    {
        // Model.
        readonly string m_Title;
        readonly ProfilerWindow m_ProfilerWindow;
        BoxPlotModel? m_Model;

        // View.
        Label m_TitleLabel;
        ReadOnlyFloatField m_MaximumField;
        ReadOnlyFloatField m_MedianField;
        ReadOnlyFloatField m_MinimumField;
        BoxPlotGraph m_Graph;
        Button m_MaximumButton;
        Button m_MedianButton;
        Button m_MinimumButton;
        Label m_NoDataLabel;
        ActivityIndicatorOverlay m_ActivityOverlay;

        public BoxPlotViewController(
            string title,
            ProfilerWindow profilerWindow)
        {
            m_Title = title;
            m_ProfilerWindow = profilerWindow;
        }

        public void ReloadData(BoxPlotModel model)
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
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("BoxPlotView.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "box-plot-view__dark";
            const string k_UssClass_Light = "box-plot-view__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_TitleLabel.text = m_Title;
            m_MaximumField.Label.text = "Max";
            m_MedianField.Label.text = "Median";
            m_MinimumField.Label.text = "Min";
            m_MaximumButton.clickable.clicked += SelectMaximumFrameInProfilerWindow;
            m_MedianButton.clickable.clicked += SelectMedianFrameInProfilerWindow;
            m_MinimumButton.clickable.clicked += SelectMinimumFrameInProfilerWindow;
            m_NoDataLabel.text = "No markers found";

            if (m_Model.HasValue)
                RefreshView();
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_TitleLabel = view.Q<Label>("box-plot-view__title-label");
            m_MaximumField = view.Q<ReadOnlyFloatField>("box-plot-view__maximum-field");
            m_MedianField = view.Q<ReadOnlyFloatField>("box-plot-view__median-field");
            m_MinimumField = view.Q<ReadOnlyFloatField>("box-plot-view__minimum-field");
            m_Graph = view.Q<BoxPlotGraph>("box-plot-view__graph");
            m_MaximumButton = view.Q<Button>("box-plot-view__maximum-button");
            m_MedianButton = view.Q<Button>("box-plot-view__median-button");
            m_MinimumButton = view.Q<Button>("box-plot-view__minimum-button");
            m_NoDataLabel = view.Q<Label>("box-plot-view__no-data-label");
            m_ActivityOverlay = view.Q<ActivityIndicatorOverlay>("box-plot-view__activity-overlay");
        }

        void RefreshView()
        {
            var model = m_Model.Value;

            var hasData = !model.Equals(BoxPlotModel.Empty);
            if (hasData)
            {
                m_MaximumField.ValueLabel.text = TimeFormatterUtility.FormatTimeNsToMs(model.Maximum.Value);
                m_MedianField.ValueLabel.text = TimeFormatterUtility.FormatTimeNsToMs(model.Median.Value);
                m_MinimumField.ValueLabel.text = TimeFormatterUtility.FormatTimeNsToMs(model.Minimum.Value);

                m_Graph.ReloadData(model);

                m_MaximumButton.text = FrameIndexFormatterUtility.DisplayStringForFrameIndex(model.Maximum.FrameIndex);
                m_MedianButton.text = FrameIndexFormatterUtility.DisplayStringForFrameIndex(model.Median.FrameIndex);
                m_MinimumButton.text = FrameIndexFormatterUtility.DisplayStringForFrameIndex(model.Minimum.FrameIndex);
            }

            SetNoDataLabelVisible(!hasData);
            SetActivityIndicatorVisible(false);
        }

        void SelectMaximumFrameInProfilerWindow()
        {
            if (m_Model.HasValue == false)
                return;

            var model = m_Model.Value;
            m_ProfilerWindow.selectedFrameIndex = model.Maximum.FrameIndex;
        }

        void SelectMedianFrameInProfilerWindow()
        {
            if (m_Model.HasValue == false)
                return;

            var model = m_Model.Value;
            m_ProfilerWindow.selectedFrameIndex = model.Median.FrameIndex;
        }

        void SelectMinimumFrameInProfilerWindow()
        {
            if (m_Model.HasValue == false)
                return;

            var model = m_Model.Value;
            m_ProfilerWindow.selectedFrameIndex = model.Minimum.FrameIndex;
        }

        void SetNoDataLabelVisible(bool visible)
        {
            UIUtility.SetElementDisplay(m_NoDataLabel, visible);
        }
    }
}
