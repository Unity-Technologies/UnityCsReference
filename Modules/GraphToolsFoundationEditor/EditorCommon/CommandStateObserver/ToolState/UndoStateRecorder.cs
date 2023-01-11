// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Class used to serialize and deserialize state components for undo operations and keep track of changesets associated with them.
    /// </summary>
    [Serializable]
    class UndoStateRecorder : ScriptableObject, ISerializationCallbackReceiver
    {
        class SerializedChangeset
        {
            string m_TypeName;
            string m_Data;

            public SerializedChangeset(IChangeset changeset)
            {
                m_TypeName = changeset?.GetType().AssemblyQualifiedName ?? "";
                m_Data = JsonUtility.ToJson(changeset);
            }

            public IChangeset GetChangeset()
            {
                var changesetType = Type.GetType(m_TypeName);
                var changeset = JsonUtility.FromJson(m_Data, changesetType) as IChangeset;
                return changeset;
            }
        }

        class OperationRecord
        {
            public uint OperationId { get; }
            public List<(SerializableGUID, uint)> ToNextChangesets { get; } = new();
            public List<(SerializableGUID, uint)> FromPreviousChangesets { get; } = new();

            public OperationRecord(uint operationId)
            {
                OperationId = operationId;
            }
        }

        static Dictionary<(SerializableGUID, uint), SerializedChangeset> s_ToNextVersionChangesets = new();
        static Dictionary<(SerializableGUID, uint), SerializedChangeset> s_FromPreviousVersionChangesets = new();
        static List<OperationRecord> s_Operations = new();
        static uint s_LastOperationId;

        // For use in tests, to get a clean state.
        internal static void PurgeAllChangesets_Internal()
        {
            s_ToNextVersionChangesets.Clear();
            s_FromPreviousVersionChangesets.Clear();
            s_Operations.Clear();
            s_LastOperationId = 0;
        }

        internal const int maxItems = 100;
        internal const int purgeTrigger = 200;

        static void PurgeOldChangesets()
        {
            if (s_Operations.Count >= purgeTrigger)
            {
                for (var i = 0; i < s_Operations.Count - maxItems; i++)
                {
                    var operation = s_Operations[i];
                    foreach (var k in operation.ToNextChangesets)
                    {
                        s_ToNextVersionChangesets.Remove(k);
                    }
                    foreach (var k in operation.FromPreviousChangesets)
                    {
                        s_FromPreviousVersionChangesets.Remove(k);
                    }
                }

                s_Operations.RemoveRange(0, s_Operations.Count - maxItems);
            }
        }

        static void RemoveOperation(int index)
        {
            foreach (var k in s_Operations[index].ToNextChangesets)
            {
                s_ToNextVersionChangesets.Remove(k);
            }
            foreach (var k in s_Operations[index].FromPreviousChangesets)
            {
                s_FromPreviousVersionChangesets.Remove(k);
            }
            s_Operations.RemoveAt(index);
        }

        [SerializeField]
        uint m_OperationId;

        [SerializeField]
        List<string> m_StateComponentTypeNames;

        [SerializeField]
        List<string> m_SerializedState;

        List<IUndoableStateComponent> m_StateComponents;
        List<uint> m_StateComponentVersions;

        bool m_DontSerialize;
        bool m_NeedToRestore;

        int m_UndoGroup;
        string m_UndoString;

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoStateRecorder"/> class.
        /// </summary>
        public UndoStateRecorder()
        {
            m_StateComponentTypeNames = new List<string>();
            m_SerializedState = new List<string>();
            m_StateComponents = new List<IUndoableStateComponent>();
            m_StateComponentVersions = new List<uint>();
        }

        void Clear()
        {
            m_StateComponentTypeNames.Clear();
            m_SerializedState.Clear();
            m_StateComponents.Clear();
            m_StateComponentVersions.Clear();
            m_UndoString = null;
        }

        /// <summary>
        /// Prepares the object for receiving <see cref="IUndoableStateComponent"/>s to record.
        /// </summary>
        /// <param name="undoString">The name of the operation, which will be displayed in the undo menu.</param>
        public void BeginRecording(string undoString)
        {
            if (m_UndoString != null)
            {
                Debug.LogError("Unbalanced UndoData usage.");
            }

            Clear();

            // When an operation is done after one or more undos, remove operations that will be pruned.
            if (m_OperationId < s_LastOperationId)
            {
                int i = s_Operations.Count - 1;
                while (i >= 0 && s_Operations[i].OperationId > m_OperationId)
                {
                    RemoveOperation(i--);
                }
            }

            m_OperationId = ++s_LastOperationId;
            m_UndoString = undoString;
        }

        /// <summary>
        /// Saves a state component before it is modified by an undoable operation.
        /// </summary>
        /// <param name="stateComponent">The state component to save.</param>
        public void RecordComponent(IUndoableStateComponent stateComponent)
        {
            if (stateComponent == null)
                return;

            if (m_StateComponents.Count == 0)
            {
                Undo.IncrementCurrentGroup();
                m_UndoGroup = Undo.GetCurrentGroup();
            }

            if (!m_StateComponents.Contains(stateComponent))
            {
                var typeName = stateComponent.GetType().AssemblyQualifiedName;
                m_StateComponentTypeNames.Add(typeName);
                m_StateComponents.Add(stateComponent);
                m_StateComponentVersions.Add(stateComponent.CurrentVersion);

                stateComponent.WillPushOnUndoStack(m_UndoString);

                // It is important that the state component be serialized immediately, to capture its state before any change occurs.
                var data = JsonUtility.ToJson(stateComponent);
                m_SerializedState.Add(data);
            }
        }

        /// <summary>
        /// Saves state components before they are modified by an undoable operation.
        /// </summary>
        /// <param name="stateComponents">The state components to save.</param>
        public void RecordComponents(IEnumerable<IUndoableStateComponent> stateComponents)
        {
            foreach (var stateComponent in stateComponents)
            {
                RecordComponent(stateComponent);
            }
        }

        /// <summary>
        /// Signals the end of a recording. Sends the undo data to the editor undo system.
        /// </summary>
        /// <param name="stateComponents">Additional state components to record; they will be recorded only if there already was other recorded state components.</param>
        public void EndRecording(params IUndoableStateComponent[] stateComponents)
        {
            if (m_StateComponents.Count > 0)
            {
                if (stateComponents != null)
                {
                    RecordComponents(stateComponents);
                }

                var operation = new OperationRecord(m_OperationId);
                for (var i = 0; i < m_StateComponents.Count; i++)
                {
                    var component = m_StateComponents[i];
                    var changeset = component.ChangesetManager?.GetAggregatedChangeset(m_StateComponentVersions[i], component.CurrentVersion);
                    var serializedChangeset = new SerializedChangeset(changeset);
                    s_ToNextVersionChangesets.TryAdd((component.Guid, m_StateComponentVersions[i]), serializedChangeset);
                    s_FromPreviousVersionChangesets.TryAdd((component.Guid, component.CurrentVersion), serializedChangeset);

                    operation.ToNextChangesets.Add((component.Guid, m_StateComponentVersions[i]));
                    operation.FromPreviousChangesets.Add((component.Guid, component.CurrentVersion));
                }
                s_Operations.Add(operation);

                // Components were already serialized in the AddComponent call. The Undo.RegisterCompleteObjectUndo
                // will trigger a call to OnBeforeSerialize, but we do not want to overwrite serialized state
                // components because state components have been updated by the undoable operation and thus
                // do not reflect the state *before* the operation.
                m_DontSerialize = true;

                // Although we want to serialize state components in the AddComponent call,
                // we want to call RegisterCompleteObjectUndo() only now because at this point we know
                // that AddComponent will not be called again.
                Undo.RegisterCompleteObjectUndo(this, m_UndoString);
                Undo.CollapseUndoOperations(m_UndoGroup);
            }

            m_UndoString = null;

            PurgeOldChangesets();
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
            // This is automatically called by the undo system
            // to serialize the current state of the ScriptableObjects.
            //
            // It will be called:
            // - when RegisterCompleteObjectUndo is called (in EndUndoableOperation); in this case,
            //   state components were already serialized and we do not want to overwrite the data.
            // - when Ctrl-Z is pressed or when CollapseUndoOperations is called, to be able to get back to
            //   the current state on a Redo operation. In this case, we want to serialize the current state.

            if (!m_DontSerialize)
            {
                m_SerializedState.Clear();
                m_StateComponentTypeNames.Clear();
                foreach (var stateComponent in m_StateComponents)
                {
                    var fullTypeName = stateComponent.GetType().AssemblyQualifiedName;
                    m_StateComponentTypeNames.Add(fullTypeName);

                    var serializedState = JsonUtility.ToJson(stateComponent);
                    m_SerializedState.Add(serializedState);
                }
            }
            else
            {
                m_DontSerialize = false;
            }
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
            m_NeedToRestore = true;
        }

        /// <summary>
        /// Restores state components with the data serialized.
        /// </summary>
        /// <param name="state">The state that holds the state components to restore.</param>
        /// <param name="isRedo">True if the context of this call is a redo operation; false if it is an undo operation.</param>
        public void RestoreState(IState state, bool isRedo)
        {
            var modifiedStateComponents = new List<IUndoableStateComponent>();

            if (m_NeedToRestore) // If false, there was an undo operation, but we were not involved in it.
            {
                for (var i = 0; i < m_SerializedState.Count; i++)
                {
                    var componentTypeName = m_StateComponentTypeNames[i];
                    var componentType = Type.GetType(componentTypeName);

                    if (JsonUtility.FromJson(m_SerializedState[i], componentType) is IUndoableStateComponent newStateComponent)
                    {
                        var stateComponent =
                            state.AllStateComponents.OfType<IUndoableStateComponent>().FirstOrDefault(c => c.Guid == newStateComponent.Guid);

                        if (stateComponent != null)
                        {
                            IChangeset changeset;

                            if (isRedo)
                            {
                                s_FromPreviousVersionChangesets.TryGetValue((newStateComponent.Guid, newStateComponent.CurrentVersion), out var serializedChangeset);
                                changeset = serializedChangeset?.GetChangeset();
                            }
                            else
                            {
                                s_ToNextVersionChangesets.TryGetValue((newStateComponent.Guid, newStateComponent.CurrentVersion), out var serializedChangeset);
                                changeset = serializedChangeset?.GetChangeset();
                                if (changeset != null && !changeset.Reverse())
                                {
                                    changeset = null;
                                }
                            }

                            stateComponent.ApplyUndoData(newStateComponent, changeset);
                            modifiedStateComponents.Add(stateComponent);
                        }
                    }
                }
            }

            foreach (var component in modifiedStateComponents)
            {
                component.UndoRedoPerformed(isRedo);
            }

            m_SerializedState.Clear();
            m_StateComponentTypeNames.Clear();
            m_NeedToRestore = false;
        }
    }
}
