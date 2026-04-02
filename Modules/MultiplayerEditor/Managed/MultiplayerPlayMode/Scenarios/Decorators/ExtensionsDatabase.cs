// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

interface IExtensionsDatabase
{
    Hash128 Hash { get; }

    void RegisterInstanceType<TController>()
        where TController : InstanceController;
    void RegisterDecorator<TDecorated, TDecorator>()
        where TDecorator : InstanceControllerDecorator
        where TDecorated : InstanceController;

    IReadOnlyCollection<Type> GetInstanceTypes();
    IEnumerable<Type> GetInstanceTypesDerivedFrom(Type baseType);
    IReadOnlyCollection<Type> GetDecoratorTypes(Type decoratedType);
}

class ExtensionsDatabase : IExtensionsDatabase
{
    Hash128 m_Hash;
    SortedSet<Type> m_InstanceTypes;
    Dictionary<Type, SortedSet<Type>> m_DecoratorsMap;

    public Hash128 Hash => m_Hash;

    public ExtensionsDatabase()
    {
        m_InstanceTypes = new(new TypeComparer());
        m_DecoratorsMap = new();
    }

    void RefreshHash()
    {
        m_Hash = new Hash128();
        foreach (var type in m_InstanceTypes)
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

    public void RegisterInstanceType<TController>() where TController : InstanceController
    {
        m_InstanceTypes.Add(typeof(TController));
        RefreshHash();
    }

    public void RegisterDecorator<TDecorated, TDecorator>()
        where TDecorator : InstanceControllerDecorator
        where TDecorated : InstanceController
    {
        if (!m_InstanceTypes.Contains(typeof(TDecorated)))
        {
            throw new InvalidOperationException($"Cannot register decorator {typeof(TDecorator).Name} for type {typeof(TDecorated).Name} because the decorated type is not registered as an instance type.");
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
