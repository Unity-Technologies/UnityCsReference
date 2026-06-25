// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UA4000
using System;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    // Drives the per-element mesh modifier chains. Owns the defer-and-return state machine that lets
    // an element park while its scheduled jobs run, so other dirty elements can keep advancing on the
    // main thread.
    class MeshModifierScheduler : IDisposable
    {
        enum SchedulerState
        {
            Ready,
            AwaitingJob,
            Complete
        }

        sealed class ElementState
        {
            public Entry rootEntry;
            public VisualElement element;
            public List<MeshModifierRegistration> chain;
            public int chainIndex;
            public JobHandle combined;
            public SchedulerState state;
        }

        static readonly ProfilerMarker k_RunMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, "UIR.MeshModifierScheduler.Run");

        readonly List<Entry> m_DrawBuffer = new(16);
        readonly List<ElementState> m_ActiveStates = new();
        readonly Stack<ElementState> m_StatePool = new();
        readonly JobMerger m_CallbackScratch = new(64);

        public void RegisterDirtyElement(Entry rootEntry, RenderData renderData)
        {
            if (renderData.isSubTreeQuad)
                return;

            var chain = renderData.m_EffectiveModifiers;
            if (chain == null || chain.Count == 0)
                return;

            var state = AcquireState();
            state.rootEntry = rootEntry;
            state.element = renderData.owner;
            state.chain = chain;
            state.chainIndex = 0;
            state.combined = default;
            state.state = SchedulerState.Ready;
            m_ActiveStates.Add(state);
        }

        public void Run(TempMeshAllocatorImpl allocator, ExtraVertexChannels panelExtras)
        {
            using (k_RunMarker.Auto())
            {
                try
                {
                    if (m_ActiveStates.Count == 0)
                        return;
                    DriveStateMachine(allocator, panelExtras);
                }
                finally
                {
                    ReleaseActiveStates();
                    m_DrawBuffer.Clear();
                }
            }
        }

        void DriveStateMachine(TempMeshAllocatorImpl allocator, ExtraVertexChannels panelExtras)
        {
            while (m_ActiveStates.Count > 0)
            {
                bool advancedAny = DrainReady(allocator, panelExtras) | PromoteCompletedWaiters();
                if (advancedAny)
                    continue;

                // No ready work and no waiter finished naturally — block on the first parked element's
                // handles so the pass keeps making progress.
                BlockOnFirstWaiter();
            }
        }

        bool DrainReady(TempMeshAllocatorImpl allocator, ExtraVertexChannels panelExtras)
        {
            // Iterate backwards so RemoveAtSwapBack doesn't disturb still-to-visit indices.
            bool any = false;
            for (int i = m_ActiveStates.Count - 1; i >= 0; --i)
            {
                var state = m_ActiveStates[i];
                if (state.state != SchedulerState.Ready)
                    continue;
                any = true;
                AdvanceElement(state, allocator, panelExtras);
                if (state.state == SchedulerState.Complete)
                    RemoveAtSwapBack(i);
            }
            return any;
        }

        void AdvanceElement(ElementState state, TempMeshAllocatorImpl allocator, ExtraVertexChannels panelExtras)
        {
            m_DrawBuffer.Clear();
            CollectDrawEntries(state.rootEntry, m_DrawBuffer);

            while (state.chainIndex < state.chain.Count)
            {
                if (state.element.elementPanel == null)
                {
                    state.state = SchedulerState.Complete;
                    return;
                }

                var reg = state.chain[state.chainIndex++];

                try
                {
                    var ctx = new MeshModificationContext
                    {
                        element = state.element,
                        drawsBuffer = m_DrawBuffer,
                        allocator = allocator,
                        panelExtras = panelExtras,
                        pendingHandles = m_CallbackScratch
                    };
                    reg.callback(ctx);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    m_CallbackScratch.MergeAndReset().Complete();
                    state.state = SchedulerState.Complete;
                    return;
                }

                if (m_CallbackScratch.count > 0)
                {
                    state.combined = m_CallbackScratch.MergeAndReset();
                    state.state = SchedulerState.AwaitingJob;
                    return;
                }
            }

            state.state = SchedulerState.Complete;
        }

        bool PromoteCompletedWaiters()
        {
            bool any = false;
            for (int i = 0; i < m_ActiveStates.Count; ++i)
            {
                var state = m_ActiveStates[i];
                if (state.state != SchedulerState.AwaitingJob)
                    continue;
                if (!state.combined.IsCompleted)
                    continue;
                state.combined.Complete();
                state.combined = default;
                state.state = SchedulerState.Ready;
                any = true;
            }
            return any;
        }

        void BlockOnFirstWaiter()
        {
            for (int i = 0; i < m_ActiveStates.Count; ++i)
            {
                var state = m_ActiveStates[i];
                if (state.state != SchedulerState.AwaitingJob)
                    continue;
                state.combined.Complete();
                state.combined = default;
                state.state = SchedulerState.Ready;
                return;
            }
        }

        // Drop the Complete state at i out of m_ActiveStates and back into the pool. Uses swap-with-last
        // so the cost is O(1) regardless of position; safe inside backward-iterating loops.
        void RemoveAtSwapBack(int i)
        {
            var s = m_ActiveStates[i];
            int last = m_ActiveStates.Count - 1;
            if (i != last)
                m_ActiveStates[i] = m_ActiveStates[last];
            m_ActiveStates.RemoveAt(last);
            PoolState(s);
        }

        // Called from the finally block of Run() to reap anything that didn't reach Complete via the normal
        // path (e.g. an exception aborted DriveStateMachine partway through). Defensive only.
        void ReleaseActiveStates()
        {
            for (int i = 0; i < m_ActiveStates.Count; ++i)
            {
                var s = m_ActiveStates[i];
                if (s.state == SchedulerState.AwaitingJob)
                    s.combined.Complete();
                PoolState(s);
            }
            m_ActiveStates.Clear();
        }

        void PoolState(ElementState s)
        {
            s.combined = default;
            s.chain = null;
            s.element = null;
            s.rootEntry = null;
            m_StatePool.Push(s);
        }

        ElementState AcquireState()
            => m_StatePool.Count > 0 ? m_StatePool.Pop() : new ElementState();

        static void CollectDrawEntries(Entry e, List<Entry> buffer)
        {
            if (e == null)
                return;
            if (IsDrawEntry(e.type))
                buffer.Add(e);
            for (var c = e.firstChild; c != null; c = c.nextSibling)
                CollectDrawEntries(c, buffer);
        }

        static bool IsDrawEntry(EntryType type)
            => type == EntryType.DrawSolidMesh
            || type == EntryType.DrawTexturedMesh
            || type == EntryType.DrawTextMesh
            || type == EntryType.DrawGradients;

        public void Dispose()
        {
            m_CallbackScratch.Dispose();
        }
    }
}
