// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Attribute used to bind a toolbar element to a specific Graph type.
    /// </summary>
    /// <remarks>
    /// Apply this attribute to toolbar element classes to make them appear in the toolbar
    /// when editing the specified graph type. The element will also appear for types derived
    /// from the specified graph type.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class GraphToolbarElementAttribute : Attribute
    {
        /// <summary>
        /// The unique identifier for this toolbar element.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The graph type this toolbar element is associated with.
        /// </summary>
        public Type GraphType { get; }

        /// <summary>
        /// The display order of this toolbar element. Lower values appear first.
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Creates a new GraphToolbarElement attribute.
        /// </summary>
        /// <param name="id">The unique identifier for this toolbar element.</param>
        /// <param name="graphType">The graph type this element should appear for. Must inherit from Graph.</param>
        /// <param name="order">The display order (default 1000). Lower values appear first.</param>
        /// <exception cref="ArgumentException">Thrown if graphType does not inherit from Graph.</exception>
        public GraphToolbarElementAttribute(string id, Type graphType, int order = 1000)
        {
            if (!typeof(Graph).IsAssignableFrom(graphType))
            {
                throw new ArgumentException(
                    $"GraphType must be assignable to {nameof(Graph)}",
                    nameof(graphType));
            }

            Id = id;
            GraphType = graphType;
            Order = order;
        }
    }
}
