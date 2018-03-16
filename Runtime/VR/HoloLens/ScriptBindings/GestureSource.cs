// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Scripting.APIUpdating;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;


namespace UnityEngine.XR.WSA.Input
{
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public enum InteractionSourceKind
    {
        Other,
        Hand,
        Voice,
        Controller
    }

    internal enum InteractionSourceFlags
    {
        None = 0,
        SupportsGrasp = 1 << 0,
        SupportsMenu = 1 << 1,
        SupportsPointing = 1 << 2,
        SupportsTouchpad = 1 << 3,
        SupportsThumbstick = 1 << 4,
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct InteractionSource
    {
        public override bool Equals(object obj)
        {
            InteractionSource? source = obj as InteractionSource ? ;
            if (source == null)
                return false;

            return source.Value.m_Id == m_Id;
        }

        public override int GetHashCode()
        {
            return (int)m_Id;
        }

        public uint id { get { return m_Id; } }
        public InteractionSourceKind kind { get { return m_SourceKind; } }

        public InteractionSourceHandedness handedness
        { get { return m_Handedness; } }

        public bool supportsGrasp
        { get { return (m_Flags & InteractionSourceFlags.SupportsGrasp) != 0; } }

        public bool supportsMenu
        { get { return (m_Flags & InteractionSourceFlags.SupportsMenu) != 0; } }

        public bool supportsPointing
        { get { return (m_Flags & InteractionSourceFlags.SupportsPointing) != 0; } }

        public bool supportsThumbstick
        { get { return (m_Flags & InteractionSourceFlags.SupportsThumbstick) != 0; } }

        public bool supportsTouchpad
        { get { return (m_Flags & InteractionSourceFlags.SupportsTouchpad) != 0; } }

        public ushort vendorId
        { get { return m_VendorId; } }

        public ushort productId
        { get { return m_ProductId; } }

        public ushort productVersion
        { get { return m_ProductVersion; } }

        internal uint m_Id;
        internal InteractionSourceKind m_SourceKind;
        internal InteractionSourceHandedness m_Handedness;
        internal InteractionSourceFlags m_Flags;

        internal ushort m_VendorId;
        internal ushort m_ProductId;
        internal ushort m_ProductVersion;
    }

    internal enum InteractionSourcePoseFlags
    {
        None = 0,
        HasGripPosition = 1 << 0,
        HasGripRotation = 1 << 1,
        HasPointerPosition = 1 << 2,
        HasPointerRotation = 1 << 3,
        HasVelocity = 1 << 4,
        HasAngularVelocity = 1 << 5,
    }

    internal enum InteractionSourceStateFlags
    {
        None = 0,
        Grasped = 1 << 0,
        AnyPressed = 1 << 1,
        TouchpadPressed = 1 << 2,
        ThumbstickPressed = 1 << 3,
        SelectPressed = 1 << 4,
        MenuPressed = 1 << 5,
        TouchpadTouched = 1 << 6,
    }

    public enum InteractionSourceHandedness
    {
        Unknown,
        Left,
        Right
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public enum InteractionSourcePressType
    {
        None,
        Select,
        Menu,
        Grasp,
        Touchpad,
        Thumbstick
    }

    public enum InteractionSourceNode
    {
        Grip,
        Pointer
    }

    public enum InteractionSourcePositionAccuracy
    {
        None,
        Approximate,
        High
    }

    [RequiredByNativeCode]
    public partial struct InteractionSourcePose
    {
        public bool TryGetPosition(out Vector3 position)
        {
            return TryGetPosition(out position, InteractionSourceNode.Grip);
        }

        public bool TryGetPosition(out Vector3 position, InteractionSourceNode node)
        {
            if (node == InteractionSourceNode.Grip)
            {
                position = m_GripPosition;
                return (m_Flags & InteractionSourcePoseFlags.HasGripPosition) != 0;
            }
            else
            {
                position = m_PointerPosition;
                return (m_Flags & InteractionSourcePoseFlags.HasPointerPosition) != 0;
            }
        }

        public bool TryGetRotation(out Quaternion rotation, InteractionSourceNode node = InteractionSourceNode.Grip)
        {
            if (node == InteractionSourceNode.Grip)
            {
                rotation = m_GripRotation;
                return (m_Flags & InteractionSourcePoseFlags.HasGripRotation) != 0;
            }
            else
            {
                rotation = m_PointerRotation;
                return (m_Flags & InteractionSourcePoseFlags.HasPointerRotation) != 0;
            }
        }

