// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.UIElements;
using UnityEditor.AdaptivePerformance.UI.Editor;
using Unity.Profiling.Editor;

namespace UnityEditor.AdaptivePerformance
{

    internal class AdaptivePerformanceDetailsViewController : ProfilerModuleViewController
    {
        sealed class ScalerElement
        {
            public readonly VisualElement container;
            public readonly Label viewName;
            public readonly VisualElement barFill;
            public readonly Label maxLabel;
            public readonly Label currentLabel;

            public ScalerElement(VisualElement container, Label viewName, VisualElement barFill, Label maxLabel, Label currentLabel)
            {
                this.container = container;
                this.viewName = viewName;
                this.barFill = barFill;
                this.maxLabel = maxLabel;
                this.currentLabel = currentLabel;
            }
        }

        const string k_UxmlResourceName = "AdaptivePerformance/UXML/Profiler/AdaptivePerformanceDetailsView.uxml";
        const string k_UxmlResourceNameScaler = "AdaptivePerformance/UXML/Profiler/AdaptivePerformanceScalerElement.uxml";
        const string k_ResourceNameScalerStyle = "AdaptivePerformance/StyleSheets/Profiler/AdaptivePerformanceScalerElement.uss";
        const string k_ResourceNameStyle = "AdaptivePerformance/StyleSheets/Profiler/AdaptivePerformanceDetailsView.uss";

        VisualElement m_view;
        Label m_DetailsViewLabel;
        VisualElement m_Scalers;
        VisualTreeAsset m_ScalerTree;
        StyleSheet m_ScalerStyle;
        Label m_EmptyScalerLabel = new  Label(L10n.Tr("No Scaler data"));
        readonly List<ScalerElement> m_ScalerElements = new List<ScalerElement>();
        UsageDial m_UsageDial;
        Label m_BottleneckLabel;
        VisualElement m_BottleneckIcon;
        Label m_UsageDialLabel;
        Label m_PerformanceModeLabel;
        VisualElement m_PerformanceModeIcon;
        StyleColor m_appliedScalerColor = new StyleColor(new Color(0.09f, 0.69f, 0.3f, 1f));
        StyleColor m_unappliedScalerColor = new StyleColor(new Color(0.09f, 0.3f, 0.69f, 1f));
        StyleColor m_inactiveColor = new StyleColor(new Color(0.29f, 0.69f, 0.3f, 0.3f));
        Length m_scalerOffset = new Length(-200, LengthUnit.Pixel);
        Length m_midDistance = new Length(100, LengthUnit.Pixel);
        StyleRotate m_scalerRotate = new StyleRotate(new Rotate(180));
        static readonly StyleColor k_Green = new StyleColor(new Color32(136, 176, 49, byte.MaxValue));
        static readonly StyleColor k_Yellow = new StyleColor(new Color32(221, 124, 69, byte.MaxValue));
        static readonly StyleColor k_Red = new StyleColor(new Color32(219, 89, 81, byte.MaxValue));
        const int m_arraySize = 320;
        byte[] m_arr = new byte[m_arraySize];

        public AdaptivePerformanceDetailsViewController(ProfilerWindow profilerWindow) : base(profilerWindow) {}

        protected override VisualElement CreateView()
        {
            var apDetailsView = EditorGUIUtility.LoadRequired(k_UxmlResourceName) as VisualTreeAsset;
            var apDetailStyle = EditorGUIUtility.LoadRequired(k_ResourceNameStyle) as StyleSheet;
            m_ScalerTree = EditorGUIUtility.LoadRequired(k_UxmlResourceNameScaler) as VisualTreeAsset;
            m_ScalerStyle = EditorGUIUtility.LoadRequired(k_ResourceNameScalerStyle) as StyleSheet;
            m_view = apDetailsView.Instantiate();
            m_view.styleSheets.Add(apDetailStyle);
            m_DetailsViewLabel = m_view.Q<Label>("ap-details-view-label");
            m_Scalers = m_view.Q<VisualElement>("ap-scalers");
            m_BottleneckLabel = m_view.Q<Label>("ap-details-view-bottleneck-icon-label");
            m_BottleneckIcon = m_view.Q<VisualElement>("ap-details-view-bottleneck-icon");
            m_PerformanceModeLabel = m_view.Q<Label>("ap-details-view-performance-mode-icon-label");
            m_PerformanceModeIcon = m_view.Q<VisualElement>("ap-details-view-performance-mode-icon");
            var usageDial = m_view.Q<VisualElement>("bg-container");
            m_UsageDial = new UsageDial();
            m_UsageDial.style.width = 64;
            m_UsageDial.style.height = 32;

            usageDial.Add(m_UsageDial);
            m_UsageDialLabel = m_view.Q<Label>("ap-details-view-thermal-label");
            if (m_UsageDial != null)
            {
                m_UsageDial.ShowLabel = false;
                m_UsageDial?.SetThresholds(40, 75);
                m_UsageDial.Value = 31;
            }

            ReloadData(ProfilerWindow.selectedFrameIndex);
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            return m_view;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            base.Dispose(disposing);
        }

