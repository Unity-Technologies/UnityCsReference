// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation;

partial class GraphModelImp
{
    // Reused across CollectChangeData calls to avoid re-allocating the outer dictionaries. Cleared at the end of each call.
    [NonSerialized]
    Dictionary<Hash128, ChangedNodeBuilder> m_NodeBuilders = new();

    [NonSerialized]
    Dictionary<Hash128, ChangedVariableBuilder> m_VariableBuilders = new();

    [NonSerialized]
    Dictionary<Hash128, ChangedConstantNodeBuilder> m_ConstantNodeBuilders = new();

    [NonSerialized]
    Dictionary<Hash128, ChangedSubgraphNodeBuilder> m_SubgraphNodeBuilders = new();

    // Mutable accumulator used while folding all per-element changes into a single ChangedNode / ChangedVariable per guid.
    struct ChangedNodeBuilder
    {
        public readonly INode Node;
        public readonly Hash128 Guid;
        public ChangeKind Kinds;
        public readonly Dictionary<Hash128, ChangedPortBuilder> Ports;

        public ChangedNodeBuilder(Hash128 guid, INode node)
        {
            Node = node;
            Guid = guid;
            Kinds = ChangeKind.None;
            Ports = new Dictionary<Hash128, ChangedPortBuilder>();
        }

        public void AddChange(ChangeKind kind)
        {
            Kinds |= kind;
        }

        public void AddPort(ChangedPortBuilder builder)
        {
            Ports.Add(builder.Guid, builder);
        }

        public ChangedNode Build()
        {
            List<ChangedPort> ports = null;
            if (Ports.Count > 0)
            {
                Kinds |= ChangeKind.PortChanged;

                ports = new List<ChangedPort>(Ports.Count);
                foreach (var pb in Ports.Values)
                    ports.Add(pb.Build());
            }

            return new ChangedNode(Node, Guid, Kinds, ports);
        }
    }

    struct ChangedPortBuilder
    {
        public readonly IPort Port;
        public readonly Hash128 Guid;
        public ChangeKind Kinds;

        public ChangedPortBuilder(Hash128 guid, IPort port)
        {
            Port = port;
            Guid = guid;
            Kinds = ChangeKind.None;
        }

        public void AddChange(ChangeKind kind)
        {
            Kinds |= kind;
        }

        public ChangedPort Build() => new ChangedPort(Port, Guid, Kinds);
    }

    struct ChangedVariableBuilder
    {
        public readonly IVariable Variable;
        public readonly Hash128 Guid;
        public ChangeKind Kinds;

        public ChangedVariableBuilder(Hash128 guid, IVariable variable)
        {
            Variable = variable;
            Guid = guid;
            Kinds = ChangeKind.None;
        }

        public void AddChange(ChangeKind kind)
        {
            Kinds |= kind;
        }

        public ChangedVariable Build() => new ChangedVariable(Variable, Guid, Kinds);
    }

    struct ChangedConstantNodeBuilder
    {
        public readonly IConstantNode ConstantNode;
        public readonly Hash128 Guid;
        public ChangeKind Kinds;

        public ChangedConstantNodeBuilder(Hash128 guid, IConstantNode constantNode)
        {
            ConstantNode = constantNode;
            Guid = guid;
            Kinds = ChangeKind.None;
        }

        public void AddChange(ChangeKind kind)
        {
            Kinds |= kind;
        }

        public ChangedConstantNode Build() => new ChangedConstantNode(ConstantNode, Guid, Kinds);
    }

    struct ChangedSubgraphNodeBuilder
    {
        public readonly ISubgraphNode SubgraphNode;
        public readonly Hash128 Guid;
        public ChangeKind Kinds;

        public ChangedSubgraphNodeBuilder(Hash128 guid, ISubgraphNode subgraphNode)
        {
            SubgraphNode = subgraphNode;
            Guid = guid;
            Kinds = ChangeKind.None;
        }

        public void AddChange(ChangeKind kind)
        {
            Kinds |= kind;
        }

        public ChangedSubgraphNode Build() => new ChangedSubgraphNode(SubgraphNode, Guid, Kinds);
    }

    protected virtual void CollectChangeData(GraphChangeDescription changes, ILogger logger)
        => CollectChangeData(changes, logger as GraphLogger);

