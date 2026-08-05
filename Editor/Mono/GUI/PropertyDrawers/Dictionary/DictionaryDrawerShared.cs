// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assemblies;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Pool;

namespace UnityEditor
{
    // Centralised "is the editor in a state where deferred UI changes (e.g. a re-sort
    // that would rebuild rows) are safe to apply right now?" gate. Used by both the
    // UITK and IMGUI dictionary drawers to coalesce key-edit driven re-sorts; lives
    // here (next to DictionaryDrawer) because that's currently the only consumer, but
    // it has no Dictionary-specific knowledge and can be reused by any drawer that
    // needs the same gate.
    internal static class EditorInteractionMonitor
    {
        internal static bool IsReadyToApplyDeferredChanges(FocusController focusController)
        {
            bool isMouseCaptured = MouseCaptureController.IsMouseCaptured() || GUIUtility.hotControl != 0;
            bool isEditingTextField = IsTextEditingFocused(focusController) || EditorGUI.IsEditingTextField();

            return
                !isMouseCaptured &&
                !isEditingTextField &&
                !EditorApplication.isCompiling &&
                !CurveEditorWindow.visible &&
                !ColorPicker.visible &&
                !GradientPicker.visible &&
                !ObjectSelector.isVisible;
        }

        static bool IsTextEditingActive(FocusController focusController)
        {
            if (focusController == null)
                return false;

            if (focusController.GetLeafFocusedElement() is TextElement textElement)
                return textElement.hasFocus && textElement.selection.isSelectable && !textElement.edition.isReadOnly;

            return false;
        }

        static bool IsTextEditingFocused(FocusController focusController)
        {
            if (focusController != null)
                return IsTextEditingActive(focusController);

            foreach (var window in EditorWindow.activeEditorWindows)
            {
                if (IsTextEditingActive(window.rootVisualElement?.panel?.focusController))
                    return true;
            }
            return false;
        }
    }


// [CustomPropertyDrawer(typeof(Dictionary<,>))] lives on the partial-class fragment in DictionaryDrawerUITK.cs.
internal partial class DictionaryDrawer
{
    internal static class SharedStyles
    {
        [NoAutoStaticsCleanup] // skinned color constant; skin is session-fixed, safe to persist
        internal static readonly EditorGUIUtility.SkinnedColor k_RowsSplitColor = new EditorGUIUtility.SkinnedColor(
            new Color(137f / 255f, 137f / 255f, 137f / 255f, 0.3f),
            new Color(36f / 255f, 36f / 255f, 36f / 255f, 0.5f));

        [NoAutoStaticsCleanup] // skinned color constant; skin is session-fixed, safe to persist
        internal static readonly EditorGUIUtility.SkinnedColor k_ResizerColor = new EditorGUIUtility.SkinnedColor(
            new Color(137f / 255f, 137f / 255f, 137f / 255f, 0.3f),
            new Color(36f / 255f, 36f / 255f, 36f / 255f, 0.8f));

        [NoAutoStaticsCleanup] // skinned color constant; skin is session-fixed, safe to persist
        internal static readonly EditorGUIUtility.SkinnedColor k_AlternatingRowColor = new EditorGUIUtility.SkinnedColor(
            new Color(0f, 0f, 0f, 0.07f),
            new Color(0f, 0f, 0f, 0.04f));

        [NoAutoStaticsCleanup] // skinned color constant; skin is session-fixed, safe to persist
        internal static readonly EditorGUIUtility.SkinnedColor k_SelectionOutlineColor = new EditorGUIUtility.SkinnedColor(
            new Color(58f / 255f, 114f / 255f, 176f / 255f),
            new Color(44f / 255f, 93f / 255f, 135f / 255f));

        [NoAutoStaticsCleanup] // skinned color constant; skin is session-fixed, safe to persist
        internal static readonly EditorGUIUtility.SkinnedColor k_SelectionOutlineColorInactive = new EditorGUIUtility.SkinnedColor(
            new Color(174f / 255f, 174f / 255f, 174f / 255f),
            new Color(77f / 255f, 77f / 255f, 77f / 255f));
    }

    internal static class Texts
    {
        internal static readonly string EmptyDictionaryLabel = L10n.Tr("Dictionary is empty");
        // Foldout title for a nested dictionary, replacing its entry "value" field's displayName ("Value").
        internal static readonly string NestedDictionaryLabel = L10n.Tr("Dictionary");
        // Foldout titles for a dictionary value that is itself a collection, replacing the "Value" displayName.
        internal static readonly string NestedArrayLabel = L10n.Tr("Array");
        internal static readonly string NestedListLabel = L10n.Tr("List");
        internal static readonly string ResetToDefaultsLabel = L10n.Tr("Reset to Defaults");
        internal static readonly string MultiEditUnsupportedMessage = L10n.Tr("Dictionary: Multi-object editing is not supported."); // Entries are sorted by key, so a given row may correspond to different entries across targets, so edits could affect unrelated entries
        internal static readonly string DuplicateMarkerTooltip = L10n.Tr("An element with the same key already exists, so this element is excluded from the runtime dictionary.");
        internal static readonly string NullKeyMarkerTooltip = L10n.Tr("The key is null. A dictionary only stores entries with a valid (non-null) key, so this element is excluded from the runtime dictionary.");
        internal static readonly string SingleItemCountLabel = L10n.Tr("1 item");
        internal static readonly string MultipleItemsCountFormat = L10n.Tr("{0} items");
        internal static readonly string IgnoredFormat = L10n.Tr("{0} ignored");
        internal static readonly string DuplicatesHelpBoxSingle = L10n.Tr("1 duplicate key ignored. Ensure all keys are unique.");
        internal static readonly string DuplicatesHelpBoxFormat = L10n.Tr("{0} duplicate keys ignored. Ensure all keys are unique.");
        internal static readonly string NullKeysHelpBoxSingle = L10n.Tr("1 null key ignored. A dictionary key can't be null.");
        internal static readonly string NullKeysHelpBoxFormat = L10n.Tr("{0} null keys ignored. A dictionary key can't be null.");
        internal static readonly string MixedIgnoredHelpBoxFormat = L10n.Tr("{0} entries ignored. Ensure all keys are unique and non-null.");
        internal static readonly string SelectFirstIgnoredButtonLabel = L10n.Tr("Select");
        // Header context-menu labels for the three DictionaryLayout values, shown as a
        // radio group (the active layout is checked).
        internal static readonly string TwoColumnsLayoutLabel = L10n.Tr("Two Columns");
        internal static readonly string OneColumnWithValueFoldoutLayoutLabel = L10n.Tr("One Column With Value Foldout");
        internal static readonly string OneColumnWithValueVisibleLayoutLabel = L10n.Tr("One Column With Value Visible");
        internal static readonly string ShowSerializedOrderLabel = L10n.Tr("Show Serialized Order (Global)");
        internal static readonly string ShowingSerializedOrderInfoLabel = L10n.Tr("Showing serialized order");

