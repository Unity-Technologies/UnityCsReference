// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

namespace UnityEditor
{
// [CustomPropertyDrawer(typeof(Dictionary<,>))]  lives on the partial-class fragment in DictionaryDrawerUITK.cs.
internal partial class DictionaryDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return DrawerInstanceIMGUI.GetPropertyHeight(this, property, label);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DrawerInstanceIMGUI.OnGUI(this, position, property, label);
    }

    /// <summary>
    /// Encapsulates all per-property IMGUI state, the keyed cache that maps a
    /// (property, container) pair to its instance, and every method/inner type that
    /// only the IMGUI backend needs. Mirrors the role of <see cref="DrawerInstance"/>
    /// for UITK so the partial <see cref="DictionaryDrawer"/> ends up with only the
    /// PropertyDrawer overrides delegating into here.
    /// </summary>
    sealed class DrawerInstanceIMGUI
    {
        // Per-(property, container) IMGUI state cache. In-memory, editor-process lifetime.
        // Key: (propertyPath, targetEntityId, imguiContainerId).
        // Eviction: automatic via DetachFromPanelEvent on the owning IMGUIContainer.
        // Entries may be either fully-initialized (TreeView etc. built by the full
        // constructor) or stubs allocated by short-circuit paths that only need
        // availableWidth (see GetOrCreate with isMultiEdit: true). GetOrCreate promotes a
        // stub to a full entry on demand while preserving any width already observed
        // during a prior short-circuit frame.
        static readonly Dictionary<PropertyCacheKey, DrawerInstanceIMGUI> s_Cache = new();

        static class Styles
        {
            // k_TreeViewHeight caps the rows area so the IMGUI drawer doesn't grow unbounded
            // with the dictionary's contents. Picked to roughly match the UITK DictionaryView,
            // whose outer collection-view inherits a ~520px cap from
            // .unity-property-field > .unity-collection-view; the IMGUI foldout +
            // column header eat the remaining ~40px, so 480px for the rows area
            // lands at the same visible overall height.
            public const float k_TreeViewHeight = 480f;

            public const float k_FooterHeight = 20f;
            public const float k_FooterButtonWidth = 25f;
            public const float k_FooterSpacing = 2f;
            public const float k_KeyLeftMargin = 13f;
            public const float k_RowVerticalPadding = 5f;
            public const float k_CellHorizontalPadding = 8f;
            public const float k_ValueLeftPadding = 16f;
            public const float k_CellLabelWidthFraction = 0.35f;
            public const float k_CellLabelMinWidth = 80f;
            public const float k_CellControlMinWidth = 40f;
            public const float k_DuplicateKeyIconLeftMargin = 4f;
            public const float k_DuplicateKeyIconTopOffset = 2f;
            public const float k_DuplicateKeyIconSize = 14f;
            public const float k_HandleWidth = 6f;
            public const float k_SortArrowSize = 12f;
            public const float k_SelectionBorderWidth = 3f;
            public const float k_VerticalScrollbarWidth = 16f;
            public const float k_DuplicatesHelpBoxTopMargin = 4f;
            public const float k_DuplicatesHelpBoxBottomMargin = 4f;

            public static readonly GUIStyle headerBackground = "RL Header";
            public static readonly GUIStyle boxBackground = "RL Background";
            public static readonly GUIStyle footerBackground = "RL Footer";
            public static readonly GUIStyle footerButton = "RL FooterButton";
            public static readonly GUIStyle columnLabel = "MultiColumnHeader";
            public static readonly GUIStyle columnLabelClipped = new GUIStyle(columnLabel) { clipping = TextClipping.Ellipsis };

            public static GUIContent iconPlus = EditorGUIUtility.TrIconContent("Toolbar Plus");
            public static GUIContent iconMinus = EditorGUIUtility.TrIconContent("Toolbar Minus");

            public static Texture2D sortAscIcon = EditorGUIUtility.LoadIconRequired("UIPackageResources/Images/scrollup_uielements.png");
            public static Texture2D sortDescIcon = EditorGUIUtility.LoadIconRequired("UIPackageResources/Images/scrolldown_uielements.png");
        }

        readonly struct PropertyCacheKey : IEquatable<PropertyCacheKey>
        {
            public readonly string propertyPath;
            public readonly EntityId targetEntityId;
            public readonly uint imguiContainerId;

            public PropertyCacheKey(string propertyPath, EntityId targetEntityId, uint imguiContainerId)
            {
                this.propertyPath = propertyPath;
                this.targetEntityId = targetEntityId;
                this.imguiContainerId = imguiContainerId;
            }

            public bool Equals(PropertyCacheKey other)
                => imguiContainerId == other.imguiContainerId
                && targetEntityId == other.targetEntityId
                && propertyPath == other.propertyPath;

            public override bool Equals(object obj) => obj is PropertyCacheKey other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(propertyPath, targetEntityId, imguiContainerId);
        }

        // Captured in the full constructor from the owning DictionaryDrawer so the
        // instance never needs a back-reference to the drawer object — matches how
        // UITK's DrawerInstance captures m_Drawer.fieldInfo once in Build().
        readonly FieldInfo m_FieldInfo;

        public readonly TreeViewState treeViewState;
        public readonly DictionaryHeader header;
        public readonly DictionaryTreeView treeView;
        public readonly SerializedProperty dictionaryProperty;
        public readonly SerializedProperty arrayProperty;
        public SortedIndexMap sortedIndices = SortedIndexMap.Empty;
        public readonly HashSet<int> duplicateEntryIndices = new HashSet<int>();
        // Count of items the TreeView is currently rendering. Equal to
        // sortedIndices.Length by invariant; may differ from arrayProperty.arraySize
        // between an external array mutation (Undo, script, prefab apply,
        // cross-inspector edit) and the next deferred PerformReload. Use this for
        // any UI-side count (foldout text, empty-state branch, height math) and as
        // the previous-snapshot baseline when comparing against the live arraySize
        // for change detection.
        public int displayedItemCount => sortedIndices.Length;
        // Snapshot of GetKeysContentHash(arrayProperty) taken at the moment
        // sortedIndices was last rebuilt. RunDeferredStructuralWork compares the
        // current hash against this to decide whether a TrackPropertyValue
        // notification reflects an actual key change (full reload required) or
        // a value-only edit (no work). Updated everywhere sortedIndices is
        // rebuilt — the full constructor, PerformReload, AddEntry,
        // RemoveSelectedEntries, and ResetToDefaults — so it is always paired
        // with sortedIndices.
        public ulong lastKnownKeysHash;
        public bool needsReload;
        public bool needsDuplicate;
        public readonly Type keyType;
        public readonly Type valueType;
        public readonly bool keyHasCustomDrawer;
        public readonly bool valueHasCustomDrawer;
        public bool variableRowHeight; // rows have non-uniform heights (enables per-row height tracking)
        public bool dynamicRowHeight; // row heights can change at runtime (expandable children present)
        public bool hasStaticInlineHeight; // inline compound type with fixed height (no expandable children)
        public bool needsHeightClassification; // deferred until first element exists to inspect
        public bool needsHeightRefresh; // a rendered row's measured height drifted from cached value
        public bool sortAscending = true;
        public readonly float attributeKeyFraction;
        public readonly Hash128 stateCacheKey;
        public float availableWidth;

        // Stubs allocated by GetOrCreate(..., isMultiEdit: true) leave treeView null;
        // a null treeView is the stable marker that distinguishes a stub from a fully-
        // initialized instance.
        public bool isFullyInitialized => treeView != null;

        // Deferred-work coordination. Cross-inspector key-edit notifications
        // (TrackPropertyValue) and the sort-toggle click in the header arrive
        // either between OnGUI passes or before the TreeView has drawn for the
        // current frame, so they are funnelled through
        // ScheduleDeferredStructuralWork → RunDeferredStructuralWork (driven by
        // EditorApplication.delayCall) to run strictly between OnGUI passes.
        // Mutating IMGUI-visible state mid-OnGUI shifts control IDs underneath
        // the input system and mis-routes keystrokes/mouse events to the wrong
        // cell.
        //
        // The footer Add/Remove buttons (and Cmd+D, processed via needsDuplicate
        // after the TreeView OnGUI pass) do not need this gating because they
        // run after the TreeView has already drawn for the current frame; they
        // call AddEntry / RemoveSelectedEntries synchronously and let the next
        // frame pick up the new state.
        //
        // deferredWorkScheduled coalesces multiple flag-sets within one frame into a
        // single delayCall registration; the flags themselves describe what kind of
        // work is pending. The pendingSortToggleSelectionArrayIndices snapshot
        // captures the selection that was valid at request time so the deferred
        // PerformSortToggle doesn't depend on selection that may have moved by
        // the time it runs.
        public bool deferredWorkScheduled;
        public bool needsDuplicateRefresh;
        public bool pendingSortToggle;
        public int[] pendingSortToggleSelectionArrayIndices;
        public bool needsTreeViewFocus;

        // Interaction check. When RunDeferredStructuralWork finds needsReload but
        // EditorInteractionMonitor.IsReadyToApplyDeferredChanges is false, we install a
        // single EditorApplication.update handler that re-checks the gate at a coarse
        // interval (k_InteractionCheckIntervalSeconds) instead of re-arming a
        // delayCall every editor tick — the latter would re-enter
        // RunDeferredStructuralWork at editor-update frequency (potentially hundreds of
        // Hz) the entire time the user is typing or holds a hot control. The handler is
        // captured here so StopInteractionCheck can unsubscribe; nextInteractionCheckTime
        // is the EditorApplication.timeSinceStartup value of the next allowed re-check.
        public EditorApplication.CallbackFunction interactionCheckHandler;
        public double nextInteractionCheckTime;

        // Captured in GetOrCreate so the deferred callback can request a repaint after
        // mutating. Held strongly: the instance is removed from s_Cache when this
        // container's DetachFromPanelEvent fires, so the ref doesn't outlive a normal
        // Inspector lifecycle.
        public IMGUIContainer imguiContainer;

        // True once TrackPropertyValue has been hooked up on imguiContainer for the
        // dictionary property. Survives stub-to-full promotion (a multi-edit stub has
        // no dictionaryProperty to track; promotion replaces the instance, so the flag
        // starts false on the new instance and registration runs once).
        public bool propertyTrackingRegistered;

        // Empty stub used by GetOrCreate(..., isMultiEdit: true): only availableWidth is
        // ever read. treeView stays null so isFullyInitialized returns false and the
        // stub is promoted on a multi-edit → single-edit transition.
        DrawerInstanceIMGUI()
        {
        }

        // Full constructor: builds dictionaryProperty / arrayProperty, resolves type
        // metadata, restores any persisted column fraction / sort order,
        // builds the TreeView, and primes the deferred-work caches.
        DrawerInstanceIMGUI(FieldInfo fieldInfo, SerializedProperty property)
        {
            m_FieldInfo = fieldInfo;

            dictionaryProperty = property.Copy();

            var genericArgs = GetDictionaryGenericArguments(m_FieldInfo);
            keyType = genericArgs[0];
            valueType = genericArgs[1];
            keyHasCustomDrawer = ScriptAttributeUtility.GetDrawerTypeForType(keyType, null) != null;
            valueHasCustomDrawer = ScriptAttributeUtility.GetDrawerTypeForType(valueType, null) != null;

            GetHeaderLabels(m_FieldInfo, out var keyLabel, out var valueLabel, out var keyFraction);
            attributeKeyFraction = keyFraction;
            stateCacheKey = ComputeStateCacheKey(property.propertyPath);

            float effectiveFraction = GetActiveKeyColumnFraction(stateCacheKey, keyFraction);
            var cachedState = s_StateCache.GetState(stateCacheKey);
            if (cachedState != null)
            {
                sortAscending = cachedState.sortAscending;
            }

            header = new DictionaryHeader(keyLabel, valueLabel, effectiveFraction, stateCacheKey);
            treeViewState = new TreeViewState();

            arrayProperty = GetArrayProperty(dictionaryProperty);
            sortedIndices = SortedIndexMap.Build(arrayProperty, sortAscending);
            lastKnownKeysHash = GetKeysContentHash(arrayProperty);
            TryRefreshDuplicateIndicesInto(dictionaryProperty, duplicateEntryIndices);

            treeView = new DictionaryTreeView(this);

            if (displayedItemCount > 0)
                ClassifyRowHeights();
            else
                needsHeightClassification = IsGenericInlineType(keyType, keyHasCustomDrawer)
                    || IsGenericInlineType(valueType, valueHasCustomDrawer);

            treeView.Reload();
        }

        // Returns a DrawerInstanceIMGUI for the property/container pair. When isMultiEdit
        // is true the short-circuit path only needs availableWidth, so any existing entry
        // (stub or full) is reused and a missing entry is filled with a fresh stub
        // (all reference fields null, isFullyInitialized == false). When isMultiEdit is
        // false the main drawing path is entered, so a cached stub is promoted to a
        // fully-initialized instance via the full constructor while preserving its
        // availableWidth — a multi-edit → single-edit transition therefore does not
        // reset the cached width.
        //
        // imguiContainer is resolved once at the OnGUI / GetPropertyHeight entry point
        // and passed down so the IMGUIContainer.GetCurrentIMGUIContainer() call site
        // stays visible there; this method assumes it is non-null.
        public static DrawerInstanceIMGUI GetOrCreate(DictionaryDrawer drawer, SerializedProperty property, IMGUIContainer imguiContainer, bool isMultiEdit)
        {
            var key = BuildPropertyCacheKey(property, imguiContainer);

            bool hadPriorEntry = s_Cache.TryGetValue(key, out var instance);
            if (hadPriorEntry && (isMultiEdit || instance.isFullyInitialized))
                return instance;

            float preservedWidth = instance?.availableWidth ?? 0f;
            instance = isMultiEdit
                ? new DrawerInstanceIMGUI()
                : new DrawerInstanceIMGUI(drawer.fieldInfo, property);
            instance.availableWidth = preservedWidth;
            instance.imguiContainer = imguiContainer;
            s_Cache[key] = instance;

            if (!hadPriorEntry)
                RegisterCacheEvictionOnDetach(imguiContainer, key);

            // Bind a property-change listener on the IMGUIContainer so any inspector
            // showing this dictionary re-sorts and refreshes its duplicate markers when
            // the SerializedObject is mutated elsewhere (e.g. a key edit in a second
            // inspector pinned to the same target). Stubs (multi-edit) have no
            // dictionaryProperty to track, so we only register on fully-initialized
            // instances; promotion from stub to full replaces the instance, so the flag
            // starts false on the new instance and registration runs once.
            if (!isMultiEdit
                && !instance.propertyTrackingRegistered
                && instance.dictionaryProperty != null)
            {
                instance.RegisterPropertyChangeTracking();
                instance.propertyTrackingRegistered = true;
            }

            return instance;
        }

        public static float GetPropertyHeight(DictionaryDrawer drawer, SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            // Invariant: PropertyDrawer's OnGUI / GetPropertyHeight only run while in an IMGUIContainer
            var imguiContainer = IMGUIContainer.GetCurrentIMGUIContainer();
            Debug.Assert(imguiContainer != null, Texts.ExpectedCurrentContainerMessage);

            // Reserved height for the multi-edit HelpBox depends on the real content width, which we
            // only know after OnGUI has run at least once. On the first frame we reserve just the
            // foldout; OnGUI will cache the width into instance.availableWidth and request a repaint
            // so the next frame reserves and draws the HelpBox with matching heights.
            if (IsEditingMultipleObjects(property))
            {
                var stub = GetOrCreate(drawer, property, imguiContainer, isMultiEdit: true);
                return stub.availableWidth > 0f
                    ? EditorGUIUtility.singleLineHeight + 2f
                        + CalcHelpBoxHeight(Texts.MultiEditUnsupportedMessage, MessageType.Info, stub.availableWidth)
                    : EditorGUIUtility.singleLineHeight;
            }

            var instance = GetOrCreate(drawer, property, imguiContainer, isMultiEdit: false);
            return instance.GetExpandedPropertyHeight();
        }

        public static void OnGUI(DictionaryDrawer drawer, Rect position, SerializedProperty property, GUIContent label)
        {
            var imguiContainer = IMGUIContainer.GetCurrentIMGUIContainer();
            Debug.Assert(imguiContainer != null, Texts.ExpectedCurrentContainerMessage);

            bool isMultiEdit = IsEditingMultipleObjects(property);
            var instance = GetOrCreate(drawer, property, imguiContainer, isMultiEdit);
            instance.OnGUI(position, property, label, isMultiEdit);
        }

        public bool HasFocus()
        {
            return treeView != null && treeView.HasFocus();
        }

        float GetExpandedPropertyHeight()
        {
            if (dynamicRowHeight && displayedItemCount > 0 && needsHeightRefresh)
            {
                treeView.RefreshCustomRowHeights();
                needsHeightRefresh = false;
            }

            float foldoutLine = EditorGUIUtility.singleLineHeight;
            float columnHeader = header.height;
            float rowsArea;

            if (displayedItemCount == 0)
            {
                rowsArea = EditorGUIUtility.singleLineHeight + 8f;
            }
            else
            {
                rowsArea = Mathf.Min(treeView.totalHeight, Styles.k_TreeViewHeight);
            }

            float footer = Styles.k_FooterHeight + Styles.k_FooterSpacing;
            float duplicatesBlock = CalcDuplicatesHelpBoxHeight(availableWidth);
            return foldoutLine + columnHeader + rowsArea + 1f + footer + duplicatesBlock;
        }

        float CalcDuplicatesHelpBoxHeight(float availWidth)
        {
            if (duplicateEntryIndices.Count == 0 || availWidth <= 0f)
                return 0f;

            string text = Texts.GetDuplicatesHelpBoxText(duplicateEntryIndices.Count);
            float helpBoxHeight = DrawerEditorGUI.GetHelpBoxWithButtonHeight(MessageType.Warning, text, availWidth);
            return Styles.k_DuplicatesHelpBoxTopMargin + helpBoxHeight  + Styles.k_DuplicatesHelpBoxBottomMargin;
        }

        void OnGUI(Rect position, SerializedProperty property, GUIContent label, bool isMultiEdit)
        {
            UpdateAvailableWidth(position);

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            if (isMultiEdit)
            {
                // Multi-edit isn't supported because the sort order can't be reconciled across
                // multiple targets; we render only the foldout + HelpBox. The HelpBox is drawn
                // outside the BeginProperty scope so it doesn't pick up the array-level bold
                // default font.
                EditorGUI.BeginProperty(foldoutRect, label, property);
                try
                {
                    property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
                }
                finally
                {
                    EditorGUI.EndProperty();
                }
                DrawMultiEditHelpBoxIfExpanded(position, foldoutRect, property);
                return;
            }

            // Pick up external array-size changes (Undo, script, prefab apply, etc.)
            // before drawing so the row-count text in the foldout reflects them this
            // frame. The actual rebuild is deferred to PerformReload between OnGUI
            // passes; until then we keep drawing with the previous sortedIndices /
            // displayedItemCount and TryGetEntryProperties guards row access so a
            // shrunk array can't throw.
            ScheduleReloadIfArrayChanged();

            // Always-visible foldout row (label + item-count / duplicates marker).
            EditorGUI.BeginProperty(foldoutRect, label, property);
            try
            {
                DrawFoldoutHeader(foldoutRect, property, label);
            }
            finally
            {
                EditorGUI.EndProperty();
            }

            if (!property.isExpanded)
            {
                // Drop any queued duplicate request — Cmd+D issued just before the
                // user collapsed the foldout would otherwise replay on next expand.
                needsDuplicate = false;
                return;
            }

            // Expanded body: Two column header, rows (or empty label), footer and help box
            DrawExpandedBody(position, foldoutRect.yMax, property);
        }

        static PropertyCacheKey BuildPropertyCacheKey(SerializedProperty property, IMGUIContainer imguiContainer)
        {
            return new PropertyCacheKey(
                property.propertyPath,
                property.serializedObject.targetObject.GetEntityId(),
                imguiContainer.controlid
                );
        }

        static void RegisterCacheEvictionOnDetach(IMGUIContainer imguiContainer, PropertyCacheKey key)
        {
            var capturedKey = key;
            imguiContainer.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                // Stop any in-flight interaction check first; its handler closes over the
                // instance and would otherwise keep it (and its captured SerializedProperty)
                // alive past the inspector that produced it.
                if (s_Cache.TryGetValue(capturedKey, out var cachedInstance))
                    cachedInstance.StopInteractionCheck();
                s_Cache.Remove(capturedKey);
            });
        }

        // The TrackPropertyValue callback fires from the panel's binding updater for
        // every container watching this property — including ones whose IMGUI input
        // flow never saw the change (a second inspector pinned to the same target),
        // and crucially also for value-only edits that can't change the sort order
        // or the duplicate set. We deliberately do NOT compute the keys-content
        // hash here: the callback can fire many times per frame (e.g. dragging a
        // slider in a value field) and ScheduleDeferredStructuralWork already
        // coalesces those bursts into a single deferred pass. The hash is computed
        // once per pass inside RunDeferredStructuralWork, which then decides
        // whether to commit to the full O(n log n) sort + treeView.Reload or to
        // skip the work entirely.
        void RegisterPropertyChangeTracking()
        {
            imguiContainer.TrackPropertyValue(dictionaryProperty, _ =>
            {
                if (!IsAlive())
                    return;

                needsReload = true;
                ScheduleDeferredStructuralWork();

                // Force this container to repaint so OnGUI runs and consumes the
                // refreshed sortedIndices / duplicateEntryIndices that
                // RunDeferredStructuralWork will produce. Without this, an unfocused
                // inspector wouldn't repaint until the user hovered or clicked it.
                imguiContainer?.MarkDirtyRepaint();
            });
        }

        // The instance and its captured properties can outlive the inspector that produced
        // them when a delayCall is in flight (e.g. user closes the inspector window or
        // triggers a domain reload between request and run). Detect those cases up front
        // so the deferred Perform* methods can assume valid inputs.
        bool IsAlive()
        {
            if (dictionaryProperty == null)
                return false;
            try
            {
                var so = dictionaryProperty.serializedObject;
                return so != null && so.targetObject != null;
            }
            catch
            {
                // SerializedObject can throw on access after disposal; treat as dead.
                return false;
            }
        }

        void ClearAllPendingFlags()
        {
            needsReload = false;
            needsDuplicateRefresh = false;
            pendingSortToggle = false;
            pendingSortToggleSelectionArrayIndices = null;
            StopInteractionCheck();
        }

        // Detect external array-size changes and (re-)arm the deferred reload.
        // needsReload may already be set by a TrackPropertyValue notification; the
        // OR keeps that pending request scheduled even on a frame where the size
        // happens to match the cached value.
        void ScheduleReloadIfArrayChanged()
        {
            int currentSize = arrayProperty.arraySize;
            if (currentSize == displayedItemCount && !needsReload)
                return;

            needsReload = true;
            ScheduleDeferredStructuralWork();
        }

        // Coalescing entry point for every structural mutation. Caller flips a pending flag
        // (or fills a snapshot) on the instance and then calls this; we register at most one
        // EditorApplication.delayCall per instance per "burst", regardless of how many flags
        // get set in the same OnGUI pass.
        void ScheduleDeferredStructuralWork()
        {
            if (deferredWorkScheduled)
                return;

            deferredWorkScheduled = true;
            EditorApplication.delayCall += RunDeferredStructuralWork;
        }

        // Installs a single EditorApplication.update handler that re-checks the
        // EditorInteractionMonitor gate every k_InteractionCheckIntervalSeconds. Only one
        // handler is registered per instance at a time; subsequent calls are no-ops while
        // the check is active (the existing handler already covers the new request because
        // needsReload remains set). The handler holds a strong ref to the instance through
        // the closure, so StopInteractionCheck must be called on container detach to avoid
        // keeping a dead inspector's instance alive.
        void StartInteractionCheck()
        {
            if (interactionCheckHandler != null)
                return;

            nextInteractionCheckTime = EditorApplication.timeSinceStartup + k_SortRetryDelayMs * 1000.0;
            interactionCheckHandler = RunInteractionCheck;
            EditorApplication.update += interactionCheckHandler;
        }

        void StopInteractionCheck()
        {
            if (interactionCheckHandler == null)
                return;

            EditorApplication.update -= interactionCheckHandler;
            interactionCheckHandler = null;
        }

        void RunInteractionCheck()
        {
            if (EditorApplication.timeSinceStartup < nextInteractionCheckTime)
                return;

            nextInteractionCheckTime = EditorApplication.timeSinceStartup + k_SortRetryDelayMs * 1000.0;

            if (!IsAlive())
            {
                StopInteractionCheck();
                ClearAllPendingFlags();
                return;
            }

            if (!EditorInteractionMonitor.IsReadyToApplyDeferredChanges(null))
                return;

            // Gate is open — hand off to the normal deferred path. RunDeferredStructuralWork
            // will re-call StopInteractionCheck once it actually performs the reload, but
            // we also stop here so a second check tick can't slip in before the delayCall runs.
            StopInteractionCheck();
            ScheduleDeferredStructuralWork();
        }

        // Runs strictly between OnGUI passes. Order matters: sort toggle runs first
        // because it rebuilds sortedIndices wholesale, which makes a subsequent gated
        // reload either a no-op or correctly idempotent; needsReload then
        // needsDuplicateRefresh follow in decreasing structural impact. The
        // interaction gate only applies to needsReload — SortToggle originates from
        // an explicit user click that is itself the interaction, so re-arming would
        // just spin.
        void RunDeferredStructuralWork()
        {
            // Reset the coalescing flag first so any new request that arrives while
            // we're running schedules a fresh delayCall instead of being dropped.
            deferredWorkScheduled = false;

            if (!IsAlive())
            {
                ClearAllPendingFlags();
                return;
            }

            bool needsRepaint = false;

            if (pendingSortToggle)
            {
                PerformSortToggle();
                pendingSortToggle = false;
                pendingSortToggleSelectionArrayIndices = null;
                needsRepaint = true;
            }

            if (needsReload)
            {
                int currentSize = arrayProperty.arraySize;
                bool sizeChanged = currentSize != displayedItemCount;
                bool keysChanged = sizeChanged || GetKeysContentHash(arrayProperty) != lastKnownKeysHash;

                if (!keysChanged)
                {
                    // Pure value-only edit. Duplicates are determined solely by key
                    // content, so a same-hash refresh would also be a no-op.
                    needsReload = false;
                    needsDuplicateRefresh = false;
                    StopInteractionCheck();
                }
                else if (EditorInteractionMonitor.IsReadyToApplyDeferredChanges(null))
                {
                    PerformReload();
                    needsReload = false;
                    // A full reload also recomputes duplicateEntryIndices, so a pending
                    // duplicate-only refresh is subsumed and can be cleared.
                    needsDuplicateRefresh = false;
                    needsRepaint = true;
                    StopInteractionCheck();
                }
                else
                {
                    // Interaction is in flight (text edit, hot control, picker open) so
                    // start a EditorApplication.update handler that checks when
                    // the user is done editing at a coarse interval and re-enters
                    // ScheduleDeferredStructuralWork once the gate opens. The duplicate
                    // refresh below still runs ungated so the per-row duplicate-key
                    // icons and the "X duplicates" count keep updating live as the
                    // user types.
                    needsDuplicateRefresh = true;
                    StartInteractionCheck();
                }
            }

            if (needsDuplicateRefresh)
            {
                needsDuplicateRefresh = false;
                if (TryRefreshDuplicateIndicesInto(dictionaryProperty, duplicateEntryIndices))
                    needsRepaint = true;
            }

            if (needsRepaint)
                imguiContainer?.MarkDirtyRepaint();
        }

        static bool IsGenericInlineType(Type type, bool hasCustomDrawer)
        {
            if (type == null || hasCustomDrawer)
                return false;
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum || typeof(UnityEngine.Object).IsAssignableFrom(type))
                return false;
            return true;
        }

        static bool HasExpandableChildren(SerializedProperty prop)
        {
            if (prop == null || !prop.isValid || prop.propertyType != SerializedPropertyType.Generic)
                return false;

            // Walk only visible children so [HideInInspector] members do not influence
            // the row-height classification (they are not rendered, so they must not
            // promote the row to dynamic-height either).
            var end = prop.GetEndProperty();
            var child = prop.Copy();
            child.unsafeMode = true;
            if (!child.NextVisible(true))
                return false;

            while (!SerializedProperty.EqualContents(child, end))
            {
                if (child.propertyType == SerializedPropertyType.Generic && child.hasVisibleChildren)
                    return true;
                if (!child.NextVisible(false))
                    break;
            }
            return false;
        }

        void ClassifyRowHeights()
        {
            needsHeightClassification = false;

            bool keyIsInline = IsGenericInlineType(keyType, keyHasCustomDrawer);
            bool valueIsInline = IsGenericInlineType(valueType, valueHasCustomDrawer);

            if (!keyIsInline && !valueIsInline)
                return;

            var element = arrayProperty.GetArrayElementAtIndex(0);
            GetKeyAndValueProperties(element, out var keyProp, out var valueProp);
            bool keyDynamic = keyIsInline && HasExpandableChildren(keyProp);
            bool valueDynamic = valueIsInline && HasExpandableChildren(valueProp);

            dynamicRowHeight = keyDynamic || valueDynamic;
            variableRowHeight = dynamicRowHeight;
            hasStaticInlineHeight = !dynamicRowHeight;

            if (hasStaticInlineHeight)
                treeView.ComputeFixedInlineRowHeight();
        }

        // Calculates the rendered height of an EditorGUI.HelpBox(rect, message, type) at a given width
        // without entering GUILayout. Matches the (GUI.Label + EditorStyles.helpBox) path used by
        // EditorGUI.HelpBox, so the returned height is pixel-accurate for the same input.
        static float CalcHelpBoxHeight(string message, MessageType messageType, float width)
        {
            var content = EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(messageType));
            return EditorStyles.helpBox.CalcHeight(content, width);
        }

        // Caches the inspector-provided width on the instance so GetPropertyHeight can
        // reserve space for width-dependent content (e.g. the multi-edit HelpBox) on the
        // next frame. Only Repaint events carry the final, drawable rect; Layout events
        // may pass a dummy width (e.g. 1 px) that would poison the cache.
        void UpdateAvailableWidth(Rect position)
        {
            if (Event.current.type != EventType.Repaint || position.width <= 1f)
                return;
            if (availableWidth == position.width)
                return;

            bool wasUnknown = availableWidth <= 0f;
            availableWidth = position.width;

            // First valid width sample: repaint so GetPropertyHeight can reserve space
            // on the next frame using the now-known width.
            if (wasUnknown)
                HandleUtility.Repaint();
        }

        // Multi-edit fallback: the dictionary drawer can't merge two TreeViews / sort
        // orders, so when more than one target is selected we render only a foldout with
        // an explanatory HelpBox in place of the rows. The HelpBox is drawn outside the
        // foldout's BeginProperty scope by the OnGUI caller so it doesn't pick up the
        // array-level bold default font. Width-dependent height is computed off the
        // cached availableWidth so GetPropertyHeight matches what we draw here.
        void DrawMultiEditHelpBoxIfExpanded(Rect position, Rect foldoutRect, SerializedProperty property)
        {
            if (!property.isExpanded || availableWidth <= 0f)
                return;

            float helpHeight = CalcHelpBoxHeight(Texts.MultiEditUnsupportedMessage, MessageType.Info, position.width);
            var helpRect = new Rect(position.x, foldoutRect.yMax + 2f, position.width, helpHeight);
            EditorGUI.HelpBox(helpRect, Texts.MultiEditUnsupportedMessage, MessageType.Info);
        }

        void DrawExpandedBody(Rect position, float startY, SerializedProperty property)
        {
            float headerH = header.height;
            float fullContentH = displayedItemCount == 0
                ? EditorGUIUtility.singleLineHeight + 8f
                : treeView.totalHeight;
            float contentH = Mathf.Min(fullContentH, Styles.k_TreeViewHeight);

            const float borderBottom = 1f;
            float y = startY;

            // Backgrounds first so the column header / rows draw on top.
            if (Event.current.type == EventType.Repaint)
            {
                var headerRect = new Rect(position.x, y, position.width, headerH);
                var contentRect = new Rect(position.x, y + headerH, position.width, contentH + borderBottom);
                Styles.headerBackground.Draw(headerRect, false, false, false, false);
                Styles.boxBackground.Draw(contentRect, false, false, false, false);
            }

            // Column header (key / value labels, sort arrow, resize handle).
            var headerContentRect = new Rect(position.x + 1, y, position.width - 2, headerH);
            header.OnGUI(headerContentRect, this);
            y += headerH;

            SetTreeViewFocusIfRequested();

            // Rows (or "empty dictionary" placeholder when there are no entries).
            if (displayedItemCount == 0)
            {
                var emptyRect = new Rect(position.x + 18f, y, position.width - 18f, EditorGUIUtility.singleLineHeight + 8f);
                EditorGUI.LabelField(emptyRect, Texts.EmptyDictionaryLabel);
                y += emptyRect.height;
            }
            else
            {
                var treeRect = new Rect(position.x + 1, y, position.width - 2, contentH);

                SetTreeViewFocusOnMouseEvents(treeRect);

                treeView.OnGUI(treeRect);
                y += contentH;
            }
            y += borderBottom;

            // Cmd+D / context-menu duplicate is queued during the TreeView OnGUI and
            // flushed here, after the rows have already drawn for this frame so we
            // don't shift control IDs underneath the in-flight event.
            if (needsDuplicate)
            {
                needsDuplicate = false;
                AddEntry();
            }

            // Footer (+ / − buttons) sits below the box, separated by k_FooterSpacing.
            var footerRect = new Rect(position.x, y + Styles.k_FooterSpacing - 1f, position.width, Styles.k_FooterHeight);
            DrawFooter(footerRect, property);

            DrawDuplicatesHelpBox(position, footerRect.yMax);
        }

        void DrawDuplicatesHelpBox(Rect position, float startY)
        {
            if (duplicateEntryIndices.Count == 0)
                return;

            string text = Texts.GetDuplicatesHelpBoxText(duplicateEntryIndices.Count);
            float helpBoxHeight = DrawerEditorGUI.GetHelpBoxWithButtonHeight(MessageType.Warning, text, position.width);
            float helpBoxY = startY + Styles.k_DuplicatesHelpBoxTopMargin;
            var helpBoxRect = new Rect(position.x, helpBoxY, position.width, helpBoxHeight);

            if (DrawerEditorGUI.HelpBoxWithButton(helpBoxRect, MessageType.Warning, text, Texts.SelectFirstDuplicateButtonLabel))
                SelectFirstDuplicate();
        }

        void SelectFirstDuplicate()
        {
            if (treeView == null)
                return;

            int firstDisplayIndex = FindFirstDuplicateDisplayIndex(duplicateEntryIndices, sortedIndices);
            if (firstDisplayIndex < 0)
                return;

            treeView.SetSelection(new[] { firstDisplayIndex }, TreeViewSelectionOptions.RevealAndFrame);
            treeView.SetFocus();
        }

        void DrawFoldoutHeader(Rect rect, SerializedProperty property, GUIContent label)
        {
            int duplicateCount = duplicateEntryIndices?.Count ?? 0;
            int itemCount = displayedItemCount;

            string countText = Texts.GetItemCountText(itemCount);
            string duplicateText = duplicateCount > 0
                ? Texts.GetDuplicateCountText(duplicateCount)
                : "";
            string infoText = $"{countText}{duplicateText}";

            var infoSize = EditorStyles.miniLabel.CalcSize(new GUIContent(infoText));
            var infoRect = new Rect(rect.xMax - infoSize.x - 4f, rect.y, infoSize.x, rect.height);

            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.LabelField(infoRect, infoText, EditorStyles.miniLabel);
        }

        void DrawFooter(Rect rect, SerializedProperty property)
        {
            float rightEdge = rect.xMax - 10f;
            float leftEdge = rightEdge - 8f - Styles.k_FooterButtonWidth * 2;
            var bgRect = new Rect(leftEdge, rect.y, rightEdge - leftEdge, rect.height);
            var addRect = new Rect(leftEdge + 4, rect.y, Styles.k_FooterButtonWidth, 16);
            var removeRect = new Rect(rightEdge - 29, rect.y, Styles.k_FooterButtonWidth, 16);

            if (Event.current.type == EventType.Repaint)
                Styles.footerBackground.Draw(bgRect, false, false, false, false);

            if (GUI.Button(addRect, Styles.iconPlus, Styles.footerButton))
            {
                AddEntry();
            }

            var selection = treeView.GetSelection();
            using (new EditorGUI.DisabledScope(selection.Count == 0))
            {
                if (GUI.Button(removeRect, Styles.iconMinus, Styles.footerButton))
                {
                    RemoveSelectedEntries();
                }
            }
        }

        void SetTreeViewFocusOnMouseEvents(Rect treeRect)
        {
            // TreeView focus grab on MouseDown / ScrollWheel. Must be called before the
            // TreeView's OnGUI().
            // Two reasons stack:
            //   - Visual: the active (blue) selection outline only shows when the
            //     treeview itself owns keyboard focus, mirroring how row clicks
            //     already grab focus via HandleRowSelectionClick.
            //   - Correctness: scrolling (wheel, scrollbar drag, repeat-button) culls
            //     rows that leave the visible area, and culled controls don't allocate
            //     their controlIDs — that shifts the IDs of the rows that remain and
            //     reroutes keyboard focus to an unrelated cell. Releasing whatever
            //     currently owns keyboard focus (typically a text field in a row)
            //     before the scroll runs avoids that. SetFocus also clears
            //     EditorGUIUtility.editingTextField, so a text edit ends cleanly.
            //
            // OnOptimizedInspectorGUI(Rect contentRect) clears GUIUtility.keyboardControl
            // = 0 even when we have treeview focus, so the !HasFocus() check is needed
            // here too — without it SetFocus would no-op when the user is just panning
            // over the treeview and we want the blue outline back.
            if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.ScrollWheel)
                && treeRect.Contains(Event.current.mousePosition)
                && !treeView.HasFocus())
                treeView.SetFocus();
        }

        void SetTreeViewFocusIfRequested()
        {
            if (!needsTreeViewFocus)
                return;
            needsTreeViewFocus = false;
            treeView.SetFocus();
        }

        static SerializedProperty GetArrayProperty(SerializedProperty dictionaryProperty)
        {
            var arrayProp = dictionaryProperty.Copy();
            arrayProp.Next(true);
            return arrayProp;
        }

        // Add and Remove run synchronously from the footer button click handlers
        // (and from the Cmd+D path via needsDuplicate, processed after the
        // TreeView OnGUI). Both call sites are reached after the TreeView has
        // already drawn for the current frame, so mutating sortedIndices /
        // the underlying array here cannot shift control IDs underneath an
        // in-flight event for the rows that were just rendered. The next frame
        // picks up the new state via the regular GetPropertyHeight → OnGUI cycle.
        // Mirrors the immediate model used by the UITK drawer's OnAddClicked /
        // OnRemoveClicked.
        void AddEntry()
        {
            var selection = treeView.GetSelection();
            int singleSelectedDisplayIndex = selection.Count == 1 ? selection[0] : -1;
            int lastIndex = InsertOrDuplicateSelectedEntry(arrayProperty, sortedIndices, singleSelectedDisplayIndex);

            sortedIndices = SortedIndexMap.Build(arrayProperty, sortAscending);
            lastKnownKeysHash = GetKeysContentHash(arrayProperty);
            TryRefreshDuplicateIndicesInto(dictionaryProperty, duplicateEntryIndices);
            if (needsHeightClassification)
                ClassifyRowHeights();
            treeView.Reload();

            int newDisplayIndex = sortedIndices.ToDisplayIndex(lastIndex);
            treeView.SetSelection(new[] { newDisplayIndex }, TreeViewSelectionOptions.RevealAndFrame);
            treeView.SetFocus();
        }

        void RemoveSelectedEntries()
        {
            var selection = treeView.GetSelection();
            int newSelectedDisplayIndex = selection.Count == 1 ? selection[0] : -1;

            bool removed = RemoveEntriesAtDisplayIndices(
                arrayProperty, selection, sortedIndices);
            if (!removed)
                return;

            sortedIndices = SortedIndexMap.Build(arrayProperty, sortAscending);
            lastKnownKeysHash = GetKeysContentHash(arrayProperty);
            TryRefreshDuplicateIndicesInto(dictionaryProperty, duplicateEntryIndices);
            treeView.Reload();

            if (displayedItemCount <= 0 || newSelectedDisplayIndex < 0)
            {
                treeView.SetSelection(Array.Empty<int>());
            }
            else
            {
                int clampedSelection = Mathf.Min(newSelectedDisplayIndex, displayedItemCount - 1);
                treeView.SetSelection(new[] { clampedSelection }, TreeViewSelectionOptions.RevealAndFrame);
                needsTreeViewFocus = true;
            }
        }

        // Snapshots the selection-as-array-indices now so the deferred PerformSortToggle
        // can restore the selection by array index after the displayIndex mapping flips.
        void RequestSortToggle()
        {
            var prevSelection = treeView.GetSelection();
            pendingSortToggleSelectionArrayIndices = MapSelectionToArrayIndices(prevSelection, sortedIndices);
            pendingSortToggle = true;
            ScheduleDeferredStructuralWork();
        }

        void PerformSortToggle()
        {
            sortAscending = !sortAscending;

            // Rebuild from the array rather than reversing the cached map: the native
            // sort keeps the array-index tiebreaker ascending in both directions so
            // duplicates always render below their original; a plain Reverse would
            // invert the tiebreaker and put the duplicate on top in descending mode.
            sortedIndices = SortedIndexMap.Build(arrayProperty, sortAscending);
            header.PersistSortOrder(sortAscending);
            treeView.Reload();
            RevealSelectionAfterSort(pendingSortToggleSelectionArrayIndices ?? Array.Empty<int>());
            needsTreeViewFocus = true;
        }

        // This function does the heavy work of sorting + rebuilding the treeview.
        // It is called outside OnGUI in an update code path to not mess with controlID
        // allocations.
        void PerformReload()
        {
            var prevSelection = treeView.GetSelection();
            int[] selectedArrayIndices = MapSelectionToArrayIndices(prevSelection, sortedIndices);

            int currentSize = arrayProperty.arraySize;
            sortedIndices = SortedIndexMap.Build(arrayProperty, sortAscending);
            lastKnownKeysHash = GetKeysContentHash(arrayProperty);
            TryRefreshDuplicateIndicesInto(dictionaryProperty, duplicateEntryIndices);
            if (needsHeightClassification && currentSize > 0)
                ClassifyRowHeights();
            treeView.Reload();
            RevealSelectionAfterSort(selectedArrayIndices);

            // New sorting: we need to remove keyboard focus from any property field
            // that caused the sorting. Otherwise, it can end up on an unrelated field
            // due to controlId allocation order. Also, we want to show the focused blue
            // selection outline of the treeview to show the user where the changed row
            // went to. Delay the focus change to a OnGUI code path for it to be picked up.
            needsTreeViewFocus = true;
        }

        void RevealSelectionAfterSort(int[] selectedArrayIndices)
        {
            if (selectedArrayIndices.Length == 0)
                return;

            var newSelection = new List<int>(selectedArrayIndices.Length);
            foreach (var arrayIdx in selectedArrayIndices)
            {
                // ContainsArrayIndex bounds-checks against the reverse map, which
                // lines up with displayedItemCount because sortedIndices is always
                // a full permutation of [0, displayedItemCount).
                if (sortedIndices.ContainsArrayIndex(arrayIdx))
                    newSelection.Add(sortedIndices.ToDisplayIndex(arrayIdx));
            }
            if (newSelection.Count > 0)
                treeView.SetSelection(newSelection, TreeViewSelectionOptions.RevealAndFrame);
        }

        void ResetToDefaults()
        {
            ClearCachedState(stateCacheKey);

            sortAscending = true;
            header.ResetToDefaultFraction(attributeKeyFraction);

            sortedIndices = SortedIndexMap.Build(arrayProperty, sortAscending);
            lastKnownKeysHash = GetKeysContentHash(arrayProperty);
            treeView.Reload();
        }

        static int[] MapSelectionToArrayIndices(IList<int> displayIndices, SortedIndexMap sortedIndices)
        {
            var result = new List<int>(displayIndices.Count);
            foreach (var displayIdx in displayIndices)
            {
                if (displayIdx >= 0 && displayIdx < sortedIndices.Length)
                    result.Add(sortedIndices.ToArrayIndex(displayIdx));
            }
            return result.ToArray();
        }

        public class DictionaryHeader
        {
            float m_Column1Fraction;
            readonly int m_ResizeHandleControlID;
            readonly int m_SortToggleControlID;
            readonly Hash128 m_StateCacheKey;
            readonly GUIContent m_KeyLabel;
            readonly GUIContent m_ValueLabel;

            public float height => EditorGUIUtility.singleLineHeight + 2f;
            public bool HasCachedState => DictionaryDrawer.HasCachedState(m_StateCacheKey);

            public float column1Fraction
            {
                get => m_Column1Fraction;
                set => m_Column1Fraction = value;
            }

            public DictionaryHeader(string keyLabel, string valueLabel, float initialFraction, Hash128 stateCacheKey)
            {
                m_KeyLabel = new GUIContent(keyLabel);
                m_ValueLabel = new GUIContent(valueLabel);
                m_Column1Fraction = initialFraction;
                m_ResizeHandleControlID = GUIUtility.GetPermanentControlID();
                m_SortToggleControlID = GUIUtility.GetPermanentControlID();
                m_StateCacheKey = stateCacheKey;
            }

            public void OnGUI(Rect rect, DrawerInstanceIMGUI instance)
            {
                var evt = Event.current;
                if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition) && instance.treeView != null)
                {
                    instance.treeView.SetFocus();
                }

                // Use the effective dictionary width (floored at k_MinDictionaryPixelWidth)
                // so col1Width can never collapse to zero/negative. The header overflows the
                // inspector to the right when rect.width is below the floor, matching the
                // row drawing path which uses the same effective width.
                GetColumnPixelWidths(m_Column1Fraction, rect.width, out var col0Width, out var col1Width);
                bool isAtMinimumWidth = IsAtMinimumDictionaryWidth(rect.width);

                float keyLabelInset = Styles.k_KeyLeftMargin;
                var col0ButtonRect = new Rect(rect.x, rect.y, col0Width - Styles.k_HandleWidth * 0.5f, rect.height);
                var label0Rect = new Rect(rect.x + keyLabelInset, rect.y, col0Width - keyLabelInset, rect.height);
                var label1Rect = new Rect(rect.x + col0Width + Styles.k_ValueLeftPadding - Styles.k_CellHorizontalPadding, rect.y, col1Width - Styles.k_ValueLeftPadding + Styles.k_CellHorizontalPadding, rect.height);

                float arrowX = rect.x + col0Width - Styles.k_HandleWidth * 0.5f - Styles.k_SortArrowSize - 5f;
                label0Rect.width = arrowX - label0Rect.x - 2f;
                GUI.Label(label0Rect, m_KeyLabel, Styles.columnLabelClipped);

                var arrowIcon = instance.sortAscending ? Styles.sortAscIcon : Styles.sortDescIcon;
                if (arrowIcon != null)
                {
                    var arrowRect = new Rect(arrowX, label0Rect.y + (label0Rect.height - Styles.k_SortArrowSize) * 0.5f, Styles.k_SortArrowSize, Styles.k_SortArrowSize);
                    GUI.DrawTexture(arrowRect, arrowIcon);
                }

                GUI.Label(label1Rect, m_ValueLabel, Styles.columnLabel);

                float dividerX = rect.x + col0Width;
                var dividerRect = new Rect(dividerX, rect.y, k_VerticalSplitterWidth, rect.height);
                EditorGUI.DrawRect(dividerRect, SharedStyles.k_ResizerColor);

                var handleRect = new Rect(dividerX - Styles.k_HandleWidth * 0.5f, rect.y, Styles.k_HandleWidth, rect.height);
                // Skip cursor + resize hit-testing entirely when at the floor: the split
                // can't move (both columns are pinned to their minimum), so a resize cursor
                // and consumed mouse events would just lie to the user.
                if (!isAtMinimumWidth)
                {
                    EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.SplitResizeLeftRight);
                    HandleResize(rect, rect.width, handleRect);
                }
                HandleContextMenu(rect, instance);
                HandleSortToggle(col0ButtonRect, instance);
            }

            public void GetColumnRects(Rect rowRect, out Rect col0Rect, out Rect col1Rect)
            {
                GetColumnPixelWidths(m_Column1Fraction, rowRect.width, out var col0Width, out var col1Width);
                col0Rect = new Rect(rowRect.x, rowRect.y, col0Width, rowRect.height);
                col1Rect = new Rect(rowRect.x + col0Width, rowRect.y, col1Width, rowRect.height);
            }

            void HandleResize(Rect headerRect, float totalWidth, Rect handleRect)
            {
                var evt = Event.current;
                switch (evt.GetTypeForControl(m_ResizeHandleControlID))
                {
                    case EventType.MouseDown:
                        if (evt.button == 0 && handleRect.Contains(evt.mousePosition))
                        {
                            GUIUtility.hotControl = m_ResizeHandleControlID;
                            evt.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == m_ResizeHandleControlID)
                        {
                            // The drag's pixel position is converted to a fraction and clipped
                            // by the floor at the current totalWidth. Inspector resize after the
                            // drag scales the fraction proportionally and only re-snaps to the
                            // floor when the new totalWidth would force a column under it.
                            float newCol0Fraction = (evt.mousePosition.x - headerRect.x) / totalWidth;
                            m_Column1Fraction = ClampDraggedKeyColumnFraction(newCol0Fraction, totalWidth);
                            evt.Use();
                        }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == m_ResizeHandleControlID)
                        {
                            GUIUtility.hotControl = 0;
                            PersistFraction();
                            evt.Use();
                        }
                        break;
                }
            }

            void HandleSortToggle(Rect sortRect, DrawerInstanceIMGUI instance)
            {
                var evt = Event.current;
                switch (evt.GetTypeForControl(m_SortToggleControlID))
                {
                    case EventType.MouseDown:
                        if (evt.button == 0 && sortRect.Contains(evt.mousePosition))
                        {
                            GUIUtility.hotControl = m_SortToggleControlID;
                            evt.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == m_SortToggleControlID)
                            evt.Use();
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == m_SortToggleControlID)
                        {
                            GUIUtility.hotControl = 0;
                            if (sortRect.Contains(evt.mousePosition))
                                instance.RequestSortToggle();
                            evt.Use();
                        }
                        break;
                }
            }

            public void ResetToDefaultFraction(float defaultFraction)
            {
                m_Column1Fraction = defaultFraction;
            }

            void PersistFraction()
            {
                UpdateCachedState(m_StateCacheKey, cached => cached.keyColumnFractionSetByUser = m_Column1Fraction);
            }

            public void PersistSortOrder(bool sortAscending)
            {
                UpdateCachedState(m_StateCacheKey, cached => cached.sortAscending = sortAscending);
            }

            static void HandleContextMenu(Rect headerRect, DrawerInstanceIMGUI instance)
            {
                var evt = Event.current;
                if (evt.type == EventType.ContextClick && headerRect.Contains(evt.mousePosition))
                {
                    var menu = new GenericMenu();

                    if (instance.header.HasCachedState)
                    {
                        menu.AddItem(new GUIContent(Texts.ResetToDefaultsLabel), false, () =>
                        {
                            instance.ResetToDefaults();
                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent(Texts.ResetToDefaultsLabel));
                    }

                    menu.ShowAsContext();
                    evt.Use();
                }
            }
        }

        public class DictionaryTreeView : TreeView
        {
            readonly DrawerInstanceIMGUI m_Instance;

            // Per-row measured heights; -1 = unmeasured. Allocated only when variableRowHeight is true.
            // Populated lazily by RowGUI; unmeasured rows use m_EstimatedRowHeight so totalHeight is
            // approximately correct before all rows have painted. Stale entries are caught by
            // RecordDynamicRowHeight setting needsHeightRefresh on the owning instance when a measured
            // row drifts.
            float[] m_LazyHeights;
            float m_EstimatedRowHeight;

            public new void RefreshCustomRowHeights() => base.RefreshCustomRowHeights();

            public void ComputeFixedInlineRowHeight()
            {
                if (m_Instance.displayedItemCount == 0)
                    return;

                bool prevWideMode = EditorGUIUtility.wideMode;
                EditorGUIUtility.wideMode = true;

                var element = m_Instance.arrayProperty.GetArrayElementAtIndex(0);
                GetKeyAndValueProperties(element, out var keyProp, out var valueProp);

                float keyH = GetPropertyFieldHeight(keyProp, m_Instance.keyType, m_Instance.keyHasCustomDrawer);
                float valH = GetPropertyFieldHeight(valueProp, m_Instance.valueType, m_Instance.valueHasCustomDrawer);

                EditorGUIUtility.wideMode = prevWideMode;

                rowHeight = Mathf.Max(keyH, valH) + Styles.k_RowVerticalPadding * 2;
            }

            void InitializeLazyHeights()
            {
                int count = m_Instance.displayedItemCount;
                if (!m_Instance.variableRowHeight || count == 0)
                {
                    m_LazyHeights = null;
                    return;
                }

                bool prevWideMode = EditorGUIUtility.wideMode;
                EditorGUIUtility.wideMode = true;

                var element = m_Instance.arrayProperty.GetArrayElementAtIndex(0);
                GetKeyAndValueProperties(element, out var keyProp, out var valueProp);

                float keyH = GetPropertyFieldHeight(keyProp, m_Instance.keyType, m_Instance.keyHasCustomDrawer);
                float valH = GetPropertyFieldHeight(valueProp, m_Instance.valueType, m_Instance.valueHasCustomDrawer);

                EditorGUIUtility.wideMode = prevWideMode;

                m_EstimatedRowHeight = Mathf.Max(keyH, valH) + Styles.k_RowVerticalPadding * 2;

                m_LazyHeights = new float[count];
                for (int i = 0; i < count; i++)
                    m_LazyHeights[i] = -1f;
            }

            public DictionaryTreeView(DrawerInstanceIMGUI instance)
                : base(instance.treeViewState)
            {
                m_Instance = instance;

                showBorder = false;
                showAlternatingRowBackgrounds = false;
                drawSelection = false;
                rowHeight = EditorGUIUtility.singleLineHeight + Styles.k_RowVerticalPadding * 2;
                useScrollView = true;
                m_TreeView.scrollViewStyle = GUIStyle.none; // Prevent the scroll view from drawing its own background which would stack on top of the Styles.boxBackground with the custom border we draw ourselves
            }

            protected override TreeViewItem BuildRoot()
            {
                InitializeLazyHeights();

                var root = new TreeViewItem { id = -1, depth = -1, displayName = "Root" };
                int count = m_Instance.sortedIndices.Length;
                if (count == 0)
                {
                    root.children = new List<TreeViewItem>();
                    return root;
                }

                var items = new List<TreeViewItem>(count);
                for (int i = 0; i < count; i++)
                    items.Add(new TreeViewItem(i, 0, i.ToString()));

                SetupParentsAndChildrenFromDepths(root, items);
                return root;
            }

            protected override float GetCustomRowHeight(int row, TreeViewItem item)
            {
                if (!m_Instance.variableRowHeight)
                    return rowHeight;

                if (m_LazyHeights != null && row >= 0 && row < m_LazyHeights.Length
                    && m_LazyHeights[row] >= 0f)
                    return m_LazyHeights[row];

                return m_EstimatedRowHeight > 0f ? m_EstimatedRowHeight : rowHeight;
            }

            static float GetPropertyFieldHeight(SerializedProperty prop, Type type, bool hasCustomDrawer)
            {
                if (prop == null)
                    return EditorGUIUtility.singleLineHeight;

                if (prop.propertyType == SerializedPropertyType.Generic && !hasCustomDrawer)
                    return GetInlineChildrenHeight(prop);

                return EditorGUI.GetPropertyHeight(prop, GUIContent.none, true);
            }

            static float GetInlineChildrenHeight(SerializedProperty parent)
            {
                if (parent == null || !parent.isValid || parent.propertyType != SerializedPropertyType.Generic)
                    return EditorGUIUtility.singleLineHeight;

                // Match DrawInlineChildren by iterating only visible children so the
                // measured height matches what is actually drawn — invisible fields
                // (e.g. [HideInInspector]) must not contribute to row height.
                float totalHeight = 0f;
                var end = parent.GetEndProperty();
                var child = parent.Copy();
                child.unsafeMode = true;
                bool hasChild = child.NextVisible(true);
                bool first = true;
                while (hasChild && !SerializedProperty.EqualContents(child, end))
                {
                    if (!first)
                        totalHeight += EditorGUIUtility.standardVerticalSpacing;
                    totalHeight += EditorGUI.GetPropertyHeight(child, true);
                    first = false;
                    hasChild = child.NextVisible(false);
                }

                return Mathf.Max(totalHeight, EditorGUIUtility.singleLineHeight);
            }

            protected override void BeforeRowsGUI()
            {
                base.BeforeRowsGUI();
                GetColumnPixelWidths(m_Instance.header.column1Fraction, treeViewRect.width, out var col0Width, out _);
                Rect lineRect = new Rect(col0Width, 0, k_VerticalSplitterWidth, totalHeight);
                EditorGUI.DrawRect(lineRect, SharedStyles.k_RowsSplitColor);
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                DrawAlternatingRowBackgroundIfNeeded(args);

                int displayIndex = args.item.id;
                if (!TryGetEntryProperties(displayIndex, out var keyProp, out var valueProp, out int arrayIndex))
                    return;

                var evt = Event.current;
                bool wasMouseDownInRow = evt.type == EventType.MouseDown && evt.button == 0 && args.rowRect.Contains(evt.mousePosition);

                float prevLabelWidth = EditorGUIUtility.labelWidth;
                bool prevWideMode = EditorGUIUtility.wideMode;
                EditorGUIUtility.wideMode = true;

                // The args.rowRect is only the visible rect, to calculate the cell rects we want to
                // use the full treeview rect width. This is needed when the dictionary drawer is
                // overflowing in narrow Inspectors.
                Rect fullRowRect = new Rect(args.rowRect.x, args.rowRect.y, treeViewRect.width, args.rowRect.height);
                m_Instance.header.GetColumnRects(fullRowRect, out var keyRect, out var valueRect);

                if (showingVerticalScrollBar)
                {
                    float visibleRightEdge = fullRowRect.xMax - Styles.k_VerticalScrollbarWidth;
                    valueRect.xMax = Mathf.Max(valueRect.xMin, visibleRightEdge);
                }

                DrawKeyCell(keyRect, keyProp, arrayIndex);
                DrawValueCell(valueRect, valueProp);
                RecordDynamicRowHeight(displayIndex, args.rowRect.height, keyProp, valueProp);

                EditorGUIUtility.labelWidth = prevLabelWidth;
                EditorGUIUtility.wideMode = prevWideMode;

                if (wasMouseDownInRow)
                    HandleRowSelectionClick(args);

                DrawRowSelectionOutlineIfSelected(args);
            }

            void HandleRowSelectionClick(RowGUIArgs args)
            {
                // Rows should be selectable via clicks both within controls and in non-control areas.

                // Drive selection through the TreeView's SelectionClick so we reuse the logic for
                // handling multiselect with shift, ctrl/cmd as well as plain-click.
                SelectionClick(args.item, keepMultiSelection: false);

                // Property fields flip evt.type to Used when one consumes the click for its
                // own control. In that case the cell already owns keyboard
                // focus, and we leave it alone; otherwise the click hit empty row
                // space, and we replicate the focus grab the TreeView's own
                // HandleUnusedMouseEventsForItem path would have done — without it
                // KeyEvent (F to frame, Cmd+D to duplicate) stops working.
                if (Event.current.type == EventType.MouseDown)
                    SetFocus();

                // Always Use() the event. Without this guard a ctrl/cmd
                // toggle would be immediately undone by the TreeView's own pass.
                Event.current.Use();
            }

            static void DrawAlternatingRowBackgroundIfNeeded(RowGUIArgs args)
            {
                if (args.row % 2 != 0 && Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(args.rowRect, SharedStyles.k_AlternatingRowColor);
            }

            bool TryGetEntryProperties(int displayIndex, out SerializedProperty keyProp, out SerializedProperty valueProp, out int arrayIndex)
            {
                keyProp = null;
                valueProp = null;
                arrayIndex = -1;

                if (displayIndex < 0 || displayIndex >= m_Instance.sortedIndices.Length)
                    return false;

                arrayIndex = m_Instance.sortedIndices.ToArrayIndex(displayIndex);
                // Defensive guard: structural changes (size shrinks especially) are
                // applied on the next editor tick via RunDeferredStructuralWork, so a
                // single OnGUI pass can run with sortedIndices snapshotted from before
                // the underlying array shrunk. Skip the row instead of letting
                // GetArrayElementAtIndex throw; the deferred reload picks it up.
                if (arrayIndex < 0 || arrayIndex >= m_Instance.arrayProperty.arraySize)
                    return false;

                var element = m_Instance.arrayProperty.GetArrayElementAtIndex(arrayIndex);
                GetKeyAndValueProperties(element, out keyProp, out valueProp);
                return true;
            }

            void DrawKeyCell(Rect keyRect, SerializedProperty keyProp, int arrayIndex)
            {
                keyRect.yMin += Styles.k_RowVerticalPadding;
                keyRect.yMax -= Styles.k_RowVerticalPadding;
                var markerKind = DictionaryKeyUtility.GetMarkerKind(arrayIndex, m_Instance.duplicateEntryIndices);
                if (markerKind != DictionaryKeyUtility.KeyMarkerKind.None)
                    DrawDuplicateKeyIcon(keyRect);
                float keyLeft = Styles.k_KeyLeftMargin;
                float minFieldWidth = GetCellMinFieldWidth(keyProp, m_Instance.keyHasCustomDrawer);
                var keyFieldRect = BuildCellFieldRect(keyRect, keyLeft + Styles.k_CellHorizontalPadding, Styles.k_CellHorizontalPadding, minFieldWidth);
                EditorGUIUtility.labelWidth = ComputeCellLabelWidth(keyFieldRect.width);
                // Key edits no longer flip needsReload / needsDuplicateRefresh from here.
                // The TrackPropertyValue listener registered on the IMGUIContainer in
                // GetOrCreate handles both same-inspector and cross-inspector
                // updates uniformly, so this draw site only renders the field.
                DrawClippedPropertyField(keyRect, keyFieldRect, keyProp, m_Instance.keyType, m_Instance.keyHasCustomDrawer);
            }

            void DrawValueCell(Rect cellRect, SerializedProperty valueProp)
            {
                cellRect.yMin += Styles.k_RowVerticalPadding;
                cellRect.yMax -= Styles.k_RowVerticalPadding;
                float minFieldWidth = GetCellMinFieldWidth(valueProp, m_Instance.valueHasCustomDrawer);
                var fieldRect = BuildCellFieldRect(cellRect, Styles.k_ValueLeftPadding, Styles.k_CellHorizontalPadding, minFieldWidth);
                EditorGUIUtility.labelWidth = ComputeCellLabelWidth(fieldRect.width);
                DrawPropertyField(fieldRect, valueProp, m_Instance.valueType, m_Instance.valueHasCustomDrawer);
            }

            // BuildCellFieldRect floors the field width at a non-zero minimum, so on a narrow
            // column the field rect can overflow its cell horizontally — the key field would
            // bleed into the value column, the value field into the inspector's scrollbar /
            // padding strip. Clipping to the cell rect hides the overflow visually and (because
            // GUI.BeginClip also masks events) routes clicks in the neighbouring cell to that
            // cell's own controls instead of the overflowing one.
            static void DrawClippedPropertyField(Rect cellRect, Rect fieldRect, SerializedProperty prop, Type type, bool hasCustomDrawer)
            {
                GUI.BeginClip(cellRect);
                var fieldRectInClipSpace = new Rect(fieldRect.x - cellRect.x, fieldRect.y - cellRect.y, fieldRect.width, fieldRect.height);
                DrawPropertyField(fieldRectInClipSpace, prop, type, hasCustomDrawer);
                GUI.EndClip();
            }

            // The cell rect can become arbitrarily narrow (very narrow Inspector, narrow column
            // fraction). Without a floor the field rect would shrink to a negative width and
            // every label/control inside it would render with garbage geometry. Floor the field
            // width at minFieldWidth; if the cell can't accommodate that, the field overflows
            // the cell horizontally rather than squeezing the control out of existence — that
            // is the documented trade-off for keeping the row usable at extreme widths.
            static Rect BuildCellFieldRect(Rect cellRect, float leftPadding, float rightPadding, float minFieldWidth)
            {
                float width = Mathf.Max(minFieldWidth, cellRect.width - leftPadding - rightPadding);
                return new Rect(cellRect.x + leftPadding, cellRect.y, width, cellRect.height);
            }

            // Returns the smallest acceptable field-rect width for a cell. The dictionary drawer
            // dispatches in DrawPropertyField:
            //   - Generic + no custom drawer  → DrawInlineChildren expands child properties and
            //     each child's PropertyField reserves EditorGUIUtility.labelWidth for its own
            //     label (e.g. "Color", "Target", "Vector"). The cell therefore needs room for
            //     both a min label and a min control, plus the kPrefixPaddingRight gap that
            //     PrefixLabel inserts between them.
            //   - Anything else              → EditorGUI.PropertyField is called with
            //     GUIContent.none, so PrefixLabel's "no label" branch hands the entire rect to
            //     the control and labelWidth is irrelevant. Only a min control width is needed.
            // IMGUI labels default to TextClipping.Overflow, so simply setting labelWidth = 0
            // does not hide the label — it keeps overflowing onto the control area. Reserving
            // the right amount of space up front is the only way to keep both visible.
            static float GetCellMinFieldWidth(SerializedProperty prop, bool hasCustomDrawer)
            {
                bool willInlineChildren = prop != null && prop.propertyType == SerializedPropertyType.Generic && !hasCustomDrawer;
                return willInlineChildren
                    ? Styles.k_CellLabelMinWidth + EditorGUI.kPrefixPaddingRight + Styles.k_CellControlMinWidth
                    : Styles.k_CellControlMinWidth;
            }

            // Computes EditorGUIUtility.labelWidth for a cell. PrefixLabel splits the field rect
            // as controlWidth = fieldWidth - labelWidth - kPrefixPaddingRight, so we clamp the
            // configured label fraction to leave at least k_CellControlMinWidth for the control.
            // When the field rect is too narrow even for the label minimum (which only happens
            // for the "no outer label" cell path where labelWidth doesn't affect the visual),
            // we collapse labelWidth to 0 so PrefixLabel's no-label branch applies.
            static float ComputeCellLabelWidth(float fieldRectWidth)
            {
                float available = fieldRectWidth - EditorGUI.kPrefixPaddingRight;
                float maxLabel = available - Styles.k_CellControlMinWidth;
                if (maxLabel < Styles.k_CellLabelMinWidth)
                    return 0f;
                float desired = fieldRectWidth * Styles.k_CellLabelWidthFraction;
                return Mathf.Clamp(desired, Styles.k_CellLabelMinWidth, maxLabel);
            }

            void RecordDynamicRowHeight(int displayIndex, float currentRowHeight, SerializedProperty keyProp, SerializedProperty valueProp)
            {
                if (!m_Instance.dynamicRowHeight)
                    return;

                float keyH = GetPropertyFieldHeight(keyProp, m_Instance.keyType, m_Instance.keyHasCustomDrawer);
                float valH = GetPropertyFieldHeight(valueProp, m_Instance.valueType, m_Instance.valueHasCustomDrawer);
                float measuredH = Mathf.Max(keyH, valH) + Styles.k_RowVerticalPadding * 2;

                if (m_LazyHeights != null && displayIndex >= 0 && displayIndex < m_LazyHeights.Length)
                    m_LazyHeights[displayIndex] = measuredH;

                if (!m_Instance.needsHeightRefresh && Mathf.Abs(currentRowHeight - measuredH) > 0.5f)
                    m_Instance.needsHeightRefresh = true;
            }

            void DrawRowSelectionOutlineIfSelected(RowGUIArgs args)
            {
                if (args.selected && Event.current.type == EventType.Repaint)
                    DrawSelectionOutline(args.rowRect, m_Instance.HasFocus());
            }

            static void DrawSelectionOutline(Rect rect, bool focused)
            {
                rect.height -= 1;
                Color color = focused
                    ? SharedStyles.k_SelectionOutlineColor.color
                    : SharedStyles.k_SelectionOutlineColorInactive.color;
                float w = Styles.k_SelectionBorderWidth;
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, w), color);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - w, rect.width, w), color);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + w, w, rect.height - 2 * w), color);
                EditorGUI.DrawRect(new Rect(rect.xMax - w, rect.y + w, w, rect.height - 2 * w), color);
            }

            static void DrawPropertyField(Rect rect, SerializedProperty prop, Type type, bool hasCustomDrawer)
            {
                if (prop == null)
                    return;

                if (prop.propertyType == SerializedPropertyType.Generic && !hasCustomDrawer)
                {
                    DrawInlineChildren(rect, prop);
                }
                else
                {
                    EditorGUI.PropertyField(rect, prop, GUIContent.none, true);
                }
            }

            static void DrawInlineChildren(Rect rect, SerializedProperty parent)
            {
                if (parent == null || !parent.isValid || parent.propertyType != SerializedPropertyType.Generic)
                    return;

                // Iterate only visible children so [HideInInspector] members are not
                // turned into editable IMGUI fields (matches the normal Inspector
                // behaviour for hidden serialized fields).
                var end = parent.GetEndProperty();
                var child = parent.Copy();
                child.unsafeMode = true;
                bool hasChild = child.NextVisible(true);
                float y = rect.y;
                while (hasChild && !SerializedProperty.EqualContents(child, end))
                {
                    float h = EditorGUI.GetPropertyHeight(child, true);
                    var childRect = new Rect(rect.x, y, rect.width, h);
                    EditorGUI.PropertyField(childRect, child, true);
                    y += h + EditorGUIUtility.standardVerticalSpacing;
                    hasChild = child.NextVisible(false);
                }
            }

            // Draws a fixed-size warning icon at the top of the key column gutter for
            // rows whose key is a duplicate. Position and size mirror UITK
            // .unity-dictionary-view__duplicate-key-icon so both backends look identical. The
            // GUI.Label call paints nothing on its own (GUIStyle.none + empty text) but
            // registers the hit area for the hover tooltip.
            static void DrawDuplicateKeyIcon(Rect cellRect)
            {
                var icon = EditorGUIUtility.GetHelpIcon(MessageType.Warning);
                if (icon == null)
                    return;

                var iconRect = new Rect(
                    cellRect.x + Styles.k_DuplicateKeyIconLeftMargin,
                    cellRect.y + Styles.k_DuplicateKeyIconTopOffset,
                    Styles.k_DuplicateKeyIconSize,
                    Styles.k_DuplicateKeyIconSize);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                GUI.Label(iconRect, EditorGUIUtility.TempContent(string.Empty, Texts.DuplicateMarkerTooltip), GUIStyle.none);
            }

            protected override bool CanMultiSelect(TreeViewItem item)
            {
                return true;
            }

            protected override void KeyEvent()
            {
                var evt = Event.current;
                if (evt.type != EventType.KeyDown)
                    return;

                if (evt.keyCode == KeyCode.F && !evt.control && !evt.command && !evt.alt)
                {
                    var selection = GetSelection();
                    if (selection.Count > 0)
                    {
                        FrameItem(selection[0]);
                        evt.Use();
                    }
                }
                else if (evt.keyCode == KeyCode.D && (evt.command || evt.control))
                {
                    if (GetSelection().Count == 1)
                    {
                        m_Instance.needsDuplicate = true;
                        evt.Use();
                    }
                }
            }
        }
    }
}

