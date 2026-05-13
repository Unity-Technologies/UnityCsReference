// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UnityEngine.UIElements.Experimental
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
            public bool removable;
        }

        // Global registry
        internal static readonly Dictionary<CallbackEventHandler, Dictionary<Type, List<ListenerRecord>>> s_Listeners =
            new Dictionary<CallbackEventHandler, Dictionary<Type, List<ListenerRecord>>>();

        public static void CleanListeners(IPanel panel)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var listeners = s_Listeners.ToList();
#pragma warning restore UA2001
            foreach (var eventRegistrationListener in listeners)
            {
                var key = eventRegistrationListener.Key as VisualElement; // VE that sends events
                if (key?.panel == null)
                    s_Listeners.Remove(eventRegistrationListener.Key);
            }
        }

        public static void UnregisterAllListeners(CallbackEventHandler ceh)
        {
            if (!IsEventDebuggerConnected)
                return;
            s_Listeners.Remove(ceh);
        }

        public static void RegisterListeners(Type eventType, CallbackEventHandler ceh, Delegate callback, CallbackOptionsInternal callbackOptions)
        {
            if (!IsEventDebuggerConnected || eventType == typeof(GeometryChangedEvent))
                return;
            Dictionary<Type, List<ListenerRecord>> dict;
            if (!s_Listeners.TryGetValue(ceh, out dict))
            {
                dict = new Dictionary<Type, List<ListenerRecord>>();
                s_Listeners.Add(ceh, dict);
            }

            var declType = callback.Method.DeclaringType?.Name ?? string.Empty;
            string objectName = callback.Target is VisualElement ve ? " [" + ve.GetDisplayName() + "]" : "";
            var useTrickleDown = (callbackOptions & CallbackOptionsInternal.TrickleDown) != 0 ? TrickleDown.TrickleDown : TrickleDown.NoTrickleDown;
            string itemName = declType + "." + callback.Method.Name + " > " + useTrickleDown + objectName;

            List<ListenerRecord> callbackRecords;
            if (!dict.TryGetValue(eventType, out callbackRecords))
            {
                callbackRecords = new List<ListenerRecord>();
                dict.Add(eventType, callbackRecords);
            }

            StackFrame callStack = new StackFrame(2, true);
            callbackRecords.Add(new ListenerRecord
            {
                hashCode = callback.GetHashCode(),
                name = itemName,
                fileName = callStack.GetFileName(),
                lineNumber = callStack.GetFileLineNumber(),
                removable = (callbackOptions & CallbackOptionsInternal.Removable) != 0
            });
        }

        public static void UnregisterListeners(Type eventType, CallbackEventHandler ceh, Delegate callback)
        {
            if (!IsEventDebuggerConnected)
                return;
            if (!s_Listeners.TryGetValue(ceh, out var dict))
                return;
            if (!dict.TryGetValue(eventType, out var callbackRecords))
                return;

            var declType = callback.Method.DeclaringType?.Name ?? string.Empty;

            // On registration, more information is included in the itemName for ease of debugging.
            // In the very rare cases where the following itemNamePrefix doesn't uniquely identify the method,
            // users can usually rename their methods to make them more specific and be able to correctly track them.
            var itemNamePrefix = declType + "." + callback.Method.Name + " > ";

            for (var i = callbackRecords.Count - 1; i >= 0; i--)
            {
                var callbackRecord = callbackRecords[i];
                if (callbackRecord.name.StartsWith(itemNamePrefix, StringComparison.Ordinal))
                {
                    callbackRecords.RemoveAt(i);
                }
            }

            if (callbackRecords.Count == 0)
            {
                dict.Remove(eventType);
                if (dict.Count == 0)
                    s_Listeners.Remove(ceh);
            }
        }

        private static readonly List<Type> k_TypesToRemove = new();
        public static void UnregisterAllRemovableListeners(CallbackEventHandler ceh)
        {
            if (!IsEventDebuggerConnected)
                return;
            if (!s_Listeners.TryGetValue(ceh, out var dict))
                return;

            foreach (var kv in dict)
            {
                var callbackRecords = kv.Value;
                for (var i = callbackRecords.Count - 1; i >= 0; i--)
                {
                    var callbackRecord = callbackRecords[i];
                    if (callbackRecord.removable)
                    {
                        callbackRecords.RemoveAt(i);
                    }
                }
                if (callbackRecords.Count == 0)
                    k_TypesToRemove.Add(kv.Key);
            }

            if (k_TypesToRemove.Count > 0)
            {
                foreach (var type in k_TypesToRemove)
                    dict.Remove(type);
                k_TypesToRemove.Clear();
                if (dict.Count == 0)
                    s_Listeners.Remove(ceh);
            }
        }
    }
}

