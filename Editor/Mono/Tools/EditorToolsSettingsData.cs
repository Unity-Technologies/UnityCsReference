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
    class EditorToolsSettingsData : EditorToolStateManager<EditorToolsSettingsData, EditorToolsSettingsData.EditorToolsSettingsState>
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

        [Serializable]
        internal class EditorToolsSettingsState : EditorToolStateBase, ISerializationCallbackReceiver
        {
            [SerializeField]
            List<GroupSettingsData> m_GroupsSettingsList = new();

            Dictionary<string, GroupSettingsData> m_GroupToSettingsData = new();

            [SerializeField]
            string m_LastPivotModeTypeString;

            [SerializeField]
            string m_LastPivotRotationTypeString;

            public Type lastPivotModeType
            {
                get
                {
                    Type pivotModeType = null;
                    if (!string.IsNullOrEmpty(m_LastPivotModeTypeString))
                    {
                        var lastPivotMode = Type.GetType(m_LastPivotModeTypeString);
                        if (EditorPivotManager.IsPivotModeAvailable(lastPivotMode, stateToolOwnerType))
                            pivotModeType = lastPivotMode;
                    }

                    if (pivotModeType == null)
                    {
                        if (stateToolOwnerType == typeof(SceneView))
                            pivotModeType = PivotManager.defaultPivotModeType;
                        else
                            pivotModeType = EditorPivotManager.GetFirstAvailablePivotMode(stateToolOwnerType);
                    }

                    return pivotModeType;
                }
            }

            public Type lastPivotRotationType
            {
                get
                {
                    Type pivotRotationType = null;
                    if (!string.IsNullOrEmpty(m_LastPivotRotationTypeString))
                    {
                        var lastPivotRotation = Type.GetType(m_LastPivotRotationTypeString);
                        if (EditorPivotManager.IsPivotRotationAvailable(lastPivotRotation, stateToolOwnerType))
                            pivotRotationType = lastPivotRotation;
                    }

                    if (pivotRotationType == null)
                    {
                        if (stateToolOwnerType == typeof(SceneView))
                            pivotRotationType = PivotManager.defaultPivotRotationType;
                        else
                            pivotRotationType = EditorPivotManager.GetFirstAvailablePivotRotation(stateToolOwnerType);
                    }

                    return pivotRotationType;
                }
            }

            static readonly List<string> s_GroupTypesToRemove = new();
            public void RefreshToolsData()
            {
                var availableEditorTools = EditorToolUtility.GetAvailableEditorTools(stateToolOwnerType);

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

                // Ensure dictionary is initialized
                if (m_GroupToSettingsData == null)
                {
                    groupSettings = default;
                    return false;
                }

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

            public bool MigrateObsoleteDataIfNeeded(List<GroupSettingsData> groupSettingsList, string lastPivotModeType, string lastPivotRotationType)
            {
                var migrated = false;
                if (groupSettingsList != null && groupSettingsList.Count > 0)
                {
                    m_GroupToSettingsData.Clear();
                    for (int i = 0; i < groupSettingsList.Count; ++i)
                    {
                        var groupSettings = groupSettingsList[i];
                        var groupToSettings = new GroupSettingsData(groupSettings.groupType, groupSettings.collapsed);
                        if (!m_GroupToSettingsData.ContainsKey(groupSettings.groupType))
                            m_GroupToSettingsData.Add(groupSettings.groupType, groupToSettings);
                    }

                    migrated = true;
                }

                if (!string.IsNullOrEmpty(lastPivotModeType))
                {
                    m_LastPivotModeTypeString = lastPivotModeType;
                    migrated = true;
                }

                if (!string.IsNullOrEmpty(lastPivotRotationType))
                {
                    m_LastPivotRotationTypeString = lastPivotRotationType;
                    migrated = true;
                }

                return migrated;
            }
        }

        // Obsolete
        [SerializeField]
        List<GroupSettingsData> m_GroupsSettingsList = new();

        // Obsolete
        [SerializeField]
        string m_LastPivotModeTypeString;

        // Obsolete
        [SerializeField]
        string m_LastPivotRotationTypeString;

        public static Type GetLastPivotModeType(Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                return state.lastPivotModeType;

            return null;
        }

        public static Type GetLastPivotRotationType(Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                return state.lastPivotRotationType;
            return null;
        }

        public static void SetLastPivotModeType(Type pivotModeType, Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                state.SetLastPivotModeType(pivotModeType);
        }

        public static void SetLastPivotRotationType(Type pivotRotationType, Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                state.SetLastPivotRotationType(pivotRotationType);
        }

        public static bool GetGroupSettings(Type groupType, Type ownerType, out GroupSettingsData groupSettings)
        {
            groupSettings = default;
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                return state.GetGroupSettings(groupType, out groupSettings);
            return false;
        }

        public static void SetGroupCollapsed(Type groupType, bool collapsed, Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                state.SetGroupCollapsed(groupType, collapsed);
        }

        public static void SetGroupsCollapsed(bool collapsed, Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                state.SetGroupsCollapsed(collapsed);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            RefreshToolsData();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            Save();
        }

        public void Save()
        {
            Save(true);
        }

        public void RefreshToolsData()
        {
            defaultState.RefreshToolsData();
            foreach (var state in customStates)
                state.RefreshToolsData();
        }

        public void SetGroupCollapsed(Type groupType, bool collapsed)
        {
            defaultState.SetGroupCollapsed(groupType, collapsed);
        }

        public void SetGroupsCollapsed(bool collapsed)
        {
            defaultState.SetGroupsCollapsed(collapsed);
        }

        public bool GetGroupSettings(Type groupType, out GroupSettingsData groupSettings)
        {
            return defaultState.GetGroupSettings(groupType, out groupSettings);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            if (defaultState.MigrateObsoleteDataIfNeeded(m_GroupsSettingsList, m_LastPivotModeTypeString, m_LastPivotRotationTypeString))
            {
                m_GroupsSettingsList = null;
                m_LastPivotModeTypeString = null;
                m_LastPivotRotationTypeString = null;
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

        public static bool IsGroupCollapsed(Type groupType, Type ownerType)
        {
            if (groupType == null)
                return false;

            if (EditorToolsSettingsData.GetGroupSettings(groupType, ownerType, out var groupSettings))
                return groupSettings.collapsed;

            return false;
        }

        public static void SetGroupCollapsed(Type groupType, bool collapsed, Type ownerType)
        {
            EditorToolsSettingsData.SetGroupCollapsed(groupType, collapsed, ownerType);
        }

        public static void SetGroupsCollapsed(bool collapsed, Type ownerType)
        {
            EditorToolsSettingsData.SetGroupsCollapsed(collapsed, ownerType);
        }
    }
}
