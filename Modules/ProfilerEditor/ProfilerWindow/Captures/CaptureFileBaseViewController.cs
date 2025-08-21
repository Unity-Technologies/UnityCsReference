// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using Unity.Collections;
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

        const string k_TooltipNoBottleneck = "{0}\n\nInformation about CPU and GPU targets is not available until a capture has been opened at least once. Open the capture to see it displayed in the profiler captures list.";
        const string k_Tooltip = "{0}\n\nCPU: {1}% of frames over target\nGPU: {2}% of frames over target";

        // Model & state
        readonly CaptureFileModel m_Model;
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
            m_Model = model;
            m_ScreenshotsManager = screenshotsManager;
        }

        protected void ScreenshotRefresh()
        {
            ScreenshotImage?.MarkDirtyRepaint();
        }

        protected CaptureFileModel Model => m_Model;

        protected Image ScreenshotImage => m_Screenshot;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                m_BottleneckModel?.Dispose();

            base.Dispose(disposing);
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
            if (m_Model == null)
                return;

            m_Name.text = m_Model.Name;
            m_PlatformIcon.image = null; // TODO: Platform icons a la Memory Profiler
            UIUtility.SetElementDisplay(m_EditorPlatformIcon, m_Model.EditorPlatform);

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

            RefreshTotal();
        }

        protected int GetFramerateTarget()
        {
            if (m_BottleneckModel == null)
                return -1;

            return Mathf.RoundToInt(1e9f / ((BlocksGraphViewRender.IDataSource)this).DataValueThresholdInGraphView());
        }

        void RefreshTotal()
        {
            var cpuPercentOver = Mathf.RoundToInt(((BlocksGraphViewRender.IDataSource)this).PercentageFramesOverTarget(0));
            var gpuPercentOver = Mathf.RoundToInt(((BlocksGraphViewRender.IDataSource)this).PercentageFramesOverTarget(1));

            if (cpuPercentOver < 0 || gpuPercentOver < 0)
                View.tooltip = string.Format(k_TooltipNoBottleneck, m_Model.Name);
            else
                View.tooltip = string.Format(k_Tooltip, m_Model.Name, cpuPercentOver, gpuPercentOver);
        }

        int BlocksGraphViewRender.IDataSource.NumberOfDataSeriesForGraphView()
        {
            if (m_BottleneckModel == null)
                return 0;

            return m_BottleneckModel.NumberOfDataSeries;
        }

        Color BlocksGraphViewRender.IDataSource.ColorForDataSeriesInGraphView(int dataSeriesIndex)
        {
            return BottlenecksChartViewModel.Colors[dataSeriesIndex];
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
