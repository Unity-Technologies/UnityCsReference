// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

static class EntityIdExtensions
{
    /// <summary>
    /// The loaded objects for <paramref name="entityIds"/>, plus each unloaded one that <paramref name="mayMatch"/>
    /// accepts, which is loaded. <see cref="Selection.objects"/> loads every selected asset, thousands in a large
    /// Project window selection (UUM-148632).
    /// </summary>
    public static IEnumerable<Object> ResolveObjects(this IEnumerable<EntityId> entityIds, Func<EntityId, bool> mayMatch)
    {
        foreach (var entityId in entityIds)
        {
            var obj = InternalEditorUtility.GetLoadedObjectFromEntityId(entityId);
            if (obj == null && mayMatch(entityId))
                obj = EditorUtility.EntityIdToObject(entityId);
            if (obj != null)
                yield return obj;
        }
    }

    /// <summary>
    /// False only when the import metadata says <paramref name="entityId"/> is a main asset that is not a GameObject.
    /// A prefab's Components and children are sub-assets, which the metadata does not classify, so they pass.
    /// So does any id the metadata cannot classify.
    /// </summary>
    public static bool MayBeGameObjectOrComponent(EntityId entityId)
    {
        // A main asset is never a Component, so its native type tells GameObject from ScriptableObject and the rest.
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(entityId, out _, out long _) || !AssetDatabase.IsMainAsset(entityId))
            return true;
        var type = InternalEditorUtility.GetTypeWithoutLoadingObject(entityId);
        return type == null || typeof(GameObject).IsAssignableFrom(type);
    }
}
