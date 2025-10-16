// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Link a <see cref="GraphViewEditorWindow"/> to a <see cref="GraphObject"/> type. This can be added on classes derived from <see cref="GraphViewEditorWindow"/> only.
    /// </summary>
    /// <remarks>A given <see cref="GraphViewEditorWindow"/> can be linked to multiple <see cref="GraphObject"/> types. A <see cref="GraphObject"/> can only be linked to a single <see cref="GraphViewEditorWindow"/> type.</remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    [UnityRestricted]
    internal class GraphEditorWindowDefinitionAttribute : Attribute
    {
        /// <summary>
        /// The <see cref="GraphObject"/> type.
        /// </summary>
        public Type GraphObjectType { get; }

        public GraphEditorWindowDefinitionAttribute(Type graphObjectType)
        {
            GraphObjectType = graphObjectType;
        }
    }
}
