// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;

namespace UnityEngineInternal.Input
{
    /// <summary>
    /// A software-defined input device that reports the state you give it.
    /// </summary>
    /// <remarks>
    /// Create builds the device disconnected, so consumers can't see it. <see cref="Connect"/> registers it
    /// with the input module under a fresh <see cref="deviceId"/>, and <see cref="Disconnect"/> deregisters it,
    /// as plugging and unplugging a real device would. <see cref="Dispose"/> destroys it. Available when
    /// <see cref="VirtualDeviceBackend.IsSupported"/> returns true. Every command returns an
    /// <see cref="InputCommandResult"/>.
    /// </remarks>
    internal unsafe class VirtualDevice : IDisposable
    {
        /// <summary>
        /// Opaque identity for the device's lifetime, unchanged across connections. Never a device id.
        /// </summary>
        public int handle { get; private set; }

        /// <summary>
        /// The live device id while connected, otherwise zero. Each Connect assigns a new one.
        /// </summary>
        /// <remarks>
        /// Tracks this object's own calls, so <see cref="VirtualDeviceBackend.DestroyAll"/> leaves it stale.
        /// </remarks>
        public int deviceId { get; private set; }

        /// <summary>
        /// True between a successful <see cref="Connect"/> and the next <see cref="Disconnect"/>.
        /// </summary>
        public bool isConnected => deviceId != 0;

        /// <summary>
        /// Length in bytes of the device's state, fixed at Create.
        /// </summary>
        public int stateSize { get; }

        private protected VirtualDevice(int handle, int stateSize)
        {
            this.handle = handle;
            this.stateSize = stateSize;
        }

        /// <summary>
        /// Creates a virtual device without connecting it to the system.
        /// </summary>
        /// <param name="descriptor">JSON device description. Cannot be empty.</param>
        /// <param name="stateFourCC">Format of the state events the device publishes.</param>
        /// <param name="flags">Behaviour, fixed for the device's lifetime.</param>
        /// <param name="initialState">Starting state, whose length fixes <see cref="stateSize"/>.</param>
        /// <exception cref="InvalidOperationException">The device couldn't be created. The message names the reason.</exception>
        public static VirtualDevice Create(
            string descriptor,
            uint stateFourCC,
            VirtualDeviceFlags flags,
            ReadOnlySpan<byte> initialState)
        {
            var result = TryCreate(descriptor, stateFourCC, flags, initialState, out var device);
            if (device == null)
                throw new InvalidOperationException("Could not create virtual device: " + InputCommandResult.ToName(result));

            return device;
        }

        /// <summary>
        /// Attempts to create a virtual device without connecting it to the system.
        /// </summary>
        /// <param name="device">The new device, or <c>null</c> on failure.</param>
        /// <returns>
        /// <see cref="InputCommandResult.success"/>, or on failure:
        /// <see cref="InputCommandResult.invalidPayload"/> when the descriptor is null or empty, or the initial
        /// state is empty or larger than the maximum state size;
        /// <see cref="InputCommandResult.invalidSize"/> when the descriptor and state don't fit one payload;
        /// <see cref="InputCommandResult.failure"/> when no device ids are left;
        /// <see cref="InputCommandResult.notSupported"/> when this build has no virtual devices.
        /// </returns>
        public static long TryCreate(
            string descriptor,
            uint stateFourCC,
            VirtualDeviceFlags flags,
            ReadOnlySpan<byte> initialState,
            out VirtualDevice device)
        {
            var result = TryCreateHandle(descriptor, stateFourCC, flags, initialState, out var handle);
            device = result >= 0 ? new VirtualDevice(handle, initialState.Length) : null;
            return result;
        }

