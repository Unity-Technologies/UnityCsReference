// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine.AdaptivePerformance;

namespace UnityEditor.AdaptivePerformance.Editor
{
    internal class AdaptivePerformanceLoaderInfoManager : IAdaptivePerformanceLoaderOrderManager
    {
        // Simple class to give us updates when the asset database changes.
        internal class AssetCallbacks : AssetPostprocessor
        {
            static bool s_EditorUpdatable = false;
            internal static System.Action Callback { get; set; }

            static AssetCallbacks()
            {
                if (!s_EditorUpdatable)
                {
                    EditorApplication.update += EditorUpdatable;
                }
                EditorApplication.projectChanged += EditorApplicationOnProjectChanged;
            }

            static void EditorApplicationOnProjectChanged()
            {
                if (Callback != null)
                    Callback.Invoke();
            }

            static void EditorUpdatable()
            {
                s_EditorUpdatable = true;
                EditorApplication.update -= EditorUpdatable;
                if (Callback != null)
                    Callback.Invoke();
            }
        }

        private SerializedObject m_SerializedObject;
        SerializedProperty m_RequiresSettingsUpdate = null;
        SerializedProperty m_LoaderList = null;

        public SerializedObject SerializedObjectData
        {
            get { return m_SerializedObject; }
            set
            {
                if (m_SerializedObject != value)
                {
                    m_SerializedObject = value;
                    PopulateProperty("m_RequiresSettingsUpdate", ref m_RequiresSettingsUpdate);
                    PopulateProperty("m_Loaders", ref m_LoaderList);
                    ShouldReload = true;
                }
            }
        }

        List<AdaptivePerformanceLoaderInfo> m_AllLoaderInfos = new List<AdaptivePerformanceLoaderInfo>();
        List<AdaptivePerformanceLoaderInfo> m_AllLoaderInfosForBuildTarget = new List<AdaptivePerformanceLoaderInfo>();
        List<AdaptivePerformanceLoaderInfo> m_AssignedLoaderInfos = new List<AdaptivePerformanceLoaderInfo>();
        List<AdaptivePerformanceLoaderInfo> m_UnassignedLoaderInfos = new List<AdaptivePerformanceLoaderInfo>();

        private BuildTargetGroup m_BuildTargetGroup = BuildTargetGroup.Unknown;
        internal BuildTargetGroup BuildTarget
        {
            get { return m_BuildTargetGroup; }
            set
            {
                if (m_BuildTargetGroup != value)
                {
                    m_BuildTargetGroup = value;
                    ShouldReload = true;
                }
            }
        }

        void AssetProcessorCallback()
        {
            ShouldReload = true;
        }

        public void OnEnable()
        {
            AssetCallbacks.Callback += AssetProcessorCallback;
            ShouldReload = true;
        }

        public bool ShouldReload
        {
            get
            {
                if (m_RequiresSettingsUpdate != null)
                {
                    SerializedObjectData.Update();

                    return m_RequiresSettingsUpdate.boolValue;
                }
                return false;
            }
            set
            {
                if (m_RequiresSettingsUpdate != null && m_RequiresSettingsUpdate.boolValue != value)
                {
                    m_RequiresSettingsUpdate.boolValue = value;
                    SerializedObjectData.ApplyModifiedProperties();
                }
            }
        }

        public void OnDisable()
        {
            AssetCallbacks.Callback -= null;
        }

        public void ReloadData()
        {
            if (m_LoaderList == null)
                return;

            PopulateAllLoaderInfos();
            PopulateLoadersForBuildTarget();
            PopulateAssignedLoaderInfos();
            PopulateUnassignedLoaderInfos();

            ShouldReload = false;
        }

        void PopulateAllLoaderInfos()
        {
            m_AllLoaderInfos.Clear();
            AdaptivePerformanceLoaderInfo.GetAllKnownLoaderInfos(m_AllLoaderInfos);
        }

        void CleanupLostAssignedLoaders()
        {
            m_AssignedLoaderInfos.RemoveAll(loaderInfo => loaderInfo == null);
        }

        void PopulateAssignedLoaderInfos()
        {
            m_AssignedLoaderInfos.Clear();
            for (int i = 0; i < m_LoaderList.arraySize; i++)
            {
                var prop = m_LoaderList.GetArrayElementAtIndex(i);

                AdaptivePerformanceLoaderInfo info = new AdaptivePerformanceLoaderInfo();
                info.loaderType = (prop.objectReferenceValue == null) ? null : prop.objectReferenceValue.GetType();
                info.assetName = AssetNameFromInstance(prop.objectReferenceValue);
                info.instance = prop.objectReferenceValue as AdaptivePerformanceLoader;

                m_AssignedLoaderInfos.Add(info);
            }
            CleanupLostAssignedLoaders();
        }

