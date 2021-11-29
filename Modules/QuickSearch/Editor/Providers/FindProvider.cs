// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_FIND_PROVIDER
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    [Flags]
    enum FindOptions
    {
        None = 0,
        Words = 1 << 0,
        Regex = 1 << 1,
        Glob = 1 << 2,
        Fuzzy = 1 << 3,
        Exact = 1 << 15,
        Packages = 1 << 28,
        All = Words | Regex | Glob | Fuzzy | Packages,

        CustomStart = 1 << 17,
        CustomFinish = 1 << 23,
        CustomRange = CustomStart | 1 << 18 | 1 << 19 | 1 << 20 | 1 << 21 | 1 << 22 | CustomFinish
    }

    static class FindOptionsExtensions
    {
        public static bool HasAny(this FindOptions flags, FindOptions f) => (flags & f) != 0;
        public static bool HasAll(this FindOptions flags, FindOptions all) => (flags & all) == all;
    }

    class FindFilesQueryFactory : SearchQueryEvaluatorFactory<SearchDocument>
    {
        public FindFilesQueryFactory(SearchQueryEvaluator<SearchDocument>.EvalHandler handler)
            : base(handler)
        {}
    }

    class FindFilesQuery : SearchQueryEvaluator<SearchDocument>
    {
        public FindFilesQuery(QueryGraph graph, EvalHandler handler)
            : base(graph, handler)
        {}
    }

    static class FindProvider
    {
        public const string providerId = "find";

        private static Dictionary<FindOptions, List<string>> s_Roots = new Dictionary<FindOptions, List<string>>();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<SearchDocument, byte>> s_RootFilePaths = new ConcurrentDictionary<string, ConcurrentDictionary<SearchDocument, byte>>();
        private static readonly QueryValidationOptions k_QueryEngineOptions = new QueryValidationOptions { validateFilters = false, skipNestedQueries = true };
        private static readonly QueryEngine<SearchDocument> s_QueryEngine = new QueryEngine<SearchDocument>(k_QueryEngineOptions);

        static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider)
        {
            if (string.IsNullOrEmpty(context.searchQuery))
                yield break;

            {
                var options = FindOptions.Words | FindOptions.Regex | FindOptions.Glob;
                if (context.wantsMore)
                    options |= FindOptions.Fuzzy;
                if (context.options.HasAny(SearchFlags.Packages))
                    options |= FindOptions.Packages;

                foreach (var e in Search(context, provider, options))
                {
                    if (!e.valid)
                        yield return null;
                    else
                        yield return AssetProvider.CreateItem("Files", context, provider, null, e.source, e.score, e.flags);
                }
            }
        }

        public static IEnumerable<SearchDocument> Search(SearchContext context, SearchProvider provider, FindOptions options)
        {
            var searchQuery = context.searchQuery;
            if (string.IsNullOrEmpty(searchQuery) || searchQuery.Length < 2)
                return Enumerable.Empty<SearchDocument>();

            var tokens = searchQuery.ToLowerInvariant().Split(' ').ToArray();
            var args = tokens.Where(t => t.Length > 1 && t[0] == '+').ToArray();
            if (args.Contains("+packages"))
            {
                options |= FindOptions.Packages;
                searchQuery = searchQuery.Replace("+packages", "");
            }

            return Search(searchQuery, GetRoots(options), context, provider, options);
        }

        public static IEnumerable<SearchDocument> Search(string searchQuery, IEnumerable<string> roots, SearchContext context, SearchProvider provider, FindOptions options)
        {
            var query = s_QueryEngine.Parse(searchQuery, new FindFilesQueryFactory(args =>
            {
                {
                    if (args.op == SearchIndexOperator.None)
                        return FindFilesQuery.EvalResult.None;
                    else if (args.op == SearchIndexOperator.Equal)
                    {
                        options &= ~(FindOptions.Fuzzy | FindOptions.Glob | FindOptions.Regex);
                        options |= FindOptions.Exact;
                    }

                    IEnumerable<SearchDocument> subset = args.andSet ??
                        roots.SelectMany(root => GetRootPaths(root, options));

                    IEnumerable<SearchDocument> results = Enumerable.Empty<SearchDocument>();
                    if (args.name == null && args.value is string word && word.Length > 0)
                        results = SearchWord(args.exclude, word, options, subset);

                    if (args.orSet != null)
                        results = results.Concat(args.orSet);

                    return FindFilesQuery.EvalResult.Combined(results);
                }
            }));

            if (!query.valid)
            {
                context.AddSearchQueryErrors(query.errors.Select(e => new SearchQueryError(e, context, provider)));
                yield break;
            }

            var results = new ConcurrentBag<SearchDocument>();
            var searchTask = Task.Run(() =>
            {
                foreach (var r in query.Apply(null))
                    results.Add(r);
            });

            while (results.Count > 0 || !searchTask.Wait(1) || results.Count > 0)
            {
                while (results.TryTake(out var e))
                    yield return e;

                if (searchTask.IsFaulted || searchTask.IsCanceled)
                    yield break;
                yield return SearchDocument.invalid;
            }
        }

        private static IEnumerable<SearchDocument> GetRootPaths(string root, FindOptions options)
        {
            {
                var isPackage = options.HasAny(FindOptions.Packages) && root.StartsWith("Packages/", StringComparison.Ordinal);
                if (!options.HasAny(FindOptions.Packages) && isPackage)
                    yield break;

                if (s_RootFilePaths.TryGetValue(root, out var docs))
                {
                    foreach (var d in docs.Keys)
                        yield return d;
                }
                else
                {
                    var docsBag = new ConcurrentBag<SearchDocument>();
                    var foundFiles = new ConcurrentDictionary<SearchDocument, byte>();
                    var baseScore = isPackage ? 1 : 0;
                    var scanFileTask = Task.Run(() =>
                    {
                        var files = Directory.EnumerateFiles(root, "*.meta", SearchOption.AllDirectories);
                        foreach (var f in files)
                        {
                            var p = f.Substring(0, f.Length - 5).Replace("\\", "/");
                            var doc = new SearchDocument(p, null, null, baseScore, SearchDocumentFlags.Asset);
                            if (foundFiles.TryAdd(doc, 0))
                                docsBag.Add(doc);
                        }
                    });

                    while (docsBag.Count > 0 || !scanFileTask.Wait(1) || docsBag.Count > 0)
                    {
                        while (docsBag.TryTake(out var e))
                            yield return e;

                        if (scanFileTask.IsFaulted || scanFileTask.IsCanceled)
                            yield break;
                    }
                    s_RootFilePaths.TryAdd(root, foundFiles);
                }
            }
        }

        private static void SearchWord(bool exclude, string word, FindOptions options, IEnumerable<SearchDocument> documents, ConcurrentBag<SearchDocument> results)
        {
            Regex globRx = null, rxm = null;
            if (options.HasAny(FindOptions.Regex) && !Utils.ParseRx(word, options.HasAny(FindOptions.Exact), out rxm))
                options &= ~FindOptions.Regex;
            if (options.HasAny(FindOptions.Glob) && !Utils.ParseGlob(word, options.HasAny(FindOptions.Exact), out globRx))
                options &= ~FindOptions.Glob;
            if (exclude)
                options &= ~FindOptions.Fuzzy;

            Parallel.ForEach(documents, doc =>
            {
                try
                {
                    var match = SearchFile(doc.name, word, options, rxm, globRx, out var score) ||
                        (!string.IsNullOrEmpty(doc.m_Source) && SearchFile(doc.m_Source, word, options, rxm, globRx, out score));
                    if (!exclude && match)
                        results.Add(new SearchDocument(doc, ComputeResultScore(score, doc.name)));
                    else if (exclude && !match)
                        results.Add(new SearchDocument(doc, ComputeResultScore(score, doc.name)));
                }
                catch
                {
                    // ignore
                }
            });
        }

        static int ComputeResultScore(int score, in string name)
        {
            if (name.Length > 2)
            {
                var sp = Math.Max(0, name.LastIndexOf('/'));
                if (sp + 2 < name.Length)
                    score += name[sp] * 5 + name[sp + 1] * 2 + name[sp + 2];
            }
            return score;
        }

        private static bool SearchFile(string f, string word, FindOptions options, in Regex rxm, in Regex globRx, out int score)
        {
            score = 0;

            if (options.HasAny(FindOptions.Words))
            {
                if (options.HasAny(FindOptions.Exact))
                {
                    if (string.Equals(word, f))
                    {
                        score |= (int)FindOptions.Words;
                        return true;
                    }
                }
                else if (f.IndexOf(word, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    score |= (int)FindOptions.Words;
                    return true;
                }
            }

            if (options.HasAny(FindOptions.Regex) && IsMatch(rxm, f))
            {
                score |= (int)FindOptions.Regex;
                return true;
            }

            if (options.HasAny(FindOptions.Glob) && IsMatch(globRx, f))
            {
                score |= (int)FindOptions.Glob;
                return true;
            }

            long fuzzyScore = 0;
            if (options.HasAny(FindOptions.Fuzzy) && FuzzySearch.FuzzyMatch(word, Path.GetFileName(f), ref fuzzyScore))
            {
                score = ComputeFuzzyScore(score, fuzzyScore);
                return true;
            }

            return false;
        }

        private static bool IsMatch(in Regex rx, in string path)
        {
            try
            {
                return rx.IsMatch(path);
            }
            catch
            {
                return false;
            }
        }

        public static IEnumerable<SearchDocument> SearchWord(bool exclude, string word, FindOptions options, IEnumerable<SearchDocument> documents)
        {
            {
                var results = new ConcurrentBag<SearchDocument>();
                var searchTask = Task.Run(() => SearchWord(exclude, word, options, documents, results));

                while (results.Count > 0 || !searchTask.Wait(1) || results.Count > 0)
                {
                    while (results.TryTake(out var e))
                        yield return e;

                    if (searchTask.IsFaulted || searchTask.IsCanceled)
                        break;
                }
            }
        }

        public static void Update(string[] updated, string[] deleted, string[] moved)
        {
            {
                foreach (var kvp in s_RootFilePaths)
                {
                    foreach (var u in updated.Concat(moved))
                    {
                        if (u.StartsWith(kvp.Key, StringComparison.Ordinal))
                            kvp.Value.TryAdd(new SearchDocument(u), 0);
                    }

                    foreach (var u in deleted)
                    {
                        if (u.StartsWith(kvp.Key, StringComparison.Ordinal))
                            kvp.Value.TryRemove(new SearchDocument(u), out _);
                    }
                }
            }
        }

        static int ComputeFuzzyScore(int baseScore, long fuzzyScore)
        {
            return baseScore | (int)FindOptions.Fuzzy | (((int)FindOptions.CustomFinish - (int)fuzzyScore) & (int)FindOptions.CustomRange);
        }

        static IEnumerable<string> GetRoots(FindOptions options)
        {
            if (s_Roots.TryGetValue(options, out var roots))
                return roots;

            var projectRoots = new List<string>(Utils.GetAssetRootFolders().Where(r => FilterRoot(r, options)));
            return (s_Roots[options] = projectRoots);
        }

        public static void Reset()
        {
            s_Roots.Clear();
        }

        private static bool FilterRoot(string root, FindOptions options)
        {
            if (!options.HasAny(FindOptions.Packages))
                return !root.StartsWith("Packages/", StringComparison.Ordinal);
            return true;
        }

        private static IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            if (options.flags.HasAny(SearchPropositionFlags.QueryBuilder))
                return FetchQueryBuilderPropositions();
            return Enumerable.Empty<SearchProposition>();
        }

        private static IEnumerable<SearchProposition> FetchQueryBuilderPropositions()
        {
            yield return new SearchProposition(category: "Find", "File with Spaces", @"\s+", color: QueryColors.word);
            yield return new SearchProposition(category: "Find", "Numeric Files", @"\d+\.\w+$", color: QueryColors.word);
        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            s_QueryEngine.SetSearchDataCallback(e => null, s => s.Length < 2 ? null : s, StringComparison.Ordinal);

            return new SearchProvider(providerId, "Files")
            {
                priority = 25,
                filterId = providerId + ":",
                isExplicitProvider = true,
                isEnabledForContextualSearch = () => Utils.IsFocusedWindowTypeName("ProjectBrowser"),
                fetchItems = (context, items, provider) => FetchItems(context, SearchService.GetProvider("asset") ?? provider),
                fetchPropositions = FetchPropositions
            };
        }

        [ShortcutManagement.Shortcut("Help/Search/Find Files")]
        [MenuItem("Window/Search/Find Files", priority = 1269)]
        internal static void OpenShortcut()
        {
            QuickSearch.OpenWithContextualProvider(providerId);
        }
    }
}
