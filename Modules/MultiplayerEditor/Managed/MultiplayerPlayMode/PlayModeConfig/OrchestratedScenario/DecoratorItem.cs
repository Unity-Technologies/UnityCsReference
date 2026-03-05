// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

interface IDecoratorItem
{
    internal const string k_SettingsPropertyPath = "m_Settings";

    TSettings GetSettings<TSettings>();
    Type GetDecoratorType();
    Type GetSettingsType();
    InstanceControllerDecorator CreateController(IInstanceItem instanceItem, OrchestratedScenario owner);

    IDecoratorItem WithSettings<TSettings>(TSettings settings);

    static IDecoratorItem Create(Type decoratorType)
    {
        var settingsType = InstanceControllerDecorator.GetSettingsType(decoratorType);
        var decoratorItemType = typeof(DecoratorItem<,>).MakeGenericType(decoratorType, settingsType);
        var newDecoratorItem = (IDecoratorItem)Activator.CreateInstance(decoratorItemType);
        return newDecoratorItem;
    }
}

[Serializable]
struct DecoratorItem<TDecorator, TSettings> : IDecoratorItem
    where TDecorator : InstanceControllerDecorator<TSettings>
    where TSettings : struct
{
    [SerializeField] TSettings m_Settings;

    public DecoratorItem(TSettings settings)
    {
        m_Settings = settings;
    }

    public readonly T GetSettings<T>()
    {
        if (m_Settings is not T settings)
        {
            throw new InvalidCastException($"Settings of type {typeof(TSettings).Name} is not of type {typeof(T).Name}.");
        }

        return settings;
    }

    public readonly Type GetDecoratorType() => typeof(TDecorator);
    public readonly Type GetSettingsType() => typeof(TSettings);

    public IDecoratorItem WithSettings<T>(T settings)
    {
        if (settings is not TSettings validSettings)
        {
            throw new InvalidCastException($"Provided settings of type {typeof(T).Name} is not of the correct type {typeof(TSettings).Name} for decorator type {typeof(TDecorator).Name}.");
        }

        var copy = this;
        copy.m_Settings = validSettings;
        return copy;
    }

    public InstanceControllerDecorator CreateController(IInstanceItem instanceItem, OrchestratedScenario owner)
    {
        return InstanceControllerDecorator<TSettings>.CreateInstance<TDecorator>(instanceItem, owner);
    }
}
