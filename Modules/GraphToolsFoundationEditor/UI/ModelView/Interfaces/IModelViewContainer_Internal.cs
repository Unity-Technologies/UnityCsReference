// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Describe a object, usually a <see cref="ModelView"/>, that contains other <see cref="ModelView"/>.
    /// </summary>
    interface IModelViewContainer_Internal
    {
        /// <summary>
        /// Returns the first level of included <see cref="ModelView"/> in this element.
        /// </summary>
        IEnumerable<ModelView> ModelViews { get; }
    }
}
