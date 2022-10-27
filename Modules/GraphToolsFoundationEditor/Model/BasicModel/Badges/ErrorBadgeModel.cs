// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A model to hold error messages to be displayed by badges.
    /// </summary>
    [Serializable]
    abstract class ErrorBadgeModel : BadgeModel
    {
        public abstract LogType ErrorType { get; }

        /// <summary>
        /// The error message to display.
        /// </summary>
        public abstract string ErrorMessage { get; }
    }
}
