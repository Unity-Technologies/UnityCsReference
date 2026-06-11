// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

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
        internal static readonly EditorGUIUtility.SkinnedColor k_RowsSplitColor = new EditorGUIUtility.SkinnedColor(
            new Color(137f / 255f, 137f / 255f, 137f / 255f, 0.3f),
            new Color(36f / 255f, 36f / 255f, 36f / 255f, 0.5f));

        internal static readonly EditorGUIUtility.SkinnedColor k_ResizerColor = new EditorGUIUtility.SkinnedColor(
            new Color(137f / 255f, 137f / 255f, 137f / 255f, 0.3f),
            new Color(36f / 255f, 36f / 255f, 36f / 255f, 0.8f));

        internal static readonly EditorGUIUtility.SkinnedColor k_AlternatingRowColor = new EditorGUIUtility.SkinnedColor(
            new Color(0f, 0f, 0f, 0.07f),
            new Color(0f, 0f, 0f, 0.04f));

        internal static readonly EditorGUIUtility.SkinnedColor k_SelectionOutlineColor = new EditorGUIUtility.SkinnedColor(
            new Color(58f / 255f, 114f / 255f, 176f / 255f),
            new Color(44f / 255f, 93f / 255f, 135f / 255f));

        internal static readonly EditorGUIUtility.SkinnedColor k_SelectionOutlineColorInactive = new EditorGUIUtility.SkinnedColor(
            new Color(174f / 255f, 174f / 255f, 174f / 255f),
            new Color(77f / 255f, 77f / 255f, 77f / 255f));
    }

    internal static class Texts
    {
        internal static readonly string EmptyDictionaryLabel = L10n.Tr("Dictionary is empty");
        internal static readonly string ResetToDefaultsLabel = L10n.Tr("Reset to Defaults");
        internal static readonly string MultiEditUnsupportedMessage = L10n.Tr("Dictionary: Multi-object editing is not supported."); // Entries are sorted by key, so a given row may correspond to different entries across targets, so edits could affect unrelated entries
        internal static readonly string DuplicateMarkerTooltip = L10n.Tr("An element with the same key already exists, so this element will not be part of the Dictionary");
        internal static readonly string SingleItemCountLabel = L10n.Tr("1 item");
        internal static readonly string MultipleItemsCountFormat = L10n.Tr("{0} items");
        internal static readonly string DuplicatesFormat = L10n.Tr("{0} ignored");
        internal static readonly string DuplicatesHelpBoxSingle = L10n.Tr("1 duplicate key ignored. Ensure all keys are unique.");
        internal static readonly string DuplicatesHelpBoxFormat = L10n.Tr("{0} duplicate keys ignored. Ensure all keys are unique.");
        internal static readonly string SelectFirstDuplicateButtonLabel = L10n.Tr("Select Duplicate");

        internal static readonly string DefaultKeyLabel = "Key";
        internal static readonly string DefaultValueLabel = "Value";
        internal static readonly string ExpectedCurrentContainerMessage = "Expected a current IMGUIContainer, please report a bug with repro steps";

        internal static string GetItemCountText(int count)
        {
            return count == 1
                ? SingleItemCountLabel
                : string.Format(MultipleItemsCountFormat, count);
        }

        // Returned text includes the leading ", " separator so callers can
        // unconditionally append it after the item-count text without any
        // separator/comma bookkeeping at the call site.
        internal static string GetDuplicateCountText(int count)
        {
            return ", " + string.Format(DuplicatesFormat, count);
        }

        internal static string GetDuplicatesHelpBoxText(int count)
        {
            return count == 1
                ? DuplicatesHelpBoxSingle
                : string.Format(DuplicatesHelpBoxFormat, count);
        }

    }

    [Serializable]
    internal class DictionaryState
    {
        // Negative sentinel means the user has never dragged the resizer;
        // GetActiveKeyColumnFraction falls back to the attribute default in that case.
        public float keyColumnFractionSetByUser = -1f;
        public bool sortAscending = true;
    }

    // Disk-backed, persistent across editor sessions. Key: Hash128 of normalized path
    // ([\d+] → []) so list siblings share state (linked resizers). Eviction: only via
    // explicit RemoveState from the "Reset to Defaults" context menu.
    static readonly StateCache<DictionaryState> s_StateCache = new StateCache<DictionaryState>("Library/StateCache/DictionaryDrawer/");

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
    {
        var cached = s_StateCache.GetState(stateCacheKey);
        if (cached == null || cached.keyColumnFractionSetByUser <= 0f)
            return attributeFraction;
        return cached.keyColumnFractionSetByUser;
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
    }

    internal static bool HasCachedState(Hash128 stateCacheKey)
    {
        return s_StateCache.GetState(stateCacheKey) != null;
    }

    internal static void ClearCachedState(Hash128 stateCacheKey)
    {
        s_StateCache.RemoveState(stateCacheKey);
    }

    // We want a shared ui state for all dictionaries in lists/arrays, so the user do not have
    // to adjust each and every dictionary in the list/array.
    static readonly Regex s_ArrayIndexPattern = new Regex(@"\[\d+\]", RegexOptions.Compiled);

    internal static Hash128 ComputeStateCacheKey(string propertyPath)
    {
        var normalizedPath = s_ArrayIndexPattern.Replace(propertyPath, "[]");
        return Hash128.Compute(normalizedPath);
    }

    static Type[] GetDictionaryGenericArguments(FieldInfo fieldInfo)
    {
        return fieldInfo.FieldType.GetGenericArguments();
    }

    internal readonly struct SortedIndexMap
    {
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
            s_SortCount++;
            int n = arrayProperty.arraySize;
            if (n == 0)
                return Empty;

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

    // Returns true if the set actually changed, so callers can skip
    // UI refreshes (label text, gutter markers) when nothing differs.
    internal static bool TryRefreshDuplicateIndicesInto(SerializedProperty dictionaryProperty, HashSet<int> target)
    {
        var newIndices = dictionaryProperty.GetDictionaryDuplicateEntryIndices() ?? Array.Empty<int>();
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

    internal static void GetHeaderLabels(FieldInfo fieldInfo, out string keyLabel, out string valueLabel, out float keyColumnFraction)
    {
        keyLabel = Texts.DefaultKeyLabel;
        valueLabel = Texts.DefaultValueLabel;
        keyColumnFraction = 0.5f;

        var attr = fieldInfo?.GetCustomAttribute<DictionaryHeaderAttribute>();
        if (attr != null)
        {
            if (!string.IsNullOrEmpty(attr.keyColumnLabel))
                keyLabel = attr.keyColumnLabel;
            if (!string.IsNullOrEmpty(attr.valueColumnLabel))
                valueLabel = attr.valueColumnLabel;
            // Sanity-clamp only — keeps NaN/<0/>1 attribute values out of the cache.
            // The actual rendered width is enforced by GetKeyColumnPixelWidth, which
            // applies the pixel floor regardless of the stored fraction's exact value.
            var fraction = attr.keyColumnFraction;
            if (float.IsNaN(fraction))
                fraction = 0.5f;
            keyColumnFraction = Mathf.Clamp(fraction, 0.01f, 0.99f);
        }
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

    internal static int FindFirstDuplicateDisplayIndex(
        IEnumerable<int> duplicateArrayIndices,
        SortedIndexMap sortedIndices)
    {
        if (duplicateArrayIndices == null)
            return -1;

        int firstDisplayIndex = int.MaxValue;
        foreach (var arrayIndex in duplicateArrayIndices)
        {
            if (!sortedIndices.ContainsArrayIndex(arrayIndex))
                continue;
            int displayIndex = sortedIndices.ToDisplayIndex(arrayIndex);
            if (displayIndex < firstDisplayIndex)
                firstDisplayIndex = displayIndex;
        }

        return firstDisplayIndex == int.MaxValue ? -1 : firstDisplayIndex;
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
    }

    public static KeyMarkerKind GetMarkerKind(int arrayIndex, HashSet<int> duplicateEntryIndices)
    {
        return duplicateEntryIndices.Contains(arrayIndex) ? KeyMarkerKind.Duplicate : KeyMarkerKind.None;
    }

}

} // end of namespace
