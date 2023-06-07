// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;

namespace UnityEditor.Search.Providers
{
    class ProfilerMarkersProvider : BasePerformanceProvider<ProfilerMarkersProvider.ProfilerRecorderInfo>
    {
        internal struct ProfilerRecorderCumulativeData
        {
            public long totalValue;
            public long totalCount;
            public long maxValue;
            public double average;

            public static ProfilerRecorderCumulativeData clear = new();
        }

        internal class ProfilerRecorderInfo
        {
            public ProfilerRecorder recorder;
            public ProfilerRecorderDescription description;
            public ProfilerRecorderCumulativeData data;

            public long GetSampleCount()
            {
                return data.totalCount;
            }

            public long GetMaxValue()
            {
                return data.maxValue;
            }

            public long GetTotalValue()
            {
                return data.totalValue;
            }

            public double GetAverageValue()
            {
                return data.average;
            }

            public void ResetData()
            {
                data = ProfilerRecorderCumulativeData.clear;
            }
        }

        internal const string providerId = "profilermarkers";
        const int k_SamplesCount = 5;
        const double k_NanoSecondsInSeconds = 1e9;
        const double k_KiloByte = 1024 * 1024;
        const double k_MegaByte = k_KiloByte * 1024;

        List<ProfilerRecorderInfo> m_Recorders = new();
        bool m_Enabled;

        int m_SecondUnitTypeHandle;
        int m_ByteUnitTypeHandle;
        int m_PercentUnitTypeHandle;
        int m_HertzUnitTypeHandle;

        [SearchItemProvider]
        public static SearchProvider CreateProvider()
        {
            var p = new ProfilerMarkersProvider(providerId, "Profiler Markers");
            p.Initialize();
            return p;
        }

        protected ProfilerMarkersProvider(string id, string displayName)
            : base(id, displayName)
        {
            var defaultTimeAvgLimit = GetDefaultPerformanceLimit(sampleAvgSelector);
            var defaultTimePeakLimit = GetDefaultPerformanceLimit(samplePeakSelector);

            AddPerformanceLimit(samplePeakSelector, ProfilerMarkerDataUnit.TimeNanoseconds, defaultTimePeakLimit.warningLimit, defaultTimePeakLimit.errorLimit);
            AddPerformanceLimit(sampleAvgSelector, ProfilerMarkerDataUnit.TimeNanoseconds, defaultTimeAvgLimit.warningLimit, defaultTimeAvgLimit.errorLimit);
            AddPerformanceLimit(samplePeakSelector, ProfilerMarkerDataUnit.Bytes, 100 * k_KiloByte, k_MegaByte);
            AddPerformanceLimit(sampleAvgSelector, ProfilerMarkerDataUnit.Bytes, 100 * k_KiloByte, k_MegaByte);

            m_SecondUnitTypeHandle = AddUnitType("s", UnitPowerType.One, UnitPowerType.Milli, UnitPowerType.Micro, UnitPowerType.Nano);
            m_ByteUnitTypeHandle = AddUnitType("b", UnitPowerType.One, UnitPowerType.Kilo, UnitPowerType.Mega, UnitPowerType.Giga, UnitPowerType.Tera, UnitPowerType.Peta);
            m_PercentUnitTypeHandle = AddUnitType("%", UnitPowerType.One);
            m_HertzUnitTypeHandle = AddUnitType("hz", UnitPowerType.One, UnitPowerType.Kilo, UnitPowerType.Mega, UnitPowerType.Giga, UnitPowerType.Tera, UnitPowerType.Peta);
        }

        public override void Initialize()
        {
            base.Initialize();
            onEnable = EnableProvider;
            onDisable = DisableProvider;
        }

