// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor;

/// <summary>
/// Utility methods to build a <see cref="VirtualWire"/> list for a <see cref="GraphModel"/>.
/// </summary>
static class VirtualWireBuilder
{
    static List<(VirtualWire wire, bool remove)> s_Changes = new ();

    static void ApplyChanges(ICollection<VirtualWire> virtualWires, PortWireIndex_Internal<VirtualWire> portWireIndex)
    {
        foreach (var (wire, remove) in s_Changes)
        {
            if (remove)
            {
                virtualWires.Remove(wire);
                portWireIndex.WireRemoved(wire);
            }
            else
            {
                virtualWires.Add(wire);
                portWireIndex.WireAdded(wire);
            }
        }
    }

    /// <summary>
    /// Generate Virtual Wires To By Pass Portal.
    /// </summary>
    /// <param name="portal">The portal for which to generate virtual wires.</param>
    /// <param name="virtualWires">A container to hold the wires.</param>
    /// <param name="portWireIndex">Index to speed up wire search.</param>
    static void GenerateVirtualWiresToByPassPortal(WirePortalModel portal, ICollection<VirtualWire> virtualWires, PortWireIndex_Internal<VirtualWire> portWireIndex)
    {
        var otherPortalType = portal is ISingleInputPortNodeModel ? typeof(ISingleOutputPortNodeModel) : typeof(ISingleInputPortNodeModel);
        var declarationModel = portal.DeclarationModel;

        s_Changes.Clear();
        foreach (var portalPort in portal.Ports)
        {
            foreach (var portalWire in portWireIndex.GetWiresForPort(portalPort))
            {
                s_Changes.Add((portalWire, true));

                foreach (var node in portal.GraphModel.NodeModels)
                {
                    if (!(node is WirePortalModel otherSidePortal && otherPortalType.IsInstanceOfType(otherSidePortal) && otherSidePortal.DeclarationModel == declarationModel))
                        continue;

                    if (portalWire.FromPort.NodeModel == node || portalWire.ToPort.NodeModel == node)
                    {
                        // Wire is a loop on the portal nodes.
                        continue;
                    }

                    foreach (var otherSidePort in otherSidePortal.Ports)
                    {
                        foreach (var otherSideWire in portWireIndex.GetWiresForPort(otherSidePort))
                        {
                            s_Changes.Add((otherSideWire, true));

                            if (otherSideWire.FromPort.NodeModel != portal && otherSideWire.ToPort.NodeModel != portal)
                            {
                                var virtualWire = otherPortalType == typeof(ISingleOutputPortNodeModel) ?
                                    new VirtualWire(portalWire, otherSideWire) :
                                    new VirtualWire(otherSideWire, portalWire);
                                s_Changes.Add((virtualWire, false));
                            }
                            // Otherwise, wire is a loop on the portal nodes.
                        }
                    }
                }
            }
        }

        ApplyChanges(virtualWires, portWireIndex);
    }

    /// <summary>
    /// Generate Virtual Wires To By Pass IONode.
    /// </summary>
    /// <param name="nodeModel">The node for which to generate virtual wires.</param>
    /// <param name="virtualWires">A container to hold the wires.</param>
    /// <param name="portWireIndex">Index to speed up wire search.</param>
    static void GenerateVirtualWiresToByPassIONode(InputOutputPortsNodeModel nodeModel, ICollection<VirtualWire> virtualWires, PortWireIndex_Internal<VirtualWire> portWireIndex)
    {
        s_Changes.Clear();
        foreach (var inputPort in nodeModel.InputsById.Values)
        {
            foreach (var inputWire in portWireIndex.GetWiresForPort(inputPort))
            {
                s_Changes.Add((inputWire, true));

                if (inputWire.FromPort.NodeModel == inputWire.ToPort.NodeModel)
                {
                    // Wire is a loop on the node.
                    continue;
                }

                foreach (var outputPort in nodeModel.OutputsById.Values)
                {
                    foreach (var outputWire in portWireIndex.GetWiresForPort(outputPort))
                    {
                        s_Changes.Add((outputWire, true));

                        if (outputWire.FromPort.NodeModel != outputWire.ToPort.NodeModel)
                        {
                            var virtualWire = new VirtualWire(inputWire, outputWire);
                            s_Changes.Add((virtualWire, false));
                        }
                        // Otherwise, wire is a loop on the node.
                    }
                }
            }
        }

        ApplyChanges(virtualWires, portWireIndex);
    }

