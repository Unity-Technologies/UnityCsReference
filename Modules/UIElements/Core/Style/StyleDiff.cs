// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.Unmanaged;

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

        public StylePropertyData(StylePropertyId id) { this.id = id; }
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
        private bool SetInlineValue<TInline, TComputed>(ref StylePropertyData<TInline, TComputed> property,
            TInline inlineValue)
            where TInline : struct, IEquatable<TInline>
        {
            if (!property.inlineValue.Equals(inlineValue))
            {
                property.inlineValue = inlineValue;
                return true;
            }

            return false;
        }

        private bool SetComputedValue<TInline, TComputed>(ref StylePropertyData<TInline, TComputed> property,
            TComputed computedValue)
            where TComputed : struct, IEquatable<TComputed>
        {
            if (!property.computedValue.Equals(computedValue))
            {
                property.computedValue = computedValue;
                return true;
            }

            return false;
        }

        private bool SetComputedValue<TInline, TComputed>(ref StylePropertyData<StyleEnum<TInline>, TComputed> property,
            TComputed computedValue)
            where TInline : struct, IConvertible
            where TComputed : struct, IConvertible
        {
            if (UnsafeUtility.EnumToInt(property.computedValue) != UnsafeUtility.EnumToInt(computedValue))
            {
                property.computedValue = computedValue;
                return true;
            }

            return false;
        }

        private bool SetComputedValue(ref StylePropertyData<StyleBackground, Background> property,
            EntityId unmanagedValue)
        {
            var computedValue = Background.From(unmanagedValue);
            if (!property.computedValue.Equals(computedValue))
            {
                property.computedValue = computedValue;
                return true;
            }

            return false;
        }

        private bool SetComputedValue(ref StylePropertyData<StyleFontDefinition, FontDefinition> property,
            EntityId unmanagedValue)
        {
            var computedValue = FontDefinition.From(unmanagedValue);
            if (!property.computedValue.Equals(computedValue))
            {
                property.computedValue = computedValue;
                return true;
            }

            return false;
        }

        private bool SetComputedValue(ref StylePropertyData<StyleUIAnimationClip, UIAnimationClip> property, EntityId unmanagedValue)
        {
            var computedValue = (UIAnimationClip)Resources.EntityIdToObject(unmanagedValue);
            if (property.computedValue != computedValue)
            {
                property.computedValue = computedValue;
                return true;
            }

            return false;
        }

        private bool SetComputedValue(ref StylePropertyData<StyleFont, Font> property, EntityId unmanagedValue)
        {
            var computedValue = (Font)Resources.EntityIdToObject(unmanagedValue);
            if (property.computedValue != computedValue)
            {
                property.computedValue = computedValue;
                return true;
            }

            return false;
        }

        private bool SetComputedValue<TComputed>(ref StylePropertyData<StyleList<TComputed>, List<TComputed>> property,
            ReadOnlySpan<TComputed> computedValue)
            where TComputed : unmanaged, IEquatable<TComputed>
        {
            if (ValueEquals(computedValue, property.computedValue))
                return false;

            var result = property.computedValue;
            computedValue.CopyTo(ref result);
            property.computedValue = result;
            return true;
        }

        private bool SetComputedValue(ref StylePropertyData<StyleList<FilterFunction>, List<FilterFunction>> property,
            ReadOnlySpan<UnmanagedFilterFunction> computedValue)
        {
            if (ValueEquals(computedValue, property.computedValue, (a, b) => a.Equals(b)))
                return false;

            var result = property.computedValue;
            computedValue.CopyTo(ref result, f => f);
            property.computedValue = result;
            return true;
        }

        private bool SetComputedValue(ref StylePropertyData<StyleList<StylePropertyName>, List<StylePropertyName>> property,
            ReadOnlySpan<StylePropertyId> computedValue)
        {
            if (ValueEquals(computedValue, property.computedValue, (a, b) => a == b.id))
                return false;

            var result = property.computedValue;
            computedValue.CopyTo(ref result, id => new StylePropertyName(id));
            property.computedValue = result;
            return true;
        }

        private bool SetComputedValue(ref StylePropertyData<StyleMaterialDefinition, MaterialDefinition> property,
            UnmanagedMaterialDefinition computedValue)
        {
            if (ValueEquals(computedValue, property.computedValue))
                return false;

            property.computedValue = MaterialDefinition.From(computedValue);
            return true;
        }

        private bool ApplyContext<TInline, TComputed>(ref StylePropertyData<TInline, TComputed> property, in ResolutionContext context)
        {
            UxmlStyleProperty uxmlValue =
                context.uxmlData.TryGetValue(property.id, out var uxmlData) && null != uxmlData.inlineProperty
                    ? new UxmlStyleProperty(uxmlData.inlineProperty)
                    : new UxmlStyleProperty(null);

            var notify = false;
            if (!property.uxmlValue.Equals(uxmlValue))
            {
                property.uxmlValue = uxmlValue;
                notify = true;
            }
            if (property.binding != uxmlData.bindingInfo.binding)
            {
                property.binding = uxmlData.bindingInfo.binding;
                notify = true;
            }
            if (!property.selector.Equals(uxmlData.selector))
            {
                property.selector = uxmlData.selector;
                notify = true;
            }

            return notify;
        }

        private static bool ValueEquals<T>(ReadOnlySpan<T> a, List<T> b)
            where T : unmanaged, IEquatable<T>
        {
            if (b == null) return false; // As long as ToList(empty) doesn't return null, we need them to differ
            if (a.IsEmpty) return b.Count == 0;
            if (a.Length != b.Count) return false;
            for (var i = 0; i < a.Length; i++)
                if (!a[i].Equals(b[i]))
                    return false;
            return true;
        }

        private static bool ValueEquals<T, TOther>(ReadOnlySpan<T> a, List<TOther> b, Func<T, TOther, bool> equals)
            where T : unmanaged
        {
            if (b == null) return false; // As long as ToList(empty) doesn't return null, we need them to differ
            if (a.IsEmpty) return b.Count == 0;
            if (a.Length != b.Count) return false;
            for (var i = 0; i < a.Length; i++)
                if (!equals(a[i], b[i]))
                    return false;
            return true;
        }

        private static bool ValueEquals(UnmanagedMaterialDefinition a, MaterialDefinition b)
        {
            return a.material == (b.material != null ? b.material.GetEntityId() : EntityId.None) &&
                   (b.propertyValues == null ? a.propertyValues.IsEmpty :
                   ValueEquals(a.propertyValues.ToReadOnlySpan(), b.propertyValues, (p1, p2) => p1.Equals(p2)));
        }

        static readonly ProfilerMarker s_StyleDiffRefreshProfilerMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, "StyleDiff.Refresh()");
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
            public readonly Dictionary<StylePropertyId, UxmlData> uxmlData;

            public ResolutionContext(
                StyleDiff diff,
                StyleSheet inline,
                Dictionary<StylePropertyId, UxmlData> uxmlData)
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

        public void RefreshRule(VisualElement element, StyleSheet styleSheet, StyleRule styleRule, StyleDiffAdditionalDataFlags flags = StyleDiffAdditionalDataFlags.All)
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
            using var uxmlDataHandle = DictionaryPool<StylePropertyId, UxmlData>.Get(out var uxmlData);

            if ((flags & StyleDiffAdditionalDataFlags.UxmlInlineProperties) == StyleDiffAdditionalDataFlags.UxmlInlineProperties && null != styleRule)
            {
                foreach (var property in styleRule.properties)
                {
                    if (property.id == StylePropertyId.Custom)
                    {
                        // [TODO] Support variables in the style diff
                        continue;
                    }
                    var d = uxmlData.GetValueOrDefault(property.id);
                    uxmlData[property.id] = UxmlData.WithProperty(d, property);

                    if (StyleDebug.IsShorthandProperty(property.id))
                    {
                        using var listHandle = ListPool<StylePropertyId>.Get(out var longHandPropertyIds);
                        StyleDebug.PopulateLonghandPropertyIds(property.id, longHandPropertyIds);
                        foreach (var longHandId in longHandPropertyIds)
                        {
                            var longHandData = uxmlData.GetValueOrDefault(longHandId);
                            uxmlData[longHandId] = UxmlData.WithProperty(longHandData, property);
                        }
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
                        if (StylePropertyUtil.propertyNameToStylePropertyId.TryGetValue(styleNamePart, out var id) ||
                            (StylePropertyUtil.cSharpNameToUssName.TryGetValue(styleNamePart, out var propertyName) &&
                            StylePropertyUtil.propertyNameToStylePropertyId.TryGetValue(propertyName, out id)))
                        {
                            var d = uxmlData.GetValueOrDefault(id);
                            uxmlData[id] = UxmlData.WithBindingInfo(d, info);
                        }
                    }
                }
            }

            if ((flags & StyleDiffAdditionalDataFlags.Selectors) == StyleDiffAdditionalDataFlags.Selectors)
            {
                using var handle = DictionaryPool<StylePropertyId, SelectorMatchRecord>.Get(out var propertyToMatchRecord);
                FindMatchingRules(currentTarget, propertyToMatchRecord);

                foreach (var record in propertyToMatchRecord)
                {
                    var propertyId = record.Key;
                    var d = uxmlData.GetValueOrDefault(propertyId);
                    uxmlData[propertyId] = UxmlData.WithSelector(d, record.Value);
                    if (StyleDebug.IsShorthandProperty(propertyId))
                    {
                        using var listHandle = ListPool<StylePropertyId>.Get(out var longHandPropertyIds);
                        StyleDebug.PopulateLonghandPropertyIds(propertyId, longHandPropertyIds);
                        foreach (var longHandId in longHandPropertyIds)
                        {
                            var longHandData = uxmlData.GetValueOrDefault(longHandId);
                            uxmlData[longHandId] = UxmlData.WithSelector(longHandData, record.Value);
                        }
                    }
                }
            }

            var context = new ResolutionContext(this, styleSheet, uxmlData);
            Refresh(currentTarget, in context);
        }

        private void FindMatchingRules(VisualElement element, Dictionary<StylePropertyId, SelectorMatchRecord> propertyToMatchRecord)
        {
            m_MatchedRules.FindMatchingRules(element);
            for (var i = 0; i < m_MatchedRules.matchRecords.Count; ++i)
            {
                var record = m_MatchedRules.matchRecords[i];
                var properties = record.complexSelector.rule.properties;
                for (var j = 0; j < properties.Length; ++j)
                {
                    var property = properties[j];
                    propertyToMatchRecord[property.id] = record;
                }
            }
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

            if (!context.uxmlData.TryGetValue(id, out var uxmlData))
                return property;

            var inlined = null != uxmlData.inlineProperty;
            property.uxmlValue = inlined
                ? new UxmlStyleProperty(uxmlData.inlineProperty)
                : new UxmlStyleProperty(null);

            property.binding = uxmlData.bindingInfo.binding;
            property.selector = uxmlData.selector;

            return property;
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void MarkAllPropertiesDirty() => Notify(null);

        void Notify([CallerMemberName] string name = null)
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
