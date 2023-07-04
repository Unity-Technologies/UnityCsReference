// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace UnityEditor.Search.Providers
{
    abstract class BasePerformanceProvider : SearchProvider
    {
        internal readonly struct PerformanceLimit
        {
            public readonly int key;
            public readonly double warningLimit;
            public readonly double errorLimit;

            public static PerformanceLimit invalid = new(-1, -1, -1);

            public PerformanceLimit(int key, double warningLimit, double errorLimit)
            {
                this.key = key;
                this.warningLimit = warningLimit;
                this.errorLimit = errorLimit;
            }
        }

        internal readonly struct UnitTypeHandle : IEquatable<UnitTypeHandle>, IComparable<UnitTypeHandle>
        {
            public readonly int value;

            public UnitTypeHandle(int value)
            {
                this.value = value;
            }

            public bool Equals(UnitTypeHandle other)
            {
                return value == other.value;
            }

            public override bool Equals(object obj)
            {
                return obj is UnitTypeHandle other && Equals(other);
            }

            public override int GetHashCode()
            {
                return value;
            }

            public int CompareTo(UnitTypeHandle other)
            {
                return value.CompareTo(other.value);
            }
        }

        internal readonly struct ValueWithUnit : IComparable<ValueWithUnit>, IComparable
        {
            public readonly double value;
            public readonly UnitTypeHandle unityTypeHandle;
            public readonly UnitPowerType powerType;

            public ValueWithUnit(double value, UnitTypeHandle unityTypeHandle, UnitPowerType unitPowerType)
            {
                this.value = value;
                this.unityTypeHandle = unityTypeHandle;
                this.powerType = unitPowerType;
            }

            public int CompareTo(ValueWithUnit other)
            {
                var compare = unityTypeHandle.CompareTo(other.unityTypeHandle);
                if (compare != 0)
                    return compare;
                var convertedValue = ConvertToPower(value, powerType);
                var otherConvertedValue = ConvertToPower(other.value, other.powerType);
                var tolerance = ConvertToPower(0.0001, GetSmallestPower(powerType, other.powerType));
                if (Math.Abs(convertedValue - otherConvertedValue) < tolerance)
                    return 0;
                if (convertedValue < otherConvertedValue)
                    return -1;
                return 1;
            }

            public int CompareTo(object obj)
            {
                if (obj is ValueWithUnit vwu)
                    return CompareTo(vwu);
                return -1;
            }
        }

        // Keep them ordered from smallest to largest
        internal enum UnitPowerType
        {
            Nano,
            Micro,
            Milli,
            Centi,
            Deci,
            One,
            Deca,
            Hecto,
            Kilo,
            Mega,
            Giga,
            Tera,
            Peta,
        }

        internal readonly struct UnitPower
        {
            public readonly string[] symbols;
            public readonly UnitPowerType type;

            public UnitPower(UnitPowerType powerType, params string[] symbols)
            {
                this.type = powerType;
                this.symbols = symbols;
            }
        }

        internal readonly struct UnitType
        {
            public readonly string suffix;
            public readonly UnitPowerType[] supportedPowers;

            public readonly UnitTypeHandle handle;

            public bool unitless => string.IsNullOrEmpty(suffix);

            public static UnitType unsupported = new("unsupported");

            public UnitType(string suffix, params UnitPowerType[] supportedPowers)
                : this(suffix, GetHandle(suffix), supportedPowers)
            {}

            public UnitType(string suffix, UnitTypeHandle unitTypeHandle, params UnitPowerType[] supportedPowers)
            {
                this.suffix = suffix ?? string.Empty;
                this.supportedPowers = supportedPowers;
                this.handle = unitTypeHandle;
            }

            public static UnitTypeHandle GetHandle(string typeSuffix)
            {
                return new UnitTypeHandle(string.IsNullOrEmpty(typeSuffix) ? 0 : typeSuffix.GetHashCode());
            }
        }

        static class Styles
        {
            public static readonly bool isDarkTheme = EditorGUIUtility.isProSkin;
            public static readonly Color normalColor = isDarkTheme ? new Color(196 / 255f, 196 / 255f, 196 / 255f) : new Color(32 / 255f, 32 / 255f, 32 / 255f);
            public static readonly Color warningColor = isDarkTheme ? new Color(255 / 255f, 204 / 255f, 0 / 255f) : new Color(240 / 255f, 105 / 255f, 53 / 255f);
            public static readonly Color criticalColor = new Color(204 / 255f, 51 / 255f, 0 / 255f);
        }

        public const string sampleCountSelector = "count";
        public const string samplePeakSelector = "peak";
        public const string sampleAvgSelector = "avg";
        public const string sampleTotalSelector = "total";

        protected Dictionary<int, PerformanceLimit> m_PerformanceLimits = new();

        protected static readonly UnitTypeHandle k_UnitlessTypeHandle = UnitType.GetHandle(null);
        protected Dictionary<UnitTypeHandle, UnitType> m_UnitTypes;
        protected Dictionary<UnitPowerType, UnitPower> m_UnitPowers;

        protected List<UnitType> m_SortedUnitTypes = null;

        protected BasePerformanceProvider(string id, string displayName)
            : base(id, displayName)
        {
            AddDefaultPerformanceLimit(samplePeakSelector, 0.5, 2);
            AddDefaultPerformanceLimit(sampleAvgSelector, 0.5, 2);

            m_UnitTypes = new Dictionary<UnitTypeHandle, UnitType>();
            m_UnitPowers = new Dictionary<UnitPowerType, UnitPower>();

            AddUnitPower(UnitPowerType.Nano, "n");
            AddUnitPower(UnitPowerType.Micro, "u", "µ");
            AddUnitPower(UnitPowerType.Milli, "m");
            AddUnitPower(UnitPowerType.Centi, "c");
            AddUnitPower(UnitPowerType.Deci, "d");
            AddUnitPower(UnitPowerType.One);
            AddUnitPower(UnitPowerType.Deca, "da", "d");
            AddUnitPower(UnitPowerType.Hecto, "h");
            AddUnitPower(UnitPowerType.Kilo, "k");
            AddUnitPower(UnitPowerType.Mega, "m");
            AddUnitPower(UnitPowerType.Giga, "g");
            AddUnitPower(UnitPowerType.Tera, "t");
            AddUnitPower(UnitPowerType.Peta, "p");

            // Add unitless types
            AddUnitType(null, k_UnitlessTypeHandle, UnitPowerType.One, UnitPowerType.Kilo, UnitPowerType.Mega, UnitPowerType.Giga, UnitPowerType.Tera, UnitPowerType.Peta);
        }

        public abstract void Initialize();

        protected virtual IEnumerable<SearchAction> GetActions()
        {
            return new List<SearchAction>(new[]
            {
                new SearchAction("open", "Profile...", item => StartProfilerRecording(item.id, true, deepProfile: false), _ => !ProfilerDriver.deepProfiling && !ProfilerDriver.enabled),
                new SearchAction("select", "Stop profiling", (SearchItem item) => StopProfilerRecordingAndOpenProfiler(), _ => ProfilerDriver.enabled),
                new SearchAction("enable_deep", "Enable Deep Profiling...", _ => SetProfilerDeepProfile(true), _ => !ProfilerDriver.deepProfiling),
                new SearchAction("start_deep", "Deep Profile...", item => StartProfilerRecording(item.id, true, deepProfile: true), _ => ProfilerDriver.deepProfiling && !ProfilerDriver.enabled),
                new SearchAction("disable_deep", "Disable Deep Profiling...", _ => SetProfilerDeepProfile(false), _ => ProfilerDriver.deepProfiling),
                new SearchAction("reset", "Reset", new GUIContent("Reset"), ResetItems),
            });
        }

        protected abstract void ResetItems(SearchItem[] items);

        protected abstract string FetchDescription(SearchItem item, SearchContext context);

        protected abstract IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options);

        protected abstract IEnumerable<SearchItem> FetchItem(SearchContext context, SearchProvider provider);

        protected virtual SearchTable GetDefaultTableConfig(SearchContext context)
        {
            return new SearchTable(id, new[] { new SearchColumn("Name", "label") }.Concat(FetchColumns(context, null)));
        }

        protected virtual IEnumerable<SearchColumn> FetchColumns(SearchContext context, IEnumerable<SearchItem> items)
        {
            yield return new SearchColumn("Performance/Sample Count", sampleCountSelector, nameof(PerformanceMetric));
            yield return new SearchColumn("Performance/Peak", samplePeakSelector, nameof(PerformanceMetric));
            yield return new SearchColumn("Performance/Average", sampleAvgSelector, nameof(PerformanceMetric));
            yield return new SearchColumn("Performance/Total", sampleTotalSelector, nameof(PerformanceMetric));
        }

        protected virtual string FormatColumnValue(SearchColumnEventArgs args)
        {
            return DefaultFormatColumnValue(args);
        }

        [SearchColumnProvider(nameof(PerformanceMetric))]
        public static void PerformanceMetric(SearchColumn column)
        {
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

        public static string DefaultFormatColumnValue(in SearchColumnEventArgs args)
        {
            if (args.value == null)
                return string.Empty;

            if (args.column.selector == sampleCountSelector)
                return args.value.ToString();

            if (Utils.TryGetNumber(args.value, out var d))
                return GetTimeLabel(d, 0.5d, 2.0d);
            return string.Empty;

        }

        protected static string ColorToHexCode(in Color color)
        {
            var r = (int)(color.r * 255);
            var g = (int)(color.g * 255);
            var b = (int)(color.b * 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        protected static Color GetPerformanceLimitColor(double time, double warningLimit, double errorLimit)
        {
            if (double.IsPositiveInfinity(time))
                return Styles.criticalColor;

            if (time >= errorLimit)
                return Styles.criticalColor;

            if (time >= warningLimit)
                return Styles.warningColor;

            return Styles.normalColor;
        }

        protected static string GetPerformanceLimitLabel(double value, double warningLimit, double errorLimit, Func<double, string> valueConverter)
        {
            var isInfinity = double.IsPositiveInfinity(value);
            var infinityPrefix = isInfinity ? ">" : "";
            if (isInfinity)
                value = long.MaxValue;
            if (warningLimit < 0 || errorLimit < 0)
                return $"{infinityPrefix}{valueConverter(value)}";
            return $"<color={ColorToHexCode(GetPerformanceLimitColor(value, warningLimit, errorLimit))}>{infinityPrefix}{valueConverter(value)}</color>";
        }

        protected static string GetPerformanceLimitLabel(double value, in PerformanceLimit performanceLimit, Func<double, string> valueConverter)
        {
            return GetPerformanceLimitLabel(value, performanceLimit.warningLimit, performanceLimit.errorLimit, valueConverter);
        }

        protected static string GetTimeLabel(double time, double warningLimit, double errorLimit)
        {
            return GetPerformanceLimitLabel(time, warningLimit, errorLimit, v => $"{ToEngineeringNotation(v)}s");
        }

        protected static string GetTimeLabel(double time, in PerformanceLimit performanceLimit)
        {
            return GetPerformanceLimitLabel(time, performanceLimit, v => $"{ToEngineeringNotation(v)}s");
        }

        protected static string ToEngineeringNotation(double d, bool printSign = false)
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

        protected static bool StartProfilerRecording(string markerFilter, bool editorProfile, bool deepProfile)
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

        protected static void StopProfilerRecording(Action toProfilerStopped = null)
        {
            SetMarkerFiltering("");
            EnableProfiler(false);
            Debug.Log($"Stop profiler recording.");

            if (toProfilerStopped != null)
                EditorApplication.delayCall += () => toProfilerStopped();
        }

        protected static void StopProfilerRecordingAndOpenProfiler()
        {
            StopProfilerRecording(() => OpenProfilerWindow());
        }

        protected static void EnableProfiler(in bool enable)
        {
            ProfilerDriver.enabled = enable;
            SessionState.SetBool("ProfilerEnabled", enable);
        }

        protected static EditorWindow OpenProfilerWindow()
        {
            var profilerWindow = EditorWindow.CreateWindow<ProfilerWindow>();
            var cpuProfilerModule = profilerWindow.GetProfilerModule<UnityEditorInternal.Profiling.CPUOrGPUProfilerModule>(ProfilerArea.CPU);
            cpuProfilerModule.ViewType = ProfilerViewType.Hierarchy;
            profilerWindow.Show();
            return profilerWindow;
        }

        protected static void SetProfilerDeepProfile(in bool deepProfile)
        {
            ProfilerWindow.SetEditorDeepProfiling(deepProfile);
        }

        protected static void SetMarkerFiltering(in string markerName)
        {
            ProfilerDriver.SetMarkerFiltering(markerName);
        }

        protected static int GetDefaultPerformanceLimitKey(in string selector)
        {
            return selector.GetHashCode();
        }

        protected void AddPerformanceLimit(in PerformanceLimit performanceLimit)
        {
            m_PerformanceLimits.TryAdd(performanceLimit.key, performanceLimit);
        }

        protected void AddPerformanceLimit(int key, double warningLimit, double errorLimit)
        {
            var performanceLimit = new PerformanceLimit(key, warningLimit, errorLimit);
            AddPerformanceLimit(performanceLimit);
        }

        protected void AddDefaultPerformanceLimit(string selector, double warningLimit, double errorLimit)
        {
            AddPerformanceLimit(GetDefaultPerformanceLimitKey(selector), warningLimit, errorLimit);
        }

        protected PerformanceLimit GetPerformanceLimit(int performanceLimitKey)
        {
            if (m_PerformanceLimits.TryGetValue(performanceLimitKey, out var limit))
                return limit;
            return PerformanceLimit.invalid;
        }

        protected PerformanceLimit GetDefaultPerformanceLimit(string selector)
        {
            return GetPerformanceLimit(GetDefaultPerformanceLimitKey(selector));
        }

        protected void AddUnitPower(UnitPowerType powerType, params string[] symbols)
        {
            var sortedSymbols = symbols.Length == 0 ? symbols : symbols.OrderByDescending(s => s.Length).ToArray();
            var unitPower = new UnitPower(powerType, sortedSymbols);
            m_UnitPowers.TryAdd(powerType, unitPower);
        }

        protected UnitTypeHandle AddUnitType(string suffix, UnitTypeHandle unitTypeHandle, params UnitPowerType[] supportedPowers)
        {
            var sortedPowerTypes = supportedPowers.OrderByDescending(p =>
            {
                var symbols = m_UnitPowers[p].symbols;
                if (symbols == null || symbols.Length == 0) return 0;
                return symbols[0].Length; // Already sorter in UnitPower.
            });
            var unitType = new UnitType(suffix?.ToLowerInvariant(), unitTypeHandle, sortedPowerTypes.ToArray());
            m_UnitTypes.TryAdd(unitType.handle, unitType);
            m_SortedUnitTypes = null;
            return unitType.handle;
        }

        protected UnitTypeHandle AddUnitType(string suffix, params UnitPowerType[] supportedPowers)
        {
            var lower = suffix?.ToLowerInvariant();
            return AddUnitType(lower, UnitType.GetHandle(lower), supportedPowers);
        }

        protected UnitTypeHandle AddUnitType(params UnitPowerType[] supportedPowers)
        {
            return AddUnitType(null, supportedPowers);
        }

        protected bool IsSupportedUnit(UnitTypeHandle unitTypeHandle)
        {
            return m_UnitTypes.ContainsKey(unitTypeHandle);
        }

        protected static bool AreUnitSameType(ValueWithUnit valueA, ValueWithUnit valueB)
        {
            return valueA.unityTypeHandle.Equals(valueB.unityTypeHandle);
        }

        protected UnitType GetUnitType(UnitTypeHandle unitTypeHandle)
        {
            return m_UnitTypes[unitTypeHandle];
        }

        protected UnitType GetUnitType(ValueWithUnit value)
        {
            return GetUnitType(value.unityTypeHandle);
        }

        protected static UnitPowerType GetSmallestPower(UnitPowerType powerTypeA, UnitPowerType powerTypeB)
        {
            if ((int)powerTypeA <= (int)powerTypeB)
                return powerTypeA;
            return powerTypeB;
        }

        protected void BuildSortedUnityTypesCache()
        {
            m_SortedUnitTypes = m_UnitTypes.Values.OrderByDescending(converter => converter.suffix.Length).ToList();
        }

        protected static double ConvertToPower(double baseValue, UnitPowerType power)
        {
            switch (power)
            {
                case UnitPowerType.Nano:
                    return baseValue / 1e9;
                case UnitPowerType.Micro:
                    return baseValue / 1e6;
                case UnitPowerType.Milli:
                    return baseValue / 1e3;
                case UnitPowerType.Centi:
                    return baseValue / 100;
                case UnitPowerType.Deci:
                    return baseValue / 10;
                case UnitPowerType.One:
                    return baseValue;
                case UnitPowerType.Deca:
                    return baseValue * 10;
                case UnitPowerType.Hecto:
                    return baseValue * 100;
                case UnitPowerType.Kilo:
                    return baseValue * 1e3;
                case UnitPowerType.Mega:
                    return baseValue * 1e6;
                case UnitPowerType.Giga:
                    return baseValue * 1e9;
                case UnitPowerType.Tera:
                    return baseValue * 1e12;
                case UnitPowerType.Peta:
                    return baseValue * 1e15;
                default:
                    throw new ArgumentOutOfRangeException(nameof(power), power, null);
            }
        }
    }

    abstract class BasePerformanceProvider<TPerformanceData> : BasePerformanceProvider
    {

        protected QueryEngine<TPerformanceData> m_QueryEngine;

        protected BasePerformanceProvider(string id, string displayName)
            : base(id, displayName)
        {}

        public override void Initialize()
        {
            m_QueryEngine = BuildQueryEngine();
            active = false;
            isExplicitProvider = true;
            fetchColumns = FetchColumns;
            fetchDescription = FetchDescription;
            fetchItems = (context, _, provider) => FetchItem(context, provider);
            tableConfig = GetDefaultTableConfig;
            actions = GetActions().ToList();
            fetchPropositions = FetchPropositions;
        }

        protected override IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            return Enumerable.Empty<SearchProposition>();
        }

        protected virtual QueryEngine<TPerformanceData> BuildQueryEngine()
        {
            var queryEngineOptions = new QueryValidationOptions { validateFilters = false, skipNestedQueries = true, skipUnknownFilters = true };
            var qe = new QueryEngine<TPerformanceData>(queryEngineOptions);

            var supportedOperators = new[] { "=", "!=", ">", ">=", "<", "<=" };
            qe.SetFilter(sampleAvgSelector, GetPerformanceAverageValue, supportedOperators);
            qe.SetFilter(sampleTotalSelector, GetPerformanceTotalValue, supportedOperators);
            qe.SetFilter(samplePeakSelector, GetPerformancePeakValue, supportedOperators);
            qe.SetFilter(sampleCountSelector, GetPerformanceSampleCountValue, supportedOperators);

            SetupFilter(qe, sampleAvgSelector, CompareValuesWithUnit);
            SetupFilter(qe, sampleTotalSelector, CompareValuesWithUnit);
            SetupFilter(qe, samplePeakSelector, CompareValuesWithUnit);
            SetupFilter(qe, sampleCountSelector, CompareValuesWithUnit);
            qe.SetSearchDataCallback(YieldPerformanceDataWords, StringComparison.OrdinalIgnoreCase);
            return qe;
        }

        protected void SetupFilter(QueryEngine<TPerformanceData> qe, string filterToken, Func<ValueWithUnit, ValueWithUnit, FilterOperatorType, bool> handler)
        {
            var filter = qe.GetFilter(filterToken);

            filter.AddTypeParser(ParseFilterValue);
            filter.AddOperator("=").AddHandler<ValueWithUnit, ValueWithUnit>((value, filterValue) => handler(value, filterValue, FilterOperatorType.Equal));
            filter.AddOperator("!=").AddHandler<ValueWithUnit, ValueWithUnit>((value, filterValue) => handler(value, filterValue, FilterOperatorType.NotEqual));
            filter.AddOperator(">").AddHandler<ValueWithUnit, ValueWithUnit>((value, filterValue) => handler(value, filterValue, FilterOperatorType.Greater));
            filter.AddOperator(">=").AddHandler<ValueWithUnit, ValueWithUnit>((value, filterValue) => handler(value, filterValue, FilterOperatorType.GreaterOrEqual));
            filter.AddOperator("<").AddHandler<ValueWithUnit, ValueWithUnit>((value, filterValue) => handler(value, filterValue, FilterOperatorType.Lesser));
            filter.AddOperator("<=").AddHandler<ValueWithUnit, ValueWithUnit>((value, filterValue) => handler(value, filterValue, FilterOperatorType.LesserOrEqual));
        }

        protected abstract IEnumerable<string> YieldPerformanceDataWords(TPerformanceData data);
        protected abstract ValueWithUnit GetPerformanceAverageValue(TPerformanceData data);
        protected abstract ValueWithUnit GetPerformanceTotalValue(TPerformanceData data);
        protected abstract ValueWithUnit GetPerformancePeakValue(TPerformanceData data);
        protected abstract ValueWithUnit GetPerformanceSampleCountValue(TPerformanceData data);

        protected bool CompareValuesWithUnit(ValueWithUnit perfDataValue, ValueWithUnit filterValue, FilterOperatorType op)
        {
            // If both values are not the same type, bail out.
            if (!AreUnitSameType(perfDataValue, filterValue))
                return false;

            switch (op)
            {
                case FilterOperatorType.Equal:
                    return perfDataValue.CompareTo(filterValue) == 0;
                case FilterOperatorType.NotEqual:
                    return perfDataValue.CompareTo(filterValue) != 0;
                case FilterOperatorType.Greater:
                    return perfDataValue.CompareTo(filterValue) > 0;
                case FilterOperatorType.GreaterOrEqual:
                    return perfDataValue.CompareTo(filterValue) >= 0;
                case FilterOperatorType.Lesser:
                    return perfDataValue.CompareTo(filterValue) < 0;
                case FilterOperatorType.LesserOrEqual:
                    return perfDataValue.CompareTo(filterValue) <= 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        ParseResult<ValueWithUnit> ParseFilterValue(string filterValue)
        {
            if (m_SortedUnitTypes == null)
                BuildSortedUnityTypesCache();

            filterValue = filterValue.Replace(',', '.');
            filterValue = filterValue.ToLowerInvariant();

            var sv = filterValue.GetStringView();
            foreach (var unitType in m_SortedUnitTypes!)
            {
                if (!sv.EndsWith(unitType.suffix))
                    continue;

                var valueWithPowerPart = sv.Substring(0, sv.length - unitType.suffix.Length);
                foreach (var supportedPower in unitType.supportedPowers)
                {
                    var unitPower = m_UnitPowers[supportedPower];
                    string matchingSymbol = null;
                    if (unitPower.symbols == null || unitPower.symbols.Length == 0)
                    {
                        matchingSymbol = string.Empty;
                    }
                    else
                    {
                        foreach (var symbol in unitPower.symbols)
                        {
                            if (valueWithPowerPart.EndsWith(symbol))
                            {
                                matchingSymbol = symbol;
                            }
                        }
                    }

                    if (matchingSymbol != null)
                    {
                        var valuePart = valueWithPowerPart.Substring(0, valueWithPowerPart.length - matchingSymbol.Length);
                        if (Utils.TryParseLowerInvariant<double>(valuePart, out var value))
                        {
                            return new ParseResult<ValueWithUnit>(true, new ValueWithUnit(value, unitType.handle, supportedPower));
                        }
                    }
                }
            }

            // Always return a success so that it doesn't generate an error but can still be filtered out.
            return new ParseResult<ValueWithUnit>(true, new ValueWithUnit(0, UnitType.unsupported.handle, UnitPowerType.One));
        }
    }

    static class PerformanceWindowHelper
    {
        [MenuItem("Window/Analysis/Performance Markers", priority = 1272)]
        public static void OpenProvider()
        {
            var providerIds = new[] { PerformanceProvider.providerId, ProfilerMarkersProvider.providerId };
            var providers = SearchService.GetProviders(providerIds).ToArray();
            var context = SearchService.CreateContext(providers, "", SearchFlags.OpenContextual);
            context.useExplicitProvidersAsNormalProviders = true;
            var columns = new[] { new SearchColumn("Name", "label") }.Concat(providers[1].fetchColumns(context, null));
            var tableConfig = new SearchTable("perf", columns);
            var viewState = new SearchViewState(context, tableConfig);
            viewState.itemSize = (float)DisplayMode.Table;
            SearchService.ShowWindow(viewState);
        }
    }
}
