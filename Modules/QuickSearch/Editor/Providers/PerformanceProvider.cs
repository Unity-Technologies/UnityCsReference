// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Profiling;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEditorInternal;
using UnityEngine.Profiling;
using System.Linq;

namespace UnityEditor.Search.Providers
{
    static class PerformanceProvider
    {
        const string type = "performance";

        static class Styles
        {
            public static readonly bool isDarkTheme = EditorGUIUtility.isProSkin;
            public static readonly Color normalColor = isDarkTheme ? new Color(196 / 255f, 196 / 255f, 196 / 255f) : new Color(32 / 255f, 32 / 255f, 32 / 255f);
            public static readonly Color warningColor = isDarkTheme ? new Color(255 / 255f, 204 / 255f, 0 / 255f) : new Color(240 / 255f, 105 / 255f, 53 / 255f);
            public static readonly Color criticalColor = new Color(204 / 255f, 51 / 255f, 0 / 255f);
        }

        [MenuItem("Window/Search/Performance Trackers", priority = 1272)]
        public static void OpenProvider()
        {
            SearchService.ShowContextual(type);
        }

        [SearchItemProvider]
        public static SearchProvider CreateProvider()
        {
            var qe = BuildQueryEngine();
            return new SearchProvider(type, "Performance")
            {
                active = false,
                isExplicitProvider = true,
                fetchColumns = FetchColumns,
                fetchDescription = FetchDescription,
                fetchItems = (context, items, provider) => FetchItem(qe, context, provider),
                tableConfig = GetDefaultTableConfig,
                actions = new List<SearchAction>(new[]
                {
                    new SearchAction("open", "Profile...", item => StartProfilerRecording(item.id, true, deepProfile: false), _ => !ProfilerDriver.deepProfiling && !ProfilerDriver.enabled),
                    new SearchAction("select", "Stop profiling", (SearchItem item) => StopProfilerRecordingAndOpenProfiler(), _ => ProfilerDriver.enabled),
                    new SearchAction("enable_deep", "Enable Deep Profiling...", _ => SetProfilerDeepProfile(true), _ => !ProfilerDriver.deepProfiling),
                    new SearchAction("start_deep", "Deep Profile...", item => StartProfilerRecording(item.id, true, deepProfile: true), _ => ProfilerDriver.deepProfiling && !ProfilerDriver.enabled),
                    new SearchAction("log", "Callstack", item => EditorPerformanceTracker.GetCallstack(item.id, cs => CaptureCallstack(item, cs))),
                    new SearchAction("reset", "Reset", item => EditorPerformanceTracker.Reset(item.id)),
                })
            };
        }

        private static SearchTable GetDefaultTableConfig(SearchContext context)
        {
            return new SearchTable(type, new [] { new SearchColumn("Name", "label") }.Concat(FetchColumns(context, null)));
        }

        [SearchColumnProvider(nameof(PerformanceMetric))]
        public static void PerformanceMetric(SearchColumn column)
        {
            column.getter = args =>
            {
                switch (column.selector)
                {
                    case "count": return EditorPerformanceTracker.GetSampleCount(args.item.id);
                    case "peak": return EditorPerformanceTracker.GetPeakTime(args.item.id);
                    case "avg": return EditorPerformanceTracker.GetAverageTime(args.item.id);
                    case "total": return EditorPerformanceTracker.GetTotalTime(args.item.id);
                    case "age": return EditorPerformanceTracker.GetTimestamp(args.item.id);
                }

                return null;
            };

            if (column.selector != "count")
            {
                column.drawer = args =>
                {
                    if (Utils.TryGetNumber(args.value, out var d))
                        GUI.Label(args.rect, GetTimeLabel(d, 0.5d, 2.0d), ItemSelectors.GetItemContentStyle(column));
                    return args.value;
                };
            }
        }
        
        [SearchSelector("count", provider: type, cacheable = false)] static object SelectCount(SearchSelectorArgs args) => EditorPerformanceTracker.GetSampleCount(args.current.id);
        [SearchSelector("peak", provider: type, cacheable = false)] static object SelectPeak(SearchSelectorArgs args) => EditorPerformanceTracker.GetPeakTime(args.current.id);
        [SearchSelector("avg", provider: type, cacheable = false)] static object SelectAvg(SearchSelectorArgs args) => EditorPerformanceTracker.GetAverageTime(args.current.id);
        [SearchSelector("total", provider: type, cacheable = false)] static object SelectTotal(SearchSelectorArgs args) => EditorPerformanceTracker.GetTotalTime(args.current.id);

