// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Attribute used to indicate which types of <see cref="ContextNode"/> can contain a given <see cref="BlockNode"/> type.
    /// </summary>
    /// <remarks>
    /// Apply this attribute to a class derived from <see cref="BlockNode"/> to declare which <see cref="ContextNode"/> types support it.
    /// This enables associations between block nodes and their compatible context nodes. Use it to validate and filter the 
    /// available blocks for specific context nodes.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UseWithContextAttribute : Attribute
    {
        internal Type[] contextTypes { get; }

        /// <summary>
        /// Determines whether the specified context node type supports the block node decorated with this attribute.
        /// </summary>
        /// <param name="contextType">The type of the context node to check against.</param>
        /// <returns><c>true</c> if the context node type is supported; otherwise, <c>false</c>.</returns>
        public bool IsContextTypeSupported(Type contextType)
        {
            foreach (var type in contextTypes)
            {
                if (type.IsAssignableFrom(contextType))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UseWithContextAttribute"/> class with the specified context node types.
        /// </summary>
        /// <param name="contextTypes">An array of context node types that can contain the decorated block node.</param>
        public UseWithContextAttribute(params Type[] contextTypes)
        {
            this.contextTypes = contextTypes;
        }
    }
}
