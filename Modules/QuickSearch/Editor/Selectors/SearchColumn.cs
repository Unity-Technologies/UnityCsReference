// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.Search
{
    [Flags]
    enum SearchColumnFlags
    {
        None = 0,
        Hidden = 1 << 0,
        Sorted = 1 << 1,
        Volatile = 1 << 2,
        IgnoreSettings = 1 << 3,

        SortedDescending = 1 << 10,

        TextAlignmentLeft = 1 << 15,
        TextAlignmentCenter = 1 << 16,
        TextAlignmentRight = 1 << 17,

        CanHide = 1 << 20,
        CanSort = 1 << 21,

        Default = CanHide | CanSort | TextAlignmentLeft,
        TextAligmentMask = TextAlignmentLeft | TextAlignmentCenter | TextAlignmentRight
    }

    static class SearchColumnFlagsExtensions
    {
        public static bool HasAny(this SearchColumnFlags flags, SearchColumnFlags f) => (flags & f) != 0;
        public static bool HasAll(this SearchColumnFlags flags, SearchColumnFlags all) => (flags & all) == all;
    }

    static class SearchColumnSettings
    {
        public static void Load(SearchColumn column)
        {
            column.width = EditorPrefs.GetFloat(GetKey(column.selector, nameof(column.width)), column.width);
            column.options = (SearchColumnFlags)EditorPrefs.GetInt(GetKey(column.selector, nameof(column.options)), (int)column.options);
            column.provider = EditorPrefs.GetString(GetKey(column.selector, nameof(column.provider)), column.provider);
        }

        public static void Save(SearchColumn column)
        {
            EditorPrefs.SetFloat(GetKey(column.selector, nameof(column.width)), column.width);
            EditorPrefs.SetInt(GetKey(column.selector, nameof(column.options)), (int)column.options);
            EditorPrefs.SetString(GetKey(column.selector, nameof(column.provider)), column.provider);
        }

        public static void Clear(IEnumerable<string> selectors)
        {
            foreach (var s in selectors)
                EditorPrefs.DeleteKey(GetKey(s, "provider"));
        }

        static string GetKey(string selector, string type) => $"Search.Column.{type}.{selector}";
    }

    [Serializable]
    class SearchColumn : IEquatable<SearchColumn>
    {
        public delegate object GetterEntry(SearchColumnEventArgs args);
        public delegate void SetterEntry(SearchColumnEventArgs args);
        public delegate object DrawEntry(SearchColumnEventArgs args);
        public delegate int CompareEntry(SearchColumnCompareArgs args);

        public string path;
        public string provider;
        public string selector;

        public float width = 50;
        public SearchColumnFlags options = SearchColumnFlags.Default;
        public GUIContent content;

        public GetterEntry getter { get; set; }
        public SetterEntry setter { get; set; }
        public DrawEntry drawer { get; set; }
        public CompareEntry comparer { get; set; }

        public string name => ParseName(path ?? string.Empty);

        public SearchColumn(string path, GUIContent content = null, SearchColumnFlags options = SearchColumnFlags.Default)
            : this(path, path, content, options)
        {
        }

        public SearchColumn(string path, string selector, GUIContent content = null, SearchColumnFlags options = SearchColumnFlags.Default)
            : this(path, selector, string.Empty, content, options)
        {
        }

        public SearchColumn(string path, string selector, string provider, GUIContent content = null, SearchColumnFlags options = SearchColumnFlags.Default)
        {
            this.path = path;
            this.selector = selector;
            this.provider = provider;
            this.options = options;
            this.content = content ?? new GUIContent(name);
            width = 145f;

            if ((options & SearchColumnFlags.IgnoreSettings) == 0)
                SearchColumnSettings.Load(this);
            InitFunctors();
        }

        public SearchColumn(SearchColumn src)
            : this(src.path, src.selector, src.provider, src.content ?? new GUIContent(), src.options)
        {
            width = src.width;
            getter = src.getter;
            setter = src.setter;
            drawer = src.drawer;
            comparer = src.comparer;
        }

        public override string ToString()
        {
            return $"{path}, {provider}/{selector} [{content.text}] ({options})";
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return path.GetHashCode() ^ selector.GetHashCode() ^ provider.GetHashCode();
            }
        }

        public override bool Equals(object other)
        {
            return other is SearchColumn l && Equals(l);
        }

        public bool Equals(SearchColumn other)
        {
            return string.Equals(path, other.path, StringComparison.Ordinal) &&
                string.Equals(selector, other.selector, StringComparison.Ordinal) &&
                string.Equals(provider, other.provider, StringComparison.Ordinal);
        }

        public static string ParseName(string path)
        {
            var pos = path.LastIndexOf('/');
            var name = pos == -1 ? path : path.Substring(pos + 1);
            return name;
        }

        internal void InitFunctors()
        {
            getter = DefaultSelect;
            setter = null;
            drawer = null;
            comparer = null;
            if (!string.IsNullOrEmpty(provider))
                SearchColumnProvider.Initialize(this);
        }

        private object DefaultSelect(SearchColumnEventArgs args)
        {
            return args.column.SelectValue(args.item, args.context);
        }

        internal object ResolveValue(SearchItem item, SearchContext context)
        {
            if (getter == null)
                return null;
            return getter(new SearchColumnEventArgs(item, item.context ?? context, this));
        }

        internal object SelectValue(SearchItem item, SearchContext context)
        {
            return SelectorManager.SelectValue(item, context, selector);
        }

        public static List<SearchColumn> Enumerate(SearchContext context, IEnumerable<SearchItem> items)
        {
            var columns = new List<SearchColumn>(ItemSelectors.Enumerate(items));

            var providerTypes = new HashSet<string>(context.providers.Select(p => p.type));
            foreach (var s in SelectorManager.selectors)
            {
                if (!s.printable)
                    continue;

                if (!string.IsNullOrEmpty(s.provider) && !providerTypes.Contains(s.provider))
                    continue;

                columns.Add(new SearchColumn($"Selectors/{s.label}", s.label));
            }

            foreach (var p in context.providers)
            {
                if (p.fetchColumns == null)
                    continue;

                columns.AddRange(p.fetchColumns(context, items.Take(50)));
            }

            return columns;
        }

        public void SetProvider(string provider)
        {
            options &= ~SearchColumnFlags.Volatile;
            this.provider = provider;
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchTableChangeColumnFormat, name, provider, selector);
            InitFunctors();
        }
    }

    struct SearchColumnEventArgs
    {
        public SearchColumnEventArgs(SearchItem item, SearchContext context, SearchColumn column)
        {
            this.item = item;
            this.context = context;
            this.column = column;
            value = null;
            multiple = false;
            rect = Rect.zero;
            focused = false;
            selected = false;
        }

        // All
        public readonly SearchItem item;
        public readonly SearchContext context;
        public readonly SearchColumn column;

        // Set
        public object value;
        public bool multiple;

        // Draw
        public Rect rect;
        public bool focused;
        public bool selected;
    }

    struct SearchColumnCompareArgs
    {
        public SearchColumnCompareArgs(SearchColumnEventArgs lhs, SearchColumnEventArgs rhs, bool sortAscending)
        {
            this.lhs = lhs;
            this.rhs = rhs;
            this.sortAscending = sortAscending;
        }

        public readonly SearchColumnEventArgs lhs;
        public readonly SearchColumnEventArgs rhs;
        public bool sortAscending;
    }

    delegate void SearchColumnProviderHandler(SearchColumn column);

    readonly struct SearchColumnProvider
    {
        public readonly string provider;
        public readonly SearchColumnProviderHandler handler;

        public SearchColumnProvider(string provider, SearchColumnProviderHandler handler)
        {
            this.provider = provider;
            this.handler = handler;
        }

        public static List<SearchColumnProvider> providers { get; private set; }
        static SearchColumnProvider()
        {
            Func<MethodInfo, SearchColumnProviderAttribute, Delegate, SearchColumnProvider> generator = (mi, attribute, handler) =>
            {
                if (handler is SearchColumnProviderHandler handlerWithStruct)
                    return new SearchColumnProvider(attribute.provider, handlerWithStruct);
                throw new CustomAttributeFormatException($"Invalid search column provider handler {mi.DeclaringType.FullName}.{mi.Name}");
            };

            var supportedSignatures = new[] { MethodSignature.FromDelegate<SearchColumnProviderHandler>() };
            providers = ReflectionUtils.LoadAllMethodsWithAttribute(generator, supportedSignatures).ToList();
        }

        public static void Initialize(SearchColumn column)
        {
            foreach (var p in providers)
            {
                if (!string.Equals(p.provider, column.provider, StringComparison.OrdinalIgnoreCase))
                    continue;

                p.handler(column);
                break;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    class SearchColumnProviderAttribute : Attribute
    {
        public SearchColumnProviderAttribute(string provider)
        {
            this.provider = provider;
        }

        public string provider { get; private set; }
    }
}
