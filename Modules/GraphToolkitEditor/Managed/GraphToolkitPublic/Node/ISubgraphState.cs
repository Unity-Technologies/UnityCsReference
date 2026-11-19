// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for a specialized state that references a subgraph.
    /// </summary>
    /// <remarks>
    /// A subgraph state acts as an entry point to another graph embedded within a <see cref="StateMachine"/>.
    /// The referenced subgraph can be either a regular <see cref="Graph"/> or another <see cref="StateMachine"/>.
    /// The owning state machine must support subgraphs through <see cref="StateMachineOptions.SupportsSubgraphs"/>.
    ///
    /// See also:
    ///
    ///- <see cref="IState"/> for the members shared with every other kind of state
    ///
    /// </remarks>
    public interface ISubgraphState : IState
    {
        /// <summary>
        /// Retrieves the subgraph linked to this state as a <see cref="Graph"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="Graph"/> instance that this state references, or <see langword="null"/> if the referenced
        /// subgraph is a <see cref="StateMachine"/> (use <see cref="GetSubgraphAsStateMachine()"/> instead) or no subgraph is set.
        /// </returns>
        Graph GetSubgraphAsGraph();

        /// <summary>
        /// Retrieves the subgraph linked to this state as a <see cref="StateMachine"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="StateMachine"/> instance that this state references, or <see langword="null"/> if the
        /// referenced subgraph is a regular <see cref="Graph"/> (use <see cref="GetSubgraphAsGraph()"/> instead) or no subgraph is set.
        /// </returns>
        StateMachine GetSubgraphAsStateMachine();
    }
}
