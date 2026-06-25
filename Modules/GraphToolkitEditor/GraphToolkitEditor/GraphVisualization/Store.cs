// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Stores and implements the logic for visualization data related to a graph visualization session.
/// </summary>
class Store
{
    internal PortPreviewStore PortPreviewStore = new();
    internal WireVisualStore WireVisualStore = new();
    internal NodeAccentStore NodeAccentStore = new();
}

