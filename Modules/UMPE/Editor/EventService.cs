// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;

namespace Unity.MPE
{
    internal delegate object OnHandler(string eventType, object[] data);
    internal delegate void PromiseHandler(Exception err, object[] data);

    internal partial class EventService
    {
        internal class RequestData
        {
            public string eventType;
            public int id;
            public List<PromiseHandler> promises;
            public long offerStartTime;
            public bool isAcknowledged;
            public string data;
            public int timeout;
        }

        public const string kRequest = "request";
        public const string kRequestAcknowledge = "requestAck";
        public const string kRequestExecute = "requestExecute";
        public const string kRequestResult = "requestResult";

        public const string kEvent = "event";
        public const int TicksPerMillisecond = 10000;
        public const int kRequestTimeout = 700 * TicksPerMillisecond;

        internal static Dictionary<string, List<OnHandler>> s_Events = new Dictionary<string, List<OnHandler>>();
        internal static Dictionary<string, RequestData> s_Requests = new Dictionary<string, RequestData>();

        public static Action On(string eventType, OnHandler handler)
        {
            // Note: User will need to register on domain reload...
            List<OnHandler> handlers = null;
            if (!s_Events.TryGetValue(eventType, out handlers))
            {
                handlers = new List<OnHandler> {handler};
                s_Events.Add(eventType, handlers);
            }
            else if (handlers.Contains(handler))
            {
                throw new Exception("Cannot add existing event handler: " + eventType);
            }
            else
            {
                handlers.Add(handler);
            }

            return () =>
            {
                Off(eventType, handler);
            };
        }

