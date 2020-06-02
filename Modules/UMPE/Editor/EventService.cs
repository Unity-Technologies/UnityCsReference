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
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.MPE
{
    [MovedFrom("Unity.MPE")]
    public enum EventDataSerialization { StandardJson, JsonUtility };

    [MovedFrom("Unity.MPE")]
    public static class EventService
    {
        internal class RequestData
        {
            public string eventType;
            public int id;
            public List<Action<Exception, object[]>> promises;
            public long offerStartTime;
            public bool isAcknowledged;
            public string data;
            public long timeoutInMs;
            public object[] dataInfos;
        }

        private const string k_RequestMsg = "request";
        private const string k_RequestAcknowledgeMsg = "requestAck";
        private const string k_RequestExecuteMsg = "requestExecute";
        private const string k_RequestResultMsg = "requestResult";

        private const string k_EventMsg = "event";
        private const string k_LogMsg = "log";
        private const long k_RequestDefaultTimeout = 700;

        internal static Dictionary<string, List<Func<string, object[], object>>> s_Events = new Dictionary<string, List<Func<string, object[], object>>>();
        internal static Dictionary<string, RequestData> s_Requests = new Dictionary<string, RequestData>();
        internal static ChannelClient m_Client;

        static EventService()
        {
            Start();
        }

        public static void Start()
        {
            if (m_Client != null || isConnected)
                return;

            m_Client = ChannelClient.GetOrCreateClient("event");
            m_Client.RegisterMessageHandler(IncomingEvent);
            m_Client.Start(false);
            int tickCount = 100;
            while (!m_Client.IsConnected() && --tickCount > 0)
            {
                m_Client.Tick();
                System.Threading.Thread.Sleep(10);
            }

            EditorApplication.update += Tick;
        }

        public static void Close()
        {
            Clear();
            m_Client.Close();
            m_Client = null;
            EditorApplication.update -= Tick;
        }

        public static Action RegisterEventHandler(string eventType, Action<string, object[]> handler)
        {
            return RegisterEventHandler(eventType, (type, args) =>
            {
                handler(type, args);
                return null;
            });
        }

        public static Action RegisterEventHandler(string eventType, Func<string, object[], object> handler)
        {
            // Note: User will need to register on domain reload...
            List<Func<string, object[], object>> handlers = null;
            if (!s_Events.TryGetValue(eventType, out handlers))
            {
                handlers = new List<Func<string, object[], object>> { handler};
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
                UnregisterEventHandler(eventType, handler);
            };
        }

        public static void UnregisterEventHandler(string eventType, Func<string, object[], object> handler)
        {
            List<Func<string, object[], object>> handlers = null;
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
            s_Requests = new Dictionary<string, RequestData>();
        }

        public static bool isConnected => m_Client != null && m_Client.IsConnected();

        public static void Emit(string eventType, object args = null, int targetId = -1, EventDataSerialization eventDataSerialization = EventDataSerialization.JsonUtility)
        {
            if (args == null)
                Emit(eventType, null, targetId, eventDataSerialization);
            else
                Emit(eventType, new object[] { args }, targetId, eventDataSerialization);
        }

        public static void Emit(string eventType, object[] args, int targetId = -1, EventDataSerialization eventDataSerialization = EventDataSerialization.JsonUtility)
        {
            const bool notifyWildcard = true;
            var req = CreateRequest(k_EventMsg, eventType, targetId, -1, args, eventDataSerialization);

            // TODO: do we want to ensure that all local listeners received json as payload? This means we could recycle handlers... If so we need to serialize/deserialize... ugly. real ugly...
            var reqStr = Json.Serialize(req);
            var reqJson = Json.Deserialize(reqStr) as Dictionary<string, object>;

            object dataInfos = null;
            reqJson.TryGetValue("dataInfos", out dataInfos);
            var data = GetDataArray(reqJson["data"], dataInfos);


            NotifyLocalListeners(eventType, data, notifyWildcard);

            m_Client.Send(reqStr);
        }

        public static bool IsRequestPending(string eventType)
        {
            return s_Requests.ContainsKey(eventType);
        }

        public static bool CancelRequest(string eventType, string message = null)
        {
            RequestData request;
            if (!s_Requests.TryGetValue(eventType, out request))
                return false;

            CleanRequest(request.eventType);
            Reject(request, new OperationCanceledException(message ?? $"Request {eventType} canceled"));
            return true;
        }

        public static void Request(string eventType, Action<Exception, object[]> promiseHandler, object args = null, long timeoutInMs = k_RequestDefaultTimeout, EventDataSerialization eventDataSerialization = EventDataSerialization.JsonUtility)
        {
            if (args == null)
                Request(eventType, promiseHandler, null, timeoutInMs, eventDataSerialization);
            else
                Request(eventType, promiseHandler, new object[] { args }, timeoutInMs, eventDataSerialization);
        }

        public static void Request(string eventType, Action<Exception, object[]> promiseHandler, object[] args, long timeoutInMs = k_RequestDefaultTimeout, EventDataSerialization eventDataSerialization = EventDataSerialization.JsonUtility)
        {
            RequestData request;
            if (s_Requests.TryGetValue(eventType, out request))
            {
                request.promises.Add(promiseHandler);
                return;
            }

            request = new RequestData { eventType = eventType, promises = new List<Action<Exception, object[]>>(1), timeoutInMs = timeoutInMs };
            request.promises.Add(promiseHandler);

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
                    Resolve(request, results);
                }
            }
            else
            {
                request.offerStartTime = Stopwatch.GetTimestamp();
                var requestId = m_Client.NewRequestId();
                request.id = requestId;

                var msg = CreateRequest(k_RequestMsg, eventType, -1, requestId, args, eventDataSerialization);
                m_Client.Send(Json.Serialize(msg));

                s_Requests.Add(eventType, request);
                request.data = Json.Serialize(msg["data"]);
                request.dataInfos = (object[])msg["dataInfos"];
            }
        }

        public static void Log(string msg)
        {
            var req = CreateRequestMsg(k_LogMsg, null, -1, -1, msg, null);
            m_Client.Send(req);
        }

        internal static bool DeserializeEvent(string eventMsg, out RequestMessage deserializedMessage)
        {
            var msg = Json.Deserialize(eventMsg) as Dictionary<string, object>;
            deserializedMessage = new RequestMessage();
            if (msg == null)
            {
                Debug.LogError("Invalid message: " + eventMsg);
                return false;
            }

            if (!msg.ContainsKey("type"))
            {
                Debug.LogError("Message doesn't contain type: " + eventMsg);
                return false;
            }

            if (!msg.ContainsKey("req"))
            {
                Debug.LogError("Message doesn't contain req: " + eventMsg);
                return false;
            }

            if (!msg.ContainsKey("senderId"))
            {
                Debug.LogError("Message doesn't contain senderId: " + eventMsg);
                return false;
            }


            deserializedMessage.reqType = msg["req"].ToString();
            deserializedMessage.eventType = msg["type"].ToString();
            deserializedMessage.senderId = Convert.ToInt32(msg["senderId"]);
            object requestId;
            if (msg.TryGetValue("requestId", out requestId))
                deserializedMessage.requestId = Convert.ToInt32(requestId);

            if (deserializedMessage.senderId == m_Client.GetChannelClientInfo().connectionId)
            {
                return false;
            }

            msg.TryGetValue("data", out var dataObj);
            object dataInfos = null;
            if (msg.TryGetValue("dataInfos", out dataInfos))
                deserializedMessage.eventDataSerialization = EventDataSerialization.JsonUtility;
            deserializedMessage.data = GetDataArray(dataObj, dataInfos);
            return true;
        }

        internal class RequestMessage
        {
            public string reqType;
            public string eventType;
            public int senderId;
            public int? requestId;
            public object[] data;
            public EventDataSerialization eventDataSerialization;
        }

        [RequiredByNativeCode]
        private static void IncomingEvent(string eventMsg)
        {
            //Console.WriteLine("[UMPE] " + eventMsg);
            RequestMessage msg;
            if (!DeserializeEvent(eventMsg, out msg))
                return;


            switch (msg.reqType)
            {
                case k_RequestMsg: // Receiver
                    // We are able to answer this request. Acknowledge it to the sender:
                    if (HasHandlers(msg.eventType))
                    {
                        var response = CreateRequestMsg(k_RequestAcknowledgeMsg, msg.eventType, msg.senderId, msg.requestId, null, null);
                        m_Client.Send(response);
                    }
                    break;
                case k_RequestAcknowledgeMsg: // Request emitter
                    var pendingRequest = GetPendingRequest(msg.eventType, msg.requestId.Value);
                    if (pendingRequest != null)
                    {
                        // A client is able to fulfill the request: proceed with request execution:
                        pendingRequest.isAcknowledged = true;
                        pendingRequest.offerStartTime = Stopwatch.GetTimestamp();

                        var message = CreateRequestMsgWithDataString(k_RequestExecuteMsg, msg.eventType, msg.senderId, msg.requestId, pendingRequest.data, pendingRequest.dataInfos);
                        m_Client.Send(message);
                    }
                    // else Request might potentially have timed out.
                    break;
                case k_RequestExecuteMsg: // Request receiver
                {
                    // We are fulfilling the request: send the execution results
                    const bool notifyWildcard = false;
                    var results = NotifyLocalListeners(msg.eventType, msg.data, notifyWildcard);
                    var response = CreateRequest(k_RequestResultMsg, msg.eventType, msg.senderId, msg.requestId, results, msg.eventDataSerialization);
                    m_Client.Send(Json.Serialize(response));
                    break;
                }
                case k_RequestResultMsg: // Request emitter
                    var pendingRequestAwaitingResult = GetPendingRequest(msg.eventType, msg.requestId.Value);
                    if (pendingRequestAwaitingResult != null)
                    {
                        var timeForSuccess = new TimeSpan(Stopwatch.GetTimestamp() - pendingRequestAwaitingResult.offerStartTime).TotalMilliseconds;
                        Console.WriteLine($"[UMPE] Request {msg.eventType} successful in {timeForSuccess} ms");
                        Resolve(pendingRequestAwaitingResult, msg.data);
                        CleanRequest(msg.eventType);
                    }
                    break;
                case k_EventMsg:
                {
                    const bool notifyWildcard = true;
                    NotifyLocalListeners(msg.eventType, msg.data, notifyWildcard);
                    break;
                }
            }
        }

        private static void Resolve(RequestData offer, object[] results)
        {
            for (int i = 0, end = offer.promises.Count; i != end; ++i)
            {
                try
                {
                    offer.promises[i](null, results);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private static void Reject(RequestData offer, Exception err)
        {
            for (int i = 0, end = offer.promises.Count; i != end; ++i)
            {
                try
                {
                    offer.promises[i](err, null);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private static RequestData GetPendingRequest(string eventType, int requestId)
        {
            RequestData pendingRequest;
            s_Requests.TryGetValue(eventType, out pendingRequest);
            if (pendingRequest != null && pendingRequest.id != requestId)
            {
                // Mismatch request: clean it.
                CleanRequest(eventType);
                pendingRequest = null;
            }
            return pendingRequest != null && pendingRequest.id == requestId ? pendingRequest : null;
        }

        private static void CleanRequest(string eventType)
        {
            s_Requests.Remove(eventType);
        }

        private static bool HasHandlers(string eventType)
        {
            List<Func<string, object[], object>> handlers;
            return s_Events.TryGetValue(eventType, out handlers) && handlers.Count > 0;
        }

        public static void Tick()
        {
            if (!isConnected)
                return;
            m_Client.Tick();

            if (s_Requests.Count > 0)
            {
                var now = Stopwatch.GetTimestamp();
                var pendingRequests = s_Requests.Values.ToArray();
                foreach (var request in pendingRequests)
                {
                    var elapsedTime = new TimeSpan(now - request.offerStartTime).TotalMilliseconds;
                    if (request.isAcknowledged)
                        continue;
                    if (elapsedTime > request.timeoutInMs)
                    {
                        var eventType = request.eventType;
                        CleanRequest(request.eventType);
                        Reject(request, new TimeoutException($"Request timeout for {eventType} ({elapsedTime} > {request.timeoutInMs})"));
                    }
                }
            }
        }

        private static object[] NotifyLocalListeners(string eventType, object[] data, bool notifyWildcard)
        {
            List<Func<string, object[], object>> handlers = null;
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
                    Console.WriteLine(e);
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
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            return result;
        }

        internal static Dictionary<string, object> CreateRequest(string msgType, string eventType, int targetId, int? requestId, object[] args, EventDataSerialization eventDataSerialization)
        {
            var dataInfos = CreateDataInfosAndFormatDataForSerialization(args, eventDataSerialization);

            return CreateRequest(msgType, eventType, targetId, requestId, args, dataInfos);
        }

        internal static string CreateRequestMsgWithDataString(string msgType, string eventType, int targetId, int? requestId, string args, object[] dataInfos)
        {
            // Create the new request and replace the data field by the original one
            var dataSearchString = Guid.NewGuid().ToString();

            var message = CreateRequestMsg(msgType, eventType, targetId, requestId, dataSearchString, dataInfos);

            message = message.Replace("\"" + dataSearchString + "\"", args);

            return message;
        }

        internal static string CreateRequestMsg(string msgType, string eventType, int targetId, int? requestId, object args, object[] dataInfos)
        {
            var request = CreateRequest(msgType, eventType, targetId, requestId, args, dataInfos);
            return Json.Serialize(request);
        }

        internal static Dictionary<string, object> CreateRequest(string msgType, string eventType, int targetId, int? requestId, object args, object[] dataInfos)
        {
            var req = new Dictionary<string, object>();
            req["req"] = msgType;
            if (targetId != -1)
            {
                req["targetId"] = targetId;
            }
            if (!string.IsNullOrEmpty(eventType))
                req["type"] = eventType;
            req["senderId"] = m_Client.GetChannelClientInfo().connectionId;
            if (requestId.HasValue)
                req["requestId"] = requestId;
            req["data"] = args;
            if (dataInfos != null)
                req["dataInfos"] = dataInfos;

            return req;
        }

        internal static object[] CreateDataInfosAndFormatDataForSerialization(object[] args, EventDataSerialization eventDataSerialization)
        {
            if (eventDataSerialization == EventDataSerialization.StandardJson)
                return null;

            // if the data is null we still send an empty dataInfo to know it uses JsonUtility (useful on a request to keep that info when sending answer)
            if (args == null)
                return new object[] {};


            List<object> dataInfos = new List<object>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null)
                    continue;
                var dataString = JsonUtility.ToJson(args[i]);
                if (!string.IsNullOrEmpty(dataString) && dataString != "{}" && args[i].GetType().IsSerializable)
                {
                    // add index and class type in the info
                    dataInfos.Add(new object[2] { i, args[i].GetType().AssemblyQualifiedName });
                    // format the object
                    args[i] = dataString;
                }
            }
            return dataInfos.ToArray();
        }

        internal static object[] GetDataArray(object dataDeserialized, object dataSerializationInfos)
        {
            if (dataDeserialized != null)
            {
                object[] arrayDeserialized = dataDeserialized is List<object>? (dataDeserialized as List<object>).ToArray() : new[] { dataDeserialized };
                object[] arraySerializationInfos = dataSerializationInfos is List<object>? (dataSerializationInfos as List<object>).ToArray() : null;
                return GetDataArray(arrayDeserialized, arraySerializationInfos);
            }
            return null;
        }

        private static object[] GetDataArray(object[] arrayDeserialized, object[] dataSerializationInfos)
        {
            if (dataSerializationInfos == null || dataSerializationInfos.Length == 0)
                return arrayDeserialized;

            int dataSerializationInfosIndex = 0;
            object[] resultData = new object[arrayDeserialized.Length];
            List<object> currentDataInfos = (List<object>)dataSerializationInfos[dataSerializationInfosIndex];
            for (int i = 0; i < arrayDeserialized.Length; ++i)
            {
                // Use JsonUtility
                if ((long)currentDataInfos[0] == i)
                {
                    var typeName = (string)currentDataInfos[1];
                    resultData[i] = JsonUtility.FromJson((string)arrayDeserialized[i], Type.GetType(typeName));
                    dataSerializationInfosIndex++;
                    if (dataSerializationInfosIndex < dataSerializationInfos.Length)
                        currentDataInfos = (List<object>)dataSerializationInfos[dataSerializationInfosIndex];
                }
                else
                    resultData[i] = arrayDeserialized[i];
            }
            return resultData;
        }
    }
}
