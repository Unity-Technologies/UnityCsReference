// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEditorInternal;
using Object = UnityEngine.Object;


namespace UnityEditor
{
    [System.Serializable]
    [DataContract]
    internal class SavedFilter
    {
        [DataMember]
        public string m_Name;
        [DataMember]
        public int m_Depth;             // Can be used for tree view representation
        [DataMember]
        public float m_PreviewSize = -1f; // if -1f then preview size is not applied when set
        [DataMember]
        public int m_ID;
        [DataMember]
        public SearchFilter m_Filter;

        public SavedFilter(string name, SearchFilter filter, int depth, float previewSize)
        {
            m_Name = name;
            m_Depth = depth;
            m_Filter = filter;
            m_PreviewSize = previewSize;
        }
    }


    [FilePathAttribute("SearchFilters", FilePathAttribute.Location.PreferencesFolder)]
    internal class SavedSearchFilters : ScriptableSingleton<SavedSearchFilters>
    {
        // The order if this list is the order they will be shown in the UI
        [SerializeField]
        List<SavedFilter> m_SavedFilters;

        // Callback when saved filters have changed
        Action m_SavedFiltersChanged;
        // Callback when saved filters have been initialized
        Action m_SavedFiltersInitialized;

        // Can be used to enable/disable hierarchical grouping of filters
        bool m_AllowHierarchy = false;

        // SavedSearchFilters are saved to disk in the Preferences folder. This means this data is reused between Unity versions.
        // For backwards compatibility back to Unity 3.5 (2012-ish) we keep the 1 billion id offset so the m_ID can be used in InstanceID space
        // for versions before switching to EntityIds. We based this assumption on the fact that InstanceID values for assets never
        // reach that high (1 billion assets).
        const int k_IdStartOffset = 1000000000;

        // --------------------
        // Static interface

        // Returns a filterId that can be used to reference the saved filter
        public static int AddSavedFilter(string displayName, SearchFilter filter, float previewSize)
        {
            int filterId = instance.Add(displayName, filter, previewSize, GetRootFilterId(), true); // using 0 adds filter after the root
            return filterId;
        }

        // Returns a filterId that can be used to reference the saved filter
        public static int AddSavedFilterAfterFilterId(string displayName, SearchFilter filter, float previewSize, int insertAfterID, bool addAsChild)
        {
            int filterId = instance.Add(displayName, filter, previewSize, insertAfterID, addAsChild);
            return filterId;
        }

        public static void RemoveSavedFilter(int filterId)
        {
            instance.Remove(filterId);
        }

        public static bool IsSavedFilter(int filterId)
        {
            return instance.IndexOf(filterId) >= 0;
        }

        public static int GetRootFilterId()
        {
            return instance.GetRoot();
        }

        public static SearchFilter GetFilter(int filterId)
        {
            SavedFilter sf = instance.Find(filterId);
            if (sf != null && sf.m_Filter != null)
                return ObjectCopier.DeepClone(sf.m_Filter);
            return null;
        }

        // Only used for testing
        internal static int GetFilterIdFromNameAndSearchString(string name, string searchFieldString)
        {
            if (instance.m_SavedFilters != null && instance.m_SavedFilters.Count > 0)
            {
                foreach (SavedFilter sf in instance.m_SavedFilters)
                {
                    if ((string.IsNullOrEmpty(name) || sf.m_Name == name) && sf.m_Filter.FilterToSearchFieldString() == searchFieldString)
                    {
                        return sf.m_ID;
                    }
                }
            }

            return 0;
        }

        public static float GetPreviewSize(int filterId)
        {
            SavedFilter sf = instance.Find(filterId);
            if (sf != null)
                return sf.m_PreviewSize;

            Debug.LogError("Could not find preview size for id: " + filterId);
            return -1f;
        }

        public static string GetName(int filterId)
        {
            SavedFilter filter = instance.Find(filterId);
            if (filter != null)
                return filter.m_Name;

            Debug.LogError("Could not find saved filter name for id: " + filterId);
            return "";
        }

