using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnityEngine.UIElements
{
    internal static class GlobalCallbackRegistry
    {
        private static bool m_IsEventDebuggerConnected = false;
        public static bool IsEventDebuggerConnected
        {
            get { return m_IsEventDebuggerConnected; }
            set
            {
                if (!value)
                    s_Listeners.Clear();

                m_IsEventDebuggerConnected = value;
            }
        }

        internal struct ListenerRecord
        {
            public int hashCode;
            public string name;
            public string fileName;
            public int lineNumber;
        }

        // Global registry
        internal static readonly Dictionary<CallbackEventHandler, Dictionary<Type, List<ListenerRecord>>> s_Listeners =
            new Dictionary<CallbackEventHandler, Dictionary<Type, List<ListenerRecord>>>();

        public static void RegisterListeners<TEventType>(CallbackEventHandler ceh, Delegate callback, TrickleDown useTrickleDown)
        {
            if (!IsEventDebuggerConnected)
                return;
            Dictionary<Type, List<ListenerRecord>> dict;
            if (!s_Listeners.TryGetValue(ceh, out dict))
            {
                dict = new Dictionary<Type, List<ListenerRecord>>();
                s_Listeners.Add(ceh, dict);
            }

            var declType = callback.Method.DeclaringType?.Name ?? string.Empty;
            string objectName = (callback.Target as VisualElement).GetDisplayName();
            string itemName = declType + "." + callback.Method.Name + " > " + useTrickleDown + " [" + objectName + "]";

            List<ListenerRecord> callbackRecords;
            if (!dict.TryGetValue(typeof(TEventType), out callbackRecords))
            {
                callbackRecords = new List<ListenerRecord>();
                dict.Add(typeof(TEventType), callbackRecords);
            }

            StackFrame callStack = new StackFrame(2, true);
            callbackRecords.Add(new ListenerRecord
            {
                hashCode = callback.GetHashCode(),
                name = itemName,
                fileName = callStack.GetFileName(),
                lineNumber = callStack.GetFileLineNumber()
            });
        }

        public static void UnregisterListeners<TEventType>(CallbackEventHandler ceh, Delegate callback)
        {
            if (!IsEventDebuggerConnected)
                return;
            Dictionary<Type, List<ListenerRecord>> dict;
            if (!s_Listeners.TryGetValue(ceh, out dict))
                return;

            var declType = callback.Method.DeclaringType?.Name ?? string.Empty;
            var itemName = declType + "." + callback.Method.Name;

            List<ListenerRecord> callbackRecords;
            if (!dict.TryGetValue(typeof(TEventType), out callbackRecords))
                return;

            for (var i = callbackRecords.Count - 1; i >= 0; i--)
            {
                var callbackRecord = callbackRecords[i];
                if (callbackRecord.name == itemName)
                {
                    callbackRecords.RemoveAt(i);
                }
            }
        }
    }
}