// Helper class that complement EditorGUI. Lives next to the IMGUI dictionary drawer
// because that's the only consumer today, but contains no dictionary-specific knowledge
// and can be reused by any property drawer that needs the same visual treatment.
internal static class DrawerEditorGUI
{
    // Vertical gap between the helpbox text and the overlaid button area, both rendered
    // inside the same EditorStyles.helpBox rect.
    const float k_HelpBoxButtonGap = 5f;
    const float k_HelpBoxButtonHeight = 20f;
    const float k_HelpBoxButtonMinWidth = 60f;
    const float k_HelpBoxButtonInset = 4f;

    // EditorStyles.helpBox uses MiddleLeft vertical alignment, which would vertically
    // center the icon+text inside the (intentionally taller-than-text) HelpBoxWithButton
    // rect and push the last lines of text underneath the overlaid button. We render the
    // same style with UpperLeft so the content stays anchored to the top, leaving the
    // bottom-right corner clear for the button overlay. Lazy-init: EditorStyles.helpBox
    // is not safe to access during static class reload.
    static GUIStyle s_HelpBoxUpperLeft;
    static GUIStyle helpBoxUpperLeft
    {
        get
        {
            if (s_HelpBoxUpperLeft == null)
                s_HelpBoxUpperLeft = new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.UpperLeft };
            return s_HelpBoxUpperLeft;
        }
    }

    // Total inner height needed to render HelpBoxWithButton at the given width: the
    // helpbox text height plus the gap and button row that overlay it. Callers add any
    // outer top/bottom margins themselves.
    public static float GetHelpBoxWithButtonHeight(MessageType messageType, string message, float width)
    {
        var content = EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(messageType));
        float textHeight = helpBoxUpperLeft.CalcHeight(content, width);
        return textHeight + k_HelpBoxButtonGap + k_HelpBoxButtonHeight;
    }

    // Draws an EditorGUI.HelpBox-styled box (with the icon for the given MessageType)
    // and overlays a button at the bottom-right of the same rect. Returns true on the frame the
    // button is pressed. Button width auto-fits the button text (clamped to k_HelpBoxButtonMinWidth)
    // using the same style we draw it with, so longer labels stay readable without truncation.
    public static bool HelpBoxWithButton(Rect position, MessageType messageType, string message, string buttonText)
    {
        var content = EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(messageType));
        GUI.Label(position, content, helpBoxUpperLeft);

        var buttonContent = EditorGUIUtility.TempContent(buttonText);
        float buttonWidth = Mathf.Max(k_HelpBoxButtonMinWidth, GUI.skin.button.CalcSize(buttonContent).x);
        var buttonRect = new Rect(
            position.xMax - buttonWidth - k_HelpBoxButtonInset,
            position.yMax - k_HelpBoxButtonHeight - k_HelpBoxButtonInset,
            buttonWidth,
            k_HelpBoxButtonHeight);
        return GUI.Button(buttonRect, buttonContent);
    }
}

} // end of namespace
