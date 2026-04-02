// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class FrameBottlenecksViewController : ViewController
    {
        internal static class Content
        {
            public static readonly string k_CpuActiveTimeTooltip = L10n.Tr("CPU Active Time is the duration within the frame that the CPU was doing work for.\n\nThis is computed by taking the longest thread duration between the main thread and the render thread, and subtracting the time that thread spent waiting, including waiting for 'present' and 'target FPS'.\n\nIt is possible for this duration to be longer than the 'CPU Time' value shown in the CPU Usage module's Timeline view when the Render Thread took longer than the Main Thread. This is because the Timeline view displays the beginning and end of the frame on the main thread.");
            public static readonly string k_GpuTimeTooltip = L10n.Tr("GPU Time is the duration between when the GPU was sent its first command for the frame and when the GPU completed its work for that frame.");
            public static readonly string k_NoValueText = L10n.Tr("No Value");
        }

        // Model.
        readonly IProfilerPersistentSettingsService m_SettingsService;
        readonly ProfilerWindow m_ProfilerWindow;
        readonly IDetailsElementBinder m_DetailsBinder;
        FrameBottlenecksModel? m_Model;

        // View.
        Label m_CpuLabel;
        Label m_GpuLabel;
        VisualElement m_CpuBar;
        VisualElement m_GpuBar;
        SelectableFrameBottleneckBar m_CpuBarWrapper;
        SelectableFrameBottleneckBar m_GpuBarWrapper;
        Label m_CpuDurationLabel;
        Label m_GpuDurationLabel;
        VisualElement m_TargetFrameDurationIndicator;
        Label m_TargetFrameDurationIndicatorLabel;
        ActivityIndicatorOverlay m_ActivityOverlay;

        public FrameBottlenecksViewController(
            IProfilerPersistentSettingsService settingsService,
            ProfilerWindow profilerWindow,
            IDetailsElementBinder detailsBinder)
        {
            m_SettingsService = settingsService;
            m_ProfilerWindow = profilerWindow;
            m_DetailsBinder = detailsBinder;

            m_SettingsService.TargetFrameDurationChanged += OnTargetFrameDurationChanged;
        }

        public void ReloadData(FrameBottlenecksModel model)
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
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("FrameBottlenecksView.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "frame-bottlenecks-view__dark";
            const string k_UssClass_Light = "frame-bottlenecks-view__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_CpuLabel.tooltip = Content.k_CpuActiveTimeTooltip;
            m_GpuLabel.tooltip = Content.k_GpuTimeTooltip;
            m_CpuBar.tooltip = "Click to select and view details";
            m_GpuBar.tooltip = "Click to select and view details";

            if (m_Model.HasValue)
                RefreshView();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_SettingsService.TargetFrameDurationChanged -= OnTargetFrameDurationChanged;
                m_DetailsBinder.UnbindDetailsElement(m_CpuBarWrapper);
                m_DetailsBinder.UnbindDetailsElement(m_GpuBarWrapper);
            }

            base.Dispose(disposing);
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_CpuLabel = view.Q<Label>("frame-bottlenecks-view__cpu-name-label");
            m_GpuLabel = view.Q<Label>("frame-bottlenecks-view__gpu-name-label");
            m_CpuBar = view.Q<VisualElement>("frame-bottlenecks-view__cpu-duration-bar");
            m_GpuBar = view.Q<VisualElement>("frame-bottlenecks-view__gpu-duration-bar");
            m_CpuDurationLabel = view.Q<Label>("frame-bottlenecks-view__cpu-duration-label");
            m_GpuDurationLabel = view.Q<Label>("frame-bottlenecks-view__gpu-duration-label");
            m_TargetFrameDurationIndicator  = view.Q<VisualElement>("frame-bottlenecks-view__target-frame-duration-indicator");
            m_TargetFrameDurationIndicatorLabel  = view.Q<Label>("frame-bottlenecks-view__target-frame-duration-indicator__label");
            m_ActivityOverlay = view.Q<ActivityIndicatorOverlay>("frame-bottlenecks-view__activity-overlay");

            // Wrap bars in selectable containers
            WrapBarInSelectableContainer(m_CpuBar, out m_CpuBarWrapper);
            WrapBarInSelectableContainer(m_GpuBar, out m_GpuBarWrapper);
        }

        void WrapBarInSelectableContainer(VisualElement bar, out SelectableFrameBottleneckBar wrapper)
        {
            var parent = bar.parent;
            var barIndex = parent.IndexOf(bar);
            parent.Remove(bar);
            wrapper = new SelectableFrameBottleneckBar(bar);
            parent.Insert(barIndex, wrapper);
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
            var largestDurationNs =
                Math.Max(model.CpuDurationNs,
                    Math.Max(model.GpuDurationNs, targetFrameDurationNs));
            var normalizedTargetFrameDuration = (float)targetFrameDurationNs / largestDurationNs;
            m_TargetFrameDurationIndicator.style.width = new Length(normalizedTargetFrameDuration * 100f, LengthUnit.Percent);

            ConfigureBar(
                m_CpuBar,
                m_CpuDurationLabel,
                model.CpuDurationNs,
                largestDurationNs,
                targetFrameDurationNs);
            ConfigureBar(
                m_GpuBar,
                m_GpuDurationLabel,
                model.GpuDurationNs,
                largestDurationNs,
                targetFrameDurationNs);

            var targetFrameDurationText = TimeFormatterUtility.FormatTimeNsToMs(m_SettingsService.TargetFrameDurationNs);
            var targetFramesPerSecond = Mathf.RoundToInt(1e9f / m_SettingsService.TargetFrameDurationNs);
            m_TargetFrameDurationIndicatorLabel.text = $"Target Frame Time\n{targetFrameDurationText}\n<b>({targetFramesPerSecond} FPS)</b>";

            // Bind details providers
            BindDetailsProviders(model.FrameIndex, model.CpuDurationNs, model.GpuDurationNs, targetFrameDurationNs);

            SetActivityIndicatorVisible(false);
        }

        void BindDetailsProviders(int frameIndex, ulong cpuDurationNs, ulong gpuDurationNs, ulong targetFrameDurationNs)
        {
            var cpuDetailsProvider = new FrameBottleneckBarDetailsProvider(
                m_ProfilerWindow,
                FrameBottleneckBarType.Cpu,
                frameIndex,
                cpuDurationNs,
                targetFrameDurationNs);
            m_DetailsBinder.BindDetailsElement(m_CpuBarWrapper, cpuDetailsProvider);

            var gpuDetailsProvider = new FrameBottleneckBarDetailsProvider(
                m_ProfilerWindow,
                FrameBottleneckBarType.Gpu,
                frameIndex,
                gpuDurationNs,
                targetFrameDurationNs);
            m_DetailsBinder.BindDetailsElement(m_GpuBarWrapper, gpuDetailsProvider);
        }

        void ConfigureBar(
            VisualElement bar,
            Label barLabel,
            ulong barDurationNs,
            ulong largestDurationNs,
            ulong targetFrameDurationNs)
        {
            var barDurationNormalized = (float)barDurationNs / largestDurationNs;
            bar.style.width = new StyleLength(new Length(barDurationNormalized * 100f, LengthUnit.Percent));
            barLabel.text = (barDurationNs == 0) ? Content.k_NoValueText : TimeFormatterUtility.FormatTimeNsToMs(barDurationNs);

            const string k_UssClass_DurationBarFillHighlighted = "frame-bottlenecks-view__chart-bar__fill-highlighted";
            if (barDurationNs > targetFrameDurationNs)
                bar.AddToClassList(k_UssClass_DurationBarFillHighlighted);
            else
                bar.RemoveFromClassList(k_UssClass_DurationBarFillHighlighted);
        }
    }
}
