// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    static class InterfaceGetterExtension
    {
        static Dictionary<Type, IReadOnlyList<Type>> s_InterfacesCache = new();
        public static IReadOnlyList<Type> GetDirectInterfaces(this Type type)
        {
            if (s_InterfacesCache.TryGetValue(type, out var interfaces))
                return interfaces;

            var interfacesList = new List<Type>();
            interfacesList.AddRange(type.GetInterfaces());

            var baseType = type.BaseType;
            if (baseType != null)
            {
                foreach (var inter in baseType.GetInterfaces())
                {
                    interfacesList.Remove(inter);
                }
                interfacesList.Capacity = interfacesList.Count;
            }

            s_InterfacesCache[type] = interfacesList;

            return interfacesList;
        }
    }
}
