// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Text;
using Unity.Collections;
using UnityEditor.Accessibility;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    internal abstract class CaptureFileBaseViewController : ViewController, BlocksGraphViewRender.IDataSource
    {
        const string k_UxmlNameLabel = "profiler-capture-file__meta-data__name";
        const string k_UxmlFPSLabel = "profiler-capture-file__meta-data__fps";
        const string k_UxmlPreviewImage = "profiler-capture-file__preview-image";
        const string k_UxmlPlatformIcon = "profiler-capture-file__preview-image__platform-icon";
        const string k_UxmlEditorPlatformIcon = "profiler-capture-file__preview-image__editor-icon";
        const string k_UxmlBlocksGraphRender = "profiler-capture-file__bar__blocks-graph-view-render";

        const string k_TooltipNoBottleneck = "Information about CPU and GPU targets is not available until a capture has been opened at least once. Open the capture to see it displayed in the profiler captures list.";
        const string k_Tooltip = "{0}\n\nCPU: {1}% of frames over target\nGPU: {2}% of frames over target";

        // Model & state
        readonly ScreenshotsManager m_ScreenshotsManager;
        protected BottlenecksChartViewModel m_BottleneckModel;

        // View
        protected Label m_Name;
        protected Label m_FPSTarget;
        Image m_Screenshot;
        Image m_PlatformIcon;
        Image m_EditorPlatformIcon;
        BlocksGraphViewRender m_BlocksGraphView;

        public CaptureFileBaseViewController(CaptureFileModel model, ScreenshotsManager screenshotsManager)
        {
            Model = model;
            m_ScreenshotsManager = screenshotsManager;
            UserAccessiblitySettings.colorBlindConditionChanged += OnColorBlindSettingChanged;
        }

        protected void ScreenshotRefresh()
        {
            ScreenshotImage?.MarkDirtyRepaint();
        }

        internal CaptureFileModel Model { get; }

        protected Image ScreenshotImage => m_Screenshot;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UserAccessiblitySettings.colorBlindConditionChanged -= OnColorBlindSettingChanged;
                m_BottleneckModel?.Dispose();
            }

            base.Dispose(disposing);
        }

        void OnColorBlindSettingChanged()
        {
            if (!IsViewLoaded)
                return;

            m_BlocksGraphView?.MarkDirtyRepaint();
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();
            RefreshView();
        }

        protected virtual void GatherReferencesInView(VisualElement view)
        {
            m_Name = view.Q<Label>(k_UxmlNameLabel);
            m_FPSTarget = view.Q<Label>(k_UxmlFPSLabel);
            m_Screenshot = view.Q<Image>(k_UxmlPreviewImage);
            m_PlatformIcon = view.Q<Image>(k_UxmlPlatformIcon);
            m_EditorPlatformIcon = view.Q<Image>(k_UxmlEditorPlatformIcon);
            m_BlocksGraphView = view.Q<BlocksGraphViewRender>(k_UxmlBlocksGraphRender);
        }

        protected virtual void RefreshView()
        {
            if (Model == null)
                return;

            m_Name.text = Model.Name;
            m_PlatformIcon.image = Model.Platform < 0 ? null : PlatformsHelper.GetPlatformIcon(Model.Platform);
            UIUtility.SetElementDisplay(m_EditorPlatformIcon, Model.EditorPlatform);

            if (m_BottleneckModel is { IsDisposed: false })
                m_BottleneckModel.Dispose();

            m_BottleneckModel = BottlenecksChartViewModelBuilder.BuildFromFile(Model.FullPath);
            m_BlocksGraphView.DataSource = this;
            m_BlocksGraphView.MarkDirtyRepaint();

            var fpsTarget = GetFramerateTarget();
            m_FPSTarget.text = fpsTarget < 0 ? "" : $"Target: {fpsTarget}fps";

            m_Screenshot.scaleMode = ScaleMode.ScaleToFit;

            var screenshotPath = Path.ChangeExtension(Model.FullPath, ".png");
            if (File.Exists(screenshotPath))
            {
                var image = m_ScreenshotsManager.Enqueue(screenshotPath, ScreenshotRefresh);
                m_Screenshot.image = image;
            }

            RefreshTooltip();
        }

        protected int GetFramerateTarget()
        {
            if (m_BottleneckModel == null)
                return -1;

            return Mathf.RoundToInt(1e9f / ((BlocksGraphViewRender.IDataSource)this).DataValueThresholdInGraphView());
        }

        void RefreshTooltip()
        {
            var cpuPercentOver = Mathf.RoundToInt(((BlocksGraphViewRender.IDataSource)this).PercentageFramesOverTarget(0));
            var gpuPercentOver = Mathf.RoundToInt(((BlocksGraphViewRender.IDataSource)this).PercentageFramesOverTarget(1));

            var sb = new StringBuilder();
            sb.AppendLine(Model.Name);

            if (m_BottleneckModel is { CaptureMetaDataVersion: >= 0 })
            {
                // These need to be individually checked because of previous issues
                // where they might not have consistently been written out.
                if (m_BottleneckModel.Platform >= 0)
                    sb.AppendLine("Platform: " + m_BottleneckModel.Platform);
                if (m_BottleneckModel.ScriptingBackend >= 0)
                    sb.AppendLine("Scripting Backend: " + m_BottleneckModel.ScriptingBackend);
                if (m_BottleneckModel.UnityVersion.Length > 0)
                    sb.AppendLine("Unity Version: " + m_BottleneckModel.UnityVersion);
                if (m_BottleneckModel.ProductName.Length > 0)
                    sb.AppendLine("Project Name: " + m_BottleneckModel.ProductName);
                if (m_BottleneckModel.DeviceModel.Length > 0)
                    sb.AppendLine("Device Model: " + m_BottleneckModel.DeviceModel);
                if (m_BottleneckModel.DeviceSystemVersion.Length > 0)
                    sb.AppendLine("Device System Version: " + m_BottleneckModel.DeviceSystemVersion);
            }

            sb.AppendLine("");
            if (cpuPercentOver < 0 || gpuPercentOver < 0)
                sb.Append(k_TooltipNoBottleneck);
            else
            {
                sb.AppendLine($"CPU: {cpuPercentOver}% of frames over target");
                sb.Append($"GPU: {gpuPercentOver}% of frames over target");
            }

            View.tooltip = sb.ToString();
        }

        int BlocksGraphViewRender.IDataSource.NumberOfDataSeriesForGraphView()
        {
            if (m_BottleneckModel == null)
                return 0;

            return m_BottleneckModel.NumberOfDataSeries;
        }

        Color BlocksGraphViewRender.IDataSource.ColorForDataSeriesInGraphView(int dataSeriesIndex)
        {
            return BottlenecksChartViewModel.GetColorForDataSeries(dataSeriesIndex);
        }

        Color BlocksGraphViewRender.IDataSource.InvalidColorForDataSeriesInGraphView()
        {
            return BottlenecksChartViewModel.InvalidColor;
        }

        float BlocksGraphViewRender.IDataSource.DataValueThresholdInGraphView()
        {
            if (m_BottleneckModel == null)
                return 0;

            return m_BottleneckModel.BottleneckThreshold;
        }

        int BlocksGraphViewRender.IDataSource.LengthForEachDataSeriesInGraphView()
        {
            if (m_BottleneckModel == null)
                return 0;

            return m_BottleneckModel.DataSeriesCapacityThumbnail;
        }

        NativeSlice<float> BlocksGraphViewRender.IDataSource.ValuesForDataSeriesInGraphView(int dataSeriesIndex)
        {
            return m_BottleneckModel.DataValueBuffers[dataSeriesIndex];
        }

        float BlocksGraphViewRender.IDataSource.PercentageFramesOverTarget(int dataSeriesIndex)
        {
            if (m_BottleneckModel == null)
                return -1f;

            return m_BottleneckModel.PercentOverThreshold[dataSeriesIndex];
        }
    }
}
