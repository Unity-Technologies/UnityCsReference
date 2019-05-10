// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine.XR.WSA
{
    internal class SimulatedSpatialController
    {
        public Handedness m_ControllerHandednss;

        internal SimulatedSpatialController(Handedness controller)
        {
            m_ControllerHandednss = controller;
        }

        public Quaternion orientation
        {
            get { return HolographicAutomation.GetHandOrientation(m_ControllerHandednss); }
            set { HolographicAutomation.TrySetHandOrientation(m_ControllerHandednss, value); }
        }

        public Vector3 position
        {
            get { return HolographicAutomation.GetControllerPosition(m_ControllerHandednss); }
            set { HolographicAutomation.TrySetControllerPosition(m_ControllerHandednss, value); }
        }

        public bool activated
        {
            get { return HolographicAutomation.GetControllerActivated(m_ControllerHandednss); }
            set { HolographicAutomation.TrySetControllerActivated(m_ControllerHandednss, value); }
        }

        public bool visible
        {
            get { return HolographicAutomation.GetControllerVisible(m_ControllerHandednss); }
        }

        public void EnsureVisible()
        {
            HolographicAutomation.TryEnsureControllerVisible(m_ControllerHandednss);
        }

        public void PerformControllerPress(SimulatedControllerPress button)
        {
            HolographicAutomation.PerformButtonPress(m_ControllerHandednss, button);
        }
    }
    internal class SimulatedBody
    {
        // Require use of HolographicEmulation.simulatedBody to get instance
        public SimulatedBody() {}

        public Vector3 position
        {
            get { return HolographicAutomation.GetBodyPosition(); }
            set { HolographicAutomation.SetBodyPosition(value); }
        }

        public float rotation
        {
            get { return HolographicAutomation.GetBodyRotation(); }
            set { HolographicAutomation.SetBodyRotation(value); }
        }

        public float height
        {
            get { return HolographicAutomation.GetBodyHeight(); }
            set { HolographicAutomation.SetBodyHeight(value); }
        }
    }

    internal class SimulatedHead
    {
        // Require use of HolographicEmulation.simulatedHead to get instance
        public SimulatedHead() {}

        public float diameter
        {
            get { return HolographicAutomation.GetHeadDiameter(); }
            set { HolographicAutomation.SetHeadDiameter(value); }
        }

        public Vector3 eulerAngles
        {
            get { return HolographicAutomation.GetHeadRotation(); }
            set { HolographicAutomation.SetHeadRotation(value); }
        }
    }

    internal class SimulatedHand
    {
        public Handedness m_Hand;

        // Require use of HolographicEmulation.simulatedLeftHand or HolographicEmulation.simulatedRightHand to get instance
        internal SimulatedHand(Handedness hand)
        {
            m_Hand = hand;
        }

        public Vector3 position
        {
            get { return HolographicAutomation.GetHandPosition(m_Hand); }
            set { HolographicAutomation.SetHandPosition(m_Hand, value); }
        }

        public bool activated
        {
            get { return HolographicAutomation.GetHandActivated(m_Hand); }
            set { HolographicAutomation.SetHandActivated(m_Hand, value); }
        }

        public bool visible
        {
            get { return HolographicAutomation.GetHandVisible(m_Hand); }
        }

        public void EnsureVisible()
        {
            HolographicAutomation.EnsureHandVisible(m_Hand);
        }

        public void PerformGesture(SimulatedGesture gesture)
        {
            HolographicAutomation.PerformGesture(m_Hand, gesture);
        }
    }

    internal partial class HolographicAutomation
    {
        static SimulatedBody s_Body = new SimulatedBody();
        static SimulatedHead s_Head = new SimulatedHead();
        static SimulatedHand s_LeftHand = new SimulatedHand(Handedness.Left);
        static SimulatedHand s_RightHand = new SimulatedHand(Handedness.Right);
        static SimulatedSpatialController s_LeftController = new SimulatedSpatialController(Handedness.Left);
        static SimulatedSpatialController s_RightController = new SimulatedSpatialController(Handedness.Right);

        public static SimulatedBody simulatedBody { get { return s_Body; } }
        public static SimulatedHead simulatedHead { get { return s_Head; } }
        public static SimulatedHand simulatedLeftHand { get { return s_LeftHand; } }
        public static SimulatedHand simulatedRightHand { get { return s_RightHand; } }

        public static SimulatedSpatialController simulatedLeftController { get { return s_LeftController; } }
        public static SimulatedSpatialController simulatedRightController { get { return s_RightController; } }
    }

    internal static class HolographicEmulationHelpers
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