        public bool TryGetForward(out Vector3 forward, InteractionSourceNode node = InteractionSourceNode.Grip)
        {
            Quaternion rotation;
            bool ret = TryGetRotation(out rotation, node);
            forward = rotation * Vector3.forward;
            return ret;
        }

        public bool TryGetRight(out Vector3 right, InteractionSourceNode node = InteractionSourceNode.Grip)
        {
            Quaternion rotation;
            bool ret = TryGetRotation(out rotation, node);
            right = rotation * Vector3.right;
            return ret;
        }

        public bool TryGetUp(out Vector3 up, InteractionSourceNode node = InteractionSourceNode.Grip)
        {
            Quaternion rotation;
            bool ret = TryGetRotation(out rotation, node);
            up = rotation * Vector3.up;
            return ret;
        }

        public bool TryGetVelocity(out Vector3 velocity)
        {
            velocity = m_Velocity;
            return (m_Flags & InteractionSourcePoseFlags.HasVelocity) != 0;
        }

        public bool TryGetAngularVelocity(out Vector3 angularVelocity)
        {
            angularVelocity = m_AngularVelocity;
            return (m_Flags & InteractionSourcePoseFlags.HasAngularVelocity) != 0;
        }

        public InteractionSourcePositionAccuracy positionAccuracy
        {
            get
            {
                return m_PositionAccuracy;
            }
        }

        internal Quaternion m_GripRotation;
        internal Quaternion m_PointerRotation;

        internal Vector3 m_GripPosition;
        internal Vector3 m_PointerPosition;
        internal Vector3 m_Velocity;
        internal Vector3 m_AngularVelocity;

