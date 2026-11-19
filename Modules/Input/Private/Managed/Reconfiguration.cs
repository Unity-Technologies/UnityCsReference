// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngineInternal.Input
{
    /// <summary>
    /// The parts of a device's configuration that a reconfiguration can change. Values combine.
    /// </summary>
    /// <remarks>
    /// What a part means for a device comes from its state type. Each state type lists the parts it
    /// defines, and a device reports only those. A state type can narrow a part to its own controls, but never use it
    /// for another layer.
    /// </remarks>
    // Keep in sync with Reconfiguration in Modules/Input/Reconfiguration.h.
    [Flags]
    internal enum Reconfiguration : uint
    {
        /// <summary>
        /// Which character and display name each control has, such as after a change of keyboard layout or input
        /// language. Each physical input drives the same control.
        /// </summary>
        CharacterMapping = 1 << 0,

        /// <summary>
        /// Which logical control each physical input drives, such as a controller held another way or its buttons
        /// remapped by the platform. The device has the same controls.
        /// </summary>
        ControlMapping = 1 << 1,

        /// <summary>How the device looks, such as its colours. No control, its input or its name changed.</summary>
        Appearance = 1 << 2,

        /// <summary>Which controls the device has, such as after a part was attached or detached.</summary>
        Composition = 1 << 3,

        /// <summary>
        /// Whether the device, or some of its controls, produce input, such as a pointer switched on or off. The
        /// device has the same controls.
        /// </summary>
        Availability = 1 << 4,
    }

    /// <summary>
    /// Constants for <see cref="Reconfiguration"/>.
    /// </summary>
    internal static class Reconfigurations
    {
        /// <summary>
        /// Every part that <see cref="Reconfiguration"/> defines.
        /// </summary>
        // Keep in sync with kKnownReconfigurations in Modules/Input/Reconfiguration.h.
        public const Reconfiguration known = Reconfiguration.CharacterMapping | Reconfiguration.ControlMapping
            | Reconfiguration.Appearance | Reconfiguration.Composition | Reconfiguration.Availability;
    }
}
