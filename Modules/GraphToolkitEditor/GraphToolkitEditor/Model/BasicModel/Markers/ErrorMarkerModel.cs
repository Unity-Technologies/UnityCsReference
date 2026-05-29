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

        /// <summary>
        /// Object to pass to <see cref="Action"/> when it is invoked.
        /// If <see cref="UserData"/> is null, the <see cref="ErrorMarker"/> tries to get its model to pass as an argument.
        /// </summary>
        public abstract object UserData { get; }

        /// <summary>
        /// The <see cref="GraphLogAction"/> for the error.
        /// </summary>
        public abstract GraphLogAction Action { get; }
    }
}
