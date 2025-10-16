
using System;
using System.Collections.Generic;


namespace Unity.DataModel
{
internal static partial class TypeTraitsRegistry
{
    private static readonly Dictionary<Type, TypeTraitsData> _registry = new();

    internal static void Register<T>(TypeTraitsData traitsData)
    {
        var type = typeof(T);
        if (!_registry.TryAdd(type, traitsData))
        {
            throw new ArgumentException($"Type has already been registered: {type.Name}");
        }
    }

    internal static bool TryGet(Type type, out TypeTraitsData traitsData)
    {
        return _registry.TryGetValue(type, out traitsData);
    }

    internal static IEnumerable<KeyValuePair<Type, TypeTraitsData>> GetAll()
    {
        return _registry;
    }

    internal static void Clear()
    {
        _registry.Clear();
    }
}
}
