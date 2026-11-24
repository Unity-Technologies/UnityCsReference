// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using UnityEngine.UIElements;

namespace UnityEditor.Networking
{
    internal readonly struct WebRequestProfilerJsonNode
    {
        public readonly string label;

        public WebRequestProfilerJsonNode(string label)
        {
            this.label = label;
        }
    }

    internal static class WebRequestProfilerJsonTree
    {
        const char k_ByteOrderMark = '\uFEFF';

        public static bool IsJsonContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            var parameters = contentType.IndexOf(';');
            var type = (parameters >= 0 ? contentType.Substring(0, parameters) : contentType).Trim();

            return type.EndsWith("/json", StringComparison.OrdinalIgnoreCase)
                || type.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryBuild(string text,
            out List<TreeViewItemData<WebRequestProfilerJsonNode>> roots, out bool incomplete)
        {
            roots = new List<TreeViewItemData<WebRequestProfilerJsonNode>>();
            incomplete = false;

            if (string.IsNullOrEmpty(text))
                return false;

            var bytes = Encoding.UTF8.GetBytes(text.TrimStart(k_ByteOrderMark));

            if (Walk(bytes, isFinalBlock: true, out roots, out incomplete))
                return true;

            return Walk(bytes, isFinalBlock: false, out roots, out incomplete) && incomplete;
        }

        static bool Walk(byte[] bytes, bool isFinalBlock,
            out List<TreeViewItemData<WebRequestProfilerJsonNode>> roots, out bool incomplete)
        {
            roots = new List<TreeViewItemData<WebRequestProfilerJsonNode>>();
            incomplete = false;

            var reader = new Utf8JsonReader(bytes, isFinalBlock, state: default);

            var open = new List<List<TreeViewItemData<WebRequestProfilerJsonNode>>> { roots };
            var names = new List<string>();
            var arrays = new List<bool>();
            var nextId = 0;
            var pendingName = (string)null;
            var sawValue = false;

            try
            {
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            pendingName = EscapeForDisplay(reader.GetString());
                            break;

                        case JsonTokenType.StartObject:
                        case JsonTokenType.StartArray:
                            names.Add(ChildName(pendingName, arrays, open[open.Count - 1].Count));
                            arrays.Add(reader.TokenType == JsonTokenType.StartArray);
                            open.Add(new List<TreeViewItemData<WebRequestProfilerJsonNode>>());
                            pendingName = null;
                            sawValue = true;
                            break;

                        case JsonTokenType.EndObject:
                        case JsonTokenType.EndArray:
                        {
                            var children = open[open.Count - 1];
                            open.RemoveAt(open.Count - 1);
                            var name = names[names.Count - 1];
                            names.RemoveAt(names.Count - 1);
                            arrays.RemoveAt(arrays.Count - 1);

                            var summary = reader.TokenType == JsonTokenType.EndObject
                                ? "{...}"
                                : "[" + children.Count.ToString(CultureInfo.InvariantCulture) + "]";
                            open[open.Count - 1].Add(new TreeViewItemData<WebRequestProfilerJsonNode>(
                                nextId++, new WebRequestProfilerJsonNode(Compose(name, summary)),
                                children));
                            break;
                        }

                        default:
                            open[open.Count - 1].Add(new TreeViewItemData<WebRequestProfilerJsonNode>(
                                nextId++, new WebRequestProfilerJsonNode(
                                    Leaf(ChildName(pendingName, arrays, open[open.Count - 1].Count), ref reader))));
                            pendingName = null;
                            sawValue = true;
                            break;
                    }
                }
            }
            catch (JsonException)
            {
                return false;
            }

            if (open.Count > 1)
            {
                incomplete = true;
                while (open.Count > 1)
                {
                    var children = open[open.Count - 1];
                    open.RemoveAt(open.Count - 1);
                    var name = names[names.Count - 1];
                    names.RemoveAt(names.Count - 1);
                    var brace = arrays[arrays.Count - 1] ? "[...]" : "{...}";
                    arrays.RemoveAt(arrays.Count - 1);

                    open[open.Count - 1].Add(new TreeViewItemData<WebRequestProfilerJsonNode>(
                        nextId++, new WebRequestProfilerJsonNode(Compose(name, brace)),
                        children));
                }
            }

            return sawValue;
        }

        static string ChildName(string pendingName, List<bool> arrays, int siblingCount)
        {
            if (pendingName != null)
                return pendingName;

            var inArray = arrays.Count > 0 && arrays[arrays.Count - 1];
            return inArray ? siblingCount.ToString(CultureInfo.InvariantCulture) : null;
        }

        static string Compose(string name, string value) => name == null ? value : name + ": " + value;

        static string EscapeForDisplay(string value) => value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");

        static string Leaf(string name, ref Utf8JsonReader reader)
        {
            var value = reader.TokenType switch
            {
                JsonTokenType.String => "\"" + EscapeForDisplay(reader.GetString()) + "\"",
                JsonTokenType.True => "true",
                JsonTokenType.False => "false",
                JsonTokenType.Null => "null",
                _ => Encoding.UTF8.GetString(reader.ValueSpan),
            };

            return Compose(name, value);
        }
    }
}
