// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class FrameGCCollectViewController : ViewController
    {
        // Model.
        readonly string m_Title;

        // View.
        Label m_TitleLabel;
        ReadOnlyFloatField m_TotalTimeField;
        ActivityIndicatorOverlay m_ActivityOverlay;

        public FrameGCCollectViewController(string title)
        {
            m_Title = title;
        }

        public void RefreshView(FrameGCCollectModel model)
        {
            m_TotalTimeField.ValueLabel.text = TimeFormatterUtility.FormatTimeNsToMs(model.TotalTimeNs);

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
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("FrameGCCollectView.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "frame-gc-collect-view__dark";
            const string k_UssClass_Light = "frame-gc-collect-view__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_TitleLabel.text = m_Title;
            m_TotalTimeField.Label.text = "Time in frame";
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_TitleLabel = view.Q<Label>("frame-gc-collect-view__title-label");
            m_TotalTimeField = view.Q<ReadOnlyFloatField>("frame-gc-collect-view__total-time-field");
            m_ActivityOverlay = view.Q<ActivityIndicatorOverlay>("frame-gc-collect-view__activity-overlay");
        }
    }
}