        public static void Off(string eventType, OnHandler handler)
        {
            List<OnHandler> handlers = null;
            if (s_Events.TryGetValue(eventType, out handlers))
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    s_Events.Remove(eventType);
                }
            }
        }

        public static void Clear()
        {
            s_Events = new Dictionary<string, List<OnHandler>>();
            s_Requests = new Dictionary<string, RequestData>();
            EditorApplication.update -= TickPendingOffers;
        }

        public static bool IsConnected => ConnectionId != -1;

        public static void Emit(string eventType, params object[] args)
        {
            const bool notifyWildcard = true;
            var req = CreateRequestMsg(kEvent, eventType, -1, args);

            // TODO: do we want to ensure that all local listeners received json as payload? This means we could recycle handlers... If so we need to serialize/deserialize... ugly. real ugly...
            var reqStr = Json.Serialize(req);
            var reqJson = Json.Deserialize(reqStr) as Dictionary<string, object>;

            NotifyLocalListeners(eventType, reqJson["data"] as object[], notifyWildcard);

            Send(reqStr);
        }

        public static void Request(string eventType, PromiseHandler promiseHandler, params object[] args)
        {
            Request(eventType, promiseHandler, args, kRequestTimeout);
        }

        public static void Request(string eventType, PromiseHandler promiseHandler, object[] args, int timeout)
        {
            RequestData request;
            if (s_Requests.TryGetValue(eventType, out request))
            {
                request.promises.Add(promiseHandler);
                return;
            }

            request = new RequestData { eventType = eventType, promises = new List<PromiseHandler>(1), timeout = timeout };
            request.promises.Add(promiseHandler);

            var req = CreateRequestMsg(kRequest, eventType, -1, args);
            var requestId = NewRequestId();
            req["requestId"] = requestId;

            if (HasHandlers(eventType))
            {
                var results = NotifyLocalListeners(eventType, args, false);
                var exception = results.FirstOrDefault(r => r is Exception);
                if (exception != null)
                {
                    Reject(request, exception as Exception);
                }
                else
                {
                    if (results.Length == 1)
                    {
                        Resolve(request, results);
                    }
                    else
                    {
                        Resolve(request, results);
                    }
                }
            }
            else
            {
                request.id = requestId;
                request.offerStartTime = Stopwatch.GetTimestamp();
                if (s_Requests.Count == 0)
                    EditorApplication.update += TickPendingOffers;

                s_Requests.Add(eventType, request);
                request.data = Json.Serialize(args);
                var msg = Json.Serialize(req);
                Send(msg);
            }
        }

        [RequiredByNativeCode]
        public static void IncomingEvent(string eventMsg)
        {
            Debug.Log("eventService: " + eventMsg);
            var msg = Json.Deserialize(eventMsg) as Dictionary<string, object>;
            if (msg == null)
            {
                Debug.LogError("Invalid message: " + eventMsg);
                return;
            }

            if (!msg.ContainsKey("type"))
            {
                Debug.LogError("Message doesn't contain type: " + eventMsg);
                return;
            }

            if (!msg.ContainsKey("req"))
            {
                Debug.LogError("Message doesn't contain req: " + eventMsg);
                return;
            }

            if (!msg.ContainsKey("senderId"))
            {
                Debug.LogError("Message doesn't contain senderId: " + eventMsg);
                return;
            }

            var reqType = msg["req"].ToString();
            var eventType = msg["type"].ToString();
            var senderId = Convert.ToInt32(msg["senderId"]);

            if (senderId == ConnectionId)
            {
                return;
            }

            object dataObj = null;
            msg.TryGetValue("data", out dataObj);
            object[] data = null;
            if (dataObj != null)
            {
                data = dataObj is List<object>? (dataObj as List<object>).ToArray() : new[] { dataObj };
            }

            switch (reqType)
            {
                case kRequest:
                    // We are able to answer this request. Acknowledge it to the sender:
                    if (HasHandlers(eventType))
                    {
                        var response = CreateRequestMsg(kRequestAcknowledge, eventType, senderId, null);
                        response["requestId"] = msg["requestId"];
                        Send(Json.Serialize(response));
                    }
                    break;
                case kRequestAcknowledge:
                    var pendingRequest = GetPendingRequest(eventType, msg);
                    if (pendingRequest != null)
                    {
                        // A client is able to fulfill the request: proceed with request execution:
                        pendingRequest.isAcknowledged = true;
                        var response = CreateRequestMsg(kRequestExecute, eventType, senderId, null);
                        response["requestId"] = msg["requestId"];
                        response["data"] = pendingRequest.data;
                        Send(Json.Serialize(response));
                    }
                    // else Request might potentially have timed out.
                    break;
                case kRequestExecute:
                {
                    // We are fulfilling the request: send the execution results
                    const bool notifyWildcard = false;
                    var results = NotifyLocalListeners(eventType, data, notifyWildcard);
                    var response = CreateRequestMsg(kRequestResult, eventType, senderId, results);
                    response["requestId"] = msg["requestId"];
                    Send(Json.Serialize(response));
                    break;
                }
                case kRequestResult:
                    var pendingRequestAwaitingResult = GetPendingRequest(eventType, msg);
                    if (pendingRequestAwaitingResult != null)
                    {
                        Resolve(pendingRequestAwaitingResult, data);
                        CleanRequest(eventType);
                    }
                    break;
                case kEvent:
                {
                    const bool notifyWildcard = true;
                    NotifyLocalListeners(eventType, data, notifyWildcard);
                    break;
                }
            }
        }

        private static void Resolve(RequestData offer, object[] results)
        {
            for (int i = 0, end = offer.promises.Count; i != end; ++i)
                offer.promises[i](null, results);
        }

        private static void Reject(RequestData offer, Exception err)
        {
            for (int i = 0, end = offer.promises.Count; i != end; ++i)
                offer.promises[i](err, null);
        }

        private static RequestData GetPendingRequest(string eventType, Dictionary<string, object> msg)
        {
            var offerId = Convert.ToInt32(msg["requestId"]);
            RequestData pendingRequest;
            s_Requests.TryGetValue(eventType, out pendingRequest);
            if (pendingRequest != null && pendingRequest.id != offerId)
            {
                // Mismatch request: clean it.
                CleanRequest(eventType);
                pendingRequest = null;
            }
            return pendingRequest != null && pendingRequest.id == offerId ? pendingRequest : null;
        }

        private static void CleanRequest(string eventType)
        {
            s_Requests.Remove(eventType);
            if (s_Requests.Count == 0)
                EditorApplication.update -= TickPendingOffers;
        }

        private static bool HasHandlers(string eventType)
        {
            List<OnHandler> handlers;
            return s_Events.TryGetValue(eventType, out handlers) && handlers.Count > 0;
        }

        private static void TickPendingOffers()
        {
            var now = Stopwatch.GetTimestamp();
            var pendingRequests = s_Requests.Values.ToArray();
            foreach (var request in pendingRequests)
            {
                if (request.isAcknowledged)
                    continue;
                var elapsedTime = now - request.offerStartTime;
                if (elapsedTime > request.timeout)
                {
                    Reject(request, new Exception("timeout"));
                    s_Requests.Remove(request.eventType);
                }
            }
        }

        private static object[] NotifyLocalListeners(string eventType, object[] data, bool notifyWildcard)
        {
            List<OnHandler> handlers = null;
            var result = new object[0];
            if (s_Events.TryGetValue(eventType, out handlers))
            {
                try
                {
                    result = handlers.Select(handler => handler(eventType, data)).ToArray();
                }
                catch (Exception e)
                {
                    result = new object[] { e };
                }
            }

            if (notifyWildcard && s_Events.TryGetValue("*", out handlers))
            {
                try
                {
                    foreach (var handler in handlers)
                    {
                        handler(eventType, data);
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        private static Dictionary<string, object> CreateRequestMsg(string msgType, string eventType, int targetId, object args)
        {
            var req = new Dictionary<string, object> {["req"] = msgType};
            if (targetId != -1)
            {
                req["targetId"] = targetId;
            }
            req["type"] = eventType;
            req["senderId"] = ConnectionId;
            req["data"] = args;
            return req;
        }
    }
}
