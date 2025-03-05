// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
namespace UnityEngine.InputForUI;

/// <summary>
/// Beware values determinate order of some events (pointer events).
/// </summary>
[VisibleToOtherModules("UnityEngine.UIElementsModule")]
internal enum EventSource
{
    /// <summary>
    /// Unspecified source, can be any device or even an event generated/simulated by code.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Event was generated from a keyboard interaction.
    /// </summary>
    Keyboard = 1,

    /// <summary>
    /// Event was generated from a gamepad interaction.
    /// </summary>
    Gamepad = 2,

    /// <summary>
    /// Event was generated from a mouse interaction.
    /// </summary>
    Mouse = 3,

    /// <summary>
    /// Event was generated from a pen interaction.
    /// </summary>
    Pen = 4,

    /// <summary>
    /// Event was generated from a touch interaction.
    /// </summary>
    Touch = 5,

    /// <summary>
    /// Event was generated from a tracked device interaction.
    /// See for example InputSystem's [[https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.XR.XRController.html|XRController]].
    /// </summary>
    TrackedDevice = 6,
}
