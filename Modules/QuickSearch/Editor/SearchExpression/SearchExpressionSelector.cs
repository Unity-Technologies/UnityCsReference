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
    delegate object SearchExpressionSelectorHandler(SearchExpressionSelectorArgs args);
    delegate object SearchExpressionSelectorHandler1(SearchItem item);
    delegate string SearchExpressionSelectorHandler2(SearchItem item);

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    class SearchExpressionSelectorAttribute : Attribute
    {
        public SearchExpressionSelectorAttribute(string pattern, int priority = 100, string provider = null)
        {
            this.pattern = new Regex(pattern,
                RegexOptions.IgnoreCase | RegexOptions.Singleline |
                RegexOptions.Compiled | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(25));
            this.priority = priority;
            this.provider = provider;
        }

        public Regex pattern { get; private set; }
        public int priority { get; private set; }
        public string provider { get; private set; }
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
        public readonly SearchExpressionSelector selector;
        public readonly SelectorGroup[] groups;

        public SelectorMatch(SearchExpressionSelector selector, IEnumerable<SelectorGroup> groups)
        {
            this.selector = selector;
            this.groups = groups.ToArray();
        }

        public override string ToString()
        {
            return $"{selector.pattern} | {string.Join("| ", groups.Select(g => g.ToString()))}";
        }
    }

    readonly struct SearchExpressionSelectorArgs
    {
        public readonly SelectorGroup[] groups;
        public readonly SearchItem current;

        public string name => groups.Length < 2 ? null : groups[1].value;
        public string path => groups[0].value;

        public string this[int index] => groups[index + 1].value;
        public string this[string captureName] => groups.FirstOrDefault(g => string.Equals(captureName, g.name, StringComparison.OrdinalIgnoreCase)).value;

        public SearchExpressionSelectorArgs(SelectorMatch match, SearchItem current)
        {
            groups = match.groups;
            this.current = current;
        }
    }

    readonly struct SearchExpressionSelector
    {
        public readonly Regex pattern;
        public readonly int priority;
        public readonly string provider;
        public readonly SearchExpressionSelectorHandler select;

        public bool valid => pattern != null && select != null;

        public SearchExpressionSelector(Regex pattern, int priority, string provider, SearchExpressionSelectorHandler select)
        {
            this.pattern = pattern;
            this.priority = priority;
            this.provider = provider;
            this.select = select;
        }

        public override string ToString()
        {
            return $"{pattern} | {select.Method.DeclaringType.FullName}.{select.Method.Name}";
        }
    }

    static class SelectorManager
    {
        public static List<SearchExpressionSelector> selectors { get; private set; }

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
            Func<MethodInfo, SearchExpressionSelectorAttribute, Delegate, SearchExpressionSelector> generator = (mi, attribute, handler) =>
            {
                if (handler is SearchExpressionSelectorHandler handlerWithStruct)
                    return new SearchExpressionSelector(attribute.pattern, attribute.priority, attribute.provider, handlerWithStruct);
                if (handler is SearchExpressionSelectorHandler1 handler1)
                    return new SearchExpressionSelector(attribute.pattern, attribute.priority, attribute.provider, args => handler1(args.current));
                if (handler is SearchExpressionSelectorHandler2 handler2)
                    return new SearchExpressionSelector(attribute.pattern, attribute.priority, attribute.provider, args => handler2(args.current));
                throw new CustomAttributeFormatException($"Invalid selector handler {mi.DeclaringType.FullName}.{mi.Name}");
            };

            var supportedSignatures = new[]
            {
                MethodSignature.FromDelegate<SearchExpressionSelectorHandler>(),
                MethodSignature.FromDelegate<SearchExpressionSelectorHandler1>(),
                MethodSignature.FromDelegate<SearchExpressionSelectorHandler2>()
            };
            selectors = ReflectionUtils.LoadAllMethodsWithAttribute(generator, supportedSignatures)
                .Where(s => s.valid)
                .OrderBy(s => s.priority)
                .ToList();
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
            var itemValue = item.GetValue(selectorName, context);
            if (itemValue != null)
                return itemValue;

            if (selectorName == null)
                return null;

            string providerType = item.provider.type;
            string localSuggestedSelectorName = null;
            itemValue = TaskEvaluatorManager.EvaluateMainThread(() =>
            {
                foreach (var m in Match(selectorName, providerType))
                {
                    var selectorArgs = new SearchExpressionSelectorArgs(m, item);
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

            if (localSuggestedSelectorName != null)
                suggestedSelectorName = localSuggestedSelectorName;
            return itemValue;
        }

        public static IEnumerable<SearchItem> SelectValues(SearchContext context, IEnumerable<SearchItem> items, string selector, string setFieldName)
        {
            return EvaluatorUtils.ProcessValues(items, setFieldName, item => SelectValue(item, context, selector)?.ToString());
        }
    }
}
