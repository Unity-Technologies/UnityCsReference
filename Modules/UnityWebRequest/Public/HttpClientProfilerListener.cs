// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace UnityEngine.Networking
{
    // Records HttpClient requests that never reach UnityWebRequest, through HttpClient's DiagnosticListener.
    // While subscribed, .NET stamps `traceparent` on every outbound request, so keep it attached only while capturing.
    [VisibleToOtherModules("UnityEditor.NetworkProfilerModule")]
    internal static class HttpClientProfilerListener
    {
        const string k_DiagnosticListenerName = "HttpHandlerDiagnosticListener";
        const string k_ActivityName = "System.Net.Http.HttpRequestOut";
        const string k_StartEventName = "System.Net.Http.HttpRequestOut.Start";
        const string k_StopEventName = "System.Net.Http.HttpRequestOut.Stop";
        const string k_RequestIdProperty = "UnityWebRequestProfilerId";

        // Fallback naming only: .NET redacts the query in url.full.
        const string k_TagUrl = "url.full";
        const string k_TagMethod = "http.request.method";
        const string k_TagStatus = "http.response.status_code";

        // Nulling these on reload would leave them subscribed with nothing to dispose them.
        [NoAutoStaticsCleanup]
        static bool s_Listening;

        [NoAutoStaticsCleanup]
        static IDisposable s_AllListenersSubscription;

        [NoAutoStaticsCleanup]
        static IDisposable s_EventsSubscription;

        // .NET hands over the request in IsEnabled and raises the start event next, on the same thread.
        [ThreadStatic]
        [NoAutoStaticsCleanup]
        static HttpRequestMessage t_PendingRequest;

        // Removing an entry under the lock is what earns the right to settle that request, so a
        // stop racing StopListening cannot settle it twice and wrap the unsigned active gauge.
        [NoAutoStaticsCleanup]
        static readonly List<(Activity activity, ulong id, bool named)> s_InFlight = new List<(Activity, ulong, bool)>();

        [NoAutoStaticsCleanup]
        static readonly object s_InFlightLock = new object();

        [VisibleToOtherModules("UnityEditor.NetworkProfilerModule")]
        internal static bool isListening => s_Listening;

        [VisibleToOtherModules("UnityEditor.NetworkProfilerModule")]
        internal static void StartListening()
        {
            if (s_Listening)
                return;

            s_Listening = true;
            s_AllListenersSubscription = DiagnosticListener.AllListeners.Subscribe(new DiagnosticListenerObserver());
        }

        [VisibleToOtherModules("UnityEditor.NetworkProfilerModule")]
        internal static void StopListening()
        {
            if (!s_Listening)
                return;

            s_AllListenersSubscription?.Dispose();
            s_AllListenersSubscription = null;

            // Disposing does not wait for callbacks already running on .NET's threads.
            lock (s_InFlightLock)
            {
                s_Listening = false;
                s_EventsSubscription?.Dispose();
                s_EventsSubscription = null;

                for (var i = 0; i < s_InFlight.Count; ++i)
                    WebRequestProfilerCapture.RequestAbandoned(s_InFlight[i].id);

                WebRequestProfilerCounters.RequestsAbandoned((uint)s_InFlight.Count);
                s_InFlight.Clear();
            }
        }

        // Only reached with the request when no ActivityListener is on System.Net.Http and no activity is current;
        // otherwise .NET skips this call and the row falls back to the redacted url.full.
        static bool IsEnabled(string name, object context, object _)
        {
            if (name == k_ActivityName)
            {
                if (context is HttpRequestMessage request)
                    t_PendingRequest = request;
                return true;
            }

            return name == k_StartEventName || name == k_StopEventName;
        }

        static void OnStart(Activity activity)
        {
            var request = t_PendingRequest;
            t_PendingRequest = null;

            var redactedUrl = TagString(activity, k_TagUrl);
            if (request?.RequestUri != null && redactedUrl.Length > 0 && !SameRequest(request.RequestUri, redactedUrl))
                request = null;

            var url = request?.RequestUri?.AbsoluteUri ?? redactedUrl;
            var method = request?.Method.Method ?? TagString(activity, k_TagMethod);

            ulong id;
            lock (s_InFlightLock)
            {
                if (!s_Listening)
                    return;

                id = WebRequestProfilerCapture.RequestStarted(url, method, UnityWebRequest.k_ProfilerSourceHttpClientDiagnostics);
                if (id == WebRequestProfilerCapture.k_InvalidRequestId)
                    return;

                // Counted only once tracked: a refused request has no stop path to bring the gauge back down.
                WebRequestProfilerCounters.RequestStarted();

                WebRequestProfilerCapture.TransportAttached(id, measurable: 0, canReportBodies: false);
                activity.SetCustomProperty(k_RequestIdProperty, id);
                s_InFlight.Add((activity, id, named: url.Length > 0));
            }

            if (request != null)
                CaptureConfiguredHeaders(id, request);
        }

        // HttpClient has merged its DefaultRequestHeaders in by now; what the handler adds later is not here.
        static void CaptureConfiguredHeaders(ulong id, HttpRequestMessage request)
        {
            CaptureHeaderLines(id, request.Headers.ToString());
            if (request.Content != null)
                CaptureHeaderLines(id, request.Content.Headers.ToString());
        }

        // ToString reads the raw values; enumerating parses them, and .NET then sends the normalized form.
        static void CaptureHeaderLines(ulong id, string headers)
        {
            foreach (var line in headers.Split('\n'))
            {
                var separator = line.IndexOf(':');
                if (separator <= 0)
                    continue;

                var start = separator + 1 < line.Length && line[separator + 1] == ' ' ? separator + 2 : separator + 1;
                WebRequestProfilerCapture.RequestHeader(id, line.Substring(0, separator), line.Substring(start).TrimEnd('\r'));
            }
        }

        // Fallback for a request started without its url, polled each editor frame.
        [VisibleToOtherModules("UnityEditor.NetworkProfilerModule")]
        internal static void NameStartedRequests()
        {
            lock (s_InFlightLock)
            {
                for (var i = 0; i < s_InFlight.Count; ++i)
                {
                    var (activity, id, named) = s_InFlight[i];
                    if (named)
                        continue;

                    var url = TagString(activity, k_TagUrl);
                    if (url.Length == 0)
                        continue;

                    WebRequestProfilerCapture.RequestDetails(id, url, TagString(activity, k_TagMethod));
                    s_InFlight[i] = (activity, id, named: true);
                }
            }
        }

        static void OnStop(Activity activity)
        {
            if (activity.GetCustomProperty(k_RequestIdProperty) is not ulong id)
                return;

            var owned = false;
            var named = false;
            lock (s_InFlightLock)
            {
                for (var i = s_InFlight.Count - 1; i >= 0; --i)
                {
                    if (s_InFlight[i].id != id)
                        continue;

                    named = s_InFlight[i].named;
                    s_InFlight.RemoveAt(i);
                    owned = true;
                    break;
                }
            }

            if (!owned)
                return;

            // Before completing, which makes the row eligible for the next snapshot.
            if (!named)
                WebRequestProfilerCapture.RequestDetails(id, TagString(activity, k_TagUrl), TagString(activity, k_TagMethod));

            var statusCode = TagStatusCode(activity);
            var succeeded = statusCode != 0 && statusCode < 400;
            WebRequestProfilerCapture.RequestCompleted(id, succeeded, statusCode);
            WebRequestProfilerCounters.RequestFinished(succeeded);
        }

        // url.full differs from the request only by its redacted query.
        static bool SameRequest(Uri requestUri, string redactedUrl)
        {
            var full = requestUri.AbsoluteUri;
            var query = full.IndexOf('?');
            var withoutQuery = query < 0 ? full : full.Substring(0, query);
            return redactedUrl.StartsWith(withoutQuery, StringComparison.Ordinal);
        }

        static string TagString(Activity activity, string tag) => activity.GetTagItem(tag) as string ?? string.Empty;

        // No status tag at all means a transport failure, not status 0.
        static ushort TagStatusCode(Activity activity)
            => activity.GetTagItem(k_TagStatus) is int status && status > 0 && status <= ushort.MaxValue
                ? (ushort)status
                : (ushort)0;

        sealed class DiagnosticListenerObserver : IObserver<DiagnosticListener>
        {
            public void OnNext(DiagnosticListener listener)
            {
                if (listener.Name != k_DiagnosticListenerName)
                    return;

                lock (s_InFlightLock)
                {
                    if (!s_Listening || s_EventsSubscription != null)
                        return;

                    s_EventsSubscription = listener.Subscribe(new EventObserver(), IsEnabled);
                }
            }

            public void OnError(Exception error) {}
            public void OnCompleted() {}
        }

        // Both events fire with the request's activity current.
        sealed class EventObserver : IObserver<KeyValuePair<string, object>>
        {
            public void OnNext(KeyValuePair<string, object> value)
            {
                var activity = Activity.Current;
                if (activity == null)
                    return;

                if (value.Key == k_StartEventName)
                    OnStart(activity);
                else if (value.Key == k_StopEventName)
                    OnStop(activity);
            }

            public void OnError(Exception error) {}
            public void OnCompleted() {}
        }
    }
}