        void EnableProvider()
        {
            var availableStatHandles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(availableStatHandles);

            foreach (var profilerRecorderHandle in availableStatHandles)
            {
                if (!profilerRecorderHandle.Valid)
                    continue;

                var recorderInfo = new ProfilerRecorderInfo()
                {
                    data = new ProfilerRecorderCumulativeData(),
                    description = ProfilerRecorderHandle.GetDescription(profilerRecorderHandle),
                    recorder = new ProfilerRecorder(profilerRecorderHandle, k_SamplesCount)
                };
                recorderInfo.recorder.Start();
                m_Recorders.Add(recorderInfo);
            }

            EditorApplication.update += OnUpdate;
            m_Enabled = true;
        }

        void DisableProvider()
        {
            m_Enabled = false;
            EditorApplication.update -= OnUpdate;
            foreach (var profilerRecorder in m_Recorders)
            {
                profilerRecorder.recorder.Stop();
                profilerRecorder.recorder.Dispose();
            }
            m_Recorders.Clear();
        }

        void OnUpdate()
        {
            if (!m_Enabled)
                return;
            foreach (var profilerRecorder in m_Recorders)
            {
                profilerRecorder.data = UpdateRecorderData(profilerRecorder.recorder, profilerRecorder.data);
            }
        }

        static ProfilerRecorderCumulativeData UpdateRecorderData(ProfilerRecorder recorder, ProfilerRecorderCumulativeData data)
        {
            var samplesCount = recorder.Capacity;
            if (samplesCount != k_SamplesCount)
                return data;

            if (recorder.Count == 0)
                return data;

            unsafe
            {
                var samples = stackalloc ProfilerRecorderSample[samplesCount];
                recorder.CopyTo(samples, samplesCount);

                var lastSampleIndex = recorder.Count - 1;
                var lastSample = samples[lastSampleIndex];
                if (lastSample is { Value: >= 0, Count: > 0 })
                {
                    data.totalValue = AddWithoutOverflow(data.totalValue, lastSample.Value);
                    data.maxValue = Math.Max(data.maxValue, lastSample.Value);
                    data.totalCount = AddWithoutOverflow(data.totalCount, lastSample.Count);
                }

                // Compute average over samples
                double totalValue = 0;
                double totalCount = 0;
                for (var i = 0; i < samplesCount; ++i)
                {
                    var sampleValue = samples[i].Value;
                    var sampleCount = samples[i].Count;
                    if (sampleValue < 0 || sampleCount <= 0)
                        continue;
                    totalValue += sampleValue;
                    totalCount += sampleCount;
                }

                if (totalCount > 0)
                {
                    data.average = totalValue / totalCount;
                }
            }

            return data;
        }

        static long AddWithoutOverflow(long a, long b)
        {
            if (b > long.MaxValue - a)
                return long.MaxValue;
            return a + b;
        }

        static ProfilerRecorderInfo GetRecorderInfo(SearchItem item)
        {
            return (ProfilerRecorderInfo)item.data;
        }

        protected override string FormatColumnValue(SearchColumnEventArgs args)
        {
            if (!m_Enabled)
                return string.Empty;

            if (args.value == null)
                return string.Empty;

            if (args.column.selector == sampleCountSelector)
                return args.value.ToString();

            double value;

            if (args.value is long l)
            {
                if (l == long.MaxValue)
                    value = double.PositiveInfinity;
                else
                    value = l;
            }
            else if (!Utils.TryGetNumber(args.value, out value))
                return string.Empty;

            var pri = GetRecorderInfo(args.item);
            if (!pri.recorder.Valid)
                return string.Empty;

            return FormatUnit(value, args.column.selector, pri.recorder.UnitType);
        }

        protected override void ResetItems(SearchItem[] items)
        {
            foreach (var item in items)
                GetRecorderInfo(item).ResetData();
        }

        static long GetSampleCount(SearchItem item)
        {
            return GetRecorderInfo(item).GetSampleCount();
        }

        static long GetMaxValue(SearchItem item)
        {
            return GetRecorderInfo(item).GetMaxValue();
        }

