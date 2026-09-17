// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using UnityEngine.Internal;
using UnityEngine.ManagedCapture.Internal;

namespace UnityEngine.ManagedCapture
{
    // The namespace and class name must match what ManagedCaptureInjectionStep searches for. Adding an
    // overload here makes it available to the IL injection step on the next linker build.
    [ExcludeFromDocs]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ManagedCaptureHooks
    {
        // eventType is the config's per-class number.
        public static void OnMethodCalled(int eventType, string mediator, string method)
            => ManagedCaptureHookEngine.Dispatch(eventType, mediator, method);

        public static void OnMethodCalled<T1>(int eventType, string mediator, string method, T1 p0)
            => ManagedCaptureHookEngine.Dispatch(eventType, mediator, method, p0);

        public static void OnMethodCalled<T1, T2>(int eventType, string mediator, string method, T1 p0, T2 p1)
            => ManagedCaptureHookEngine.Dispatch(eventType, mediator, method, p0, p1);

        public static void OnMethodCalled<T1, T2, T3>(int eventType, string mediator, string method, T1 p0, T2 p1, T3 p2)
            => ManagedCaptureHookEngine.Dispatch(eventType, mediator, method, p0, p1, p2);

        public static void OnMethodCalled<T1, T2, T3, T4>(int eventType, string mediator, string method, T1 p0, T2 p1, T3 p2, T4 p3)
            => ManagedCaptureHookEngine.Dispatch(eventType, mediator, method, p0, p1, p2, p3);

        public static void OnMethodCalled(int eventType, string mediator, string method, object[] args)
            => ManagedCaptureHookEngine.DispatchFallback(eventType, mediator, method, args);

        // eventType is the config's per-signal number, authored to match the unityapis registry's
        // value for this (mediator, l1Event) pair.
        public static void BeginEvent(int eventType, string l1Event, string mediator, string sourceMethod)
            => ManagedCaptureHookEngine.BeginEvent(eventType, l1Event, mediator, sourceMethod);

        public static void Field(string name, string value) => ManagedCaptureHookEngine.Field(name, value);

        public static void Field(string name, double value) => ManagedCaptureHookEngine.Field(name, value);

        public static void Field(string name, long value) => ManagedCaptureHookEngine.Field(name, value);

        public static void Field(string name, int value) => ManagedCaptureHookEngine.Field(name, value);

        public static void Field(string name, bool value) => ManagedCaptureHookEngine.Field(name, value);

        public static void EndEvent() => ManagedCaptureHookEngine.EndEvent();

        public static string MapValue(string raw, string mapSpec) => ManagedCaptureHookEngine.MapValue(raw, mapSpec);

        public static void SetCorrelation(object key, string field, int role) => ManagedCaptureHookEngine.SetCorrelation(key, field, role);

        public static Action<T> WrapAction<T>(int eventType, string mediator, string callbackRole, Action<T> userHandler)
        {
            if (userHandler == null)
                return null;
            return arg =>
            {
                // Explicit type argument: with T = object[], inference would bind the object[] overload.
                try
                {
                    OnMethodCalled<T>(eventType, mediator, callbackRole, arg);
                }
                catch (Exception exception)
                {
                    ReportWrapFailure(typeof(T).Name, exception);
                }
                userHandler(arg);
            };
        }

        public static Action<T1, T2> WrapAction<T1, T2>(int eventType, string mediator, string callbackRole, Action<T1, T2> userHandler)
        {
            if (userHandler == null)
                return null;
            return (a, b) =>
            {
                try
                {
                    OnMethodCalled<T1, T2>(eventType, mediator, callbackRole, a, b);
                }
                catch (Exception exception)
                {
                    ReportWrapFailure(typeof(T1).Name + "," + typeof(T2).Name, exception);
                }
                userHandler(a, b);
            };
        }

        static void ReportWrapFailure(string typeNames, Exception exception)
            => ManagedCaptureHookEngine.SafeLogWarning($"[ManagedCapture] WrapAction<{typeNames}> hook threw: {exception.Message}");
    }
}