    /// <summary>
    /// Returns a read-only collection of <see cref="VirtualWire"/> for the <paramref name="graphModel"/>.
    /// </summary>
    /// <param name="graphModel">The graph model</param>
    /// <returns>Returns a list of <see cref="VirtualWire"/>, for the <paramref name="graphModel"/>. A virtual wire is
    /// created for each wire in the graph, except for wires linked to portals. In this case, virtual wires that
    /// bypass the portals are created.</returns>
    public static IReadOnlyCollection<VirtualWire> GetVirtualWiresOverPortals(this GraphModel graphModel)
    {
        var virtualWires = new List<VirtualWire>(graphModel.WireModels.Count);

        foreach (var wireModel in graphModel.WireModels)
        {
            var virtualWire = new VirtualWire(wireModel);
            virtualWires.Add(virtualWire);
        }

        var portWireIndex = new PortWireIndex_Internal<VirtualWire>(virtualWires);
        s_Changes = new List<(VirtualWire wire, bool remove)>();

        foreach (var nodeModel in graphModel.NodeModels)
        {
            if (nodeModel is WirePortalModel portal)
            {
                GenerateVirtualWiresToByPassPortal(portal, virtualWires, portWireIndex);
            }
        }

        s_Changes = null;

        return virtualWires;
    }

    /// <summary>
    /// Returns a read-only collection of <see cref="VirtualWire"/> for the <paramref name="graphModel"/>.
    /// </summary>
    /// <param name="graphModel">The graph model</param>
    /// <param name="nodeFilter">When this function returns true for a node, the node will be bypassed by virtual wires.</param>
    /// <returns>Returns a list of <see cref="VirtualWire"/>, for the <paramref name="graphModel"/>. A virtual wire is
    /// created for each wire in the graph, except for wires linked to bypassed nodes. In this case, virtual wires that
    /// bypass the node are created.</returns>
    /// <remarks>Only nodes of type InputOutputPortsNodeModel and WirePortalModel can be bypassed.</remarks>
    public static IReadOnlyCollection<VirtualWire> GetVirtualWires(this GraphModel graphModel, Func<AbstractNodeModel, bool> nodeFilter)
    {
        HashSet<Type> errorTypes = null;
        var virtualWires = new List<VirtualWire>(graphModel.WireModels.Count);

        foreach (var wireModel in graphModel.WireModels)
        {
            var virtualWire = new VirtualWire(wireModel);
            virtualWires.Add(virtualWire);
        }

        var portWireIndex = new PortWireIndex_Internal<VirtualWire>(virtualWires);
        s_Changes = new List<(VirtualWire wire, bool remove)>();

        foreach (var nodeModel in graphModel.NodeModels)
        {
            if (nodeFilter(nodeModel))
            {
                switch (nodeModel)
                {
                    case WirePortalModel portal:
                        GenerateVirtualWiresToByPassPortal(portal, virtualWires, portWireIndex);
                        break;
                    case InputOutputPortsNodeModel inputOutputPortsNode:
                        GenerateVirtualWiresToByPassIONode(inputOutputPortsNode, virtualWires, portWireIndex);
                        break;
                    default:
                    {
                        errorTypes ??= new HashSet<Type>();
                        if (!errorTypes.TryGetValue(nodeModel.GetType(), out var _))
                        {
                            Debug.LogWarning($"Unsupported node type {nodeModel.GetType().Name} while computing virtual wires.");
                            errorTypes.Add(nodeModel.GetType());
                        }
                        break;
                    }
                }
            }
        }

        s_Changes = null;

        return virtualWires;
    }
}
