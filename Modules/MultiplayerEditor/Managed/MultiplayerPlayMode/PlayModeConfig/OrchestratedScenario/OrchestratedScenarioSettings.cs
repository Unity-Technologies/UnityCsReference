// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Editor;

[Serializable]
struct OrchestratedScenarioSettings
{
    const string k_DefaultInstanceName = "";
    internal const string k_InstanceItemsPropertyName = nameof(m_InstanceItems);
    internal const string k_ScenarioItemsPropertyName = nameof(m_ScenarioItems);

    [SerializeField, FormerlySerializedAs("m_ExtensionsHash")] Hash128 m_ControllersHash;
    [SerializeReference, FormerlySerializedAs("m_ControllerItems")] List<IPlayModeControllerItem> m_InstanceItems;
    [SerializeReference] List<IPlayModeControllerItem> m_ScenarioItems;

    public readonly int InstanceCount => m_InstanceItems?.Count ?? 0;
    internal readonly int ScenarioItemCount => m_ScenarioItems?.Count ?? 0;
    internal readonly Hash128 ControllersHash => m_ControllersHash;

    public GUID AddInstance<TController, TSettings>(string name, TSettings settings)
        where TController : PlayModeController<TSettings>
        where TSettings : struct
    {
        if (m_InstanceItems == null)
            m_InstanceItems = new();

        var newItem = new PlayModeControllerItem<TController, TSettings>(name, settings);
        m_InstanceItems.Add(newItem);
        return newItem.GetId();
    }

    public GUID AddInstance<TController, TSettings>()
        where TController : PlayModeController<TSettings>
        where TSettings : struct
    {
        return AddInstance<TController, TSettings>(k_DefaultInstanceName, PlayModeController<TSettings>.GetDefaultSettings());
    }

    public GUID AddInstance<TController, TSettings>(string name)
        where TController : PlayModeController<TSettings>
        where TSettings : struct
    {
        return AddInstance<TController, TSettings>(name, PlayModeController<TSettings>.GetDefaultSettings());
    }

    public GUID AddInstance<TController, TSettings>(TSettings settings)
        where TController : PlayModeController<TSettings>
        where TSettings : struct
    {
        return AddInstance<TController, TSettings>(k_DefaultInstanceName, settings);
    }

    public readonly TSettings GetInstanceSettings<TSettings>(GUID id)
    {
        return m_InstanceItems[FindInstanceIndexById(id)].GetSettings<TSettings>();
    }

    public void SetInstanceSettings<TSettings>(GUID id, TSettings settings)
    {
        var index = FindInstanceIndexById(id);
        m_InstanceItems[index] = m_InstanceItems[index].WithSettings(settings);
    }

    public readonly TSettings GetDecoratorSettings<TDecorator, TSettings>(GUID id)
        where TDecorator : PlayModeControllerDecorator<TSettings>
        where TSettings : struct
    {
        var item = m_InstanceItems[FindInstanceIndexById(id)];
        var decoratorItem = item.GetDecoratorItem<TDecorator>();
        if (decoratorItem == null)
            throw new KeyNotFoundException($"Instance with id '{id}' does not have a decorator of type '{typeof(TDecorator).Name}'.");

        return decoratorItem.GetSettings<TSettings>();
    }

    public void SetDecoratorSettings<TDecorator, TSettings>(GUID id, TSettings settings)
        where TDecorator : PlayModeControllerDecorator<TSettings>
        where TSettings : struct
    {
        var index = FindInstanceIndexById(id);
        var item = m_InstanceItems[index];
        m_InstanceItems[index] = item.WithDecoratorSettings<TDecorator, TSettings>(settings);
    }

    public void SetInstanceName(GUID id, string name)
    {
        var index = FindInstanceIndexById(id);
        m_InstanceItems[index] = m_InstanceItems[index].WithName(name);
    }

    internal readonly IEnumerable<IPlayModeControllerItem> GetAllInstanceItems()
    {
        if (m_InstanceItems == null)
            return Array.Empty<IPlayModeControllerItem>();

        return m_InstanceItems;
    }

    internal readonly IEnumerable<IPlayModeControllerItem> GetAllScenarioItems()
    {
        if (m_ScenarioItems == null)
            return Array.Empty<IPlayModeControllerItem>();

        return m_ScenarioItems;
    }

    internal IPlayModeControllerItem this[int index]
    {
        readonly get => m_InstanceItems[index];
        set => m_InstanceItems[index] = value;
    }

    internal void RemoveInstanceAt(int index)
    {
        m_InstanceItems.RemoveAt(index);
    }

    internal readonly IPlayModeControllerItem FindInstanceItemById(GUID id)
    {
        return m_InstanceItems[FindInstanceIndexById(id)];
    }

    internal readonly IPlayModeControllerItem FindScenarioItem(Type controllerType)
        => IPlayModeControllerItem.FindByControllerType(m_ScenarioItems, controllerType);

    internal void RefreshItems(bool force = false)
    {
        if (!force && m_ControllersHash == PlayModeControllerRegistry.Hash)
            return;

        SyncScenarioItems();
        GenerateMissingDecorators(m_InstanceItems);

        m_ControllersHash = PlayModeControllerRegistry.Hash;
    }

    // Scenario-scoped items are materialized from the registry, not added by the user. An item whose
    // kind is no longer registered is kept: registration can arrive after an asset's OnEnable, and
    // dropping the item would take the per-user settings keyed on its id with it.
    void SyncScenarioItems()
    {
        m_ScenarioItems?.RemoveAll(item => item == null);

        foreach (var controllerType in PlayModeControllerRegistry.GetScenarioTypes())
        {
            if (FindScenarioItem(controllerType) != null || !PlayModeController.IsControllerWithSettings(controllerType))
                continue;

            m_ScenarioItems ??= new();
            m_ScenarioItems.Add(IPlayModeControllerItem.Create(controllerType));
        }
    }

    static void GenerateMissingDecorators(List<IPlayModeControllerItem> items)
    {
        if (items == null)
            return;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            item.GenerateMissingDecoratorsAndRemoveDuplicates(PlayModeControllerRegistry.GetDecoratorTypes(item.GetInstanceType()));
            items[i] = item;
        }
    }

    readonly int FindInstanceIndexById(GUID id)
    {
        if (m_InstanceItems == null)
            throw new KeyNotFoundException($"No instances in scenario.");

        for (int i = 0; i < m_InstanceItems.Count; i++)
        {
            var item = m_InstanceItems[i];
            if (item.GetId() == id)
            {
                return i;
            }
        }

        throw new KeyNotFoundException($"No instance with id '{id}' found in scenario.");
    }
}
