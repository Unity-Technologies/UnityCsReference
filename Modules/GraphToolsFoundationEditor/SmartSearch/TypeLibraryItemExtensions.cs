// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.ItemLibrary.Editor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Extension methods for <see cref="TypeLibraryItem"/>.
    /// </summary>
    [PublicAPI]
    static class TypeLibraryItemExtensions
    {
        const string k_Class = "Classes";

        /// <summary>
        /// Creates a <see cref="ItemLibraryDatabase"/> for types.
        /// </summary>
        /// <param name="types">Types to create the <see cref="ItemLibraryDatabase"/> from.</param>
        /// <returns>A <see cref="ItemLibraryDatabase"/> containing the types passed in parameter.</returns>
        public static ItemLibraryDatabaseBase ToDatabase(this IEnumerable<Type> types)
        {
            var items = new List<ItemLibraryItem>();
            foreach (var type in types)
            {
                var typeHandle = type.GenerateTypeHandle();
                var meta = new TypeMetadata(typeHandle, type);
                if ((meta.IsClass || meta.IsValueType) && !meta.IsEnum)
                {
                    var path = k_Class + "/" + meta.Namespace.Replace(".", "/");
                    var classItem = new TypeLibraryItem(meta.FriendlyName, typeHandle) { CategoryPath = path};
                    items.Add(classItem);
                }
            }
            return new ItemLibraryDatabase(items);
        }
    }
}
