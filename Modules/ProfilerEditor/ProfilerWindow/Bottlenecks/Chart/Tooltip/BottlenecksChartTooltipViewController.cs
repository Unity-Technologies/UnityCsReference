// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class BottlenecksChartTooltipViewController : ViewController
    {
        const string k_UxmlResourceName = "BottlenecksChartTooltipView.uxml";
        const string k_UssClass_Dark = "bottlenecks-chart-tooltip-view__dark";
        const string k_UssClass_Light = "bottlenecks-chart-tooltip-view__light";
        const string k_UxmlIdentifier_Label = "bottlenecks-chart-tooltip-view__label";

        Label m_Label;

        public void SetDurations(float cpuDuration, float gpuDuration, float targetFrameDurationNs)
        {
            if (!IsViewLoaded)
                return;

            var text = BuildTooltipTextForDurations(cpuDuration, gpuDuration, targetFrameDurationNs);
            m_Label.text = text;
        }

        public void SetPosition(Vector2 position)
        {
            if (!IsViewLoaded)
                return;

            View.style.left = position.x;
            View.style.top = position.y;
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlResourceName);
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_Label = view.Q<Label>(k_UxmlIdentifier_Label);
        }

        string BuildTooltipTextForDurations(float cpuDurationNs, float gpuDurationNs, float targetFrameDurationNs)
        {
            var isMissingCpuDuration = IsInvalidDuration(cpuDurationNs);
            var isMissingGpuDuration = IsInvalidDuration(gpuDurationNs);

            string title;
            if (isMissingCpuDuration && isMissingGpuDuration)
                title = "There is no data available for this frame.";
            else if (isMissingGpuDuration && cpuDurationNs < targetFrameDurationNs) // Only display 'missing timing' if there is no bottleneck on the other timing.
                title = "There is no GPU timing for this frame.";
            else if (isMissingCpuDuration && gpuDurationNs < targetFrameDurationNs)
                title = "There is no CPU timing for this frame.";
            else if (cpuDurationNs < targetFrameDurationNs && gpuDurationNs < targetFrameDurationNs)
                title = "You are within your target frame time.";
            else if (cpuDurationNs > targetFrameDurationNs && gpuDurationNs > targetFrameDurationNs)
                title = $"The CPU and GPU exceeded your target frame time.";
            else
                title = $"The {((cpuDurationNs > gpuDurationNs) ? "CPU" : "GPU")} exceeded your target frame time.";

            var cpuTime = FormatDuration(cpuDurationNs, "CPU", targetFrameDurationNs);
            var gpuTime = FormatDuration(gpuDurationNs, "GPU", targetFrameDurationNs);
            return $"<b>{title}</b>\n\n{cpuTime}\n{gpuTime}";
        }

        static bool IsInvalidDuration(float durationNs)
        {
            // A value of -1 is what our existing counters API returns when there is no counter data in the selected frame.
            // A value of 0 is what FTM will write when GPU misses the deadline and does not record a measurement.
            return Mathf.Approximately(durationNs, -1f) || Mathf.Approximately(durationNs, 0f);
        }

        static string FormatDuration(float durationNs, string durationName, float targetDurationNs)
        {
            var durationFormatted = FormatTimeNsToMs(durationNs);
            if (durationNs > targetDurationNs)
                return $"<color=#ED5656>{durationName}: {durationFormatted}</color>";
            else
                return $"{durationName}: {durationFormatted}";
        }

        static string FormatTimeNsToMs(float timeNs)
        {
            if (IsInvalidDuration(timeNs))
                return string.Format($"--ms");

            return string.Format($"{timeNs * 1.0e-6f:F3}ms");
        }
    }
}
