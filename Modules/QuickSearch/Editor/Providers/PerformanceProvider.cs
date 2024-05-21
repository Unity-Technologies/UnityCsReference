// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Profiling;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;

namespace UnityEditor.Search.Providers
{
    class PerformanceProvider : BasePerformanceProvider<string>
    {
        internal const string providerId = "performance";

        static readonly UnitTypeHandle k_SecondUnitTypeHandle = UnitType.GetHandle("s");

        [SearchItemProvider]
        public static SearchProvider CreateProvider()
        {
            var p = new PerformanceProvider(providerId, "Performance Trackers");
            p.Initialize();
            p.filterId = "perf:";
            return p;
        }

        protected PerformanceProvider(string id, string displayName)
            : base(id, displayName)
        {
            AddUnitType("s", k_SecondUnitTypeHandle, UnitPowerType.One, UnitPowerType.Milli, UnitPowerType.Micro, UnitPowerType.Nano);
        }

        protected override IEnumerable<SearchAction> GetActions()
        {
            return base.GetActions().Append(new SearchAction("log", "Callstack", item => EditorPerformanceTracker.GetCallstack(item.id, cs => CaptureCallstack(item, cs))));
        }

        protected override void ResetItems(SearchItem[] items)
        {
            foreach (var item in items)
                EditorPerformanceTracker.Reset(item.id);
        }

        [SearchSelector(sampleCountSelector, provider: providerId, cacheable = false)] static object SelectCount(SearchSelectorArgs args) => TrackerToValueWithUnit(EditorPerformanceTracker.GetSampleCount(args.current.id));
        [SearchSelector(samplePeakSelector, provider: providerId, cacheable = false)] static object SelectPeak(SearchSelectorArgs args) => TrackerToValueWithUnit(EditorPerformanceTracker.GetPeakTime(args.current.id));
        [SearchSelector(sampleAvgSelector, provider: providerId, cacheable = false)] static object SelectAvg(SearchSelectorArgs args) => TrackerToValueWithUnit(EditorPerformanceTracker.GetAverageTime(args.current.id));
        [SearchSelector(sampleTotalSelector, provider: providerId, cacheable = false)] static object SelectTotal(SearchSelectorArgs args) => TrackerToValueWithUnit(EditorPerformanceTracker.GetTotalTime(args.current.id));
        [SearchSelector(sampleLastTimeSelector, provider: providerId, cacheable = false)] static object SelectLastTime(SearchSelectorArgs args) => TrackerToValueWithUnit(EditorPerformanceTracker.GetLastTime(args.current.id));

        static void CaptureCallstack(in SearchItem item, string callstack)
        {
            Debug.Log(callstack);
            item.data = callstack;
        }

        protected override string FetchDescription(SearchItem item, SearchContext context)
        {
            var fullDescription = item.options.HasAny(SearchItemOptions.FullDescription);
            var description = GetTrackerDescription(item.id, fullDescription ? '\n' : ' ');
            if (fullDescription && item.data != null)
                return $"{description}\n\n{FormatCallstackForConsole((string)item.data)}";
            if (item.options.HasAny(SearchItemOptions.Compacted))
                return $"<b>{item.id}</b> {description}";
            return description;
        }

        protected override string FormatColumnValue(SearchColumnEventArgs args)
        {
            if (args.value == null)
                return string.Empty;
            if (args.value is string valueStr)
                return valueStr;

            var valueWithUnit = (ValueWithUnit)args.value;
            if (args.column.selector == sampleCountSelector)
                return valueWithUnit.value.ToString("F0");

            return GetTimeLabel(valueWithUnit.value, GetDefaultPerformanceLimit(args.column.selector));
        }

        static string FormatCallstackForConsole(string callstack)
        {
            return Regex.Replace(callstack, "\\[(\\S+?):(\\d+)\\]", "[<a href=\"$1\" line=\"$2\">$1:$2</a>]");
        }

        string GetTrackerDescription(string trackerName, char splitter)
        {
            var sampleCount = EditorPerformanceTracker.GetSampleCount(trackerName);
            var peakTime = EditorPerformanceTracker.GetPeakTime(trackerName);
            var avgTime = EditorPerformanceTracker.GetAverageTime(trackerName);
            var totalTime = EditorPerformanceTracker.GetTotalTime(trackerName);
            return $"Sample Count: <b>{sampleCount}</b>{splitter}" +
                $"Peak: {GetTimeLabel(peakTime, GetDefaultPerformanceLimit(samplePeakSelector))}{splitter}" +
                $"Avg: {GetTimeLabel(avgTime, GetDefaultPerformanceLimit(sampleAvgSelector))}{splitter}" +
                $"Total: {GetTimeLabel(totalTime, GetDefaultPerformanceLimit(sampleTotalSelector))}";
        }

        protected override IEnumerable<string> YieldPerformanceDataWords(string trackerName)
        {
            yield return trackerName;
        }

        protected override ValueWithUnit GetPerformanceAverageValue(string trackerName) => TrackerToValueWithUnit(EditorPerformanceTracker.GetAverageTime(trackerName));
        protected override ValueWithUnit GetPerformanceTotalValue(string trackerName) => TrackerToValueWithUnit(EditorPerformanceTracker.GetTotalTime(trackerName));
        protected override ValueWithUnit GetPerformancePeakValue(string trackerName) => TrackerToValueWithUnit(EditorPerformanceTracker.GetPeakTime(trackerName));
        protected override ValueWithUnit GetPerformanceSampleCountValue(string trackerName) => TrackerToValueWithUnit(EditorPerformanceTracker.GetSampleCount(trackerName));
        protected override ValueWithUnit GetPerformanceLastTimeValue(string trackerName) => TrackerToValueWithUnit(EditorPerformanceTracker.GetLastTime(trackerName));

        static ValueWithUnit TrackerToValueWithUnit(double value)
        {
            return new ValueWithUnit(value, k_SecondUnitTypeHandle, UnitPowerType.One);
        }

        static ValueWithUnit TrackerToValueWithUnit(int value)
        {
            return new ValueWithUnit(value, k_UnitlessTypeHandle, UnitPowerType.One);
        }

        protected override IEnumerable<SearchItem> FetchItem(SearchContext context, SearchProvider provider)
        {
            var query = m_QueryEngine.ParseQuery(context.searchQuery);
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
    }
}
