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
        private readonly long m_DebounceDelay;
        private object m_Context;
        private readonly bool m_IsThrottle;
        private readonly bool m_FirstExecuteImmediate;
        private bool m_DelayInProgress;

        internal static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Throttle the action to be executed at most once every delay.
        /// If no delay is specified default delay is <see cref="DefaultDelay"/>
        /// </summary>
        /// <param name="action">action to be executed</param>
        /// <param name="delay">delay of the action, if not specified <see cref="DefaultDelay"/> will be used</param>
        /// <returns>a new instance of a <see cref="Delayer"/></returns>
        public static Delayer Throttle(Action<object> action, TimeSpan? delay = null)
        {
            return new Delayer(action, delay ?? DefaultDelay, true, false);
        }

        /// <summary>
        /// Throttle the action to be executed at most once every delay.
        /// </summary>
        /// <param name="action">action to be executed</param>
        /// <param name="delay">delay of the action</param>
        /// <param name="firstExecuteImmediate">if true, the action will be executed immediately</param>
        /// <returns>a new instance of a <see cref="Delayer"/></returns>
        public static Delayer Throttle(Action<object> action, TimeSpan delay, bool firstExecuteImmediate)
        {
            return new Delayer(action, delay, true, firstExecuteImmediate);
        }

        /// <summary>
        /// Debounce the action to be executed after the delay has passed.
        /// If no delay is specified default delay is <see cref="DefaultDelay"/>
        /// </summary>
        /// <param name="action">action to be executed</param>
        /// <param name="delay">delay of the action, if not specified <see cref="DefaultDelay"/> will be used</param>
        /// <returns>a new instance of a <see cref="Delayer"/></returns>
        public static Delayer Debounce(Action<object> action, TimeSpan? delay = null)
        {
            return new Delayer(action, delay ?? DefaultDelay, false, false);
        }

        /// <summary>
        /// Debounce the action to be executed after the delay has passed.
        /// </summary>
        /// <param name="action">action to be executed</param>
        /// <param name="delay">delay for the action</param>
        /// <param name="firstExecuteImmediate">if true, the action will be executed immediately</param>
        /// <returns>a new instance of a <see cref="Delayer"/></returns>
        public static Delayer Debounce(Action<object> action, TimeSpan delay, bool firstExecuteImmediate)
        {
            return new Delayer(action, delay, false, firstExecuteImmediate);
        }

        /// <summary>
        /// Try to execute the action
        /// </summary>
        /// <param name="context">Context object to pass to the configured action</param>
        public void Execute(object context = null)
        {
            m_Context = context;

            if (m_IsThrottle)
            {
                if (m_LastExecutionTime == 0 || !m_DelayInProgress)
                    Throttle();
            }
            else
            {
                if (m_FirstExecuteImmediate && m_LastExecutionTime == 0)
                {
                    m_Action?.Invoke(m_Context);
                    m_LastExecutionTime = DateTime.UtcNow.Ticks;
                }
                else
                {
                    m_LastExecutionTime = DateTime.UtcNow.Ticks;
                    Debounce();
                }
            }
        }

        private Delayer(Action<object> action, TimeSpan delay, bool isThrottle, bool firstExecuteImmediate)
        {
            m_Action = action;
            m_DebounceDelay = delay.Ticks;
            m_IsThrottle = isThrottle;
            m_FirstExecuteImmediate = firstExecuteImmediate;
        }

        public void Abort()
        {
            EditorApplication.tick -= Debounce;
            EditorApplication.tick -= Throttle;
        }

        public void Dispose()
        {
            Abort();

            m_Context = null;
            m_Action = null;
            m_DelayInProgress = false;
        }

        private void Debounce()
        {
            var currentTime = DateTime.UtcNow.Ticks;
            if (m_LastExecutionTime != 0 && DelayHasPassed(currentTime))
            {
                m_DelayInProgress = false;
                EditorApplication.tick -= Debounce;
                m_Action?.Invoke(m_Context);
                m_LastExecutionTime = 0;
            }
            else
            {
                if (!m_DelayInProgress)
                    EditorApplication.tick += Debounce;
                m_DelayInProgress = true;
            }
        }

        private void Throttle()
        {
            var currentTime = DateTime.UtcNow.Ticks;

            if (m_FirstExecuteImmediate)
            {
                if (m_LastExecutionTime == 0 || DelayHasPassed(currentTime))
                {
                    m_DelayInProgress = false;
                    EditorApplication.tick -= Throttle;
                    m_Action?.Invoke(m_Context);
                    m_LastExecutionTime = currentTime;
                }
                else
                {
                    if (!m_DelayInProgress)
                        EditorApplication.tick += Throttle;
                    m_DelayInProgress = true;
                }
            }
            else
            {
                if (m_LastExecutionTime != 0 && DelayHasPassed(currentTime))
                {
                    m_DelayInProgress = false;
                    EditorApplication.tick -= Throttle;
                    m_Action?.Invoke(m_Context);
                    m_LastExecutionTime = 0;
                }
                else
                {
                    if (m_LastExecutionTime == 0)
                        m_LastExecutionTime = currentTime;
                    if (!m_DelayInProgress)
                        EditorApplication.tick += Throttle;
                    m_DelayInProgress = true;
                }
            }
        }

        private bool DelayHasPassed(long currentTime)
        {
            var timeSpan = new TimeSpan(currentTime - m_LastExecutionTime);
            return timeSpan.Ticks >= m_DebounceDelay;
        }
    }
}
