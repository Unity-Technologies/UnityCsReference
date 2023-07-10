// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEditor.Connect;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Profiling;
using UnityEngine.Scripting;
using static UnityEditor.PlayModeAnalytics;
using static UnityEditor.Profiling.Analytics.ProfilerWindowAnalytics;

namespace UnityEditor.Profiling.Analytics
{
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ProfilerAnalyticsSaveLoadData : IAnalytic.IData
    {
        public string extension;
        public long duration;
        public long size;
        public uint frameCount;
        public bool customFrameRange;
        public string details;
        public bool success;
        public bool isLoad;
        public bool isSave;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ProfilerAnalyticsConnectionData : IAnalytic.IData
    {
        public bool success;
        public string connectionDetail;
        public string details;
    }

    [Serializable]
    internal struct ProfilerAnalyticsViewUsability : IAnalytic.IData
    {
        public string element;
        public uint mouseEvents;
        public uint keyboardEvents;
        public double time;
    }

    [Serializable]
    internal struct ProfilerAnalyticsViewUsabilitySession : IAnalytic.IData
    {
        public ProfilerAnalyticsViewUsability[] session;
        public uint mouseEvents;
        public uint keyboardEvents;
        public double time;
    }

    [Serializable]
    internal struct ProfilerAnalyticsCapture : IAnalytic.IData
    {
        public bool deepProfileEnabled;
        public bool callStacksEnabled;
        public string platformName; // Editor is platform
        public string connectionName;
    }

    [Serializable]
    internal struct BottleneckLink : IAnalytic.IData
    {
        public string linkDescription;
    }


    internal static class ProfilerWindowAnalytics
    {
        const int k_MaxEventsPerHour = 1000; // Max Events send per hour.
        const int k_MaxNumberOfElements = 1000; //Max number of elements sent.
        const string k_VendorKey = "unity.profiler";
        const string k_ProfilerSaveLoad = "profilerSaveLoad";
        const string k_ProfilerConnection = "profilerConnection";
        const string k_ProfilerElementUsability = "profilerSessionElementUsability";
        const string k_ProfilerCapture = "profilerCapture";
        const string k_BottlenecksModuleLinkSelected = "bottlenecksModuleLinkSelected";

        static ProfilerAnalyticsViewUsabilitySession s_ProfilerSession;
        static List<ProfilerAnalyticsViewUsability> s_Views;
        static int s_CurrentViewIndex;

        static IAnalyticsService s_AnalyticsService;

        static ProfilerWindowAnalytics()
        {
            // Instantiate default editor analytics, but only for user sessions when analytics is enabled.
            if (!InternalEditorUtility.inBatchMode && EditorAnalytics.enabled)
                SetAnalyticsService(new EditorAnalyticsService());
        }

        public static IAnalyticsService SetAnalyticsService(IAnalyticsService service)
        {
            var oldService = s_AnalyticsService;
            s_AnalyticsService = service;

            return oldService;
        }

        [AnalyticInfo(eventName: k_ProfilerElementUsability, vendorKey: k_VendorKey)]
        internal class ProfilerWindowDestroy : IAnalytic
        {
            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = s_ProfilerSession;
                return data != null;
            }
        }

        [AnalyticInfo(eventName: k_ProfilerSaveLoad, vendorKey: k_VendorKey)]
        internal class SaveLoadEvent : IAnalytic
        {
            public SaveLoadEvent(ProfilerAnalyticsSaveLoadData data) { m_data = data; }
            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_data;
                return data != null;
            }
            private ProfilerAnalyticsSaveLoadData m_data;
        }

