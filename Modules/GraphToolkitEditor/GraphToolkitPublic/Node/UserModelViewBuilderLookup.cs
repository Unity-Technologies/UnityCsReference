// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor;

abstract class UserModelViewBuilderLookup<TUserView> where TUserView : class
{
    protected const BindingFlags k_ConstructorBindingFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public const string k_ConstructorExceptionText = "Exception thrown from constructor of";

    protected readonly Dictionary<Type, ConstructorInfo> m_ConstructorCache = new();

    protected abstract Type BaseModelType { get; }
    protected abstract Type ViewGenericTypeDef { get; }

    protected ConstructorInfo GetOrFindConstructor(Type modelType)
    {
        if (!m_ConstructorCache.TryGetValue(modelType, out var constructor))
        {
            constructor = FindConstructor(modelType);
            m_ConstructorCache[modelType] = constructor;
        }

        return constructor;
    }

    protected TUserView TryInstantiate(ConstructorInfo constructor)
    {
        try
        {
            return (TUserView)constructor.Invoke(Array.Empty<object>());
        }
        catch (Exception e)
        {
            var actual = (e as TargetInvocationException)?.InnerException ?? e;
            Debug.LogError($"{k_ConstructorExceptionText} {constructor.DeclaringType?.Name}");
            Debug.LogException(actual);
            return null;
        }
    }

    ConstructorInfo FindConstructor(Type modelType)
    {
        var current = modelType;
        while (current != null && BaseModelType.IsAssignableFrom(current))
        {
            foreach (var candidate in TypeCache.GetTypesDerivedFrom<TUserView>())
            {
                if (candidate.IsAbstract || candidate.IsGenericTypeDefinition)
                    continue;

                if (!IsViewOf(candidate, current))
                    continue;

                var constructor = candidate.GetConstructor(
                    k_ConstructorBindingFlags,
                    null,
                    Type.EmptyTypes,
                    null);

                if (constructor != null)
                    return constructor;
            }

            current = current.BaseType;
        }

        return null;
    }

    bool IsViewOf(Type candidate, Type modelType)
    {
        var t = candidate;
        while (t != null && t != typeof(object))
        {
            if (t.IsGenericType
                && t.GetGenericTypeDefinition() == ViewGenericTypeDef
                && t.GetGenericArguments()[0] == modelType)
                return true;
            t = t.BaseType;
        }
        return false;
    }
}
