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

    readonly struct FindResult : IEquatable<FindResult>, IComparable<FindResult>
    {
        public FindResult(string path, int score)
        {
            this.path = path;
            this.score = score;
        }

        public readonly string path;
        public readonly int score;

        public bool valid => path != null && score != int.MaxValue;

        public static FindResult none = new FindResult(null, int.MaxValue);

        public bool Equals(FindResult other)
        {
            return string.Equals(path, other.path, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return path.GetHashCode();
            }
        }

        public override bool Equals(object other)
        {
            return other is FindResult l && Equals(l);
        }

        public int CompareTo(FindResult other)
        {
            var c = other.score.CompareTo(other.score);
            if (c == 0)
                return string.CompareOrdinal(path, other.path);
            return c;
        }
    }

    class FindFilesQueryFactory : SearchQueryEvaluatorFactory<FindResult>
    {
        public FindFilesQueryFactory(SearchQueryEvaluator<FindResult>.EvalHandler handler)
            : base(handler)
        {}
    }

    class FindFilesQuery : SearchQueryEvaluator<FindResult>
    {
        public FindFilesQuery(QueryGraph graph, EvalHandler handler)
            : base(graph, handler)
        {}
    }

    static class FindProvider
    {
        public const string providerId = "find";

        private static List<string> s_Roots;
        private static readonly List<string> s_ProjectRoots = new List<string>() { "Assets" };
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> s_RootFilePaths = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>();

        private static readonly string[] s_SubsetRoots = new string[] { "<subset>" };
        private static readonly QueryEngine<FindResult> s_QueryEngine = new QueryEngine<FindResult>(validateFilters: false);

        static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider)
        {
            var options = FindOptions.Words | FindOptions.Regex | FindOptions.Glob;
            if (context.wantsMore)
                options |= FindOptions.Packages | FindOptions.Fuzzy;

            foreach (var e in Search(context, options))
            {
                yield return provider.CreateItem(context, e.path, e.score,
                    null,
                    null, null, null);
            }
        }

        public static IEnumerable<FindResult> Search(SearchContext context, FindOptions options)
        {
            var searchQuery = context.searchQuery;
            if (string.IsNullOrEmpty(searchQuery) || searchQuery.Length < 2)
                return Enumerable.Empty<FindResult>();

            var tokens = searchQuery.ToLowerInvariant().Split(' ').ToArray();
            var args = tokens.Where(t => t.Length > 1 && t[0] == '+').ToArray();
            var words = tokens.Where(t => t.Length > 1).ToArray();
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

            return Search(searchQuery.Trim(), words, GetRoots(options), options);
        }

        public static IEnumerable<FindResult> Search(string searchQuery, string[] words, IEnumerable<string> roots, FindOptions options)
        {
            {
                var query = s_QueryEngine.Parse(searchQuery, new FindFilesQueryFactory(args =>
                {
                    if (args.op == SearchIndexOperator.None)
                        return FindFilesQuery.EvalResult.None;

                    ConcurrentDictionary<string, byte> subset = null;
                    if (args.andSet != null)
                        subset = new ConcurrentDictionary<string, byte>(args.andSet.Distinct().Select(e => new KeyValuePair<string, byte>(e.path, 0)));

                    if (args.op == SearchIndexOperator.Equal)
                    {
                        options &= ~FindOptions.Fuzzy;
                        options |= FindOptions.Exact;
                    }

                    IEnumerable<FindResult> results = new List<FindResult>();
                    if (args.name == null && args.value is string word && word.Length > 0)
                    {
                        results = SearchWord(args.exclude, word, subset != null ? s_SubsetRoots : roots, options, subset);
                    }

                    if (args.orSet != null)
                        results = results.Concat(args.orSet);

                    return FindFilesQuery.EvalResult.Combined(results);
                }));

                if (!query.valid)
                    return Enumerable.Empty<FindResult>();
                return query.Apply(null).OrderBy(e => e.score).Distinct();
            }
        }

        private static void SearchWord(bool exclude, string word, IEnumerable<string> roots, FindOptions options, ConcurrentDictionary<string, byte> subset, ConcurrentBag<FindResult> results)
        {
            Regex globRx = null, rxm = null;
            if (options.HasFlag(FindOptions.Regex) && !ParseRx(word, options.HasFlag(FindOptions.Exact), out rxm))
                options &= ~FindOptions.Regex;
            if (options.HasFlag(FindOptions.Glob) && !ParseGlob(word, options.HasFlag(FindOptions.Exact), out globRx))
                options &= ~FindOptions.Glob;
            if (exclude)
                options &= ~FindOptions.Fuzzy;

            Parallel.ForEach(roots, r =>
            {
                var isPackage = options.HasFlag(FindOptions.Packages) && r.StartsWith("Packages/", StringComparison.Ordinal);
                if (!options.HasFlag(FindOptions.Packages) && isPackage)
                    return;

                ConcurrentDictionary<string, byte> files = subset;
                if (files == null && !s_RootFilePaths.TryGetValue(r, out files))
                {
                    files = new ConcurrentDictionary<string, byte>(Directory.EnumerateFiles(r, "*", SearchOption.AllDirectories)
                        .Where(p => !p.EndsWith(".meta", StringComparison.Ordinal))
                        .Select(p => p.Replace("\\", "/")).ToDictionary(p => p, p => (byte)0));
                    s_RootFilePaths.TryAdd(r, files);
                }

                Parallel.ForEach(files, kvp =>
                {
                    try
                    {
                        var result = SearchFile(kvp.Key, word, isPackage, options, rxm, globRx);
                        if (!exclude && result.valid)
                            results.Add(result);
                        else if (exclude && !result.valid)
                            results.Add(new FindResult(kvp.Key, (int)FindOptions.AllFiles));
                    }
                    catch
                    {
                        // ignore
                    }
                });
            });
        }

        private static FindResult SearchFile(string f, string word, bool isPackage, FindOptions options, Regex rxm, Regex globRx)
        {
            int score = isPackage ? (int)FindOptions.Packages : 0;

            if (options.HasFlag(FindOptions.Words))
            {
                if (options.HasFlag(FindOptions.Exact))
                {
                    if (string.Equals(word, f))
                        return new FindResult(f, score | (int)FindOptions.Words);
                }
                else if (f.IndexOf(word, StringComparison.OrdinalIgnoreCase) != -1)
                    return new FindResult(f, score | (int)FindOptions.Words);
            }

            if (options.HasFlag(FindOptions.Regex) && rxm.IsMatch(f))
                return new FindResult(f, score | (int)FindOptions.Regex);

            if (options.HasFlag(FindOptions.Glob) && globRx.IsMatch(f))
                return new FindResult(f, score | (int)FindOptions.Glob);

            long fuzzyScore = 0;
            if (options.HasFlag(FindOptions.Fuzzy) && FuzzySearch.FuzzyMatch(word, f, ref fuzzyScore))
                return new FindResult(f, ComputeFuzzyScore(score, fuzzyScore));

            return FindResult.none;
        }

        private static IEnumerable<FindResult> SearchWord(bool exclude, string word, IEnumerable<string> roots, FindOptions options, ConcurrentDictionary<string, byte> files)
        {
            var results = new ConcurrentBag<FindResult>();
            var searchTask = Task.Run(() => SearchWord(exclude, word, roots, options, files, results));

            while (results.Count > 0 || !searchTask.Wait(1) || results.Count > 0)
            {
                if (results.TryTake(out var e))
                    yield return e;
            }
        }

        public static void Update(string[] updated, string[] deleted, string[] moved)
        {
            {
                foreach (var kvp in s_RootFilePaths)
                {
                    foreach (var u in updated)
                    {
                        if (u.StartsWith(kvp.Key, StringComparison.Ordinal))
                            kvp.Value.TryAdd(u, 0);
                    }

                    foreach (var u in deleted.Concat(moved))
                    {
                        if (u.StartsWith(kvp.Key, StringComparison.Ordinal))
                            kvp.Value.TryRemove(u, out var _);
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
                rx = new Regex(!exact ? pattern : $"^{pattern}$", RegexOptions.IgnoreCase);
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
                pattern = Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".");
                rx = new Regex(!exact ? pattern : $"^{pattern}$", RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                rx = null;
                return false;
            }

            return true;
        }

        static IEnumerable<string> GetRoots(FindOptions options)
        {
            var projectRoots = s_ProjectRoots;
            if (options.HasFlag(FindOptions.AllFiles))
            {
                var baseProjectRoot = new DirectoryInfo(Path.Combine(Application.dataPath, "..")).FullName.Replace("\\", "/");
                projectRoots = Directory.EnumerateDirectories(baseProjectRoot, "*", SearchOption.TopDirectoryOnly)
                    .Select(d => d.Replace(baseProjectRoot, "").Substring(1))
                    .Where(d => d.Length > 0 && char.IsLetterOrDigit(d[0]))
                    .ToList();
            }

            if (!options.HasFlag(FindOptions.Packages))
                return projectRoots;

            if (s_Roots != null)
                return s_Roots;

            var listRequest = UnityEditor.PackageManager.Client.List(offlineMode: true);
            while (!listRequest.IsCompleted)
                ;
            return (s_Roots = projectRoots.Concat(listRequest.Result.Select(r => r.assetPath)).ToList());
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
