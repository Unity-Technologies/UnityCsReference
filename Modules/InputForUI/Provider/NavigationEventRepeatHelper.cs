// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;

namespace UnityEngine.InputForUI
{
    internal class NavigationEventRepeatHelper
    {
        private int m_ConsecutiveMoveCount;
        private NavigationEvent.Direction m_LastDirection;
        private DiscreteTime m_PrevActionTime;

        // TODO move out to the settings
        private readonly DiscreteTime m_InitialRepeatDelay = new(0.5f);
        private readonly DiscreteTime m_ConsecutiveRepeatDelay = new(0.1f);

        public void Reset()
        {
            m_ConsecutiveMoveCount = 0;
            m_LastDirection = NavigationEvent.Direction.None;
            m_PrevActionTime = DiscreteTime.Zero;
        }

        public bool ShouldSendMoveEvent(DiscreteTime timestamp, NavigationEvent.Direction direction, bool axisButtonsWherePressedThisFrame)
        {
            if (
                // Always allow the event, if user pressed axis buttons.
                axisButtonsWherePressedThisFrame ||
                // Always allow the event if user changed the direction.
                direction != m_LastDirection ||
                // Otherwise wait for the delay. Which is either initial delay between first and second move event, or consecutive repeat delay between others.
                timestamp > m_PrevActionTime + (m_ConsecutiveMoveCount == 1 ? m_InitialRepeatDelay : m_ConsecutiveRepeatDelay)
            )
            {
                // Increment the move count if direction was the same, reset otherwise.
                m_ConsecutiveMoveCount = direction == m_LastDirection ? m_ConsecutiveMoveCount + 1 : 1;
                m_LastDirection = direction;
                m_PrevActionTime = timestamp;
                return true;
            }

            return false;
        }
    }
}
