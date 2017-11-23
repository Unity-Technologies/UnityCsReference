// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    class EventPool<T> where T : EventBase<T>, new()
    {
        private readonly Stack<T> m_Stack = new Stack<T>();

        public T Get()
        {
            T evt = m_Stack.Count == 0 ? new T() : m_Stack.Pop();
            return evt;
        }

        public void Release(T element)
        {
            if (m_Stack.Contains(element))
                Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
            m_Stack.Push(element);
        }
    }
}
