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

    [SerializeReference] List<IInstanceItem> m_InstanceItems;

    public readonly int InstanceCount => m_InstanceItems?.Count ?? 0;

    public GUID AddInstance<TController, TSettings>(string name, TSettings settings)
        where TController : InstanceController<TController, TSettings>
        where TSettings : struct
    {
        if (m_InstanceItems == null)
            m_InstanceItems = new();

        var newItem = new InstanceItem<TController, TSettings>(name, settings);
        m_InstanceItems.Add(newItem);
        return newItem.GetId();
    }

    public GUID AddInstance<TController, TSettings>()
        where TController : InstanceController<TController, TSettings>
        where TSettings : struct
    {
        return AddInstance<TController, TSettings>(k_DefaultInstanceName, InstanceController<TController, TSettings>.GetDefaultSettings());
    }

    public GUID AddInstance<TController, TSettings>(string name)
        where TController : InstanceController<TController, TSettings>
        where TSettings : struct
    {
        return AddInstance<TController, TSettings>(name, InstanceController<TController, TSettings>.GetDefaultSettings());
    }

    public GUID AddInstance<TController, TSettings>(TSettings settings)
        where TController : InstanceController<TController, TSettings>
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
