// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;

namespace UnityEditor.Search
{
    class RetriableOperation<T> where T : Exception
    {
        public const uint DefaultRetryCount = 5;
        public static readonly TimeSpan DefaultSleepTimeMs = TimeSpan.FromMilliseconds(5);

        Action m_Operation;
        uint m_RetryCount;
        TimeSpan m_SleepTime;

        public RetriableOperation(Action operation, uint retryCount, TimeSpan sleepTime)
        {
            m_Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            m_RetryCount = retryCount;
            m_SleepTime = sleepTime;
        }

        public void Execute()
        {
            var retry = m_RetryCount;
            do
            {
                try
                {
                    m_Operation();
                    break;
                }
                catch (T)
                {
                    if (retry <= 0)
                        throw;
                    if (m_SleepTime > TimeSpan.Zero)
                        Thread.Sleep(m_SleepTime);
                }
            } while (retry-- > 0);
        }

        public static void Execute(Action operation, uint retryCount, TimeSpan sleepTimeMs)
        {
            var retriableOperation = new RetriableOperation<T>(operation, retryCount, sleepTimeMs);
            retriableOperation.Execute();
        }

        public static void Execute(Action operation)
        {
            Execute(operation, DefaultRetryCount, DefaultSleepTimeMs);
        }
    }
}