        void OnSelectedFrameIndexChanged(long selectedFrameIndex)
        {
            ReloadData(selectedFrameIndex);
        }

        void ReloadData(long selectedFrameIndex)
        {
            var selectedFrameIndexInt32 = Convert.ToInt32(selectedFrameIndex);
            using (var frameData = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(selectedFrameIndexInt32, 0))
            {
                if (frameData == null || !frameData.valid)
                {
                    m_DetailsViewLabel.text = "No Adaptive Performance Frame Data.";
                    PopulateScalers(Array.Empty<AdaptivePerformanceProfilerStats.ScalerInfo>());
                    return;
                }

                var thermalWarningLevel = (WarningLevel)ExtractAdaptivePerformanceCounterValueInt(frameData, "Thermal Warning Level");
                var bottleneck = (PerformanceBottleneck)ExtractAdaptivePerformanceCounterValueInt(frameData, "Bottleneck");
                var performanceMode = (PerformanceMode)ExtractAdaptivePerformanceCounterValueInt(frameData, "Performance Mode");

                m_DetailsViewLabel.text = $"CPU frametime: {ExtractAdaptivePerformanceCounterValueFloat(frameData, "CPU frametime") / 1000000.0f} ms \t\t" +
                    $"Average CPU frametime: {ExtractAdaptivePerformanceCounterValueFloat(frameData, "CPU avg frametime") / 1000000.0f} ms \n" +
                    $"GPU frametime: {ExtractAdaptivePerformanceCounterValueFloat(frameData, "GPU frametime") / 1000000.0f} ms \t\t" +
                    $"Average GPU frametime: {ExtractAdaptivePerformanceCounterValueFloat(frameData, "GPU avg frametime") / 1000000.0f} ms \n" +
                    $"Frametime: {ExtractAdaptivePerformanceCounterValueFloat(frameData, "Frametime") / 1000000.0f} ms \t\t\t" +
                    $"Average frametime: {ExtractAdaptivePerformanceCounterValueFloat(frameData, "Avg frametime") / 1000000.0f} ms \n" +
                    $"\n" +
                    $"CPU performance level: {ExtractAdaptivePerformanceCounterValueInt(frameData, "CPU performance level")} \n" +
                    $"GPU performance level: {ExtractAdaptivePerformanceCounterValueInt(frameData, "GPU performance level")} \n" +
                    $"\n" +
                    $"Temperature Level: {ExtractAdaptivePerformanceCounterValueFloat(frameData, "Temperature Level")} \n" +
                    $"Temperature Trend: {ExtractAdaptivePerformanceCounterValueFloat(frameData, "Temperature Trend")} \n" +
                    $"\n" +
                    $"Thermal Warning Level: {thermalWarningLevel} \n" +
                    $"Bottleneck: {bottleneck} \n" +
                    $"Performance Mode: {performanceMode} \n";

                if (m_BottleneckLabel != null && m_BottleneckIcon != null)
                {
                    if (bottleneck == PerformanceBottleneck.CPU)
                    {
                        m_BottleneckIcon.style.backgroundColor = k_Red;
                        m_BottleneckLabel.text = "CPU";
                    }
                    else if (bottleneck == PerformanceBottleneck.GPU)
                    {
                        m_BottleneckIcon.style.backgroundColor = k_Red;
                        m_BottleneckLabel.text = "GPU";
                    }
                    else if (bottleneck == PerformanceBottleneck.TargetFrameRate)
                    {
                        m_BottleneckIcon.style.backgroundColor = k_Yellow;
                        m_BottleneckLabel.text = "Target Framerate";
                    }
                    else
                    {
                        m_BottleneckIcon.style.backgroundColor = k_Yellow;
                        m_BottleneckLabel.text = "Unknown";
                    }
                }

                if (m_UsageDial != null)
                {
                    if (thermalWarningLevel == WarningLevel.NoWarning)
                    {
                        m_UsageDial.Value = 25;
                        m_UsageDialLabel.text = "No Warning";
                    }
                    else if (thermalWarningLevel == WarningLevel.ThrottlingImminent)
                    {
                        m_UsageDial.Value = 60;
                        m_UsageDialLabel.text = "Throttling Imminent";
                    }
                    else if (thermalWarningLevel >= WarningLevel.Throttling)
                    {
                        m_UsageDial.Value = 90;
                        m_UsageDialLabel.text = "Throttling";
                    }
                }

                if (m_PerformanceModeLabel != null && m_PerformanceModeIcon != null)
                {
                    if (performanceMode == PerformanceMode.Optimize)
                    {
                        m_PerformanceModeIcon.style.backgroundColor = k_Red;
                        m_PerformanceModeLabel.text = "Optimize";
                    }
                    else if (performanceMode == PerformanceMode.CPU)
                    {
                        m_PerformanceModeIcon.style.backgroundColor = k_Red;
                        m_PerformanceModeLabel.text = "CPU";
                    }
                    else if (performanceMode == PerformanceMode.GPU)
                    {
                        m_PerformanceModeIcon.style.backgroundColor = k_Red;
                        m_PerformanceModeLabel.text = "GPU";
                    }
                    else if (performanceMode == PerformanceMode.Battery)
                    {
                        m_PerformanceModeIcon.style.backgroundColor = k_Yellow;
                        m_PerformanceModeLabel.text = "Battery";
                    }
                    else if (performanceMode == PerformanceMode.Standard)
                    {
                        m_PerformanceModeIcon.style.backgroundColor = k_Green;
                        m_PerformanceModeLabel.text = "Standard";
                    }
                    else
                    {
                        m_PerformanceModeIcon.style.backgroundColor = k_Yellow;
                        m_PerformanceModeLabel.text = "Unknown";
                    }
                }

                var scalerInfos = GetScalerFromProfilerStream(selectedFrameIndexInt32);
                PopulateScalers(scalerInfos);
            }
        }

