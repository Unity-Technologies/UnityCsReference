// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Attribute to mark a field as being a node option, one that appear in the Node Options section
    /// of the model inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    class NodeOptionAttribute : Attribute
    {
    }
}
