namespace UnityEngine.UIElements
{
    static class PointerDeviceState
    {
        private static Vector2[] m_Positions = new Vector2[PointerId.maxPointers];
        private static IPanel[] m_Panels = new IPanel[PointerId.maxPointers];
        private static int[] m_PressedButtons = new int[PointerId.maxPointers];

        // For test usage
        internal static void Reset()
        {
            for (var i = 0; i < PointerId.maxPointers; i++)
            {
                m_Positions[i] = Vector2.zero;
                m_Panels[i] = null;
                m_PressedButtons[i] = 0;
            }
        }

        public static void SavePointerPosition(int pointerId, Vector2 position, IPanel panel)
        {
            m_Positions[pointerId] = position;
            m_Panels[pointerId] = panel;
        }

        public static void PressButton(int pointerId, int buttonId)
        {
            Debug.Assert(buttonId >= 0);
            Debug.Assert(buttonId < 32);
            m_PressedButtons[pointerId] |= (1 << buttonId);
        }

        public static void ReleaseButton(int pointerId, int buttonId)
        {
            Debug.Assert(buttonId >= 0);
            Debug.Assert(buttonId < 32);
            m_PressedButtons[pointerId] &= ~(1 << buttonId);
        }

        public static void ReleaseAllButtons(int pointerId)
        {
            m_PressedButtons[pointerId] = 0;
        }

        public static Vector2 GetPointerPosition(int pointerId)
        {
            return m_Positions[pointerId];
        }

        public static IPanel GetPanel(int pointerId)
        {
            return m_Panels[pointerId];
        }

        public static int GetPressedButtons(int pointerId)
        {
            return m_PressedButtons[pointerId];
        }

        internal static bool HasAdditionalPressedButtons(int pointerId, int exceptButtonId)
        {
            return (m_PressedButtons[pointerId] & ~(1 << exceptButtonId)) != 0;
        }
    }
}
