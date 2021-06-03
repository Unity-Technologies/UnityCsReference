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
        CustomRange = CustomStart | 1 << 18 | 1 << 19 | 1 << 20 | 1 << 21 | 1 << 22 | CustomFinish,

        AllFiles = 1 << 31
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
        const int k_MaxRegexTimeout = 25;

        private static Dictionary<FindOptions, List<string>> s_Roots = new Dictionary<FindOptions, List<string>>();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<SearchDocument, byte>> s_RootFilePaths = new ConcurrentDictionary<string, ConcurrentDictionary<SearchDocument, byte>>();
        private static readonly QueryValidationOptions k_QueryEngineOptions = new QueryValidationOptions { validateFilters = false, skipNestedQueries = true };
        private static readonly QueryEngine<SearchDocument> s_QueryEngine = new QueryEngine<SearchDocument>(k_QueryEngineOptions);

        static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider)
        {
            var options = FindOptions.Words | FindOptions.Regex | FindOptions.Glob;
            if (context.wantsMore)
                options |= FindOptions.Packages | FindOptions.Fuzzy;

            foreach (var e in Search(context, provider, options))
                yield return AssetProvider.CreateItem("Files", context, provider, null, e.source, e.score, e.flags);
        }

        public static IEnumerable<SearchDocument> Search(SearchContext context, SearchProvider provider, FindOptions options)
        {
            var searchQuery = context.searchQuery;
            if (string.IsNullOrEmpty(searchQuery) || searchQuery.Length < 2)
                return Enumerable.Empty<SearchDocument>();

            var tokens = searchQuery.ToLowerInvariant().Split(' ').ToArray();
            var args = tokens.Where(t => t.Length > 1 && t[0] == '+').ToArray();
            if (args.Contains("+all"))
            {
                options |= FindOptions.AllFiles;
                searchQuery = searchQuery.Replace("+all", "");
            }

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
                    results = SearchWord(args.exclude, word, options, subset.ToList());

                if (args.orSet != null)
                    results = results.Concat(args.orSet);

                return FindFilesQuery.EvalResult.Combined(results);
            }));

            if (!query.valid)
            {
                context.AddSearchQueryErrors(query.errors.Select(e => new SearchQueryError(e, context, provider)));
                return Enumerable.Empty<SearchDocument>();
            }
            return query.Apply(null);
        }

        private static IEnumerable<SearchDocument> GetRootPaths(string root, FindOptions options)
        {
            var isPackage = options.HasAny(FindOptions.Packages) && root.StartsWith("Packages/", StringComparison.Ordinal);
            if (!options.HasAny(FindOptions.Packages) && isPackage)
                return Enumerable.Empty<SearchDocument>();

            if (!s_RootFilePaths.TryGetValue(root, out var files))
            {
                files = new ConcurrentDictionary<SearchDocument, byte>(Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                    .Where(p => !p.EndsWith(".meta", StringComparison.Ordinal))
                    .Select(p => p.Replace("\\", "/")).ToDictionary(p => new SearchDocument(p, null, null, 0, SearchDocumentFlags.Asset), p => (byte)0));
                s_RootFilePaths.TryAdd(root, files);
            }

            return files.Keys;
        }

        private static void SearchWord(bool exclude, string word, FindOptions options, IEnumerable<SearchDocument> documents, ConcurrentBag<SearchDocument> results)
        {
            Regex globRx = null, rxm = null;
            if (options.HasAny(FindOptions.Regex) && !ParseRx(word, options.HasAny(FindOptions.Exact), out rxm))
                options &= ~FindOptions.Regex;
            if (options.HasAny(FindOptions.Glob) && !ParseGlob(word, options.HasAny(FindOptions.Exact), out globRx))
                options &= ~FindOptions.Glob;
            if (exclude)
                options &= ~FindOptions.Fuzzy;

            Parallel.ForEach(documents, doc =>
            {
                try
                {
                    var match = SearchFile(doc.name, word, options, rxm, globRx, out var score);
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
            var results = new ConcurrentBag<SearchDocument>();
            var searchTask = Task.Run(() => SearchWord(exclude, word, options, documents, results));

            while (results.Count > 0 || !searchTask.Wait(1) || results.Count > 0)
            {
                while (results.TryTake(out var e))
                    yield return e;
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

        static bool ParseRx(string pattern, bool exact, out Regex rx)
        {
            try
            {
                rx = new Regex(!exact ? pattern : $"^{pattern}$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(k_MaxRegexTimeout));
            }
            catch (ArgumentException)
            {
                rx = null;
                return false;
            }

            return true;
        }

        static bool ParseGlob(string pattern, bool exact, out Regex rx)
        {
            try
            {
                pattern = Regex.Escape(RemoveDuplicateAdjacentCharacters(pattern, '*')).Replace(@"\*", ".*").Replace(@"\?", ".");
                rx = new Regex(!exact ? pattern : $"^{pattern}$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(k_MaxRegexTimeout));
            }
            catch (ArgumentException)
            {
                rx = null;
                return false;
            }

            return true;
        }

        static string RemoveDuplicateAdjacentCharacters(string pattern, char c)
        {
            for (int i = pattern.Length - 1; i >= 0; --i)
            {
                if (pattern[i] != c || i == 0)
                    continue;

                if (pattern[i - 1] == c)
                    pattern = pattern.Remove(i, 1);
            }

            return pattern;
        }

        static IEnumerable<string> GetRoots(FindOptions options)
        {
            if (s_Roots.TryGetValue(options, out var roots))
                return roots;

            var projectRoots = new List<string>(Utils.GetAssetRootFolders().Where(r => FilterRoot(r, options)));
            if (options.HasAny(FindOptions.AllFiles))
            {
                var baseProjectRoot = new DirectoryInfo(Path.Combine(Application.dataPath, "..")).FullName.Replace("\\", "/");
                projectRoots.AddRange(Directory.EnumerateDirectories(baseProjectRoot, "*", SearchOption.TopDirectoryOnly)
                    .Select(d => d.Replace(baseProjectRoot, "").Substring(1))
                    .Where(d => d.Length > 0 && char.IsLetterOrDigit(d[0])));
            }

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
                fetchItems = (context, items, provider) => FetchItems(context, SearchService.GetProvider("asset") ?? provider)
            };
        }

        [ShortcutManagement.Shortcut("Help/Search/Find Files")]
        internal static void OpenShortcut()
        {
            QuickSearch.OpenWithContextualProvider(providerId);
        }
    }
}
