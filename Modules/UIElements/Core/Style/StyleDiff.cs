// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.Rendering;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    internal struct UxmlStyleProperty : IDisposable, IEquatable<UxmlStyleProperty>
    {
        public NativeArray<StyleValueHandle> values;
        public bool requireVariableResolve;

        public bool isInlined => values.Length > 0;

        public UxmlStyleProperty(StyleValueHandle[] values, bool requireVariableResolve)
        {
            this.values = new NativeArray<StyleValueHandle>(values, Allocator.Persistent);
            this.requireVariableResolve = requireVariableResolve;
        }

        public bool Equals(UxmlStyleProperty other)
        {
            if (requireVariableResolve != other.requireVariableResolve)
                return false;

            if (values.IsCreated != other.values.IsCreated)
                return false;

            if (!values.IsCreated)
                return true;

            if (values.Length != other.values.Length)
                return false;

            for (var i = 0; i < values.Length; ++i)
            {
                if (values[i] != other.values[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is UxmlStyleProperty other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(values, requireVariableResolve);
        }

        public void Dispose()
        {
            values.Dispose();
        }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal struct StylePropertyData<
        TInline,         /* .style */
        TComputedValue>  /* .computedStyle */
        : IEquatable<StylePropertyData<TInline, TComputedValue>>, IDisposable
    {
        public VisualElement target { get; internal set; }

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
            // Intentionally leaving the target out of the comparison.
            return EqualityComparer<TInline>.Default.Equals(inlineValue, other.inlineValue) &&
                   EqualityComparer<UxmlStyleProperty>.Default.Equals(uxmlValue, other.uxmlValue) &&
                   EqualityComparer<TComputedValue>.Default.Equals(computedValue, other.computedValue) &&
                   binding == other.binding &&
                   EqualityComparer<SelectorMatchRecord>.Default.Equals(selector, other.selector);
        }

        public override bool Equals(object obj)
        {
            return obj is StylePropertyData<StyleLength, Length> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(inlineValue, uxmlValue, computedValue, binding, selector);
        }

        public static bool operator ==(StylePropertyData<TInline, TComputedValue> lhs, StylePropertyData<TInline, TComputedValue> rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(StylePropertyData<TInline, TComputedValue> lhs, StylePropertyData<TInline, TComputedValue> rhs)
        {
            return !(lhs == rhs);
        }

        public void Dispose()
        {
            uxmlValue.Dispose();
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

    [Flags]
    internal enum StyleDiffAdditionalDataFlags
    {
        None = 0,
        UxmlInlineProperties = 1,
        Bindings = 2,
        Selectors = 4,

        All = UxmlInlineProperties | Bindings | Selectors
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal sealed partial class StyleDiff : INotifyBindablePropertyChanged, IDataSourceViewHashProvider, IDisposable
    {
        internal readonly struct ResolutionContext
        {
            public readonly StyleDiff diff;
            public readonly StyleSheet styleSheet;
            public readonly Dictionary<string, UxmlData> uxmlData;
            public readonly HashSet<string> uxmlOverrides;

            public ResolutionContext(
                StyleDiff diff,
                StyleSheet inline,
                Dictionary<string, UxmlData> uxmlData,
                HashSet<string> uxmlOverrides)
            {
                this.diff = diff;
                styleSheet = inline;
                this.uxmlData = uxmlData;
                this.uxmlOverrides = uxmlOverrides;
            }

            public void MarkAsOverride(string name)
            {
                if (uxmlOverrides.Add(name))
                    diff.Notify(name);
            }

            public void ClearOverride(string name)
            {
                if(uxmlOverrides.Remove(name))
                    diff.Notify(name);
            }
        }

        private long m_Version;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        [CreateProperty]
        private readonly HashSet<string> uxmlOverrides = new HashSet<string>();

        private MatchedRulesExtractor m_MatchedRules;

        public StyleDiff()
        {
            m_MatchedRules = new MatchedRulesExtractor(null);
        }

        public void Refresh(VisualElement element, StyleDiffAdditionalDataFlags flags = StyleDiffAdditionalDataFlags.All)
        {
            if (element == null)
            {
                // Set initial values
                return;
            }

            var visualTreeAsset = element.visualTreeAssetSource;
            var styleSheet = visualTreeAsset ? visualTreeAsset.inlineSheet : null;
            var styleRule = element.inlineStyleAccess?.inlineRule.rule;

            Refresh(element, styleSheet, styleRule, flags);
        }

        internal void Refresh(VisualElement element, StyleSheet styleSheet, StyleRule styleRule, StyleDiffAdditionalDataFlags flags = StyleDiffAdditionalDataFlags.All)
        {
            m_MatchedRules.Clear();
            uxmlOverrides.Clear();

            using var uxmlDataHandle = DictionaryPool<string, UxmlData>.Get(out var uxmlData);

            if ((flags & StyleDiffAdditionalDataFlags.UxmlInlineProperties) == StyleDiffAdditionalDataFlags.UxmlInlineProperties && null != styleRule)
            {
                foreach (var property in styleRule.properties)
                {
                    if (StylePropertyUtil.ussNameToCSharpName.TryGetValue(property.name, out var csharpName) && csharpName != property.name)
                    {
                        uxmlOverrides.Add(csharpName);
                        var d = uxmlData.GetValueOrDefault(csharpName);
                        uxmlData[csharpName] = UxmlData.WithProperty(d, property);
                    }

                    {
                        uxmlOverrides.Add(property.name);
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
                        uxmlOverrides.Add(styleNamePart);
                        var d = uxmlData.GetValueOrDefault(styleNamePart);
                        uxmlData[styleNamePart] = UxmlData.WithBindingInfo(d, info);
                    }
                }
            }

            if ((flags & StyleDiffAdditionalDataFlags.Selectors) == StyleDiffAdditionalDataFlags.Selectors)
            {
                using var handle = DictionaryPool<string, SelectorMatchRecord>.Get(out var propertyToMatchRecord);
                FindMatchingRules(element, propertyToMatchRecord);

                foreach (var record in propertyToMatchRecord)
                {
                    var d = uxmlData.GetValueOrDefault(record.Key);
                    uxmlData[record.Key] = UxmlData.WithSelector(d, record.Value);
                }
            }

            var context = new ResolutionContext(this, styleSheet, uxmlData, uxmlOverrides);
            Refresh(element, in context);
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
            VisualElement element,
            string propertyName,
            in TInline inlineStyle,
            in TComputed computedStyle,
            in ResolutionContext context)
        {
            var property = new StylePropertyData<TInline, TComputed>
            {
                target = element,
                inlineValue = inlineStyle,
                computedValue = computedStyle,
            };

            if (!context.uxmlData.TryGetValue(propertyName, out var uxmlData))
            {
                context.ClearOverride(propertyName);
                return property;
            }

            var inlined = null != uxmlData.inlineProperty;
            property.uxmlValue = inlined
                ? new UxmlStyleProperty(uxmlData.inlineProperty.values, uxmlData.inlineProperty.ContainsVariable())
                : new UxmlStyleProperty(Array.Empty<StyleValueHandle>(), false);

            property.binding = uxmlData.bindingInfo.binding;

            if (inlined || null != uxmlData.bindingInfo.binding)
                context.MarkAsOverride(propertyName);
            else
                context.ClearOverride(propertyName);

            property.selector = uxmlData.selector;
            return property;
        }

        public bool HasUxmlOverrides(string stylePropertyName)
        {
            return !string.IsNullOrEmpty(stylePropertyName) &&
                   uxmlOverrides.Contains(stylePropertyName);
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
            m_MatchedRules.Clear();
            uxmlOverrides.Clear();
            DisposeProperties();
        }

        partial void DisposeProperties();
    }
}
