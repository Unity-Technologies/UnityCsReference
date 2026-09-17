// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngineInternal.Input
{
    /// <summary>
    /// Flags indicating various focus states for the application and editor.
    /// </summary>
    internal enum FocusFlags : ushort
    {
        /// <summary>
        /// No focus state is active.
        /// </summary>
        None = 0,

        /// <summary>
        /// The application has focus.
        /// </summary>
        ApplicationFocus = (1 << 0)
    };

    /// <summary>
    /// Whether the platform is capable of delivering a given kind of input.
    /// </summary>
    /// <remarks>
    /// Mirrors CapabilityState in Modules/Input/InputDeviceIOCTL.h, whose wire values are pinned by
    /// tests on both sides. The value space is open and may gain values, so treat an unrecognised
    /// value as negative rather than as an error.
    /// </remarks>
    internal enum CapabilityState : byte
    {
        /// <summary>
        /// The platform cannot determine the answer, or does not implement the query yet.
        /// </summary>
        /// <remarks>
        /// Zero so that an unwritten payload reads as indeterminate rather than as a confident
        /// <see cref="NotSupported"/>.
        /// </remarks>
        Unknown = 0,

        /// <summary>
        /// The platform definitively cannot deliver it. A platform merely unsure answers
        /// <see cref="Unknown"/> instead.
        /// </summary>
        NotSupported = 1,

        /// <summary>
        /// The platform can deliver it.
        /// </summary>
        Supported = 2
    }

    /// <summary>
    /// Payload for every system capability query.
    /// </summary>
    /// <remarks>
    /// Mirrors IOCTLCapabilityState in Modules/Input/InputDeviceIOCTL.h, which static_asserts
    /// this size. Nothing generates this mirror, so the two must be changed together.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = 1)]
    internal struct InputCapabilityStatePayload
    {
        /// <summary>
        /// The answer written by the system endpoint.
        /// </summary>
        [FieldOffset(0)] public CapabilityState state;
    }

    /// <summary>
    /// Addressing and codes for system-scoped IOCTLs, those answered by the system endpoint
    /// rather than by a device.
    /// </summary>
    /// <remarks>
    /// Keep in sync with kSystemInputDeviceId in Modules/Input/InputDeviceData.h and the system
    /// capability codes in Modules/Input/InputFourCC.h.
    /// </remarks>
    internal static class NativeInputCapabilities
    {
        /// <summary>
        /// Device id addressing the system endpoint, which is never registered as a device and so
        /// never appears in a device list.
        /// </summary>
        public const int systemDeviceId = 0xffff;

        /// <summary>
        /// 'QPEN'. Whether the platform can deliver pen input at all.
        /// </summary>
        public const int queryPenSupported = ('Q' << 24) | ('P' << 16) | ('E' << 8) | 'N';

        /// <summary>
        /// 'QMOU'. Whether the platform can deliver mouse input at all.
        /// </summary>
        public const int queryMouseSupported = ('Q' << 24) | ('M' << 16) | ('O' << 8) | 'U';

        /// <summary>
        /// 'QTPS'. Whether the platform can deliver a real pressure value with touch input, as
        /// opposed to the constant 1.0f reported by platforms without pressure.
        /// </summary>
        public const int queryTouchPressureSupported = ('Q' << 24) | ('T' << 16) | ('P' << 8) | 'S';
    }
}