        static long GetTotalValue(SearchItem item)
        {
            return GetRecorderInfo(item).GetTotalValue();
        }

        static double GetAverageValue(SearchItem item)
        {
            return GetRecorderInfo(item).GetAverageValue();
        }

        [SearchSelector(sampleCountSelector, provider: providerId)] static object SelectCount(SearchSelectorArgs args) => GetSampleCount(args.current);
        [SearchSelector(samplePeakSelector, provider: providerId)] static object SelectPeak(SearchSelectorArgs args) => GetMaxValue(args.current);
        [SearchSelector(sampleAvgSelector, provider: providerId)] static object SelectAvg(SearchSelectorArgs args) => GetAverageValue(args.current);
        [SearchSelector(sampleTotalSelector, provider: providerId)] static object SelectTotal(SearchSelectorArgs args) => GetTotalValue(args.current);

        protected override string FetchDescription(SearchItem item, SearchContext context)
        {
            var fullDescription = item.options.HasAny(SearchItemOptions.FullDescription);
            var description = GetTrackerDescription(item, fullDescription ? '\n' : ' ');
            if (item.options.HasAny(SearchItemOptions.Compacted))
                return $"<b>{item.id}</b> {description}";
            return description;
        }

        string GetTrackerDescription(SearchItem item, char splitter)
        {
            if (!m_Enabled)
                return string.Empty;

            var pri = GetRecorderInfo(item);
            var sampleCount = pri.GetSampleCount();
            var peak = pri.GetMaxValue();
            var avg = pri.GetAverageValue();
            var total = pri.GetTotalValue();
            var unitType = pri.recorder.UnitType;
            return $"Sample Count: <b>{sampleCount}</b>{splitter}" +
                $"Peak: {FormatUnit(peak, samplePeakSelector, unitType)}{splitter}" +
                $"Avg: {FormatUnit(avg, sampleAvgSelector, unitType)}{splitter}" +
                $"Total: {FormatUnit(total, sampleTotalSelector, unitType)}{splitter}" +
                $"Category: {pri.description.Category}{splitter}" +
                $"Unit Type: {pri.description.UnitType}{splitter}" +
                $"Data Type: {pri.description.DataType}";
        }

