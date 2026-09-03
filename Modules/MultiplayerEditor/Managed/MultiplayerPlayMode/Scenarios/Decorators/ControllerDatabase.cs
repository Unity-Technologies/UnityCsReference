// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

interface IControllerDatabase
{
    Hash128 Hash { get; }

    void RegisterInstanceController<TController>()
        where TController : PlayModeController;
    void RegisterScenarioController<TController>()
        where TController : PlayModeController;
    void RegisterDecorator<TDecorated, TDecorator>()
        where TDecorator : PlayModeControllerDecorator
        where TDecorated : PlayModeController;

    IReadOnlyCollection<Type> GetInstanceTypes();
    IEnumerable<Type> GetInstanceTypesDerivedFrom(Type baseType);
    IReadOnlyCollection<Type> GetScenarioTypes();
    IEnumerable<Type> GetScenarioTypesDerivedFrom(Type baseType);
    IReadOnlyCollection<Type> GetDecoratorTypes(Type decoratedType);
}

class ControllerDatabase : IControllerDatabase
{
    Hash128 m_Hash;
    SortedSet<Type> m_InstanceTypes;
    SortedSet<Type> m_ScenarioTypes;
    Dictionary<Type, SortedSet<Type>> m_DecoratorsMap;

    public Hash128 Hash => m_Hash;

    public ControllerDatabase()
    {
        m_InstanceTypes = new(new TypeComparer());
        m_ScenarioTypes = new(new TypeComparer());
        m_DecoratorsMap = new();
    }

    void RefreshHash()
    {
        m_Hash = new Hash128();
        AppendScope("instance", m_InstanceTypes);
        AppendScope("scenario", m_ScenarioTypes);
    }

    // The scope tag is a header for the set, appended once. The empty-set early-out is
    // what keeps it a header: an unconditional append would give an empty database a
    // non-default hash.
    void AppendScope(string scopeTag, SortedSet<Type> types)
    {
        if (types.Count == 0)
        {
            return;
        }

        m_Hash.Append(scopeTag);

        foreach (var type in types)
        {
            m_Hash.Append(type.FullName);

            if (m_DecoratorsMap.TryGetValue(type, out var decoratorsList))
            {
                foreach (var decoratorType in decoratorsList)
                {
                    m_Hash.Append(decoratorType.FullName);
                }
            }
        }
    }

    public void RegisterInstanceController<TController>() where TController : PlayModeController
    {
        if (m_ScenarioTypes.Contains(typeof(TController)))
        {
            throw new InvalidOperationException($"Cannot register {typeof(TController).Name} as an instance type because it is already registered as a scenario type.");
        }

        m_InstanceTypes.Add(typeof(TController));
        RefreshHash();
    }

    public void RegisterScenarioController<TController>() where TController : PlayModeController
    {
        if (m_InstanceTypes.Contains(typeof(TController)))
        {
            throw new InvalidOperationException($"Cannot register {typeof(TController).Name} as a scenario type because it is already registered as an instance type.");
        }

        m_ScenarioTypes.Add(typeof(TController));
        RefreshHash();
    }

    public void RegisterDecorator<TDecorated, TDecorator>()
        where TDecorator : PlayModeControllerDecorator
        where TDecorated : PlayModeController
    {
        if (!m_InstanceTypes.Contains(typeof(TDecorated)) && !m_ScenarioTypes.Contains(typeof(TDecorated)))
        {
            throw new InvalidOperationException($"Cannot register decorator {typeof(TDecorator).Name} for type {typeof(TDecorated).Name} because the decorated type is not registered as an instance type or a scenario type.");
        }

        if (!m_DecoratorsMap.TryGetValue(typeof(TDecorated), out var decoratorsList))
        {
            decoratorsList = new(new TypeComparer());
            m_DecoratorsMap[typeof(TDecorated)] = decoratorsList;
        }

        decoratorsList.Add(typeof(TDecorator));
        RefreshHash();
    }

    public IReadOnlyCollection<Type> GetInstanceTypes()
        => m_InstanceTypes;

    public IEnumerable<Type> GetInstanceTypesDerivedFrom(Type baseType)
    {
        foreach (var type in m_InstanceTypes)
        {
            if (baseType.IsAssignableFrom(type))
            {
                yield return type;
            }
        }
    }

    public IReadOnlyCollection<Type> GetScenarioTypes()
        => m_ScenarioTypes;

    public IEnumerable<Type> GetScenarioTypesDerivedFrom(Type baseType)
    {
        foreach (var type in m_ScenarioTypes)
        {
            if (baseType.IsAssignableFrom(type))
            {
                yield return type;
            }
        }
    }

    public IReadOnlyCollection<Type> GetDecoratorTypes(Type decoratedType)
        => m_DecoratorsMap.TryGetValue(decoratedType, out var decoratorsList) ? decoratorsList : Array.Empty<Type>();

    struct TypeComparer : IComparer<Type>
    {
        public int Compare(Type x, Type y)
        {
            return string.Compare(x.FullName, y.FullName, StringComparison.Ordinal);
        }
    }
}