        [AnalyticInfo(eventName: k_ProfilerConnection, vendorKey: k_VendorKey)]
        internal class ConnectionEvent : IAnalytic
        {
            public ConnectionEvent(ProfilerAnalyticsConnectionData data) { m_data = data; }
            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_data;
                return data != null;
            }
            private ProfilerAnalyticsConnectionData m_data;
        }

        [AnalyticInfo(eventName: k_ProfilerCapture, vendorKey: k_VendorKey)]
        internal class CaptureEvent : IAnalytic
        {
            public CaptureEvent(ProfilerAnalyticsCapture data) { m_data = data; }
            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_data;
                return data != null;
            }
            private ProfilerAnalyticsCapture m_data;
        }

        [AnalyticInfo(eventName: k_BottlenecksModuleLinkSelected, vendorKey: k_VendorKey)]
        internal class BottleneckLinkEvent : IAnalytic
        {
            public BottleneckLinkEvent(BottleneckLink data) { m_data = data; }
            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_data;
                return data != null;
            }
            private BottleneckLink m_data;
        }

        

        /// <summary>
        /// Reset analytics tracking on profiler window open and record the session start time.
        /// </summary>
        public static void OnProfilerWindowAwake()
        {
            ClearElementUsabilityQueue(EditorApplication.timeSinceStartup);
        }

        /// <summary>
        /// Sent out all accumulated view change events and reset analytics tracking.
        /// </summary>
        public static void OnProfilerWindowDestroy()
        {
            SendElementUsabilityEventsAndClearQueue();
        }

        /// <summary>
        /// Increment mouse event count for the current session and view.
        /// </summary>
        public static void RecordMouseDownUsabilityEvent()
        {
            if (s_Views == null || s_CurrentViewIndex == -1)
                return;

            s_ProfilerSession.mouseEvents++;

            var view = s_Views[s_CurrentViewIndex];
            view.mouseEvents++;
            s_Views[s_CurrentViewIndex] = view;
        }

        /// <summary>
        /// Increment key event count for the current session and view.
        /// </summary>
        public static void RecordKeyDownUsabilityEvent()
        {
            if (s_Views == null || s_CurrentViewIndex == -1)
                return;

            s_ProfilerSession.keyboardEvents++;

            var view = s_Views[s_CurrentViewIndex];
            view.keyboardEvents++;
            s_Views[s_CurrentViewIndex] = view;
        }

        /// <summary>
        /// The function will accumulate a sequence of view changes and will send out an event once we get into the view switch cycles.
        /// E.g. when user goes through CPU Hierarchy view, CPU Timeline view and then Memory module we will send an array of ["CPU;Hierarchy", "CPU;Timeline", "Memory"].
        /// But for the sequence CPU View, Memory View, CPU View we will send 2 events - ["CPU;Hierarchy", "Memory"] and ["CPU;Hierarchy"].
        /// This way we can track individual views usage as well as detect view flows.
        /// </summary>
        /// <param name="element">View name</param>
        public static void SwitchActiveView(string element)
        {
            if (string.IsNullOrEmpty(element))
                throw new ArgumentNullException(nameof(element));

            // Selecting the same element
            if (s_Views != null && s_CurrentViewIndex >= 0 && s_CurrentViewIndex < s_Views.Count && s_Views[s_CurrentViewIndex].element == element)
                return;


            // Check if we start a new sequence or continuing the old one
            s_Views ??= new List<ProfilerAnalyticsViewUsability>();
            var idx = s_Views.FindIndex(x => x.element == element);
            if (idx != -1)
            {
                // Views looped, send data out and start a new sequence.
                // This way we are able to track sequences and bundle events together to avoid frequent sends.
                SendElementUsabilityEventsAndClearQueue();
            }
            // New element has been selected.
            // Update time of the current view
            var currentTime = EditorApplication.timeSinceStartup;
            if (s_CurrentViewIndex != -1 && s_CurrentViewIndex < s_Views.Count)
            {
                var view = s_Views[s_CurrentViewIndex];
                view.time = currentTime - view.time;
                s_Views[s_CurrentViewIndex] = view;
            }

            // Add new view to the array and record the activation time.
            s_Views.Add(new ProfilerAnalyticsViewUsability()
            {
                element = element,
                mouseEvents = 0,
                keyboardEvents = 0,
                time = currentTime,
            });
            s_CurrentViewIndex = s_Views.Count - 1;
        }

        static void SendElementUsabilityEventsAndClearQueue()
        {
            // Check if there is anything to send
            if (s_Views == null || s_Views.Count == 0)
                return;

            if (s_AnalyticsService == null)
                return;

            // Update duration for the current view and send all accumulated events.
            var view = s_Views[s_CurrentViewIndex];
            view.time = EditorApplication.timeSinceStartup - view.time;
            s_Views[s_CurrentViewIndex] = view;

            var currentTime = EditorApplication.timeSinceStartup;
            s_ProfilerSession.session = s_Views.ToArray();
            s_ProfilerSession.time = currentTime - s_ProfilerSession.time;

            ProfilerWindowDestroy analytic = new ProfilerWindowDestroy();
            s_AnalyticsService.SendAnalytic(analytic);


            ClearElementUsabilityQueue(currentTime);
        }

        static void ClearElementUsabilityQueue(double startTime)
        {
            s_ProfilerSession = new ProfilerAnalyticsViewUsabilitySession()
            {
                session = null,
                mouseEvents = 0,
                keyboardEvents = 0,
                time = EditorApplication.timeSinceStartup,
            };
            s_Views?.Clear();
            s_CurrentViewIndex = -1;
        }

        /// <summary>
        /// Send event when we start getting data from the stream
        /// </summary>
        public static void StartCapture()
        {
            if (s_AnalyticsService == null)
                return;

            var captureEvt = new ProfilerAnalyticsCapture
            {
                callStacksEnabled = ProfilerDriver.memoryRecordMode != 0,
                deepProfileEnabled = ProfilerDriver.deepProfiling,
                // Includes platform name or Editor is profiling Editor
                connectionName = ProfilerDriver.GetConnectionIdentifier(ProfilerDriver.connectedProfiler),
                // Editor platform name
                platformName = Application.platform.ToString()
            };

            CaptureEvent analytic = new CaptureEvent(captureEvt);
            s_AnalyticsService.SendAnalytic(analytic);
        }

        public static void SendSaveLoadEvent(ProfilerAnalyticsSaveLoadData data)
        {
          if (s_AnalyticsService == null)
                return;

            SaveLoadEvent analytic = new SaveLoadEvent(data);
            s_AnalyticsService.SendAnalytic(analytic);
        }

        public static void SendConnectionEvent(ProfilerAnalyticsConnectionData data)
        {
            if (s_AnalyticsService == null)
                return;
            ConnectionEvent analytic = new ConnectionEvent(data);
            s_AnalyticsService.SendAnalytic(analytic);
        }

        public static void SendBottleneckLinkSelectedEvent(string linkDescription)
        {
            if (s_AnalyticsService == null)
                return;

            var data = new BottleneckLink
            {
                linkDescription = linkDescription
            };

            BottleneckLinkEvent analytic = new BottleneckLinkEvent(data);
            s_AnalyticsService.SendAnalytic(analytic);

        }
    }
}
