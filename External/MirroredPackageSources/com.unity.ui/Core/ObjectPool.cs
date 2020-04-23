using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    class ObjectPool<T> where T : new()
    {
        private readonly Stack<T> m_Stack = new Stack<T>();
        private int m_MaxSize;

        public int maxSize
        {
            get { return m_MaxSize; }
            set
            {
                m_MaxSize = Math.Max(0, value);
                while (Size() > m_MaxSize)
                {
                    Get();
                }
            }
        }

        public ObjectPool(int maxSize = 100)
        {
            this.maxSize = maxSize;
        }

        public int Size()
        {
            return m_Stack.Count;
        }

        public void Clear()
        {
            m_Stack.Clear();
        }

        public T Get()
        {
            T evt = m_Stack.Count == 0 ? new T() : m_Stack.Pop();
            return evt;
        }

        public void Release(T element)
        {
            if (m_Stack.Count > 0 && ReferenceEquals(m_Stack.Peek(), element))
                Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");

            if (m_Stack.Count < maxSize)
            {
                m_Stack.Push(element);
            }
        }
    }
}
