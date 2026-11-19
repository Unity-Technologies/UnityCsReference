// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// A copy of what a <see cref="LocalAuthoringSelection"/> holds, in an object the undo system records, so that
/// undo and redo bring a local selection back the way they do the editor selection.
/// </summary>
/// <remarks>
/// It belongs to whoever outlives the selections it serves: a window builds a new context after every domain
/// reload, and hands the new selection the same object so that the recorded history still applies.
/// </remarks>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed class LocalSelectionUndoState : ScriptableObject
{
    [SerializeField]
    EntityId[] m_EntityIds = Array.Empty<EntityId>();

    [SerializeField]
    EntityId m_ActiveEntityId;

    public EntityId[] EntityIds => m_EntityIds;

    public EntityId ActiveEntityId => m_ActiveEntityId;

    public static LocalSelectionUndoState Create()
    {
        var state = CreateInstance<LocalSelectionUndoState>();
        state.hideFlags = HideFlags.HideAndDontSave;
        return state;
    }

    public void Write(List<EntityId> entityIds, EntityId activeEntityId)
    {
        m_EntityIds = entityIds.ToArray();
        m_ActiveEntityId = activeEntityId;
    }
}
