// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

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

                var descriptionKey = $"{template.ToolKey}.description".ToLowerInvariant();
                var document = new Dictionary<string, List<string>>();
                AppendCommonData(template, AssetDatabase.GetLabels(context.target), document);
                foreach (var entry in document)
                {
                    // Description is the one field we don't offer as a search suggestion.
                    var saveKeyword = entry.Key != descriptionKey;
                    foreach (var value in entry.Value)
                        indexer.IndexProperty<string, T>(context.documentIndex, entry.Key, value, saveKeyword);
                }
                IndexWords<T>(context, indexer, document);
        }

        internal static void IndexCustomData<T>(CustomObjectIndexerTarget context, ObjectIndexer indexer, Dictionary<string, List<string>> customDataCollection)
        {
            var document = new Dictionary<string, List<string>>();
            AppendCustomData(customDataCollection, document);
            foreach (var entry in document)
            {
                foreach (var value in entry.Value)
                    indexer.IndexProperty<string, T>(context.documentIndex, entry.Key, value, saveKeyword: true);
            }
            IndexWords<T>(context, indexer, document);
        }

        internal static bool IsWordSearchable(string documentKey) =>
            !documentKey.EndsWith(".description") && !documentKey.EndsWith(".label");

        static void IndexWords<T>(CustomObjectIndexerTarget context, ObjectIndexer indexer, Dictionary<string, List<string>> document)
        {
            foreach (var entry in document)
            {
                if (!IsWordSearchable(entry.Key))
                    continue;
                foreach (var value in entry.Value)
                    indexer.IndexWordComponents(context.documentIndex, value);
            }
        }

        // The same data QuickSearch stores for a template, so we can match it without the index.
        internal static Dictionary<string, List<string>> BuildSearchDocument(GraphViewTemplateDescriptor template, IEnumerable<string> labels)
        {
            var document = new Dictionary<string, List<string>>();
            AppendCommonData(template, labels, document);
            AppendCustomData(template.searchTerms.GetCustomData($"{template.ToolKey}."), document);
            return document;
        }

        static void AppendCommonData(GraphViewTemplateDescriptor template, IEnumerable<string> labels, Dictionary<string, List<string>> document)
        {
            Add(document, $"{template.ToolKey}.category", SanitizeCategory(template.category));
            Add(document, $"{template.ToolKey}.description", template.description);
            Add(document, $"{template.ToolKey}.name", template.name);
            if (labels != null)
            {
                foreach (var label in labels)
                    Add(document, $"{template.ToolKey}.label", label);
            }
        }

        static void AppendCustomData(Dictionary<string, List<string>> customDataCollection, Dictionary<string, List<string>> document)
        {
            if (customDataCollection == null)
                return;

            foreach (var customData in customDataCollection)
            {
                foreach (var value in customData.Value)
                    Add(document, customData.Key, value);
            }
        }

        // Match how QuickSearch stores entries: lower-case keys/values and skip empties.
        static void Add(Dictionary<string, List<string>> document, string key, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            key = key.ToLowerInvariant();
            value = value.ToLowerInvariant();
            if (!document.TryGetValue(key, out var values))
            {
                values = new List<string>();
                document[key] = values;
            }
            values.Add(value);
        }

        static string SanitizeCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return TemplateSearchProvider.kUncategorized;
            }

            return category
                .Replace('/', '_')
                .Replace('\\', '_');
        }
    }
}
