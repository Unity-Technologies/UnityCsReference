// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.GraphToolkit.ItemLibrary.Editor;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Extension methods for <see cref="TypeLibraryItem"/>.
    /// </summary>
    [PublicAPI]
    [UnityRestricted]
    internal static class TypeLibraryItemExtensions
    {
        const string k_Class = "Classes";

        /// <summary>
        /// Creates a <see cref="ItemLibraryDatabase"/> for types.
        /// </summary>
        /// <param name="types">Types to create the <see cref="ItemLibraryDatabase"/> from.</param>
        /// <param name="graphModel">The graph model associated with the database</param>
        /// <returns>A <see cref="ItemLibraryDatabase"/> containing the types passed in parameter.</returns>
        public static ItemLibraryDatabaseBase ToDatabase(this IEnumerable<Type> types, GraphModel graphModel)
        {
            return ToDatabase(types, t => t.GenerateTypeHandle(), graphModel);
        }

        /// <summary>
        /// Creates a <see cref="ItemLibraryDatabase"/> for types.
        /// </summary>
        /// <param name="types">Types to create the <see cref="ItemLibraryDatabase"/> from.</param>
        /// <param name="graphModel">The graph model associated with the database</param>
        /// <returns>A <see cref="ItemLibraryDatabase"/> containing the types passed in parameter.</returns>
        public static ItemLibraryDatabaseBase ToDatabase(this IEnumerable<TypeHandle> types, GraphModel graphModel)
        {
            return ToDatabase(types, t => t, graphModel);
        }

        static ItemLibraryDatabaseBase ToDatabase<T>(this IEnumerable<T> types, Func<T, TypeHandle> func, GraphModel graphModel)
        {
            var items = new List<ItemLibraryItem>();
            foreach (var item in types)
            {
                var typeHandle = func(item);
                var type = typeHandle.Resolve();
                if (type.IsClass || type.IsValueType)
                {
                    var classItem = new TypeLibraryItem(TypeHelpers.GetFriendlyName(type), typeHandle, graphModel);
                    items.Add(classItem);
                }
            }
            return new ItemLibraryDatabase(items);
        }
    }
}
