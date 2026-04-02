// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Generic settings provider implementation for scriptable objects, namely
    /// those discovered by <see cref="PackageSettingsProvider"/>. Loads UI metadata from
    /// the publicly accesible <see cref="BuildProfileSettingsProvider"/> and embeds a custom
    /// or default editor through <see cref="EditorAsVisualElement"/>.
    /// </summary>
    /// <typeparam name="T">Scriptable object type managed by this settings provider.</typeparam>
    class ScriptableObjectSettingsProvider<T> : IBuildProfileSettingsProvider where T : ScriptableObject
    {
        BuildProfileSettingsProvider m_SettingsObjectInfo;
        HashSet<GUID> m_ValidSDKPlatformIds;

        public ScriptableObjectSettingsProvider(BuildProfileSettingsProvider settingsInfo)
        {
            m_SettingsObjectInfo = settingsInfo;

            m_ValidSDKPlatformIds = new HashSet<GUID>();
            var sdkExtensions = BuildProfileModuleUtil.GetAllSDKPlatformExtensions();
            foreach (var extension in sdkExtensions)
            {
                var found = Array.IndexOf(extension.Value.requiredComponents, m_SettingsObjectInfo.settingsType) > -1;
                if (found)
                    m_ValidSDKPlatformIds.Add(extension.Key);
            }
        }

        public string GetDisplayName() => m_SettingsObjectInfo.displayName;

        public int GetDisplayOrder() => m_SettingsObjectInfo.displayOrder;

        public string GetTooltip() => m_SettingsObjectInfo.tooltip;

        public bool CanAddSettings(BuildProfile profile)
        {
            var canAddSetting = m_SettingsObjectInfo.canAddSetting?.Invoke(profile) ?? false;

            if (!GetIsRequired())
                return canAddSetting;

            return m_ValidSDKPlatformIds.Contains(profile.platformGuid) && canAddSetting;
        }

        public Action<BuildProfile> GetResetAction() => OnReset;

        public bool GetIsRequired() => m_SettingsObjectInfo.isRequired;

        public bool HasSettings(BuildProfile profile) => profile.GetComponent<T>() is not null;

        public void OnAdd(BuildProfile profile)
        {
            var instance = ScriptableObject.CreateInstance<T>();
            profile.AddComponent(instance);

            if (m_SettingsObjectInfo.isRequired)
            {
                var componentRef = profile.GetComponent<T>() as ScriptableObject;

                var result = new List<ScriptableObject>(profile.requiredComponents);
                result.Add(componentRef);
                profile.requiredComponents = result.ToArray();
            }
        }

        public void OnRemove(BuildProfile profile)
        {
            var instance = profile.GetComponent<T>();
            if (instance is null || instance is not ScriptableObject so)
            {
                Debug.LogWarning(profile.name + " does not have a component of type " + typeof(T).Name);
            }

            profile.RemoveComponent<T>(instance);
            ScriptableObject.DestroyImmediate(instance);
        }

        public VisualElement CreateInspectorGUI(BuildProfile profile, SerializedObject serializedObject)
        {
            var target = profile.GetComponent<T>();
            return new EditorAsVisualElement(target, m_SettingsObjectInfo.hasCustomEditor);
        }

        static void OnReset(BuildProfile profile)
        {
            var instance = profile.GetComponent<T>();
            if (instance is null || instance is not ScriptableObject so)
            {
                Debug.LogWarning(profile.name + " does not have a component of type " + typeof(T).Name);
            }

            // Forcibly remove components so that required components can handled.
            profile.ForceRemoveComponent<T>();
            ScriptableObject.DestroyImmediate(instance);

            instance = ScriptableObject.CreateInstance<T>();
            profile.AddComponent(instance);
        }
    }
}


