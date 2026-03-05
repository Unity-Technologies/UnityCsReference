// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

[Serializable]
struct OrchestratedScenarioSettings
{
    const string k_DefaultInstanceName = "";
    internal const string k_InstanceItemsPropertyName = nameof(m_InstanceItems);

    [SerializeField] Hash128 m_ExtensionsHash;
    [SerializeReference] List<IInstanceItem> m_InstanceItems;

    public readonly int InstanceCount => m_InstanceItems?.Count ?? 0;
    internal readonly Hash128 ExtensionsHash => m_ExtensionsHash;

    public GUID AddInstance<TController, TSettings>(string name, TSettings settings)
        where TController : InstanceController<TSettings>
        where TSettings : struct
    {
        if (m_InstanceItems == null)
            m_InstanceItems = new();

        var newItem = new InstanceItem<TController, TSettings>(name, settings);
        m_InstanceItems.Add(newItem);
        return newItem.GetId();
    }

    public GUID AddInstance<TController, TSettings>()
        where TController : InstanceController<TSettings>
        where TSettings : struct
    {
        return AddInstance<TController, TSettings>(k_DefaultInstanceName, InstanceController<TSettings>.GetDefaultSettings());
    }

    public GUID AddInstance<TController, TSettings>(string name)
        where TController : InstanceController<TSettings>
        where TSettings : struct
    {
        return AddInstance<TController, TSettings>(name, InstanceController<TSettings>.GetDefaultSettings());
    }

    public GUID AddInstance<TController, TSettings>(TSettings settings)
        where TController : InstanceController<TSettings>
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
        where TDecorator : InstanceControllerDecorator<TSettings>
        where TSettings : struct
    {
        var item = m_InstanceItems[FindInstanceIndexById(id)];
        var decoratorItem = item.GetDecoratorItem<TDecorator>();
        if (decoratorItem == null)
            throw new KeyNotFoundException($"Instance with id '{id}' does not have a decorator of type '{typeof(TDecorator).Name}'.");

        return decoratorItem.GetSettings<TSettings>();
    }

    public void SetDecoratorSettings<TDecorator, TSettings>(GUID id, TSettings settings)
        where TDecorator : InstanceControllerDecorator<TSettings>
        where TSettings : struct
    {
        var index = FindInstanceIndexById(id);
        var item = m_InstanceItems[index];
        m_InstanceItems[index] = item.WithDecoratorSettings<TDecorator, TSettings>(settings);
    }

    public void SetInstanceRunningMode(GUID id, RunModeState runMode)
    {
        var index = FindInstanceIndexById(id);
        m_InstanceItems[index] = m_InstanceItems[index].WithRunMode(runMode);
    }

    public void SetInstanceName(GUID id, string name)
    {
        var index = FindInstanceIndexById(id);
        m_InstanceItems[index] = m_InstanceItems[index].WithName(name);
    }

    internal readonly IEnumerable<IInstanceItem> GetAllInstanceItems()
    {
        if (m_InstanceItems == null)
            return Array.Empty<IInstanceItem>();

        return m_InstanceItems;
    }

    internal IInstanceItem this[int index]
    {
        readonly get => m_InstanceItems[index];
        set => m_InstanceItems[index] = value;
    }

    internal void RemoveInstanceAt(int index)
    {
        m_InstanceItems.RemoveAt(index);
    }

    internal readonly IInstanceItem FindInstanceItemById(GUID id)
    {
        return m_InstanceItems[FindInstanceIndexById(id)];
    }

    internal void RefreshDecorators(bool force = false)
    {
        if (!force && m_ExtensionsHash == InstanceExtensionManager.Hash)
            return;

        for (var i = 0; i < InstanceCount; i++)
        {
            var item = m_InstanceItems[i];
            item.GenerateMissingDecoratorsAndRemoveDuplicates(InstanceExtensionManager.GetDecoratorTypes(item.GetInstanceType()));
            m_InstanceItems[i] = item;
        }

        m_ExtensionsHash = InstanceExtensionManager.Hash;
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
