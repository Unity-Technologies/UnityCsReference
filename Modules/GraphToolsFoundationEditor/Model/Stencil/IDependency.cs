// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Interface that represents a dependency.
    /// </summary>
    interface IDependency
    {
        /// <summary>
        /// The dependant node in the dependency.
        /// </summary>
        AbstractNodeModel DependentNode { get; }
    }
}
