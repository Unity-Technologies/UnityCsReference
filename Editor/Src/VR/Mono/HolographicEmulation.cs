// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditorInternal.VR
{
    public class SimulatedBody
    {
        // Require use of HolographicEmulation.simulatedBody to get instance
        internal SimulatedBody() {}

        public Vector3 position
        {
            get
            {
                return Vector3.zero;
            }
            set
            {
            }
        }

        public float rotation
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        public float height
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }
    }

    public class SimulatedHead
    {
        // Require use of HolographicEmulation.simulatedHead to get instance
        internal SimulatedHead() {}

        public float diameter
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        public Vector3 eulerAngles
        {
            get
            {
                return Vector3.zero;
            }
            set
            {
            }
        }
    }

    public class SimulatedHand
    {

        // Require use of HolographicEmulation.simulatedLeftHand or HolographicEmulation.simulatedRightHand to get instance
        internal SimulatedHand(GestureHand hand)
        {
        }

        public Vector3 position
        {
            get
            {
                return Vector3.zero;
            }
            set
            {
            }
        }

        public bool activated
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        public bool visible
        {
            get
            {
                return false;
            }
        }

        public void EnsureVisible()
        {
        }

        public void PerformGesture(SimulatedGesture gesture)
        {
        }
    }

    public sealed partial class HolographicEmulation
    {
        static SimulatedBody s_Body = new SimulatedBody();
        static SimulatedHead s_Head = new SimulatedHead();
        static SimulatedHand s_LeftHand = new SimulatedHand(GestureHand.Left);
        static SimulatedHand s_RightHand = new SimulatedHand(GestureHand.Right);

        public static SimulatedBody simulatedBody { get { return s_Body; } }
        public static SimulatedHead simulatedHead { get { return s_Head; } }
        public static SimulatedHand simulatedLeftHand { get { return s_LeftHand; } }
        public static SimulatedHand simulatedRightHand { get { return s_RightHand; } }
    }

    public static class HolographicEmulationHelpers
    {
        public const float k_DefaultBodyHeight = 1.776f;
        public const float k_DefaultHeadDiameter = 0.2319999f;
        public const float k_ForwardOffset = 0.0985f;

        // this method uses the experimentally-found constants above to adjust
        // transform data in order to account for an emulator with different
        // settings for height, head size, and offset from the center of the head
        // to where the eyes are actually located (the respective constants are
        // defined above)
        public static Vector3 CalcExpectedCameraPosition(SimulatedHead head, SimulatedBody body)
        {
            Vector3 adjustedCameraPosition = body.position;

            // account for body height difference and offset for head size
            adjustedCameraPosition.y += body.height - k_DefaultBodyHeight;

            adjustedCameraPosition.y -= head.diameter / 2f;
            adjustedCameraPosition.y += k_DefaultHeadDiameter / 2f;

            // get total effective head rotation and convert to quaternion...
            var angles = head.eulerAngles;
            angles.y += body.rotation;
            var rotation = Quaternion.Euler(angles);

            // ...to account for the experimentally found forward-offset between the
            // center of the simulated head and average position of the eyes
            adjustedCameraPosition += rotation * (k_ForwardOffset * Vector3.forward);

            return adjustedCameraPosition;
        }
    }
}
