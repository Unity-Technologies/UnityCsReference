// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A model to hold error messages to be displayed by markers.
    /// </summary>
    [UnityRestricted]
    internal abstract class ErrorMarkerModel : MarkerModel
    {
        /// <summary>
        /// The error type.
        /// </summary>
        /// <remarks>Setter implementations should set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public abstract LogType ErrorType { get; }

        /// <summary>
        /// The error message to display.
        /// </summary>
        /// <remarks>Setter implementations should set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public abstract string ErrorMessage { get; }
    }
}
