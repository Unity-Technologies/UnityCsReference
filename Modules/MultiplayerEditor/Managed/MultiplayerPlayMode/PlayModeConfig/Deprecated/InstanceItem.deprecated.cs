// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

interface IUpgradablePlayModeControllerItem
{
    IPlayModeControllerItem Upgrade();
}

[Serializable]
struct InstanceItem<TController, TSettings> : IPlayModeControllerItem, IUpgradablePlayModeControllerItem
    where TController : PlayModeController<TSettings>
    where TSettings : struct
{
#pragma warning disable 649 // assigned by the deserializer only
    [SerializeField] GUID m_Id;
    [SerializeField] string m_Name;
    [SerializeField] RunModeState m_RunMode;
    [SerializeField] TSettings m_Settings;
    [SerializeReference] List<IDecoratorItem> m_Decorators;
#pragma warning restore 649

    public readonly IPlayModeControllerItem Upgrade()
    {
        var item = new PlayModeControllerItem<TController, TSettings>(m_Id, m_Name, m_Settings, m_Decorators);

        // Move the run mode field from the instance item to the run mode decorator.
        item.GenerateMissingDecoratorsAndRemoveDuplicates(PlayModeControllerRegistry.GetDecoratorTypes(typeof(TController)));

        // Kinds without a run mode decorator (main and clone editors) drop the value: it never had an effect for them.
        if (!item.HasDecorator<RunModeDecorator>())
            return item;

        return item.WithDecoratorSettings<RunModeDecorator, RunModeDecorator.DecoratorSettings>(
            new RunModeDecorator.DecoratorSettings { RunMode = m_RunMode });
    }

    public readonly GUID GetId() => m_Id;
    public readonly string GetName() => m_Name ?? string.Empty;
    public readonly Type GetInstanceType() => typeof(TController);
    public readonly Type GetSettingsType() => typeof(TSettings);

    public readonly bool IsInstanceType(Type type) => throw NotUpgraded();
    public readonly T GetSettings<T>() => throw NotUpgraded();
    public readonly T GetUserSettings<T>(OrchestratedScenario owner) where T : struct => throw NotUpgraded();
    public readonly bool HasDecorator(Type decoratorType) => throw NotUpgraded();
    public readonly bool HasDecorator<T>() where T : PlayModeControllerDecorator => throw NotUpgraded();
    public readonly IDecoratorItem GetDecoratorItem(Type decoratorType) => throw NotUpgraded();
    public readonly IDecoratorItem GetDecoratorItem<T>() where T : PlayModeControllerDecorator => throw NotUpgraded();
    public readonly bool TryGetDecoratorItemIndex(Type decoratorType, out int index) => throw NotUpgraded();
    public readonly void GenerateMissingDecoratorsAndRemoveDuplicates(IEnumerable<Type> decoratorTypes) => throw NotUpgraded();
    public readonly PlayModeController CreateController(OrchestratedScenario owner) => throw NotUpgraded();
    public readonly IPlayModeControllerItem WithName(string name) => throw NotUpgraded();
    public readonly IPlayModeControllerItem WithSettings<T>(T settings) => throw NotUpgraded();

    public readonly IPlayModeControllerItem WithDecoratorSettings<TDecorator, TDecoratorSettings>(TDecoratorSettings settings)
        where TDecorator : PlayModeControllerDecorator<TDecoratorSettings>
        where TDecoratorSettings : struct
        => throw NotUpgraded();

    static NotSupportedException NotUpgraded()
        => new($"{nameof(InstanceItem<TController, TSettings>)} is a deserialization-only shim; call {nameof(Upgrade)}() before using it.");
}
