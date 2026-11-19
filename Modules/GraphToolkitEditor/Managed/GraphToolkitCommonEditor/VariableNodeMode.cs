// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Options for the mode of a <see cref="IVariableNode"/>, which determines the ports it exposes.
    /// </summary>
    /// <remarks>
    /// The mode controls how the node interacts with the variable's value and which ports are available on it.
    ///
    /// A <see cref="Get"/> node is the default mode for all variable kinds. For variables of kind
    /// <see cref="VariableKind.Local"/> or <see cref="VariableKind.Input"/>, it exposes an output port.
    /// For variables of kind <see cref="VariableKind.Output"/>, it exposes an input port.
    ///
    /// A <see cref="Set"/> node exposes an extra input port, so that you can assign the value directly in the graph.
    /// Only variables of kind <see cref="VariableKind.Local"/> support <see cref="Set"/> mode.
    /// Variables of kind <see cref="VariableKind.Input"/> or <see cref="VariableKind.Output"/> always use <see cref="Get"/> mode.
    ///
    /// To change the mode of an existing variable node, use the **Allow to set value in graph** checkbox in the node's inspector.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[
    /// // Create a local variable and add both a get node and a set node to the graph.
    /// IVariable speed = graph.CreateVariable<float>("Speed");
    ///
    /// graph.UndoBeginRecordGraph("Add Variable Nodes");
    /// IVariableNode getNode = graph.AddVariableNode(speed, new Vector2(100, 100));
    /// IVariableNode setNode = graph.AddVariableNode(speed, new Vector2(100, 200), VariableNodeMode.Set);
    /// graph.UndoEndRecordGraph();
    ///
    /// // getNode.Mode == VariableNodeMode.Get
    /// // Speed is a local variable, so getNode has an output port that provides its current value.
    ///
    /// // setNode.Mode == VariableNodeMode.Set
    /// // setNode has an extra input port for writing the value of Speed in the graph.
    /// ]]></code>
    /// </example>
    public enum VariableNodeMode
    {
        /// <summary>
        /// Determines the node's ports from the variable's kind. This is the default mode.```
        /// </summary>
        /// <remarks>
        /// This mode is valid for all variable kinds.
        /// The port topology depends on the variable's kind:
        ///
        /// For variables of kind <see cref="VariableKind.Local"/> or <see cref="VariableKind.Input"/>,
        /// the node exposes an output port that provides the variable's value to connected nodes.
        ///
        /// For variables of kind <see cref="VariableKind.Output"/>, the node exposes an input port.
        /// Connect a value to this port to specify what the subgraph returns to the parent graph for this variable.
        /// </remarks>
        Get,

        /// <summary>
        /// Exposes an extra input port for writing a new value to the variable directly in the graph.
        /// </summary>
        /// <remarks>
        /// Only valid for variables of kind <see cref="VariableKind.Local"/>.
        /// </remarks>
        Set,
    }
}
