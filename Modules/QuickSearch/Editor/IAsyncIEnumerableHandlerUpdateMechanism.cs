// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    interface IAsyncIEnumerableHandlerUpdateMechanism<T>
    {
        void AttachToHandler(BaseAsyncIEnumerableHandler<T> enumerableHandler);
        void DetachFromHandler(BaseAsyncIEnumerableHandler<T> enumerableHandler);
        void Start();
        void Stop();
        void Update();
    }

    class EditorApplicationUpdateMechanism<T> : IAsyncIEnumerableHandlerUpdateMechanism<T>
    {
        private BaseAsyncIEnumerableHandler<T> m_EnumerableHandler;
        List<T> m_PerUpdateItems = new();
        TimeSpan m_MaxFetchTime;

        public EditorApplicationUpdateMechanism(TimeSpan maxFetchTime)
        {
            m_MaxFetchTime = maxFetchTime;
        }

        public void AttachToHandler(BaseAsyncIEnumerableHandler<T> enumerableHandler)
        {
            m_EnumerableHandler = enumerableHandler;
        }

        public void DetachFromHandler(BaseAsyncIEnumerableHandler<T> enumerableHandler)
        {
            if (m_EnumerableHandler == enumerableHandler)
                m_EnumerableHandler = null;
            Stop();
        }

        public void Start()
        {
            // Do not use EditorApplication.update as it is not updated manually during synchronous tests.
            Utils.tick += Update;
        }

        public void Stop()
        {
            // Do not use EditorApplication.update as it is not updated manually during synchronous tests.
            Utils.tick -= Update;
        }

        public void Update()
        {
            m_PerUpdateItems.Clear();
            m_EnumerableHandler?.Update(m_PerUpdateItems, m_MaxFetchTime);
        }
    }

    class ManualUpdateMechanism<T> : IAsyncIEnumerableHandlerUpdateMechanism<T>
    {
        public void AttachToHandler(BaseAsyncIEnumerableHandler<T> enumerableHandler)
        {}

        public void DetachFromHandler(BaseAsyncIEnumerableHandler<T> enumerableHandler)
        {}

        public void Start()
        {}

        public void Stop()
        {}

        public void Update()
        {}
    }
}
