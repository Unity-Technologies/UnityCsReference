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

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct InteractionSource
    {
        public InteractionSource(uint sourceId, InteractionSourceKind sourceKind) : this()
        {
            m_Id = sourceId;
            m_Kind = sourceKind;
        }

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
        public InteractionSourceKind kind { get { return m_Kind; } }

        internal uint m_Id;
        internal InteractionSourceKind m_Kind;
    }

    internal enum InteractionSourceRayFlags
    {
        None = 0,
        HasPosition = 1 << 0,
        HasRotation = 1 << 1
    }

    internal enum InteractionSourceLocationFlags
    {
        None = 0,
        HasVelocity = 1 << 0
    }

    internal enum InteractionSourceStateFlags
    {
        None = 0,
        SupportsTouchpad = 1 << 0,
        SupportsThumbstick = 1 << 1,
        SupportsPointing = 1 << 2,
        SupportsGrasp = 1 << 3,
        SupportsMenu = 1 << 4,
        Grasped = 1 << 5,
        AnyPressed = 1 << 6,
        TouchpadPressed = 1 << 7,
        ThumbstickPressed = 1 << 8,
        SelectPressed = 1 << 9,
        MenuPressed = 1 << 10,
        TouchpadTouched = 1 << 11,
        VendorDataValid = 1 << 12
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public enum InteractionSourceHandType
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

#pragma warning disable 649 //Field is never assigned to and will always have its default value
    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct InteractionSourceLocation
    {
        public bool TryGetVelocity(out Vector3 velocity)
        {
            velocity = m_Velocity;
            return (m_Flags & InteractionSourceLocationFlags.HasVelocity) != 0;
        }

        [Obsolete("InteractionSourceLocation.TryGetPosition is deprecated, and will be removed in a future release. Update your scripts to use InteractionSourceLocation.pointer.TryGetPosition instead.", false)]
        public bool TryGetPosition(out Vector3 position)
        {
            return m_Pointer.TryGetPosition(out position);
        }

        public InteractionSourceRay pointer
        { get { return m_Pointer; } }

        public InteractionSourceRay grip
        { get { return m_Grip; } }

        InteractionSourceRay m_Pointer;
        InteractionSourceRay m_Grip;

        Vector3 m_Velocity;
        InteractionSourceLocationFlags m_Flags;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct InteractionSourceRay
    {
        public bool TryGetPosition(out Vector3 position)
        {
            position = m_Position;
            return (m_Flags & InteractionSourceRayFlags.HasPosition) != 0;
        }

        public bool TryGetRotation(out Quaternion rotation)
        {
            rotation = m_Rotation;
            return (m_Flags & InteractionSourceRayFlags.HasRotation) != 0;
        }

        public bool TryGetRay(out Ray ray)
        {
            ray = new Ray(m_Position, m_Rotation * Vector3.forward);
            return (m_Flags & InteractionSourceRayFlags.HasPosition) != 0 && (m_Flags & InteractionSourceRayFlags.HasRotation) != 0;
        }

        internal Vector3 m_Position;
        internal Quaternion m_Rotation;

        internal InteractionSourceRayFlags m_Flags;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct InteractionSourceProperties
    {
        public double sourceLossRisk { get { return m_sourceLossRisk; } }
        public Vector3 sourceLossMitigationDirection { get { return m_sourceLossMitigationDirection; } }
        public InteractionSourceLocation location { get { return m_location; } }

        internal double m_sourceLossRisk;
        internal Vector3 m_sourceLossMitigationDirection;
        internal InteractionSourceLocation m_location;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public partial struct InteractionSourceState
    {
        public bool anyPressed
        { get { return (m_Flags & InteractionSourceStateFlags.AnyPressed) != 0; } }

        public InteractionSourceProperties properties
        { get { return m_Properties; } }

        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourceRay headSourceRay
        { get { return m_HeadRay; } }

        public float selectPressedAmount
        { get { return m_SelectPressedAmount; } }

        public InteractionSourceHandType handType
        { get { return m_HandType; } }

        public bool TryGetVendorId(out ushort vendorId)
        {
            vendorId = m_VendorId;
            return (m_Flags & InteractionSourceStateFlags.VendorDataValid) != 0;
        }

        public bool TryGetProductId(out ushort productId)
        {
            productId = m_ProductId;
            return (m_Flags & InteractionSourceStateFlags.VendorDataValid) != 0;
        }

        public bool TryGetProductVersion(out ushort productVersion)
        {
            productVersion = m_ProductVersion;
            return (m_Flags & InteractionSourceStateFlags.VendorDataValid) != 0;
        }

        public bool selectPressed
        { get { return (m_Flags & InteractionSourceStateFlags.SelectPressed) != 0; } }

        public bool supportsPointing
        { get { return (m_Flags & InteractionSourceStateFlags.SupportsPointing) != 0; } }

        public bool supportsMenu
        { get { return (m_Flags & InteractionSourceStateFlags.SupportsMenu) != 0; } }

        public bool menuPressed
        { get { return (m_Flags & InteractionSourceStateFlags.MenuPressed) != 0; } }

        public bool supportsGrasp
        { get { return (m_Flags & InteractionSourceStateFlags.SupportsGrasp) != 0; } }

        public bool grasped
        { get { return (m_Flags & InteractionSourceStateFlags.Grasped) != 0; } }

        public bool supportsTouchpad
        { get { return (m_Flags & InteractionSourceStateFlags.SupportsTouchpad) != 0; } }

        public bool touchpadTouched
        { get { return (m_Flags & InteractionSourceStateFlags.TouchpadTouched) != 0; } }

        public bool touchpadPressed
        { get { return (m_Flags & InteractionSourceStateFlags.TouchpadPressed) != 0; } }

        public Vector2 touchpadPosition
        { get { return m_TouchpadPosition; } }

        public bool supportsThumbstick
        { get { return (m_Flags & InteractionSourceStateFlags.SupportsThumbstick) != 0; } }

        public Vector2 thumbstickPosition
        { get { return m_ThumbstickPosition; } }

        public bool thumbstickPressed
        { get { return (m_Flags & InteractionSourceStateFlags.ThumbstickPressed) != 0; } }

        InteractionSourceProperties m_Properties;
        InteractionSource m_Source;
        InteractionSourceRay m_HeadRay;

        internal Vector2 m_ThumbstickPosition;
        internal Vector2 m_TouchpadPosition;

        internal float m_SelectPressedAmount;

        internal InteractionSourceHandType m_HandType;

        internal InteractionSourceStateFlags m_Flags;

        internal ushort m_VendorId;
        internal ushort m_ProductId;
        internal ushort m_ProductVersion;
    }

#pragma warning restore 649

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct SourceDetectedEventArgs
    {
        public InteractionSourceState state
        { get; private set; }

        public SourceDetectedEventArgs(InteractionSourceState state) : this()
        {
            this.state = state;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct SourceLostEventArgs
    {
        public InteractionSourceState state
        { get; private set; }

        public SourceLostEventArgs(InteractionSourceState state) : this()
        {
            this.state = state;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct SourcePressedEventArgs
    {
        public InteractionSourceState state
        { get; private set; }

        public InteractionSourcePressType pressType
        { get; private set; }

        public SourcePressedEventArgs(InteractionSourceState state, InteractionSourcePressType pressType) : this()
        {
            this.state = state;
            this.pressType = pressType;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct SourceReleasedEventArgs
    {
        public InteractionSourceState state
        { get; private set; }

        public InteractionSourcePressType pressType
        { get; private set; }

        public SourceReleasedEventArgs(InteractionSourceState state, InteractionSourcePressType pressType) : this()
        {
            this.state = state;
            this.pressType = pressType;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct SourceUpdatedEventArgs
    {
        public InteractionSourceState state
        { get; private set; }

        public SourceUpdatedEventArgs(InteractionSourceState state) : this()
        {
            this.state = state;
        }
    }

    public partial class InteractionManager
    {
        public static event Action<SourceDetectedEventArgs> OnSourceDetected;
        public static event Action<SourceLostEventArgs> OnSourceLost;
        public static event Action<SourcePressedEventArgs> OnSourcePressed;
        public static event Action<SourceReleasedEventArgs> OnSourceReleased;
        public static event Action<SourceUpdatedEventArgs> OnSourceUpdated;

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

        private delegate void InternalSourceEventHandler(EventType eventType, InteractionSourceState state, InteractionSourcePressType pressType);
        private static InternalSourceEventHandler m_OnSourceEventHandler;

        static InteractionManager()
        {
            m_OnSourceEventHandler = OnSourceEvent;
            Initialize(Marshal.GetFunctionPointerForDelegate(m_OnSourceEventHandler));
        }

#pragma warning disable 0618
        [AOT.MonoPInvokeCallback(typeof(InternalSourceEventHandler))]
        private static void OnSourceEvent(EventType eventType, InteractionSourceState state, InteractionSourcePressType pressType)
        {
            switch (eventType)
            {
                case EventType.SourceDetected:
                {
                    var deprecatedEventHandler = SourceDetected;
                    if (deprecatedEventHandler != null)
                        deprecatedEventHandler(state);

                    var eventHandler = OnSourceDetected;
                    if (eventHandler != null)
                        eventHandler(new SourceDetectedEventArgs(state));
                }
                break;

                case EventType.SourceLost:
                {
                    var deprecatedEventHandler = SourceLost;
                    if (deprecatedEventHandler != null)
                        deprecatedEventHandler(state);

                    var eventHandler = OnSourceLost;
                    if (eventHandler != null)
                        eventHandler(new SourceLostEventArgs(state));
                }
                break;

                case EventType.SourceUpdated:
                {
                    var deprecatedEventHandler = SourceUpdated;
                    if (deprecatedEventHandler != null)
                        deprecatedEventHandler(state);

                    var eventHandler = OnSourceUpdated;
                    if (eventHandler != null)
                        eventHandler(new SourceUpdatedEventArgs(state));
                }
                break;

                case EventType.SourcePressed:
                {
                    var deprecatedEventHandler = SourcePressed;
                    if (deprecatedEventHandler != null)
                        deprecatedEventHandler(state);

                    var eventHandler = OnSourcePressed;
                    if (eventHandler != null)
                        eventHandler(new SourcePressedEventArgs(state, pressType));
                }
                break;

                case EventType.SourceReleased:
                {
                    var deprecatedEventHandler = SourceReleased;
                    if (deprecatedEventHandler != null)
                        deprecatedEventHandler(state);

                    var eventHandler = OnSourceReleased;
                    if (eventHandler != null)
                        eventHandler(new SourceReleasedEventArgs(state, pressType));
                }
                break;

                default:
                    throw new ArgumentException("OnSourceEvent: Invalid EventType");
            }
        }

#pragma warning restore 0618
    }
}
