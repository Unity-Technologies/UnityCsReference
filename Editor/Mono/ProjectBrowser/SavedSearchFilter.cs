// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor.Collaboration;
using Object = UnityEngine.Object;


namespace UnityEditor
{
    [System.Serializable]
    internal class SavedFilter
    {
        public string m_Name;
        public int m_Depth;             // Can be used for tree view representation
        public float m_PreviewSize = -1f; // if -1f then preview size is not applied when set
        public int m_ID;
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
        System.Action m_SavedFiltersChanged;
        // Callback when saved filters have been initialized
        System.Action m_SavedFiltersInitialized;
        bool m_AllowHierarchy = false;

        // --------------------
        // Static interface

        // returns is instanceID given to the SavedFilter
        public static int AddSavedFilter(string displayName, SearchFilter filter, float previewSize)
        {
            int instanceID = instance.Add(displayName, filter, previewSize, GetRootInstanceID(), true); // using 0 adds filter after the root
            return instanceID;
        }

        public static int AddSavedFilterAfterInstanceID(string displayName, SearchFilter filter, float previewSize, int insertAfterID, bool addAsChild)
        {
            int instanceID = instance.Add(displayName, filter, previewSize, insertAfterID, addAsChild);
            return instanceID;
        }

        public static void RemoveSavedFilter(int instanceID)
        {
            instance.Remove(instanceID);
        }

        public static bool IsSavedFilter(int instanceID)
        {
            return instance.IndexOf(instanceID) >= 0;
        }

        public static int GetRootInstanceID()
        {
            return instance.GetRoot();
        }

        public static SearchFilter GetFilter(int instanceID)
        {
            SavedFilter sf = instance.Find(instanceID);
            if (sf != null && sf.m_Filter != null)
                return ObjectCopier.DeepClone(sf.m_Filter);
            return null;
        }

        public static int GetFilterInstanceID(string name, string searchFieldString)
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

        public static float GetPreviewSize(int instanceID)
        {
            SavedFilter sf = instance.Find(instanceID);
            if (sf != null)
                return sf.m_PreviewSize;
            return -1f;
        }

        public static string GetName(int instanceID)
        {
            SavedFilter filter = instance.Find(instanceID);
            if (filter != null)
                return filter.m_Name;

            Debug.LogError("Could not find saved filter " + instanceID + " " + instance.ToString());
            return "";
        }

        public static void SetName(int instanceID, string name)
        {
            SavedFilter filter = instance.Find(instanceID);
            if (filter != null)
            {
                filter.m_Name = name;
                instance.Changed();
            }
            else
                Debug.LogError("Could not set name of saved filter " + instanceID + " " + instance.ToString());
        }

        public static void UpdateExistingSavedFilter(int instanceID, SearchFilter filter, float previewSize)
        {
            instance.UpdateFilter(instanceID, filter, previewSize);
        }

