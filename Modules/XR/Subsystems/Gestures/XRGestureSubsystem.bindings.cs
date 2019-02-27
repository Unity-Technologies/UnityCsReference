// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.XR;
using System.Collections.Generic;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.XR
{
    [Flags]
    [UsedByNativeCode]
    public enum GestureEventTypes : UInt32
    {
        None = 0,
        Hold = 1 << 0,
        Manipulation = 1 << 1,
        Navigation = 1 << 2,
        Recognition = 1 << 3,
        Tapped = 1 << 4,

        All = ~0u
    };

    [UsedByNativeCode]
    public enum GestureEventState : UInt32
    {
        Discrete,      // Event is a discrete event (used by Tapped event)
        Started,       // Event has been started (used by Hold, Manipulation, Navigation and Recognition events)
        Updated,       // Event has been updated (used by Navigation event)
        Completed,     // Event has been completed (used by Hold, Manipulation and Navigation and Recognition events)
        Canceled       // Event has been cancelled (used by Hold, Manipulation and Navigation events)
    }

    [Flags]
    public enum GestureTrackingCoordinates : UInt32
    {
        None = 0,
        X = 1 << 0,
        Y = 1 << 1,
        Z = 1 << 2,
    }

    [Flags]
    public enum GestureHoldValidFields : UInt32
    {
        None = 0,
        TimeStamp = 1 << 0,
        DeviceId = 1 << 1,
        PointerPose = 1 << 2
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(Header = "Modules/XR/Subsystems/Gestures/ProviderInterface/IUnityXRGesture.h")]
    [NativeConditional("ENABLE_XR")]
    internal struct NativeGestureHoldEvent
    {
        internal GestureEventState eventState { get; set; }
        internal Int64 timeStamp { get; set; }
        internal UInt32 internalDeviceId { get; set; }
        internal Pose pointerPose { get; set; }
        internal GestureHoldValidFields validFields { get; set; }
    }

    public struct GestureHoldEvent
    {
        public GestureEventState eventState { get { return nativeEvent.eventState; } }
        public Int64 timeStamp { get { return nativeEvent.timeStamp; } }
        public Pose pointerPose { get { return nativeEvent.pointerPose; } }
        public GestureHoldValidFields validFields { get { return nativeEvent.validFields; } }

        internal NativeGestureHoldEvent nativeEvent { get; set; }
        internal XRGestureSubsystem gestureSubsystem { get; set; }
        public InputDevice inputDevice { get { return gestureSubsystem.GetInputDeviceForInternalDeviceId(nativeEvent.internalDeviceId); } }
    }

    [Flags]
    public enum GestureManipulationValidFields : UInt32
    {
        None = 0,
        TimeStamp = 1 << 0,
        DeviceId = 1 << 1,
        Translation = 1 << 2,
        PointerPose = 1 << 3
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(Header = "Modules/XR/Subsystems/Gestures/ProviderInterface/IUnityXRGesture.h")]
    [NativeConditional("ENABLE_XR")]
    internal struct NativeGestureManipulationEvent
    {
        internal GestureEventState eventState { get; set; }
        internal Int64 timeStamp { get; set; }
        internal UInt32 internalDeviceId { get; set; }
        internal Vector3 translation { get; set; }
        internal Pose pointerPose { get; set; }
        internal GestureManipulationValidFields validFields { get; set; }
    }

    public struct GestureManipulationEvent
    {
        public GestureEventState eventState { get { return nativeEvent.eventState; } }
        public Int64 timeStamp { get { return nativeEvent.timeStamp; } }
        public Vector3 translation { get { return nativeEvent.translation; } }
        public Pose pointerPose { get { return nativeEvent.pointerPose; } }
        public GestureManipulationValidFields validFields { get { return nativeEvent.validFields; } }

        internal NativeGestureManipulationEvent nativeEvent { get; set; }
        internal XRGestureSubsystem gestureSubsystem { get; set; }
        public InputDevice inputDevice { get { return gestureSubsystem.GetInputDeviceForInternalDeviceId(nativeEvent.internalDeviceId); } }
    }

    [Flags]
    public enum GestureNavigationValidFields : UInt32
    {
        None = 0,
        TimeStamp = 1 << 0,
        DeviceId = 1 << 1,
        GestureTrackingCoordinates = 1 << 2,
        NormalizedOffset = 1 << 3,
        PointerPose = 1 << 4
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(Header = "Modules/XR/Subsystems/Gestures/ProviderInterface/IUnityXRGesture.h")]
    [NativeConditional("ENABLE_XR")]
    internal struct NativeGestureNavigationEvent
    {
        internal GestureEventState eventState { get; set; }
        internal Int64 timeStamp { get; set; }
        internal UInt32 internalDeviceId { get; set; }
        internal GestureTrackingCoordinates gestureTrackingCoordinates { get; set; }
        internal Vector3 normalizedOffset { get; set; }
        internal Pose pointerPose { get; set; }
        internal GestureNavigationValidFields validFields { get; set; }
    }

    public struct GestureNavigationEvent
    {
        public GestureEventState eventState { get { return nativeEvent.eventState; } }
        public Int64 timeStamp { get { return nativeEvent.timeStamp; } }
        public GestureTrackingCoordinates gestureTrackingCoordinates { get { return nativeEvent.gestureTrackingCoordinates; } }
        public Vector3 normalizedOffset { get { return nativeEvent.normalizedOffset; } }
        public Pose pointerPose { get { return nativeEvent.pointerPose; } }
        public GestureNavigationValidFields validFields { get { return nativeEvent.validFields; } }

        internal NativeGestureNavigationEvent nativeEvent { get; set; }
        internal XRGestureSubsystem gestureSubsystem { get; set; }
        public InputDevice inputDevice { get { return gestureSubsystem.GetInputDeviceForInternalDeviceId(nativeEvent.internalDeviceId); } }
    }

    [Flags]
    public enum GestureRecognitionValidFields : UInt32
    {
        None = 0,
        TimeStamp = 1 << 0,
        DeviceId = 1 << 1,
        PointerPose = 1 << 2
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(Header = "Modules/XR/Subsystems/Gestures/ProviderInterface/IUnityXRGesture.h")]
    [NativeConditional("ENABLE_XR")]
    internal struct NativeGestureRecognitionEvent
    {
        internal GestureEventState eventState { get; set; }
        internal Int64 timeStamp { get; set; }
        internal UInt32 internalDeviceId { get; set; }
        internal Pose pointerPose { get; set; }
        internal GestureRecognitionValidFields validFields { get; set; }
    }

    public struct GestureRecognitionEvent
    {
        public GestureEventState eventState { get { return nativeEvent.eventState; } }
        public Int64 timeStamp { get { return nativeEvent.timeStamp; } }
        public Pose pointerPose { get { return nativeEvent.pointerPose; } }
        public GestureRecognitionValidFields validFields { get { return nativeEvent.validFields; } }

        internal NativeGestureRecognitionEvent nativeEvent { get; set; }
        internal XRGestureSubsystem gestureSubsystem { get; set; }
        public InputDevice inputDevice { get { return gestureSubsystem.GetInputDeviceForInternalDeviceId(nativeEvent.internalDeviceId); } }
    }

    [Flags]
    public enum GestureTappedValidFields : UInt32
    {
        None = 0,
        TimeStamp = 1 << 0,
        DeviceId = 1 << 1,
        TappedCount = 1 << 2,
        PointerPose = 1 << 3
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(Header = "Modules/XR/Subsystems/Gestures/ProviderInterface/IUnityXRGesture.h")]
    [NativeConditional("ENABLE_XR")]
    internal struct NativeGestureTappedEvent
    {
        internal GestureEventState eventState { get; set; }
        internal Int64 timeStamp { get; set; }
        internal UInt32 internalDeviceId { get; set; }
        internal UInt32 tappedCount { get; set; }
        internal Pose pointerPose { get; set; }
        internal GestureTappedValidFields validFields { get; set; }
    }

    public struct GestureTappedEvent
    {
        public GestureEventState eventState { get { return nativeEvent.eventState; } }
        public Int64 timeStamp { get { return nativeEvent.timeStamp; } }
        public UInt32 tappedCount { get { return nativeEvent.tappedCount; } }
        public Pose pointerPose { get { return nativeEvent.pointerPose; } }
        public GestureTappedValidFields validFields { get { return nativeEvent.validFields; } }

        internal NativeGestureTappedEvent nativeEvent { get; set; }
        internal XRGestureSubsystem gestureSubsystem { get; set; }
        public InputDevice inputDevice { get { return gestureSubsystem.GetInputDeviceForInternalDeviceId(nativeEvent.internalDeviceId); } }
    }

    [NativeType(Header = "Modules/XR/Subsystems/Gestures/XRGestureSubsystem.h")]
    [UsedByNativeCode]
    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeConditional("ENABLE_XR")]
    public class XRGestureSubsystem : IntegratedSubsystem<XRGestureSubsystemDescriptor>
    {
        private Action<GestureHoldEvent> m_HoldChanged;
        public event Action<GestureHoldEvent> HoldChanged
        {
            add
            {
                m_HoldChanged += value;
            }
            remove
            {
                m_HoldChanged -= value;
            }
        }

        [RequiredByNativeCode]
        private void InvokeHoldChanged(NativeGestureHoldEvent gestureEvent)
        {
            if (m_HoldChanged != null)
            {
                m_HoldChanged(new GestureHoldEvent() { nativeEvent = gestureEvent, gestureSubsystem = this });
            }
        }

        private Action<GestureManipulationEvent> m_ManipulationChanged;
        public event Action<GestureManipulationEvent> ManipulationChanged
        {
            add
            {
                m_ManipulationChanged += value;
            }
            remove
            {
                m_ManipulationChanged -= value;
            }
        }

        [RequiredByNativeCode]
        private void InvokeManipulationChanged(NativeGestureManipulationEvent gestureEvent)
        {
            if (m_ManipulationChanged != null)
            {
                m_ManipulationChanged(new GestureManipulationEvent() { nativeEvent = gestureEvent, gestureSubsystem = this });
            }
        }

        private Action<GestureNavigationEvent> m_NavigationChanged;
        public event Action<GestureNavigationEvent> NavigationChanged
        {
            add
            {
                m_NavigationChanged += value;
            }
            remove
            {
                m_NavigationChanged -= value;
            }
        }

        [RequiredByNativeCode]
        private void InvokeNavigationChanged(NativeGestureNavigationEvent gestureEvent)
        {
            if (m_NavigationChanged != null)
            {
                m_NavigationChanged(new GestureNavigationEvent() { nativeEvent = gestureEvent, gestureSubsystem = this });
            }
        }

        private Action<GestureRecognitionEvent> m_RecognitionChanged;
        public event Action<GestureRecognitionEvent> RecognitionChanged
        {
            add
            {
                m_RecognitionChanged += value;
            }
            remove
            {
                m_RecognitionChanged -= value;
            }
        }

        [RequiredByNativeCode]
        private void InvokeRecognitionChanged(NativeGestureRecognitionEvent gestureEvent)
        {
            if (m_RecognitionChanged != null)
            {
                m_RecognitionChanged(new GestureRecognitionEvent() { nativeEvent = gestureEvent, gestureSubsystem = this });
            }
        }

        private Action<GestureTappedEvent> m_TappedChanged;
        public event Action<GestureTappedEvent> TappedChanged
        {
            add
            {
                m_TappedChanged += value;
            }
            remove
            {
                m_TappedChanged -= value;
            }
        }

        [RequiredByNativeCode]
        private void InvokeTappedChanged(NativeGestureTappedEvent gestureEvent)
        {
            if (m_TappedChanged != null)
            {
                m_TappedChanged(new GestureTappedEvent() { nativeEvent = gestureEvent, gestureSubsystem = this });
            }
        }

        public extern GestureEventTypes GetAvailableGestures();
        public extern bool SetEnabledGestures(GestureEventTypes enabledGestures);
        public extern bool CancelAllGestures();
        internal InputDevice GetInputDeviceForInternalDeviceId(UInt32 internalDeviceId)
        {
            return new InputDevice(GetInputDeviceIdForInternalDeviceId(internalDeviceId));
        }

        internal extern UInt64 GetInputDeviceIdForInternalDeviceId(UInt32 internalDeviceId);
    }
}