        string FormatUnit(double value, string selector, ProfilerMarkerDataUnit unit)
        {
            var performanceLimitKey = GetPerformanceLimitKey(selector, unit);
            var performanceLimit = GetPerformanceLimit(performanceLimitKey);

            switch (unit)
            {
                case ProfilerMarkerDataUnit.Undefined:
                    return FormatUndefined(value, performanceLimit);
                case ProfilerMarkerDataUnit.TimeNanoseconds:
                    return FormatTime(value, performanceLimit);
                case ProfilerMarkerDataUnit.Bytes:
                    return FormatByte(value, performanceLimit);
                case ProfilerMarkerDataUnit.Count:
                    return FormatCount(value, performanceLimit);
                case ProfilerMarkerDataUnit.Percent:
                    if (selector == sampleTotalSelector)
                        return string.Empty;
                    return FormatPercent(value, performanceLimit);
                case ProfilerMarkerDataUnit.FrequencyHz:
                    if (selector == sampleTotalSelector)
                        return string.Empty;
                    return FormatFrequency(value, performanceLimit);
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
        }

        static string FormatUndefined(double value, in PerformanceLimit performanceLimit)
        {
            return GetPerformanceLimitLabel(value, performanceLimit, d => $"{d}");
        }

        static string FormatCount(double value, in PerformanceLimit performanceLimit)
        {
            return GetPerformanceLimitLabel(value, performanceLimit, d => $"{Utils.FormatCount((ulong)d)} hit(s)");
        }

        static string FormatTime(double value, in PerformanceLimit performanceLimit)
        {
            // GetTimeLabel expects time in seconds
            return GetTimeLabel(value / k_NanoSecondsInSeconds, performanceLimit);
        }

        static string FormatByte(double value, in PerformanceLimit performanceLimit)
        {
            return GetPerformanceLimitLabel(value, performanceLimit, d => $"{Utils.FormatBytes((long)d)}");
        }

        static string FormatPercent(double value, in PerformanceLimit performanceLimit)
        {
            return GetPerformanceLimitLabel(value, performanceLimit, d => $"{d}%");
        }

        static string FormatFrequency(double value, in PerformanceLimit performanceLimit)
        {
            return GetPerformanceLimitLabel(value, performanceLimit, d => $"{Utils.FormatCount((ulong)d)}Hz");
        }

        protected override IEnumerable<string> YieldPerformanceDataWords(ProfilerRecorderInfo pri)
        {
            yield return pri.description.Name;
            yield return pri.description.Category.Name;
        }

        protected override ValueWithUnit GetPerformanceAverageValue(ProfilerRecorderInfo pri) => ProfilerRecorderInfoToUnitWithValue(pri, pri.GetAverageValue());
        protected override ValueWithUnit GetPerformanceTotalValue(ProfilerRecorderInfo pri) => ProfilerRecorderInfoToUnitWithValue(pri, pri.GetTotalValue());
        protected override ValueWithUnit GetPerformancePeakValue(ProfilerRecorderInfo pri) => ProfilerRecorderInfoToUnitWithValue(pri, pri.GetMaxValue());
        protected override ValueWithUnit GetPerformanceSampleCountValue(ProfilerRecorderInfo pri) => ProfilerRecorderInfoToUnitWithValue(pri, pri.GetSampleCount());

        ValueWithUnit ProfilerRecorderInfoToUnitWithValue(ProfilerRecorderInfo pri, double value)
        {
            switch (pri.description.UnitType)
            {
                case ProfilerMarkerDataUnit.Undefined:
                    return new ValueWithUnit(value, m_UnitlessTypeHandle, UnitPowerType.One);
                case ProfilerMarkerDataUnit.TimeNanoseconds:
                    return new ValueWithUnit(value, m_SecondUnitTypeHandle, UnitPowerType.Nano);
                case ProfilerMarkerDataUnit.Bytes:
                    return new ValueWithUnit(value, m_ByteUnitTypeHandle, UnitPowerType.One);
                case ProfilerMarkerDataUnit.Count:
                    return new ValueWithUnit(value, m_UnitlessTypeHandle, UnitPowerType.One);
                case ProfilerMarkerDataUnit.Percent:
                    return new ValueWithUnit(value, m_PercentUnitTypeHandle, UnitPowerType.One);
                case ProfilerMarkerDataUnit.FrequencyHz:
                    return new ValueWithUnit(value, m_HertzUnitTypeHandle, UnitPowerType.One);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override IEnumerable<SearchItem> FetchItem(SearchContext context, SearchProvider provider)
        {
            var query = m_QueryEngine.Parse(context.searchQuery);
            if (!query.valid)
                yield break;

            foreach (var trackerName in query.Apply(m_Recorders))
                yield return CreateItem(context, provider, trackerName);
        }

        static SearchItem CreateItem(in SearchContext context, in SearchProvider provider, in ProfilerRecorderInfo pri)
        {
            var markerName = pri.description.Name;
            var item = provider.CreateItem(context, markerName, markerName, null, null, pri);
            item.options = SearchItemOptions.AlwaysRefresh;
            return item;
        }

        static int GetPerformanceLimitKey(string selector, ProfilerMarkerDataUnit unit)
        {
            return selector.GetHashCode() ^ unit.GetHashCode();
        }

        void AddPerformanceLimit(string selector, ProfilerMarkerDataUnit unit, double warningLimit, double errorLimit)
        {
            AddPerformanceLimit(GetPerformanceLimitKey(selector, unit), warningLimit, errorLimit);
        }
    }
}
