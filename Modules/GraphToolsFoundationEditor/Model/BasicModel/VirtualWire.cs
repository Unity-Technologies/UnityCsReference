// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor;

/// <summary>
/// This class is used to represent connections between a subset of the nodes in a graph. A series of
/// <see cref="Wire"/> to/between/from nodes that are not part of the subset is represented as
/// a single virtual wire.
/// </summary>
class VirtualWire : IPortWireIndexModel_Internal
{
    PortReference m_FromPortReference;
    PortReference m_ToPortReference;
    WireModel[] m_WireModels;
    PortModel m_FromPortModelCache;
    PortModel m_ToPortModelCache;

    /// <summary>
    /// The port from which the wire originates.
    /// </summary>
    public PortModel FromPort => m_FromPortReference.GetPortModel(PortDirection.Output, ref m_FromPortModelCache);

    /// <summary>
    /// The port to which the wire goes.
    /// </summary>
    public PortModel ToPort => m_ToPortReference.GetPortModel(PortDirection.Input, ref m_ToPortModelCache);

    /// <summary>
    /// The <see cref="WireModel"/>s that this <see cref="VirtualWire"/> represents.
    /// </summary>
    public IReadOnlyList<WireModel> Wires => m_WireModels;

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualWire"/> class.
    /// </summary>
    /// <param name="wireModel">The <see cref="WireModel"/> represented by the virtual wire.</param>
    public VirtualWire(WireModel wireModel)
    {
        if (wireModel == null)
            return;

        m_FromPortReference = new PortReference();
        m_FromPortReference.Assign(wireModel.FromPort);

        m_ToPortReference = new PortReference();
        m_ToPortReference.Assign(wireModel.ToPort);

        m_WireModels = new[] { wireModel };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualWire"/> class by merging two <see cref="VirtualWire"/>s.
    /// </summary>
    /// <param name="first">The first wire.</param>
    /// <param name="second">The second wire.</param>
    public VirtualWire(VirtualWire first, VirtualWire second)
    {
        m_FromPortReference = new PortReference();
        m_FromPortReference.Assign(first.FromPort);

        m_ToPortReference = new PortReference();
        m_ToPortReference.Assign(second.ToPort);

        m_WireModels = new WireModel[first.m_WireModels.Length + second.m_WireModels.Length];
        Array.Copy(first.m_WireModels, m_WireModels, first.m_WireModels.Length);
        Array.Copy(second.m_WireModels, 0, m_WireModels, first.m_WireModels.Length, second.m_WireModels.Length);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{m_FromPortReference} -> {m_ToPortReference} (replaces {m_WireModels.Length} wire(s))";
    }
}
