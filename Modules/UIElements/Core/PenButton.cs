// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Describes a PenButton. Based on W3 conventions: https://www.w3.org/TR/pointerevents2/#the-buttons-property.
    /// </summary>
    public enum PenButton
    {
        /// <summary>
        /// The Pen is in Contact.
        /// </summary>
        PenContact = 0,
        /// <summary>
        /// The Pen Barrel Button.
        /// </summary>
        PenBarrel = 1,
        /// <summary>
        /// The Pen Eraser.
        /// </summary>
        PenEraser = 5
    }
}
