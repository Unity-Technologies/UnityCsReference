// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

// Keep the functional pattern (immutable "With..." methods) to prevent mutations on boxed values
// from propagating undesirably to other references. This approach keeps it behaving as a value type.
interface IInstanceItem
{
    internal const string k_SettingsPropertyPath = "m_Settings";
    internal const string k_NamePropertyPath = "m_Name";
    internal const string k_RunModePropertyPath = "m_RunMode";

    GUID GetId();
    string GetName();
    RunModeState GetRunMode();
    TSettings GetSettings<TSettings>();
    bool IsInstanceType(Type type);
    Type GetInstanceType();
    Type GetSettingsType();
    InstanceController CreateController();

    IInstanceItem WithName(string name);
    IInstanceItem WithRunMode(RunModeState runMode);
    IInstanceItem WithSettings<TSettings>(TSettings settings);
}

[Serializable]
struct InstanceItem<TController, TSettings> : IInstanceItem
    where TController : InstanceController<TController, TSettings>
    where TSettings : struct
{
    // The Ids of the instances are guaranteed to be unique within a scenario, but not globally unique.
    // E.g. when duplicating a scenario asset, the duplicated instances will have the same Ids as the original ones.
    [SerializeField] GUID m_Id;
    [SerializeField] string m_Name;
    [SerializeField] RunModeState m_RunMode;
    [SerializeField] TSettings m_Settings;

    public InstanceItem(string name, TSettings settings)
    {
        // GUID.Generate() cannot be used in serialization contexts, so we parse it from a System.Guid.
        var systemGuid = Guid.NewGuid().ToString("N");
        if (!GUID.TryParse(systemGuid, out m_Id))
        {
            throw new InvalidOperationException( $"Failed to parse System.Guid '{systemGuid}' as Unity GUID for InstanceItem '{name}'. ");
        }

        m_Name = name;
        m_RunMode = RunModeState.ScenarioControl;
        m_Settings = settings;
    }

    public readonly GUID GetId() => m_Id;
    public readonly string GetName() => m_Name ?? string.Empty;
    public readonly RunModeState GetRunMode() => m_RunMode;
    public readonly Type GetInstanceType() => typeof(TController);
    public readonly Type GetSettingsType() => typeof(TSettings);

    public readonly bool IsInstanceType(Type type)
    {
        if (type.IsAssignableFrom(typeof(TController)))
            return true;

        if (type.IsGenericTypeDefinition)
        {
            var currentType = typeof(TController);
            while (currentType != null && currentType != typeof(object))
            {
                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == type)
                    return true;

                currentType = currentType.BaseType;
            }
        }

        return false;
    }

    public readonly T GetSettings<T>()
    {
        if (m_Settings is not T settings)
        {
            throw new InvalidCastException($"Settings of type {typeof(TSettings).Name} is not of type {typeof(T).Name}.");
        }

        return settings;
    }

    public readonly InstanceController CreateController()
    {
        return InstanceController<TController, TSettings>.CreateInstance(m_Settings);
    }

    public readonly IInstanceItem WithName(string name)
    {
        var copy = this;
        copy.m_Name = name;
        return copy;
    }

    public readonly IInstanceItem WithRunMode(RunModeState runMode)
    {
        var copy = this;
        copy.m_RunMode = runMode;
        return copy;
    }

    public readonly IInstanceItem WithSettings<T>(T settings)
    {
        if (settings is not TSettings typedSettings)
        {
            throw new InvalidCastException($"Settings of type {typeof(T).Name} is not of type {typeof(TSettings).Name}.");
        }

        var copy = this;
        copy.m_Settings = typedSettings;
        return copy;
    }
}
