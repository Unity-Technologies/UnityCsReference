// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;

namespace UnityEditor.Search
{
    delegate object SearchSelectorHandler(SearchSelectorArgs args);
    delegate object SearchSelectorHandler1(SearchItem item);
    delegate string SearchSelectorHandler2(SearchItem item);

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    class SearchSelectorAttribute : Attribute
    {
        public SearchSelectorAttribute(string pattern, int priority = 100, string provider = null, bool printable = true)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException("Empty selector pattern", nameof(pattern));

            if (pattern[0] != '^')
                pattern = "^" + pattern;
            if (pattern[pattern.Length - 1] != '$')
                pattern += "$";
            this.pattern = new Regex(pattern,
                RegexOptions.IgnoreCase | RegexOptions.Singleline |
                RegexOptions.Compiled | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(25));
            this.priority = priority;
            this.provider = provider;
            this.printable = printable;
        }

        public Regex pattern { get; private set; }
        public int priority { get; private set; }
        public string provider { get; private set; }
        public bool printable { get; private set; }
    }

    readonly struct SelectorGroup
    {
        public readonly string name;
        public readonly string value;

        public SelectorGroup(string name, string value)
        {
            this.name = name;
            this.value = value;
        }

        public override string ToString()
        {
            return $"{name}={value}";
        }
    }

    readonly struct SelectorMatch
    {
        public readonly SearchSelector selector;
        public readonly SelectorGroup[] groups;

        public SelectorMatch(SearchSelector selector, IEnumerable<SelectorGroup> groups)
        {
            this.selector = selector;
            this.groups = groups.ToArray();
        }

        public override string ToString()
        {
            return $"{selector.pattern} | {string.Join("| ", groups.Select(g => g.ToString()))}";
        }
    }

    readonly struct SearchSelectorArgs
    {
        public readonly SelectorGroup[] groups;
        public readonly SearchItem current;

        public string name => groups.Length < 2 ? null : groups[1].value;
        public string path => groups[0].value;

        public string this[int index] => groups[index + 1].value;
        public string this[string captureName] => groups.FirstOrDefault(g => string.Equals(captureName, g.name, StringComparison.OrdinalIgnoreCase)).value;

        public SearchSelectorArgs(SelectorMatch match, SearchItem current)
        {
            groups = match.groups;
            this.current = current;
        }
    }

    readonly struct SearchSelector
    {
        public readonly Regex pattern;
        public readonly int priority;
        public readonly string provider;
        public readonly bool printable;
        public readonly SearchSelectorHandler select;

        public bool valid => pattern != null && select != null;

        public string label => Regex.Replace(pattern.ToString(), @"[\^\$\?\<\>\+\.\(\)\[\]\p{C}]+", string.Empty);

        internal SearchSelector(SearchSelectorAttribute attr, SearchSelectorHandler select)
            : this(attr.pattern, attr.priority, attr.provider, attr.printable, select)
        {
        }

        public SearchSelector(Regex pattern, int priority, string provider, bool printable, SearchSelectorHandler select)
        {
            this.pattern = pattern;
            this.priority = priority;
            this.provider = provider;
            this.printable = printable;
            this.select = select;
        }

        public override string ToString()
        {
            return $"[{priority}, {provider}] {pattern} ({printable}) | {select.Method.DeclaringType.FullName}.{select.Method.Name}";
        }
    }

    static class SelectorManager
    {
        public static List<SearchSelector> selectors { get; private set; }

        static SelectorManager()
        {
            RefreshSelectors();
        }

        public static IEnumerable<SelectorMatch> Match(string input, string provider = null)
        {
            foreach (var e in selectors)
            {
                if (e.provider != null && !string.Equals(e.provider, provider, StringComparison.Ordinal))
                    continue;
                var match = e.pattern.Match(input);
                if (!match.Success)
                    continue;

                yield return new SelectorMatch(e, match.Groups.Cast<Group>().Select(g => new SelectorGroup(g.Name, g.Value)));
            }
        }

        private static void RefreshSelectors()
        {
            Func<MethodInfo, SearchSelectorAttribute, Delegate, SearchSelector> generator = (mi, attribute, handler) =>
            {
                if (handler is SearchSelectorHandler handlerWithStruct)
                    return new SearchSelector(attribute, handlerWithStruct);
                if (handler is SearchSelectorHandler1 handler1)
                    return new SearchSelector(attribute, args => handler1(args.current));
                if (handler is SearchSelectorHandler2 handler2)
                    return new SearchSelector(attribute, args => handler2(args.current));
                throw new CustomAttributeFormatException($"Invalid selector handler {mi.DeclaringType.FullName}.{mi.Name}");
            };

            var supportedSignatures = new[]
            {
                MethodSignature.FromDelegate<SearchSelectorHandler>(),
                MethodSignature.FromDelegate<SearchSelectorHandler1>(),
                MethodSignature.FromDelegate<SearchSelectorHandler2>()
            };
            selectors = ReflectionUtils.LoadAllMethodsWithAttribute(generator, supportedSignatures)
                .Where(s => s.valid)
                .OrderBy(s => s.priority)
                .OrderBy(s => string.IsNullOrEmpty(s.provider))
                .ToList();
        }

        //[MenuItem("Window/Search/Print Selectors")]
        internal static void PrintSelectors()
        {
            foreach (var s in selectors)
                UnityEngine.Debug.Log(s);
        }

        public static object SelectValue(SearchExpressionContext context, string selectorName)
        {
            return SelectValue(context, selectorName, out var _);
        }

        public static object SelectValue(SearchExpressionContext context, string selectorName, out string suggestedSelectorName)
        {
            foreach (var e in context.items)
            {
                var selectedValue = SelectValue(e, context.search, selectorName, out suggestedSelectorName);
                if (selectedValue != null)
                    return selectedValue;
            }

            suggestedSelectorName = selectorName;
            return null;
        }

        public static object SelectValue(SearchItem item, SearchContext context, string selectorName)
        {
            return SelectValue(item, context, selectorName, out var _);
        }

        public static object SelectValue(SearchItem item, SearchContext context, string selectorName, out string suggestedSelectorName)
        {
            suggestedSelectorName = selectorName;
            if (item.TryGetValue(selectorName, context, out var field))
                return field.value;

            if (string.IsNullOrEmpty(selectorName))
                return null;

            {

                string localSuggestedSelectorName = null;
                string providerType = item.provider.type;
                var itemValue = TaskEvaluatorManager.EvaluateMainThread(() =>
                {
                    foreach (var m in Match(selectorName, providerType))
                    {
                        var selectorArgs = new SearchSelectorArgs(m, item);
                        var selectedValue = m.selector.select(selectorArgs);
                        if (selectedValue != null)
                        {
                            if (selectorArgs.name != null)
                                localSuggestedSelectorName = selectorArgs.name;
                            return selectedValue;
                        }
                    }

                    return null;
                });

                if (itemValue == null)
                    return null;

                if (!string.IsNullOrEmpty(localSuggestedSelectorName))
                    suggestedSelectorName = localSuggestedSelectorName;


                return itemValue;
            }
        }

        public static IEnumerable<SearchItem> SelectValues(SearchContext context, IEnumerable<SearchItem> items, string selector, string setFieldName)
        {
            return EvaluatorUtils.ProcessValues(items, setFieldName, item => SelectValue(item, context, selector)?.ToString());
        }
    }
}
