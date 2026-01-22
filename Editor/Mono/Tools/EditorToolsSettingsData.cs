// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor.EditorTools;

namespace UnityEditor
{
    [FilePath(assetPath, FilePathAttribute.Location.ProjectFolder)]
    class EditorToolsSettingsData : ScriptableSingleton<EditorToolsSettingsData>, ISerializationCallbackReceiver
    {
        internal const string assetPath = "Library/EditorToolsSettings.asset";
        [Serializable]
        public struct GroupSettingsData
        {
            public GroupSettingsData(string groupType, bool collapsed)
            {
                this.groupType = groupType;
                this.collapsed = collapsed;
            }

            public string groupType;
            public bool collapsed;
        }

        [SerializeField] List<GroupSettingsData> m_GroupsSettingsList = new();
        Dictionary<string, GroupSettingsData> m_GroupToSettingsData = new();

        [SerializeField] 
        string m_LastPivotModeTypeString;

        public Type lastPivotModeType
        {
            get
            {
                var pivotModeType = PivotManager.defaultPivotModeType;
                if (!string.IsNullOrEmpty(m_LastPivotModeTypeString))
                    pivotModeType = Type.GetType(m_LastPivotModeTypeString) ?? pivotModeType;
                return pivotModeType;
            }
        }

        [SerializeField]
        string m_LastPivotRotationTypeString;

        public Type lastPivotRotationType
        {
            get
            {
                var pivotRotationType = PivotManager.defaultPivotRotationType;
                if (!string.IsNullOrEmpty(m_LastPivotRotationTypeString))
                    pivotRotationType = Type.GetType(m_LastPivotRotationTypeString) ?? pivotRotationType;
                return pivotRotationType;
            }
        }
        
        void OnEnable()
        {
            RefreshToolsData();
        }
        
        void OnDisable()
        {
            Save();
        }
        
        public void Save()
        {
            Save(true);
        }

        static readonly List<string> s_GroupTypesToRemove = new();
        public void RefreshToolsData()
        {
            var availableEditorTools = EditorToolUtility.availailableEditorTools;
            
            // Remove settings for no longer existing groups
            s_GroupTypesToRemove.Clear();
            foreach (var kvp in m_GroupToSettingsData)
            {
                var cachedGroupType = kvp.Key;
                var found = false;
                foreach (var editorTypeAssociation in availableEditorTools)
                {
                    var availableGroupType = editorTypeAssociation.group ?? editorTypeAssociation.targetBehaviour;
                    if (availableGroupType == null)
                        continue;
                    
                    if (availableGroupType.AssemblyQualifiedName.Equals(cachedGroupType))
                    {
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                    s_GroupTypesToRemove.Add(cachedGroupType);
            }
            foreach (var groupType in s_GroupTypesToRemove)
                m_GroupToSettingsData.Remove(groupType);

            // Add group settings for new groups
            foreach (var editorTypeAssociation in availableEditorTools)
            {
                var groupType = editorTypeAssociation.group ?? editorTypeAssociation.targetBehaviour;
                if (groupType != null)
                {
                    var groupTypeName = groupType.AssemblyQualifiedName;
                    if (!m_GroupToSettingsData.ContainsKey(groupTypeName))
                        m_GroupToSettingsData.Add(groupTypeName, new GroupSettingsData(groupTypeName, false));
                }
            }
        }
        
        public void SetGroupCollapsed(Type groupType, bool collapsed)
        {
            if (groupType == null)
                throw new ArgumentNullException(nameof(groupType));

            if (GetGroupSettings(groupType, out var groupSettings))
            {
                groupSettings.collapsed = collapsed;
                m_GroupToSettingsData[groupType.AssemblyQualifiedName] = groupSettings;
            }
        }

        static readonly List<string> s_KeysBuffer = new();
        public void SetGroupsCollapsed(bool collapsed)
        {
            s_KeysBuffer.Clear();
            s_KeysBuffer.AddRange(m_GroupToSettingsData.Keys);
            
            foreach (var groupTypeName in s_KeysBuffer)
            {
                var groupSettings = m_GroupToSettingsData[groupTypeName];
                groupSettings.collapsed = collapsed;
                m_GroupToSettingsData[groupTypeName] = groupSettings;
            }
        }
        
        public bool GetGroupSettings(Type groupType, out GroupSettingsData groupSettings)
        {
            if (groupType == null)
                throw new ArgumentNullException(nameof(groupType));

            return m_GroupToSettingsData.TryGetValue(groupType.AssemblyQualifiedName, out groupSettings);
        }
        
        public void SetLastPivotModeType(Type pivotModeType)
        {
            m_LastPivotModeTypeString = pivotModeType?.AssemblyQualifiedName;
        }

        public void SetLastPivotRotationType(Type pivotRotationType)
        {
            m_LastPivotRotationTypeString = pivotRotationType?.AssemblyQualifiedName;
        }
        
        public void OnBeforeSerialize()
        {
            m_GroupsSettingsList.Clear();
            foreach (var dataPair in m_GroupToSettingsData)
            {
                var groupSettings = dataPair.Value;
                m_GroupsSettingsList.Add(new GroupSettingsData(groupSettings.groupType, groupSettings.collapsed));
            }
        }

        public void OnAfterDeserialize()
        {
            m_GroupToSettingsData.Clear();
            for (int i = 0; i < m_GroupsSettingsList.Count; ++i)
            {
                var groupSettings = m_GroupsSettingsList[i];
                var groupToSettings = new GroupSettingsData(groupSettings.groupType, groupSettings.collapsed);
                m_GroupToSettingsData.Add(groupSettings.groupType, groupToSettings);
            }
        }
    }
    
    static class EditorToolsSettings
    {
        public static bool IsGroupCollapsed(Type groupType)
        {
            if (groupType == null)
                return false;
            
            if (EditorToolsSettingsData.instance.GetGroupSettings(groupType, out var groupSettings))
                return groupSettings.collapsed;

            return false;
        }
        
        public static void SetGroupCollapsed(Type groupType, bool collapsed)
        {
            EditorToolsSettingsData.instance.SetGroupCollapsed(groupType, collapsed);
        }

        public static void SetGroupsCollapsed(bool collapsed)
        {
            EditorToolsSettingsData.instance.SetGroupsCollapsed(collapsed);
        }
    }
}
