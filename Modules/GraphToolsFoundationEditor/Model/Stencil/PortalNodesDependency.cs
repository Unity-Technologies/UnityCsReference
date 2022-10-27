// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Represents a dependency between two nodes linked together by portal pair.
    /// </summary>
    class PortalNodesDependency : IDependency
    {
        /// <inheritdoc />
        public AbstractNodeModel DependentNode { get; set; }
    }
}
