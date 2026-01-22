// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Search;

namespace UnityEditor.Experimental.GraphView
{
    static class GraphViewIndexerExtension
    {
        internal static void IndexCommonData<T>(CustomObjectIndexerTarget context, ObjectIndexer indexer, GraphViewTemplateDescriptor template)
        {
                // Important Notes:
                // Use IndexProperty<PropertyType, PropertyOwnerType> : Give proposition in QueryBuilder
                // Ensure that property name is ALWAYS lower case
                // Prefix <propertyname> with something (ex: PropertyOwnerType) to have a unique property name that won't clash in the QueryBuilder
                // saveKeyword: false -> Ensure the index keyword list won't be polluted with the keyword values.

                indexer.IndexProperty<string, T>(context.documentIndex, $"{template.ToolKey}.category", string.IsNullOrEmpty(template.category) ? TemplateSearchProvider.kUncategorized : template.category, saveKeyword: true);
                indexer.IndexProperty<string, T>(context.documentIndex, $"{template.ToolKey}.description", template.description, saveKeyword: false);
                indexer.IndexProperty<string, T>(context.documentIndex, $"{template.ToolKey}.name", template.name, saveKeyword: true);
                indexer.IndexWord(context.documentIndex, template.name);

                foreach (var label in AssetDatabase.GetLabels(context.target))
                {
                    indexer.IndexProperty<string, T>(context.documentIndex, $"{template.ToolKey}.label", label, saveKeyword: true);
                }

                foreach (var customData in template.customData.GetCustomData())
                {
                    foreach (var value in customData.Value)
                    {
                        indexer.IndexProperty<string, T>(context.documentIndex, $"{template.ToolKey}.{customData.Key}", value, saveKeyword: true);
                    }
                }
        }
    }
}
