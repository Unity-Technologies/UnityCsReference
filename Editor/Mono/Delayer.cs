// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    internal class Delayer
    {
        private double m_LastExecutionTime;
        private readonly Action<object> m_Action;
        private readonly double m_DebounceDelay;
        private object m_Context;
        private readonly bool m_IsThrottle;

        public static Delayer Throttle(Action<object> action, double delay = 0.2)
        {
            return new Delayer(action, delay, true);
        }

        public static Delayer Debounce(Action<object> action, double delay = 0.2)
        {
            return new Delayer(action, delay, false);
        }

        public void Execute(object context = null)
        {
            m_Context = context;
            if (m_IsThrottle)
            {
                if (m_LastExecutionTime == 0)
                    Throttle();
            }
            else
            {
                m_LastExecutionTime = EditorApplication.timeSinceStartup;
                Debounce();
            }
        }

        private Delayer(Action<object> action, double delay, bool isThrottle)
        {
            m_Action = action;
            m_DebounceDelay = delay;
            m_IsThrottle = isThrottle;
        }

        private void Debounce()
        {
            EditorApplication.delayCall -= Debounce;
            var currentTime = EditorApplication.timeSinceStartup;
            if (m_LastExecutionTime != 0 && currentTime - m_LastExecutionTime > m_DebounceDelay)
            {
                m_Action(m_Context);
                m_LastExecutionTime = 0;
            }
            else
            {
                EditorApplication.delayCall += Debounce;
            }
        }

        private void Throttle()
        {
            EditorApplication.delayCall -= Throttle;
            var currentTime = EditorApplication.timeSinceStartup;
            if (m_LastExecutionTime != 0 && currentTime - m_LastExecutionTime > m_DebounceDelay)
            {
                m_Action(m_Context);
                m_LastExecutionTime = 0;
            }
            else
            {
                if (m_LastExecutionTime == 0)
                    m_LastExecutionTime = currentTime;
                EditorApplication.delayCall += Throttle;
            }
        }
    }
}