    void CollectChangeData(GraphChangeDescription changes, GraphLogger graphLogger)
    {
        if (changes != null)
        {
            foreach (var guid in changes.NewModels)
            {
                if (!TryGetModelFromGuid(guid, out var model))
                    continue;

                if (model is IVariable variable)
                {
                    if (TryGetOrAddVariableBuilder(m_VariableBuilders, guid, variable, out var vb))
                    {
                        vb.AddChange(ChangeKind.Added);
                        m_VariableBuilders[guid] = vb;
                    }
                }
                else if (model is IPort port)
                {
                    var ownerGuid = (port as PortModel)?.NodeModel?.Guid ?? default;

                    if (TryGetOrAddNodeBuilder(m_NodeBuilders, ownerGuid, null, out var nb) &&
                        TryGetOrAddPortBuilder(nb.Ports, guid, port, out var pb))
                    {
                        pb.AddChange(ChangeKind.Added);
                        nb.Ports[guid] = pb;
                    }
                }
                else if (model is IUserNodeModelImp imp)
                {
                    if (TryGetOrAddNodeBuilder(m_NodeBuilders, guid, imp.Node, out var nb))
                    {
                        nb.AddChange(ChangeKind.Added);
                        m_NodeBuilders[guid] = nb;
                    }
                }
                else if (model is IVariableNode variableNode)
                {
                    if (TryGetOrAddNodeBuilder(m_NodeBuilders, guid, variableNode, out var nb))
                    {
                        nb.AddChange(ChangeKind.Added);
                        m_NodeBuilders[guid] = nb;
                    }
                }
                else if (model is IConstantNode constantNode)
                {
                    if (TryGetOrAddConstantNodeBuilder(m_ConstantNodeBuilders, guid, constantNode, out var cb))
                    {
                        cb.AddChange(ChangeKind.Added);
                        m_ConstantNodeBuilders[guid] = cb;
                    }
                }
                else if (model is ISubgraphNode subgraphNode)
                {
                    if (TryGetOrAddSubgraphNodeBuilder(m_SubgraphNodeBuilders, guid, subgraphNode, out var sb))
                    {
                        sb.AddChange(ChangeKind.Added);
                        m_SubgraphNodeBuilders[guid] = sb;
                    }
                }
            }

            foreach (var kvp in changes.ChangedModels)
            {
                if (!TryGetModelFromGuid(kvp.Key, out var model))
                    continue;

                if (model is IVariable variable)
                {
                    CollectVariableHints(kvp, variable);
                }
                else if (model is IVariableNode variableNode)
                {
                    if (TryGetOrAddNodeBuilder(m_NodeBuilders, kvp.Key, variableNode, out var nb))
                    {
                        foreach (var hint in kvp.Value.Hints)
                            nb.AddChange(hint.ToKind());
                        m_NodeBuilders[kvp.Key] = nb;
                    }
                }
                else if (model is IPort port)
                {
                    var ownerGuid = (port as PortModel)?.NodeModel?.Guid ?? default;

                    if (TryGetOrAddNodeBuilder(m_NodeBuilders, ownerGuid, null, out var nb) &&
                        TryGetOrAddPortBuilder(nb.Ports, kvp.Key, port, out var pb))
                    {
                        foreach (var hint in kvp.Value.Hints)
                            pb.AddChange(hint.ToKind());
                        nb.Ports[kvp.Key] = pb;
                    }
                }
                else if (model is IUserNodeModelImp imp)
                {
                    if (TryGetOrAddNodeBuilder(m_NodeBuilders, kvp.Key, imp.Node, out var nb))
                    {
                        foreach (var hint in kvp.Value.Hints)
                            nb.AddChange(hint.ToKind());
                        m_NodeBuilders[kvp.Key] = nb;
                    }
                }
                else if (model is IConstantNode constantNode)
                {
                    if (TryGetOrAddConstantNodeBuilder(m_ConstantNodeBuilders, kvp.Key, constantNode, out var cb))
                    {
                        foreach (var hint in kvp.Value.Hints)
                            cb.AddChange(hint.ToKind());
                        m_ConstantNodeBuilders[kvp.Key] = cb;
                    }
                }
                else if (model is ISubgraphNode subgraphNode)
                {
                    if (TryGetOrAddSubgraphNodeBuilder(m_SubgraphNodeBuilders, kvp.Key, subgraphNode, out var sb))
                    {
                        foreach (var hint in kvp.Value.Hints)
                            sb.AddChange(hint.ToKind());
                        m_SubgraphNodeBuilders[kvp.Key] = sb;
                    }
                }
            }
        }

        foreach (var kvp in m_DeletedPortToNodeGuid)
        {
            // For genuinely-removed nodes the resolution inside TryGetOrAddNodeBuilder fails and we skip.
            if (!TryGetOrAddNodeBuilder(m_NodeBuilders, kvp.Value, out var nb))
                continue;

            if (TryGetOrAddPortBuilder(nb.Ports, kvp.Key, port: null, out var pb))
            {
                pb.AddChange(ChangeKind.Removed);
                nb.Ports[kvp.Key] = pb;
            }
        }

        m_DeletedPortToNodeGuid.Clear();

        var changedNodes = new List<ChangedNode>(m_NodeBuilders.Count);
        foreach (var nb in m_NodeBuilders.Values)
        {
            if (nb.Node == null)
                continue;

            changedNodes.Add(nb.Build());
        }

        var changedVariables = new List<ChangedVariable>(m_VariableBuilders.Count);
        foreach (var vb in m_VariableBuilders.Values)
            changedVariables.Add(vb.Build());

        var changedConstantNodes = new List<ChangedConstantNode>(m_ConstantNodeBuilders.Count);
        foreach (var cb in m_ConstantNodeBuilders.Values)
            changedConstantNodes.Add(cb.Build());

        var changedSubgraphNodes = new List<ChangedSubgraphNode>(m_SubgraphNodeBuilders.Count);
        foreach (var sb in m_SubgraphNodeBuilders.Values)
            changedSubgraphNodes.Add(sb.Build());

        graphLogger?.SetChangeData(changedNodes, changedVariables, changedConstantNodes, changedSubgraphNodes);

        m_NodeBuilders.Clear();
        m_VariableBuilders.Clear();
        m_ConstantNodeBuilders.Clear();
        m_SubgraphNodeBuilders.Clear();

        void CollectVariableHints(KeyValuePair<Hash128, ChangeHintList> kvp, IVariable variable)
        {
            if (TryGetOrAddVariableBuilder(m_VariableBuilders, kvp.Key, variable, out var vb))
            {
                foreach (var hint in kvp.Value.Hints)
                    vb.AddChange(hint.ToKind());
                m_VariableBuilders[kvp.Key] = vb;
            }
        }
    }