        private protected static long TryCreateHandle(
            string descriptor,
            uint stateFourCC,
            VirtualDeviceFlags flags,
            ReadOnlySpan<byte> initialState,
            out int handle)
        {
            handle = 0;

            if (descriptor == null)
                return InputCommandResult.invalidPayload;

            var headerSize = sizeof(VirtualDeviceCreatePayload);
            var descriptorSize = Encoding.UTF8.GetByteCount(descriptor);
            var tailSize = (long)descriptorSize + initialState.Length;
            if (!VirtualDeviceBackend.TryGetPayloadSize(headerSize, tailSize, out var size))
                return InputCommandResult.invalidSize;

            Span<byte> buffer = size <= VirtualDeviceBackend.k_StackBudget ? stackalloc byte[size] : new byte[size];

            Encoding.UTF8.GetBytes(descriptor, buffer.Slice(headerSize, descriptorSize));
            initialState.CopyTo(buffer.Slice(headerSize + descriptorSize));

            fixed (byte* bytes = buffer)
            {
                var header = (VirtualDeviceCreatePayload*)bytes;
                *header = new VirtualDeviceCreatePayload
                {
                    stateFourCC = stateFourCC,
                    flags = flags,
                    descriptorOffset = (uint)headerSize,
                    descriptorSize = (uint)descriptorSize,
                    stateOffset = (uint)(headerSize + descriptorSize),
                    stateSize = (uint)initialState.Length,
                };

                var result = VirtualDeviceBackend.Dispatch(NativeInputVirtualDevices.create, bytes, size);
                handle = header->outHandle;
                return result;
            }
        }

        /// <summary>
        /// Makes the device visible to input consumers under a fresh <see cref="deviceId"/>.
        /// </summary>
        public long Connect()
        {
            var payload = new VirtualDeviceConnectPayload { handle = handle };
            var result = VirtualDeviceBackend.Dispatch(
                NativeInputVirtualDevices.connect,
                &payload,
                sizeof(VirtualDeviceConnectPayload));

            if (result >= 0)
                deviceId = payload.outDeviceId;

            return result;
        }

        /// <summary>
        /// Disconnects the device from the input module, as unplugging a real device would. It keeps its handle
        /// and state.
        /// </summary>
        public long Disconnect()
        {
            var result = DispatchHandleOnly(NativeInputVirtualDevices.disconnect);

            // Both refusals mean native no longer has it connected, whoever disconnected it.
            if (result >= 0 ||
                result == InputCommandResult.invalidHandle ||
                result == InputCommandResult.invalidState)
            {
                deviceId = 0;
            }

            return result;
        }

        /// <summary>
        /// Tells consumers to re-query what they cached about the device. Requires a connected device.
        /// </summary>
        public long Reconfigure()
        {
            return DispatchHandleOnly(NativeInputVirtualDevices.reconfigure);
        }

        /// <summary>
        /// Replaces the device's state, publishing it at once when the device is event-driven and connected.
        /// </summary>
        /// <param name="state">Exactly <see cref="stateSize"/> bytes.</param>
        public long SetState(ReadOnlySpan<byte> state)
        {
            var headerSize = sizeof(VirtualDeviceStatePayload);
            if (!VirtualDeviceBackend.TryGetPayloadSize(headerSize, state.Length, out var size))
                return InputCommandResult.invalidSize;

            Span<byte> buffer = size <= VirtualDeviceBackend.k_StackBudget ? stackalloc byte[size] : new byte[size];
            state.CopyTo(buffer.Slice(headerSize));

            fixed (byte* bytes = buffer)
            {
                var header = (VirtualDeviceStatePayload*)bytes;
                header->handle = handle;
                header->stateSizeBytes = (uint)state.Length;

                return VirtualDeviceBackend.Dispatch(NativeInputVirtualDevices.setState, bytes, size);
            }
        }

        /// <summary>
        /// Copies the device's state into the first <see cref="stateSize"/> bytes of <paramref name="state"/>.
        /// </summary>
        /// <returns>
        /// <see cref="InputCommandResult.bufferTooSmall"/> when <paramref name="state"/> is shorter than
        /// <see cref="stateSize"/>.
        /// </returns>
        public long GetState(Span<byte> state)
        {
            var headerSize = sizeof(VirtualDeviceStatePayload);
            if (!VirtualDeviceBackend.TryGetPayloadSize(headerSize, state.Length, out var size))
                return InputCommandResult.invalidSize;

            Span<byte> buffer = size <= VirtualDeviceBackend.k_StackBudget ? stackalloc byte[size] : new byte[size];

            long result;
            fixed (byte* bytes = buffer)
            {
                var header = (VirtualDeviceStatePayload*)bytes;
                header->handle = handle;
                header->stateSizeBytes = (uint)state.Length;

                result = VirtualDeviceBackend.Dispatch(NativeInputVirtualDevices.getState, bytes, size);
            }

            // Native writes only stateSize bytes, so the rest of a larger buffer is uninitialised.
            if (result >= 0)
                buffer.Slice(headerSize, stateSize).CopyTo(state);

            return result;
        }

