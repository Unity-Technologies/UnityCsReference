// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Profiling;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal struct UxmlStyleProperty : IEquatable<UxmlStyleProperty>
    {
        [CreateProperty(ReadOnly = true)]
        public StyleProperty inlineProperty;
        public bool requireVariableResolve => inlineProperty?.requireVariableResolve ?? false;

        [CreateProperty(ReadOnly = true)]
        public bool isInlined => inlineProperty?.handleCount > 0;
        public int cookie;

        public UxmlStyleProperty(StyleProperty inlineProperty)
        {
            this.inlineProperty = inlineProperty;
            cookie = 0;
            if (this.inlineProperty != null)
            {
                foreach (var value in this.inlineProperty.values)
                {
                    cookie = cookie * 31 + value.GetHashCode();
                }
            }
        }

        // Doesn't really support value changes to uxml values, but we can deal with that when we get there.
        public bool Equals(UxmlStyleProperty other)
        {
            if (inlineProperty != other.inlineProperty)
                return false;

            return cookie == other.cookie;
        }

        public override bool Equals(object obj)
        {
            return obj is UxmlStyleProperty other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(inlineProperty.values, requireVariableResolve);
        }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal struct ShortHandStylePropertyData
        : IEquatable<ShortHandStylePropertyData>
    {
        public StylePropertyId id { get; internal set; }

        [CreateProperty(ReadOnly = true)]
        public UxmlStyleProperty uxmlValue { get; internal set; }

        [CreateProperty(ReadOnly = true)]
        public Binding binding { get; internal set; }

        [CreateProperty(ReadOnly = true)]
        public SelectorMatchRecord selector { get; internal set; }

        [CreateProperty(ReadOnly = true)]
        public bool isUxmlOverridden => uxmlValue.isInlined || binding != null;

        public bool Equals(ShortHandStylePropertyData other)
        {
            return id == other.id &&
                   uxmlValue.Equals(other.uxmlValue) &&
                   binding == other.binding &&
                   selector.Equals(other.selector);
        }

        public override bool Equals(object obj)
        {
            return obj is ShortHandStylePropertyData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)id, uxmlValue, binding, selector);
        }

        public static bool operator ==(ShortHandStylePropertyData lhs, ShortHandStylePropertyData rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ShortHandStylePropertyData lhs, ShortHandStylePropertyData rhs)
        {
            return !(lhs == rhs);
        }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal struct StylePropertyData<
        TInline,         /* .style */
        TComputedValue>  /* .computedStyle */
        : IEquatable<StylePropertyData<TInline, TComputedValue>>
    {
        public StylePropertyId id { get; internal set; }

        [CreateProperty(ReadOnly = true)]
        public TInline inlineValue { get; internal set; }

        [CreateProperty(ReadOnly = true)]
        public UxmlStyleProperty uxmlValue { get; internal set; }

        [CreateProperty(ReadOnly = true)]
        public TComputedValue computedValue { get; internal set; }

        [CreateProperty(ReadOnly = true)]
        public Binding binding { get; internal set; }

        [CreateProperty(ReadOnly = true)]
        public SelectorMatchRecord selector { get; internal set; }

        [CreateProperty(ReadOnly = true)]
        public bool isUxmlOverridden => uxmlValue.isInlined || binding != null;

        public bool Equals(StylePropertyData<TInline, TComputedValue> other)
        {
            return id == other.id &&
                   EqualityComparer<TInline>.Default.Equals(inlineValue, other.inlineValue) &&
                   uxmlValue.Equals(other.uxmlValue) &&
                   EqualityComparer<TComputedValue>.Default.Equals(computedValue, other.computedValue) &&
                   binding == other.binding &&
                   selector.Equals(other.selector);
        }

        public override bool Equals(object obj)
        {
            return obj is StylePropertyData<StyleLength, Length> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)id, inlineValue, uxmlValue, computedValue, binding, selector);
        }

        public static bool operator ==(StylePropertyData<TInline, TComputedValue> lhs, StylePropertyData<TInline, TComputedValue> rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(StylePropertyData<TInline, TComputedValue> lhs, StylePropertyData<TInline, TComputedValue> rhs)
        {
            return !(lhs == rhs);
        }
    }

    internal readonly struct UxmlData
    {
        public readonly StyleProperty inlineProperty;
        public readonly BindingInfo bindingInfo;
        public readonly SelectorMatchRecord selector;

        public UxmlData(StyleProperty p, BindingInfo b, SelectorMatchRecord s)
        {
            inlineProperty = p;
            bindingInfo = b;
            selector = s;
        }

        public static UxmlData WithProperty(in UxmlData data, StyleProperty property)
        {
            return new UxmlData(property, data.bindingInfo, data.selector);
        }

        public static UxmlData WithBindingInfo(in UxmlData data, BindingInfo bindingInfo)
        {
            return new UxmlData(data.inlineProperty, bindingInfo, data.selector);
        }

        public static UxmlData WithSelector(in UxmlData data, SelectorMatchRecord selector)
        {
            return new UxmlData(data.inlineProperty, data.bindingInfo, selector);
        }
    }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    [Flags]
    internal enum StyleDiffAdditionalDataFlags
    {
        None = 0,
        UxmlInlineProperties = 1,
        Bindings = 2,
        Selectors = 4,

        All = UxmlInlineProperties | Bindings | Selectors
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal sealed partial class StyleDiff : INotifyBindablePropertyChanged, IDataSourceViewHashProvider, IDisposable
    {
        static readonly ProfilerMarker s_StyleDiffRefreshProfilerMarker = new ProfilerMarker("StyleDiff.Refresh()");
        internal static readonly MemoryLabel k_MemoryLabel = new (nameof(UIElements), $"Style.{nameof(StyleDiff)}");

        public enum ContextType
        {
            None,          // No current target
            VisualElement, // inline style sheet + inline styles
            StyleSheet     // style sheet + rule
        }

        internal readonly struct ResolutionContext
        {
            public readonly StyleDiff diff;
            public readonly StyleSheet styleSheet;
            public readonly Dictionary<string, UxmlData> uxmlData;

            public ResolutionContext(
                StyleDiff diff,
                StyleSheet inline,
                Dictionary<string, UxmlData> uxmlData)
            {
                this.diff = diff;
                styleSheet = inline;
                this.uxmlData = uxmlData;
            }
        }

        private long m_Version;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        private MatchedRulesExtractor m_MatchedRules;

        public ContextType currentContextType { get; private set; }
        public VisualElement currentTarget { get; private set; }
        public StyleSheet currentStyleSheet { get; private set; }
        public StyleRule currentRule { get; private set; }

        public List<SelectorMatchRecord> matchRecords => m_MatchedRules?.matchRecords;

        public StyleDiff()
        {
            m_MatchedRules = new MatchedRulesExtractor(null);
            currentContextType = ContextType.None;
        }

        public void RefreshElement(VisualElement element, StyleDiffAdditionalDataFlags flags = StyleDiffAdditionalDataFlags.All)
        {
            if (element == null)
            {
                // Set initial values
                Clear();
                return;
            }

            var visualTreeAsset = element.visualTreeAssetSource;
            var styleSheet = visualTreeAsset ? visualTreeAsset.inlineSheet : null;
            var styleRule = element.inlineStyleAccess?.inlineRule.rule;

            Refresh(element, styleSheet, styleRule, ContextType.VisualElement, flags);
        }

        public void RefreshRule(VisualElement element, StyleSheet styleSheet, StyleRule rule, StyleRule styleRule, StyleDiffAdditionalDataFlags flags = StyleDiffAdditionalDataFlags.All)
        {
            Refresh(element, styleSheet, styleRule, ContextType.StyleSheet, flags);
        }

        internal void Refresh(VisualElement element, StyleSheet styleSheet, StyleRule styleRule, ContextType type, StyleDiffAdditionalDataFlags flags = StyleDiffAdditionalDataFlags.All)
        {
            currentContextType = type;
            currentTarget = element;
            currentStyleSheet = styleSheet;
            currentRule = styleRule;

            m_MatchedRules.Clear();


            using var marker = s_StyleDiffRefreshProfilerMarker.Auto();
            using var uxmlDataHandle = DictionaryPool<string, UxmlData>.Get(out var uxmlData);

            if ((flags & StyleDiffAdditionalDataFlags.UxmlInlineProperties) == StyleDiffAdditionalDataFlags.UxmlInlineProperties && null != styleRule)
            {
                foreach (var property in styleRule.properties)
                {
                    if (StylePropertyUtil.ussNameToCSharpName.TryGetValue(property.name, out var csharpName) && csharpName != property.name)
                    {
                        var d = uxmlData.GetValueOrDefault(csharpName);
                        uxmlData[csharpName] = UxmlData.WithProperty(d, property);
                    }

                    {
                        var d = uxmlData.GetValueOrDefault(csharpName);
                        uxmlData[property.name] = UxmlData.WithProperty(d, property);
                    }
                }
            }

            if ((flags & StyleDiffAdditionalDataFlags.Bindings) == StyleDiffAdditionalDataFlags.Bindings)
            {
                using var listHandle = ListPool<BindingInfo>.Get(out var bindingInfos);
                element.GetBindingInfos(bindingInfos);

                foreach (var info in bindingInfos)
                {
                    PropertyPath path = info.bindingId;
                    if (path.Length == 2 && path[0].IsName && string.CompareOrdinal(path[0].Name, "style") == 0 && path[1].IsName)
                    {
                        var styleNamePart = path[1].Name;
                        var d = uxmlData.GetValueOrDefault(styleNamePart);
                        uxmlData[styleNamePart] = UxmlData.WithBindingInfo(d, info);
                    }
                }
            }

            if ((flags & StyleDiffAdditionalDataFlags.Selectors) == StyleDiffAdditionalDataFlags.Selectors)
            {
                using var handle = DictionaryPool<string, SelectorMatchRecord>.Get(out var propertyToMatchRecord);
                FindMatchingRules(currentTarget, propertyToMatchRecord);

                foreach (var record in propertyToMatchRecord)
                {
                    var d = uxmlData.GetValueOrDefault(record.Key);
                    uxmlData[record.Key] = UxmlData.WithSelector(d, record.Value);
                }
            }

            var context = new ResolutionContext(this, styleSheet, uxmlData);
            Refresh(currentTarget, in context);
        }

        private void FindMatchingRules(VisualElement element, Dictionary<string, SelectorMatchRecord> propertyToMatchRecord)
        {
            m_MatchedRules.FindMatchingRules(element);
            for (var i = 0; i < m_MatchedRules.matchRecords.Count; ++i)
            {
                var record = m_MatchedRules.matchRecords[i];
                var properties = record.complexSelector.rule.properties;
                for (var j = 0; j < properties.Length; ++j)
                {
                    var property = properties[j];
                    if (StylePropertyUtil.ussNameToCSharpName.TryGetValue(property.name, out var csharpName) && csharpName != property.name)
                        propertyToMatchRecord[csharpName] = record;
                    propertyToMatchRecord[property.name] = record;
                }
            }
        }

        static StylePropertyData<TInline, TComputed> ComputeStyleProperty<TInline, TComputed>(
            StylePropertyId id,
            string propertyName,
            in TInline inlineStyle,
            in TComputed computedStyle,
            in ResolutionContext context)
        {
            var property = new StylePropertyData<TInline, TComputed>
            {
                id = id,
                inlineValue = inlineStyle,
                computedValue = computedStyle,
            };

            if (!context.uxmlData.TryGetValue(propertyName, out var uxmlData))
                return property;

            var inlined = null != uxmlData.inlineProperty;
            property.uxmlValue = inlined
                ? new UxmlStyleProperty(uxmlData.inlineProperty)
                : new UxmlStyleProperty(null);

            property.binding = uxmlData.bindingInfo.binding;

            property.selector = uxmlData.selector;
            return property;
        }

        static ShortHandStylePropertyData ComputeStyleProperty(
            StylePropertyId id,
            string propertyName,
            in ResolutionContext context)
        {
            var property = new ShortHandStylePropertyData
            {
                id = id,
            };

            if (!context.uxmlData.TryGetValue(propertyName, out var uxmlData))
                return property;

            var inlined = null != uxmlData.inlineProperty;
            property.uxmlValue = inlined
                ? new UxmlStyleProperty(uxmlData.inlineProperty)
                : new UxmlStyleProperty(null);

            property.binding = uxmlData.bindingInfo.binding;
            property.selector = uxmlData.selector;
            return property;
        }

        private void Notify([CallerMemberName] string name = null)
        {
            m_Version += 1;
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(name));
        }

        public long GetViewHashCode()
        {
            return m_Version;
        }

        public void Dispose()
        {
            Clear();
        }

        public void Clear()
        {
            currentContextType = ContextType.None;
            currentTarget = null;
            currentStyleSheet = null;
            currentRule = null;
            m_MatchedRules.Clear();
        }
    }
}
