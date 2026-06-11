// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Stores and implements the logic for visualization data related to a graph visualization session.
/// </summary>
class PortPreviewStore
{
    // TODO: Using a dictionary is not thread-safe, might need to change the container in the future if requested.
    internal Dictionary<Hash128, PortPreviewData> AllPortPreviewData { get; } = new();

    internal void Set(Hash128 portGuid, string displayValue)
    {
        // Update the cache.
        if (AllPortPreviewData.TryGetValue(portGuid, out var portPreview))
        {
            portPreview.StringValue = displayValue;
        }
        else
        {
            AllPortPreviewData[portGuid] = new PortPreviewData { StringValue = displayValue };
        }
    }

    internal bool TryGet(Hash128 portGuid, out string value)
    {
        value = null;

        if (!AllPortPreviewData.TryGetValue(portGuid, out var portPreviewData))
            return false;

        if (portPreviewData.StringValue != null)
            value = portPreviewData.StringValue;
        else
        {
            // Should not be the case: The port preview is removed from the store when the string value is null.
            // But in case it happens, we remove the port preview from the store to avoid keeping invalid data.
            AllPortPreviewData.Remove(portGuid);
            Debug.LogError($"Port preview with null value found in the store for port {portGuid}. This should not happen, as port previews with null values should be cleared. Clearing the port preview to avoid keeping invalid data.");
            return false;
        }

        return true;
    }

    internal void Clear(Hash128 portGuid)
    {
        AllPortPreviewData.Remove(portGuid);
    }

    internal void ClearAll()
    {
        AllPortPreviewData.Clear();
    }
}