        /// <summary>
        /// Publishes the device's current state now. Requires a connected, enabled device.
        /// </summary>
        public long PollNow()
        {
            return DispatchHandleOnly(NativeInputVirtualDevices.pollNow);
        }

        /// <summary>
        /// Reads the device's counters.
        /// </summary>
        public long QueryTelemetry(out VirtualDeviceTelemetryPayload telemetry)
        {
            var payload = new VirtualDeviceTelemetryPayload { handle = handle };
            var result = VirtualDeviceBackend.Dispatch(
                NativeInputVirtualDevices.queryTelemetry,
                &payload,
                sizeof(VirtualDeviceTelemetryPayload));

            telemetry = result >= 0 ? payload : default;
            return result;
        }

        /// <summary>
        /// Destroys the device, disconnecting it first. Later calls do nothing.
        /// </summary>
        public void Dispose()
        {
            if (handle == 0)
                return;

            DispatchHandleOnly(NativeInputVirtualDevices.destroy);
            handle = 0;
            deviceId = 0;
        }

        long DispatchHandleOnly(int code)
        {
            var payload = new VirtualDeviceHandleOnlyPayload { handle = handle };
            return VirtualDeviceBackend.Dispatch(code, &payload, sizeof(VirtualDeviceHandleOnlyPayload));
        }
    }

    /// <summary>
    /// A <see cref="VirtualDevice"/> whose state is sized based on <typeparamref name="TState"/>.
    /// </summary>
    internal sealed unsafe class VirtualDevice<TState> : VirtualDevice where TState : unmanaged
    {
        VirtualDevice(int handle)
            : base(handle, sizeof(TState))
        {
        }

        /// <summary>
        /// Creates a virtual device without connecting it to the system.
        /// </summary>
        /// <exception cref="InvalidOperationException">The device couldn't be created. The message names the reason.</exception>
        public static VirtualDevice<TState> Create(
            string descriptor,
            uint stateFourCC,
            VirtualDeviceFlags flags,
            TState initialState)
        {
            var result = TryCreate(descriptor, stateFourCC, flags, initialState, out var device);
            if (device == null)
                throw new InvalidOperationException("Could not create virtual device: " + InputCommandResult.ToName(result));

            return device;
        }

        /// <summary>
        /// Attempts to create a virtual device without connecting it to the system.
        /// </summary>
        /// <param name="device">The new device, or <c>null</c> on failure.</param>
        /// <returns>
        /// <see cref="InputCommandResult.success"/>, or on failure:
        /// <see cref="InputCommandResult.invalidPayload"/> when the descriptor is null or empty, or the initial
        /// state is empty or larger than the maximum state size;
        /// <see cref="InputCommandResult.invalidSize"/> when the descriptor and state don't fit one payload;
        /// <see cref="InputCommandResult.failure"/> when no device ids are left;
        /// <see cref="InputCommandResult.notSupported"/> when this build has no virtual devices.
        /// </returns>
        public static long TryCreate(
            string descriptor,
            uint stateFourCC,
            VirtualDeviceFlags flags,
            TState initialState,
            out VirtualDevice<TState> device)
        {
            var initialBytes = new ReadOnlySpan<byte>(&initialState, sizeof(TState));
            var result = TryCreateHandle(descriptor, stateFourCC, flags, initialBytes, out var handle);
            device = result >= 0 ? new VirtualDevice<TState>(handle) : null;
            return result;
        }

        /// <summary>
        /// Replaces the device's state, publishing it at once when the device is event-driven and connected.
        /// </summary>
        public long SetState(TState state)
        {
            return SetState(new ReadOnlySpan<byte>(&state, sizeof(TState)));
        }

        /// <summary>
        /// Reads the device's current state.
        /// </summary>
        /// <param name="state">The device's state, or default on failure.</param>
        public long GetState(out TState state)
        {
            TState read = default;
            var result = GetState(new Span<byte>(&read, sizeof(TState)));
            state = read;
            return result;
        }
    }
}
