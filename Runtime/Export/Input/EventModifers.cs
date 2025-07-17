// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    /// <summary>
    /// Types of modifier key that can be active during a keystroke event.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, @"UnityEngine.IMUGI", @"UnityEngine")]
    [Flags]
    public enum EventModifiers
    {
        /// <summary>
        /// No modifier keys are pressed.
        /// </summary>
        None = 0,

        /// <summary>
        /// The Shift key is pressed.
        /// </summary>
        Shift = 1,

        /// <summary>
        /// The Control key is pressed.
        /// </summary>
        Control = 2,

        /// <summary>
        /// The Alt key is pressed.
        /// </summary>
        Alt = 4,

        /// <summary>
        /// The Meta key (Windows key on Windows, Command key on Mac) is pressed.
        /// </summary>
        Command = 8,

        /// <summary>
        /// The Num Lock key is pressed.
        /// </summary>
        Numeric = 16,

        /// <summary>
        /// The caps lock key is pressed
        /// </summary>
        CapsLock = 32,

        /// <summary>
        /// The Function key (F1-F15) is pressed.
        /// </summary>
        FunctionKey = 64
    }
}
