// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public enum Capture
    {
        NoCapture = 0,
        Capture = 1
    }

    internal class ListPool<T>
    {
        readonly Stack<List<T>> m_Stack = new Stack<List<T>>();

        public List<T> Get(List<T> initializer)
        {
            List<T> element;
            if (m_Stack.Count == 0)
            {
                if (initializer != null)
                    element = new List<T>(initializer);
                else
                    element = new List<T>();
            }
            else
            {
                element = m_Stack.Pop();
                if (initializer != null)
                    element.AddRange(initializer);
            }
            return element;
        }

        public void Release(List<T> element)
        {
            element.Clear();
            m_Stack.Push(element);
        }
    }

    internal class EventCallbackRegistry
    {
        private static readonly ListPool<EventCallbackFunctorBase> s_ListPool = new ListPool<EventCallbackFunctorBase>();

        private static List<EventCallbackFunctorBase> GetCallbackList(List<EventCallbackFunctorBase> initializer = null)
        {
            return s_ListPool.Get(initializer);
        }

        private static void ReleaseCallbackList(List<EventCallbackFunctorBase> toRelease)
        {
            s_ListPool.Release(toRelease);
        }

        private List<EventCallbackFunctorBase> m_Callbacks;
        private List<EventCallbackFunctorBase> m_TemporaryCallbacks;
        int m_IsInvoking;

        public EventCallbackRegistry()
        {
            m_IsInvoking = 0;
        }

        List<EventCallbackFunctorBase> GetCallbackListForWriting()
        {
            if (m_IsInvoking > 0)
            {
                if (m_TemporaryCallbacks == null)
                {
                    if (m_Callbacks != null)
                    {
                        m_TemporaryCallbacks = GetCallbackList(m_Callbacks);
                    }
                    else
                    {
                        m_TemporaryCallbacks = GetCallbackList();
                    }
                }

                return m_TemporaryCallbacks;
            }
            else
            {
                if (m_Callbacks == null)
                {
                    m_Callbacks = GetCallbackList();
                }

                return m_Callbacks;
            }
        }

        List<EventCallbackFunctorBase> GetCallbackListForReading()
        {
            if (m_TemporaryCallbacks != null)
            {
                return m_TemporaryCallbacks;
            }

            return m_Callbacks;
        }

        bool ShouldRegisterCallback(long eventTypeId, Delegate callback, Capture useCapture)
        {
            if (callback == null)
            {
                return false;
            }

            List<EventCallbackFunctorBase> callbackList = GetCallbackListForReading();
            if (callbackList != null)
            {
                EventCallbackFunctorBase found = callbackList.Find(ftor => ftor.IsEquivalentTo(eventTypeId, callback, useCapture));
                if (found != null)
                {
                    return false;
                }
            }

            return true;
        }

        bool UnregisterCallback(long eventTypeId, Delegate callback, Capture useCapture)
        {
            if (callback == null)
            {
                return false;
            }

            List<EventCallbackFunctorBase> callbackList = GetCallbackListForWriting();
            return callbackList.RemoveAll(ftor => ftor.IsEquivalentTo(eventTypeId, callback, useCapture)) > 0;
        }

        public void RegisterCallback<TEventType>(EventCallback<TEventType> callback, Capture useCapture = Capture.NoCapture) where TEventType : EventBase, new()
        {
            long eventTypeId = new TEventType().GetEventTypeId();
            if (ShouldRegisterCallback(eventTypeId, callback, useCapture))
            {
                List<EventCallbackFunctorBase> callbackList = GetCallbackListForWriting();
                callbackList.Add(new EventCallbackFunctor<TEventType>(callback, useCapture));
            }
        }

        public void RegisterCallback<TEventType, TCallbackArgs>(EventCallback<TEventType, TCallbackArgs> callback, TCallbackArgs userArgs, Capture useCapture = Capture.NoCapture) where TEventType : EventBase, new()
        {
            long eventTypeId = new TEventType().GetEventTypeId();
            if (ShouldRegisterCallback(eventTypeId, callback, useCapture))
            {
                List<EventCallbackFunctorBase> callbackList = GetCallbackListForWriting();
                callbackList.Add(new EventCallbackFunctor<TEventType, TCallbackArgs>(callback, userArgs, useCapture));
            }
        }

        public bool UnregisterCallback<TEventType>(EventCallback<TEventType> callback, Capture useCapture = Capture.NoCapture) where TEventType : EventBase, new()
        {
            long eventTypeId = new TEventType().GetEventTypeId();
            return UnregisterCallback(eventTypeId, callback, useCapture);
        }

        public bool UnregisterCallback<TEventType, TCallbackArgs>(EventCallback<TEventType, TCallbackArgs> callback, Capture useCapture = Capture.NoCapture) where TEventType : EventBase, new()
        {
            long eventTypeId = new TEventType().GetEventTypeId();
            return UnregisterCallback(eventTypeId, callback, useCapture);
        }

        public void InvokeCallbacks(EventBase evt)
        {
            if (m_Callbacks == null)
            {
                return;
            }

            m_IsInvoking++;

            for (var i = 0; i < m_Callbacks.Count; i++)
            {
                if (evt.isImmediatePropagationStopped)
                    break;

                m_Callbacks[i].Invoke(evt);
            }

            m_IsInvoking--;

            // FIXME: this does not completely support the case where callbacks send events
            // and add/remove callbacks in the registry. For now, all recursively sent events
            // will invoke the initial callback list.
            if (m_IsInvoking == 0)
            {
                // If callbacks were modified during callback invocation, update them now.
                if (m_TemporaryCallbacks != null)
                {
                    ReleaseCallbackList(m_Callbacks);
                    m_Callbacks = GetCallbackList(m_TemporaryCallbacks);
                    ReleaseCallbackList(m_TemporaryCallbacks);
                    m_TemporaryCallbacks = null;
                }
            }
        }
    }
}
