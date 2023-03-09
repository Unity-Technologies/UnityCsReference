// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Scripting;

namespace UnityEditor.Profiling.Analytics
{
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ProfilerAnalyticsSaveLoadData
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
    internal struct ProfilerAnalyticsConnectionData
    {
        public bool success;
        public string connectionDetail;
        public string details;
    }

    [Serializable]
    internal struct ProfilerAnalyticsViewUsability
    {
        public string element;
        public uint mouseEvents;
        public uint keyboardEvents;
        public double time;
    }

    [Serializable]
    internal struct ProfilerAnalyticsViewUsabilitySession
    {
        public ProfilerAnalyticsViewUsability[] session;
        public uint mouseEvents;
        public uint keyboardEvents;
        public double time;
    }

    [Serializable]
    internal struct ProfilerAnalyticsCapture
    {
        public bool deepProfileEnabled;
        public bool callStacksEnabled;
        public string platformName; // Editor is platform
        public string connectionName;
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

        static IAnalyticsService s_AnalyticsService;

        // events registered
        static bool s_ProfilerElementUsabilityRegistered;
        static bool s_ProfilerCaptureRegistered;
        static bool s_ProfilerSaveLoadRegistered;
        static bool s_ProfilerConnectionRegistered;

        static ProfilerAnalyticsViewUsabilitySession s_ProfilerSession;
        static List<ProfilerAnalyticsViewUsability> s_Views;
        static int s_CurrentViewIndex;

        static ProfilerWindowAnalytics()
        {
            // Instantiate default editor analytics, but only for user sessions when analytics is enabled.
            if (!InternalEditorUtility.inBatchMode && EditorAnalytics.enabled)
                SetAnalyticsService(new EditorAnalyticsService());
        }

        static bool RegisterEvent(ref bool eventRegisteredFlag, string eventName)
        {
            if (s_AnalyticsService == null)
                return false;

            if (!eventRegisteredFlag)
                eventRegisteredFlag = s_AnalyticsService.RegisterEventWithLimit(eventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

            return eventRegisteredFlag;
        }

        public static IAnalyticsService SetAnalyticsService(IAnalyticsService service)
        {
            var oldService = s_AnalyticsService;
            s_AnalyticsService = service;

            // Reset events registration
            s_ProfilerElementUsabilityRegistered = false;
            s_ProfilerCaptureRegistered = false;
            s_ProfilerSaveLoadRegistered = false;
            s_ProfilerConnectionRegistered = false;

            return oldService;
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

            if (!RegisterEvent(ref s_ProfilerElementUsabilityRegistered, k_ProfilerElementUsability))
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

            if (!RegisterEvent(ref s_ProfilerElementUsabilityRegistered, k_ProfilerElementUsability))
                return;

            // Update duration for the current view and send all accumulated events.
            var view = s_Views[s_CurrentViewIndex];
            view.time = EditorApplication.timeSinceStartup - view.time;
            s_Views[s_CurrentViewIndex] = view;

            var currentTime = EditorApplication.timeSinceStartup;
            s_ProfilerSession.session = s_Views.ToArray();
            s_ProfilerSession.time = currentTime - s_ProfilerSession.time;
            s_AnalyticsService.SendEventWithLimit(k_ProfilerElementUsability, s_ProfilerSession);

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
            if (!RegisterEvent(ref s_ProfilerCaptureRegistered, k_ProfilerCapture))
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
            s_AnalyticsService.SendEventWithLimit(k_ProfilerCapture, captureEvt);
        }

        public static void SendSaveLoadEvent(ProfilerAnalyticsSaveLoadData data)
        {
            if (!RegisterEvent(ref s_ProfilerSaveLoadRegistered, k_ProfilerSaveLoad))
                return;

            s_AnalyticsService.SendEventWithLimit(k_ProfilerSaveLoad, data);
        }

        public static void SendConnectionEvent(ProfilerAnalyticsConnectionData data)
        {
            if (!RegisterEvent(ref s_ProfilerConnectionRegistered, k_ProfilerConnection))
                return;

            s_AnalyticsService.SendEventWithLimit(k_ProfilerConnection, data);
        }
    }
}
