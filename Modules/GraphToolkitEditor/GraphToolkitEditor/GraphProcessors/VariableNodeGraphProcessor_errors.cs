// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor;

partial class VariableNodeGraphProcessor
{
    // Roughly where collecting and rechecking the touched nodes starts to cost more than a full recheck.
    // Calculated after measuring moving 1-50% of a 1000 node graph, incremental stops being faster around 7%.
    const int k_NodeChangeRescanThreshold = 16;

    void UpdatePortErrors(GraphChangeDescription changes)
    {
        // Custom connection rules can depend on anything in the graph, so only local rules allow a partial recheck.
        if (changes != null && m_PortErrorsInitialized && m_GraphModel.HasLocalConnectionRules)
        {
            using var pooledNodesToRecheck = HashSetPool<AbstractNodeModel>.Get(out var nodesToRecheck);
            if (TryCollectNodesToRecheck(changes, nodesToRecheck))
            {
                foreach (var nodeModel in nodesToRecheck)
                {
                    RecheckPortErrors(nodeModel);
                }

                return;
            }
        }

        RecheckAllPortErrors();
        m_PortErrorsInitialized = true;
    }

    // Same result as RecheckPortErrors on every top-level node, but only nodes with an invalid wire are rechecked.
    void RecheckAllPortErrors()
    {
        m_PortErrorsByNode.Clear();
        using var pooledNodesWithErrors = HashSetPool<AbstractNodeModel>.Get(out var nodesWithErrors);

        var wires = m_GraphModel.WireModels;
        for (var i = 0; i < wires.Count; i++)
        {
            var fromPort = wires[i].FromPort;
            var toPort = wires[i].ToPort;
            if (fromPort == null || toPort == null)
                continue;

            if (toPort.Direction == PortDirection.Input && !m_GraphModel.CanAssignTo(toPort, fromPort))
                AddNodeToRecheck(toPort.NodeModel, nodesWithErrors);
            if (fromPort.Direction == PortDirection.Input && !m_GraphModel.CanAssignTo(fromPort, toPort))
                AddNodeToRecheck(fromPort.NodeModel, nodesWithErrors);
        }

        foreach (var nodeModel in nodesWithErrors)
        {
            RecheckPortErrors(nodeModel);
        }
    }

    void RecheckPortErrors(AbstractNodeModel abstractNodeModel)
    {
        m_PortErrorsByNode.Remove(abstractNodeModel.Guid);
        if (abstractNodeModel is not NodeModel nodeModel)
            return;

        var portErrors = nodeModel.CheckNodeErrors();
        if (portErrors != null)
            m_PortErrorsByNode[nodeModel.Guid] = portErrors;
    }

    // Returns false when the changes cannot be mapped to a set of nodes and a full recheck is needed.
    bool TryCollectNodesToRecheck(GraphChangeDescription changes, HashSet<AbstractNodeModel> nodesToRecheck)
    {
        if (changes.DeletedModels.Count > 0)
            return false;

        var changeCount = changes.NewModels.Count;
        foreach (var changedModel in changes.ChangedModels)
        {
            if (CanAffectPortErrors(changedModel.Value))
                changeCount++;
        }

        if (changeCount > m_GraphModel.NodeModels.Count / k_NodeChangeRescanThreshold)
            return false;

        foreach (var guid in changes.NewModels)
        {
            if (!TryCollectNodesToRecheck(guid, nodesToRecheck))
                return false;
        }

        foreach (var changedModel in changes.ChangedModels)
        {
            if (CanAffectPortErrors(changedModel.Value) && !TryCollectNodesToRecheck(changedModel.Key, nodesToRecheck))
                return false;
        }

        return true;
    }

    static bool CanAffectPortErrors(ChangeHintList changeHints)
    {
        if (changeHints == null || changeHints.Hints.Count == 0)
            return true;

        var hints = changeHints.Hints;
        for (var i = 0; i < hints.Count; i++)
        {
            if (!ChangeHint.NonDataHints.Contains(hints[i]))
                return true;
        }

        return false;
    }

    bool TryCollectNodesToRecheck(Hash128 guid, HashSet<AbstractNodeModel> nodesToRecheck)
    {
        if (!m_GraphModel.TryGetModelFromGuid(guid, out var model))
            return false;

        switch (model)
        {
            case PortNodeModel portNodeModel:
                AddNodeAndConnectedNodes(portNodeModel, nodesToRecheck);
                return true;
            case PortModel portModel:
                AddPortAndConnectedNodes(portModel, nodesToRecheck);
                return true;
            case WireModel wireModel:
                AddNodeToRecheck(wireModel.ToPort?.NodeModel, nodesToRecheck);
                AddNodeToRecheck(wireModel.FromPort?.NodeModel, nodesToRecheck);
                return true;
            case StickyNoteModel:
            case PlacematModel:
                return true;
            default:
                return false;
        }
    }

    static void AddNodeAndConnectedNodes(PortNodeModel portNodeModel, HashSet<AbstractNodeModel> nodesToRecheck)
    {
        AddNodeToRecheck(portNodeModel, nodesToRecheck);

        var ports = portNodeModel.GetPorts();
        for (var i = 0; i < ports.Count; i++)
        {
            AddPortAndConnectedNodes(ports[i], nodesToRecheck);
        }
    }

    static void AddPortAndConnectedNodes(PortModel portModel, HashSet<AbstractNodeModel> nodesToRecheck)
    {
        AddNodeToRecheck(portModel.NodeModel, nodesToRecheck);

        var wires = portModel.GetConnectedWires();
        for (var i = 0; i < wires.Count; i++)
        {
            AddNodeToRecheck(wires[i].GetOtherPort(portModel)?.NodeModel, nodesToRecheck);
        }
    }

    static void AddNodeToRecheck(AbstractNodeModel nodeModel, HashSet<AbstractNodeModel> nodesToRecheck)
    {
        // Port errors are only reported for top-level nodes, blocks are not checked.
        if (nodeModel != null && nodeModel is not BlockNodeModel)
            nodesToRecheck.Add(nodeModel);
    }
}
