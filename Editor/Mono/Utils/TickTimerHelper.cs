// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor
{
    internal class TickTimerHelper
    {
        double m_NextTick;
        double m_Interval;

        public TickTimerHelper(double intervalBetweenTicksInSeconds)
        {
            m_Interval = intervalBetweenTicksInSeconds;
        }

        public bool DoTick()
        {
            if (EditorApplication.timeSinceStartup > m_NextTick)
            {
                m_NextTick = EditorApplication.timeSinceStartup + m_Interval;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            m_NextTick = 0;
        }
    }
}
