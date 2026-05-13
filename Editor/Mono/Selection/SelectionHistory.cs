// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [Serializable]
    internal class SelectionEntry : IEquatable<SelectionEntry>
    {
        [SerializeField] internal EntityId[] m_SelectedEids;
        [SerializeField] internal SceneHandle[] m_Scenes; // scene handles that the selected objects were part of
        [SerializeField] internal string m_CustomKey; // custom selection handler key (i.e. which handler to invoke)
        [SerializeField] internal string m_CustomData;
        [SerializeField] internal bool m_CustomUpdateObjectSelection;
        [SerializeField] internal EntityId m_CustomActiveContextEid;
        [SerializeField] internal DataMode m_CustomDataMode;
        [SerializeField] internal int m_UndoGroup; // "current undo group" at the time when entry was made, used to collapse entries added during the same undo group
        [SerializeField] internal string m_FocusedWindowClass;
        [SerializeField] internal EntityId m_FocusedWindowEid;

        internal SelectionEntry(ReadOnlySpan<EntityId> selectedEids, string customKey, string customData, bool customUpdateObjectSelection,
            EntityId customActiveContextEid = default, DataMode customDataMode = DataMode.Disabled)
        {
            m_UndoGroup = Undo.GetCurrentGroup();

            if (!selectedEids.IsEmpty)
            {
                m_SelectedEids = new EntityId[selectedEids.Length];

                using (HashSetPool<SceneHandle>.Get(out var scenes))
                {
                    for (var i = 0; i < selectedEids.Length; ++i)
                    {
                        var eid = selectedEids[i];
                        m_SelectedEids[i] = eid;

                        if (SceneIdFromObject(EditorUtility.EntityIdToObject(eid), out var sceneId))
                            scenes.Add(sceneId);
                    }

                    if (scenes.Count > 0)
                    {
                        m_Scenes = new SceneHandle[scenes.Count];
                        scenes.CopyTo(m_Scenes);
                    }
                }
            }
            else
            {
                m_SelectedEids = Array.Empty<EntityId>();
            }

            m_CustomKey = customKey ?? string.Empty;
            m_CustomData = customData;
            m_CustomUpdateObjectSelection = customUpdateObjectSelection;

            m_CustomActiveContextEid = customActiveContextEid;
            m_CustomDataMode = customDataMode;

            GetFocusedWindow(out m_FocusedWindowClass, out m_FocusedWindowEid);
        }

        static void GetFocusedWindow(out string typeName, out EntityId eid)
        {
            typeName = null;
            eid = EntityId.None;
            var w = EditorWindow.focusedWindow;
            if (w == null) return;

            typeName = w.GetType().FullName;
            eid = w.GetEntityId();
        }

        static bool SceneIdFromObject(Object o, out SceneHandle sceneId)
        {
            if (o != null && o is GameObject go && go.scene.IsValid())
            {
                sceneId = go.scene.handle;
                return true;
            }

            sceneId = SceneHandle.None;
            return false;
        }

        internal static SelectionEntry CreateFromCurrentSelection() => new SelectionEntry(Selection.entityIds, "", "", false);

        internal bool IsSelectionEntryValid()
        {
            var isNullSelectionValid = !string.IsNullOrEmpty(m_CustomKey) && !m_CustomUpdateObjectSelection;

            if (m_SelectedEids == null) return isNullSelectionValid;

            var selectedLength = m_SelectedEids.Length;
            if (selectedLength == 0) return isNullSelectionValid;

            for (int i = 0; i < selectedLength; i++)
            {
                if (EditorUtility.IsEidValid(m_SelectedEids[i])) return true;
            }

            return false;
        }

        public bool Equals(SelectionEntry o)
        {
            // Dont compare the m_Scenes; if anything here will be different then scenes will be different too
            // Dont compare m_FocusedWindowClass as m_FocusedWindowEid covers that
            // Dont compare m_UndoGroup, as same selection with different undo group is functionally the same

            if (ReferenceEquals(null, o)) return false;
            if (ReferenceEquals(this, o)) return true;

            if (!Equals(m_FocusedWindowEid, o.m_FocusedWindowEid)) return false;
            if (!Equals(m_SelectedEids?.Length, o.m_SelectedEids?.Length)) return false;

            if (!Equals(m_CustomKey, o.m_CustomKey)) return false;
            if (!Equals(m_CustomData, o.m_CustomData)) return false;

            if (!string.IsNullOrEmpty(m_CustomKey))
            {
                // Only check these for custom entries
                if (!Equals(m_CustomActiveContextEid, o.m_CustomActiveContextEid)) return false;
                if (!Equals(m_CustomDataMode, o.m_CustomDataMode)) return false;
            }

            if (m_SelectedEids == null) return true; //We already check above if eids length is the same, so if null both will be null
            for (int i = 0; i < m_SelectedEids.Length; i++)
            {
                if (!Equals(m_SelectedEids[i], o.m_SelectedEids[i])) return false;
            }

            return true;
        }

        public override bool Equals(object o)
        {
            if (o is null) return false;
            if (ReferenceEquals(this, o)) return true;
            if (o.GetType() != GetType()) return false;
            return Equals((SelectionEntry)o);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(m_FocusedWindowEid);
            hash.Add(m_CustomKey);
            hash.Add(m_CustomData);

            if (!string.IsNullOrEmpty(m_CustomKey))
            {
                hash.Add(m_CustomActiveContextEid);
                hash.Add(m_CustomDataMode);
            }

            if (m_SelectedEids != null)
            {
                foreach (var eid in m_SelectedEids)
                {
                    hash.Add(eid);
                }
            }

            return hash.ToHashCode();
        }
    }

    internal class SelectionHistory : ScriptableSingleton<SelectionHistory>
    {
        const int kHistorySize = 50;

        [SerializeField] List<SelectionEntry> m_History = new(kHistorySize);
        [SerializeField] int m_Index;

        bool m_IgnoreNextSelectionChange;
        bool m_ApplyingCustomSelection;

        public static EditorApplication.CallbackFunction indexChanged;

        [MenuItem("Edit/Previous Selection %#[", priority = 70)]
        static void GoBackMenuItem() => instance.GoBack();
        [MenuItem("Edit/Previous Selection %#[", priority = 70, validate = true)]
        static bool ValidateGoBack() => instance.GetIndex() < instance.GetHistory().Count - 1;

        [MenuItem("Edit/Next Selection %#]", priority = 75)]
        static void GoForwardMenuItem() => instance.GoForward();
        [MenuItem("Edit/Next Selection %#]", priority = 75, validate = true)]
        static bool ValidateGoForward() => instance.GetIndex() > 0;

        void OnEnable()
        {
            EditorSceneManager.sceneClosing += OnSceneClosing;
            ObjectChangeEvents.changesPublished += OnObjectChanged;
        }

        void OnDisable()
        {
            EditorSceneManager.sceneClosing -= OnSceneClosing;
            ObjectChangeEvents.changesPublished -= OnObjectChanged;
        }

        // Used by tests
        internal bool GetIgnoreNextSelectionChange() => m_IgnoreNextSelectionChange;

        internal SelectionEntry GetCurrent() => HistoryHasValidIndex() ? m_History[m_Index] : null;

        internal List<SelectionEntry> GetHistory() => m_History;

        internal int GetIndex() => m_Index;
        void SetIndex(int newIndex) => m_Index = m_History.Count > 0 ? Math.Clamp(newIndex, 0, m_History.Count - 1) : 0;

        bool m_indexDecreasing;
        internal void GoBack()
        {
            if (m_Index >= m_History.Count - 1) return;
            m_indexDecreasing = false;
            SelectAtIndex(m_Index + 1);
            indexChanged?.Invoke();
        }

        internal void GoForward()
        {
            if (m_Index <= 0) return;
            m_indexDecreasing = true;
            SelectAtIndex(m_Index - 1);
            indexChanged?.Invoke();
        }

        bool HistoryHasValidIndex() => m_History.Count > 0 && m_Index >= 0 && m_Index < m_History.Count;

        void AddEntry(SelectionEntry entry)
        {
            if (entry == null) return;

            // If the new entry is a duplcate of the current index or a Deselect, then replace as it's "the same user interaction" and we dont want duplication
            if (HistoryHasValidIndex()
                && (entry.m_UndoGroup == m_History[m_Index].m_UndoGroup || !m_History[m_Index].IsSelectionEntryValid() || entry.Equals(m_History[m_Index])))
            {
                m_History[m_Index] = entry;
            }
            else
            {
                // If we are currently in the middle of the history list, remove everything newer than current as we have diverged
                for (int i = m_Index - 1; i >= 0; i--)
                    m_History.RemoveAt(i);

                m_History.Insert(0, entry);

                while (m_History.Count > kHistorySize)
                    m_History.RemoveAt(m_History.Count - 1);
            }

            SetIndex(0);
            indexChanged?.Invoke();
        }

        public void OnSelectionChange()
        {
            if (m_IgnoreNextSelectionChange)
            {
                m_IgnoreNextSelectionChange = false;
                return;
            }

            AddEntry(SelectionEntry.CreateFromCurrentSelection());
        }

        bool RemoveEntriesMatchingSelected(HashSet<EntityId> toRemove)
        {
            var changed = false;
            for (int i = 0; i < m_History.Count; i++)
            {
                var entry = m_History[i];

                if (entry.m_SelectedEids == null) continue;
                using (ListPool<EntityId>.Get(out var entityIDsToKeep))
                {
                    entityIDsToKeep.AddRange(entry.m_SelectedEids);
                    foreach (var selectedEid in entry.m_SelectedEids)
                    {
                        if (toRemove.Contains(selectedEid)) entityIDsToKeep.Remove(selectedEid);
                    }

                    var keepCount = entityIDsToKeep.Count;
                    if (keepCount == entry.m_SelectedEids.Length) continue;

                    if (keepCount > 0)
                    {
                        entry.m_SelectedEids = entityIDsToKeep.ToArray();
                        continue;
                    }
                }

                RemoveEntry(i);
                i--;
                changed = true;
            }

            return changed;
        }

        void RemoveEntry(int i)
        {
            // Remove the entry, and make sure to update currently selected index if needed
            m_History.RemoveAt(i);
            if (m_Index > i) SetIndex(m_Index - 1);
            else if (m_Index >= m_History.Count && m_History.Count > 0) SetIndex(m_History.Count - 1);
        }

        void OnSceneClosing(Scene scene, bool removingScene)
        {
            // Remove selection history entries that were part of the scene that's being closed
            var sceneId = scene.handle;
            var changed = false;

            for (int i = 0; i < m_History.Count; i++)
            {
                var entry = m_History[i];
                if (entry.m_Scenes == null || Array.IndexOf(entry.m_Scenes, sceneId) == -1)
                    continue;

                RemoveEntry(i);
                i--;
                changed = true;
            }
            if (changed) indexChanged?.Invoke();
        }

        void SetSelection(ReadOnlySpan<EntityId> eids, EntityId activeContextEid, DataMode dataModeHint)
        {
            // Changes to selection done via Selection API are gathered on the native side,
            // and the rest of the editor is only notified of the changes in the "next editor frame".
            // Make sure to ignore this next change notification, that would be done while we are
            // applying a selection history entry.
            // We need to get notified about reselects so we dont leak this state and miss valid events
            m_IgnoreNextSelectionChange = true;

            var activeEid = eids.Length > 0 ? eids[0] : EntityId.None;
            Selection.SetFullSelectionByID(eids, activeEid, activeContextEid, dataModeHint, true);
        }

        internal void SetCustomSelection(string key, string data, ReadOnlySpan<EntityId> selectedEids, bool updateObjectSelection,
            EntityId activeContextEid = default, DataMode dataMode = DataMode.Disabled)
        {
            if (m_ApplyingCustomSelection || m_IgnoreNextSelectionChange) return;

            if (updateObjectSelection) SetSelection(selectedEids, activeContextEid, dataMode);

            AddEntry(new SelectionEntry(selectedEids, key, data, updateObjectSelection, activeContextEid, dataMode));
        }

        void SelectAtIndex(int index)
        {
            if (index < 0 || index >= m_History.Count) return;

            SetIndex(index);

            if (!TryGetValidEntryAndFocusWindow(ref index, out var e)) return;

            RemoveSubsequentDuplicateEntries(e, ref index);

            bool updateObjectSelection = true;
            if (TryGetCustomHandler(e.m_CustomKey, out var handler))
            {
                m_ApplyingCustomSelection = true;
                try
                {
                    handler(e.m_CustomData, e.m_SelectedEids);
                }
                finally
                {
                    m_ApplyingCustomSelection = false;
                }

                updateObjectSelection = e.m_CustomUpdateObjectSelection;
            }

            if (updateObjectSelection) SetSelection(e.m_SelectedEids, e.m_CustomActiveContextEid, e.m_CustomDataMode);

            // After first going back in history, see if 0th object is a deselect and remove it from history to avoid button clicks that lead to nothing
            if (index == 1 && !m_History[0].IsSelectionEntryValid()) RemoveEntry(0);
        }

        void RemoveSubsequentDuplicateEntries(SelectionEntry e, ref int index)
        {
            // Remove duplicates that have accumulated to avoid many button presses that seemingly do nothing
            if (m_indexDecreasing)
            {
                var prevIndex = index + 1;
                var removeCount = 0;
                // Check if the element we are coming from is part of the chain of duplicates
                if (prevIndex < m_History.Count && e.Equals(m_History[prevIndex]))
                    m_History.RemoveAt(prevIndex);

                for (int i = index - 1; i >= 0; i--)
                {
                    if (!e.Equals(m_History[i])) break;

                    m_History.RemoveAt(i);
                    removeCount++;
                }
                if (removeCount > 0)
                {
                    SetIndex(index - removeCount);
                    index -= removeCount;
                }
            }
            else
            {
                var prevIndex = index - 1;
                var removeCount = 0;
                // Check if the element we are coming from is part of the chain of duplicates
                if (prevIndex >= 0 && e.Equals(m_History[prevIndex]))
                {
                    m_History.RemoveAt(prevIndex);
                    index--;
                    removeCount++;
                }

                for (int i = index + 1; i < m_History.Count; i++)
                {
                    if (!e.Equals(m_History[i])) break;

                    m_History.RemoveAt(i);
                    i--;
                    removeCount++;
                }

                if (removeCount > 0) SetIndex(index);
            }
        }

        bool TryGetValidEntryAndFocusWindow(ref int index, out SelectionEntry entry)
        {
            var windowFocused = false;
            var decreasing = m_indexDecreasing;
            entry = null;
            var changed = false;
            while (index < m_History.Count && index >= 0)
            {
                entry = m_History[index];

                var validWindow = TryGetWindow(entry, out var window);

                // A dead selection can remain in history, i.e references to Destroyed ScriptableObjects.
                // We never want to selection action to do "nothing" so move to the next entry if this is invalid
                bool validSelection;
                if (TryGetCustomValidator(entry.m_CustomKey, out var validator))
                    validSelection = entry.IsSelectionEntryValid() && validator(entry.m_CustomData, window);
                else
                    validSelection = entry.IsSelectionEntryValid();

                // Accept valid selection if it has no window but is not custom as the inspector will still respond
                if (validSelection && (validWindow || string.IsNullOrEmpty(entry.m_CustomKey)))
                {
                    // A valid window doesnt always mean that the window is not null!
                    if (validWindow) window?.Focus();
                    windowFocused = true;
                    break;
                }

                changed = true;

                // Entry is invalid so we assume that this cannot be replayed and should be evicted from history
                m_History.RemoveAt(index);

                // Only change the index when moving toward 0, as removing when moving toward list count "brings the elements down" to you
                if (decreasing) index -= 1;
            }

            if (changed)
            {
                if (m_Index != index) SetIndex(index);
                indexChanged?.Invoke();
            }

            return windowFocused;
        }

        bool TryGetWindow(SelectionEntry entry, out EditorWindow window)
        {
            window = null;
            if (string.IsNullOrEmpty(entry.m_FocusedWindowClass)) return true; // No Focused window when Entry was recorded, therefore null is valid

            var windows = EditorWindow.activeEditorWindows;
            EditorWindow similarWindow = null;

            foreach (var win in windows)
            {
                if (win.GetEntityId() == entry.m_FocusedWindowEid)
                {
                    window = win;
                    return true;
                }

                if (win.GetType().FullName.Equals(entry.m_FocusedWindowClass, StringComparison.Ordinal))
                {
                    similarWindow = win;
                }
            }

            if (similarWindow != null)
            {
                window = similarWindow;
                entry.m_FocusedWindowEid = similarWindow.GetEntityId();
                return true;
            }

            return false;
        }

        void OnObjectChanged(ref ObjectChangeEventStream stream)
        {
            using (HashSetPool<EntityId>.Get(out var instanceIDsToRemove))
            {
                for (int i = 0; i < stream.length; ++i)
                {
                    var type = stream.GetEventType(i);
                    if (type == ObjectChangeKind.DestroyGameObjectHierarchy)
                    {
                        stream.GetDestroyGameObjectHierarchyEvent(i, out var e);
                        instanceIDsToRemove.Add(e.entityId);
                    }
                    else if (type == ObjectChangeKind.DestroyAssetObject)
                    {
                        stream.GetDestroyAssetObjectEvent(i, out var e);
                        instanceIDsToRemove.Add(e.entityId);
                    }
                }

                if (RemoveEntriesMatchingSelected(instanceIDsToRemove)) indexChanged?.Invoke();
            }
        }

        internal void RemoveEntriesWithCustomKey(string key)
        {
            var changed = false;
            for (int i = m_History.Count - 1; i >= 0 ; i--)
            {
                if (m_History[i].m_CustomKey.Equals(key))
                {
                    RemoveEntry(i);
                    changed = true;
                }
            }

            if (changed) indexChanged?.Invoke();
        }

        internal void RemoveEntriesWithGUIDOrID(EntityId eid)
        {
            using (HashSetPool<EntityId>.Get(out var instanceIDsToRemove))
            {
                instanceIDsToRemove.Add(eid);
                if (RemoveEntriesMatchingSelected(instanceIDsToRemove))
                    indexChanged?.Invoke();
            }
        }

        internal void ClearHistory()
        {
            // Used by tests
            m_History.Clear();
            m_Index = 0;
            indexChanged?.Invoke();
        }

        readonly Dictionary<string, Action<string, EntityId[]>> m_CustomHandlers = new();
        readonly Dictionary<string, Func<string, EditorWindow, bool>> m_CustomValidators = new();
        internal void RegisterCustomHandler(string key, Action<string, EntityId[]> handler, Func<string, EditorWindow, bool> validator = null)
        {
            m_CustomHandlers[key] = handler;
            if (validator != null) m_CustomValidators[key] = validator;
        }
        internal void UnregisterCustomHandler(string key)
        {
            m_CustomHandlers.Remove(key);
            m_CustomValidators.Remove(key);

            RemoveEntriesWithCustomKey(key);
        }

        bool TryGetCustomHandler(string key, out Action<string, EntityId[]> handler)
        {
            if (string.IsNullOrEmpty(key))
            {
                handler = null;
                return false;
            }

            return m_CustomHandlers.TryGetValue(key, out handler);
        }
        bool TryGetCustomValidator(string key, out Func<string, EditorWindow, bool> validator)
        {
            if (string.IsNullOrEmpty(key))
            {
                validator = null;
                return false;
            }

            var got = m_CustomValidators.TryGetValue(key, out validator);
            return got && validator != null;
        }
    }

    // ObjectChangeKind.DestroyAssetObject events are not fired when simply deleting an asset in the project
    // view. Have to resort to an AssetModificationProcessor to catch that and remove the related history entries.
    internal class SelectionHistoryAssetDeletionHandler : AssetModificationProcessor
    {
        static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions opt)
        {
            var eid = AssetDatabase.GetMainAssetEntityId(path);
            SelectionHistory.instance.RemoveEntriesWithGUIDOrID(eid);
            return AssetDeleteResult.DidNotDelete;
        }
    }
}
