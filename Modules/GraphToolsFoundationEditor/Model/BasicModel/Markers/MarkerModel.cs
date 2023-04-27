// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Data to be displayed by a marker.
    /// </summary>
    abstract class MarkerModel : Model
    {
        /// <summary>
        /// The model to which the marker is attached.
        /// </summary>
        public abstract GraphElementModel GetParentModel(GraphModel graphModel);
    }
}
