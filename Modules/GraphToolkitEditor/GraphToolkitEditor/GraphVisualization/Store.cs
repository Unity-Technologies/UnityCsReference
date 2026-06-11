// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Stores and implements the logic for visualization data related to a graph visualization session.
/// </summary>
class Store
{
    PortPreviewStore m_PortPreviewStore;
    internal PortPreviewStore PortPreviewStore => m_PortPreviewStore ??= new PortPreviewStore();
    
    internal NodeAccentStore NodeAccentStore = new();
}

