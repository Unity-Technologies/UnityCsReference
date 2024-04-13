// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    public sealed class WaitWhile : CustomYieldInstruction
    {
        readonly Func<bool> m_Predicate;
        readonly Action m_TimeoutCallback;
        readonly WaitTimeoutMode m_TimeoutMode;
        readonly double m_MaxExecutionTime = -1;

        public override bool keepWaiting
        {
            get
            {
                if (m_MaxExecutionTime == -1)
                    return m_Predicate();

                if (GetTime() > m_MaxExecutionTime)
                {
                    m_TimeoutCallback();
                    return false;
                }

                return m_Predicate();
            }
        }

        public WaitWhile(Func<bool> predicate) { m_Predicate = predicate; }

        public WaitWhile(Func<bool> predicate, TimeSpan timeout, Action onTimeout, WaitTimeoutMode timeoutMode = WaitTimeoutMode.Realtime) : this(predicate)
        {
            if (timeoutMode is WaitTimeoutMode.InGameTime && !Application.isPlaying)
                throw new ArgumentException($"{nameof(WaitTimeoutMode.InGameTime)} mode is not supported in Editor in edit mode", nameof(timeoutMode));
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentException("Timeout must be greater than zero", nameof(timeout));

            m_TimeoutCallback = onTimeout ?? throw new ArgumentNullException(nameof(onTimeout), "Timeout callback must be specified");
            m_TimeoutMode = timeoutMode;
            m_MaxExecutionTime = GetTime() + timeout.TotalSeconds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        double GetTime() => m_TimeoutMode is WaitTimeoutMode.InGameTime ? Time.timeAsDouble : Time.realtimeSinceStartupAsDouble;
    }
}