        void PopulateScalers(AdaptivePerformanceProfilerStats.ScalerInfo[] scalerInfos)
        {
            if (m_Scalers == null || m_ScalerTree == null || m_ScalerStyle == null)
                return;

            if (scalerInfos == null || scalerInfos.Length == 0)
            {
                ShowEmptyScalerLabel();
                SetUnusedScalerElementsHidden(0);
                return;
            }

            HideEmptyScalerLabel();
            SortScalerInfosByName(scalerInfos);
            EnsureScalerElementPoolSize(scalerInfos.Length);

            unsafe
            {
                for (var i = 0; i < scalerInfos.Length; ++i)
                {
                    var scalerInfo = scalerInfos[i];
                    var scalerName = GetScalerName(scalerInfo.scalerName);
                    var scalerElement = m_ScalerElements[i];

                    scalerElement.container.style.display = DisplayStyle.Flex;
                    scalerElement.viewName.text = scalerName.Replace("Adaptive", "");
                    scalerElement.viewName.name = $"{scalerName}-element-label";
                    scalerElement.barFill.name = $"{scalerName}-element-bar-fill";
                    scalerElement.maxLabel.name = $"{scalerName}-element-max-label";
                    scalerElement.currentLabel.name = $"{scalerName}-element-current-label";
                    scalerElement.currentLabel.style.bottom = m_midDistance;
                    scalerElement.currentLabel.text = $"{scalerInfo.currentLevel}";

                    if (scalerInfo.enabled == 0)
                    {
                        scalerElement.barFill.style.backgroundColor = m_inactiveColor;
                    }
                    else if (scalerInfo.applied == 1)
                    {
                        scalerElement.barFill.style.backgroundColor = m_appliedScalerColor;
                    }
                    else
                    {
                        scalerElement.barFill.style.backgroundColor = m_unappliedScalerColor;
                    }

                    var height = scalerInfo.maxLevel > 0
                        ? new Length((float)scalerInfo.currentLevel / scalerInfo.maxLevel * 100.0f, LengthUnit.Percent)
                        : new Length(0, LengthUnit.Percent);
                    scalerElement.barFill.style.height = height;
                    scalerElement.barFill.style.bottom = m_scalerOffset;
                    scalerElement.barFill.style.rotate = m_scalerRotate;
                    scalerElement.maxLabel.text = $"{scalerInfo.maxLevel}";
                }
            }

            SetUnusedScalerElementsHidden(scalerInfos.Length);
        }