        internal static readonly string DefaultKeyLabel = L10n.Tr("Key");
        internal static readonly string DefaultValueLabel = L10n.Tr("Value");
        // One-column mode stacks the key and value, so the single header column spans both.
        internal static readonly string OneColumnHeaderFormat = L10n.Tr("{0} & {1}");
        internal static readonly string ExpectedCurrentContainerMessage = "Expected a current IMGUIContainer, please report a bug with repro steps";

        internal static string GetOneColumnHeaderLabel(string keyLabel, string valueLabel) =>
            string.Format(OneColumnHeaderFormat, keyLabel, valueLabel);

        internal static string GetItemCountText(int count)
        {
            return count == 1
                ? SingleItemCountLabel
                : string.Format(MultipleItemsCountFormat, count);
        }

        // Returned text includes the leading ", " separator so callers can
        // unconditionally append it after the item-count text without any
        // separator/comma bookkeeping at the call site.
        internal static string GetIgnoredCountText(int ignoredCount)
        {
            return ", " + string.Format(IgnoredFormat, ignoredCount);
        }

        internal static string GetIgnoredHelpBoxText(int duplicateCount, int nullKeyCount)
        {
            bool hasDuplicates = duplicateCount > 0;
            bool hasNullKeys = nullKeyCount > 0;

            if (hasDuplicates && hasNullKeys)
                return string.Format(MixedIgnoredHelpBoxFormat, duplicateCount + nullKeyCount);

            if (hasNullKeys)
                return nullKeyCount == 1
                    ? NullKeysHelpBoxSingle
                    : string.Format(NullKeysHelpBoxFormat, nullKeyCount);

            return duplicateCount == 1
                ? DuplicatesHelpBoxSingle
                : string.Format(DuplicatesHelpBoxFormat, duplicateCount);
        }

    }

    [Serializable]
    internal class DictionaryState
    {
        // Negative sentinel means the user has never dragged the resizer;
        // GetActiveKeyColumnFraction falls back to the attribute default in that case.
        public float keyColumnFractionSetByUser = -1f;
        public bool sortAscending = true;
        // Single source of truth for the column layout. TwoColumns: key | value side by
        // side. OneColumnWithValueFoldout: key stacked over value, each value collapsible
        // behind a "Value" foldout via SerializedProperty.isExpanded. OneColumnWithValueVisible:
        // same stacking but every value renders inline with no per-row foldout.
        public DictionaryLayout layout = DictionaryLayout.TwoColumns;
        // Negative sentinel pattern, mirroring keyColumnFractionSetByUser: false means the
        // user has never picked a layout from the context menu, so GetActiveLayout falls
        // back to the attribute default. Set to true the moment the user toggles layout.
        public bool layoutSetByUser = false;
    }

    // Disk-backed, persistent across editor sessions. Key: Hash128 of normalized path
    // ([\d+] → []) so container siblings share state (linked views). Eviction: only via
    // explicit RemoveState from the "Reset to Defaults" context menu.
    [NoAutoStaticsCleanup] // disk-backed state cache, intentionally persistent across sessions; safe to persist
    static readonly StateCache<DictionaryState> s_StateCache = new StateCache<DictionaryState>("Library/StateCache/DictionaryDrawer/");

    [NoAutoStaticsCleanup] // session-scoped change counter; value is irrelevant across reloads, only that it changes
    static int s_StateVersion;

    internal static int StateVersion => s_StateVersion;

    internal const float k_MinColumnPixelWidth = 40f;
    internal const float k_MinDictionaryPixelWidth = 2f * k_MinColumnPixelWidth;
    internal const float k_VerticalSplitterWidth = 1f;
    internal const float k_ResizerPadding = 10f;

    // We don't want to sort while the user is interacting with the fields that
    // affect sorting so after we have detected a change we delay the actual sort until
    // the interaction have stopped.
    internal const int k_SortRetryDelayMs = 200;

    // Used by tests to assert that certain changes do not trigger a re-sort. Kept on the
    // shared so a single counter is shared regardless of the per-property DrawerInstance.
    [NoAutoStaticsCleanup] // test-only diagnostic counter, reset by tests; safe to persist
    internal static int s_SortCount;

