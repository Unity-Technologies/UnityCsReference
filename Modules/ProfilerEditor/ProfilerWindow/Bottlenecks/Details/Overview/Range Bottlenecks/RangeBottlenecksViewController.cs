// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class RangeBottlenecksViewController : ViewController
    {
        // Model.
        readonly IProfilerPersistentSettingsService m_SettingsService;
        RangeBottlenecksModel? m_Model;

        // View.
        Label m_TitleLabel;
        Label m_CpuLabel;
        Label m_GpuLabel;
        VisualElement m_CpuBar;
        VisualElement m_GpuBar;
        ActivityIndicatorOverlay m_ActivityOverlay;

        public RangeBottlenecksViewController(IProfilerPersistentSettingsService settingsService)
        {
            m_SettingsService = settingsService;

            m_SettingsService.TargetFrameDurationChanged += OnTargetFrameDurationChanged;
        }

        public void ReloadData(RangeBottlenecksModel model)
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
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("RangeBottlenecksView.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "range-bottlenecks-view__dark";
            const string k_UssClass_Light = "range-bottlenecks-view__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();
            m_TitleLabel.text = "Bottlenecks";

            if (m_Model.HasValue)
                RefreshView();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_SettingsService.TargetFrameDurationChanged -= OnTargetFrameDurationChanged;
            }

            base.Dispose(disposing);
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_TitleLabel = view.Q<Label>("range-bottlenecks-view__title-label");
            m_CpuLabel = view.Q<Label>("range-bottlenecks-view__cpu-label");
            m_GpuLabel = view.Q<Label>("range-bottlenecks-view__gpu-label");
            m_CpuBar = view.Q<VisualElement>("range-bottlenecks-view__cpu-bar");
            m_GpuBar = view.Q<VisualElement>("range-bottlenecks-view__gpu-bar");
            m_ActivityOverlay = view.Q<ActivityIndicatorOverlay>("range-bottlenecks-view__activity-overlay");
        }

        void OnTargetFrameDurationChanged()
        {
            if (IsViewLoaded == false)
                return;

            if (m_Model.HasValue == false)
                return;

            RefreshView();
        }

        void RefreshView()
        {
            if (m_Model.HasValue == false)
                return;

            var model = m_Model.Value;
            var targetFrameDurationNs = m_SettingsService.TargetFrameDurationNs;
            var cpuOverBudgetPercentage = model.ComputePercentageOfCpuValuesOverBudget(targetFrameDurationNs);
            var gpuOverBudgetPercentage = model.ComputePercentageOfGpuValuesOverBudget(targetFrameDurationNs);
            m_CpuLabel.text = $"CPU ({cpuOverBudgetPercentage}% of frames over target)";
            m_GpuLabel.text = $"GPU ({gpuOverBudgetPercentage}% of frames over target)";
            m_CpuBar.style.width = new StyleLength(new Length(cpuOverBudgetPercentage, LengthUnit.Percent));
            m_GpuBar.style.width = new StyleLength(new Length(gpuOverBudgetPercentage, LengthUnit.Percent));

            SetActivityIndicatorVisible(false);
        }
    }
}
