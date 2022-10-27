// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Interface to be implemented by a <see cref="AbstractNodeModel"/> when the local inspector should display some
    /// other object instead of the node.
    /// </summary>
    interface IHasInspectorSurrogate
    {
        object Surrogate { get; }
    }
}