    // True when totalWidth has hit the floor, i.e. dragging the resizer can't move the
    // split anywhere because both columns are already pinned to their minimum. Callers
    // use this to short-circuit cursor / hot-control changes that would otherwise still
    // fire even though ClampDraggedKeyColumnFraction below would discard the drag.
    internal static bool IsAtMinimumDictionaryWidth(float totalWidth)
        => totalWidth <= k_MinDictionaryPixelWidth;

    internal static float GetKeyColumnPixelWidth(float keyColumnFraction, float totalWidth)
    {
        if (totalWidth <= k_MinDictionaryPixelWidth)
            return k_MinColumnPixelWidth;
        return Mathf.Clamp(keyColumnFraction * totalWidth,
            k_MinColumnPixelWidth, totalWidth - k_MinColumnPixelWidth);
    }

    // Resolves both column widths against the effective dictionary width so callers
    // never need to repeat the floor-and-subtract dance. col0Width is rounded so it
    // lines up with pixel boundaries (header divider, row split line, cell rects);
    // col1Width is the remainder of the effective width, which means it can be
    // fractional but is always at least k_MinColumnPixelWidth.
    internal static void GetColumnPixelWidths(float keyColumnFraction, float totalWidth, out float col0Width, out float col1Width)
    {
        float effectiveTotal = Mathf.Max(totalWidth, k_MinDictionaryPixelWidth);
        col0Width = Mathf.Round(GetKeyColumnPixelWidth(keyColumnFraction, effectiveTotal));
        col1Width = effectiveTotal - col0Width;
    }

    internal static float ClampDraggedKeyColumnFraction(float keyColumnFraction, float totalWidth)
    {
        // Below k_MinDictionaryPixelWidth the bounds invert (min > 1, max < 0)
        // and the clamp would produce nonsensical fractions. Drags here are visually
        // no-ops anyway (the resize handle is pinned to the floor), so preserve the
        // existing fraction so a previously-stored intent survives a stray drag in a
        // temporarily-narrow inspector.
        if (totalWidth <= k_MinDictionaryPixelWidth)
            return keyColumnFraction;
        float minFraction = k_MinColumnPixelWidth / totalWidth;
        return Mathf.Clamp(keyColumnFraction, minFraction, 1f - minFraction);
    }

    internal static float GetActiveKeyColumnFraction(Hash128 stateCacheKey, float attributeFraction)
        => GetActiveKeyColumnFraction(s_StateCache.GetState(stateCacheKey), attributeFraction);

    // Overload for callers that already hold the cached state (e.g. a per-frame sync that reads
    // several fields), so they resolve all of them from a single GetState lookup.
    internal static float GetActiveKeyColumnFraction(DictionaryState state, float attributeFraction)
    {
        if (state == null || state.keyColumnFractionSetByUser <= 0f)
            return attributeFraction;
        return state.keyColumnFractionSetByUser;
    }

    // Layout follows the same default-vs-override rules as the key column fraction: the
    // attribute supplies the default, and the cached layout only wins once the user has
    // explicitly toggled it from the header context menu (layoutSetByUser). "Reset to
    // Defaults" removes the cache entry, so the attribute default returns.
    internal static DictionaryLayout GetActiveLayout(Hash128 stateCacheKey, DictionaryLayout attributeLayout)
        => GetActiveLayout(s_StateCache.GetState(stateCacheKey), attributeLayout);

    internal static DictionaryLayout GetActiveLayout(DictionaryState state, DictionaryLayout attributeLayout)
    {
        if (state == null || !state.layoutSetByUser)
            return attributeLayout;
        return state.layout;
    }

    static DictionaryState GetOrCreateCachedState(Hash128 stateCacheKey)
    {
        return s_StateCache.GetState(stateCacheKey) ?? new DictionaryState();
    }

    internal static DictionaryState GetCachedState(Hash128 stateCacheKey)
    {
        return s_StateCache.GetState(stateCacheKey);
    }

    internal static void UpdateCachedState(Hash128 stateCacheKey, Action<DictionaryState> updateState)
    {
        var state = GetOrCreateCachedState(stateCacheKey);
        updateState(state);
        s_StateCache.SetState(stateCacheKey, state);
        s_StateVersion++;
    }

    internal static bool HasCachedState(Hash128 stateCacheKey)
    {
        return s_StateCache.GetState(stateCacheKey) != null;
    }

    internal static void ClearCachedState(Hash128 stateCacheKey)
    {
        s_StateCache.RemoveState(stateCacheKey);
        s_StateVersion++;
    }

    internal const string k_SerializedOrderSessionKey = "DictionarySerializedOrderInUI";

    internal static bool ShowSerializedOrder => SessionState.GetBool(k_SerializedOrderSessionKey, false);

    internal static event Action SerializedOrderChanged;

    internal static void SetShowSerializedOrder(bool value)
    {
        if (ShowSerializedOrder == value)
            return;
        SessionState.SetBool(k_SerializedOrderSessionKey, value);
        SerializedOrderChanged?.Invoke();
        DrawerInstanceIMGUI.InvalidateAllSortOrders();
    }

    // We want a shared ui state for all dictionaries in lists/arrays, so the user do not have
    // to adjust each and every dictionary in the list/array.
    static readonly Regex s_ArrayIndexPattern = new Regex(@"\[\d+\]", RegexOptions.Compiled);

    internal static Hash128 ComputeStateCacheKey(string propertyPath)
    {
        var normalizedPath = s_ArrayIndexPattern.Replace(propertyPath, "[]");
        return Hash128.Compute(normalizedPath);
    }

