// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.Search
{
    [Serializable]
    class SearchReport
    {
        public const string extension = "tvc";
        public enum ExportedType
        {
            None, Bool, Number, String, ObjectReference, Color
        }

        public readonly struct ObjectValue
        {
            public readonly UnityEngine.Object objectValue;
            public readonly string fallbackString;

            public ObjectValue(UnityEngine.Object objectValue, string fallbackString)
            {
                this.objectValue = objectValue;
                this.fallbackString = fallbackString;
            }

            public override string ToString()
            {
                if (!string.IsNullOrEmpty(fallbackString))
                    return fallbackString;
                return objectValue?.name ?? string.Empty;
            }
        }

        [Serializable]
        public struct Value
        {
            public ExportedType type;
            public string data;

            public Value(ExportedType type, string data)
            {
                this.type = type;
                this.data = data;
            }

            public Value(object data)
            {
                this.data = ExtractDataValue(data, out type);
            }

            internal static string ExtractDataValue(object data, out ExportedType type, bool convertForCSV = false)
            {
                if (data == null)
                {
                    type = ExportedType.None;
                    return string.Empty;
                }
                else if (data is UnityEngine.Object o)
                {
                    type = ExportedType.ObjectReference;
                    string objectToString = o.name;
                    if (string.IsNullOrEmpty(o.name))
                    {
                        objectToString = AssetDatabase.GetAssetPath(o);
                        if (string.IsNullOrEmpty(objectToString))
                            objectToString = o.GetType().ToString();
                    }
                    if (convertForCSV)
                        return o.ToString();
                    else
                        return $"{GlobalObjectId.GetGlobalObjectIdSlow(o).ToString()};{objectToString}";
                }
                else if (data is SerializedProperty sp)
                {
                    return ExtractDataValue(PropertySelectors.GetSerializedPropertyValue(sp), out type);
                }
                else if (data is MaterialProperty mp)
                {
                    return ExtractDataValue(MaterialSelectors.GetMaterialPropertyValue(mp), out type);
                }
                else if (data is Color color)
                {
                    type = ExportedType.Color;
                    return '#' + ColorUtility.ToHtmlStringRGBA(color);
                }
                else
                {
                    if (data is bool)
                        type = ExportedType.Bool;
                    else if (Utils.TryGetNumber(data, out var number))
                        type = ExportedType.Number;
                    else
                        type = ExportedType.String;
                    return data.ToString();
                }
            }

            internal object TryConvert()
            {
                switch (type)
                {
                    case ExportedType.Bool:
                        bool.TryParse(data, out var resultBool);
                        return resultBool;
                    case ExportedType.Number:
                        double.TryParse(data, out var resultDouble);
                        return resultDouble;
                    case ExportedType.String:
                        return data;
                    case ExportedType.ObjectReference:
                        var parts = data.Split(';');
                        if (parts.Length != 2)
                            return null;
                        if (GlobalObjectId.TryParse(parts[0], out var id))
                            return new ObjectValue(GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id), parts[1]);
                        return data;
                    case ExportedType.Color:
                        ColorUtility.TryParseHtmlString(data, out var resultColor);
                        return resultColor;
                }
                return null;
            }
        }

        [Serializable]
        public struct Field : IEquatable<Field>
        {
            public string name;
            public Value value;

            public Field(string name, Value value)
            {
                this.name = name;
                this.value = value;
            }

            public Field(string name, ExportedType type, string value)
                : this(name, new Value(type, value))
            {
            }

            public Field(string name, object data)
                : this(name, new Value(data))
            {
            }

            public override int GetHashCode() => name.GetHashCode();
            public override bool Equals(object other) => other is Field l && Equals(l);
            public bool Equals(Field other) => name.Equals(other.name);
        }

        [Serializable]
        public struct Item
        {
            public string id;
            public Field[] fields;
        }

        public string query;
        public SearchColumn[] columns;
        public Item[] items;

        public string Export(bool format = true)
        {
            return EditorJsonUtility.ToJson(this, format);
        }

        public static SearchReport Create(SearchTable table)
        {
            return Create(null, table.columns, null);
        }

        public static SearchReport Create(SearchTable table, IEnumerable<SearchItem> items)
        {
            return Create(null as SearchContext, table, items);
        }

        public static SearchReport Create(string query, SearchTable table)
        {
            return Create(query, table, null);
        }

        public static SearchReport Create(string query, SearchTable table, IEnumerable<SearchItem> items)
        {
            var report = Create(table, items);
            report.query = query;
            return report;
        }

        public static SearchReport Create(SearchContext context, SearchTable table)
        {
            return Create(context, table.columns, null);
        }

        public static SearchReport Create(SearchContext context, SearchTable table, IEnumerable<SearchItem> items)
        {
            return Create(context, table.columns, items);
        }

        public static SearchReport Create(SearchContext context, IEnumerable<SearchColumn> columns, IEnumerable<SearchItem> items)
        {
            return new SearchReport
            {
                query = context?.searchText ?? string.Empty,
                columns = columns.ToArray(),
                items = items != null ? items.Select(e => CreateItem(e, context, columns)).ToArray() : (new Item[0])
            };
        }

        private static Item CreateItem(SearchItem e, SearchContext context, IEnumerable<SearchColumn> columns)
        {
            var v = e.GetValue(null, context);
            var ri = new Item()
            {
                id = e.id,
            };

            var fields = new HashSet<Field>();
            foreach (var fieldName in e.GetFieldNames())
                fields.Add(new Field(fieldName, e.GetValue(fieldName, context)));

            foreach (var column in columns)
                fields.Add(new Field(column.selector, column.ResolveValue(e, context)));
            ri.fields = fields.ToArray();
            return ri;
        }

        internal IEnumerable<SearchItem> CreateSearchItems(SearchContext context, SearchProvider provider)
        {
            foreach (var item in items)
            {
                var newItem = provider.CreateItem(context, Guid.NewGuid().ToString("N"));
                for (int i = 0; i < item.fields.Length; ++i)
                    newItem.SetField(item.fields[i].name, item.fields[i].value.TryConvert());
                yield return newItem;
            }
        }

        public static string Export(SearchContext context, IEnumerable<SearchColumn> columns, IEnumerable<SearchItem> items)
        {
            return Create(context, columns, items).Export();
        }

        public static string ExportAsCsv(SearchContext context, IEnumerable<SearchColumn> columns, IEnumerable<SearchItem> items)
        {
            if (!columns.Any())
                return string.Empty;

            var sb = new StringBuilder();
            // Column titles
            foreach (var column in columns)
            {
                sb.Append(EscapeCSVStringIfNeeded(column.content.text));
                sb.Append(",");
            }
            sb.Remove(sb.Length - 1, 1); // remove last ,
            sb.AppendLine();

            // Items
            foreach (var item in items)
            {
                foreach (var column in columns)
                {
                    sb.Append(EscapeCSVStringIfNeeded(Value.ExtractDataValue(column.ResolveValue(item, context), out var type, true)));
                    sb.Append(",");
                }
                sb.Remove(sb.Length - 1, 1); // remove last ,
                sb.AppendLine();
            }

            sb.Remove(sb.Length - Environment.NewLine.Length, Environment.NewLine.Length); // remove last new line
            return sb.ToString();
        }

        private static string EscapeCSVStringIfNeeded(string text)
        {
            if (text == null)
                return string.Empty;
            var stringParts = text.Split('"');
            // we must double "
            if (stringParts.Length > 1)
            {
                var sb = new StringBuilder();
                sb.Append('"');
                foreach (var part in stringParts)
                {
                    sb.Append(part);
                    sb.Append("\"\""); // " are doubled
                }
                // string now ends with "", we must remove 1 "
                sb.Remove(sb.Length - 1, 1);
                return sb.ToString();
            }
            if (text.Contains(',') || text.Contains(Environment.NewLine))
                return '"' + text + '"';
            return text;
        }

        public static string searchReportFolder
        {
            get => EditorPrefs.GetString(nameof(searchReportFolder), SearchSettings.GetFullQueryFolderPath());
            set => EditorPrefs.SetString(nameof(searchReportFolder), value);
        }

        static internal void Export(string name, IEnumerable<SearchColumn> columns, IEnumerable<SearchItem> items, SearchContext context)
        {
            var tcPath = EditorUtility.SaveFilePanel("Export report...", searchReportFolder, name, SearchReport.extension);
            if (string.IsNullOrEmpty(tcPath))
                return;
            Save(tcPath, context, columns, items);
            searchReportFolder = tcPath;
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchExportReport, "Report");
        }

        static internal void ExportAsCsv(string name, IEnumerable<SearchColumn> columns, IEnumerable<SearchItem> items, SearchContext context)
        {
            var tcPath = EditorUtility.SaveFilePanel("Export report as csv...", searchReportFolder, name, "csv");
            if (string.IsNullOrEmpty(tcPath))
                return;
            SaveAsCsv(tcPath, context, columns, items);
            searchReportFolder = tcPath;
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchExportReport, "CSV");
        }

        public static void Save(string path, SearchContext context, IEnumerable<SearchColumn> columns, IEnumerable<SearchItem> items)
        {
            FileUtil.WriteTextFileToDisk(path, Export(context, columns, items));
        }

        public static void SaveAsCsv(string path, SearchContext context, IEnumerable<SearchColumn> columns, IEnumerable<SearchItem> items)
        {
            var text = ExportAsCsv(context, columns, items);
            try
            {
                FileUtil.WriteTextFileToDisk(path, text);
            }
            catch (Exception)
            {
                Debug.LogError($"File could not be saved at {path}");
            }
        }

        public static SearchReport Load(string fileContent)
        {
            var searchReport = new SearchReport();
            EditorJsonUtility.FromJsonOverwrite(fileContent, searchReport);
            return searchReport;
        }

        public static SearchReport LoadFromFile(string filePath)
        {
            return Load(File.ReadAllText(filePath));
        }

        public static string Import()
        {
            var tcPath = EditorUtility.OpenFilePanel("Import report...", searchReportFolder, extension);
            if (string.IsNullOrEmpty(tcPath))
                return null;
            searchReportFolder = tcPath;
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchImportReport);
            return tcPath;
        }
    }
}
