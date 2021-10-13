// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    class ObjectPool<T> where T : new()
    {
        private readonly Stack<T> m_Stack = new Stack<T>();
        private int m_MaxSize;
        internal Func<T> CreateFunc;

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

        public ObjectPool(Func<T> CreateFunc, int maxSize = 100)
        {
            this.maxSize = maxSize;

            if (CreateFunc == null)
            {
                this.CreateFunc = () => new T();
            }
            else
            {
                this.CreateFunc = CreateFunc;
            }
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
            T evt = m_Stack.Count == 0 ? CreateFunc() : m_Stack.Pop();
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
