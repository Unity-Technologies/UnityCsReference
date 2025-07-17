// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class FrameGCAllocationsViewController : ViewController
    {
        // Model.
        readonly string m_Title;

        // View.
        Label m_TitleLabel;
        ReadOnlyFloatField m_TotalCountField;
        ReadOnlyFloatField m_TotalSizeField;
        Label m_NoDataLabel;
        ActivityIndicatorOverlay m_ActivityOverlay;

        public FrameGCAllocationsViewController(string title)
        {
            m_Title = title;
        }

        public void RefreshView(FrameGCAllocationsModel model)
        {
            var hasData = !model.Equals(FrameGCAllocationsModel.Empty);
            if (hasData)
            {
                m_TotalCountField.ValueLabel.text = $"{model.TotalCount:N0}";
                m_TotalSizeField.ValueLabel.text = EditorUtility.FormatBytes(model.TotalSize);
            }

            SetNoDataLabelVisible(!hasData);
            SetActivityIndicatorVisible(false);
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
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("FrameGCAllocationsView.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "frame-gc-allocations-view__dark";
            const string k_UssClass_Light = "frame-gc-allocations-view__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_TitleLabel.text = m_Title;
            m_TotalCountField.Label.text = "Count in frame";
            m_TotalSizeField.Label.text = "Size in frame";
            m_NoDataLabel.text = "No markers found";
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_TitleLabel = view.Q<Label>("frame-gc-allocations-view__title-label");
            m_TotalCountField = view.Q<ReadOnlyFloatField>("frame-gc-allocations-view__total-count-field");
            m_TotalSizeField = view.Q<ReadOnlyFloatField>("frame-gc-allocations-view__total-size-field");
            m_NoDataLabel = view.Q<Label>("frame-gc-allocations-view__no-data-label");
            m_ActivityOverlay = view.Q<ActivityIndicatorOverlay>("frame-gc-allocations-view__activity-overlay");
        }

        void SetNoDataLabelVisible(bool visible)
        {
            UIUtility.SetElementDisplay(m_NoDataLabel, visible);
        }
    }
}
