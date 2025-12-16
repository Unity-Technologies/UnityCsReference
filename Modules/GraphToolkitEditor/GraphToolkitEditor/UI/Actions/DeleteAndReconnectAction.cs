// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Action that can be triggered from within the context of a GraphView.
    /// Usually used by the contextual menu and the shortcut system.
    /// The action will reconnect the wires between the output ports and the input ports of unselected nodes.
    /// </summary>
    [UnityRestricted]
    class DeleteAndReconnectAction
    {
        bool m_IsValidAction;
        bool m_HasNodes;

        IReadOnlyList<GraphElementModel> m_Selection;
        List<(PortModel output, PortModel input)> m_WiresToReconnect;

        /// <summary>
        /// Function to determine if a port is a flow port. Defaults to checking if the port's data type is ExecutionFlow.
        /// </summary>
        public Func<PortModel, bool> IsFlowPort = p => p.DataTypeHandle == TypeHandle.ExecutionFlow;

        /// <summary>
        /// Resets the state of the action.
        /// </summary>
        public void Clear()
        {
            m_IsValidAction = false;
            m_HasNodes = false;
            m_WiresToReconnect?.Clear();
        }

        /// <summary>
        /// After ValidateAction has been called, returns true if there is at least one node in the current selection.
        /// </summary>
        public bool HasNodes => m_HasNodes;

        /// <summary>
        /// After ValidateAction has been called, returns true if the action is valid and can be executed.
        /// </summary>
        public bool IsValidAction => m_IsValidAction;

        /// <summary>
        /// Validates the action based on the selection passed.
        ///
        /// this action is valid if there is a path between output ports of unselected nodes and input ports of selected nodes,
        /// through the first connected input port of the selected nodes and the first connected output port of the selected nodes, considering flow ports and non flow ports separately, as defined by <see cref="IsFlowPort"/>.
        /// And that there is either only one wire on the input side our on the output side of the selected nodes as we don't want cartesian products of wires.
        /// </summary>
        public bool ValidateAction(GraphModel graphModel, IReadOnlyList<GraphElementModel> selection)
        {
            Clear();
            m_Selection = selection;
            m_IsValidAction = false;

            using var dispose_node = HashSetPool<InputOutputPortsNodeModel>.Get(out var nodes);

            for (int i = 0; i < selection.Count; ++i)
            {
                if (selection[i] is not InputOutputPortsNodeModel nodeModel)
                    continue;
                m_HasNodes = true;
                nodes.Add(nodeModel);
            }

            ComputeReconnectablePorts(nodes, false);
            ComputeReconnectablePorts(nodes, true);

            if (m_WiresToReconnect == null || m_WiresToReconnect.Count < 1 )
            {
                return false;
            }

            m_IsValidAction = true;
            return m_IsValidAction;


            static PortModel OutputPort(WireModel wireModel) => wireModel.FromPort.Direction == PortDirection.Output ? wireModel.FromPort : wireModel.ToPort;
            static PortModel InputPort(WireModel wireModel) => wireModel.FromPort.Direction == PortDirection.Input ? wireModel.FromPort : wireModel.ToPort;

            void ComputeReconnectablePorts(HashSet<InputOutputPortsNodeModel> nodeModels, bool useFlowPorts)
            {
                // nodePassthroughs connect the first connected input port to the first connected output port and traverse and replace the node.
                using var dispose_nodePassthrough = DictionaryPool<PortModel, PortModel>.Get(out var nodePassthroughs);
                // wires tha are between nodes that are being deleted
                using var dispose_insideWires = DictionaryPool<PortModel, List<PortModel>>.Get(out var insideWires);
                // wires that are between the output of an unselected node and the input of a deleted node
                using var dispose_outputWires = DictionaryPool<PortModel, List<PortModel>>.Get(out var outputWires);
                // wires that are between the output of a deleted node and the input of an unselected node
                using var dispose_inputWires = DictionaryPool<PortModel, List<PortModel>>.Get(out var inputWires);
                // input ports from deleted nodes that have multiple wires connected to them
                using var dispose_internalInputsWithMultipleWires = HashSetPool<PortModel>.Get(out var internalInputsWithMultipleWires);

                using var trashBin = TrashBin.Get();

                void AddWire(Dictionary<PortModel, List<PortModel>> dictionary, WireModel wire)
                {
                    var outputPort = OutputPort(wire);
                    if (!dictionary.TryGetValue(outputPort, out var ports))
                    {
                        trashBin.Add(ListPool<PortModel>.Get(out ports));
                        dictionary.Add(outputPort, ports);
                    }

                    ports.Add(InputPort(wire));
                }

                foreach (var nodeModel in nodeModels)
                {
                    PortModel firstInputPortWithWires = null;

                    foreach (var inputPort in nodeModel.InputPorts)
                    {
                        if (IsFlowPort(inputPort) != useFlowPorts)
                            continue;
                        if (inputPort.IsConnected())
                        {
                            firstInputPortWithWires = inputPort;
                            break;
                        }
                    }

                    if (firstInputPortWithWires == null)
                        continue;

                    PortModel firstOutputPortWithWires = null;
                    foreach (var outputPort in nodeModel.OutputPorts)
                    {
                        if (IsFlowPort(outputPort) != useFlowPorts)
                            continue;
                        if (outputPort.IsConnected())
                        {
                            firstOutputPortWithWires = outputPort;
                            break;
                        }
                    }

                    if (firstOutputPortWithWires == null)
                        continue;

                    var inputPortWires = firstInputPortWithWires.GetConnectedWires();
                    var outputPortWires = firstOutputPortWithWires.GetConnectedWires();

                    nodePassthroughs.Add(firstInputPortWithWires, firstOutputPortWithWires);

                    if (inputPortWires.Count > 1)
                    {
                        internalInputsWithMultipleWires.Add(firstInputPortWithWires);
                    }

                    foreach (var wire in inputPortWires)
                    {
                        if (nodeModels.Contains(wire.GetOtherPort(firstInputPortWithWires).NodeModel))
                        {
                            AddWire(insideWires, wire);
                        }
                        else
                        {
                            AddWire(outputWires, wire);
                        }
                    }

                    foreach (var wire in outputPortWires)
                    {
                        if (!nodeModels.Contains(wire.GetOtherPort(firstOutputPortWithWires).NodeModel))
                        {
                            AddWire(inputWires, wire);
                        }
                    }
                }

                if (outputWires.Count == 0 || inputWires.Count == 0)
                    return;

                //Reconnections follow externalOutputPort => outputWire => nodePassthrough => [insideWire => nodePassthrough ]* => inputWire => externalInputPort
                //We want to remove the outputWires, inputWires, nodePassthroughs and the insideWires to get all the externalOutputPort => newWire => externalInputPort wires.

                using var dispose_nextInputs = ListPool<PortModel>.Get(out var nextInputs);
                using var dispose_currentInputs = ListPool<PortModel>.Get(out var currentInputs);

                foreach (var outputWire in outputWires)
                {
                    var externalOutputPort = outputWire.Key;
                    //externalOutputPort => outputWire => input
                    foreach (var internalInputPort in outputWire.Value)
                    {
                        currentInputs.Add(internalInputPort);
                        while (currentInputs.Count > 0)
                        {
                            foreach (var currentInput in currentInputs)
                            {
                                // input => nodePassthrough => output
                                if (!nodePassthroughs.TryGetValue(currentInput, out var outputPort))
                                    continue;

                                // output => inputWires => externalInputPort
                                if (inputWires.TryGetValue(outputPort, out var externalInputs) &&
                                    (externalInputs.Count < 2 ||
                                     !internalInputsWithMultipleWires.Contains(internalInputPort)))
                                {
                                    foreach (var externalInputPort in externalInputs)
                                    {
                                        if (graphModel.IsCompatiblePort(externalOutputPort, externalInputPort))
                                        {
                                            m_WiresToReconnect ??= new List<(PortModel output, PortModel input)>();
                                            m_WiresToReconnect.Add((output: externalOutputPort,
                                                input: externalInputPort));
                                        }
                                    }
                                }

                                // output => insideWires => input
                                if (insideWires.TryGetValue(outputPort, out var internalInputs))
                                {
                                    nextInputs.AddRange(internalInputs);
                                }
                            }

                            (nextInputs, currentInputs) = (currentInputs, nextInputs);
                            nextInputs.Clear();
                        }
                    }
                }
            }
        }

        //For tests
        internal UndoableCommand GetCommand()
        {
            return new DeleteAndReconnectCommand(m_WiresToReconnect, new List<GraphElementModel>(m_Selection));
        }

        /// <summary>
        /// Executes the previously validated action by dispatching a command to the <paramref name="commandTarget"/>.
        /// </summary>
        /// <param name="commandTarget">The <see cref="ICommandTarget"/> on which the command is dispatched.</param>
        public void ExecuteAction(ICommandTarget commandTarget)
        {
            if (!IsValidAction)
                return;
            commandTarget.Dispatch(GetCommand());
            m_WiresToReconnect = null; //Since we pass the list in WiresToReconnect to the command, we don't want to reuse as the command stores it.
        }
    }
}
