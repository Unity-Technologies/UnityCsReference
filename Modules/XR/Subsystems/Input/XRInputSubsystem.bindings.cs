// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.XR
{
    public enum TrackingOriginModeFlags
    {
        Unknown = 0,
        Device = 1,
        Floor = 2,
        TrackingReference = 4
    }

    [NativeType(Header = "Modules/XR/Subsystems/Input/XRInputSubsystem.h")]
    [UsedByNativeCode]
    [NativeConditional("ENABLE_XR")]
    public class XRInputSubsystem : IntegratedSubsystem<XRInputSubsystemDescriptor>
    {
        internal extern UInt32 GetIndex();

        public extern bool TryRecenter();

        public bool TryGetInputDevices(List<InputDevice> devices)
        {
            if (devices == null)
                throw new ArgumentNullException("devices");

            devices.Clear();

            if (m_DeviceIdsCache == null)
                m_DeviceIdsCache = new List<UInt64>();

            m_DeviceIdsCache.Clear();

            TryGetDeviceIds_AsList(m_DeviceIdsCache);
            for (int i = 0; i < m_DeviceIdsCache.Count; i++)
            {
                devices.Add(new InputDevice(m_DeviceIdsCache[i]));
            }
            return true;
        }

        public extern bool TrySetTrackingOriginMode(TrackingOriginModeFlags origin);
        public extern TrackingOriginModeFlags GetTrackingOriginMode();
        public extern TrackingOriginModeFlags GetSupportedTrackingOriginModes();

        public bool TryGetBoundaryPoints(List<Vector3> boundaryPoints)
        {
            if (boundaryPoints == null)
                throw new ArgumentNullException("boundaryPoints");

            return TryGetBoundaryPoints_AsList(boundaryPoints);
        }

        private extern bool TryGetBoundaryPoints_AsList(List<Vector3> boundaryPoints);

        public event Action<XRInputSubsystem> trackingOriginUpdated;

        public event Action<XRInputSubsystem> boundaryChanged;

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void InvokeTrackingOriginUpdatedEvent(IntPtr internalPtr)
        {
            IntegratedSubsystem subsystem = SubsystemManager.GetIntegratedSubsystemByPtr(internalPtr);
            XRInputSubsystem inputSubsystem = subsystem as XRInputSubsystem;
            if ((inputSubsystem != null) && (inputSubsystem.trackingOriginUpdated != null))
                inputSubsystem.trackingOriginUpdated(inputSubsystem);
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void InvokeBoundaryChangedEvent(IntPtr internalPtr)
        {
            IntegratedSubsystem subsystem = SubsystemManager.GetIntegratedSubsystemByPtr(internalPtr);
            XRInputSubsystem inputSubsystem = subsystem as XRInputSubsystem;
            if ((inputSubsystem != null) && (inputSubsystem.boundaryChanged != null))
                inputSubsystem.boundaryChanged(inputSubsystem);
        }

        internal extern void TryGetDeviceIds_AsList(List<UInt64> deviceIds);

        private List<UInt64> m_DeviceIdsCache;
    }
}
