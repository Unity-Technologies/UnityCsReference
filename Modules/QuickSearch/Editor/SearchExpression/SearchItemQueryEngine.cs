// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

namespace UnityEditor.Search
{
    class SearchItemQueryEngine : QueryEngine<SearchItem>
    {
        SearchExpressionContext m_Context;

        struct PropertyValue
        {
            public enum Type
            {
                String,
                Boolean,
                Double,
                Int,
                Object
            }

            public PropertyValue(object v)
            {
                value = v;
                if (v is string)
                    type = Type.String;
                else if (v is float f)
                {
                    type = Type.Double;
                    value = (double)f;
                }
                else if (v is double)
                    type = Type.Double;
                else if (v is int)
                    type = Type.Int;
                else if (v is bool)
                    type = Type.Boolean;
                else
                    type = Type.Object;
            }

            public Type type;
            public object value;

            public bool IsNumber()
            {
                return type == Type.Int || type == Type.Double;
            }

            public int ToInt()
            {
                if (type == Type.Int)
                    return (int)value;

                if (type == Type.Double)
                    return (int)(double)value;

                throw new System.Exception($"Cannot convert property value to number {value}");
            }

            public double ToDouble()
            {
                if (type == Type.Int)
                    return (int)value;

                if (type == Type.Double)
                    return (double)value;

                throw new System.Exception($"Cannot convert property value to number {value}");
            }

            public bool ToBool()
            {
                if (type == Type.Int)
                    return (int)value != 0;

                if (type == Type.Double)
                    return (double)value != 0.0;

                if (type == Type.Boolean)
                    return (bool)value;

                return value != null;
            }

            public override string ToString()
            {
                return value.ToString();
            }
        }

        public SearchItemQueryEngine()
        {
            Setup();
        }

        public IEnumerable<SearchItem> Where(SearchExpressionContext context, IEnumerable<SearchItem> dataSet, string queryStr)
        {
            queryStr = ConvertSelectors(queryStr);

            m_Context = context;
            var query = Parse(queryStr, true);
            if (query.errors.Count != 0)
            {
                foreach (var queryError in query.errors)
                {
                    Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, $"Error parsing input at {queryError.index}: {queryError.reason}");
                }

                var errorStr = string.Join("\n", query.errors.Select(err => $"Error parsing input at {err.index}: {err.reason}"));
                context.ThrowError(errorStr);
            }

            foreach (var item in dataSet)
            {
                if (item != null)
                {
                    if (query.Test(item))
                        yield return item;
                }
                else
                    yield return null;
            }
            m_Context = default;
        }

        public IEnumerable<SearchItem> WhereMainThread(SearchExpressionContext context, IEnumerable<SearchItem> dataSet, string queryStr)
        {
            queryStr = ConvertSelectors(queryStr);

            m_Context = context;
            var query = Parse(queryStr, true);
            if (query.errors.Count != 0)
            {
                foreach (var queryError in query.errors)
                {
                    Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, $"Error parsing input at {queryError.index}: {queryError.reason}");
                }

                var errorStr = string.Join("\n", query.errors.Select(err => $"Error parsing input at {err.index}: {err.reason}"));
                context.ThrowError(errorStr);
            }

            var results =  TaskEvaluatorManager.EvaluateMainThread(dataSet, item =>
            {
                if (query.Test(item))
                    return item;
                return null;
            }, 25);
            m_Context = default;
            return results;
        }

        static string ConvertSelectors(string queryStr)
        {
            var re = new Regex(ParserUtils.k_QueryWithSelectorPattern);
            var evaluator = new MatchEvaluator(match => {
                return match.Value.Replace(match.Groups[2].Value, $"p({match.Groups[2].Value.Substring(1)})");
            });
            var sanitizeQuery = re.Replace(queryStr, evaluator);
            return sanitizeQuery;
        }

        private void Setup()
        {
            AddFilter("p", GetValue, s => s, StringComparison.OrdinalIgnoreCase);

            AddOperatorHandler("=", (PropertyValue v, int fv) => v.IsNumber() && v.ToInt() == fv);
            AddOperatorHandler("!=", (PropertyValue v, int fv) => v.IsNumber() && v.ToInt() != fv);
            AddOperatorHandler("<=", (PropertyValue v, int fv) => v.IsNumber() && v.ToInt() <= fv);
            AddOperatorHandler("<", (PropertyValue v, int fv) => v.IsNumber() && v.ToInt() < fv);
            AddOperatorHandler(">=", (PropertyValue v, int fv) => v.IsNumber() && v.ToInt() >= fv);
            AddOperatorHandler(">", (PropertyValue v, int fv) => v.IsNumber() && v.ToInt() > fv);

            AddOperatorHandler("=", (PropertyValue v, double fv) => v.IsNumber() && v.ToDouble() == fv);
            AddOperatorHandler("!=", (PropertyValue v, double fv) => v.IsNumber() && v.ToDouble() != fv);
            AddOperatorHandler("<=", (PropertyValue v, double fv) => v.IsNumber() && v.ToDouble() <= fv);
            AddOperatorHandler("<", (PropertyValue v, double fv) => v.IsNumber() && v.ToDouble() < fv);
            AddOperatorHandler(">=", (PropertyValue v, double fv) => v.IsNumber() && v.ToDouble() >= fv);
            AddOperatorHandler(">", (PropertyValue v, double fv) => v.IsNumber() && v.ToDouble() > fv);

            AddOperatorHandler(":", (PropertyValue v, string fv, StringComparison sc) => v.value != null && StringContains(v.ToString(), fv, sc));

            AddOperatorHandler("=", (PropertyValue v, string fv, StringComparison sc) => v.value != null && string.Equals(v.ToString(), fv, sc));
            AddOperatorHandler("!=", (PropertyValue v, string fv, StringComparison sc) => v.value != null && !string.Equals(v.ToString(), fv, sc));
            AddOperatorHandler("<=", (PropertyValue v, string fv, StringComparison sc) => v.value != null && string.Compare(v.ToString(), fv, sc) <= 0);
            AddOperatorHandler("<", (PropertyValue v, string fv, StringComparison sc) => v.value != null && string.Compare(v.ToString(), fv, sc) < 0);
            AddOperatorHandler(">", (PropertyValue v, string fv, StringComparison sc) => v.value != null && string.Compare(v.ToString(), fv, sc) > 0);
            AddOperatorHandler(">=", (PropertyValue v, string fv, StringComparison sc) => v.value != null && string.Compare(v.ToString(), fv, sc) >= 0);

            AddOperatorHandler("=", (PropertyValue v, bool fv) => v.ToBool() == fv);
            AddOperatorHandler("!=", (PropertyValue v, bool fv) => v.ToBool() != fv);

            SetSearchDataCallback(GetSearchableData, StringComparison.OrdinalIgnoreCase);
        }

        IEnumerable<string> GetSearchableData(SearchItem item)
        {
            yield return item.value.ToString();
            yield return item.id;
            if (item.label != null)
                yield return item.label;
        }

        static bool StringContains(string s1, string s2, StringComparison sc)
        {
            return s1.IndexOf(s2, sc) != -1;
        }

        PropertyValue GetValue(SearchItem item, string selector)
        {
            var v = SelectorManager.SelectValue(item, m_Context.search, selector);
            return new PropertyValue(v);
        }
    }
}
