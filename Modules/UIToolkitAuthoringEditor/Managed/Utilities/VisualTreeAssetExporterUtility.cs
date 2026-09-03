// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.UIToolkit.Editor.Importers;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

static class VisualTreeAssetExporterUtility
{
    // Exports a set of roots to a UXML string suitable for the system copy buffer.
    // Roots may belong to different VisualTreeAssets; each group is exported against
    // its own asset and the results are merged into a single consolidated document.
    internal static string ToUxmlString(List<UxmlAsset> roots)
    {
        var exporter = VisualTreeAssetExporter.Default;

        var groups = new Dictionary<VisualTreeAsset, List<UxmlAsset>>();
        var order = new List<VisualTreeAsset>();
        foreach (var root in roots)
        {
            if (root == null)
                continue;
            var vta = root.visualTreeAsset;
            if (vta == null)
                continue;
            if (!groups.TryGetValue(vta, out var group))
            {
                groups[vta] = group = new List<UxmlAsset>();
                order.Add(vta);
            }
            group.Add(root);
        }

        if (order.Count == 0)
            return string.Empty;

        if (order.Count == 1)
            return exporter.ToUxmlString(order[0], groups[order[0]]);

        // Multiple sources: export each against its own asset, import it, then swallow the results into one
        // consolidated asset. Swallowing reparents the assets, and an asset changing document re-files its
        // inline rule into the new one's inline stylesheet, so the final single-asset export resolves every
        // element's inline styles correctly.
        VisualTreeAsset combined = null;
        try
        {
            foreach (var vta in order)
            {
                var exported = exporter.ToUxmlString(vta, groups[vta]);
                var importer = new TempVisualTreeAssetImporter();
                importer.ImportXmlFromString(exported, out var imported);

                if (combined == null)
                {
                    combined = imported;
                }
                else
                {
                    combined.Swallow(combined.visualTree, imported);
                    UnityEngine.Object.DestroyImmediate(imported);
                }
            }

            return exporter.ToUxmlString(combined);
        }
        finally
        {
            if (combined != null)
                UnityEngine.Object.DestroyImmediate(combined);
        }
    }
}