        public static TreeViewItem ConvertToTreeView()
        {
            return instance.BuildTreeView();
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

        public static void MoveSavedFilter(int instanceID, int parentInstanceID, int targetInstanceID, bool after)
        {
            instance.Move(instanceID, parentInstanceID, targetInstanceID, after);
        }

        public static bool CanMoveSavedFilter(int instanceID, int parentInstanceID, int targetInstanceID, bool after)
        {
            return instance.IsValidMove(instanceID, parentInstanceID, targetInstanceID, after);
        }

        public static bool AllowsHierarchy()
        {
            return instance.m_AllowHierarchy;
        }

        // ------------
        // Impl

        bool IsValidMove(int instanceID, int parentInstanceID, int targetInstanceID, bool after)
        {
            int index = IndexOf(instanceID);
            int parentIndex = IndexOf(parentInstanceID);
            int targetIndex = IndexOf(targetInstanceID);

            if (index < 0 || parentIndex < 0 || targetIndex < 0)
            {
                Debug.LogError("Move of a SavedFilter has invalid input: " + index + " " + parentIndex + " " + targetIndex);
                return false;
            }

            if (instanceID == targetInstanceID)
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

        void Move(int instanceID, int parentInstanceID, int targetInstanceID, bool after)
        {
            if (!IsValidMove(instanceID, parentInstanceID, targetInstanceID, after))
                return;

            int index = IndexOf(instanceID);
            int parentIndex = IndexOf(parentInstanceID);
            int targetIndex = IndexOf(targetInstanceID);

            SavedFilter filter = m_SavedFilters[index];
            SavedFilter parent = m_SavedFilters[parentIndex];


            int depthChange = 0;
            if (m_AllowHierarchy)
                depthChange = (parent.m_Depth + 1) - filter.m_Depth;

            // Remove range from list (there could be children that is also moved)
            List<SavedFilter> moveList = GetSavedFilterAndChildren(instanceID);
            m_SavedFilters.RemoveRange(index, moveList.Count);

            // Update depth
            foreach (SavedFilter s in moveList)
                s.m_Depth += depthChange;

            // Insert after target
            targetIndex = IndexOf(targetInstanceID);
            if (targetIndex != -1)
            {
                if (after)
                    targetIndex += 1;
                m_SavedFilters.InsertRange(targetIndex, moveList);
            }
            Changed();
        }

        void UpdateFilter(int instanceID, SearchFilter filter, float previewSize)
        {
            SavedFilter savedFilter = Find(instanceID);
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
                Debug.LogError("Could not find saved filter " + instanceID + " " + instance.ToString());
            }
        }

        int GetNextAvailableID()
        {
            List<int> allIDs = new List<int>();
            foreach (SavedFilter sf in m_SavedFilters)
                if (sf.m_ID >= ProjectWindowUtil.k_FavoritesStartInstanceID)
                    allIDs.Add(sf.m_ID);
            allIDs.Sort();

            // Now try find
            int result = ProjectWindowUtil.k_FavoritesStartInstanceID;
            int i = 0;
            while (i < 1000)
            {
                if (allIDs.BinarySearch(result) < 0)
                    return result;

                result++;
                i++;
            }

            Debug.LogError("Could not assign valid ID to saved filter " + DebugUtils.ListToString(allIDs) + " " + result);
            return ProjectWindowUtil.k_FavoritesStartInstanceID + 1000;
        }

        int Add(string displayName, SearchFilter filter, float previewSize, int insertAfterInstanceID, bool addAsChild)
        {
            SearchFilter filterCopy = null;
            if (filter != null)
                filterCopy = ObjectCopier.DeepClone(filter);

            // Clear unused data before saving
            if (filterCopy.GetState() == SearchFilter.State.SearchingInAllAssets ||
                filterCopy.GetState() == SearchFilter.State.SearchingInAssetStore)
            {
                filterCopy.folders = new string[0];
            }

            int afterIndex = 0; // add after root index
            if (insertAfterInstanceID != 0)
            {
                afterIndex = IndexOf(insertAfterInstanceID);
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

            // Select new Saved filter
            //Selection.activeInstanceID = savedFilter.m_ID;

            Changed();
            return savedFilter.m_ID;
        }

        List<SavedFilter> GetSavedFilterAndChildren(int instanceID)
        {
            List<SavedFilter> result = new List<SavedFilter>();
            int index = IndexOf(instanceID);
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

        void Remove(int instanceID)
        {
            int index = IndexOf(instanceID);
            if (index >= 1)
            {
                List<SavedFilter> deleteList = GetSavedFilterAndChildren(instanceID);
                if (deleteList.Count > 0)
                {
                    m_SavedFilters.RemoveRange(index, deleteList.Count);
                    Changed();
                }
            }
        }

        int IndexOf(int instanceID)
        {
            for (int i = 0; i < m_SavedFilters.Count; ++i)
                if (m_SavedFilters[i].m_ID == instanceID)
                    return i;

            return -1;
        }

        SavedFilter Find(int instanceID)
        {
            int index = IndexOf(instanceID);
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
            filter.classNames = new string[0];
            m_SavedFilters[0].m_Name = "Favorites";
            m_SavedFilters[0].m_Filter = filter;
            m_SavedFilters[0].m_Depth = 0;
            m_SavedFilters[0].m_ID = ProjectWindowUtil.k_FavoritesStartInstanceID;

            // At init check if all have valid ids (for patching up ids old saved filters)
            for (int i = 0; i < m_SavedFilters.Count; ++i)
                if (m_SavedFilters[i].m_ID < ProjectWindowUtil.k_FavoritesStartInstanceID)
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
        TreeViewItem BuildTreeView()
        {
            Init();

            if (m_SavedFilters.Count == 0)
            {
                Debug.LogError("BuildTreeView: No saved filters! We should at least have a root");
                return null;
            }

            TreeViewItem root = null;

            // Create rest of nodes
            List<TreeViewItem> items = new List<TreeViewItem>();
            for (int i = 0; i < m_SavedFilters.Count; ++i)
            {
                SavedFilter savedFilter = m_SavedFilters[i];
                int instanceID = savedFilter.m_ID;
                int depth = savedFilter.m_Depth;
                bool isFolder = savedFilter.m_Filter.GetState() == SearchFilter.State.FolderBrowsing;
                TreeViewItem item = new SearchFilterTreeItem(instanceID, depth, null, savedFilter.m_Name, isFolder);
                if (i == 0)
                    root = item;
                else
                {
                    if (Collab.instance.collabFilters.ContainsSearchFilter(savedFilter.m_Name, savedFilter.m_Filter.FilterToSearchFieldString()))
                    {
                        if (!Collab.instance.IsCollabEnabledForCurrentProject())
                            continue;
                    }

                    if (SoftlockViewController.Instance.softLockFilters.ContainsSearchFilter(savedFilter.m_Name, savedFilter.m_Filter.FilterToSearchFieldString()))
                    {
                        if (CollabSettingsManager.IsAvailable(CollabSettingType.InProgressEnabled))
                        {
                            if (!Collab.instance.IsCollabEnabledForCurrentProject() || !CollabSettingsManager.inProgressEnabled)
                                continue;
                        }
                        else
                            continue;
                    }
                    items.Add(item);
                }
            }

            // Fix child/parent references
            TreeViewUtility.SetChildParentReferences(items, root);

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
                int instanceID = m_SavedFilters[i].m_ID;
                SavedFilter s = m_SavedFilters[i];
                text += string.Format(": {0} ({1})({2})({3}) ", s.m_Name, instanceID, s.m_Depth, s.m_PreviewSize);
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

            System.Runtime.Serialization.IFormatter formatter =
                new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}
