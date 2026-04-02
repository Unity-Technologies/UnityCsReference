// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

// Keep the functional pattern (immutable "With..." methods) to prevent mutations on boxed values
// from propagating undesirably to other references. This approach keeps it behaving as a value type.
interface IInstanceItem
{
    internal const string k_SettingsPropertyPath = "m_Settings";
    internal const string k_DecoratorsPropertyPath = "m_Decorators";
    internal const string k_NamePropertyPath = "m_Name";
    internal const string k_RunModePropertyPath = "m_RunMode";

    GUID GetId();
    string GetName();
    RunModeState GetRunMode();
    TSettings GetSettings<TSettings>();
    TSettings GetUserSettings<TSettings>(OrchestratedScenario owner) where TSettings : struct;
    bool IsInstanceType(Type type);
    Type GetInstanceType();
    Type GetSettingsType();
    bool HasDecorator(Type type);
    bool HasDecorator<TDecorator>() where TDecorator : InstanceControllerDecorator;
    IDecoratorItem GetDecoratorItem(Type decoratorType);
    IDecoratorItem GetDecoratorItem<TDecorator>() where TDecorator : InstanceControllerDecorator;
    void GenerateMissingDecoratorsAndRemoveDuplicates(IEnumerable<Type> decoratorTypes);
    InstanceController CreateController(OrchestratedScenario owner);

    IInstanceItem WithName(string name);
    IInstanceItem WithRunMode(RunModeState runMode);
    IInstanceItem WithSettings<TSettings>(TSettings settings);
    IInstanceItem WithDecoratorSettings<TDecorator, TSettings>(TSettings settings)
        where TDecorator : InstanceControllerDecorator<TSettings>
        where TSettings : struct;
}

[Serializable]
struct InstanceItem<TController, TSettings> : IInstanceItem
    where TController : InstanceController<TSettings>
    where TSettings : struct
{
    // The Ids of the instances are guaranteed to be unique within a scenario, but not globally unique.
    // E.g. when duplicating a scenario asset, the duplicated instances will have the same Ids as the original ones.
    [SerializeField] GUID m_Id;
    [SerializeField] string m_Name;
    [SerializeField] RunModeState m_RunMode;
    [SerializeField] TSettings m_Settings;
    [SerializeReference] List<IDecoratorItem> m_Decorators;

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

        GenerateMissingDecoratorsAndRemoveDuplicates(InstanceExtensionManager.GetDecoratorTypes(GetInstanceType()));
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

    public readonly T GetUserSettings<T>(OrchestratedScenario owner) where T : struct
    {
        return OrchestratedScenarioUserSettings.GetSettings<T>(owner, this);
    }

    public readonly IDecoratorItem GetDecoratorItem(Type decoratorType)
    {
        var index = GetDecoratorItemIndex(decoratorType);
        return m_Decorators[index];
    }

    public readonly IDecoratorItem GetDecoratorItem<T>() where T : InstanceControllerDecorator
        => GetDecoratorItem(typeof(T));

    public readonly bool HasDecorator(Type decoratorType)
    {
        if (!typeof(InstanceControllerDecorator).IsAssignableFrom(decoratorType))
        {
            throw new ArgumentException($"Type {decoratorType.Name} must derive from InstanceControllerDecorator.");
        }

        return TryGetDecoratorItemIndex(decoratorType, out _);
    }

    public readonly bool HasDecorator<T>() where T : InstanceControllerDecorator
        => HasDecorator(typeof(T));

    readonly int GetDecoratorItemIndex(Type decoratorType)
    {
        if (!TryGetDecoratorItemIndex(decoratorType, out var index))
        {
            throw new KeyNotFoundException($"Instance with id '{m_Id}' does not have a decorator of type '{decoratorType.Name}'.");
        }

        return index;
    }

    readonly bool TryGetDecoratorItemIndex(Type decoratorType, out int index)
    {
        index = -1;
        if (m_Decorators == null)
            return false;

        for (var i = 0; i < m_Decorators.Count; i++)
        {
            if (m_Decorators[i].GetDecoratorType() == decoratorType)
            {
                index = i;
                return true;
            }
        }

        return false;
    }

    public void GenerateMissingDecoratorsAndRemoveDuplicates(IEnumerable<Type> decoratorTypes)
    {
        m_Decorators ??= new();

        var existingDecoratorTypes = new HashSet<Type>();
        existingDecoratorTypes.Clear();
        for (int i = m_Decorators.Count - 1; i >= 0; i--)
        {
            IDecoratorItem decorator = m_Decorators[i];
            if (existingDecoratorTypes.Contains(decorator.GetDecoratorType()))
            {
                Debug.LogWarning($"InstanceItem '{GetName()}' with id '{m_Id}' has multiple decorators of type '{decorator.GetDecoratorType().Name}'. These duplicate decorators will be removed.");
                m_Decorators.RemoveAt(i);
                continue;
            }
            existingDecoratorTypes.Add(decorator.GetDecoratorType());
        }

        foreach (var decoratorType in decoratorTypes)
        {
            if (!existingDecoratorTypes.Contains(decoratorType) && InstanceControllerDecorator.IsDecoratorWithSettings(decoratorType))
            {
                m_Decorators.Add(IDecoratorItem.Create(decoratorType));
            }
        }
    }

    public readonly InstanceController CreateController(OrchestratedScenario owner)
    {
        return InstanceController.CreateInstance<TController>(this, owner);
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

    public readonly IInstanceItem WithDecoratorSettings<TDecorator, TDecoratorSettings>(TDecoratorSettings settings)
        where TDecorator : InstanceControllerDecorator<TDecoratorSettings>
        where TDecoratorSettings : struct
    {
        var decoratorIndex = GetDecoratorItemIndex(typeof(TDecorator));
        var decoratorItem = m_Decorators[decoratorIndex];
        var updatedDecoratorItem = decoratorItem.WithSettings(settings);
        var copy = this;
        copy.m_Decorators = new(m_Decorators);
        copy.m_Decorators[decoratorIndex] = updatedDecoratorItem;

        return copy;
    }
}