    // Why siblings share state at all: every collection element at a given level intentionally
    // shares ONE persisted DictionaryState — their paths all normalize to the same
    // ComputeStateCacheKey. Two reasons:
    //   1. Avoid an explosion of persisted StateCache objects: without it, resizing a column (or
    //      changing sort/layout) inside a container with 100,000+ elements could write 100,000+
    //      per-element StateCache entries to disk. Collapsing the index means one shared entry per
    //      level regardless of element count.
    //   2. Consistency, matching how [DictionaryDisplayForType] configures appearance for ALL
    //      dictionaries of a given closed type: every element at the same nested level should look
    //      and behave the same, so the user adjusts sort/layout/column-width once rather than
    //      re-doing it across every one of many nested dictionaries.
    // Returns true when the property is a collection element (its path carries a numeric [\d+] index
    // token), so it has sibling dictionaries whose live views should be linked and kept in sync.
    internal static bool ShouldLinkViewStateWithSiblings(string propertyPath)
    {
        return s_ArrayIndexPattern.IsMatch(propertyPath);
    }

    static Type[] GetDictionaryGenericArguments(FieldInfo fieldInfo)
    {
        return fieldInfo.FieldType.GetGenericArguments();
    }

    internal readonly struct SortedIndexMap
    {
        [NoAutoStaticsCleanup] // immutable empty sentinel backed by Array.Empty; safe to persist
        public static readonly SortedIndexMap Empty =
            new SortedIndexMap(Array.Empty<int>(), Array.Empty<int>());

        public readonly int[] DisplayToArray;
        public readonly int[] ArrayToDisplay;

        public int Length => DisplayToArray.Length;
        public bool IsEmpty => DisplayToArray.Length == 0;

        SortedIndexMap(int[] displayToArray, int[] arrayToDisplay)
        {
            DisplayToArray = displayToArray;
            ArrayToDisplay = arrayToDisplay;
        }

        public static SortedIndexMap Build(SerializedProperty arrayProperty, bool ascending)
        {
            int n = arrayProperty.arraySize;
            if (n == 0)
                return Empty;

            if (DictionaryDrawer.ShowSerializedOrder)
            {
                var identity = new int[n];
                for (int i = 0; i < n; i++)
                    identity[i] = i;
                return new SortedIndexMap(identity, identity);
            }

            s_SortCount++;

            // The native sort flips its key comparison based on `ascending` but always
            // breaks ties on the original array index in ascending order. Reversing the
            // sorted indices in C# would also flip the tiebreaker, pushing a duplicate
            // above its original entry in descending mode.
            var displayToArray = arrayProperty.GetDictionarySortedIndices(n, ascending);

            var arrayToDisplay = new int[n];
            for (int i = 0; i < n; i++)
                arrayToDisplay[displayToArray[i]] = i;

            return new SortedIndexMap(displayToArray, arrayToDisplay);
        }

        public int ToArrayIndex(int displayIndex) => DisplayToArray[displayIndex];

        public int ToDisplayIndex(int arrayIndex) => ArrayToDisplay[arrayIndex];

        public bool ContainsArrayIndex(int arrayIndex) =>
            (uint)arrayIndex < (uint)ArrayToDisplay.Length;

        public bool DisplayOrderEquals(SortedIndexMap other) =>
            SortedOrderEquals(DisplayToArray, other.DisplayToArray);
    }

    // Cheap O(n) "did the keys actually change since the last time we sorted?"
    // signature. Both drawers gate their deferred reload on this so a value-only
    // edit (which can never change sort order or duplicate detection) skips the
    // O(n log n) sort + the row rebuild entirely. Always called on the inner
    // array property — the dictionary field property has the array as its single
    // child but the extension only walks the array.
    internal static ulong GetKeysContentHash(SerializedProperty arrayProperty)
        => arrayProperty.GetDictionaryKeysContentHash();

