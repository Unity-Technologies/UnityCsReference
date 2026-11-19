// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngineInternal.Input
{
    /// <summary>
    /// A device configuration change event, as the pipeline emits it for a reconfigured device.
    /// </summary>
    /// <remarks>
    /// An event exactly <see cref="NativeInputEvent.structSize"/> long comes from the legacy path, which doesn't
    /// say what changed, so any part may have. Read the parts with <see cref="PartsOf"/>, which handles both.
    /// Don't cast an event to this type without checking its size, since an event from the legacy path is shorter.
    /// </remarks>
    // Keep in sync with DeviceConfigurationChangedInputEventData in Modules/Input/InputEventData.h.
    [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
    internal struct InputDeviceConfigurationChangedEvent
    {
        /// <summary>
        /// The native input event type identifier for configuration change events (DCFG).
        /// </summary>
        public const NativeInputEventType Type = NativeInputEventType.DeviceConfigChanged;

        /// <summary>
        /// The size of an event that names its parts, in bytes.
        /// </summary>
        public const int structSize = NativeInputEvent.structSize + sizeof(uint);

        /// <summary>
        /// The underlying native input event structure.
        /// </summary>
        [FieldOffset(0)]
        public NativeInputEvent baseEvent;

        /// <summary>
        /// The parts of the device's configuration that changed.
        /// </summary>
        [FieldOffset(NativeInputEvent.structSize)]
        public Reconfiguration changed;

        /// <summary>
        /// Reads the parts a configuration change event names.
        /// </summary>
        /// <param name="inputEvent">An event from the input event buffer.</param>
        /// <returns>
        /// The parts. Every known part for an event from the legacy path, which doesn't say what changed, and none
        /// for an event of another type.
        /// </returns>
        public static unsafe Reconfiguration PartsOf(NativeInputEvent* inputEvent)
        {
            if (inputEvent->type != Type)
                return default;
            if (inputEvent->sizeInBytes < structSize)
                return Reconfigurations.known;

            return ((InputDeviceConfigurationChangedEvent*)inputEvent)->changed;
        }
    }
}
