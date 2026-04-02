// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.PlayMode.Editor;

// Important note:
// Struct settings that have a parameterless constructor will crash the editor when trying to deserialize.
// This is because the serialization system doesn't expect structs to have constructors as they are not supported
// in users code. Structs with parameterless constructors are supported in engine modules code, so we need to make
// sure to not have any of those in the settings we want to serialize in OrchestratedScenarioUserSettings.
[FilePath(k_AssetPath, FilePathAttribute.Location.ProjectFolder)]
class OrchestratedScenarioUserSettings : ScriptableSingleton<OrchestratedScenarioUserSettings>
{
    internal const string k_AssetPath = "UserSettings/OrchestratedScenarioUserSettings.asset";

    static bool s_Loaded;

    SerializedObject m_SerializedObject;

    OrchestratedScenarioUserSettings()
    {
        s_Loaded = true;

        EditorApplication.quitting -= CleanUpAndSave;
        EditorApplication.quitting += CleanUpAndSave;
    }

    [SerializeField] CustomAssetsData m_UsersSettingsData = new();

    public static void SetSettings<T>(OrchestratedScenario scenario, IInstanceItem instanceItem, T settings)
        where T : struct
    {
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(scenario, out var assetGuid, out _))
            throw new ArgumentException($"The provided scenario '{scenario.name}' is not a valid asset.");

        var key = GetSettingsKey<T>(instanceItem);
        instance.m_UsersSettingsData.SetData(new GUID(assetGuid), key, settings);

        EditorUtility.SetDirty(instance);
    }

    public static T GetSettings<T>(OrchestratedScenario scenario, IInstanceItem instanceItem, T defaultValue = default)
        where T : struct
    {
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(scenario, out var assetGuid, out _))
            throw new ArgumentException($"The provided scenario '{scenario.name}' is not a valid asset.");

        var key = GetSettingsKey<T>(instanceItem);
        if (instance.m_UsersSettingsData.TryGetData(new GUID(assetGuid), key, out T settings))
        {
            return settings;
        }

        return defaultValue;
    }

    public static SerializedProperty GetSerializedSettingsProperty<T>(OrchestratedScenario scenario, IInstanceItem instanceItem, T defaultValue = default)
        where T : struct
    {
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(scenario, out var assetGuid, out _))
            throw new ArgumentException($"The provided scenario '{scenario.name}' is not a valid asset.");

        var key = GetSettingsKey<T>(instanceItem);
        if (!instance.m_UsersSettingsData.TryGetDataPropertyPath(new GUID(assetGuid), key, out var settingsPath))
        {
            SetSettings(scenario, instanceItem, defaultValue);
            var propertyCreated = instance.m_UsersSettingsData.TryGetDataPropertyPath(new GUID(assetGuid), key, out settingsPath);
            Assert.IsTrue(propertyCreated, $"Failed to find serialized property for settings of type '{typeof(T).FullName}' with instance ID '{instanceItem.GetId()}' on scenario '{scenario.name}'.");
        }

        if (instance.m_SerializedObject == null)
        {
            instance.m_SerializedObject = new SerializedObject(instance);
        }

        instance.m_SerializedObject.Update();
        var propertyPath = $"{nameof(m_UsersSettingsData)}.{settingsPath}";
        var property = instance.m_SerializedObject.FindProperty(propertyPath);
        Assert.IsNotNull(property, $"Failed to find serialized property at path '{propertyPath}' for settings of type '{typeof(T).FullName}' with instance ID '{instanceItem.GetId()}' on scenario '{scenario.name}'.");
        return property;
    }

    static string GetSettingsKey<T>(IInstanceItem instanceItem)
        where T : struct
        => $"{instanceItem.GetId()}-{typeof(T).FullName}";

    internal static void CleanUpAndSave()
    {
        if (s_Loaded && EditorUtility.IsDirty(instance))
        {
            instance.m_UsersSettingsData.CleanUpDeletedAssets();
            instance.Save(true);
        }   
    }
}
