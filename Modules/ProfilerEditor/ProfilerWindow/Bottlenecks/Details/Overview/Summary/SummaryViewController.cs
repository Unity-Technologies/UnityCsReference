// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace Unity.Profiling.Editor.UI
{
    abstract class SummaryViewController : ViewController, TopMarkersViewController.IResponder
    {
        // Model.
        protected readonly IProfilerCaptureDataService m_DataService;
        protected readonly IProfilerPersistentSettingsService m_SettingsService;
        protected readonly ProfilerWindow m_ProfilerWindow;
        protected readonly IResponder m_Responder;

        public SummaryViewController(
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            ProfilerWindow profilerWindow,
            IResponder responder)
        {
            m_DataService = dataService;
            m_SettingsService = settingsService;
            m_ProfilerWindow = profilerWindow;
            m_Responder = responder;
        }

        void TopMarkersViewController.IResponder.OnMarkerSelected(
            TopMarkersModel.Marker marker,
            TopMarkersViewController.Action action)
        {
            m_Responder?.OnTopMarkerSelected(marker, action);
        }

        public interface IResponder
        {
            void OnTopMarkerSelected(TopMarkersModel.Marker marker, TopMarkersViewController.Action action);
        }
    }
}
