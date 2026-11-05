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
using Unity.Scripting.LifecycleManagement;
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

    internal struct UxmlData
    {
        public StyleProperty inlineProperty;
        public Binding binding;
        public SelectorMatchRecord selector;

        // Whether the entry is listed in the ids of the written entries.
        public bool written;
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
            in UnmanagedBackground unmanagedValue)
        {
            // Comparing the unmanaged value first avoids resolving the image and allocating the gradient stops.
            if (ValueEquals(unmanagedValue, property.computedValue))
                return false;

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
            if (GetEntityId(property.computedValue) == unmanagedValue)
                return false;

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
            where TComputed : unmanaged
        {
            if (ValueEquals(computedValue, property.computedValue))
                return false;

            var result = property.computedValue;
            computedValue.CopyTo(ref result);
            property.computedValue = result;
            return true;
        }

        private bool SetComputedValue(ref StylePropertyData<StyleList<UIAnimationClip>, List<UIAnimationClip>> property,
            ReadOnlySpan<EntityId> computedValue)
        {
            if (ValueEquals(computedValue, property.computedValue,
                    (id, clip) => id == (clip != null ? clip.GetEntityId() : EntityId.None)))
                return false;

            var result = property.computedValue;
            computedValue.CopyTo(ref result, id => (UIAnimationClip)Resources.EntityIdToObject(id));
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
            ref readonly var uxmlData = ref context.uxmlData[(int)property.id];
            var uxmlValue = new UxmlStyleProperty(uxmlData.inlineProperty);

            var notify = false;
            if (!property.uxmlValue.Equals(uxmlValue))
            {
                property.uxmlValue = uxmlValue;
                notify = true;
            }
            if (property.binding != uxmlData.binding)
            {
                property.binding = uxmlData.binding;
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
            where T : unmanaged
        {
            if (b == null) return false; // As long as ToList(empty) doesn't return null, we need them to differ
            if (a.IsEmpty) return b.Count == 0;
            if (a.Length != b.Count) return false;
            // EqualityComparer<T>.Default handles enum element types (which don't implement IEquatable<T>) without boxing.
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < a.Length; i++)
                if (!comparer.Equals(a[i], b[i]))
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

        // Entries that are the same as at the previous refresh are already applied to the properties, unless their
        // inline property was modified since.
        private void FindChangedUxmlData(bool propertiesMatchPreviousRefresh)
        {
            if (!propertiesMatchPreviousRefresh)
            {
                Array.Fill(m_UxmlDataChanged, true);
                return;
            }

            Array.Clear(m_UxmlDataChanged, 0, m_UxmlDataChanged.Length);

            // Entries that were only written at the previous refresh differ from the default current ones.
            for (var i = 0; i < m_PreviousUxmlDataIds.Count; ++i)
            {
                var id = m_PreviousUxmlDataIds[i];
                m_UxmlDataChanged[id] = !m_UxmlData[id].written;
            }

            for (var i = 0; i < m_UxmlDataIds.Count; ++i)
            {
                var id = m_UxmlDataIds[i];
                ref readonly var current = ref m_UxmlData[id];
                ref readonly var previous = ref m_PreviousUxmlData[id];
                m_UxmlDataChanged[id] =
                    current.inlineProperty != null ||
                    previous.inlineProperty != null ||
                    current.binding != previous.binding ||
                    !current.selector.Equals(previous.selector);
            }
        }

        private ref UxmlData WriteUxmlData(int id)
        {
            ref var data = ref m_UxmlData[id];
            if (!data.written)
            {
                data.written = true;
                m_UxmlDataIds.Add(id);
            }
            return ref data;
        }

        // Stores the computed style group in the snapshot, and returns whether the snapshot already held the same bytes.
        private static unsafe bool UpdateComputedStyleSnapshot<T>(ref T snapshot, in T current) where T : unmanaged
        {
            bool unchanged;
            fixed (T* snapshotPtr = &snapshot)
                unchanged = UnsafeUtility.MemCmp(snapshotPtr, Unsafe.AsPointer(ref Unsafe.AsRef(in current)), sizeof(T)) == 0;

            if (!unchanged)
                snapshot = current;
            return unchanged;
        }

        // Destroyed objects keep their id, and compare equal to the null that their id resolves to.
        private static EntityId GetEntityId(UnityEngine.Object obj)
        {
            return ReferenceEquals(obj, null) ? EntityId.None : obj.GetEntityId();
        }

        // Same result as comparing Background.From(a) with b.
        private static bool ValueEquals(in UnmanagedBackground a, in Background b)
        {
            UnityEngine.Object image = b.texture;
            if (ReferenceEquals(image, null))
                image = b.sprite;
            if (ReferenceEquals(image, null))
                image = b.renderTexture;
            if (ReferenceEquals(image, null))
                image = b.vectorImage;
            if (a.imageEntityId != GetEntityId(image))
                return false;

            var gradient = a.GetGradient();
            if (gradient.stopCount <= 0)
                return b.gradient.Equals(default);

            var stops = b.gradient.stops;
            if (stops == null || stops.Length != gradient.stopCount)
                return false;
            if (gradient.type != b.gradient.type ||
                gradient.shape != b.gradient.shape ||
                gradient.size != b.gradient.size ||
                !gradient.angle.Equals(b.gradient.angle) ||
                gradient.angleUnit != b.gradient.angleUnit ||
                !gradient.position.Equals(b.gradient.position))
            {
                return false;
            }

            for (var i = 0; i < stops.Length; ++i)
            {
                if (!gradient.stops[i].Equals((UnmanagedBackgroundGradientStop)stops[i]))
                    return false;
            }
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
            public readonly UxmlData[] uxmlData;

            // Whether the entry of each property can differ from the one applied at the previous refresh.
            public readonly bool[] uxmlDataChanged;

            // Whether the properties hold the values of the previous refresh, so that unchanged values can be skipped.
            public readonly bool propertiesMatchPreviousRefresh;

            public ResolutionContext(
                StyleDiff diff,
                StyleSheet inline,
                UxmlData[] uxmlData,
                bool[] uxmlDataChanged,
                bool propertiesMatchPreviousRefresh)
            {
                this.diff = diff;
                styleSheet = inline;
                this.uxmlData = uxmlData;
                this.uxmlDataChanged = uxmlDataChanged;
                this.propertiesMatchPreviousRefresh = propertiesMatchPreviousRefresh;
            }
        }

        private long m_Version;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        private StyleMatchingContext m_MatchingContext;
        private readonly List<StyleSelectorMatch> m_SelectorMatches = new();
        private readonly List<SelectorMatchRecord> m_MatchRecords = new();

        static readonly int k_StylePropertyIdCount = GetStylePropertyIdCount();
        static readonly StylePropertyId[][] k_LonghandPropertyIds = CreateLonghandPropertyIds();

        private UxmlData[] m_UxmlData = new UxmlData[k_StylePropertyIdCount];
        private UxmlData[] m_PreviousUxmlData = new UxmlData[k_StylePropertyIdCount];
        private readonly bool[] m_UxmlDataChanged = new bool[k_StylePropertyIdCount];

        // Ids of the entries written in m_UxmlData and m_PreviousUxmlData, the others are all default.
        private List<int> m_UxmlDataIds = new();
        private List<int> m_PreviousUxmlDataIds = new();
        // False until a refresh completes, so that an interrupted refresh doesn't skip the values it didn't apply.
        private bool m_PropertiesMatchPreviousRefresh;

        // Most specific selector of each property, in the order in which the properties were first matched.
        private readonly SelectorMatchRecord[] m_PropertySelectors = new SelectorMatchRecord[k_StylePropertyIdCount];
        private readonly bool[] m_HasPropertySelector = new bool[k_StylePropertyIdCount];
        private readonly List<StylePropertyId> m_SelectorPropertyIds = new();

        // USS and C# style property names, as used in "style.<name>" binding ids.
        [NoAutoStaticsCleanup]
        static readonly Dictionary<string, StylePropertyId> k_StyleBindingNameToPropertyId = CreateStyleBindingNameToPropertyId();
        private readonly List<string> m_StyleBindingNames = new();

        // Read-only stand-in for elements without inline styles, so that the diff doesn't create one for them.
        [NoAutoStaticsCleanup]
        static readonly InlineStyleAccess k_EmptyInlineStyle = new(null);
        private bool m_InlineValuesUnset;

        public ContextType currentContextType { get; private set; }
        public VisualElement currentTarget { get; private set; }
        public StyleSheet currentStyleSheet { get; private set; }
        public StyleRule currentRule { get; private set; }

        public List<SelectorMatchRecord> matchRecords => m_MatchRecords;

        public StyleDiff()
        {
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
            if (element is { resourcesReleased: true })
            {
                Clear();
                return;
            }

            currentContextType = type;
            currentTarget = element;
            currentStyleSheet = styleSheet;
            currentRule = styleRule;

            m_MatchRecords.Clear();


            using var marker = s_StyleDiffRefreshProfilerMarker.Auto();
            (m_UxmlData, m_PreviousUxmlData) = (m_PreviousUxmlData, m_UxmlData);
            (m_UxmlDataIds, m_PreviousUxmlDataIds) = (m_PreviousUxmlDataIds, m_UxmlDataIds);
            var uxmlData = m_UxmlData;
            for (var i = 0; i < m_UxmlDataIds.Count; ++i)
                uxmlData[m_UxmlDataIds[i]] = default;
            m_UxmlDataIds.Clear();

            if ((flags & StyleDiffAdditionalDataFlags.UxmlInlineProperties) == StyleDiffAdditionalDataFlags.UxmlInlineProperties && null != styleRule)
            {
                var properties = styleRule.properties;
                for (var i = 0; i < properties.Length; ++i)
                {
                    var property = properties[i];
                    if (property.id == StylePropertyId.Custom)
                    {
                        // [TODO] Support variables in the style diff
                        continue;
                    }
                    WriteUxmlData((int)property.id).inlineProperty = property;

                    var longhandIds = k_LonghandPropertyIds[(int)property.id];
                    for (var j = 0; j < longhandIds.Length; ++j)
                        WriteUxmlData((int)longhandIds[j]).inlineProperty = property;
                }
            }

            if ((flags & StyleDiffAdditionalDataFlags.Bindings) == StyleDiffAdditionalDataFlags.Bindings)
            {
                // Same bindings as VisualElement.GetBindingInfos, without building a BindingInfo for each of them.
                using var listHandle = ListPool<(Binding binding, BindingId bindingId)>.Get(out var bindingRequests);
                DataBindingManager.GetBindingRequests(element, bindingRequests);
                for (var i = 0; i < bindingRequests.Count; ++i)
                {
                    var request = bindingRequests[i];
                    ApplyStyleBinding(request.binding, request.bindingId);
                }

                var bindingData = element.elementPanel?.dataBindingManager.GetBindingData(element);
                if (bindingData != null)
                {
                    for (var i = 0; i < bindingData.Count; ++i)
                    {
                        var data = bindingData[i];
                        ApplyStyleBinding(data.binding, data.target.bindingId);
                    }
                }
                m_StyleBindingNames.Clear();
            }

            if ((flags & StyleDiffAdditionalDataFlags.Selectors) == StyleDiffAdditionalDataFlags.Selectors)
            {
                FindMatchingRules(currentTarget);

                for (var i = 0; i < m_SelectorPropertyIds.Count; ++i)
                {
                    var propertyId = (int)m_SelectorPropertyIds[i];
                    var record = m_PropertySelectors[propertyId];
                    m_HasPropertySelector[propertyId] = false;
                    m_PropertySelectors[propertyId] = default;

                    WriteUxmlData(propertyId).selector = record;
                    var longhandIds = k_LonghandPropertyIds[propertyId];
                    for (var j = 0; j < longhandIds.Length; ++j)
                        WriteUxmlData((int)longhandIds[j]).selector = record;
                }
                m_SelectorPropertyIds.Clear();
            }

            var propertiesMatchPreviousRefresh = m_PropertiesMatchPreviousRefresh;
            FindChangedUxmlData(propertiesMatchPreviousRefresh);
            var context = new ResolutionContext(this, styleSheet, uxmlData, m_UxmlDataChanged, propertiesMatchPreviousRefresh);

            m_PropertiesMatchPreviousRefresh = false;
            Refresh(currentTarget, in context);
            m_PropertiesMatchPreviousRefresh = true;
        }

        // The first binding of each binding id wins, like in VisualElement.GetBindingInfos.
        private void ApplyStyleBinding(Binding binding, in BindingId bindingId)
        {
            PropertyPath path = bindingId;
            if (path.Length != 2 || !path[0].IsName || !path[1].IsName || string.CompareOrdinal(path[0].Name, "style") != 0)
                return;

            var styleName = path[1].Name;
            if (!k_StyleBindingNameToPropertyId.TryGetValue(styleName, out var id))
                return;

            for (var i = 0; i < m_StyleBindingNames.Count; ++i)
            {
                if (string.Equals(m_StyleBindingNames[i], styleName, StringComparison.Ordinal))
                    return;
            }
            m_StyleBindingNames.Add(styleName);

            if (binding != null)
                WriteUxmlData((int)id).binding = binding;
        }

        private static Dictionary<string, StylePropertyId> CreateStyleBindingNameToPropertyId()
        {
            var nameToId = new Dictionary<string, StylePropertyId>(StylePropertyUtil.propertyNameToStylePropertyId);
            foreach (var pair in StylePropertyUtil.cSharpNameToUssName)
            {
                if (StylePropertyUtil.propertyNameToStylePropertyId.TryGetValue(pair.Value, out var id))
                    nameToId.TryAdd(pair.Key, id);
            }
            return nameToId;
        }

        private void FindMatchingRules(VisualElement element)
        {
            m_MatchingContext ??= new StyleMatchingContext(applyPseudoMasks: false);
            m_MatchingContext.currentElement = element;
            var pushedElementCount = PushStyleSheetsAndAncestors(element, m_MatchingContext);
            try
            {
                StyleSelectorHelper.FindMatches(m_MatchingContext, m_SelectorMatches);
                m_SelectorMatches.Sort(StyleSelectorMatch.Comparison);
                for (var i = 0; i < m_SelectorMatches.Count; ++i)
                {
                    var match = m_SelectorMatches[i];
                    m_MatchRecords.Add(new SelectorMatchRecord(in match));
                }
            }
            finally
            {
                m_SelectorMatches.Clear();

                // Popping only touches the slots that were pushed, so the reset doesn't need to clear the whole filter.
                for (var i = 0; i < pushedElementCount; ++i)
                    m_MatchingContext.ancestorFilter.PopElement();
                m_MatchingContext.Reset();
            }

            for (var i = 0; i < m_MatchRecords.Count; ++i)
            {
                var record = m_MatchRecords[i];
                var properties = record.complexSelector.rule.properties;
                for (var j = 0; j < properties.Length; ++j)
                {
                    var propertyId = properties[j].id;
                    if (propertyId == StylePropertyId.Custom)
                        continue;

                    if (!m_HasPropertySelector[(int)propertyId])
                    {
                        m_HasPropertySelector[(int)propertyId] = true;
                        m_SelectorPropertyIds.Add(propertyId);
                    }
                    m_PropertySelectors[(int)propertyId] = record;
                }
            }
        }

        private static int PushStyleSheetsAndAncestors(VisualElement element, StyleMatchingContext matchingContext)
        {
            var parent = element.hierarchy.parent;
            var pushedElementCount = parent != null ? PushStyleSheetsAndAncestors(parent, matchingContext) : 0;

            // The ancestor filter is populated for the Bloom filter detection to work.
            matchingContext.ancestorFilter.PushElement(element);

            var sheets = element.styleSheetList;
            if (sheets != null)
            {
                for (var i = 0; i < sheets.Count; ++i)
                    matchingContext.AddStyleSheet(sheets[i]);
            }

            return pushedElementCount + 1;
        }

        static ShortHandStylePropertyData ComputeStyleProperty(
            StylePropertyId id,
            string propertyName,
            in ResolutionContext context)
        {
            ref readonly var uxmlData = ref context.uxmlData[(int)id];
            return new ShortHandStylePropertyData
            {
                id = id,
                uxmlValue = new UxmlStyleProperty(uxmlData.inlineProperty),
                binding = uxmlData.binding,
                selector = uxmlData.selector,
            };
        }

        // The inline values only need to be compared when the element or the previous one has some.
        private IStyle GetInlineStyle(VisualElement element, bool propertiesMatchPreviousRefresh, out bool refreshInlineValues)
        {
            var inlineStyle = element.inlineStyleAccess;
            var hasInlineValues = inlineStyle != null && inlineStyle.hasInlineValues;
            refreshInlineValues = hasInlineValues || !m_InlineValuesUnset || !propertiesMatchPreviousRefresh;
            m_InlineValuesUnset = !hasInlineValues;
            return inlineStyle ?? k_EmptyInlineStyle;
        }

        private static int GetStylePropertyIdCount()
        {
            var maxId = 0;
            foreach (StylePropertyId id in Enum.GetValues(typeof(StylePropertyId)))
                maxId = Math.Max(maxId, (int)id);
            return maxId + 1;
        }

        private static StylePropertyId[][] CreateLonghandPropertyIds()
        {
            var longhandPropertyIds = new StylePropertyId[k_StylePropertyIdCount][];
            var longhandIds = new List<StylePropertyId>();
            for (var i = 0; i < longhandPropertyIds.Length; ++i)
            {
                var id = (StylePropertyId)i;
                if (!StyleDebug.IsShorthandProperty(id))
                {
                    longhandPropertyIds[i] = Array.Empty<StylePropertyId>();
                    continue;
                }

                longhandIds.Clear();
                StyleDebug.PopulateLonghandPropertyIds(id, longhandIds);
                longhandPropertyIds[i] = longhandIds.ToArray();
            }
            return longhandPropertyIds;
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void MarkAllPropertiesDirty() => Notify(new BindingId((string)null));

        void Notify(StylePropertyId id) => Notify(in k_PropertyBindingIds[(int)id]);

        void Notify(in BindingId propertyName)
        {
            m_Version += 1;
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(in propertyName));
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
            m_MatchRecords.Clear();
            Array.Clear(m_UxmlData, 0, m_UxmlData.Length);
            Array.Clear(m_PreviousUxmlData, 0, m_PreviousUxmlData.Length);
            m_UxmlDataIds.Clear();
            m_PreviousUxmlDataIds.Clear();
            m_PropertiesMatchPreviousRefresh = false;
        }
    }
}