        public static void SetName(int filterId, string name)
        {
            SavedFilter filter = instance.Find(filterId);
            if (filter != null)
            {
                filter.m_Name = name;
                instance.Changed();
            }
            else
                Debug.LogError("Could not set name of saved filter " + filterId);
        }

        public static void UpdateExistingSavedFilter(int filterId, SearchFilter filter, float previewSize)
        {
            instance.UpdateFilter(filterId, filter, previewSize);
        }

        public static TreeViewItem<EntityId> ConvertToTreeView(Func<int, EntityId> filterIdToEntityIdRemapper)
        {
            return instance.BuildTreeView(filterIdToEntityIdRemapper);
        }

        public static void RefreshSavedFilters()
        {
            instance.Changed();
        }

        public static void AddChangeListener(System.Action callback)
        {
            instance.m_SavedFiltersChanged -= callback; // ensures its not added twice
            instance.m_SavedFiltersChanged += callback;
        }

        internal static void AddInitializedListener(System.Action callback)
        {
            instance.m_SavedFiltersInitialized -= callback; // ensures its not added twice
            instance.m_SavedFiltersInitialized += callback;
        }

        internal static void RemoveInitializedListener(System.Action callback)
        {
            instance.m_SavedFiltersInitialized -= callback;
        }

        public static void MoveSavedFilter(int filterId, int parentFilterId, int targetFilterId, bool after)
        {
            instance.Move(filterId, parentFilterId, targetFilterId, after);
        }

        public static bool CanMoveSavedFilter(int filterId, int parentFilterId, int targetFilterId, bool after)
        {
            return instance.IsValidMove(filterId, parentFilterId, targetFilterId, after);
        }

        public static bool AllowsHierarchy()
        {
            return instance.m_AllowHierarchy;
        }

        // ------------
        // Impl

        bool IsValidMove(int filterId, int parentFilterId, int targetFilterId, bool after)
        {
            int index = IndexOf(filterId);
            int parentIndex = IndexOf(parentFilterId);
            int targetIndex = IndexOf(targetFilterId);

            if (index < 0 || parentIndex < 0 || targetIndex < 0)
            {
                Debug.LogError("Move of a SavedFilter has invalid input: " + index + " " + parentIndex + " " + targetIndex);
                return false;
            }

            if (filterId == targetFilterId)
            {
                //Debug.LogError ("Cannot move to same position");
                return false;
            }

            // Remove range from list (there could be children that is also moved)
            for (int i = index + 1; i < m_SavedFilters.Count; ++i)
            {
                if (m_SavedFilters[i].m_Depth > m_SavedFilters[index].m_Depth)
                {
                    if (i == targetIndex || i == parentIndex)
                    {
                        //Debug.LogError ("Cannot move filter to a child location ");
                        return false;
                    }
                }
                else
                    break;
            }

            // Valid move
            return true;
        }

        void Move(int filterId, int parentFilterId, int targetFilterId, bool after)
        {
            if (!IsValidMove(filterId, parentFilterId, targetFilterId, after))
                return;

            int index = IndexOf(filterId);
            int parentIndex = IndexOf(parentFilterId);
            int targetIndex = IndexOf(targetFilterId);

            SavedFilter filter = m_SavedFilters[index];
            SavedFilter parent = m_SavedFilters[parentIndex];


            int depthChange = 0;
            if (m_AllowHierarchy)
                depthChange = (parent.m_Depth + 1) - filter.m_Depth;

            // Remove range from list (there could be children that is also moved)
            List<SavedFilter> moveList = GetSavedFilterAndChildren(filterId);
            m_SavedFilters.RemoveRange(index, moveList.Count);

            // Update depth
            foreach (SavedFilter s in moveList)
                s.m_Depth += depthChange;

            // Insert after target
            targetIndex = IndexOf(targetFilterId);
            if (targetIndex != -1)
            {
                if (after)
                    targetIndex += 1;
                m_SavedFilters.InsertRange(targetIndex, moveList);
            }
            Changed();
        }

