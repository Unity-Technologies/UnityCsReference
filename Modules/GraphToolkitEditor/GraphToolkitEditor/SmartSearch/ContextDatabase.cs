// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.GraphToolkit.ItemLibrary.Editor;
using UnityEditor;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A <see cref="ItemLibraryDatabase"/> for contexts.
    /// </summary>
    class ContextDatabase
    {
        readonly List<ItemLibraryItem> m_Items;
        readonly GraphModel m_GraphModel;
        Type m_ContextType;

        /// <summary>
        /// Creates a ContextDatabase.
        /// </summary>
        /// <param name="graphModel">The graph model to use.</param>
        /// <param name="contextType">The Type of context to use.</param>
        public ContextDatabase(GraphModel graphModel, Type contextType)
        {
            m_GraphModel = graphModel;
            m_Items = new List<ItemLibraryItem>();
            m_ContextType = contextType;
        }

        /// <summary>
        /// Builds the <see cref="ItemLibraryDatabase"/>.
        /// </summary>
        /// <returns>The built <see cref="ItemLibraryDatabase"/>.</returns>
        public ItemLibraryDatabase Build()
        {
            var containerInstance = Activator.CreateInstance(m_ContextType) as ContextNodeModel;

            var types = TypeCache.GetTypesWithAttribute<LibraryItemAttribute>();
            foreach (var type in types)
            {
                var attributes = type.GetCustomAttributes<LibraryItemAttribute>().ToList();
                if (!attributes.Any())
                    continue;

                if (!typeof(BlockNodeModel).IsAssignableFrom(type))
                    continue;

                var blockInstance = Activator.CreateInstance(type) as BlockNodeModel;

                if (blockInstance == null || !blockInstance.IsCompatibleWith(containerInstance))
                    continue;

                foreach (var attribute in attributes)
                {
                    if (!attribute.GraphModelType.IsInstanceOfType(m_GraphModel))
                        continue;

                    ItemLibraryItem.ExtractPathAndNameFromFullName(attribute.Path, out var categoryPath, out var name);
                    var node = new GraphNodeModelLibraryItem(
                        name,
                        new NodeItemLibraryData(type),
                        data => data.CreateBlock(type, contextTypeToCreate: m_ContextType))
                    {
                        CategoryPath = categoryPath,
                        StyleName = attribute.StyleName
                    };
                    m_Items.Add(node);

                    break;
                }
            }

            return new ItemLibraryDatabase(m_Items);
        }
    }
}