    internal static bool SortedOrderEquals(int[] a, int[] b)
    {
        if (a == null || b == null || a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }

    // Returns true if either set actually changed, so callers can skip
    // UI refreshes (label text, gutter markers) when nothing differs.
    internal static bool TryRefreshDuplicateAndNullKeyIndicesInto(
        SerializedProperty dictionaryProperty, HashSet<int> duplicateTarget, HashSet<int> nullKeyTarget)
    {
        var ignored = dictionaryProperty.GetDictionaryIgnoredEntries();
        bool duplicatesChanged = TryRefreshIndicesInto(ignored.duplicateEntryIndices, duplicateTarget);
        bool nullKeysChanged = TryRefreshIndicesInto(ignored.nullKeyEntryIndices, nullKeyTarget);
        return duplicatesChanged || nullKeysChanged;
    }

    static bool TryRefreshIndicesInto(int[] newIndices, HashSet<int> target)
    {
        newIndices ??= Array.Empty<int>();
        if (target.Count == newIndices.Length)
        {
            bool allMatch = true;
            for (int i = 0; i < newIndices.Length; i++)
            {
                if (!target.Contains(newIndices[i]))
                {
                    allMatch = false;
                    break;
                }
            }
            if (allMatch)
                return false;
        }

        target.Clear();
        for (int i = 0; i < newIndices.Length; i++)
            target.Add(newIndices[i]);
        return true;
    }

    // Resolves the key/value column header labels and default key-column fraction for a dictionary.
    // These come from the same [DictionaryDisplay] attribute that drives layout: a field-level
    // attribute on the directly-declared field, or an assembly-level attribute targeting the exact
    // closed Dictionary<K,V>. dictionaryType is the property's static closed type, which for a nested
    // inner dictionary differs from fieldInfo.FieldType (see GetFieldDisplayAttribute).
    internal static void GetHeaderLabels(FieldInfo fieldInfo, Type dictionaryType, out string keyLabel, out string valueLabel, out float keyColumnFraction)
    {
        keyLabel = Texts.DefaultKeyLabel;
        valueLabel = Texts.DefaultValueLabel;
        keyColumnFraction = 0.5f;

        var fieldAttr = GetFieldDisplayAttribute(fieldInfo, dictionaryType);
        if (fieldAttr != null)
        {
            ApplyHeaderLabels(fieldAttr, ref keyLabel, ref valueLabel, ref keyColumnFraction);
            return;
        }

        if (dictionaryType != null && GetAssemblyLayoutRegistry().TryGetValue(dictionaryType, out var entry))
            ApplyHeaderLabels(entry.attribute, ref keyLabel, ref valueLabel, ref keyColumnFraction);
    }

    static void ApplyHeaderLabels(DictionaryDisplayAttribute attr, ref string keyLabel, ref string valueLabel, ref float keyColumnFraction)
    {
        if (!string.IsNullOrEmpty(attr.keyLabel))
            keyLabel = attr.keyLabel;
        if (!string.IsNullOrEmpty(attr.valueLabel))
            valueLabel = attr.valueLabel;
        // Sanity-clamp only — keeps NaN/<0/>1 attribute values out of the cache.
        // The actual rendered width is enforced by GetKeyColumnPixelWidth, which
        // applies the pixel floor regardless of the stored fraction's exact value.
        var fraction = attr.keyColumnFraction;
        if (float.IsNaN(fraction))
            fraction = 0.5f;
        keyColumnFraction = Mathf.Clamp(fraction, 0.01f, 0.99f);
    }

    // Returns the field-level [DictionaryDisplay] that applies to THIS dictionary, or null.
    // A field attribute only governs the dictionary the field *directly* declares. For a nested
    // inner dictionary, fieldInfo resolves to the outer field (dict elements are not fields), so
    // its dictionaryType differs from fieldInfo.FieldType; in that case the field attribute belongs
    // to the outer dictionary and must not leak onto the inner one (which resolves via the
    // assembly-level registry instead). The assembly form (DictionaryDisplayForTypeAttribute) is
    // AttributeTargets.Assembly, so it can never appear on a field — no form check is needed here.
    static DictionaryDisplayAttribute GetFieldDisplayAttribute(FieldInfo fieldInfo, Type dictionaryType)
    {
        if (fieldInfo == null || dictionaryType != fieldInfo.FieldType)
            return null;

        return fieldInfo.GetCustomAttribute<DictionaryDisplayAttribute>();
    }

    // Single source of truth for the foldout title of a nested collection value, keyed on its type:
    // "Dictionary" / "Array" / "List" (add new collection kinds, e.g. HashSet, here). Returns null for
    // any non-collection type so the value cell stays label-less and the field keeps its "Value" name.
    //
    // The enclosing dictionary calls this for its value type and feeds the result to the value cell as a
    // plain label (see DictionaryView.m_ValueFieldLabel / DrawerInstanceIMGUI.valueCollectionLabel). The
    // value's own drawer then renders it: a nested dictionary reads it through PropertyField (UITK) or its
    // OnGUI label (IMGUI), an array/list through its built-in foldout title — so no drawer needs to detect
    // the nested-value case itself.
    internal static string GetNestedCollectionValueLabel(Type collectionType)
    {
        if (collectionType == null)
            return null;
        if (collectionType.IsArray)
            return Texts.NestedArrayLabel;
        if (collectionType.IsGenericType)
        {
            var definition = collectionType.GetGenericTypeDefinition();
            if (definition == typeof(Dictionary<,>))
                return Texts.NestedDictionaryLabel;
            if (definition == typeof(List<>))
                return Texts.NestedListLabel;
        }
        return null;
    }

    // Resolves the default layout for a dictionary, applying this precedence:
    //   1. A field-level [DictionaryDisplay] on the dictionary field (explicit per-field intent).
    //   2. An assembly-level [DictionaryDisplayForType(typeof(Dictionary<K,V>), ...)] matching the exact
    //      closed dictionary type — the only way to reach a nested dictionary or a type you don't own.
    //      Matches globally (applies wherever such a dictionary is used), but a rule is only admitted
    //      if its declaring assembly owns K or V — see GetAssemblyLayoutRegistry / DeclaresTargetType.
    //   3. TwoColumns.
    // The user's context-menu choice still wins at runtime; GetActiveLayout layers that on top of this.
    // dictionaryType is the closed Dictionary<K,V> for this specific field/value (from the property's
    // static type), which for nested dictionaries differs from fieldInfo.FieldType.
    internal static DictionaryLayout ResolveDefaultLayout(FieldInfo fieldInfo, Type dictionaryType)
    {
        var fieldAttr = GetFieldDisplayAttribute(fieldInfo, dictionaryType);
        if (fieldAttr != null)
            return fieldAttr.layout;

        if (dictionaryType != null && GetAssemblyLayoutRegistry().TryGetValue(dictionaryType, out var exact))
            return exact.attribute.layout;

        return DictionaryLayout.TwoColumns;
    }

    // True for a closed Dictionary<TKey,TValue>. Used by the registry builder to reject a
    // [DictionaryDisplayForType] whose target is not a closed dictionary: only exact Dictionary<K,V>
    // targets are supported. IsConstructedGenericType (not IsGenericType) is required so the open
    // typeof(Dictionary<,>) — which has no concrete K/V to match — is rejected rather than silently
    // registered under a key nothing ever resolves to.
    static bool IsExactDictionaryType(Type type)
        => type != null && type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);