        void UpdateFilter(int filterId, SearchFilter filter, float previewSize)
        {
            SavedFilter savedFilter = Find(filterId);
            if (savedFilter != null)
            {
                SearchFilter copiedFilter = null;
                if (filter != null)
                {
                    copiedFilter = ObjectCopier.DeepClone(filter);
                    savedFilter.m_Filter = copiedFilter;
                }

                savedFilter.m_PreviewSize = previewSize;
                Changed();
            }
            else
            {
                Debug.LogError("Could not find saved filter " + filterId);
            }
        }

        int GetNextAvailableID()
        {
            HashSet<int> usedIds = new HashSet<int>(m_SavedFilters.Count);
            foreach (var sf in m_SavedFilters)
            {
                if (sf.m_ID >= k_IdStartOffset)
                    usedIds.Add(sf.m_ID);
            }

            const int k_MaxIds = 1000;
            for (int id = k_IdStartOffset; id < k_IdStartOffset + k_MaxIds; id++)
            {
                if (!usedIds.Contains(id))
                    return id;
            }

            Debug.LogError("Could not find an available filterId. Current filter count: " + usedIds.Count);
            return k_IdStartOffset + k_MaxIds;
        }

        int Add(string displayName, SearchFilter filter, float previewSize, int insertAfterUniqueId, bool addAsChild)
        {
            SearchFilter filterCopy = null;
            if (filter != null)
                filterCopy = ObjectCopier.DeepClone(filter);

            // Clear unused data before saving
            if (filterCopy.GetState() == SearchFilter.State.SearchingInAllAssets ||
                filterCopy.GetState() == SearchFilter.State.SearchingInAssetsOnly ||
                filterCopy.GetState() == SearchFilter.State.SearchingInPackagesOnly)
            {
                filterCopy.folders = Array.Empty<string>();
            }

            int afterIndex = 0; // add after root index
            if (insertAfterUniqueId != 0)
            {
                afterIndex = IndexOf(insertAfterUniqueId);
                if (afterIndex == -1)
                {
                    Debug.LogError("Invalid insert position");
                    return 0;
                }
            }

            int depth = m_SavedFilters[afterIndex].m_Depth + (addAsChild ? 1 : 0);

            SavedFilter savedFilter = new SavedFilter(displayName, filterCopy, depth, previewSize);
            savedFilter.m_ID = GetNextAvailableID();

            if (m_SavedFilters.Count == 0)
            {
                m_SavedFilters.Add(savedFilter);
            }
            else
            {
                m_SavedFilters.Insert(afterIndex + 1, savedFilter); // insert after wanted index
            }

            Changed();
            return savedFilter.m_ID;
        }

        List<SavedFilter> GetSavedFilterAndChildren(int filterId)
        {
            List<SavedFilter> result = new List<SavedFilter>();
            int index = IndexOf(filterId);
            if (index >= 0)
            {
                result.Add(m_SavedFilters[index]);
                for (int i = index + 1; i < m_SavedFilters.Count; ++i)
                {
                    if (m_SavedFilters[i].m_Depth > m_SavedFilters[index].m_Depth)
                        result.Add(m_SavedFilters[i]);
                    else
                        break;
                }
            }

            return result;
        }

        void Remove(int filterId)
        {
            int index = IndexOf(filterId);
            if (index >= 1)
            {
                List<SavedFilter> deleteList = GetSavedFilterAndChildren(filterId);
                if (deleteList.Count > 0)
                {
                    m_SavedFilters.RemoveRange(index, deleteList.Count);
                    Changed();
                }
            }
        }

        int IndexOf(int filterId)
        {
            for (int i = 0; i < m_SavedFilters.Count; ++i)
                if (m_SavedFilters[i].m_ID == filterId)
                    return i;

            return -1;
        }

        SavedFilter Find(int filterId)
        {
            int index = IndexOf(filterId);
            if (index >= 0)
                return m_SavedFilters[index];
            return null;
        }

