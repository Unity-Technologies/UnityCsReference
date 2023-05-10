// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Unity.GraphToolsFoundation;

static class CollectionExtensions
{
    public static bool ContainsReference<T>(this IEnumerable<T> container, T search) where T : class
    {
        foreach (var element in container)
        {
            if (ReferenceEquals(element,search))
                return true;
        }

        return false;
    }

    public static List<T> OfTypeToList<T, T2>(this IEnumerable<T2> list)
    {
        var result = new List<T>();
        foreach (var element in list)
            if( element is T tElement)
                result.Add(tElement);

        return result;
    }

    public static IDisposable OfTypeToPooledList<T, T2>(this IEnumerable<T2> original, out List<T> filtered)
    {
        var handle = ListPool<T>.Get(out filtered);
        foreach (var element in original)
            if( element is T tElement)
                filtered.Add(tElement);

        return handle;
    }

    public static IReadOnlyList<T2> Cast<T1, T2>(this IReadOnlyList<T1> list)
    {
        if (typeof(T1) == typeof(T2))
            return (IReadOnlyList<T2>)list;
        var result = new List<T2>(list.Count);
        foreach (var element in list)
        {
            if (element is T2 t2)
                result.Add(t2);
            else
                throw new InvalidCastException($"Cannot cast from type {typeof(T1).FullName} to type {typeof(T2).FullName}");
        }

        return result;
    }
}
