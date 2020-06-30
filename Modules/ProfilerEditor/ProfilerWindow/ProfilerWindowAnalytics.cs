// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Profiling;
using UnityEngine.Scripting;

namespace UnityEditor
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

    internal struct ProfilerAnalyticsViewUsability
    {
        public string element;
        public uint mouseEvents;
        public uint keyboardEvents;
        public string param;
        public double time;
    }
    internal struct ProfilerAnalyticsCapture
    {
        public bool deepProfileEnabled;
        public bool callStacksEnabled;
        public string platformName; // Editor is platform
        public string connectionName;
    }


    internal class ProfilerWindowAnalytics
    {
        const int k_MaxEventsPerHour = 1000; // Max Events send per hour.
        const int k_MaxNumberOfElements = 1000; //Max number of elements sent.
        const string k_VendorKey = "unity.profiler";
        const string k_ProfilerSaveLoad = "profilerSaveLoad";
        const string k_ProfilerConnection = "profilerConnection";
        const string k_ProfilerElementUsability = "profilerElementUsability";
        const string k_ProfilerCapture = "profilerCapture";

        // events registered
        static bool profilerElementUsabilityRegistered;
        static bool profilerCaptureRegistered;
        static bool profilerSaveLoadRegistered;
        static bool profilerConnectionRegistered;

        // known elements
        public static string profilerWindowElement = "profilerwindow";
        public static string profilerCPUModule = profilerWindowElement + ".cpu";
        public static string profilerCPUModuleTimeline = profilerCPUModule + ".timeline";
        public static string profilerCPUModuleHierarchy = profilerCPUModule + ".hierarchy";
        public static string profilerCPUModuleSearch = profilerCPUModule + ".search";

        static ProfilerAnalyticsViewUsability ProfilerSession;
        static ProfilerAnalyticsViewUsability CurrentView;
        static List<ProfilerAnalyticsViewUsability> Views;

        static ProfilerAnalyticsCapture Capture;
        static bool Capturing;

        public static void OnProfilerWindowFocused()
        {
            ProfilerSession = new ProfilerAnalyticsViewUsability();
            ProfilerSession.element = profilerWindowElement;
            ProfilerSession.time = EditorApplication.timeSinceStartup;

            Views = new List<ProfilerAnalyticsViewUsability>();
            CurrentView = new ProfilerAnalyticsViewUsability();
        }

        static bool RegisterEvent(string eventName)
        {
            var analyticsResult = EditorAnalytics.RegisterEventWithLimit(eventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);
            return analyticsResult == AnalyticsResult.Ok;
        }

        public static void OnProfilerWindowLostFocus()
        {
            ProfilerSession.time = EditorApplication.timeSinceStartup - ProfilerSession.time;
            if (!EditorAnalytics.enabled)
                return;

            if (!profilerElementUsabilityRegistered)
                profilerElementUsabilityRegistered = RegisterEvent(k_ProfilerElementUsability);

            // no interaction during session
            if (ProfilerSession.keyboardEvents == 0 && ProfilerSession.mouseEvents == 0)
                return;

            Views.Insert(0, ProfilerSession);
            Views.Add(CurrentView);
            EditorAnalytics.SendEventWithLimit(k_ProfilerElementUsability, Views.ToArray());
        }

        public static void SendSaveLoadEvent(ProfilerAnalyticsSaveLoadData data)
        {
            if (!EditorAnalytics.enabled)
                return;

            if (!profilerSaveLoadRegistered)
                profilerSaveLoadRegistered = RegisterEvent(k_ProfilerSaveLoad);

            EditorAnalytics.SendEventWithLimit(k_ProfilerSaveLoad, data);
        }

        public static void SendConnectionEvent(ProfilerAnalyticsConnectionData data)
        {
            if (!EditorAnalytics.enabled)
                return;

            if (data.success)
                data.connectionDetail = ProfilerDriver.GetConnectionIdentifier(ProfilerDriver.connectedProfiler);

            if (!profilerConnectionRegistered)
                profilerConnectionRegistered = RegisterEvent(k_ProfilerConnection);

            EditorAnalytics.SendEventWithLimit(k_ProfilerConnection, data);
        }

        public static void RecordProfilerSessionMouseEvent()
        {
            ProfilerSession.mouseEvents++;
        }

        public static void RecordProfilerSessionKeyboardEvent()
        {
            ProfilerSession.keyboardEvents++;
        }

        public static void RecordViewMouseEvent(string element)
        {
            if (CurrentView.element == element)
            {
                CurrentView.mouseEvents++;
                return;
            }

            AddNewView(element);
            CurrentView.mouseEvents++;
        }

        public static void RecordViewKeyboardEvent(string element)
        {
            if (CurrentView.element == element)
            {
                CurrentView.keyboardEvents++;
                return;
            }

            AddNewView(element);
            CurrentView.keyboardEvents++;
        }

        public static void AddNewView(string element)
        {
            if (!string.IsNullOrEmpty(CurrentView.element))
            {
                CurrentView.time = EditorApplication.timeSinceStartup - CurrentView.time;
                Views.Add(CurrentView);
            }

            CurrentView = new ProfilerAnalyticsViewUsability();
            CurrentView.time = EditorApplication.timeSinceStartup;
            CurrentView.element = element;
        }

        public static void ProfilingStateChange(bool state)
        {
            if (state == Capturing) return;

            if (state)
                StartCapture();
            else
            {
                StopCapture();
            }
        }

        public static void StartCapture()
        {
            Capturing = true;
            Capture = new ProfilerAnalyticsCapture();

            Capture.callStacksEnabled = Profiler.enableAllocationCallstacks;
            Capture.deepProfileEnabled = ProfilerDriver.deepProfiling;
            Capture.connectionName = ProfilerDriver.GetConnectionIdentifier(ProfilerDriver.connectedProfiler);
            Capture.platformName = ProfilerDriver.profileEditor ? "Editor" : "";
        }

        public static void StopCapture()
        {
            if (!Capturing)
                return;
            if (!EditorAnalytics.enabled)
                return;
            Capturing = false;

            if (!profilerCaptureRegistered)
                profilerCaptureRegistered = RegisterEvent(k_ProfilerCapture);

            EditorAnalytics.SendEventWithLimit(k_ProfilerCapture, Capture);
        }
    }
}
