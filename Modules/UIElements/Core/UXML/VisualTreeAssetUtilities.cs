// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    internal static class VisualTreeAssetUtilities
    {
        public static UxmlAsset GetParentAsset(this VisualTreeAsset vta, UxmlAsset asset)
        {
            var parentId = asset.parentId;

            var parentIndex = vta.visualElementAssets.FindIndex(ua => ua.id == parentId);
            if (parentIndex >= 0)
            {
                return vta.visualElementAssets[parentIndex];
            }

            for (var entryIndex = 0; entryIndex < vta.uxmlObjectEntries.Count; ++entryIndex)
            {
                var uxmlAssets = vta.uxmlObjectEntries[entryIndex].uxmlObjectAssets;
                parentIndex = uxmlAssets.FindIndex(ua => ua.id == parentId);
                if (parentIndex >= 0)
                {
                    return uxmlAssets[parentIndex];
                }
            }

            return null;
        }

        public static IEnumerable<string> EnumerateEnclosingNamespaces(string fullTypeName)
        {
            var startIndex = fullTypeName.Length - 1;

            while (true)
            {
                var lastDot = fullTypeName.LastIndexOf(".", startIndex, StringComparison.Ordinal);
                if (lastDot >= 0)
                {
                    yield return fullTypeName[..lastDot];
                    startIndex = lastDot - 1;
                    continue;
                }
                yield break;
            }
        }

        public static UxmlNamespaceDefinition FindUxmlNamespaceDefinitionFromPrefix(this VisualTreeAsset vta, UxmlAsset asset, string prefix)
        {
            var current = asset;
            while (current != null)
            {
                for(var i = 0; i < current.namespaceDefinitions.Count; ++i)
                {
                    var namespaceDefinition = current.namespaceDefinitions[i];
                    if (string.Compare(namespaceDefinition.prefix, prefix, StringComparison.Ordinal) == 0)
                        return namespaceDefinition;
                }
                current = vta.GetParentAsset(current);
            }
            return UxmlNamespaceDefinition.Empty;
        }

        public static UxmlNamespaceDefinition FindUxmlNamespaceDefinitionForTypeName(this VisualTreeAsset vta, UxmlAsset asset, string fullTypeName)
        {
            using var listHandle = ListPool<UxmlNamespaceDefinition>.Get(out var namespaceDefinitions);
            {
                var current = asset;
                while (current != null)
                {
                    namespaceDefinitions.AddRange(current.namespaceDefinitions);
                    current = vta.GetParentAsset(current);
                }
            }

            if (namespaceDefinitions.Count == 0)
                return UxmlNamespaceDefinition.Empty;

            foreach (var ns in EnumerateEnclosingNamespaces(fullTypeName))
            {
                for (var i = 0; i < namespaceDefinitions.Count; ++i)
                {
                    if (namespaceDefinitions[i].resolvedNamespace.Equals(ns, StringComparison.Ordinal))
                    {
                        return namespaceDefinitions[i];
                    }
                }
            }

            return UxmlNamespaceDefinition.Empty;
        }

        public static void GatherUxmlNamespaceDefinitions(this VisualTreeAsset vta, UxmlAsset asset, List<UxmlNamespaceDefinition> definitions)
        {
            var current = asset;
            while (current != null)
            {
                definitions.InsertRange(0, current.namespaceDefinitions);
                current = vta.GetParentAsset(current);
            }
        }
    }
}
