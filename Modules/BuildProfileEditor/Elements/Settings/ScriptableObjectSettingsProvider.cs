// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine;
using UnityEngine.UIElements;

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

        public ScriptableObjectSettingsProvider(BuildProfileSettingsProvider settingsInfo)
        {
            m_SettingsObjectInfo = settingsInfo;
        }

        public string GetDisplayName() => m_SettingsObjectInfo.displayName;

        public string GetTooltip() => m_SettingsObjectInfo.tooltip;

        public bool CanAddSettings(BuildProfile profile)
        {
            return m_SettingsObjectInfo.canAddSetting?.Invoke(profile) ?? false;
        }

        public Action<BuildProfile> GetResetAction() => OnReset;

        public bool HasSettings(BuildProfile profile) => profile.GetComponent<T>() is not null;

        public void OnAdd(BuildProfile profile)
        {
            var instance = ScriptableObject.CreateInstance<T>();
            profile.AddComponent(instance);
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

            profile.RemoveComponent<T>();
            ScriptableObject.DestroyImmediate(instance);

            instance = ScriptableObject.CreateInstance<T>();
            profile.AddComponent(instance);
        }
    }
}


