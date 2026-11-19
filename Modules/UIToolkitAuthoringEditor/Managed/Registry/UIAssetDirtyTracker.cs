// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// A serializable snapshot of an asset's exported (on-disk) content, used as the "clean" reference point
/// when deciding whether the asset has real unsaved changes.
/// </summary>
/// <remarks>
/// <see cref="AssetId"/> is the durable key that survives a domain reload and resolves back to the same
/// asset; <see cref="Hash"/> is the hash of the exported UXML/USS text at the moment the baseline was
/// captured. <see cref="LastDirtyCount"/> and <see cref="LastResult"/> are a per-session memoization of the
/// last dirty answer keyed on <see cref="EditorUtility.GetDirtyCount"/>, so they are intentionally not
/// serialized.
/// </remarks>
[Serializable]
struct AssetBaseline
{
    public GlobalObjectId AssetId;

    // Resolves an asset that is not saved anywhere, which the GlobalObjectId cannot name. Valid until the editor quits.
    public EntityId EntityId;

    public Hash128 Hash;

    [NonSerialized] public int LastDirtyCount;
    [NonSerialized] public bool LastResult;
}

/// <summary>
/// Shared dirty-detection engine for <see cref="VisualTreeAsset"/> / <see cref="StyleSheet"/> authoring.
/// An asset is considered modified only when re-exporting it to its on-disk text form (UXML for a
/// VisualTreeAsset, USS for a StyleSheet) would differ from the last captured clean baseline.
/// </summary>
/// <remarks>
/// <see cref="EditorUtility.IsDirty"/> / <see cref="EditorUtility.GetDirtyCount"/> is used only as a cheap
/// gate and a memoization key, never as the answer on its own: those over-report, because opening a
/// document in the UI Builder, undo/redo, and driven properties all bump the dirty count without changing
/// the exported content. This is the mechanism previously embedded in <c>VisualElementEditingStage</c>,
/// lifted out so every authoring tool shares one definition of "dirty".
/// </remarks>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed class UIAssetDirtyTracker
{
    readonly Dictionary<EntityId, AssetBaseline> m_Baselines = new();

    /// <summary>Number of assets that currently have a captured baseline.</summary>
    public int Count => m_Baselines.Count;

    /// <summary>
    /// Returns whether the asset's exported content differs from its captured clean baseline. Assets that
    /// are not flagged dirty by the engine, or that export identically to their baseline, are reported clean.
    /// </summary>
    public bool IsModified(EntityId entityId, UnityEngine.Object asset)
    {
        if (asset == null)
            return false;

        // Cheap gate: if the engine does not even flag the asset dirty, there is nothing to compare.
        if (!EditorUtility.IsDirty(asset))
            return false;

        // Flagged dirty but never baselined: treat as modified (the caller has not vouched for its content).
        if (!m_Baselines.TryGetValue(entityId, out var cache))
            return true;

        // Memoize against the dirty counter so repeated polls (the stage title "*") don't re-export every frame.
        var dirtyCount = EditorUtility.GetDirtyCount(entityId);
        if (dirtyCount == cache.LastDirtyCount)
            return cache.LastResult;

        cache.LastDirtyCount = dirtyCount;
        cache.LastResult = ComputeContentHash(asset) != cache.Hash;
        m_Baselines[entityId] = cache;
        return cache.LastResult;
    }

    /// <summary>Convenience overload that resolves the entity id from the asset.</summary>
    public bool IsModified(UnityEngine.Object asset)
        => asset != null && IsModified(asset.GetEntityId(), asset);

    /// <summary>
    /// <see cref="IsModified(EntityId, UnityEngine.Object)"/> plus the exported text it had to produce anyway,
    /// for callers that need both — capturing an unsaved snapshot — without serializing the asset twice.
    /// <paramref name="text"/> is null when the asset is unmodified.
    /// </summary>
    public bool TryGetModifiedContent(EntityId entityId, UnityEngine.Object asset, out string text)
    {
        text = null;
        if (asset == null || !EditorUtility.IsDirty(asset))
            return false;

        var content = ComputeContentText(asset);
        if (!m_Baselines.TryGetValue(entityId, out var cache))
        {
            // Flagged dirty but never baselined: treat as modified, matching IsModified.
            text = content;
            return true;
        }

        var modified = (content == null ? default : Hash128.Compute(content)) != cache.Hash;

        // Refresh the memo, so a following IsModified poll does not re-export for the same answer.
        cache.LastDirtyCount = EditorUtility.GetDirtyCount(entityId);
        cache.LastResult = modified;
        m_Baselines[entityId] = cache;

        if (modified)
            text = content;
        return modified;
    }

    /// <summary>
    /// Records the asset's current exported content as its clean baseline. Captures even when the asset is
    /// already <see cref="EditorUtility.IsDirty"/> (opening a document dirties it without a content change),
    /// so that <see cref="IsModified(EntityId, UnityEngine.Object)"/> can compare content and report it
    /// unchanged until it actually differs.
    /// </summary>
    public void Capture(UnityEngine.Object asset)
    {
        if (asset == null)
            return;

        var entityId = asset.GetEntityId();
        m_Baselines[entityId] = new AssetBaseline
        {
            AssetId = GlobalObjectId.GetGlobalObjectIdSlow(asset),
            EntityId = entityId,
            Hash = ComputeContentHash(asset),
            LastDirtyCount = EditorUtility.GetDirtyCount(entityId),
            LastResult = false,
        };
    }

    public bool HasBaseline(EntityId entityId) => m_Baselines.ContainsKey(entityId);

    /// <summary>
    /// The durable id recorded with the baseline, for callers that must persist something keyed to the asset
    /// after its (session-local) entry is gone.
    /// </summary>
    public bool TryGetGlobalId(EntityId entityId, out GlobalObjectId globalId)
    {
        if (m_Baselines.TryGetValue(entityId, out var baseline))
        {
            globalId = baseline.AssetId;
            return true;
        }

        globalId = default;
        return false;
    }

    public void Remove(EntityId entityId) => m_Baselines.Remove(entityId);

    /// <summary>
    /// Re-attaches a persisted baseline after a domain reload. The asset is resolved from the serialized
    /// <see cref="AssetBaseline.AssetId"/> and re-keyed by its (session-local) entity id, recomputing the
    /// memoized result against the restored hash.
    /// </summary>
    /// <remarks>
    /// Must not run inside a deserialize callback: <see cref="GlobalObjectId.GlobalObjectIdentifierToObjectSlow"/>
    /// cannot resolve there. Callers defer restoration to a later editor tick.
    /// </remarks>
    public void Restore(in AssetBaseline serialized)
    {
        var asset = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(serialized.AssetId);
        if (asset == null)
            asset = ResolveInMemoryAsset(serialized.EntityId);
        if (asset == null)
            return;

        var entityId = asset.GetEntityId();
        m_Baselines[entityId] = new AssetBaseline
        {
            AssetId = serialized.AssetId,
            EntityId = entityId,
            Hash = serialized.Hash,
            LastDirtyCount = EditorUtility.GetDirtyCount(entityId),
            LastResult = EditorUtility.IsDirty(asset) && ComputeContentHash(asset) != serialized.Hash,
        };
    }

    // An asset that only lives in memory, such as a document that was never saved, keeps its entity id across a
    // domain reload. A persistent asset is never resolved this way: its GlobalObjectId already names it.
    static UnityEngine.Object ResolveInMemoryAsset(EntityId entityId)
    {
        if (entityId == EntityId.None)
            return null;

        var asset = EditorUtility.EntityIdToObject(entityId);
        return asset is VisualTreeAsset or StyleSheet && !EditorUtility.IsPersistent(asset) ? asset : null;
    }

    /// <summary>Copies the current baselines into <paramref name="results"/> for serialization. Clears it first.</summary>
    public void Serialize(List<AssetBaseline> results)
    {
        results.Clear();
        foreach (var baseline in m_Baselines.Values)
            results.Add(baseline);
    }

    /// <summary>
    /// The asset's exported on-disk text — exactly what a save would write. UXML for a VisualTreeAsset, USS
    /// for a StyleSheet; null for an unsupported type.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static string ComputeContentText(UnityEngine.Object asset)
    {
        switch (asset)
        {
            case VisualTreeAsset vta:
                return VisualTreeAssetExporter.Default.ToUxmlString(vta);
            case StyleSheet styleSheet:
                return StyleSheetExporter.Default.ToUssString(styleSheet);
            default:
                return null;
        }
    }

    /// <summary>
    /// Hashes the asset's exported on-disk representation. Two assets with equal hashes would produce
    /// identical files.
    /// </summary>
    internal static Hash128 ComputeContentHash(UnityEngine.Object asset)
    {
        var text = ComputeContentText(asset);
        return text == null ? default : Hash128.Compute(text);
    }
}