        static IEnumerable<SearchColumn> FetchColumns(SearchContext context, IEnumerable<SearchItem> items)
        {
            yield return new SearchColumn("Performance/Sample Count", "count", nameof(PerformanceMetric));
            yield return new SearchColumn("Performance/Peak Time", "peak", nameof(PerformanceMetric));
            yield return new SearchColumn("Performance/Average Time", "avg", nameof(PerformanceMetric));
            yield return new SearchColumn("Performance/Total Time", "total", nameof(PerformanceMetric));
        }

        static void CaptureCallstack(in SearchItem item, string callstack)
        {
            Debug.Log(callstack);
            item.data = callstack;
        }

        static string ColorToHexCode(Color color)
        {
            var r = (int)(color.r * 255);
            var g = (int)(color.g * 255);
            var b = (int)(color.b * 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        static Color GetLabelTimeColor(double time, double warningLimit, double errorLimit)
        {
            if (time >= errorLimit)
                return Styles.criticalColor;

            if (time >= warningLimit)
                return Styles.warningColor;

            return Styles.normalColor;
        }

        static string FetchDescription(SearchItem item, SearchContext context)
        {
            var fullDescription = item.options.HasAny(SearchItemOptions.FullDescription);
            var description = GetTrackerDescription(item.id, fullDescription ? '\n' : ' ');
            if (fullDescription && item.data != null)
                return $"{description}\n\n{FormatCallstackForConsole((string)item.data)}";
            if (item.options.HasAny(SearchItemOptions.Compacted))
                return $"<b>{item.id}</b> {description}";
            return description;
        }

        private static string FormatCallstackForConsole(string callstack)
        {
            return Regex.Replace(callstack, "\\[(\\S+?):(\\d+)\\]", "[<a href=\"$1\" line=\"$2\">$1:$2</a>]");
        }

        private static string GetTrackerDescription(string trackerName, char splitter)
        {
            var sampleCount = EditorPerformanceTracker.GetSampleCount(trackerName);
            var peakTime = EditorPerformanceTracker.GetPeakTime(trackerName);
            var avgTime = EditorPerformanceTracker.GetAverageTime(trackerName);
            var totalTime = EditorPerformanceTracker.GetTotalTime(trackerName);
            return $"Sample Count: <b>{sampleCount}</b>{splitter}" +
                $"Peak: {GetTimeLabel(peakTime, 0.5, 1.0)}{splitter}" +
                $"Avg: {GetTimeLabel(avgTime, 0.1, 0.5)}{splitter}" +
                $"Total: {GetTimeLabel(totalTime, 5, 10)}";
        }

        static string GetTimeLabel(double time, double warningLimit, double errorLimit)
        {
            return $"<color={ColorToHexCode(GetLabelTimeColor(time, warningLimit, errorLimit))}>{ToEngineeringNotation(time)}s</color>";
        }

        static string ToEngineeringNotation(double d, bool printSign = false)
        {
            var sign = !printSign || d < 0 ? "" : "+";
            if (Math.Abs(d) >= 1)
                return $"{sign}{d.ToString("###.0", System.Globalization.CultureInfo.InvariantCulture)}";

            if (Math.Abs(d) > 0)
            {
                double exponent = Math.Log10(Math.Abs(d));
                switch ((int)Math.Floor(exponent))
                {
                    case -1: case -2: case -3: return $"{sign}{(d * 1e3):###.0} m";
                    case -4: case -5: case -6: return $"{sign}{(d * 1e6):###.0} µ";
                    case -7: case -8: case -9: return $"{sign}{(d * 1e9):###.0} n";
                    case -10: case -11: case -12: return $"{sign}{(d * 1e12):###.0} p";
                    case -13: case -14: case -15: return $"{sign}{(d * 1e15):###.0} f";
                    case -16: case -17: case -18: return $"{sign}{(d * 1e15):###.0} a";
                    case -19: case -20: case -21: return $"{sign}{(d * 1e15):###.0} z";
                    default: return $"{sign}{(d * 1e15):###.0} y";
                }
            }

            return "0";
        }

        static QueryEngine<string> BuildQueryEngine()
        {
            var queryEngineOptions = new QueryValidationOptions { validateFilters = false, skipNestedQueries = true };
            var qe = new QueryEngine<string>(queryEngineOptions);

            qe.AddFilter("age", trackerName => EditorPerformanceTracker.GetTimestamp(trackerName));
            qe.AddFilter("avg", trackerName => EditorPerformanceTracker.GetAverageTime(trackerName));
            qe.AddFilter("total", trackerName => EditorPerformanceTracker.GetTotalTime(trackerName));
            qe.AddFilter("peak", trackerName => EditorPerformanceTracker.GetPeakTime(trackerName));
            qe.AddFilter("count", trackerName => EditorPerformanceTracker.GetSampleCount(trackerName));

            qe.SetSearchDataCallback(YieldTrackerWords, StringComparison.OrdinalIgnoreCase);
            return qe;
        }

        static IEnumerable<string> YieldTrackerWords(string trackerName)
        {
            yield return trackerName;
        }

        static IEnumerable<SearchItem> FetchItem(QueryEngine<string> qe, SearchContext context, SearchProvider provider)
        {
            var query = qe.ParseQuery(context.searchQuery);
            if (!query.valid)
                yield break;

            var trackers = EditorPerformanceTracker.GetAvailableTrackers();
            foreach (var trackerName in query.Apply(trackers))
                yield return CreateItem(context, provider, trackerName);
        }

        static SearchItem CreateItem(in SearchContext context, in SearchProvider provider, in string trackerName)
        {
            var item = provider.CreateItem(context, trackerName, trackerName, null, null, null);
            item.options = SearchItemOptions.AlwaysRefresh;
            return item;
        }

        static bool StartProfilerRecording(string markerFilter, bool editorProfile, bool deepProfile)
        {
            if (ProfilerDriver.deepProfiling != deepProfile)
            {
                if (deepProfile)
                    Debug.LogWarning("Enabling deep profiling. Domain reload will occur. Please restart Profiling.");
                else
                    Debug.LogWarning("Disabling deep profiling. Domain reload will occur. Please restart Profiling.");

                SetProfilerDeepProfile(deepProfile);
                return false;
            }

            var editorProfileStr = editorProfile ? "editor" : "playmode";
            var deepProfileStr = deepProfile ? " - deep profile" : "";
            var hasMarkerFilter = !string.IsNullOrEmpty(markerFilter);
            var markerStr = hasMarkerFilter ? $"- MarkerFilter: {markerFilter}" : "";
            Debug.Log($"Start profiler recording: {editorProfileStr} {deepProfileStr} {markerStr}...");

            EnableProfiler(false);

            EditorApplication.delayCall += () =>
            {
                ProfilerDriver.ClearAllFrames();
                ProfilerDriver.profileEditor = editorProfile;
                ProfilerDriver.deepProfiling = deepProfile;
                if (hasMarkerFilter)
                    SetMarkerFiltering(markerFilter);

                EditorApplication.delayCall += () => EnableProfiler(true);
            };

            return true;
        }

        static void StopProfilerRecording(Action toProfilerStopped = null)
        {
            SetMarkerFiltering("");
            EnableProfiler(false);
            Debug.Log($"Stop profiler recording.");

            if (toProfilerStopped != null)
                EditorApplication.delayCall += () => toProfilerStopped();
        }

        static void StopProfilerRecordingAndOpenProfiler()
        {
            StopProfilerRecording(() => OpenProfilerWindow());
        }

        static void EnableProfiler(in bool enable)
        {
            ProfilerDriver.enabled = enable;
            SessionState.SetBool("ProfilerEnabled", enable);
        }

        static EditorWindow OpenProfilerWindow()
        {
            var profilerWindow = EditorWindow.CreateWindow<ProfilerWindow>();
            var cpuProfilerModule = profilerWindow.GetProfilerModule<UnityEditorInternal.Profiling.CPUOrGPUProfilerModule>(ProfilerArea.CPU);
            cpuProfilerModule.ViewType = ProfilerViewType.Hierarchy;
            profilerWindow.Show();
            return profilerWindow;
        }

        static void SetProfilerDeepProfile(in bool deepProfile)
        {
            ProfilerWindow.SetEditorDeepProfiling(deepProfile);
        }

        static void SetMarkerFiltering(in string markerName)
        {
            ProfilerDriver.SetMarkerFiltering(markerName);
        }
    }
}