        internal static void SortScalerInfosByName(AdaptivePerformanceProfilerStats.ScalerInfo[] scalerInfos)
        {
            if (scalerInfos == null || scalerInfos.Length <= 1)
                return;

            Array.Sort(scalerInfos, CompareScalerInfosByName);
        }

        static unsafe int CompareScalerInfosByName(AdaptivePerformanceProfilerStats.ScalerInfo left, AdaptivePerformanceProfilerStats.ScalerInfo right)
        {
            for (var i = 0; i < m_arraySize; ++i)
            {
                var leftByte = left.scalerName[i];
                var rightByte = right.scalerName[i];

                if (leftByte != rightByte)
                    return leftByte.CompareTo(rightByte);

                if (leftByte == 0)
                    return 0;
            }

            return 0;
        }

        void EnsureScalerElementPoolSize(int scalerCount)
        {
            while (m_ScalerElements.Count < scalerCount)
            {
                var container = m_ScalerTree.CloneTree();
                container.styleSheets.Add(m_ScalerStyle);

                var viewName = container.Q<Label>("ap-scaler-element-label");
                var barFill = container.Q<VisualElement>("ap-scaler-element-bar-fill");
                var maxLabel = container.Q<Label>("ap-scaler-element-max-label");
                var currentLabel = container.Q<Label>("ap-scaler-element-level-label");

                if (currentLabel == null || maxLabel == null || barFill == null || viewName == null)
                    throw new InvalidOperationException("AdaptivePerformanceScalerElement.uxml is missing required elements.");

                var scalerElement = new ScalerElement(container, viewName, barFill, maxLabel, currentLabel);
                m_ScalerElements.Add(scalerElement);
                m_Scalers.Add(container);
            }
        }

        void SetUnusedScalerElementsHidden(int usedScalerCount)
        {
            for (var i = usedScalerCount; i < m_ScalerElements.Count; ++i)
                m_ScalerElements[i].container.style.display = DisplayStyle.None;
        }

        void ShowEmptyScalerLabel()
        {
            if (m_EmptyScalerLabel.parent != m_Scalers)
                m_Scalers.Add(m_EmptyScalerLabel);

            m_EmptyScalerLabel.style.display = DisplayStyle.Flex;
        }

        void HideEmptyScalerLabel()
        {
            if (m_EmptyScalerLabel.parent == m_Scalers)
                m_EmptyScalerLabel.style.display = DisplayStyle.None;
        }

        unsafe string GetScalerName(byte* scalerNamePtr)
        {
            Marshal.Copy((IntPtr)scalerNamePtr, m_arr, 0, m_arraySize);
            return Encoding.ASCII.GetString(m_arr).Replace(" ", string.Empty).Replace("\0", string.Empty);
        }

        static int ExtractAdaptivePerformanceCounterValueInt(UnityEditor.Profiling.FrameDataView frameData, string counterName)
        {
            if (frameData == null || counterName.Length == 0)
                return 0;

            var counterMarkerId = frameData.GetMarkerId(counterName);
            return frameData.GetCounterValueAsInt(counterMarkerId);
        }

        static float ExtractAdaptivePerformanceCounterValueFloat(UnityEditor.Profiling.FrameDataView frameData, string counterName)
        {
            if (frameData == null || counterName.Length == 0)
                return 0;

            var counterMarkerId = frameData.GetMarkerId(counterName);
            return frameData.GetCounterValueAsFloat(counterMarkerId);
        }

        public static AdaptivePerformanceProfilerStats.ScalerInfo[] GetScalerFromProfilerStream(int frame)
        {
            using (var frameData = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(frame, 0))
            {
                var returnVal = Array.Empty<AdaptivePerformanceProfilerStats.ScalerInfo>();
                if (frameData != null)
                {
                    var clientInfos =
                        frameData.GetFrameMetaData<AdaptivePerformanceProfilerStats.ScalerInfo>(AdaptivePerformanceProfilerStats.kAdaptivePerformanceProfilerModuleGuid, AdaptivePerformanceProfilerStats.kScalerDataTag);
                    if (clientInfos.Length != 0)
                    {
                        returnVal = clientInfos.ToArray();
                    }
                }
                return returnVal;
            }
        }
    }
}
