// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

/**
 * JSONServer class implementation
 * Open a websocket channel, listen to remote call requests, execute the request and returns a result
 *
 * -- INVOKE Exchange --
 *
 * Request format:
 * {
 *  version: x.xx,
 *  messageID: xxxx,
 *  type: <INVOKE>,
 *  destination: <ObjectReference>,
 *  method: <methodName>
 *  params: [
 *      <param1>,
 *      <param2>,
 *      ...
 *      <paramN>
 *  ]
 * }
 *
 * Reply format:
 * {
 *  version: x.xx,
 *  messageID: xxxx,
 *  status: <request status>,
 *  result: <resultData>
 * }
 *
 *
 *  -- GETSTUBINFO Exchange --
 *
 * Request format:
 * {
 *  version: x.xx,
 *  messageID: xxxx,
 *  type: <INVOKE>,
 *  reference: <ObjectReference>
 * }
 *
 * Reply format:
 *
 * {
 *  version: x.xx,
 *  messageID: xxxx,
 *  status: <request status>,
 *  reference: <resultData>,
 *  result: {
 *      properties: [
 *          {
 *              name:  <prop. name>,
 *              value: <prop. value>
 *          },
 *          ...
 *      ],
 *
 *      methods: [
 *          {
 *              name: <meth. name>,
 *              parameters: [
 *                  <param1_name>,
 *                  ...
 *              ]
 *          }
 *      ]
 *  }
 * }
 *
 *
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using UnityEditor.Collaboration;
namespace UnityEditor.Web
{
    internal class JspmPropertyInfo
    {
        public string name;
        public object value = null;

        public JspmPropertyInfo(string name, object value)
        {
            this.name = name;
            this.value = value;
        }
    }

    internal class JspmMethodInfo
    {
        public string name;
        public string[] parameters = null;

        public JspmMethodInfo(string name, string[] parameters)
        {
            this.name = name;
            this.parameters = parameters;
        }
    };

    internal class JspmStubInfo
    {
        public JspmPropertyInfo[] properties = null;
        public JspmMethodInfo[] methods = null;
        public string[] events = null;

        public JspmStubInfo(JspmPropertyInfo[] properties, JspmMethodInfo[] methods, string[] events)
        {
            this.methods = methods;
            this.properties = properties;
            this.events = events;
        }
    };

    internal class JspmResult
    {
        public double version;
        public long messageID;
        public int status;

        public JspmResult()
        {
            version = JSProxyMgr.kProtocolVersion;
            messageID = JSProxyMgr.kInvalidMessageID;
            status = JSProxyMgr.kErrNone;
        }

        public JspmResult(long messageID, int status)
        {
            version = JSProxyMgr.kProtocolVersion;
            this.messageID = messageID;
            this.status = status;
        }
    };

    internal class JspmError : JspmResult
    {
        public JspmError(long messageID, int status, string errorClass, string message) : base(messageID, status)
        {
            this.errorClass = errorClass;
            this.message = message;
        }

        public string errorClass;
        public string message;
    };

    internal class JspmSuccess : JspmResult
    {
        public object result;
        public string type;

        public JspmSuccess(long messageID, object result, string type)
            : base(messageID, JSProxyMgr.kErrNone)
        {
            this.result = result;
            this.type = type;
        }
    };

    internal class JspmStubInfoSuccess : JspmSuccess
    {
        public string reference;

        public JspmStubInfoSuccess(long messageID, string reference, JspmPropertyInfo[] properties, JspmMethodInfo[] methods, string[] events)
            : base(messageID, new JspmStubInfo(properties, methods, events), JSProxyMgr.kTypeGetStubInfo)
        {
            this.reference = reference;
        }
    };

    [InitializeOnLoad]
    internal class JSProxyMgr
    {
        // current protocol version
        public const double kProtocolVersion = 1.0;

        // class constants
        public const long kInvalidMessageID = -1;

        // error code definitions
        public const int kErrNone = 0;
        public const int kErrInvalidMessageFormat = -1000;
        public const int kErrUnknownObject = -1001;
        public const int kErrUnknownMethod = -1002;
        public const int kErrInvocationFailed = -1003;
        public const int kErrUnsupportedProtocol = -1004;
        public const int kErrUnknownEvent = -1005;

        // messages types
        public const string kTypeInvoke = "INVOKE";
        public const string kTypeGetStubInfo = "GETSTUBINFO";
        public const string kTypeOnEvent = "ONEVENT";

        // task list
        private Queue<TaskCallback> m_TaskList = null;

        // global object list
        private Dictionary<string, object> m_GlobalObjects = null;

        // singleton
        private static JSProxyMgr s_Instance = null;

        // ignored methods
        private static readonly string[] s_IgnoredMethods = { "Equals", "GetHashCode", "GetType", "ToString"};

        public static JSProxyMgr GetInstance()
        {
            if (s_Instance == null)
            {
                s_Instance = new JSProxyMgr();
            }

            return s_Instance;
        }

        static JSProxyMgr()
        {
            WebView.OnDomainReload();
        }

        public static void DoTasks()
        {
            GetInstance().ProcessTasks();
        }

        ~JSProxyMgr()
        {
            m_GlobalObjects.Clear();
            m_GlobalObjects = null;
        }

        protected JSProxyMgr()
        {
            m_TaskList = new Queue<TaskCallback>();
            m_GlobalObjects = new Dictionary<string, object>();
            AddGlobalObject("unity/collab",  Collab.instance);
        }

        protected delegate void TaskCallback();

        public void AddGlobalObject(string referenceName, object obj)
        {
            if (m_GlobalObjects == null)
            {
                m_GlobalObjects = new Dictionary<string, object>();
            }

            RemoveGlobalObject(referenceName);
            m_GlobalObjects.Add(referenceName, obj);
        }

        public void RemoveGlobalObject(string referenceName)
        {
            if (m_GlobalObjects == null)
                return;

            if (m_GlobalObjects.ContainsKey(referenceName))
            {
                m_GlobalObjects.Remove(referenceName);
            }
        }

        private void AddTask(TaskCallback task)
        {
            if (m_TaskList == null)
            {
                m_TaskList = new Queue<TaskCallback>();
            }

            m_TaskList.Enqueue(task);
        }

        private void ProcessTasks()
        {
            if (m_TaskList == null || m_TaskList.Count == 0)
                return;

            // Deque and call maximum of 10 tasks
            var maxTasks = 10;
            while (m_TaskList.Count > 0 && maxTasks > 0)
            {
                TaskCallback callback = m_TaskList.Dequeue();
                callback();
                maxTasks--;
            }
        }

        public delegate void ExecCallback(object result);

        public bool DoMessage(string jsonRequest, ExecCallback callback, WebView webView)
        {
            long messageID = kInvalidMessageID;

            try
            {
                var jsonData = Json.Deserialize(jsonRequest) as Dictionary<string, object>;
                if (jsonData == null || !jsonData.ContainsKey("messageID") || !jsonData.ContainsKey("version") || !jsonData.ContainsKey("type"))
                {
                    callback(FormatError(messageID, kErrInvalidMessageFormat, "errInvalidMessageFormat", jsonRequest));
                    return false;
                }

                messageID = (long)jsonData["messageID"];
                double versionNumber = double.Parse((string)jsonData["version"], System.Globalization.CultureInfo.InvariantCulture);
                string type = (string)jsonData["type"];

                if (versionNumber > kProtocolVersion)
                {
                    callback(FormatError(messageID, kErrUnsupportedProtocol, "errUnsupportedProtocol", "The protocol version <" + versionNumber + "> is not supported by this verison of the code"));
                    return false;
                }

                if (type == kTypeInvoke)
                {
                    return DoInvokeMessage(messageID, callback, jsonData);
                }

                if (type == kTypeGetStubInfo)
                {
                    return DoGetStubInfoMessage(messageID, callback, jsonData);
                }

                if (type == kTypeOnEvent)
                {
                    return DoOnEventMessage(messageID, callback, jsonData, webView);
                }
            }
            catch (Exception ex)
            {
                callback(FormatError(messageID, kErrInvalidMessageFormat, "errInvalidMessageFormat", ex.Message));
            }

            return false;
        }

        private bool DoGetStubInfoMessage(long messageID, ExecCallback callback, Dictionary<string, object> jsonData)
        {
            if (!jsonData.ContainsKey("reference"))
            {
                callback(FormatError(messageID, kErrUnknownObject, "errUnknownObject", "object reference missing"));
                return false;
            }

            string reference = (string)jsonData["reference"];
            object destObject = GetDestinationObject(reference);

            if (destObject == null)
            {
                callback(FormatError(messageID, kErrUnknownObject, "errUnknownObject", "cannot find object with reference <" + reference + ">"));
                return false;
            }

            //Add the public functions and static methods
            List<MethodInfo> methods = (destObject.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)).ToList();
            methods.AddRange((destObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Static)).ToList());

            ArrayList methodList = new ArrayList();
            foreach (MethodInfo method in methods)
            {
                if (Array.IndexOf(s_IgnoredMethods, method.Name) >= 0)
                    continue;

                if (method.IsSpecialName && (method.Name.StartsWith("set_") || method.Name.StartsWith("get_")))
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                ArrayList parameterList = new ArrayList();
                foreach (ParameterInfo parameter in parameters)
                {
                    parameterList.Add(parameter.Name);
                }

                JspmMethodInfo info = new JspmMethodInfo(method.Name, (string[])parameterList.ToArray(typeof(string)));
                methodList.Add(info);
            }

            List<PropertyInfo> properties = destObject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).ToList();
            ArrayList propertyList = new ArrayList();
            foreach (PropertyInfo property in properties)
            {
                propertyList.Add(new JspmPropertyInfo(property.Name, property.GetValue(destObject, null)));
            }

            List<FieldInfo> fields = destObject.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public).ToList();
            foreach (FieldInfo field in fields)
            {
                propertyList.Add(new JspmPropertyInfo(field.Name, field.GetValue(destObject)));
            }

            List<EventInfo> events = destObject.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public).ToList();
            ArrayList eventList = new ArrayList();
            foreach (EventInfo evt in events)
            {
                eventList.Add(evt.Name);
            }

            callback(new JspmStubInfoSuccess(messageID, reference,
                    (JspmPropertyInfo[])propertyList.ToArray(typeof(JspmPropertyInfo)),
                    (JspmMethodInfo[])methodList.ToArray(typeof(JspmMethodInfo)),
                    (string[])eventList.ToArray(typeof(string))));

            return true;
        }

        private bool DoOnEventMessage(long messageID, ExecCallback callback, Dictionary<string, object> jsonData, WebView webView)
        {
            callback(FormatError(messageID, kErrUnknownMethod, "errUnknownMethod", "method DoOnEventMessage is deprecated"));
            return false;
        }

        private bool DoInvokeMessage(long messageID, ExecCallback callback, Dictionary<string, object> jsonData)
        {
            if (!jsonData.ContainsKey("destination") || !jsonData.ContainsKey("method") || !jsonData.ContainsKey("params"))
            {
                callback(FormatError(messageID, kErrUnknownObject, "errUnknownObject", "object reference, method name or parameters missing"));
                return false;
            }

            string destination = (string)jsonData["destination"];
            string methodName = (string)jsonData["method"];
            List<object> paramList = (List<object>)jsonData["params"];

            object destObject = GetDestinationObject(destination);
            if (destObject == null)
            {
                callback(FormatError(messageID, kErrUnknownObject, "errUnknownObject", "cannot find object with reference <" + destination + ">"));
                return false;
            }

            Type type = destObject.GetType();
            MethodInfo[] methods = type.GetMethods();
            MethodInfo foundMethod = null;
            object[] parameters = null;
            string err = "";

            foreach (MethodInfo method in methods)
            {
                if (method.Name != methodName)
                    continue;

                try
                {
                    parameters = ParseParams(method, paramList);
                    foundMethod = method;
                    break;
                }
                catch (Exception e)
                {
                    err = e.Message;
                }
            }

            if (foundMethod == null)
            {
                callback(FormatError(messageID, kErrUnknownMethod, "errUnknownMethod", "cannot find method <" + methodName + "> for object <" + destination + ">, reason:" + err));
                return false;
            }

            AddTask(() =>
                {
                    try
                    {
                        object res = foundMethod.Invoke(destObject, parameters);
                        callback(FormatSuccess(messageID, res));
                    }
                    catch (TargetInvocationException tiex)
                    {
                        if (tiex.InnerException != null)
                        {
                            callback(FormatError(messageID, kErrInvocationFailed, tiex.InnerException.GetType().Name, tiex.InnerException.Message));
                        }
                        else
                        {
                            callback(FormatError(messageID, kErrInvocationFailed, tiex.GetType().Name, tiex.Message));
                        }
                    }
                    catch (Exception ex)
                    {
                        callback(FormatError(messageID, kErrInvocationFailed, ex.GetType().Name, ex.Message));
                    }
                });

            return true;
        }

        public static JspmError FormatError(long messageID, int status, string errorClass, string message)
        {
            return new JspmError(messageID, status, errorClass, message);
        }

        public static JspmSuccess FormatSuccess(long messageID, object result)
        {
            return new JspmSuccess(messageID, result, kTypeInvoke);
        }

        public object GetDestinationObject(string reference)
        {
            object destination;
            m_GlobalObjects.TryGetValue(reference, out destination);
            return destination;
        }

        public object[] ParseParams(MethodInfo method, List<object> data)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != data.Count)
                return null;

            List<object> result = new List<object>(data.Count);
            for (int i = 0; i < data.Count; i++)
            {
                object val = InternalParseParam(parameters[i].ParameterType, data[i]);
                result.Add(val);
            }
            return result.ToArray();
        }

        private object InternalParseParam(Type type, object data)
        {
            IList asList;
            IDictionary asDict;
            string asStr;

            if (data == null)
            {
                return null;
            }

            if ((asList = data as IList) != null)
            {
                if (!type.IsArray)
                    throw new InvalidOperationException("Not an array " + type.FullName);

                Type elemType = type.GetElementType();
                ArrayList res = new ArrayList();
                for (int i = 0; i < asList.Count; i++)
                {
                    object val = InternalParseParam(elemType, asList[i]);
                    res.Add(val);
                }
                return res.ToArray(elemType);
            }

            if ((asDict = data as IDictionary) != null)
            {
                if (!type.IsClass)
                    throw new InvalidOperationException("Not a class " + type.FullName);

                ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.Any, new Type[0], null);
                if (constructor == null)
                    throw new InvalidOperationException("Cannot find a default constructor for " + type.FullName);

                object obj = constructor.Invoke(new object[0]);
                List<FieldInfo> fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public).ToList();
                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        object val = InternalParseParam(field.FieldType, asDict[field.Name]);
                        field.SetValue(obj, val);
                    }
                    catch (KeyNotFoundException)
                    {
                        // For now, do nothing
                    }
                }
                List<PropertyInfo> properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).ToList();
                foreach (PropertyInfo property in properties)
                {
                    try
                    {
                        object val = InternalParseParam(property.PropertyType, asDict[property.Name]);
                        MethodInfo method = property.GetSetMethod();
                        if (method != null)
                        {
                            method.Invoke(obj, new[] { val });
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        // For now, do nothing
                    }
                    catch (TargetInvocationException)
                    {
                        // For now, do nothing
                    }
                }

                return Convert.ChangeType(obj, type);
            }

            if ((asStr = data as string) != null)
            {
                return asStr;
            }

            if (data is bool)
            {
                return (bool)data;
            }

            if (data is double)
            {
                return (double)data;
            }

            if (data is int || data is Int16 || data is Int32 || data is Int64 || data is long)
            {
                return Convert.ChangeType(data, type);
            }

            throw new InvalidOperationException("Cannot parse " + Json.Serialize(data));
        }

        public string Stringify(object target)
        {
            string res = Json.Serialize(target);
            return res;
        }

        protected object GetMemberValue(MemberInfo member, object target)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).GetValue(target);

                case MemberTypes.Property:
                    try
                    {
                        return ((PropertyInfo)member).GetValue(target, null);
                    }
                    catch (TargetParameterCountException e)
                    {
                        throw new ArgumentException("MemberInfo has index parameters", "member", e);
                    }

                default:
                    throw new ArgumentException("MemberInfo is not of type FieldInfo or PropertyInfo", "member");
            }
        }
    }
}
