// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor
{
    enum SceneInputAxis : int
    {
        Right = 0,
        Up = 1,
        Forward = 2,
        Left = 3,
        Down = 4,
        Backward = 5
    }

    static class SceneNavigationInput
    {
        static Vector3 m_PreviousVector;
        public static float deltaTime => s_Timer.Update();
        static TimeHelper s_Timer = new TimeHelper();
        static readonly MovementVector s_CurrentInputVector = new MovementVector();

        public static bool moving => !Mathf.Approximately(currentInputVector.sqrMagnitude, 0f);
        public static Vector3 currentInputVector => s_CurrentInputVector.direction;
        public static MovementVector input => s_CurrentInputVector;

        // helper class keeps track of the last pressed direction key per-axis. this allows fps view to continue working
        // when inverse keys on the same axis are pressed and released simultaneously.
        public class MovementVector
        {
            // x, y, z, -x, -y, -z
            readonly bool[] m_Pressed = new bool[6] { false, false, false, false, false, false };
            Vector3 m_Direction = Vector3.zero;

            public Vector3 direction => m_Direction;

            static SceneInputAxis InverseAxis(SceneInputAxis i) => (int) i > 2 ? i - 3 : i + 3;

            public bool this[SceneInputAxis i]
            {
                get => m_Pressed[(int) i];

                set
                {
                    // on key down, apply direction. on key up, check if the corresponding opposite key is pressed and
                    // set direction to inverse if true, or 0 if no key on axis is pressed.
                    // ReSharper disable once AssignmentInConditionalExpression
                    if (m_Pressed[(int)i] = value)
                        m_Direction[(int)i % 3] = (int) i > 2 ? -1 : 1;
                    else if (m_Pressed[(int)InverseAxis(i)])
                        m_Direction[(int)i % 3] = (int) i > 2 ? 1 : -1;
                    else
                        m_Direction[(int)i % 3] = 0f;
                }
            }
        }

        public static void Update()
        {
            if (moving && Mathf.Approximately(m_PreviousVector.sqrMagnitude, 0f))
                s_Timer.Begin();

            m_PreviousVector = currentInputVector;
        }
    }

    [PriorityContext, ReserveModifiers(ShortcutModifiers.Shift)]
    class CameraFlyModeContext : ClutchShortcutContext
    {
        EditorWindow m_Window;
        public EditorWindow window
        {
            get => m_Window;
            set => m_Window = value;
        }
    }
}
