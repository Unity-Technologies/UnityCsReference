// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor;

/// <summary>
/// A model for a marker that displays the value of a port, usually for debugging purposes.
/// </summary>
[UnityRestricted]
class PortPreviewModel : MarkerModel
{
    /// <summary>
    /// The port model whose value is displayed by this marker.
    /// </summary>
    public PortModel PortModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PortPreviewModel" /> class.
    /// </summary>
    /// <param name="portModel">The <see cref="PortModel"/> used to initialize the instance.</param>
    public PortPreviewModel(PortModel portModel)
    {
        PortModel = portModel ?? throw new ArgumentNullException(nameof(portModel));
    }

    public override GraphElementModel GetParentModel(GraphModel graphModel) => PortModel;
}
