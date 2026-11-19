// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The editor selection, as seen by <see cref="AuthoringContext.Global"/>.
/// </summary>
sealed class GlobalAuthoringSelection : IAuthoringSelection
{
    public event Action Changed
    {
        add => Selection.selectionChanged += value;
        remove => Selection.selectionChanged -= value;
    }

    public UnityEngine.Object ActiveObject
    {
        get => Selection.activeObject;
        set => Selection.activeObject = value;
    }

    public EntityId ActiveEntityId
    {
        get => Selection.activeEntityId;
        set => Selection.activeEntityId = value;
    }

    public EntityId[] EntityIds
    {
        get => Selection.entityIds;
        set => Selection.entityIds = value ?? Array.Empty<EntityId>();
    }

    public UnityEngine.Object[] Objects => Selection.objects;

    public bool Contains(EntityId entityId) => Selection.Contains(entityId);

    public void Add(EntityId entityId) => Selection.Add(entityId);

    public void Remove(EntityId entityId) => Selection.Remove(entityId);

    public void Clear() => Selection.entityIds = Array.Empty<EntityId>();
}
