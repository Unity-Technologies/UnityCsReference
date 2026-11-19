// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements.UIR
{
    partial class RenderTreeCompositor
    {
        sealed class BatchScheduler
        {
            // The ready ops that draw with the same material and pass
            class ReadyBucket
            {
                public Material material;
                public int passIndex;
                public bool hasSettingsCallback;
                public bool batchable; // Follows from the other keys: computed once per bucket, as it reads the material

                public bool queued;
                public readonly List<DrawOperation> ops = new();
                public int unscheduled; // The ops of the scheduling chunk with this key that haven't run
            }

            readonly ExecutionPlan m_Plan;
            readonly List<ReadyBucket> m_Buckets = new();
            int m_BucketCount;
            readonly List<ReadyBucket> m_BucketQueue = new();
            readonly List<DrawOperation> m_PostOrder = new();
            readonly List<DrawOperation> m_Emitting = new();

            // The chunk budget, in pixels, is the area of non-output regions that a chunk keeps alive, which bounds the
            // transient memory
            const long k_PixelsPerMegapixel = 1024 * 1024;
            const long k_DefaultChunkBudget = k_PixelsPerMegapixel;

            int m_SchedulingChunk;

            public BatchScheduler(ExecutionPlan plan)
            {
                m_Plan = plan;
            }

            // Chunk by chunk, reorders the execution so that each op runs as soon as the ops it reads have run, whatever its
            // nesting depth, and the ready ops with the same material and pass run together. Buckets run in the order they
            // became ready.
            public void Schedule(FilterTextureBudget chunkBudget)
            {
                var executionOrder = m_Plan.order;
                var batchStarts = m_Plan.batchStarts;
                var chunkStarts = m_Plan.chunkStarts;
                batchStarts.Clear();
                chunkStarts.Clear();
                if (Testing.disableBatching)
                {
                    for (int i = 0; i < executionOrder.Count; ++i)
                        batchStarts.Add(i);
                    return;
                }

                m_PostOrder.Clear();
                m_PostOrder.AddRange(executionOrder);
                executionOrder.Clear();
                m_BucketCount = 0;

                int chunkCount = AssignChunks(ResolveChunkBudget(chunkBudget));
                for (int i = 0; i < m_PostOrder.Count; ++i)
                {
                    var op = m_PostOrder[i];
                    op.pendingDependencies = op.type == DrawOperationType.Effect ? op.inputs.Count : CountChildren(op);
                }

                int next = 0;
                for (m_SchedulingChunk = 0; m_SchedulingChunk < chunkCount; ++m_SchedulingChunk)
                {
                    chunkStarts.Add(batchStarts.Count);
                    int chunkStart = next;
                    for (; next < m_PostOrder.Count && m_PostOrder[next].chunk == m_SchedulingChunk; ++next)
                        ++FindBucket(m_PostOrder[next]).unscheduled;
                    for (int i = chunkStart; i < next; ++i)
                    {
                        if (m_PostOrder[i].pendingDependencies == 0)
                            MakeReady(m_PostOrder[i]);
                    }
                    EmitReadyBuckets();
                }

                Debug.Assert(executionOrder.Count == m_PostOrder.Count, "Every op must be scheduled once.");
                for (int i = 0; i < m_BucketCount; ++i)
                    m_Buckets[i].material = null;
            }

            // A unit is an element's content and passes, which follow the units nested in it in post-order: a unit only
            // reads the outputs of the units before it, and outputs stay alive. Consecutive units go in the same chunk
            // while the area of their regions that aren't outputs fits the budget.
            int AssignChunks(long budget)
            {
                int chunk = 0;
                long chunkArea = 0;
                long unitArea = 0;
                int unitStart = 0;
                for (int i = 0; i < m_PostOrder.Count; ++i)
                {
                    var op = m_PostOrder[i];
                    if (!op.isOutput)
                    {
                        if (NeedsAtlasBlock(op))
                            unitArea += (long)op.dstAtlasBlock.regionWidth * op.dstAtlasBlock.regionHeight;
                        continue;
                    }

                    if (chunkArea > 0 && chunkArea + unitArea > budget)
                    {
                        ++chunk;
                        chunkArea = 0;
                    }
                    chunkArea += unitArea;
                    for (int j = unitStart; j <= i; ++j)
                        m_PostOrder[j].chunk = chunk;
                    unitStart = i + 1;
                    unitArea = 0;
                }

                Debug.Assert(unitStart == m_PostOrder.Count, "Every unit must end with its output.");
                return m_PostOrder.Count > 0 ? chunk + 1 : 0;
            }

            static long ResolveChunkBudget(FilterTextureBudget budget)
            {
                if (Testing.chunkBudget > 0)
                    return Testing.chunkBudget;
                return budget switch
                {
                    FilterTextureBudget.HalfMegapixel => k_PixelsPerMegapixel / 2,
                    FilterTextureBudget.OneMegapixel => k_PixelsPerMegapixel,
                    FilterTextureBudget.TwoMegapixels => 2 * k_PixelsPerMegapixel,
                    FilterTextureBudget.FourMegapixels => 4 * k_PixelsPerMegapixel,
                    FilterTextureBudget.EightMegapixels => 8 * k_PixelsPerMegapixel,
                    _ => k_DefaultChunkBudget,
                };
            }

            void EmitReadyBuckets()
            {
                var executionOrder = m_Plan.order;
                var batchStarts = m_Plan.batchStarts;
                while (m_BucketQueue.Count > 0)
                {
                    int index = FindCompleteBucket();
                    var bucket = m_BucketQueue[index];
                    m_BucketQueue.RemoveAt(index);
                    bucket.queued = false;
                    m_Emitting.Clear();
                    m_Emitting.AddRange(bucket.ops);
                    for (int i = 0; i < m_Emitting.Count; ++i)
                        m_Emitting[i].sortIndex = i;
                    m_Emitting.Sort(k_TallestBlockFirst);
                    bucket.ops.Clear();
                    bucket.unscheduled -= m_Emitting.Count;

                    for (int i = 0; i < m_Emitting.Count; ++i)
                    {
                        if (i == 0 || !bucket.batchable)
                            batchStarts.Add(executionOrder.Count);
                        executionOrder.Add(m_Emitting[i]);
                    }

                    for (int i = 0; i < m_Emitting.Count; ++i)
                        CompleteScheduling(m_Emitting[i]);
                }
            }

            // A bucket waits while ops of its chunk with the same key aren't ready, unless all ready buckets wait
            int FindCompleteBucket()
            {
                for (int i = 0; i < m_BucketQueue.Count; ++i)
                {
                    if (m_BucketQueue[i].unscheduled == m_BucketQueue[i].ops.Count)
                        return i;
                }
                return 0;
            }

            static int CountChildren(DrawOperation op)
            {
                int count = 0;
                for (var child = op.firstChild; child != null; child = child.nextSibling)
                    ++count;
                return count;
            }

            void CompleteScheduling(DrawOperation op)
            {
                var readers = op.readers;
                for (int i = 0; i < readers.Count; ++i)
                    CompleteDependency(readers[i]);

                // A render tree draws the quads of its outputs; the root op has no parent and isn't scheduled
                var parent = op.parent;
                if (op.isOutput && parent.parent != null)
                    CompleteDependency(parent);
            }

            // An op of a later chunk waits for its chunk to start
            void CompleteDependency(DrawOperation op)
            {
                if (--op.pendingDependencies == 0 && op.chunk == m_SchedulingChunk)
                    MakeReady(op);
            }

            void MakeReady(DrawOperation op)
            {
                var bucket = FindBucket(op);
                bucket.ops.Add(op);
                if (!bucket.queued)
                {
                    bucket.queued = true;
                    m_BucketQueue.Add(bucket);
                }
            }

            ReadyBucket FindBucket(DrawOperation op)
            {
                bool isEffect = op.type == DrawOperationType.Effect;
                ref var pass = ref op.FilterPass;
                var material = isEffect ? pass.material : null;
                int passIndex = isEffect ? pass.passIndex : -1;
                bool hasSettingsCallback = isEffect && pass.applySettingsCallback != null;

                ReadyBucket bucket = null;
                for (int i = 0; i < m_BucketCount; ++i)
                {
                    var candidate = m_Buckets[i];
                    if (ReferenceEquals(candidate.material, material) && candidate.passIndex == passIndex && candidate.hasSettingsCallback == hasSettingsCallback)
                    {
                        bucket = candidate;
                        break;
                    }
                }

                if (bucket == null)
                {
                    if (m_BucketCount == m_Buckets.Count)
                        m_Buckets.Add(new ReadyBucket());
                    bucket = m_Buckets[m_BucketCount++];
                    bucket.material = material;
                    bucket.passIndex = passIndex;
                    bucket.hasSettingsCallback = hasSettingsCallback;
                    bucket.batchable = isEffect ? FilterHelper.CanDrawInstanced(pass) : !Testing.disableTreeGrouping;
                    bucket.ops.Clear();
                    bucket.queued = false;
                    bucket.unscheduled = 0;
                }
                return bucket;
            }
        }
    }
}
