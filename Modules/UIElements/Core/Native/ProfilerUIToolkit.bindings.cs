// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Profiling;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Per-batch (per-EvaluateChain KickRanges) metrics. The PANEL_BATCH_METRICS metadata tag
    /// carries one chunk per panel per frame, whose payload is a contiguous
    /// <c>NativeArray&lt;UIToolkitBatchMetricsInfo&gt;</c> (one element per batch). Each element's
    /// <see cref="ownerOffset"/> and <see cref="ownerCount"/> slice into the corresponding
    /// PANEL_BATCH_OWNERS chunk (paired by per-frame ordinal) which carries a flat
    /// <c>NativeArray&lt;EntityId&gt;</c> of every batch's IPanelComponent owners back-to-back.
    /// Both chunks are emitted in a single P/Invoke via the
    /// <c>ProfilerUIToolkit_EmitBatchMetricsForPanel</c> binding (rather than
    /// <see cref="Profiler.EmitFrameMetaData{T}(Guid,int,NativeArray{T})"/>, which is
    /// <c>[Conditional("ENABLE_PROFILER")]</c> and would strip the entire managed call site).
    /// </summary>
    [NativeHeader("Modules/UIElements/Core/Native/ProfilerUIToolkit.h")]
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEditor.UIElementsModule")]
    internal struct UIToolkitBatchMetricsInfo
    {
        public EntityId panelEntityId;
        public UInt32 drawCallCount;
        public UInt32 vertexCount;
        public UInt32 indexCount;
        public UInt32 immediateDraws;
        public UInt32 drawRangeCount;
        public byte kickRangesReason;        // UIR.KickRangesReason ordinal — byte-sized; pair with isRenderingNestedTreeRT to avoid alignment waste.
        public byte isRenderingNestedTreeRT; // 0 or 1
        // 2 bytes padding here to align ownerOffset
        public UInt32 ownerOffset; // Index into the per-frame PANEL_BATCH_OWNERS chunk.
        public UInt32 ownerCount;
    }

    /// <summary>
    /// Per-panel per-frame update metrics. Used both as the in-flight struct passed to the native
    /// AddPanelUpdateMetrics binding AND as the on-disk payload stored under the PANEL_METRICS
    /// metadata tag (one chunk per panel per frame, written during
    /// <see cref="ProfilerUIToolkitPanelMetadataCapture.RecordForCapture"/>). Batch totals are not
    /// duplicated here — the view derives them by summing PANEL_BATCH_METRICS chunks per panel.
    /// </summary>
    [NativeHeader("Modules/UIElements/Core/Native/ProfilerUIToolkit.h")]
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEditor.UIElementsModule")]
    internal struct UIToolkitPanelUpdateMetricsInfo
    {
        public EntityId panelEntityId;
        public UInt32 hierarchyVersionChanges;
        public UInt32 repaintVersionChanges;
        public Int32 visualElementCount;
    }

    /// <summary>
    /// One element per input event captured by <see cref="EventDispatcher"/>. The PANEL_EVENTS
    /// metadata tag carries a single per-frame chunk whose payload is a contiguous
    /// <c>NativeArray&lt;UIToolkitPanelEventInfo&gt;</c> holding events for every panel that
    /// processed input that frame (reader buckets by <see cref="panelEntityId"/>).
    /// <see cref="eventNameIndex"/> indexes the event's concrete type name in the per-session
    /// PANEL_EVENT_TYPE_NAMES chunk set (every event carries one). <see cref="eventKind"/> is a
    /// <see cref="UIToolkitProfilerEventKind"/> value telling the reader how to interpret the
    /// payload slots: <see cref="UIToolkitProfilerEventKind.Pointer"/> → button in
    /// <see cref="buttonOrKeyCode"/> + panel-space position in <see cref="positionX"/>/<see cref="positionY"/>/<see cref="positionZ"/>
    /// (Z is the pointer depth, 0 for mouse / screen-space panels; covers pointer events and
    /// WheelEvent) + EventModifiers in the high 16 bits of <see cref="keyCharAndModifiers"/>;
    /// <see cref="UIToolkitProfilerEventKind.Keyboard"/> → KeyCode in <see cref="buttonOrKeyCode"/>
    /// + character (low 16 bits) / EventModifiers (high 16 bits) packed into
    /// <see cref="keyCharAndModifiers"/>;
    /// <see cref="UIToolkitProfilerEventKind.NavigationMove"/> → NavigationMoveEvent.Direction in
    /// <see cref="buttonOrKeyCode"/> + move vector in <see cref="positionX"/>/<see cref="positionY"/>
    /// + EventModifiers in the high 16 bits of <see cref="keyCharAndModifiers"/>;
    /// <see cref="UIToolkitProfilerEventKind.Navigation"/> → EventModifiers in the high 16 bits of
    /// <see cref="keyCharAndModifiers"/> only (NavigationSubmitEvent / NavigationCancelEvent);
    /// <see cref="UIToolkitProfilerEventKind.None"/> → no payload (other events show name only).
    /// <see cref="targetTypeNameIndex"/> and <see cref="targetElementNameIndex"/> reference the target
    /// VisualElement's type name (e.g. "Button") and instance name (e.g. "ok-button") in that same
    /// PANEL_EVENT_TYPE_NAMES chunk set. All three name fields use a 1-based encoding so "none" and
    /// "overflow" are distinct from each other and from a real reference: 0 = none (no value — e.g. no
    /// VisualElement target, or an unnamed element; also the zero-init default), 0xFFFF = the value
    /// existed but the per-session pool was full (read back as "Unknown"), and 1..0xFFFE = pool
    /// position + 1. Unused payload slots stay zero. Layout MUST match the native struct in
    /// ProfilerUIToolkit.h (48 bytes, 8-aligned).
    /// </summary>
    [NativeHeader("Modules/UIElements/Core/Native/ProfilerUIToolkit.h")]
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEditor.UIElementsModule")]
    internal struct UIToolkitPanelEventInfo
    {
        public EntityId panelEntityId;
        public EntityId targetEntityId;
        public byte eventKind;                // UIToolkitProfilerEventKind
        public byte _padding;
        // The three *NameIndex fields are 1-based references into the PANEL_EVENT_TYPE_NAMES pool:
        // 0 = none (no value), 0xFFFF = overflow (value existed but pool was full, shown as "Unknown"),
        // and 1..0xFFFE = pool position + 1.
        public UInt16 eventNameIndex;         // event type name; 0 / 0xFFFF both read as "Unknown"
        public UInt16 targetTypeNameIndex;    // target VisualElement type; 0 = no target, 0xFFFF = overflow
        public UInt16 targetElementNameIndex; // target VisualElement name; 0 = unnamed / no target, 0xFFFF = overflow
        public UInt32 buttonOrKeyCode;
        public float positionX;
        public float positionY;
        public float positionZ;        // pointer depth (IPointerEvent); 0 for mouse / screen-space panels
        // High 16 bits = EventModifiers (set for pointer, mouse, keyboard, and navigation events).
        // Keyboard also stores the character (UTF-16) in the low 16 bits; the others leave it 0. Zero otherwise.
        public UInt32 keyCharAndModifiers;
    }

    /// <summary>
    /// UI Toolkit profiler frame metadata. <see cref="ProfilerUIToolkit.RecordProfilerPanelMetadataForCapture"/>
    /// runs from <c>ProfilerUIToolkit::CaptureFrame</c> and forwards to
    /// <see cref="ProfilerUIToolkitPanelMetadataCapture.RecordForCapture"/>, which emits the per-panel
    /// metadata chunks (PANEL_ENTRIES + PANEL_METRICS) and folds the panel update counters into the
    /// global counters via the native AddPanelUpdateMetrics binding. Per-batch metadata
    /// (PANEL_BATCH_METRICS + PANEL_BATCH_OWNERS) is emitted once per panel from the renderer at the
    /// end of EvaluateChain via the native <c>ProfilerUIToolkit_EmitBatchMetricsForPanel</c> binding;
    /// the aggregate batch counters are updated in a single per-panel call to
    /// <see cref="AddBatchAggregateCounters"/>.
    /// </summary>
    [NativeHeader("Modules/UIElements/Core/Native/ProfilerUIToolkit.h")]
    [NativeHeader("Runtime/Interfaces/IProfilerUIToolkit.h")]
    internal static partial class ProfilerUIToolkit
    {
        internal static readonly Guid kProfilerMetadataGuid = new Guid("a8f3c2d1-5e4b-4a7c-9d2e-1f0a3b6c8d5e");

        // Must match UNITY_PROFILER_UI_TOOLKIT_METADATA_TAG_* in ProfilerUIToolkit.h.
        internal const int kProfilerUIToolkitMetadataTagPanelEntries = 0;
        internal const int kProfilerUIToolkitMetadataTagPanelMetrics = 1;
        internal const int kProfilerUIToolkitMetadataTagPanelBatchMetrics = 2;
        // PANEL_BATCH_OWNERS: per-panel flat NativeArray<EntityId>; sliced by each
        // UIToolkitBatchMetricsInfo's (ownerOffset, ownerCount). Paired with the matching panel's
        // PANEL_BATCH_METRICS chunk by per-frame ordinal.
        internal const int kProfilerUIToolkitMetadataTagPanelBatchOwners = 3;
        // PANEL_EVENTS: one chunk per frame holding a flat NativeArray<UIToolkitPanelEventInfo>
        // of every captured EventDispatcher event for the frame across all panels. Each event
        // carries its payload (per eventKind) plus an eventNameIndex into PANEL_EVENT_TYPE_NAMES.
        internal const int kProfilerUIToolkitMetadataTagPanelEvents = 4;
        // PANEL_EVENT_TYPE_NAMES: per-session interned UTF-8 strings for the events in PANEL_EVENTS —
        // event type names plus each event's target VisualElement type name and instance name. One
        // chunk per distinct string, emitted once per capture session and read back
        // frame-independently via GetSessionMetaData; an event's eventNameIndex / targetTypeNameIndex
        // / targetElementNameIndex each index into this set (chunk N = the Nth distinct string
        // encountered in the session).
        internal const int kProfilerUIToolkitMetadataTagPanelEventTypeNames = 5;

        // Mirrors ProfilerUIToolkit::CaptureMode in ProfilerUIToolkit.h. Pushed from native via
        // the SetActiveCaptureMode callback whenever the mode changes (in ProfilerUIToolkit::NewFrame).
        // Cached here so ShouldCapturePanel can answer without a native crossing on every batch event.
        internal enum CaptureMode
        {
            Disabled = 0,
            EditorAndPlaymode = 1,
            PlaymodeOnly = 2,
        }

        static CaptureMode s_ActiveCaptureMode = CaptureMode.Disabled;

        // Registered into native via Native_RegisterManagedCallbacks at managed init time so
        // ProfilerUIToolkit::NewFrame (native) can push mode transitions back to managed. The
        // static readonly fields keep the delegates alive — Marshal.GetFunctionPointerForDelegate
        // doesn't pin them.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void SetActiveCaptureModeDelegate(int mode);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void RecordProfilerPanelMetadataForCaptureDelegate();

        static readonly SetActiveCaptureModeDelegate s_SetActiveCaptureModeDelegate = SetActiveCaptureMode;
        static readonly RecordProfilerPanelMetadataForCaptureDelegate s_RecordProfilerPanelMetadataForCaptureDelegate = RecordProfilerPanelMetadataForCapture;

        [FreeFunction("ProfilerUIToolkit_RegisterManagedCallbacks")]
        extern static void Native_RegisterManagedCallbacks(IntPtr setActiveCaptureMode, IntPtr recordProfilerPanelMetadataForCapture);

        // Called once during managed UIElements init (UIElementsInitialization.InitializeUIElementsManaged)
        // to wire the native-side function pointers.
        internal static void RegisterManagedCallbacks()
        {
            Native_RegisterManagedCallbacks(
                Marshal.GetFunctionPointerForDelegate(s_SetActiveCaptureModeDelegate),
                Marshal.GetFunctionPointerForDelegate(s_RecordProfilerPanelMetadataForCaptureDelegate));
        }

        [AOT.MonoPInvokeCallback(typeof(SetActiveCaptureModeDelegate))]
        internal static void SetActiveCaptureMode(int mode)
        {
            s_ActiveCaptureMode = (CaptureMode)mode;

            // Profiler fully stopped (native NewFrame pushes Disabled with profiler_is_enabled()
            // false): the next capture is a fresh native session, so reset the intern tables to
            // re-intern names from index 0. A UI Toolkit *category* toggle also pushes Disabled but
            // with Profiler.enabled still true — skip the reset there, or we'd re-emit names and
            // shift the eventNameIndex mapping the reader relies on.
            if (s_ActiveCaptureMode == CaptureMode.Disabled && !Profiler.enabled)
            {
                s_EventTypeNameIndices.Clear();
                s_StyleStringIndices.Clear();
                s_InternedStrings.Clear();
                s_EmittedInternedStringCount = 0;

                // Also drop events enqueued after the last flush: FlushPendingEvents only runs while
                // enabled, so these can never be emitted this session and their eventNameIndex now
                // points into the just-cleared name table.
                s_PendingEventsCount = 0;
            }
        }

        [FreeFunction(Name = "ProfilerUIToolkit::EmitProfilerPanelMetadata")]
        internal static extern void EmitProfilerPanelMetadata([NotNull] EntityId[] entityIds, int count);

        // Per-panel update metrics: updates 3 counters and emits the PANEL_METRICS chunk in one
        // native call. Singleton is fetched native-side (no cached IntPtr round-trip needed at
        // per-panel cadence).
        [FreeFunction("ProfilerUIToolkit_AddPanelUpdateMetrics")]
        extern static void Native_AddPanelUpdateMetrics(ref UIToolkitPanelUpdateMetricsInfo panelMetrics);

        // Per-panel batch aggregate counter update: updates the 4 batch counters (BatchCount,
        // DrawCalls, Vertices, Indices) in one call. No metadata emission (the per-batch chunks
        // are emitted from native via Native_EmitBatchMetricsForPanel).
        [FreeFunction("ProfilerUIToolkit_AddBatchAggregateCounters")]
        extern internal static void AddBatchAggregateCounters(UInt32 batchCount, UInt32 drawCalls, UInt32 vertices, UInt32 indices);

        // Per-panel PANEL_BATCH_METRICS + PANEL_BATCH_OWNERS emission. Replaces the two
        // Profiler.EmitFrameMetaData<T> calls that used to live in EmitBatchMetricsForPanel:
        // those are [Conditional("ENABLE_PROFILER")], which strips the whole managed call site
        // (including the NativeArray prep) from non-development builds and silently neuters the
        // per-batch profiler payload. Routing through a native binding keeps the managed site
        // live and moves the ENABLE_PROFILER gate to native, where it matches AddPanelUpdateMetrics
        // and AddBatchAggregateCounters. Both chunks go through one P/Invoke since they're emitted
        // as a paired ordinal.
        [FreeFunction("ProfilerUIToolkit_EmitBatchMetricsForPanel")]
        extern static unsafe void Native_EmitBatchMetricsForPanel(
            UIToolkitBatchMetricsInfo* batches, int batchCount,
            EntityId* owners, int ownerCount);

        // Per-frame PANEL_EVENTS emission. Mirrors Native_EmitBatchMetricsForPanel's pattern —
        // raw pointer + count routed through native so the [Conditional("ENABLE_PROFILER")] gate
        // moves to the .cpp side and the managed pinning call site survives in non-development
        // builds. Counter increment is folded into the native function so the chunk and counter
        // can never disagree.
        [FreeFunction("ProfilerUIToolkit_EmitPanelEvents")]
        extern static unsafe void Native_EmitPanelEvents(UIToolkitPanelEventInfo* events, int count);

        // Emits one PANEL_EVENT_TYPE_NAMES chunk holding a single UTF-8 interned string as session
        // metadata. Called once per distinct string (event type name, target VisualElement type, or
        // element name) per capture session from FlushPendingEvents, in index order, so the emitted
        // session chunk ordinal matches the index stored on the events. No counter bump.
        [FreeFunction("ProfilerUIToolkit_EmitEventTypeName")]
        extern static unsafe void Native_EmitEventTypeName(byte* utf8, int length);

        // Per-frame in-flight buffer for events captured by EventDispatcher. Backed by a managed
        // array (rather than List<T> or NativeArray) so the flush path can pin it via `fixed`
        // without per-frame allocations.
        static UIToolkitPanelEventInfo[] s_PendingEvents = new UIToolkitPanelEventInfo[64];
        static int s_PendingEventsCount;

        // Per-session de-duplicated string pool for captured events: event type names plus each
        // event's target VisualElement type name and instance name. s_InternedStrings preserves
        // insertion order for emission; its 0-based position is the string's PANEL_EVENT_TYPE_NAMES
        // session chunk ordinal. Two dictionaries front it, keyed by the cheapest identity available
        // for each source so the hot path does no string hashing: s_EventTypeNameIndices by event Type
        // (reference identity), s_StyleStringIndices by UniqueStyleString id (the pre-interned int
        // behind VisualElement.typeNameId / nameId). Both map into the one shared list. All persist
        // across frames for the whole capture session — reset only when the profiler stops (see
        // SetActiveCaptureMode). s_EmittedInternedStringCount is the high-water mark of strings
        // already emitted to native this session, so each string is encoded and emitted exactly once.
        //
        // The events store references to the pool in UInt16 fields using a 1-BASED encoding so the two
        // out-of-band states are distinct from a real index AND from each other (the previous single
        // 0xFFFF sentinel conflated them, and a zero-initialized field aliased real index 0):
        //   0                 = k_InternedStringNone     — no value (no target / unnamed element); this
        //                                                 is the zero-init default, so a field left
        //                                                 unset correctly reads as "none".
        //   0xFFFF            = k_InternedStringOverflow — a value existed but the pool was full.
        //   1 .. 0xFFFE       = pool position + 1       — string is s_InternedStrings[value - 1].
        // The pool is therefore capped at k_InternedStringPoolMax (0xFFFE) distinct strings/session;
        // once full, AddInternedString returns the overflow sentinel rather than a value that would
        // alias 0xFFFF or wrap the UInt16. Reaching that many distinct strings in one session is
        // pathological; the graceful degradation is that further values read back as "Unknown".
        const ushort k_InternedStringNone = 0;
        const ushort k_InternedStringOverflow = ushort.MaxValue;     // 0xFFFF
        const int k_InternedStringPoolMax = ushort.MaxValue - 1;     // 0xFFFE distinct strings/session
        static readonly Dictionary<Type, ushort> s_EventTypeNameIndices = new();
        static readonly Dictionary<int, ushort> s_StyleStringIndices = new();
        static readonly List<string> s_InternedStrings = new();
        static int s_EmittedInternedStringCount;

        // Called from EventDispatcher.ProcessEvent after every dispatched event; captures it into the
        // per-frame PANEL_EVENTS buffer. A no-op unless the UI Toolkit profiler is recording this
        // panel: ShouldCapturePanel is a managed-side check against the cached capture mode (no native
        // crossing), and internal events implementing IProfilerIgnoredEvent are skipped. Every captured
        // event counts toward "Event Count", carries its concrete type name (interned per session) for
        // display, and records a payload kind describing which fields PopulateEventInfo filled
        // (button/position for pointer & mouse events, keyCode/character/modifiers for keyboard events,
        // direction/move/modifiers for NavigationMoveEvent, modifiers for other navigation events, none
        // for the rest).
        internal static void CapturePanelEvent(BaseVisualElementPanel panel, EventBase evt)
        {
            if (!ShouldCapturePanel(panel.contextType == ContextType.Editor) || evt is IProfilerIgnoredEvent)
                return;

            var info = new UIToolkitPanelEventInfo
            {
                panelEntityId = panel.ownerObject != null ? panel.ownerObject.GetEntityId() : EntityId.None,
            };
            if (evt.elementTarget is { } target)
            {
                // targetTypeNameIndex / targetElementNameIndex intern the target VisualElement's type
                // name and instance name off its already-cached UniqueStyleString ids (typeNameId /
                // nameId); they are left at their zero-init default (k_InternedStringNone) when there's
                // no target or no name. targetEntityId is the IPanelComponent that owns the target
                // (walked up from it), or EntityId.None when no IPanelComponent root is found (editor
                // panels, flat runtime panels, or null target).
                info.targetTypeNameIndex = InternStyleString(target.typeNameId);
                if (target.nameId >= 0)
                    info.targetElementNameIndex = InternStyleString(target.nameId);
                if (target.GetFirstOfType<IPanelComponentRootElement>() is { panelComponent: var pc }
                    && pc is UnityEngine.Object pcObj && pcObj != null)
                    info.targetEntityId = pcObj.GetEntityId();
            }

            info.eventKind = (byte)UIToolkitProfilerEventTypeMap.PopulateEventInfo(evt, ref info);
            info.eventNameIndex = InternEventTypeName(evt.GetType());
            EnqueuePanelEvent(in info);
        }

        // Appends a captured event to the per-frame buffer. Called only from CapturePanelEvent.
        static void EnqueuePanelEvent(in UIToolkitPanelEventInfo info)
        {
            if (s_PendingEventsCount == s_PendingEvents.Length)
                Array.Resize(ref s_PendingEvents, s_PendingEvents.Length * 2);
            s_PendingEvents[s_PendingEventsCount++] = info;
        }

        // Called from CapturePanelEvent for every captured event. Records the event Type so its name
        // can be displayed, returning the 1-based pool reference stored in the event's eventNameIndex.
        // De-duplicated per session so each type (e.g. PointerMoveEvent) emits one name for the whole
        // capture, not once per frame. Returns the overflow sentinel (read back as "Unknown") if the
        // pool is full — see AddInternedString.
        static ushort InternEventTypeName(Type eventType)
        {
            if (s_EventTypeNameIndices.TryGetValue(eventType, out var stored))
                return stored;
            // Cache the result even when it's the overflow sentinel: a later event of the same type
            // then resolves in one lookup instead of re-hitting AddInternedString. A cached 0xFFFF
            // flows straight back to the UInt16 field and reads as "Unknown".
            stored = AddInternedString(eventType.Name);
            s_EventTypeNameIndices[eventType] = stored;
            return stored;
        }

        // Called from CapturePanelEvent for the target VisualElement's type and instance name. The id
        // is a pre-interned UniqueStyleString id (VisualElement.typeNameId / nameId), so de-duplication
        // is an int dictionary lookup and the backing string is fetched without re-hashing. Returns the
        // 1-based pool reference stored in the event's targetTypeNameIndex / targetElementNameIndex;
        // shares the session string pool with InternEventTypeName. Returns the overflow sentinel (read
        // back as "Unknown") if the pool is full — see AddInternedString.
        static ushort InternStyleString(int uniqueStyleStringId)
        {
            if (s_StyleStringIndices.TryGetValue(uniqueStyleStringId, out var stored))
                return stored;
            // Cache the result even when it's the overflow sentinel (see InternEventTypeName).
            stored = AddInternedString(new UniqueStyleString(uniqueStyleStringId).value);
            s_StyleStringIndices[uniqueStyleStringId] = stored;
            return stored;
        }

        // Appends a never-before-seen string to the shared pool and returns its 1-based reference
        // (pool position + 1), or k_InternedStringOverflow (0xFFFF) when the pool can't grow without a
        // reference aliasing 0xFFFF or overflowing the events' UInt16 fields. Never returns 0 — that
        // value is reserved for "none". Callers cache the result keyed by their source identity —
        // including the overflow sentinel — so once full, repeat lookups of an overflowed key resolve
        // in one dictionary hit and read back as "Unknown". A pathological session with more than
        // k_InternedStringPoolMax distinct strings can therefore grow the front dictionaries past the
        // pool size; both are dropped when the profiler stops (see SetActiveCaptureMode).
        static ushort AddInternedString(string value)
        {
            var index = s_InternedStrings.Count;
            if (index >= k_InternedStringPoolMax)
                return k_InternedStringOverflow;
            s_InternedStrings.Add(value);
            return (ushort)(index + 1);
        }

        // Called once per frame from RecordForCapture (which itself fires from
        // ProfilerUIToolkit::CaptureFrame on the native side). Pins the buffer, hands it to native
        // for emission + counter bump, then clears it. No allocation in the common path.
        internal static unsafe void FlushPendingEvents()
        {
            if (s_PendingEventsCount > 0)
            {
                fixed (UIToolkitPanelEventInfo* ptr = s_PendingEvents)
                    Native_EmitPanelEvents(ptr, s_PendingEventsCount);
                s_PendingEventsCount = 0;
            }

            // Emit only the strings interned since the last flush (the high-water mark), in index
            // order, as session metadata — so each distinct string is encoded and emitted exactly
            // once per capture session rather than once per frame. The session chunk ordinal matches
            // the index stored on the events. Order relative to the PANEL_EVENTS emission is
            // independent (the reader keys strings by ordinal, not interleaving). The intern tables are
            // not cleared here; they persist for the whole session and are reset when the profiler
            // stops (see SetActiveCaptureMode).
            //
            // Only never-before-seen strings are encoded here (a handful per session), so a small stack
            // buffer suffices. Allocate it once before the loop — a stackalloc inside the loop would
            // accumulate on the stack until this method returns — and fall back to the heap only for a
            // type name too long for the 128-byte buffer (effectively never for a C# type name).
            if (s_EmittedInternedStringCount < s_InternedStrings.Count)
            {
                Span<byte> stackUtf8 = stackalloc byte[128];
                for (var i = s_EmittedInternedStringCount; i < s_InternedStrings.Count; i++)
                {
                    var name = s_InternedStrings[i];
                    var maxBytes = Encoding.UTF8.GetMaxByteCount(name.Length);
                    Span<byte> utf8 = maxBytes <= stackUtf8.Length ? stackUtf8 : new byte[maxBytes];
                    var byteCount = Encoding.UTF8.GetBytes(name, utf8);
                    fixed (byte* p = utf8)
                        Native_EmitEventTypeName(p, byteCount);
                }
                s_EmittedInternedStringCount = s_InternedStrings.Count;
            }
        }

        // Pure managed check against the cached mode (kept in sync from native NewFrame). No
        // native crossing per query — important for the per-panel hot path in EvaluateChain.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ShouldCapturePanel(bool isEditorPanel)
        {
            var mode = s_ActiveCaptureMode;
            if (mode == CaptureMode.Disabled)
                return false;
            return mode == CaptureMode.EditorAndPlaymode || !isEditorPanel; // PlaymodeOnly: skip editor panels
        }

        // Emit the per-panel batch metrics + flat owners arrays for one EvaluateChain pass. Called
        // once per panel render (after the final KickRanges) — not per batch. Both chunks are
        // emitted as a pair so the reader can pair them by per-frame ordinal; the owners chunk may
        // legitimately be empty for a flat panel with no IPanelComponent contributors. Skipped
        // entirely when batches is empty so we don't insert a phantom ordinal slot.
        // Caller must have already gated this via ShouldCapturePanel.
        internal static unsafe void EmitBatchMetricsForPanel(NativeArray<UIToolkitBatchMetricsInfo> batches, NativeArray<EntityId> owners)
        {
            if (batches.Length == 0)
                return;

            Native_EmitBatchMetricsForPanel(
                (UIToolkitBatchMetricsInfo*)batches.GetUnsafeReadOnlyPtr(), batches.Length,
                (EntityId*)owners.GetUnsafeReadOnlyPtr(), owners.Length);
        }

        // Caller must have already gated this via ShouldCapturePanel.
        internal static void AddPanelUpdateMetrics(EntityId panelId, UInt32 hierarchyVersionChanges, UInt32 repaintVersionChanges, Int32 veCount)
        {
            // Single P/Invoke: native side updates global counters and emits the PANEL_METRICS
            // chunk in one call.
            var info = new UIToolkitPanelUpdateMetricsInfo
            {
                panelEntityId = panelId,
                hierarchyVersionChanges = hierarchyVersionChanges,
                repaintVersionChanges = repaintVersionChanges,
                visualElementCount = veCount,
            };
            Native_AddPanelUpdateMetrics(ref info);
        }

        [AOT.MonoPInvokeCallback(typeof(RecordProfilerPanelMetadataForCaptureDelegate))]
        internal static void RecordProfilerPanelMetadataForCapture()
        {
            ProfilerUIToolkitPanelMetadataCapture.RecordForCapture();
        }
    }

    /// <summary>
    /// Implementation for <see cref="ProfilerUIToolkit.RecordProfilerPanelMetadataForCapture"/>. Kept
    /// separate so the native entry point does not reference types that pull in blacklisted UI
    /// controls via static analysis.
    /// </summary>
    internal static class ProfilerUIToolkitPanelMetadataCapture
    {
        static EntityId[] s_PanelEntityIdsScratch;

        static void EnsurePanelEntityIdsScratchCapacity(int minLength)
        {
            if (s_PanelEntityIdsScratch != null && s_PanelEntityIdsScratch.Length >= minLength)
                return;
            var newSize = minLength;
            if (s_PanelEntityIdsScratch != null && s_PanelEntityIdsScratch.Length > 0)
                newSize = Math.Max(minLength, s_PanelEntityIdsScratch.Length * 2);
            s_PanelEntityIdsScratch = new EntityId[newSize];
        }

        internal static void RecordForCapture()
        {
            if (!Profiler.enabled)
                return;

            // Honor PlaymodeOnly capture mode: skip the entire editor-panel pass (including
            // PANEL_ENTRIES emission) so editor data doesn't leak into a player-targeted capture.
            if (ProfilerUIToolkit.ShouldCapturePanel(isEditorPanel: true))
            {
                EnsurePanelEntityIdsScratchCapacity(1);
                var iterator = UIElementsUtility.GetPanelsIterator();
                while (iterator.MoveNext())
                {
                    var panel = iterator.Current.Value;
                    if (panel.contextType != ContextType.Editor)
                        continue;
                    var pid = panel.ownerObject != null ? panel.ownerObject.GetEntityId() : EntityId.None;
                    s_PanelEntityIdsScratch[0] = pid;
                    ProfilerUIToolkit.EmitProfilerPanelMetadata(s_PanelEntityIdsScratch, 1);
                    EmitPanelUpdateMetricsForPanel(panel as Panel, pid);
                }
            }
            // Player panels are always captured when the profiler is enabled (both EditorAndPlaymode
            // and PlaymodeOnly include them). GetSortedPlayerPanels returns UIElementsRuntimeUtility's
            // cached list (not a new collection per call). Indexing avoids any doubt about
            // IEnumerable enumerator boxing; List<T>.Enumerator is a struct anyway.
            var playerPanels = UIElementsRuntimeUtility.GetSortedPlayerPanels();
            for (var i = 0; i < playerPanels.Count; i++)
                RecordProfilerMetadataForPlayerPanel(playerPanels[i]);

            // Drain the EventDispatcher event queue once all panel chunks are out. PANEL_EVENTS is
            // a single per-frame chunk holding events for every panel, so it intentionally lives
            // outside the per-panel emission loop above.
            ProfilerUIToolkit.FlushPendingEvents();
        }

        static void RecordProfilerMetadataForPlayerPanel(BaseRuntimePanel panel)
        {
            var pid = panel.ownerObject != null ? panel.ownerObject.GetEntityId() : EntityId.None;
            if (panel is RuntimePanel runtimePanel && runtimePanel.panelComponents.Count > 0)
            {
                var n = runtimePanel.panelComponents.Count;
                var count = 1 + n;
                EnsurePanelEntityIdsScratchCapacity(count);
                s_PanelEntityIdsScratch[0] = pid;
                for (var i = 0; i < n; i++)
                {
                    var comp = runtimePanel.panelComponents[i];
                    s_PanelEntityIdsScratch[1 + i] = (comp as Object) != null ? ((Object)comp).GetEntityId() : EntityId.None;
                }

                ProfilerUIToolkit.EmitProfilerPanelMetadata(s_PanelEntityIdsScratch, count);
            }
            else
            {
                EnsurePanelEntityIdsScratchCapacity(1);
                s_PanelEntityIdsScratch[0] = pid;
                ProfilerUIToolkit.EmitProfilerPanelMetadata(s_PanelEntityIdsScratch, 1);
            }

            EmitPanelUpdateMetricsForPanel(panel as Panel, pid);
        }

        // Caller must have already gated by ShouldCapturePanel for the appropriate panel kind.
        static void EmitPanelUpdateMetricsForPanel(Panel panel, EntityId pid)
        {
            if (panel == null)
                return;

            panel.ConsumePendingProfilerMetrics(out var hier, out var repaint, out var ve);
            ProfilerUIToolkit.AddPanelUpdateMetrics(pid, hier, repaint, ve);
        }
    }
}
