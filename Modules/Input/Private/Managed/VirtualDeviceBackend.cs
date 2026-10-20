// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngineInternal.Input
{
    /// <summary>
    /// Operations that apply to every virtual device at once. To drive one device, use <see cref="VirtualDevice"/>.
    /// </summary>
    internal static unsafe class VirtualDeviceBackend
    {
        // Covers every valid state, which native caps below kMaxInputEventSizeInBytes, and typical descriptors.
        internal const int k_StackBudget = 2048;

        internal static long Dispatch(int code, void* payload, int size)
        {
            return NativeInputSystem.IOCTL(NativeInputCapabilities.systemDeviceId, code, new IntPtr(payload), size);
        }

        // In long, so a tail near int.MaxValue is refused here rather than wrapping negative.
        internal static bool TryGetPayloadSize(int headerSize, long tailSize, out int size)
        {
            var total = headerSize + tailSize;
            size = total > int.MaxValue ? 0 : (int)total;
            return total <= int.MaxValue;
        }

        /// <summary>
        /// Returns whether this build supports virtual devices.
        /// </summary>
        public static bool IsSupported()
        {
            var payload = new InputCapabilityStatePayload { state = CapabilityState.Unknown };
            var result = Dispatch(
                NativeInputCapabilities.queryVirtualDevicesSupported,
                &payload,
                sizeof(InputCapabilityStatePayload));

            return result >= 0 && payload.state == CapabilityState.Supported;
        }

        /// <summary>
        /// Destroys every virtual device, including connected ones. Devices still held become unusable.
        /// </summary>
        public static long DestroyAll()
        {
            return Dispatch(NativeInputVirtualDevices.destroyAll, null, 0);
        }

        /// <summary>
        /// Lists the virtual devices in creation order, filling as many as <paramref name="records"/> holds.
        /// </summary>
        /// <param name="count">Records written.</param>
        /// <param name="total">Devices that exist, however many fit.</param>
        /// <returns>
        /// <see cref="InputCommandResult.success"/> when every device fit in <paramref name="records"/>, otherwise
        /// <see cref="InputCommandResult.bufferTooSmall"/>.
        /// </returns>
        public static long Enumerate(Span<VirtualDeviceRecord> records, out int count, out int total)
        {
            count = 0;
            total = 0;

            var headerSize = sizeof(VirtualDeviceEnumeratePayload);
            if (!TryGetPayloadSize(headerSize, (long)records.Length * sizeof(VirtualDeviceRecord), out var size))
                return InputCommandResult.invalidSize;

            Span<byte> buffer = size <= k_StackBudget ? stackalloc byte[size] : new byte[size];

            fixed (byte* bytes = buffer)
            {
                var header = (VirtualDeviceEnumeratePayload*)bytes;
                *header = new VirtualDeviceEnumeratePayload { capacity = (uint)records.Length };

                var result = Dispatch(NativeInputVirtualDevices.enumerate, bytes, size);

                total = (int)header->total;
                count = Math.Min((int)header->count, records.Length);
                new ReadOnlySpan<VirtualDeviceRecord>(bytes + headerSize, count).CopyTo(records);
                return result;
            }
        }

        /// <summary>
        /// Returns how many virtual devices exist, connected or not.
        /// </summary>
        public static int Count()
        {
            Enumerate(Span<VirtualDeviceRecord>.Empty, out _, out var total);
            return total;
        }
    }
}
