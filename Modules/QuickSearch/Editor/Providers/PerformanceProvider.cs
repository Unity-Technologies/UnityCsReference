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

        int m_SecondUnitTypeHandle;

        [SearchItemProvider]
        public static SearchProvider CreateProvider()
        {
            var p = new PerformanceProvider(providerId, "Performance Trackers");
            p.Initialize();
            return p;
        }

        protected PerformanceProvider(string id, string displayName)
            : base(id, displayName)
        {
            m_SecondUnitTypeHandle = AddUnitType("s", UnitPowerType.One, UnitPowerType.Milli, UnitPowerType.Micro, UnitPowerType.Nano);
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

        [SearchSelector(sampleCountSelector, provider: providerId)] static object SelectCount(SearchSelectorArgs args) => EditorPerformanceTracker.GetSampleCount(args.current.id);
        [SearchSelector(samplePeakSelector, provider: providerId)] static object SelectPeak(SearchSelectorArgs args) => EditorPerformanceTracker.GetPeakTime(args.current.id);
        [SearchSelector(sampleAvgSelector, provider: providerId)] static object SelectAvg(SearchSelectorArgs args) => EditorPerformanceTracker.GetAverageTime(args.current.id);
        [SearchSelector(sampleTotalSelector, provider: providerId)] static object SelectTotal(SearchSelectorArgs args) => EditorPerformanceTracker.GetTotalTime(args.current.id);

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

            if (args.column.selector == sampleCountSelector)
                return args.value.ToString();

            if (Utils.TryGetNumber(args.value, out var d))
                return GetTimeLabel(d, GetDefaultPerformanceLimit(args.column.selector));
            return string.Empty;
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

        ValueWithUnit TrackerToValueWithUnit(double value)
        {
            return new ValueWithUnit(value, m_SecondUnitTypeHandle, UnitPowerType.One);
        }

        ValueWithUnit TrackerToValueWithUnit(int value)
        {
            return new ValueWithUnit(value, m_UnitlessTypeHandle, UnitPowerType.One);
        }

        protected override IEnumerable<SearchItem> FetchItem(SearchContext context, SearchProvider provider)
        {
            var query = m_QueryEngine.Parse(context.searchQuery);
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
