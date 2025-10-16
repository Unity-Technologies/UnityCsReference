// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Attribute used to define a link between a subgraph type and a main graph type.
    /// </summary>
    /// <remarks>
    /// Apply this attribute to a custom <see cref="Graph"/> class to declare it as a valid subgraph type for a specific parent (main) graph type.
    /// This attribute is required when you want to designate a specific graph type to function as a subgraph in tools that support subgraphs.
    /// <br/>
    /// <br/>
    /// Use it on custom graph classes that are designed to act as subgraphs.
    /// This is useful when you want to provide specialized subgraph behaviors, customize the user experience, or restrict subgraph usage
    /// to certain graph types.
    /// <br/>
    /// <br/>
    /// When a graph type declares that it supports subgraphs using <see cref="GraphOptions.SupportsSubgraphs"/>
    /// but no corresponding <see cref="SubgraphAttribute"/> is found, the main graph type itself is used as the subgraph type by default.
    /// <br/>
    /// <br/>
    /// You can associate multiple subgraph types with the same main graph type. In this case, the editor's context menu includes multiple
    /// "Create <c>Subgraph class name</c> Subgraph from Selection" actions—one for each valid subgraph type.
    /// </remarks>
    /// <example>
    /// <code>
    /// // This declares a subgraph type used by MyMainGraph
    /// [Subgraph(typeof(MyMainGraph))]
    /// public class MySubgraph : Graph
    /// </code>
    /// </example>
    public sealed class SubgraphAttribute : Attribute
    {
        /// <summary>
        /// The type of the main <see cref="Graph"/> that supports this subgraph type.
        /// </summary>
        public Type MainGraphType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubgraphAttribute"/> class.
        /// </summary>
        /// <param name="mainGraphType">
        /// The type of the main <see cref="Graph"/> that this subgraph type is compatible with.
        /// This must be a type that inherits from <see cref="Graph"/>.
        /// </param>
        /// <remarks>
        /// Use this constructor to associate a custom subgraph class with a parent graph type. This enables the graph tool to recognize
        /// the subgraph as a valid option for the specified main graph. The attribute must be applied to a <see cref="Graph"/>-derived class
        /// intended to function as a subgraph.
        /// </remarks>
        /// <example>
        /// <code>
        /// // This declares a subgraph type used by MyMainGraph
        /// [Subgraph(typeof(MyMainGraph))]
        /// public class MySubgraph : Graph
        /// </code>
        /// </example>
        public SubgraphAttribute(Type mainGraphType)
        {
            MainGraphType = mainGraphType;
        }

    }
}
