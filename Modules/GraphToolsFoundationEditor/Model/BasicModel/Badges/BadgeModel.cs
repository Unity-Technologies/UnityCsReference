// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Data to be displayed by a badge.
    /// </summary>
    [Serializable]
    abstract class BadgeModel : GraphElementModel
    {
        /// <summary>
        /// The model to which the badge is attached.
        /// </summary>
        public abstract GraphElementModel ParentModel { get; }
    }
}
