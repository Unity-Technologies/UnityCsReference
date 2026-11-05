// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.LowLevel;

namespace UnityEngineInternal.Input
{
    /// <summary>
    /// Operations that apply to every virtual device at once. To drive one device, use <see cref="VirtualDevice"/>.
    /// </summary>
    internal static unsafe partial class VirtualDeviceBackend
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
        /// Hands every event that deferred devices hold to the input module, as a platform tick would.
        /// </summary>
        /// <remarks>
        /// Each deferred device holds a bounded number of events and merges or refuses what arrives once it is full,
        /// so call it on the main thread, outside a consumer's update, at least once per frame. It sends each device's
        /// events in publish order, merged across devices by their time.
        /// </remarks>
        /// <returns>
        /// The number of events the input module accepted. Read it as a count rather than against
        /// <see cref="InputCommandResult.success"/>, which is also 1. <see cref="InputCommandResult.invalidState"/>
        /// off the main thread or inside a consumer's update, which releases nothing.
        /// <see cref="InputCommandResult.notSupported"/> when this build has no virtual devices.
        /// </returns>
        public static long ReleaseDeferred()
        {
            return Dispatch(NativeInputVirtualDevices.releaseDeferred, null, 0);
        }

        /// <summary>
        /// Releases what deferred devices hold once per frame, as a platform tick would, until disposed.
        /// </summary>
        /// <remarks>
        /// Adds a player loop system ahead of the rest of <see cref="UnityEngine.PlayerLoop.Initialization"/>, which on
        /// every platform runs after the platform tick and the frame counter advance, and before any input update.
        /// So <see cref="UnityEngine.Time.frameCount"/> in <paramref name="onReleased"/> reads the frame whose input
        /// update follows. On platforms that tick before the wait for the last presentation, state set from another
        /// thread during that wait is released one frame earlier than real input would be. Call on the main thread.
        /// Runs in players and in Play mode.
        /// </remarks>
        /// <param name="onReleased">Called with the result of each <see cref="ReleaseDeferred"/>.</param>
        /// <returns>The scope. Disposing it removes only its own system from the player loop.</returns>
        /// <exception cref="InvalidOperationException">
        /// A scope is already active, since two releases a frame would split one tick's batch, or the player loop has
        /// no Initialization phase.
        /// </exception>
        public static IDisposable ReleaseDeferredEachFrame(Action<long> onReleased = null)
        {
            return ReleaseEachFrameScope.Install(onReleased);
        }

        sealed partial class ReleaseEachFrameScope : IDisposable
        {
            // Names the system in the player loop and the profiler.
            struct ReleaseDeferredSystem
            {
            }

            // Disposed and cleared on code reload, which also resets the player loop.
            [AutoStaticsCleanupOnCodeReload]
            static ReleaseEachFrameScope s_Active;

            readonly Action<long> m_OnReleased;

            ReleaseEachFrameScope(Action<long> onReleased)
            {
                m_OnReleased = onReleased;
            }

            public static ReleaseEachFrameScope Install(Action<long> onReleased)
            {
                if (s_Active != null)
                    throw new InvalidOperationException(
                        "Deferred devices are already released each frame. Dispose the earlier scope first.");

                var scope = new ReleaseEachFrameScope(onReleased);
                var system = new PlayerLoopSystem
                {
                    type = typeof(ReleaseDeferredSystem),
                    updateDelegate = scope.Release,
                };
                if (!TryEditInitialization(systems => Prepend(system, systems)))
                    throw new InvalidOperationException("The player loop has no Initialization phase.");

                s_Active = scope;
                return scope;
            }

            // An earlier scope's Dispose must not remove a later scope's system.
            public void Dispose()
            {
                if (s_Active != this)
                    return;

                s_Active = null;
                TryEditInitialization(
                    systems => Array.FindAll(systems, system => system.type != typeof(ReleaseDeferredSystem)));
            }

            void Release()
            {
                var result = ReleaseDeferred();
                m_OnReleased?.Invoke(result);
            }

            static PlayerLoopSystem[] Prepend(PlayerLoopSystem system, PlayerLoopSystem[] systems)
            {
                var edited = new PlayerLoopSystem[systems.Length + 1];
                edited[0] = system;
                Array.Copy(systems, 0, edited, 1, systems.Length);
                return edited;
            }

            static bool TryEditInitialization(Func<PlayerLoopSystem[], PlayerLoopSystem[]> edit)
            {
                var loop = PlayerLoop.GetCurrentPlayerLoop();
                var phases = loop.subSystemList ?? Array.Empty<PlayerLoopSystem>();
                for (var i = 0; i < phases.Length; ++i)
                {
                    if (phases[i].type != typeof(UnityEngine.PlayerLoop.Initialization))
                        continue;

                    phases[i].subSystemList = edit(phases[i].subSystemList ?? Array.Empty<PlayerLoopSystem>());
                    PlayerLoop.SetPlayerLoop(loop);
                    return true;
                }

                return false;
            }
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
