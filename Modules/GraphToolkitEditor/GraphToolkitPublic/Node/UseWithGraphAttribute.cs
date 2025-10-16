// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Attribute used to specify which <see cref="Graph"/> types are compatible with the decorated <see cref="Node"/> class.
    /// </summary>
    /// <remarks>
    /// This attribute links a specific <see cref="Node"/> class to one or more <see cref="Graph"/> types, enabling fine-grained control
    /// over which graph types support the node. This allows framework authors to explicitly declare node compatibility across different
    /// kinds of graphs and ensures that only valid nodes are available for use in each graph context.
    /// <br/>
    /// <br/>
    /// By default, nodes defined in the same assembly as the graph are considered compatible and available.
    /// In this default setup, the <see cref="UseWithGraphAttribute"/> is not required.
    /// However, when a graph uses <see cref="GraphOptions.DisableAutoInclusionOfNodesFromGraphAssembly "/>, this attribute must be used to declare which <see cref="Graph"/> types support the node.
    /// <br/>
    /// <br/>
    /// This attribute affects editor behaviors such as graph item library population and helps prevent the accidental use of unsupported nodes.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class UseWithGraphAttribute : Attribute
    {
        Type[] graphTypes { get; }

        /// <summary>
        /// Determines whether the specified graph type supports the node decorated with this attribute.
        /// </summary>
        /// <param name="graphType">The type of the graph to validate.</param>
        /// <returns><c>true</c> if the graph type supports the node; otherwise, <c>false</c>.</returns>
        public bool IsGraphTypeSupported(Type graphType)
        {
            foreach (var type in graphTypes)
            {
                if (type.IsAssignableFrom(graphType))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UseWithGraphAttribute"/> class with the specified supported graph types.
        /// </summary>
        /// <param name="graphTypes">An array of graph types that support the decorated node type.</param>
        public UseWithGraphAttribute(params Type[] graphTypes)
        {
            this.graphTypes = graphTypes;
        }
    }
}
