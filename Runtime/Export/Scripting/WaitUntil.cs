// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public sealed class WaitUntil : CustomYieldInstruction
    {
        readonly Func<bool> m_Predicate;
        readonly Action m_TimeoutCallback;
        readonly double m_MaxExecutionTime = -1;

        public override bool keepWaiting
        {
            get
            {
                if (m_MaxExecutionTime == -1)
                    return !m_Predicate();

                if (Time.timeAsDouble > m_MaxExecutionTime)
                {
                    m_TimeoutCallback();
                    return false;
                }

                return !m_Predicate();
            }
        }

        public WaitUntil(Func<bool> predicate) { m_Predicate = predicate; }

        public WaitUntil(Func<bool> predicate, TimeSpan timeout, Action onTimeout) : this(predicate)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentException("Timeout must be greater than zero", nameof(timeout));
            m_TimeoutCallback = onTimeout ?? throw new ArgumentNullException(nameof(onTimeout), "Timeout callback must be specified");
            m_MaxExecutionTime = Time.timeAsDouble + timeout.TotalSeconds;
        }
    }
}