    // Returns true when `builder` is either the existing entry or a freshly-created one that has been stored in `builders`.
    // Because the builders are structs, callers must write `builder` back to `builders[guid]` after any Kinds mutations.
    bool TryGetOrAddNodeBuilder(Dictionary<Hash128, ChangedNodeBuilder> builders, Hash128 guid,
        out ChangedNodeBuilder builder)
    {
        return TryGetOrAddNodeBuilder(builders, guid, null, out builder);
    }

    bool TryGetOrAddNodeBuilder(Dictionary<Hash128, ChangedNodeBuilder> builders, Hash128 guid, INode node, out ChangedNodeBuilder builder)
    {
        if (builders.TryGetValue(guid, out builder))
            return true;

        if (node != null)
        {
            builder = new ChangedNodeBuilder(guid, node);
            builders[guid] = builder;
            return true;
        }

        if (TryGetModelFromGuid(guid, out var ownerModel))
        {
            if (ownerModel is IUserNodeModelImp imp)
            {
                builder = new ChangedNodeBuilder(guid, imp.Node);
                builders[guid] = builder;
                return true;
            }

            if (ownerModel is IVariableNode variableNode)
            {
                builder = new ChangedNodeBuilder(guid, variableNode);
                builders[guid] = builder;
                return true;
            }
        }

        builder = default;
        return false;
    }

    static bool TryGetOrAddPortBuilder(Dictionary<Hash128, ChangedPortBuilder> ports, Hash128 guid, IPort port,
        out ChangedPortBuilder builder)
    {
        if (ports.TryGetValue(guid, out builder))
            return true;

        builder = new ChangedPortBuilder(guid, port);
        ports[guid] = builder;
        return true;
    }

    static bool TryGetOrAddVariableBuilder(Dictionary<Hash128, ChangedVariableBuilder> builders, Hash128 guid,
        IVariable variable, out ChangedVariableBuilder builder)
    {
        if (builders.TryGetValue(guid, out builder))
            return true;

        builder = new ChangedVariableBuilder(guid, variable);
        builders[guid] = builder;
        return true;
    }

    static bool TryGetOrAddConstantNodeBuilder(Dictionary<Hash128, ChangedConstantNodeBuilder> builders, Hash128 guid,
        IConstantNode constantNode, out ChangedConstantNodeBuilder builder)
    {
        if (builders.TryGetValue(guid, out builder))
            return true;

        builder = new ChangedConstantNodeBuilder(guid, constantNode);
        builders[guid] = builder;
        return true;
    }

    static bool TryGetOrAddSubgraphNodeBuilder(Dictionary<Hash128, ChangedSubgraphNodeBuilder> builders, Hash128 guid,
        ISubgraphNode subgraphNode, out ChangedSubgraphNodeBuilder builder)
    {
        if (builders.TryGetValue(guid, out builder))
            return true;

        builder = new ChangedSubgraphNodeBuilder(guid, subgraphNode);
        builders[guid] = builder;
        return true;
    }
}
