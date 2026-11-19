// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// How a tool holds an open asset. Read-only holders never contribute to dirtiness and never trigger a
/// save prompt on close; read-write holders do.
/// </summary>
enum UIAssetAccess
{
    ReadOnly,
    ReadWrite,
}

/// <summary>The kind of authoring asset an entry tracks.</summary>
enum UIAssetKind
{
    VisualTreeAsset,
    StyleSheet,
}

/// <summary>
/// A single open asset tracked by <see cref="UIAssetRegistry"/>: its identity, its cached path, and the set
/// of tools (owners) that currently hold it open and in which access mode. The dirty baseline itself lives
/// in the registry's <see cref="UIAssetDirtyTracker"/>, keyed by the same <see cref="Id"/>.
/// </summary>
sealed class AssetEntry
{
    public EntityId Id;
    public GlobalObjectId GlobalId;
    public UIAssetKind Kind;

    // The live asset. Kept so the registry can operate on the object without re-resolving from an id, and
    // so CloseAll/GetDirtyAssets can enumerate. A destroyed asset becomes a fake-null reference, which the
    // callers tolerate.
    public UnityEngine.Object Asset;

    // Cached because EntityId is dead after a delete and only the path can reconcile the entry afterwards.
    // Refreshed lazily on rename/move.
    public string AssetPath;

    public readonly Dictionary<object, UIAssetAccess> ReferencesByOwner = new(OwnerReferenceComparer.Instance);

    // Number of owners holding this asset ReadWrite, maintained incrementally so close is O(1).
    public int ReadWriteCount;

    // Last dirtiness the registry observed for this asset, so dirty-state-changed is raised only on a
    // transition instead of on every command.
    public bool WasDirty;
}

/// <summary>
/// Equality by reference identity, bypassing any overridden <c>Equals</c>/<c>GetHashCode</c> (notably
/// <see cref="UnityEngine.Object"/>'s). Owner tokens are matched by "is it the very same object", so a
/// destroyed <see cref="UnityEngine.Object"/> owner still matches its own entry for cleanup.
/// </summary>
sealed partial class OwnerReferenceComparer : IEqualityComparer<object>
{
    [NoAutoStaticsCleanup]
    public static readonly OwnerReferenceComparer Instance = new();

    bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);

    int IEqualityComparer<object>.GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
}