        void Init()
        {
            // Data is serialized to hdd so this code is only run first time (if we did not provide a default set of saved filters)
            if (m_SavedFilters == null || m_SavedFilters.Count == 0)
            {
                m_SavedFilters = new List<SavedFilter>();

                // Ensure we have a root at depth 0
                m_SavedFilters.Add(new SavedFilter("Favorites", null, 0, -1f));
            }

            // Always set root filter data (is serialized so we need to set it here to affect serialized data)
            SearchFilter filter = new SearchFilter();
            filter.classNames = Array.Empty<string>();
            m_SavedFilters[0].m_Name = "Favorites";
            m_SavedFilters[0].m_Filter = filter;
            m_SavedFilters[0].m_Depth = 0;
            m_SavedFilters[0].m_ID = k_IdStartOffset;

            // At init check if all have valid ids (for patching up ids old saved filters)
            for (int i = 0; i < m_SavedFilters.Count; ++i)
                if (m_SavedFilters[i].m_ID < k_IdStartOffset)
                    m_SavedFilters[i].m_ID = GetNextAvailableID();

            // Ensure depth is valid
            if (!m_AllowHierarchy)
                for (int i = 1; i < m_SavedFilters.Count; ++i)
                    m_SavedFilters[i].m_Depth = 1;

            if (m_SavedFiltersInitialized != null && m_SavedFilters.Count > 1)
            {
                m_SavedFiltersInitialized();
            }
        }

        int GetRoot()
        {
            if (m_SavedFilters != null && m_SavedFilters.Count > 0)
                return m_SavedFilters[0].m_ID;
            return 0;
        }

        // Utility function for building a tree view from saved filter state. Returns root of tree
        TreeViewItem<EntityId> BuildTreeView(Func<int, EntityId> filterIdToEntityIdRemapper)
        {
            Init();

            if (m_SavedFilters.Count == 0)
            {
                Debug.LogError("BuildTreeView: No saved filters! We should at least have a root");
                return null;
            }

            TreeViewItem<EntityId> root = null;

            // Create rest of nodes
            var items = new List<TreeViewItem<EntityId>>();
            for (int i = 0; i < m_SavedFilters.Count; ++i)
            {
                SavedFilter savedFilter = m_SavedFilters[i];
                EntityId entityId = filterIdToEntityIdRemapper(savedFilter.m_ID);
                int depth = savedFilter.m_Depth;
                bool isFolder = savedFilter.m_Filter.GetState() == SearchFilter.State.FolderBrowsing;
                var item = new SearchFilterTreeItem(entityId, depth, null, savedFilter.m_Name, isFolder);
                if (i == 0)
                    root = item;
                else
                {
                    items.Add(item);
                }
            }

            // Fix child/parent references
            TreeViewUtility<EntityId>.SetChildParentReferences(items, root);

            return root;
        }

        void Changed()
        {
            bool saveAsText = true;
            Save(saveAsText);

            // Notify listeners of change
            if (m_SavedFiltersChanged != null)
                m_SavedFiltersChanged();
        }

        public override string ToString()
        {
            string text = "Saved Filters ";
            for (int i = 0; i < m_SavedFilters.Count; ++i)
            {
                int filterId = m_SavedFilters[i].m_ID;
                SavedFilter s = m_SavedFilters[i];
                text += string.Format(": {0} ({1})({2})({3}) ", s.m_Name, filterId, s.m_Depth, s.m_PreviewSize);
            }
            return text;
        }
    }


    // Provides a method for performing a deep copy of an object.
    // Binary Serialization is used to perform the copy.
    // Reference Article http://www.codeproject.com/KB/tips/SerializedObjectCloner.aspx
    internal static class ObjectCopier
    {
        // Perform a deep Copy of the source object.
        public static T DeepClone<T>(T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            var serializer = new DataContractSerializer(typeof(T));
            Stream stream = new MemoryStream();
            using (stream)
            {
                serializer.WriteObject(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)serializer.ReadObject(stream);
            }
        }
    }
}
