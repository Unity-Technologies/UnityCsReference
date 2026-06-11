// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
