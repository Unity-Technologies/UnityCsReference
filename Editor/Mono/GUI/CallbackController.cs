// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    class CallbackController
    {
        readonly Action m_Callback;
        readonly float m_CallbacksPerSecond;
        double m_NextCallback;

        public CallbackController(Action callback, float callbacksPerSecond)
        {
            m_Callback = callback;
            m_CallbacksPerSecond = Mathf.Max(callbacksPerSecond, 1f);
        }

        public bool active { get; private set; }

        public void Start()
        {
            m_NextCallback = 0;
            EditorApplication.update += Update;
            active = true;
        }

        public void Stop()
        {
            EditorApplication.update -= Update;
            active = false;
        }

        void Update()
        {
            double time = EditorApplication.timeSinceStartup;
            if (time > m_NextCallback)
            {
                m_NextCallback = time + (1f / m_CallbacksPerSecond);
                if (m_Callback != null)
                    m_Callback();
            }
        }
    }
}
