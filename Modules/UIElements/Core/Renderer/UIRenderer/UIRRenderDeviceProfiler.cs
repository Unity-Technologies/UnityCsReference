// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR
{
    // Per-EvaluateChain profiler accumulator. EvaluateChain gates subsequent calls on the
    // BeginPanel return value.
    //
    // Per-batch counters (draw calls, vertices, indices, ...) are derived from the host
    // device's DrawStatistics via baseline snapshots: BeginPanel records the panel baseline,
    // each AppendBatch reports (current - batchBaseline) and rolls the batch baseline forward,
    // and EmitPanel reports (current - panelBaseline) for the per-panel aggregate. This keeps
    // the EvaluateChain hot loop free of a parallel set of counter increments.
    //
    // Tracks IPanelComponent activity using a lazy-capture rule: contributors are only recorded
    // at draw time, so a Begin followed by a kick doesn't credit the outgoing batch.
    internal sealed class UIRRenderDeviceProfiler : IDisposable
    {
        static readonly MemoryLabel s_ScratchLabel = new(nameof(UIElements), "UIR.ProfilerBatchScratch");

        readonly List<EntityId> m_ActivePanelComponents = new();
        readonly List<EntityId> m_BatchContributors = new();
        bool m_NeedsContributorCapture;

        NativeList<UIToolkitBatchMetricsInfo> m_BatchMetrics;
        NativeList<EntityId> m_BatchOwners;

        // Snapshots of the m_DrawStats fields we actually consume — copying the full struct on every
        // batch boundary would carry along ~7 unused counters.
        // Batch baseline: rolled forward on each AppendBatch to compute UIToolkitBatchMetricsInfo deltas.
        uint m_BatchBaseDrawCalls, m_BatchBaseVerts, m_BatchBaseIndices, m_BatchBaseImmediates, m_BatchBaseDrawRanges;
        // Panel baseline: frozen at BeginPanel, drives the per-panel AddBatchAggregateCounters totals.
        uint m_PanelBaseDrawCalls, m_PanelBaseVerts, m_PanelBaseIndices;

        // Caller is responsible for gating on UIRUtility.k_ProfilerSupported and
        // ProfilerUIToolkit.ShouldCapturePanel — keeps those checks at the call site where the
        // panel reference and editor-status are already in scope, and lets the JIT short-circuit
        // the entire profiler path when the static-readonly gate is false.
        public void BeginPanel(in UIRenderDevice.DrawStatistics current = default)
        {
            m_ActivePanelComponents.Clear();
            m_BatchContributors.Clear();
            m_NeedsContributorCapture = false;
            m_BatchMetrics ??= new NativeList<UIToolkitBatchMetricsInfo>(64, s_ScratchLabel);
            m_BatchOwners ??= new NativeList<EntityId>(256, s_ScratchLabel);
            m_BatchMetrics.Clear();
            m_BatchOwners.Clear();
            m_BatchBaseDrawCalls = m_PanelBaseDrawCalls = current.drawCommandCount;
            m_BatchBaseVerts = m_PanelBaseVerts = current.totalVertices;
            m_BatchBaseIndices = m_PanelBaseIndices = current.totalIndices;
            m_BatchBaseImmediates = current.immediateDraws;
            m_BatchBaseDrawRanges = current.drawRangeCount;
        }

        internal bool TryGetAccumulated(
            out NativeArray<UIToolkitBatchMetricsInfo> batches,
            out NativeArray<EntityId> owners)
        {
            if (m_BatchMetrics == null)
            {
                batches = default;
                owners = default;
                return false;
            }
            batches = m_BatchMetrics.GetBuffer().GetSubArray(0, m_BatchMetrics.Count);
            owners = m_BatchOwners.GetBuffer().GetSubArray(0, m_BatchOwners.Count);
            return true;
        }

        public void BeginComponent(EntityId componentId)
        {
            m_ActivePanelComponents.Add(componentId);
            m_NeedsContributorCapture = true;
        }

        public void EndComponent()
        {
            Debug.Assert(m_ActivePanelComponents.Count > 0, "EndComponent without matching BeginComponent");
            m_ActivePanelComponents.RemoveAt(m_ActivePanelComponents.Count - 1);
        }

        public void OnDraw()
        {
            if (!m_NeedsContributorCapture)
                return;
            for (var i = 0; i < m_ActivePanelComponents.Count; i++)
            {
                var id = m_ActivePanelComponents[i];
                if (!m_BatchContributors.Contains(id))
                    m_BatchContributors.Add(id);
            }
            m_NeedsContributorCapture = false;
        }

        public void AppendBatch(EntityId panelId, bool isRenderingNestedTreeRT, KickRangesReason kickRangesReason, in UIRenderDevice.DrawStatistics current)
        {
            var info = new UIToolkitBatchMetricsInfo
            {
                panelEntityId = panelId,
                drawCallCount = current.drawCommandCount - m_BatchBaseDrawCalls,
                vertexCount = current.totalVertices - m_BatchBaseVerts,
                indexCount = current.totalIndices - m_BatchBaseIndices,
                immediateDraws = current.immediateDraws - m_BatchBaseImmediates,
                drawRangeCount = current.drawRangeCount - m_BatchBaseDrawRanges,
                kickRangesReason = (byte)kickRangesReason,
                isRenderingNestedTreeRT = isRenderingNestedTreeRT ? (byte)1 : (byte)0,
                ownerOffset = (uint)m_BatchOwners.Count,
                ownerCount = (uint)m_BatchContributors.Count,
            };
            m_BatchMetrics.Add(ref info);
            for (var i = 0; i < m_BatchContributors.Count; i++)
            {
                var ownerId = m_BatchContributors[i];
                m_BatchOwners.Add(ref ownerId);
            }

            m_BatchBaseDrawCalls = current.drawCommandCount;
            m_BatchBaseVerts = current.totalVertices;
            m_BatchBaseIndices = current.totalIndices;
            m_BatchBaseImmediates = current.immediateDraws;
            m_BatchBaseDrawRanges = current.drawRangeCount;

            // Re-arm contributor capture if any components are still active.
            m_BatchContributors.Clear();
            m_NeedsContributorCapture = m_ActivePanelComponents.Count > 0;
        }

        public void EmitPanel(in UIRenderDevice.DrawStatistics current)
        {
            if (m_BatchMetrics == null || m_BatchMetrics.Count == 0)
                return;
            TryGetAccumulated(out var batchView, out var ownerView);
            ProfilerUIToolkit.EmitBatchMetricsForPanel(batchView, ownerView);
            ProfilerUIToolkit.AddBatchAggregateCounters(
                (uint)m_BatchMetrics.Count,
                current.drawCommandCount - m_PanelBaseDrawCalls,
                current.totalVertices - m_PanelBaseVerts,
                current.totalIndices - m_PanelBaseIndices);
        }

        public void Dispose()
        {
            m_BatchMetrics?.Dispose();
            m_BatchOwners?.Dispose();
        }
    }
}
