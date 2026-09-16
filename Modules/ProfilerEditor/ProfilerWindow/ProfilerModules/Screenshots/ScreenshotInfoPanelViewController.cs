// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling.Editor.UI;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditorInternal.Profiling
{
    // Right-hand details panel for the Screenshots module: the selected frame number, the resolution
    // and source of the displayed screenshot, an Info help box when that screenshot is older than the
    // selected frame, and CPU/GPU frame timings that turn red when over the frame budget.
    internal class ScreenshotInfoPanelViewController : ViewController
    {
        const string k_UxmlResourceName = "Profiler/Screenshots/ScreenshotInfoPanelView.uxml";

        const string k_UxmlIdentifier_FrameNumberLabel = "screenshot-info-panel-view__frame-number";
        const string k_UxmlIdentifier_ResolutionLabel = "screenshot-info-panel-view__resolution-label";
        const string k_UxmlIdentifier_ResolutionValue = "screenshot-info-panel-view__resolution-value";
        const string k_UxmlIdentifier_SourceLabel = "screenshot-info-panel-view__source-label";
        const string k_UxmlIdentifier_SourceValue = "screenshot-info-panel-view__source-value";
        const string k_UxmlIdentifier_StaleHelpBoxContainer = "screenshot-info-panel-view__stale-help-box-container";
        const string k_UxmlIdentifier_TimingsHeader = "screenshot-info-panel-view__timings-header";
        const string k_UxmlIdentifier_CpuLabel = "screenshot-info-panel-view__cpu-label";
        const string k_UxmlIdentifier_CpuValue = "screenshot-info-panel-view__cpu-value";
        const string k_UxmlIdentifier_GpuLabel = "screenshot-info-panel-view__gpu-label";
        const string k_UxmlIdentifier_GpuValue = "screenshot-info-panel-view__gpu-value";

        const string k_UssClass_StaleHelpBox = "screenshot-info-panel-view__stale-help-box";
        internal const string k_UssClass_TimingValueOverBudget = "screenshot-info-panel-view__timing-value--over-budget";

        // Shown in value cells where no data is available (e.g. no screenshot resolution yet).
        const string k_EmptyValue = "-";

        static class Content
        {
            public static readonly string k_ResolutionLabel = L10n.Tr("Resolution");
            public static readonly string k_SourceLabel = L10n.Tr("Source");
            public static readonly string k_TimingsHeader = L10n.Tr("Frame timings");
            public static readonly string k_CpuLabel = L10n.Tr("CPU active time");
            public static readonly string k_GpuLabel = L10n.Tr("GPU time");
            public static readonly string k_NoValue = L10n.Tr("No value");
            public static readonly string k_NoScreenshot = L10n.Tr("No screenshot");
            public static readonly string k_ThisFrame = L10n.Tr("This frame");
            public static readonly string k_NoFrameSelected = L10n.Tr("No frame selected");
            public static readonly string k_FrameNumberFormat = L10n.Tr("Frame {0}");
            public static readonly string k_ResolutionFormat = L10n.Tr("{0}x{1}");
            public static readonly string k_StaleHelpOneFrame = L10n.Tr("Showing the screenshot from frame {0} (1 frame ago). No screenshot was captured on the selected frame.");
            public static readonly string k_StaleHelpManyFrames = L10n.Tr("Showing the screenshot from frame {0} ({1} frames ago). No screenshot was captured on the selected frame.");
            public static readonly string k_OverBudgetTooltipFormat = L10n.Tr("Exceeds target frame duration ({0})");
        }

        readonly ScreenshotIndexCatalogue m_Catalogue;

        Label m_FrameNumberLabel;
        Label m_ResolutionValueLabel;
        Label m_SourceValueLabel;
        HelpBox m_StaleHelpBox;
        Label m_CpuValueLabel;
        Label m_GpuValueLabel;

        public ScreenshotInfoPanelViewController(ScreenshotIndexCatalogue catalogue)
        {
            m_Catalogue = catalogue;
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlResourceName);
            if (view == null)
                throw new InvalidOperationException($"Failed to load UXML for {nameof(ScreenshotInfoPanelViewController)}");

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_FrameNumberLabel = View.Q<Label>(k_UxmlIdentifier_FrameNumberLabel);
            m_ResolutionValueLabel = View.Q<Label>(k_UxmlIdentifier_ResolutionValue);
            m_SourceValueLabel = View.Q<Label>(k_UxmlIdentifier_SourceValue);
            m_CpuValueLabel = View.Q<Label>(k_UxmlIdentifier_CpuValue);
            m_GpuValueLabel = View.Q<Label>(k_UxmlIdentifier_GpuValue);

            var staleHelpBoxContainer = View.Q<VisualElement>(k_UxmlIdentifier_StaleHelpBoxContainer);
            if (m_FrameNumberLabel == null || m_ResolutionValueLabel == null || m_SourceValueLabel == null
                || m_CpuValueLabel == null || m_GpuValueLabel == null || staleHelpBoxContainer == null)
                throw new InvalidOperationException($"Failed to find required elements in UXML for {nameof(ScreenshotInfoPanelViewController)}");

            // HelpBox is created in code rather than UXML for compatibility, then hidden until the
            // displayed screenshot is older than the selected frame.
            m_StaleHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            m_StaleHelpBox.AddToClassList(k_UssClass_StaleHelpBox);
            m_StaleHelpBox.style.display = DisplayStyle.None;
            staleHelpBoxContainer.Add(m_StaleHelpBox);

            SetStaticLabelText(k_UxmlIdentifier_ResolutionLabel, Content.k_ResolutionLabel);
            SetStaticLabelText(k_UxmlIdentifier_SourceLabel, Content.k_SourceLabel);
            SetStaticLabelText(k_UxmlIdentifier_TimingsHeader, Content.k_TimingsHeader);
            SetStaticLabelText(k_UxmlIdentifier_CpuLabel, Content.k_CpuLabel);
            SetStaticLabelText(k_UxmlIdentifier_GpuLabel, Content.k_GpuLabel);
        }

        void SetStaticLabelText(string identifier, string text)
        {
            var label = View.Q<Label>(identifier);
            if (label != null)
                label.text = text;
        }

        // Refreshes the panel for the profiler's currently selected frame. requestedLogicalFrame is
        // the frame the user navigated to; firstDisplayedFrame bounds the nearest-prior fallback so we
        // never surface a screenshot trimmed out of the display window (matching the large preview).
        public void UpdateForFrame(int requestedLogicalFrame, int firstDisplayedFrame)
        {
            if (!IsViewLoaded)
                return;

            m_FrameNumberLabel.text = FormatFrameNumber(requestedLogicalFrame);

            UpdateScreenshotInfo(requestedLogicalFrame, firstDisplayedFrame);
            UpdateTimings(requestedLogicalFrame);
        }

        void UpdateScreenshotInfo(int requestedLogicalFrame, int firstDisplayedFrame)
        {
            if (m_Catalogue == null
                || !m_Catalogue.TryResolveDisplayedScreenshot(requestedLogicalFrame, firstDisplayedFrame, out var source))
            {
                m_ResolutionValueLabel.text = k_EmptyValue;
                m_SourceValueLabel.text = Content.k_NoScreenshot;
                m_StaleHelpBox.style.display = DisplayStyle.None;
                return;
            }

            m_ResolutionValueLabel.text = TryGetResolution(source.EmissionFrame, out var width, out var height)
                ? FormatResolution(width, height)
                : k_EmptyValue;

            var framesAgo = requestedLogicalFrame - source.LogicalFrame;
            if (framesAgo > 0)
            {
                m_SourceValueLabel.text = string.Format(Content.k_FrameNumberFormat, source.LogicalFrame + 1);
                m_StaleHelpBox.text = FormatStaleHelp(source.LogicalFrame, framesAgo);
                m_StaleHelpBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_SourceValueLabel.text = Content.k_ThisFrame;
                m_StaleHelpBox.style.display = DisplayStyle.None;
            }
        }

        void UpdateTimings(int frameIndex)
        {
            ScreenshotFrameTimings.GetDurationsNs(frameIndex, out var cpuNs, out var gpuNs);
            var targetNs = ScreenshotFrameTimings.GetTargetFrameDurationNs();

            ConfigureTimingLabel(m_CpuValueLabel, cpuNs, targetNs);
            ConfigureTimingLabel(m_GpuValueLabel, gpuNs, targetNs);
        }

        internal static void ConfigureTimingLabel(Label valueLabel, ulong durationNs, ulong targetFrameDurationNs)
        {
            if (durationNs == 0UL)
            {
                valueLabel.text = Content.k_NoValue;
                valueLabel.style.color = StyleKeyword.Null;
                valueLabel.EnableInClassList(k_UssClass_TimingValueOverBudget, false);
                valueLabel.tooltip = string.Empty;
                return;
            }

            valueLabel.text = TimeFormatterUtility.FormatTimeNsToMs(durationNs);

            // Over budget is communicated three ways so the meaning is never carried by colour alone
            // (accessibility): the shared Highlights over-budget colour (colour-blind aware, kept in
            // sync via BottlenecksChartViewModel), a state class that adds a non-colour visual cue, and
            // an explanatory tooltip. Otherwise fall back to the default label styling for the theme.
            var isOverBudget = ScreenshotFrameTimings.IsOverBudget(durationNs, targetFrameDurationNs);
            valueLabel.style.color = isOverBudget
                ? new StyleColor(BottlenecksChartViewModel.GetColorForDataSeries(BottlenecksChartViewModel.CpuDataSeriesIndex))
                : StyleKeyword.Null;
            valueLabel.EnableInClassList(k_UssClass_TimingValueOverBudget, isOverBudget);
            valueLabel.tooltip = isOverBudget ? FormatOverBudgetTooltip(targetFrameDurationNs) : string.Empty;
        }

        internal static string FormatOverBudgetTooltip(ulong targetFrameDurationNs)
        {
            return string.Format(Content.k_OverBudgetTooltipFormat, TimeFormatterUtility.FormatTimeNsToMs(targetFrameDurationNs));
        }

        internal static string FormatFrameNumber(int logicalFrame)
        {
            return logicalFrame < 0
                ? Content.k_NoFrameSelected
                : string.Format(Content.k_FrameNumberFormat, logicalFrame + 1);
        }

        internal static string FormatResolution(int width, int height)
        {
            return string.Format(Content.k_ResolutionFormat, width, height);
        }

        internal static string FormatStaleHelp(int sourceLogicalFrame, int framesAgo)
        {
            return framesAgo == 1
                ? string.Format(Content.k_StaleHelpOneFrame, sourceLogicalFrame + 1)
                : string.Format(Content.k_StaleHelpManyFrames, sourceLogicalFrame + 1, framesAgo);
        }

        static bool TryGetResolution(int emissionFrame, out int width, out int height)
        {
            width = 0;
            height = 0;
            using (var frameData = ProfilerDriver.GetRawFrameDataView(emissionFrame, 0))
            {
                if (!frameData.valid)
                    return false;
                return ScreenshotIndexCatalogue.TryGetScreenshotTextureInfo(frameData, out width, out height, out _);
            }
        }
    }
}