        string AssetNameFromInstance(UnityEngine.Object asset)
        {
            if (asset == null)
                return "";

            string assetPath = AssetDatabase.GetAssetPath(asset);
            return Path.GetFileNameWithoutExtension(assetPath);
        }

        void PopulateLoadersForBuildTarget()
        {
            m_AllLoaderInfosForBuildTarget = FilteredLoaderInfos(m_AllLoaderInfos);
        }

        void PopulateUnassignedLoaderInfos()
        {
            m_UnassignedLoaderInfos.Clear();
            foreach (var info in m_AllLoaderInfosForBuildTarget)
            {
                bool isUnassigned = true;
                foreach (var loader in m_AssignedLoaderInfos)
                {
                    if (loader.loaderType == info.loaderType)
                    {
                        isUnassigned = false;
                        break;
                    }
                }

                if(isUnassigned) m_UnassignedLoaderInfos.Add(info);
            }
        }

        void PopulateProperty(string propertyPath, ref SerializedProperty prop)
        {
            if (SerializedObjectData != null && prop == null) prop = SerializedObjectData.FindProperty(propertyPath);
        }

        private List<AdaptivePerformanceLoaderInfo> FilteredLoaderInfos(List<AdaptivePerformanceLoaderInfo> loaderInfos)
        {
            List<AdaptivePerformanceLoaderInfo> ret = new List<AdaptivePerformanceLoaderInfo>();

            foreach (var info in loaderInfos)
            {
                if (info.loaderType == null)
                    continue;

                object[] attrs;

                try
                {
                    attrs = info.loaderType.GetCustomAttributes(typeof(AdaptivePerformanceSupportedBuildTargetAttribute), true);
                }
                catch (Exception)
                {
                    attrs = default;
                }

                if (attrs.Length == 0)
                {
                    // If unmarked we assume it will be applied to all build targets.
                    ret.Add(info);
                }
                else
                {
                    foreach (AdaptivePerformanceSupportedBuildTargetAttribute attr in attrs)
                    {
                        if (attr.buildTargetGroup == m_BuildTargetGroup)
                        {
                            ret.Add(info);
                            break;
                        }
                    }
                }
            }

            return ret;
        }

        void UpdateSerializedProperty()
        {
            if (m_LoaderList != null && m_LoaderList.isArray)
            {
                m_LoaderList.ClearArray();

                int index = 0;
                foreach (AdaptivePerformanceLoaderInfo info in m_AssignedLoaderInfos)
                {
                    m_LoaderList.InsertArrayElementAtIndex(index);
                    var prop = m_LoaderList.GetArrayElementAtIndex(index);
                    prop.objectReferenceValue = info.instance;
                    index++;
                }
            }

            SerializedObjectData.ApplyModifiedProperties();
        }

        #region IAdaptivePerformanceLoaderOrderManager
        List<AdaptivePerformanceLoaderInfo> IAdaptivePerformanceLoaderOrderManager.AssignedLoaders { get { return m_AssignedLoaderInfos; } }
        List<AdaptivePerformanceLoaderInfo> IAdaptivePerformanceLoaderOrderManager.UnassignedLoaders { get { return m_UnassignedLoaderInfos; } }

        void IAdaptivePerformanceLoaderOrderManager.AssignLoader(AdaptivePerformanceLoaderInfo assignedInfo)
        {
            m_AssignedLoaderInfos.Add(assignedInfo);
            m_UnassignedLoaderInfos.Remove(assignedInfo);
            UpdateSerializedProperty();
            ShouldReload = true;
        }

        void IAdaptivePerformanceLoaderOrderManager.UnassignLoader(AdaptivePerformanceLoaderInfo unassignedInfo)
        {
            m_AssignedLoaderInfos.Remove(unassignedInfo);
            m_UnassignedLoaderInfos.Add(unassignedInfo);
            UpdateSerializedProperty();
            ShouldReload = true;
        }

        void IAdaptivePerformanceLoaderOrderManager.Update()
        {
            UpdateSerializedProperty();
            ShouldReload = true;
        }

        #endregion
    }
}
