// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// A selection owned by one local <see cref="AuthoringContext"/>. It never reaches <see cref="Selection"/>.
/// </summary>
/// <remarks>
/// Unlike the editor selection, <see cref="Changed"/> is raised synchronously, once per change. Changes are
/// recorded for undo only when the selection has an <see cref="UndoState"/>.
/// </remarks>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed class LocalAuthoringSelection : IAuthoringSelection
{
    static readonly string k_UndoName = L10n.Tr("Selection Change", null);

    readonly List<EntityId> m_EntityIds = new();
    EntityId m_ActiveEntityId;
    LocalSelectionUndoState m_UndoState;
    int m_UndoSuspensionCount;

    public event Action Changed;

    public LocalAuthoringSelection()
    {
    }

    public LocalAuthoringSelection(LocalSelectionUndoState undoState)
    {
        UndoState = undoState;
    }

    /// <summary>
    /// The object every change is recorded in for undo, and that undo and redo bring the selection back from, or
    /// <see langword="null"/> when the selection is not undoable.
    /// </summary>
    /// <remarks>
    /// The object takes the current selection without recording it: the history recorded before still applies.
    /// </remarks>
    public LocalSelectionUndoState UndoState
    {
        get => m_UndoState;
        set
        {
            if (ReferenceEquals(m_UndoState, value))
                return;

            if (ReferenceEquals(m_UndoState, null))
                Undo.undoRedoPerformed += OnUndoRedoPerformed;
            else if (ReferenceEquals(value, null))
                Undo.undoRedoPerformed -= OnUndoRedoPerformed;

            m_UndoState = value;
            if (m_UndoState != null)
                m_UndoState.Write(m_EntityIds, m_ActiveEntityId);
        }
    }

    public UnityEngine.Object ActiveObject
    {
        get => EditorUtility.EntityIdToObject(m_ActiveEntityId);
        set => ActiveEntityId = value != null ? value.GetEntityId() : EntityId.None;
    }

    public EntityId ActiveEntityId
    {
        get => m_ActiveEntityId;
        set
        {
            if (value == EntityId.None)
            {
                Clear();
                return;
            }

            if (m_ActiveEntityId == value && m_EntityIds.Count == 1)
                return;

            m_EntityIds.Clear();
            m_EntityIds.Add(value);
            m_ActiveEntityId = value;
            OnChanged();
        }
    }

    public EntityId[] EntityIds
    {
        get => m_EntityIds.ToArray();
        set => Set(value ?? Array.Empty<EntityId>());
    }

    public UnityEngine.Object[] Objects
    {
        get
        {
            var objects = new List<UnityEngine.Object>(m_EntityIds.Count);
            foreach (var entityId in m_EntityIds)
            {
                var obj = EditorUtility.EntityIdToObject(entityId);
                if (obj != null)
                    objects.Add(obj);
            }
            return objects.ToArray();
        }
    }

    public bool Contains(EntityId entityId) => m_EntityIds.Contains(entityId);

    public void Add(EntityId entityId)
    {
        if (entityId == EntityId.None || m_EntityIds.Contains(entityId))
            return;

        m_EntityIds.Add(entityId);
        UpdateActive();
        OnChanged();
    }

    public void Remove(EntityId entityId)
    {
        if (!m_EntityIds.Remove(entityId))
            return;

        UpdateActive();
        OnChanged();
    }

    public void Clear()
    {
        if (m_EntityIds.Count == 0 && m_ActiveEntityId == EntityId.None)
            return;

        m_EntityIds.Clear();
        m_ActiveEntityId = EntityId.None;
        OnChanged();
    }

    /// <summary>
    /// Drops the selected objects that were destroyed, like the editor selection does, such as the selection
    /// objects of deleted elements.
    /// </summary>
    /// <remarks>
    /// Not recorded for undo: nothing was chosen, and undoing what destroyed them does not bring them back.
    /// </remarks>
    public void RemoveDestroyedObjects()
    {
        if (m_EntityIds.RemoveAll(entityId => EditorUtility.EntityIdToObject(entityId) == null) == 0)
            return;

        UpdateActive();
        using (SuspendUndo())
            OnChanged();
    }

    /// <summary>
    /// Stops recording changes for undo until the returned scope is disposed, for changes that bring back a
    /// selection rather than make one, such as restoring it after a domain reload.
    /// </summary>
    public UndoSuspensionScope SuspendUndo() => new(this);

    void Set(EntityId[] entityIds)
    {
        var previousActive = m_ActiveEntityId;
        using var _ = ListPool<EntityId>.Get(out var previous);
        previous.AddRange(m_EntityIds);

        m_EntityIds.Clear();
        foreach (var entityId in entityIds)
        {
            if (entityId != EntityId.None && !m_EntityIds.Contains(entityId))
                m_EntityIds.Add(entityId);
        }
        UpdateActive();

        // Compared as sets, like the editor selection: reordering the same ids is not a change.
        var changed = previousActive != m_ActiveEntityId || previous.Count != m_EntityIds.Count;
        for (var i = 0; !changed && i < previous.Count; i++)
            changed = !m_EntityIds.Contains(previous[i]);

        if (changed)
            OnChanged();
    }

    void UpdateActive()
    {
        if (m_EntityIds.Count == 0)
            m_ActiveEntityId = EntityId.None;
        else if (m_ActiveEntityId == EntityId.None || !m_EntityIds.Contains(m_ActiveEntityId))
            m_ActiveEntityId = m_EntityIds[0];
    }

    void OnChanged()
    {
        // The undo state still holds the previous selection, which is what undo brings back. Recorded whole: it is
        // also written without recording, and a diff would then apply over a selection it was not taken from.
        if (m_UndoState != null)
        {
            if (m_UndoSuspensionCount == 0)
                Undo.RegisterCompleteObjectUndo(m_UndoState, k_UndoName);
            m_UndoState.Write(m_EntityIds, m_ActiveEntityId);
        }

        Changed?.Invoke();
    }

    void OnUndoRedoPerformed()
    {
        if (m_UndoState == null)
            return;

        var entityIds = m_UndoState.EntityIds;
        var activeEntityId = m_UndoState.ActiveEntityId;
        if (activeEntityId == m_ActiveEntityId && SameEntityIds(entityIds))
            return;

        // Objects destroyed since the change was recorded, such as the elements of a closed document, are left out.
        m_EntityIds.Clear();
        foreach (var entityId in entityIds)
        {
            if (entityId != EntityId.None && !m_EntityIds.Contains(entityId) &&
                EditorUtility.EntityIdToObject(entityId) != null)
            {
                m_EntityIds.Add(entityId);
            }
        }
        m_ActiveEntityId = activeEntityId;
        UpdateActive();

        m_UndoState.Write(m_EntityIds, m_ActiveEntityId);
        Changed?.Invoke();
    }

    bool SameEntityIds(EntityId[] entityIds)
    {
        if (entityIds.Length != m_EntityIds.Count)
            return false;

        for (var i = 0; i < entityIds.Length; ++i)
        {
            if (entityIds[i] != m_EntityIds[i])
                return false;
        }

        return true;
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    public readonly struct UndoSuspensionScope : IDisposable
    {
        readonly LocalAuthoringSelection m_Selection;

        internal UndoSuspensionScope(LocalAuthoringSelection selection)
        {
            m_Selection = selection;
            ++m_Selection.m_UndoSuspensionCount;
        }

        public void Dispose()
        {
            if (m_Selection != null)
                --m_Selection.m_UndoSuspensionCount;
        }
    }
}
