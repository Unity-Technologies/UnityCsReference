// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// What part of the element is used as the reference for snapping.
    /// </summary>
    enum SnapReference_Internal
    {
        /// <summary>
        /// Snap the left wire of the element.
        /// </summary>
        LeftWire,

        /// <summary>
        /// Snap the horizontal center of the element.
        /// </summary>
        HorizontalCenter,

        /// <summary>
        /// Snap the right wire of the element.
        /// </summary>
        RightWire,

        /// <summary>
        /// Snap the top wire of the element.
        /// </summary>
        TopWire,

        /// <summary>
        /// Snap the vertical center of the element.
        /// </summary>
        VerticalCenter,

        /// <summary>
        /// Snap the bottom wire of the element.
        /// </summary>
        BottomWire
    }
}
