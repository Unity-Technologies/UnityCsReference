// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    public class WaitForSecondsRealtime : CustomYieldInstruction
    {
        public float waitTime { get; set; }
        float m_WaitUntilTime = -1;

        public override bool keepWaiting
        {
            get
            {
                if (m_WaitUntilTime < 0)
                {
                    m_WaitUntilTime = Time.realtimeSinceStartup + waitTime;
                }

                bool wait =  Time.realtimeSinceStartup < m_WaitUntilTime;
                if (!wait)
                {
                    // Reset so it can be reused.
                    Reset();
                }
                return wait;
            }
        }

        public WaitForSecondsRealtime(float time)
        {
            waitTime = time;
        }

        public override void Reset()
        {
            m_WaitUntilTime = -1;
        }
    }
}
