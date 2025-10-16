// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface that represents a dependency.
    /// </summary>
    [UnityRestricted]
    internal interface IDependency
    {
        /// <summary>
        /// The dependant node in the dependency.
        /// </summary>
        AbstractNodeModel DependentNode { get; }
    }
}
