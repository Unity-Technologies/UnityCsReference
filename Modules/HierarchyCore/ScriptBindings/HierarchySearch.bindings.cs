// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Filter operator. Colon (:) is considered to be "contains" operator. "-" is considered to be "not" operator.
    /// </summary>
    [NativeType("Modules/HierarchyCore/Public/HierarchySearch.h")]
    public enum HierarchySearchFilterOperator
    {
        /// <summary>
        /// Check if a filter value is equal from right hand operand.
        /// </summary>
        Equal,
        /// <summary>
        /// Compare filter value with contains. (ex: with a string value this means string.Contains()
        /// </summary>
        Contains,
        /// <summary>
        /// Check if a numerical filter value is greater than right hand operand.
        /// </summary>
        Greater,
        /// <summary>
        /// Check if a numerical filter value is greater or equal than right hand operand.
        /// </summary>
        GreaterOrEqual,
        /// <summary>
        /// Check if a numerical filter value is lesser than right hand operand.
        /// </summary>
        Lesser,
        /// <summary>
        /// Check if a numerical filter value is lesser or equal than right hand operand.
        /// </summary>
        LesserOrEqual,
        /// <summary>
        /// Check if a filter value is different from right hand operand.
        /// </summary>
        NotEqual,
        /// <summary>
        /// Does a not operation on a filter value: this means all items NOT matching the filter.
        /// </summary>
        Not
    }

    /// <summary>
    /// Encapsulate all data needed to filter a hierarchy.
    /// </summary>
    [NativeType("Modules/HierarchyCore/Public/HierarchySearch.h")]
    [RequiredByNativeCode, StructLayout(LayoutKind.Sequential), Serializable]
    public struct HierarchySearchFilter
    {
        static readonly char[] s_WhiteSpaces = { ' ', '\t', '\n' };
        static readonly HierarchySearchFilter s_Invalid;

        /// <summary>
        /// Default invalid HierarchySearchFilter. This assume the Hierarchy has a query, but is invalid, so no nodes would be shown.
        /// </summary>
        public static ref readonly HierarchySearchFilter Invalid => ref s_Invalid;

        /// <summary>
        /// Is the filter valid: does it have a name.
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(Name);

        /// <summary>
        /// Filter name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Filter textual value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Filter numerical value.
        /// </summary>
        public float NumValue { get; set; }

        /// <summary>
        /// Filter operator.
        /// </summary>
        public HierarchySearchFilterOperator Op { get; set; }

        /// <summary>
        /// Convert an operator to its textual value.
        /// </summary>
        /// <param name="op">Filter operator</param>
        /// <returns>Operator textual value.</returns>
        public static string ToString(HierarchySearchFilterOperator op)
        {
            switch (op)
            {
                case HierarchySearchFilterOperator.Equal:
                    return "=";
                case HierarchySearchFilterOperator.Contains:
                    return ":";
                case HierarchySearchFilterOperator.Greater:
                    return ">";
                case HierarchySearchFilterOperator.GreaterOrEqual:
                    return ">=";
                case HierarchySearchFilterOperator.Lesser:
                    return "<";
                case HierarchySearchFilterOperator.LesserOrEqual:
                    return "<=";
                case HierarchySearchFilterOperator.NotEqual:
                    return "!=";
                case HierarchySearchFilterOperator.Not:
                    return "-";
                default:
                    throw new NotImplementedException($"Cannot convert {op} to string");
            }
        }

        /// <summary>
        /// Convert a textual value to its operator value if possible. 
        /// </summary>
        /// <param name="op">Textual operator. Ex: =, <=, : ... </param>
        /// <returns></returns>
        public static HierarchySearchFilterOperator ToOp(string op)
        {
            switch (op)
            {
                case "<":
                    return HierarchySearchFilterOperator.Lesser;
                case "<=":
                    return HierarchySearchFilterOperator.LesserOrEqual;
                case ">":
                    return HierarchySearchFilterOperator.Greater;
                case ">=":
                    return HierarchySearchFilterOperator.GreaterOrEqual;
                case "=":
                    return HierarchySearchFilterOperator.Equal;
                case ":":
                    return HierarchySearchFilterOperator.Contains;
                case "!=":
                    return HierarchySearchFilterOperator.NotEqual;
                case "-":
                    return HierarchySearchFilterOperator.Not;
                default:
                    throw new NotImplementedException($"Cannot convert {op} to SearchFilterOperator");
            }
        }

        /// <summary>
        /// Convert a Filter to its full textual value: <FilerName><Filter operator><FilterValue>. The textual value will be escaped with doublequotes if needed.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var rhs = float.IsNaN(NumValue) ? Value : NumValue.ToString();
            return $"{Name}{ToString(Op)}{QuoteStringIfNeeded(rhs)}";
        }

        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal static HierarchySearchFilter CreateFilter(string name, string op, string value)
        {
            return CreateFilter(name, ToOp(op), value);
        }

        internal static HierarchySearchFilter CreateFilter(string name, HierarchySearchFilterOperator op, string str)
        {
            var value = str;
            var numValue = float.NaN;
            try
            {
                numValue = Convert.ToSingle(str);
                value = null;
            }
            catch (System.Exception)
            {
            }
            return new HierarchySearchFilter()
            {
                Name = name,
                Op = op,
                Value = value,
                NumValue = numValue
            };
        }

        internal static string QuoteStringIfNeeded(string s)
        {
            if (s.Length > 0 && s.IndexOfAny(s_WhiteSpaces) != -1 && s[0] != '"')
            {
                return $"\"{s}\"";
            }
            return s;
        }
    }

    /// <summary>
    /// Encapsulate all the query filters and text values that are used to filter a Hierarchy.
    /// </summary>
    [NativeType("Modules/HierarchyCore/Public/HierarchySearch.h"), NativeAsStruct]
    [RequiredByNativeCode, StructLayout(LayoutKind.Sequential), Serializable]
    public sealed class HierarchySearchQueryDescriptor
    {
        static readonly HashSet<string> s_SystemFilters = new HashSet<string>(new[] { "nodetype", "strict" });
        static readonly HierarchySearchQueryDescriptor s_Empty = new HierarchySearchQueryDescriptor();
        static readonly HierarchySearchQueryDescriptor s_InvalidQuery = new HierarchySearchQueryDescriptor() { Invalid = true };

        /// <summary>
        /// Default Empty query;
        /// </summary>
        public static HierarchySearchQueryDescriptor Empty => s_Empty;

        /// <summary>
        /// Default Invalid query.
        /// </summary>
        public static HierarchySearchQueryDescriptor InvalidQuery => s_InvalidQuery;

        /// <summary>
        /// Filters used by the Hierarchy. Filters are of the form [filterName][operator][filterValue]. Ex: nodetype:gameobject. These filters are global to all NodeHandlers.
        /// </summary>
        public HierarchySearchFilter[] SystemFilters { get; set; }

        /// <summary>
        /// User define filters. Filters are of the form [filterName][operator][filterValue]. Ex: t:Light. Each of these filters can be used by a NodeHandler to filter according to domain specific characteristics.
        /// </summary>
        public HierarchySearchFilter[] Filters { get; set; }

        /// <summary>
        /// All textual values. ex: "cube"
        /// </summary>
        public string[] TextValues { get; set; }

        /// <summary>
        /// Is the query evaluated strictly. This means if any filters is considered to be invalid the whole query is invalid.
        /// </summary>
        public bool Strict { get; set; }

        /// <summary>
        /// Is the query invalid. An invalid query will yield no node.
        /// </summary>
        public bool Invalid { get; set; }

        /// <summary>
        /// Is the query value.
        /// </summary>
        public bool IsValid => !Invalid && !IsEmpty;

        /// <summary>
        /// Is the query empty
        /// </summary>
        public bool IsEmpty => Filters.Length == 0 && TextValues.Length == 0 && SystemFilters.Length == 0;

        /// <summary>
        /// Is the query only using System filters. This means NodeHandlers won't be called for filtering.
        /// </summary>
        public bool IsSystemOnlyQuery => SystemFilters.Length > 0 && Filters.Length == 0 && TextValues.Length == 0;

        /// <summary>
        /// Constructor for a Query.
        /// </summary>
        /// <param name="filters">List of user filters</param>
        /// <param name="textValues">List of textual values.</param>
        public HierarchySearchQueryDescriptor(HierarchySearchFilter[] filters = null, string[] textValues = null)
        {
            filters = filters ?? new HierarchySearchFilter[0];
            textValues = textValues ?? new string[0];
            Filters = Where(filters, f => !s_SystemFilters.Contains(f.Name));
            SystemFilters = Where(filters, f => s_SystemFilters.Contains(f.Name));
            TextValues = textValues;
            var strictFilter = HierarchySearchFilter.Invalid;
            foreach (var f in SystemFilters)
            {
                if (f.Name == "strict")
                {
                    strictFilter = f;
                    break;
                }
            }

            Invalid = false;
            Strict = !strictFilter.IsValid || strictFilter.Value == "true";
        }
        /// <summary>
        /// Copy constructor for a Query.
        /// </summary>
        /// <param name="desc">Query to copy.</param>
        public HierarchySearchQueryDescriptor(HierarchySearchQueryDescriptor desc)
        {
            SystemFilters = new HierarchySearchFilter[desc.SystemFilters.Length];
            Array.Copy(desc.SystemFilters, SystemFilters, desc.SystemFilters.Length);
            Filters = new HierarchySearchFilter[desc.Filters.Length];
            Array.Copy(desc.Filters, Filters, desc.Filters.Length);
            TextValues = new string[desc.TextValues.Length];
            Array.Copy(desc.TextValues, TextValues, desc.TextValues.Length);
            Strict = desc.Strict;
            Invalid = desc.Invalid;
        }

        /// <summary>
        /// Convert the query to textual form. A textual query is of the form <All system Filters>  <All user filters> <All textual values.>
        /// </summary>
        /// <returns>return a text query.</returns>
        public override string ToString()
        {
            return BuildQuery();
        }

        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal string BuildFilterQuery()
        {
            return string.Join(" ", Filters);
        }

        internal string BuildSystemFilterQuery()
        {
            return string.Join(" ", SystemFilters);
        }

        internal string BuildTextQuery()
        {
            var textValues = new string[TextValues.Length];
            for (var i = 0; i < textValues.Length; ++i)
                textValues[i] = HierarchySearchFilter.QuoteStringIfNeeded(TextValues[i]);
            return string.Join(" ", textValues);
        }

        internal string BuildQuery()
        {
            var query = "";
            if (SystemFilters.Length > 0)
            {
                query += BuildSystemFilterQuery();
            }

            if (Filters.Length > 0)
            {
                if (query.Length > 0)
                    query += " ";
                query += BuildFilterQuery();
            }

            if (TextValues.Length > 0)
            {
                if (query.Length > 0)
                    query += " ";
                query += BuildTextQuery();
            }

            return query;
        }

        private static T[] Where<T>(IEnumerable<T> src, Func<T, bool> pred)
        {
            var count = 0;
            foreach (var e in src)
                if (pred(e))
                    count++;

            var a = new T[count];
            var i = 0;
            foreach (var e in src)
                if (pred(e))
                    a[i++] = e;
            return a;
        }
    }
}
