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
    internal const string k_ControllerItemsPropertyName = nameof(m_ControllerItems);

    [SerializeField, FormerlySerializedAs("m_ExtensionsHash")] Hash128 m_ControllersHash;
    [SerializeReference, FormerlySerializedAs("m_InstanceItems")] List<IPlayModeControllerItem> m_ControllerItems;

    public readonly int InstanceCount => m_ControllerItems?.Count ?? 0;
    internal readonly Hash128 ControllersHash => m_ControllersHash;

    public GUID AddInstance<TController, TSettings>(string name, TSettings settings)
        where TController : PlayModeController<TSettings>
        where TSettings : struct
    {
        if (m_ControllerItems == null)
            m_ControllerItems = new();

        var newItem = new PlayModeControllerItem<TController, TSettings>(name, settings);
        m_ControllerItems.Add(newItem);
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
        return m_ControllerItems[FindInstanceIndexById(id)].GetSettings<TSettings>();
    }

    public void SetInstanceSettings<TSettings>(GUID id, TSettings settings)
    {
        var index = FindInstanceIndexById(id);
        m_ControllerItems[index] = m_ControllerItems[index].WithSettings(settings);
    }

    public readonly TSettings GetDecoratorSettings<TDecorator, TSettings>(GUID id)
        where TDecorator : PlayModeControllerDecorator<TSettings>
        where TSettings : struct
    {
        var item = m_ControllerItems[FindInstanceIndexById(id)];
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
        var item = m_ControllerItems[index];
        m_ControllerItems[index] = item.WithDecoratorSettings<TDecorator, TSettings>(settings);
    }

    public void SetInstanceName(GUID id, string name)
    {
        var index = FindInstanceIndexById(id);
        m_ControllerItems[index] = m_ControllerItems[index].WithName(name);
    }

    internal readonly IEnumerable<IPlayModeControllerItem> GetAllControllerItems()
    {
        if (m_ControllerItems == null)
            return Array.Empty<IPlayModeControllerItem>();

        return m_ControllerItems;
    }

    internal IPlayModeControllerItem this[int index]
    {
        readonly get => m_ControllerItems[index];
        set => m_ControllerItems[index] = value;
    }

    internal void RemoveInstanceAt(int index)
    {
        m_ControllerItems.RemoveAt(index);
    }

    internal readonly IPlayModeControllerItem FindControllerItemById(GUID id)
    {
        return m_ControllerItems[FindInstanceIndexById(id)];
    }

    internal void RefreshDecorators(bool force = false)
    {
        if (!force && m_ControllersHash == PlayModeControllerRegistry.Hash)
            return;

        for (var i = 0; i < InstanceCount; i++)
        {
            var item = m_ControllerItems[i];
            item.GenerateMissingDecoratorsAndRemoveDuplicates(PlayModeControllerRegistry.GetDecoratorTypes(item.GetInstanceType()));
            m_ControllerItems[i] = item;
        }

        m_ControllersHash = PlayModeControllerRegistry.Hash;
    }

    readonly int FindInstanceIndexById(GUID id)
    {
        if (m_ControllerItems == null)
            throw new KeyNotFoundException($"No instances in scenario.");

        for (int i = 0; i < m_ControllerItems.Count; i++)
        {
            var item = m_ControllerItems[i];
            if (item.GetId() == id)
            {
                return i;
            }
        }

        throw new KeyNotFoundException($"No instance with id '{id}' found in scenario.");
    }
}