    // One resolved assembly-level entry. Holds the originating attribute so layout, labels, and
    // fraction all read from the same source without a second reflection pass, plus the name of the
    // assembly that declared it so a duplicate warning can point back to the winning declaration.
    readonly struct AssemblyLayoutEntry
    {
        public readonly DictionaryDisplayAttribute attribute;
        public readonly string declaringAssemblyName;
        public AssemblyLayoutEntry(DictionaryDisplayAttribute attribute, string declaringAssemblyName)
        {
            this.attribute = attribute;
            this.declaringAssemblyName = declaringAssemblyName;
        }
    }

    // Lazily-built map of [assembly: DictionaryDisplayForType(targetType, ...)] across all loaded
    // assemblies. A target must be a closed Dictionary<K,V> (other shapes are ignored). Matching is
    // global — a target matches wherever such a dictionary is used — but an assembly may only declare
    // a rule for a target that involves a type it *defines* somewhere in the dictionary's shape — its
    // key, its value, or a type nested within either (see DeclaresTargetType). That ownership gate lets
    // an extension style dictionaries over its own types everywhere they appear, while stopping a rule
    // for a shape it has no stake in (e.g. Dictionary<int,int>) from hijacking every such dictionary in
    // a project. The key is the closed Dictionary<K,V> targetType.
    // Statics reset on domain reload, so the cache rebuilds automatically when assemblies change.
    [AutoStaticsCleanupOnCodeReload]
    static Dictionary<Type, AssemblyLayoutEntry> s_AssemblyLayoutRegistry;

    static Dictionary<Type, AssemblyLayoutEntry> GetAssemblyLayoutRegistry()
    {
        if (s_AssemblyLayoutRegistry != null)
            return s_AssemblyLayoutRegistry;

        var registry = new Dictionary<Type, AssemblyLayoutEntry>();

        // CurrentAssemblies.GetLoadedAssemblies() is the Unity-safe enumeration (AppDomain.GetAssemblies
        // can return already-unloaded assemblies — analyzer UAC0005). Sort by name so a target that is
        // legitimately owned by more than one assembly (K and V authored in different assemblies)
        // resolves deterministically (first by assembly name wins).
        var assemblies = new List<Assembly>(CurrentAssemblies.GetLoadedAssemblies());
        assemblies.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));

        foreach (var assembly in assemblies)
        {
            object[] attrs;
            try
            {
                attrs = assembly.GetCustomAttributes(typeof(DictionaryDisplayForTypeAttribute), false);
            }
            catch
            {
                // A dynamic or otherwise reflection-hostile assembly: skip it.
                continue;
            }

            foreach (DictionaryDisplayForTypeAttribute attr in attrs)
            {
                var target = attr.targetType;

                // Only exact closed Dictionary<K,V> targets are supported.
                if (!IsExactDictionaryType(target))
                {
                    Debug.LogWarning($"The DictionaryDisplayForType attribute targeting {FormatType(target)} in {assembly.GetName().Name} is ignored: it must target a Dictionary<K,V>, for example typeof(Dictionary<string, MyType>).");
                    continue;
                }

                // Ownership gate: reject a rule whose target involves no type defined in this assembly.
                if (!DeclaresTargetType(target, assembly))
                {
                    Debug.LogWarning(OwnershipRejectionMessage(target, assembly));
                    continue;
                }

                // Duplicate rules for the same target: keep the first and warn. Note this only sees
                // duplicates that survive to metadata — two byte-for-byte identical attributes in one
                // assembly (same ctor args and same named args) are folded into a single entry by the
                // C# compiler, so they never reach here and cannot be warned about at runtime. What we
                // do catch: same-assembly duplicates that differ in any setting, and any cross-assembly
                // duplicate (identical or not, since each assembly contributes its own metadata entry).
                if (registry.TryGetValue(target, out var existing))
                {
                    var assemblyName = assembly.GetName().Name;
                    var where = existing.declaringAssemblyName == assemblyName
                        ? $"is declared more than once in {assemblyName}"
                        : $"is declared more than once: kept the rule from {existing.declaringAssemblyName} and ignored the one from {assemblyName}";
                    Debug.LogWarning($"The DictionaryDisplayForType attribute targeting {FormatType(target)} {where}; the first registered rule is used.");
                    continue;
                }
                registry.Add(target, new AssemblyLayoutEntry(attr, assembly.GetName().Name));
            }
        }

