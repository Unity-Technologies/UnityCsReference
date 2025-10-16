// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Utilities/NamespaceBuilding.h")]
    internal struct Utilities
    {
        static internal void AddToList<T>(ref List<T> list, T value)
        {
            if (list == null)
                list = new List<T>();
            list.Add(value);
        }

        [FreeFunction("ShaderFoundry::BuildSymbolNamespace")]
        extern internal static string BuildSymbolNamespace(string symbolName, DataType dataType);

        static internal Namespace BuildSymbolNamespace(ShaderContainer container, string objectName, DataType dataType)
        {
            string namespaceName = BuildSymbolNamespace(objectName, dataType);
            var builder = new Namespace.Builder(container, namespaceName);
            return builder.Build();
        }
    }
    static class SelectExtensions
    {
        static internal IEnumerable<StructField> Select(this IEnumerable<StructField> items, StructFieldInternal.Flags flags)
        {
            foreach (var item in items)
            {
                if (item.HasFlag(flags))
                    yield return item;
            }
        }
        static internal IEnumerable<FunctionParameter> Select(this IEnumerable<FunctionParameter> items, FunctionParameterInternal.Flags flags)
        {
            foreach (var item in items)
            {
                if (item.HasFlag(flags))
                    yield return item;
            }
        }
    }
}
