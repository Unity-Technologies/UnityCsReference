// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    class Delayer
    {
        private long m_LastExecutionTime;
        private Action<object> m_Action;
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
                m_LastExecutionTime = DateTime.Now.Ticks;
                Debounce();
            }
        }

        private Delayer(Action<object> action, double delay, bool isThrottle)
        {
            m_Action = action;
            m_DebounceDelay = delay;
            m_IsThrottle = isThrottle;
        }

        public void Dispose()
        {
            EditorApplication.delayCall -= Debounce;
            EditorApplication.delayCall -= Throttle;
            m_Context = null;
            m_Action = null;
        }

        private void Debounce()
        {
            EditorApplication.delayCall -= Debounce;
            var currentTime = DateTime.Now.Ticks;
            if (m_LastExecutionTime != 0 && DelayHasPassed(currentTime))
            {
                m_Action?.Invoke(m_Context);
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
            var currentTime = DateTime.Now.Ticks;
            if (m_LastExecutionTime != 0 && DelayHasPassed(currentTime))
            {
                m_Action?.Invoke(m_Context);
                m_LastExecutionTime = 0;
            }
            else
            {
                if (m_LastExecutionTime == 0)
                    m_LastExecutionTime = currentTime;
                EditorApplication.delayCall += Throttle;
            }
        }

        private bool DelayHasPassed(long currentTime)
        {
            var timeSpan = new TimeSpan(currentTime - m_LastExecutionTime);
            return timeSpan.TotalSeconds > m_DebounceDelay;
        }
    }
}