        s_AssemblyLayoutRegistry = registry;
        return s_AssemblyLayoutRegistry;
    }

    // Gates which [DictionaryDisplayForType] rules an assembly may declare: you may only style a
    // Dictionary<K,V> that involves a type you authored *anywhere in its shape*, not merely as the
    // direct K or V. A closed generic's own Assembly is its definition's (Dictionary<,> and List<>
    // live in the framework), so ownership is carried by the type arguments, and we recurse through
    // element types and nested generic arguments to find an authored type at any depth. This is
    // intentionally broader than "the direct key or value is owned", because a dictionary that
    // contains your type is legitimately yours to style. For an assembly that defines only MyType:
    //   Dictionary<int, MyType>                       -> owned (MyType is the direct value)
    //   Dictionary<int, MyType[]>                     -> owned (an array reports its element's assembly)
    //   Dictionary<int, List<MyType>>                 -> owned (MyType nested in the value's generic args)
    //   Dictionary<int, Dictionary<string, MyType>>   -> owned (MyType nested in the inner dictionary)
    //   Dictionary<int, int>                          -> NOT owned (no authored type appears anywhere)
    // The gate's purpose is only to stop an assembly from hijacking a shape it has no stake in (the
    // last case); it is not meant to require ownership of the outermost key/value specifically.
    static bool DeclaresTargetType(Type type, Assembly declaringAssembly)
    {
        if (type == null)
            return false;
        if (type.Assembly == declaringAssembly)
            return true;
        if (type.HasElementType && DeclaresTargetType(type.GetElementType(), declaringAssembly))
            return true;
        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                if (DeclaresTargetType(arg, declaringAssembly))
                    return true;
            }
        }
        return false;
    }

    // Builds the warning for a Dictionary<K,V> rule rejected by the ownership gate. It names K and V
    // and states the rule in plain English: the fix is for the developer to declare the attribute in
    // whichever assembly defines a type used in the dictionary — they know which one that is, so we
    // don't try to guess it. A rejection means no authored type appears anywhere in the shape (not the
    // direct key or value, nor any type nested within them), so naming the top-level key and value is
    // enough to point at the problem. target is a closed Dictionary<,> (guaranteed by IsExactDictionaryType upstream).
    static string OwnershipRejectionMessage(Type target, Assembly declaringAssembly)
    {
        var args = target.GetGenericArguments();
        var declaringName = declaringAssembly.GetName().Name;

        return $"The DictionaryDisplayForType attribute targeting {FormatType(target)} in {declaringName} is ignored: it must "
            + $"target a dictionary whose key, value, or a type nested within either is defined in the declaring assembly, "
            + $"but neither key '{FormatType(args[0])}' nor value '{FormatType(args[1])}' involves a type defined in {declaringName}. "
            + $"Apply the attribute in the assembly that defines a key or value type used by the dictionary.";
    }

    // Renders a Type for a warning: angle-bracket generics (Dictionary`2[Int32,Foo] ->
    // Dictionary<Int32, Foo>), recursing through generic arguments. Arrays (and other
    // element types) are not generic, so format the element type and re-append the suffix
    // (e.g. List`1[] -> List<Int32>[]) rather than printing the raw runtime name.
    static string FormatType(Type type)
    {
        if (type == null)
            return "<null>";
        if (type.HasElementType)
        {
            var element = type.GetElementType();
            var runtimeName = type.Name;
            var suffix = runtimeName.StartsWith(element.Name) ? runtimeName.Substring(element.Name.Length) : string.Empty;
            return FormatType(element) + suffix;
        }
        if (!type.IsGenericType)
            return type.Name;

        var name = type.Name;
        var tick = name.IndexOf('`');
        if (tick >= 0)
            name = name.Substring(0, tick);
        var args = Array.ConvertAll(type.GetGenericArguments(), FormatType);
        return $"{name}<{string.Join(", ", args)}>";
    }

    internal static bool IsEditingMultipleObjects(SerializedProperty property)
        => property.serializedObject.isEditingMultipleObjects;

    // The returned keyProp / valueProp are intentionally NOT placed in unsafeMode.
    // unsafeMode short-circuits SerializedProperty.Verify(), which is also where
    // SyncSerializedObjectVersion() runs to lazily refresh a property's version
    // stamp against its parent SerializedObject. Inside a single OnGUI pass, the
    // key cell's PropertyField can mutate keyProp (e.g. the user picks an asset
    // in the object picker), which bumps the SerializedObject version. valueProp
    // sits at a higher byte offset in the same array element, so the version
    // bump leaves it out of sync.
    // The element property itself stays in unsafeMode purely as a perf shortcut
    // for the two FindPropertyRelative navigations below.
    internal static void GetKeyAndValueProperties(SerializedProperty element, out SerializedProperty keyProp, out SerializedProperty valueProp)
    {
        element.unsafeMode = true;
        keyProp = element.FindPropertyRelative(DictionarySerialization.KeyFieldName);
        valueProp = element.FindPropertyRelative(DictionarySerialization.ValueFieldName);
    }

    // Performs the dictionary "Add" mutation: either inserts a fresh element at
    // the end, or duplicates the currently selected (or last) entry and moves it
    // to the end so the resulting array index is stable across Prefab override
    // comparisons.
    // `singleSelectedDisplayIndex` is the display index of the lone selected
    // row, or any value < 0 when there is no single selection (no selection or
    // multi-selection); in that case the last sorted entry is duplicated.
    //
    // Returns the array index (not the display index) where the new entry ends up.
    // This is always equal to the pre-mutation array size (`lastIndex`), regardless
    // of whether the path went through DuplicateCommand+Move or the plain
    // InsertArrayElementAtIndex fallback.

    internal static int InsertOrDuplicateSelectedEntry(
        SerializedProperty arrayProperty,
        SortedIndexMap sortedIndices,
        int singleSelectedDisplayIndex)
    {
        var so = arrayProperty.serializedObject;
        so.Update();

        // Safety: sortedIndices is built from an earlier arrayProperty snapshot. After
        // so.Update() the array may have been mutated externally (another
        // Inspector window, a script, an undo); when its length no longer
        // matches the array's, sortedIndices's array indices can no longer be
        // trusted to map to current slots. We only take the duplicate-the-
        // selection path when both are in sync; otherwise fall back to a plain
        // append, since the caller rebuilds sortedIndices and resyncs the view
        // immediately after this returns anyway.
        int currentSize = arrayProperty.arraySize;
        int lastIndex = currentSize;
        bool sortedIndicesInSync = sortedIndices.Length == currentSize;

        if (!sortedIndicesInSync || currentSize == 0)
        {
            arrayProperty.InsertArrayElementAtIndex(lastIndex);
        }
        else
        {
            int arrayIndexToDuplicate = singleSelectedDisplayIndex >= 0
                ? sortedIndices.ToArrayIndex(singleSelectedDisplayIndex)
                : sortedIndices.ToArrayIndex(currentSize - 1);

            var elementToDuplicate = arrayProperty.GetArrayElementAtIndex(arrayIndexToDuplicate);
            if (elementToDuplicate.DuplicateCommand())
            {
                // The Duplicate command above will place the copy in the array after the elementToDuplicate
                // but we want to add it the end of the array for Prefab Overrides to be more stable (they are index based)
                int duplicateIndex = arrayIndexToDuplicate + 1;
                if (duplicateIndex < lastIndex)
                    arrayProperty.MoveArrayElement(duplicateIndex, lastIndex); // Ensuring stable Prefab override indices
            }
            else
            {
                arrayProperty.InsertArrayElementAtIndex(lastIndex);
            }
        }

        so.ApplyModifiedProperties();
        return lastIndex;
    }

    internal static int FindFirstIgnoredDisplayIndex(
        IEnumerable<int> duplicateArrayIndices,
        IEnumerable<int> nullKeyArrayIndices,
        SortedIndexMap sortedIndices)
    {
        int firstDisplayIndex = int.MaxValue;
        firstDisplayIndex = MinDisplayIndex(duplicateArrayIndices, sortedIndices, firstDisplayIndex);
        firstDisplayIndex = MinDisplayIndex(nullKeyArrayIndices, sortedIndices, firstDisplayIndex);
        return firstDisplayIndex == int.MaxValue ? -1 : firstDisplayIndex;
    }

    static int MinDisplayIndex(IEnumerable<int> arrayIndices, SortedIndexMap sortedIndices, int current)
    {
        if (arrayIndices == null)
            return current;

        foreach (var arrayIndex in arrayIndices)
        {
            if (!sortedIndices.ContainsArrayIndex(arrayIndex))
                continue;
            int displayIndex = sortedIndices.ToDisplayIndex(arrayIndex);
            if (displayIndex < current)
                current = displayIndex;
        }
        return current;
    }

    internal static bool RemoveEntryAtDisplayIndex(SerializedProperty arrayProperty, int index, SortedIndexMap sortedIndices)
    {
        using (ListPool<int>.Get(out var tempList))
        {
            tempList.Add(index);
            return RemoveEntriesAtDisplayIndices(arrayProperty, tempList, sortedIndices);
        }
    }

    // Performs the dictionary "Remove" mutation: maps the current selection from
    // display indices to array indices, deletes them in descending order so each
    // delete leaves earlier indices untouched, and commits the change. Returns
    // true if at least one entry was actually removed; false when there is no
    // selection, every selected display index falls outside `sortedIndices`
    // (e.g. a selection that survived a stale UI snapshot), or `sortedIndices`
    // is out of sync with the freshly-synced array (external mutation). Callers
    // can skip downstream UI refreshes when the return is false.
    internal static bool RemoveEntriesAtDisplayIndices(
        SerializedProperty arrayProperty,
        IEnumerable<int> selectedDisplayIndices,
        SortedIndexMap sortedIndices)
    {
        if (selectedDisplayIndices == null)
            return false;

        var so = arrayProperty.serializedObject;
        so.Update();

        // Same in-sync requirement as InsertOrDuplicateSelectedEntry: when
        // sortedIndices's length doesn't match the freshly-synced array, the
        // display indices the caller is handing us can no longer be mapped to
        // current array slots without risk of OOB or deleting the wrong entry.
        int currentSize = arrayProperty.arraySize;
        if (sortedIndices.Length != currentSize)
            return false;

        var arrayIndicesToRemove = new List<int>();
        foreach (var displayIndex in selectedDisplayIndices)
        {
            if (displayIndex >= 0 && displayIndex < currentSize)
                arrayIndicesToRemove.Add(sortedIndices.ToArrayIndex(displayIndex));
        }

        if (arrayIndicesToRemove.Count == 0)
            return false;

        // Sort descending so each delete leaves earlier indices untouched.
        arrayIndicesToRemove.Sort((a, b) => b.CompareTo(a));

        foreach (var idx in arrayIndicesToRemove)
            arrayProperty.DeleteArrayElementAtIndex(idx);
        so.ApplyModifiedProperties();

        return true;
    }

}

static class DictionaryKeyUtility
{
    public enum KeyMarkerKind
    {
        None,
        Duplicate,
        NullKey,
    }

    public static KeyMarkerKind GetMarkerKind(int arrayIndex, HashSet<int> duplicateEntryIndices, HashSet<int> nullKeyEntryIndices)
    {
        if (duplicateEntryIndices != null && duplicateEntryIndices.Contains(arrayIndex))
            return KeyMarkerKind.Duplicate;
        if (nullKeyEntryIndices != null && nullKeyEntryIndices.Contains(arrayIndex))
            return KeyMarkerKind.NullKey;
        return KeyMarkerKind.None;
    }

    public static string GetMarkerTooltip(KeyMarkerKind kind)
    {
        switch (kind)
        {
            case KeyMarkerKind.Duplicate: return DictionaryDrawer.Texts.DuplicateMarkerTooltip;
            case KeyMarkerKind.NullKey: return DictionaryDrawer.Texts.NullKeyMarkerTooltip;
            default: return null;
        }
    }

}

} // end of namespace
