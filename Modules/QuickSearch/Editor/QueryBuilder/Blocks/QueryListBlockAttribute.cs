// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Search
{
    [AttributeUsage(AttributeTargets.Class)]
    public class QueryListBlockAttribute : Attribute
    {
        internal class ListBlockData
        {
            public QueryListBlockAttribute attribute;

            private QueryListBlock m_TemplateBlock;
            public QueryListBlock templateBlock => m_TemplateBlock ??= (QueryListBlock)Activator.CreateInstance(attribute.type, new object[] { null, attribute.id, string.Empty, attribute });
        }

        static Dictionary<string, ListBlockData> s_IdToAttribute;
        static Dictionary<Type, ListBlockData> s_TypeToAttribute;

        public QueryListBlockAttribute(string category, string name, string id, string op = "=")
            : this(category, name, new []{id}, op, 0)
        {}

        public QueryListBlockAttribute(string category, string name, string[] ids, string op = "=")
            : this(category, name, ids, op, 0)
        {}

        internal QueryListBlockAttribute(string category, string name, string id, string op, int priority = 0)
            : this(category, name, new[] { id }, op, priority)
        {}

        internal QueryListBlockAttribute(string category, string name, string[] ids, string op, int priority = 0)
        {
            this.ids = ids ?? new string[] { };
            this.category = category;
            this.name = name;
            this.op = op;
            this.priority = priority;
        }

        public string[] ids { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public string op { get; set; }
        public Type type { get; set; }
        internal int priority { get; private set; }

        public string id => ids.Length > 0 ? ids[0] : string.Empty;

        public override string ToString()
        {
            return $"{category}{op}{name} ({string.Join(',', ids)}) priority:{priority}";
        }

        internal static QueryListBlock CreateBlock(Type type, IQuerySource source, string value)
        {
            var attr = FindBlock(type)?.attribute;
            if (attr != null)
                return (QueryListBlock)Activator.CreateInstance(type, new object[] { source, attr.id, value, attr });
            return null;
        }

        internal static QueryListBlock CreateBlock(string id, string op, IQuerySource source, string value)
        {
            var listBlockData = FindBlock(id);
            QueryMarker.TryParse(value, out var marker);
            var isValidMarker = marker.valid && marker.type == "list";
            if (listBlockData != null)
            {
                if (isValidMarker)
                {
                    return new QueryListMarkerBlock(source, id, marker, listBlockData.attribute);
                }

                var block = (QueryListBlock)Activator.CreateInstance(listBlockData.attribute.type, new object[] { source, id, value, listBlockData.attribute });
                block.op = op;
                return block;
            }
            else if (isValidMarker)
            {
                return new QueryListMarkerBlock(source, id, id, op, marker);
            }
            return null;
        }

        internal static ListBlockData FindBlock(Type t)
        {
            if (s_IdToAttribute == null)
                RefreshQueryListBlock();
            return s_TypeToAttribute.GetValueOrDefault(t);
        }

        internal static ListBlockData FindBlock(string id)
        {
            if (s_IdToAttribute == null)
                RefreshQueryListBlock();

            return s_IdToAttribute.GetValueOrDefault(id);
        }

        internal static void RefreshQueryListBlock()
        {
            s_IdToAttribute = new();
            s_TypeToAttribute = new();

            var types = TypeCache.GetTypesWithAttribute<QueryListBlockAttribute>();
            foreach (var ti in types)
            {
                try
                {
                    var attr = ti.GetCustomAttributes(typeof(QueryListBlockAttribute), false).Cast<QueryListBlockAttribute>().First();
                    attr.type = ti;
                    if (!typeof(QueryListBlock).IsAssignableFrom(ti))
                        continue;

                    var listBlockData = new ListBlockData() { attribute = attr };

                    s_TypeToAttribute[attr.type] = listBlockData;
                    foreach (var id in attr.ids)
                    {
                        if (!s_IdToAttribute.TryGetValue(id, out var alreadyExists) || alreadyExists.attribute.priority > attr.priority)
                        {
                            s_IdToAttribute[id] = listBlockData;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Cannot register QueryListBlock provider: {ti.Name}\n{e}");
                }
            }
        }

        internal static bool TryGetReplacement(string id, string type, ref Type blockType, out string replacement)
        {
            var blockData = FindBlock(id);
            if (blockData != null)
                return blockData.templateBlock.TryGetReplacement(id, type, ref blockType, out replacement);
            replacement = string.Empty;
            return false;
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static IEnumerable<SearchProposition> GetPropositions(Type type)
        {
            var blockData = FindBlock(type);
            if (blockData != null)
                return blockData.templateBlock.GetPropositions();
            return Array.Empty<SearchProposition>();
        }
    }
}
