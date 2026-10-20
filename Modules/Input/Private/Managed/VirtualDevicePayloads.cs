// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngineInternal.Input
{
    /// <summary>
    /// Behaviour a virtual device is created with, fixed for its lifetime.
    /// </summary>
    [Flags]
    internal enum VirtualDeviceFlags : uint
    {
        None = 0,

        /// <summary>The polling thread samples the device, rather than SetState publishing directly.</summary>
        Polled = 1 << 0,

        /// <summary>The answer the device gives to the run-in-background query.</summary>
        RunInBackground = 1 << 1,

        /// <summary>Answer <see cref="InputCommandResult.notSupported"/> to the reset IOCTL.</summary>
        RefuseReset = 1 << 2,

        /// <summary>Answer <see cref="InputCommandResult.notSupported"/> to the sync IOCTL.</summary>
        RefuseSync = 1 << 3,

        /// <summary>Answer <see cref="InputCommandResult.notSupported"/> to the query-enabled IOCTL.</summary>
        RefuseQueryEnabled = 1 << 4,

        /// <summary>Answer <see cref="InputCommandResult.notSupported"/> to the enable and disable IOCTLs.</summary>
        RefuseEnableDisable = 1 << 5,

        /// <summary>Answer <see cref="InputCommandResult.notSupported"/> to the run-in-background query.</summary>
        RefuseRunInBackground = 1 << 6,
    }

    /// <summary>
    /// Header for the create command, followed by the descriptor and initial state bytes.
    /// </summary>
    /// <remarks>
    /// These payloads mirror Modules/Input/VirtualDeviceIOCTL.h by hand, so the two change together.
    /// </remarks>
    internal struct VirtualDeviceCreatePayload
    {
        public uint stateFourCC;
        public VirtualDeviceFlags flags;
        public uint descriptorOffset;
        public uint descriptorSize;
        public uint stateOffset;
        public uint stateSize;
        public int outHandle;
    }

    /// <summary>
    /// Payload for the connect command.
    /// </summary>
    internal struct VirtualDeviceConnectPayload
    {
        public int handle;
        public int outDeviceId;
    }

    /// <summary>
    /// Payload for destroy, disconnect, reconfigure and poll-now.
    /// </summary>
    internal struct VirtualDeviceHandleOnlyPayload
    {
        public int handle;
    }

    /// <summary>
    /// Header for set-state and get-state, followed by the state bytes.
    /// </summary>
    internal struct VirtualDeviceStatePayload
    {
        public int handle;

        /// <summary>
        /// Set-state: bytes supplied. Get-state: capacity in, required size out on
        /// <see cref="InputCommandResult.bufferTooSmall"/>.
        /// </summary>
        public uint stateSizeBytes;
    }

    /// <summary>
    /// Per-device counters.
    /// </summary>
    internal struct VirtualDeviceTelemetryPayload
    {
        public int handle;
        public uint padding;

        /// <summary>State events handed to the event queue, which is not a delivery count (ISX-2497).</summary>
        public ulong eventsPublished;
        public ulong eventsDropped;

        /// <summary>State bytes published, excluding event headers.</summary>
        public ulong bytesPublished;
        public ulong timerPollCount;
        public ulong explicitPollCount;
        public ulong setCount;
        public ulong deviceIoctlCount;
    }

    /// <summary>
    /// One record in the enumerate reply.
    /// </summary>
    internal unsafe struct VirtualDeviceRecord
    {
        public int handle;

        /// <summary>Zero while disconnected.</summary>
        public int liveDeviceId;
        public uint stateFourCC;
        public VirtualDeviceFlags flags;

        /// <summary>The descriptor's product name, null-terminated.</summary>
        public fixed byte displayName[64];
    }

    /// <summary>
    /// Header for the enumerate command, followed by up to <see cref="capacity"/> records.
    /// </summary>
    internal struct VirtualDeviceEnumeratePayload
    {
        public uint capacity;
        public uint count;
        public uint total;
        public uint padding;
    }

    /// <summary>
    /// Virtual device command codes, all addressed to <see cref="NativeInputCapabilities.systemDeviceId"/>.
    /// </summary>
    internal static class NativeInputVirtualDevices
    {
        public const int create = ('V' << 24) | ('D' << 16) | ('C' << 8) | 'R';
        public const int destroy = ('V' << 24) | ('D' << 16) | ('D' << 8) | 'S';
        public const int destroyAll = ('V' << 24) | ('D' << 16) | ('D' << 8) | 'A';
        public const int connect = ('V' << 24) | ('D' << 16) | ('C' << 8) | 'N';
        public const int disconnect = ('V' << 24) | ('D' << 16) | ('D' << 8) | 'C';
        public const int reconfigure = ('V' << 24) | ('D' << 16) | ('R' << 8) | 'C';
        public const int setState = ('V' << 24) | ('D' << 16) | ('S' << 8) | 'T';
        public const int getState = ('V' << 24) | ('D' << 16) | ('G' << 8) | 'T';
        public const int pollNow = ('V' << 24) | ('D' << 16) | ('P' << 8) | 'N';
        public const int queryTelemetry = ('V' << 24) | ('D' << 16) | ('T' << 8) | 'M';
        public const int enumerate = ('V' << 24) | ('D' << 16) | ('E' << 8) | 'N';
    }
}