        internal InteractionSourcePositionAccuracy m_PositionAccuracy;
        internal InteractionSourcePoseFlags m_Flags;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public partial struct InteractionSourceProperties
    {
        public double sourceLossRisk { get { return m_SourceLossRisk; } }
        public Vector3 sourceLossMitigationDirection { get { return m_SourceLossMitigationDirection; } }

        internal double m_SourceLossRisk;
        internal Vector3 m_SourceLossMitigationDirection;
        internal InteractionSourcePose m_SourcePose;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public partial struct InteractionSourceState
    {
        public bool anyPressed
        { get { return (m_Flags & InteractionSourceStateFlags.AnyPressed) != 0; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        public InteractionSourceProperties properties
        { get { return m_Properties; } }

        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_Properties.m_SourcePose; } }

        public float selectPressedAmount
        { get { return m_SelectPressedAmount; } }

        public bool selectPressed
        { get { return (m_Flags & InteractionSourceStateFlags.SelectPressed) != 0; } }

        public bool menuPressed
        { get { return (m_Flags & InteractionSourceStateFlags.MenuPressed) != 0; } }

        public bool grasped
        { get { return (m_Flags & InteractionSourceStateFlags.Grasped) != 0; } }

        public bool touchpadTouched
        { get { return (m_Flags & InteractionSourceStateFlags.TouchpadTouched) != 0; } }

        public bool touchpadPressed
        { get { return (m_Flags & InteractionSourceStateFlags.TouchpadPressed) != 0; } }

        public Vector2 touchpadPosition
        { get { return m_TouchpadPosition; } }

        public Vector2 thumbstickPosition
        { get { return m_ThumbstickPosition; } }

        public bool thumbstickPressed
        { get { return (m_Flags & InteractionSourceStateFlags.ThumbstickPressed) != 0; } }

        internal InteractionSourceProperties m_Properties;
        internal InteractionSource m_Source;
        internal Pose m_HeadPose;

        internal Vector2 m_ThumbstickPosition;
        internal Vector2 m_TouchpadPosition;

        internal float m_SelectPressedAmount;

        internal InteractionSourceStateFlags m_Flags;
    }

    public struct InteractionSourceDetectedEventArgs
    {
        public InteractionSourceState state
        { get; private set; }

        public InteractionSourceDetectedEventArgs(InteractionSourceState state) : this()
        {
            this.state = state;
        }
    }

    public struct InteractionSourceLostEventArgs
    {
        public InteractionSourceState state
        { get; private set; }

        public InteractionSourceLostEventArgs(InteractionSourceState state) : this()
        {
            this.state = state;
        }
    }

    public struct InteractionSourcePressedEventArgs
    {
        public InteractionSourceState state
        { get; private set; }

        public InteractionSourcePressType pressType
        { get; private set; }

        public InteractionSourcePressedEventArgs(InteractionSourceState state, InteractionSourcePressType pressType) : this()
        {
            this.state = state;
            this.pressType = pressType;
        }
    }

    public struct InteractionSourceReleasedEventArgs
    {
        public InteractionSourceState state
        { get; private set; }

        public InteractionSourcePressType pressType
        { get; private set; }

        public InteractionSourceReleasedEventArgs(InteractionSourceState state, InteractionSourcePressType pressType) : this()
        {
            this.state = state;
            this.pressType = pressType;
        }
    }

    public struct InteractionSourceUpdatedEventArgs
    {
        public InteractionSourceState state
        { get; private set; }

        public InteractionSourceUpdatedEventArgs(InteractionSourceState state) : this()
        {
            this.state = state;
        }
    }

    public partial class InteractionManager
    {
        public static event Action<InteractionSourceDetectedEventArgs> InteractionSourceDetected;
        public static event Action<InteractionSourceLostEventArgs> InteractionSourceLost;
        public static event Action<InteractionSourcePressedEventArgs> InteractionSourcePressed;
        public static event Action<InteractionSourceReleasedEventArgs> InteractionSourceReleased;
        public static event Action<InteractionSourceUpdatedEventArgs> InteractionSourceUpdated;

        public static int GetCurrentReading(InteractionSourceState[] sourceStates)
        {
            if (sourceStates == null)
                throw new ArgumentNullException("sourceStates");

            if (sourceStates.Length > 0)
                return GetCurrentReading_Internal(sourceStates);
            else
                return 0;
        }

        public static InteractionSourceState[] GetCurrentReading()
        {
            InteractionSourceState[] sourceStates = new InteractionSourceState[numSourceStates];
            if (sourceStates.Length > 0)
                GetCurrentReading_Internal(sourceStates);
            return sourceStates;
        }

        // In sync with Runtime/HoloLens/Gestures/GestureSource.h
        private enum EventType
        {
            SourceDetected,
            SourceLost,
            SourceUpdated,
            SourcePressed,
            SourceReleased
        }

        private delegate void InternalSourceEventHandler(EventType eventType, ref InteractionSourceState state, InteractionSourcePressType pressType);
        private static InternalSourceEventHandler m_OnSourceEventHandler;

        static InteractionManager()
        {
            m_OnSourceEventHandler = OnSourceEvent;
            Initialize(Marshal.GetFunctionPointerForDelegate(m_OnSourceEventHandler));
        }

#pragma warning disable 0618
        [AOT.MonoPInvokeCallback(typeof(InternalSourceEventHandler))]
        private static void OnSourceEvent(EventType eventType, ref InteractionSourceState state, InteractionSourcePressType pressType)
        {
            switch (eventType)
            {
                case EventType.SourceDetected:
                {
                    var deprecatedEventHandler = InteractionSourceDetectedLegacy;
                    if (deprecatedEventHandler != null)
                        deprecatedEventHandler(state);

                    var eventHandler = InteractionSourceDetected;
                    if (eventHandler != null)
                        eventHandler(new InteractionSourceDetectedEventArgs(state));
                }
                break;

                case EventType.SourceLost:
                {
                    var deprecatedEventHandler = InteractionSourceLostLegacy;
                    if (deprecatedEventHandler != null)
                        deprecatedEventHandler(state);

                    var eventHandler = InteractionSourceLost;
                    if (eventHandler != null)
                        eventHandler(new InteractionSourceLostEventArgs(state));
                }
                break;

                case EventType.SourceUpdated:
                {
                    var deprecatedEventHandler = InteractionSourceUpdatedLegacy;
                    if (deprecatedEventHandler != null)
                        deprecatedEventHandler(state);

                    var eventHandler = InteractionSourceUpdated;
                    if (eventHandler != null)
                        eventHandler(new InteractionSourceUpdatedEventArgs(state));
                }
                break;

                case EventType.SourcePressed:
                {
                    var deprecatedEventHandler = InteractionSourcePressedLegacy;
                    if (deprecatedEventHandler != null)
                        deprecatedEventHandler(state);

                    var eventHandler = InteractionSourcePressed;
                    if (eventHandler != null)
                        eventHandler(new InteractionSourcePressedEventArgs(state, pressType));
                }
                break;

                case EventType.SourceReleased:
                {
                    var deprecatedEventHandler = InteractionSourceReleasedLegacy;
                    if (deprecatedEventHandler != null)
                        deprecatedEventHandler(state);

                    var eventHandler = InteractionSourceReleased;
                    if (eventHandler != null)
                        eventHandler(new InteractionSourceReleasedEventArgs(state, pressType));
                }
                break;

                default:
                    throw new ArgumentException("OnSourceEvent: Invalid EventType");
            }
        }

#pragma warning restore 0618
    }
}
