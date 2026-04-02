// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    // A simple container view controller responsible for embedding either a RangeSummaryViewController or a FrameSummaryViewController, depending on if the provided selection is a range of frames or a single frame. When the selection changes the appropriate view controller is first embedded or disposed if necessary, and then refreshed for the current selection.
    class SelectionSummaryViewController : ViewController
    {
        // Model.
        readonly IProfilerCaptureDataService m_DataService;
        readonly IProfilerPersistentSettingsService m_SettingsService;
        readonly ProfilerWindow m_ProfilerWindow;
        readonly SummaryViewController.IResponder m_Responder;
        readonly IDetailsElementBinder m_DetailsBinder;

        // Children.
        FrameSummaryViewController m_FrameSummaryViewController;
        RangeSummaryViewController m_RangeSummaryViewController;

        public SelectionSummaryViewController(
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            ProfilerWindow profilerWindow,
            SummaryViewController.IResponder responder,
            IDetailsElementBinder detailsBinder)
        {
            m_DataService = dataService;
            m_SettingsService = settingsService;
            m_ProfilerWindow = profilerWindow;
            m_Responder = responder;
            m_DetailsBinder = detailsBinder;
        }

        public void ReloadData(Range frameRange, Action<IDetailsProvider> onDetailsProviderReady = null)
        {
            if (IsViewLoaded == false)
                throw new InvalidOperationException("View must be loaded prior to calling ReloadData.");

            var rangeLength = frameRange.End.Value - frameRange.Start.Value;
            var isSingleFrame = rangeLength == 1;
            if (isSingleFrame)
            {
                DisposeChildViewControllerIfNotNull(ref m_RangeSummaryViewController);
                m_FrameSummaryViewController ??= CreateAndEmbedChildViewController(() =>
                {
                    return new FrameSummaryViewController(m_DataService, m_SettingsService, m_ProfilerWindow, m_Responder, m_DetailsBinder);
                });

                m_FrameSummaryViewController.ReloadData(frameRange.Start.Value, onDetailsProviderReady);
            }
            else
            {
                DisposeChildViewControllerIfNotNull(ref m_FrameSummaryViewController);
                m_RangeSummaryViewController ??= CreateAndEmbedChildViewController(() =>
                {
                    return new RangeSummaryViewController(m_DataService, m_SettingsService, m_ProfilerWindow, m_Responder, m_DetailsBinder);
                });

                m_RangeSummaryViewController.ReloadData(frameRange, onDetailsProviderReady);
            }
        }

        public void CancelReloadDataIfNecessary()
        {
            m_FrameSummaryViewController?.CancelReloadDataIfNecessary();
            m_RangeSummaryViewController?.CancelReloadDataIfNecessary();
        }

        protected override VisualElement LoadView()
        {
            // We don't need the overhead of uxml/uss files for such a simple container view.
            var view = new VisualElement()
            {
                name = "selection-summary-view",
                style =
                {
                    flexGrow = 1,
                    minWidth = 620,
                }
            };

            return view;
        }

        T CreateAndEmbedChildViewController<T>(Func<T> createViewController)
            where T : ViewController
        {
            T viewController = createViewController();
            View.Add(viewController.View);
            AddChild(viewController);
            return viewController;
        }

        void DisposeChildViewControllerIfNotNull<T>(ref T viewController)
            where T : ViewController
        {
            if (viewController != null)
            {
                RemoveChild(viewController);
                viewController.Dispose();
                viewController = null;
            }
        }
    }
}
