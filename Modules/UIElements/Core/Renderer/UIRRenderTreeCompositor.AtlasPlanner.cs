// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements.UIR
{
    partial class RenderTreeCompositor
    {
        sealed class AtlasPlanner
        {
            readonly ExecutionPlan m_Plan;
            readonly RenderTreeAtlas m_Atlas;
            readonly RenderTreeManager m_RenderTreeManager;
            readonly List<int> m_ForbiddenPages = new();
            readonly List<DrawOperation> m_FilterOutputs = new();

            public AtlasPlanner(ExecutionPlan plan, RenderTreeAtlas atlas, RenderTreeManager renderTreeManager)
            {
                m_Plan = plan;
                m_Atlas = atlas;
                m_RenderTreeManager = renderTreeManager;
            }

            RenderTreeAtlas.PageTraits GetPageTraits(DrawOperation op)
            {
                RenderTreeAtlas.PageTraits traits = 0;
                bool isOutput = op.isOutput;
                if (isOutput)
                    traits |= RenderTreeAtlas.PageTraits.Output;
                if (op.type == DrawOperationType.RenderTree)
                    traits |= RenderTreeAtlas.PageTraits.DepthStencil; // Only render trees use the stencil (masking)

                // Under force-gamma: a filter's last pass outputs sRGB (parent samples linear + re-applies gamma); a plain
                // nested-tree quad stays UNorm so it blends in gamma like direct rendering (UI-5094).
                bool isLastFilterPass = op.type == DrawOperationType.Effect && isOutput;
                if (m_RenderTreeManager.forceGammaRendering && !isLastFilterPass)
                    traits |= RenderTreeAtlas.PageTraits.GammaEncoded;
                return traits;
            }

            // Replays the batches twice: once to measure the live texture area, once to place the blocks. The inputs of a
            // batch are freed only after the whole batch, since its draws can run in any order. Each chunk starts by
            // recycling the pages that the previous chunks emptied.
            public void Plan()
            {
                using (k_MarkerMeasure.Auto())
                    MeasureAtlas();
                using (k_MarkerPack.Auto())
                    PackAtlas();
            }

            void MeasureAtlas()
            {
                var executionOrder = m_Plan.order;
                var batchStarts = m_Plan.batchStarts;
                m_Atlas.BeginPlan();

                ResetPlanReaders();
                for (int batch = 0; batch < batchStarts.Count; ++batch)
                {
                    int end = m_Plan.GetBatchEnd(batch);
                    for (int i = batchStarts[batch]; i < end; ++i)
                    {
                        var op = executionOrder[i];
                        if (NeedsAtlasBlock(op))
                            m_Atlas.MeasureAlloc(GetPageTraits(op), op.dstAtlasBlock);
                    }
                    m_Atlas.EndMeasureBatch();

                    for (int i = batchStarts[batch]; i < end; ++i)
                    {
                        var inputs = executionOrder[i].inputs;
                        for (int j = 0; j < inputs.Count; ++j)
                        {
                            var input = inputs[j];
                            if (--input.planReaders == 0 && NeedsAtlasBlock(input))
                                m_Atlas.MeasureFree(GetPageTraits(input), input.dstAtlasBlock);
                        }
                    }
                }
                m_Atlas.EndMeasure();
            }

            void PackAtlas()
            {
                var executionOrder = m_Plan.order;
                var batchStarts = m_Plan.batchStarts;
                var chunkStarts = m_Plan.chunkStarts;
                ResetPlanReaders();
                PackFilterOutputs();
                int nextChunk = 0;
                for (int batch = 0; batch < batchStarts.Count; ++batch)
                {
                    if (nextChunk < chunkStarts.Count && chunkStarts[nextChunk] == batch)
                    {
                        m_Atlas.RecycleEmptyPages();
                        ++nextChunk;
                    }

                    int end = m_Plan.GetBatchEnd(batch);
                    CollectBatchReadPages(batchStarts[batch], end);
                    for (int i = batchStarts[batch]; i < end; ++i)
                    {
                        var op = executionOrder[i];
                        if (!NeedsAtlasBlock(op))
                            continue;
                        if (!op.hasAtlasBlock)
                            m_Atlas.Allocate(GetPageTraits(op), ref op.dstAtlasBlock, m_ForbiddenPages, batch);
                        m_Atlas.NoteUse(op.dstAtlasBlock.page, batch);
                    }

                    for (int i = batchStarts[batch]; i < end; ++i)
                    {
                        var inputs = executionOrder[i].inputs;
                        for (int j = 0; j < inputs.Count; ++j)
                        {
                            var input = inputs[j];
                            if (!input.hasAtlasBlock)
                                continue;
                            m_Atlas.NoteUse(input.dstAtlasBlock.page, batch);
                            if (--input.planReaders == 0)
                                m_Atlas.Free(input.dstAtlasBlock);
                        }
                    }
                }
                m_Atlas.EndPlan();
            }

            // Filter outputs stay alive all frame, so they are packed first, tallest first across all chunks: chunk by chunk,
            // their pages would get the sizes in several sorted runs, which fragments the packer again. They only read pages
            // of other traits, so they need no forbidden pages.
            void PackFilterOutputs()
            {
                var executionOrder = m_Plan.order;
                m_FilterOutputs.Clear();
                for (int i = 0; i < executionOrder.Count; ++i)
                {
                    var op = executionOrder[i];
                    if (op.type == DrawOperationType.Effect && op.isOutput && NeedsAtlasBlock(op))
                    {
                        op.sortIndex = m_FilterOutputs.Count;
                        m_FilterOutputs.Add(op);
                    }
                }
                m_FilterOutputs.Sort(k_TallestBlockFirst);

                m_ForbiddenPages.Clear();
                for (int i = 0; i < m_FilterOutputs.Count; ++i)
                {
                    var op = m_FilterOutputs[i];
                    m_Atlas.Allocate(GetPageTraits(op), ref op.dstAtlasBlock, m_ForbiddenPages, 0);
                }
            }

            void ResetPlanReaders()
            {
                var executionOrder = m_Plan.order;
                for (int i = 0; i < executionOrder.Count; ++i)
                {
                    var op = executionOrder[i];
                    op.planReaders = op.readers.Count;
                }
            }

            // A draw never reads from the page it writes to. The ops of a batch avoid all the pages the batch reads: they
            // can then share a target page, and the next batch reads fewer pages, which draws less.
            void CollectBatchReadPages(int start, int end)
            {
                m_ForbiddenPages.Clear();
                var executionOrder = m_Plan.order;
                for (int i = start; i < end; ++i)
                    AddReadPages(executionOrder[i]);
            }

            void AddReadPages(DrawOperation op)
            {
                var inputs = op.inputs;
                for (int i = 0; i < inputs.Count; ++i)
                {
                    if (inputs[i].hasAtlasBlock)
                        AddForbiddenPage(inputs[i].dstAtlasBlock.page);
                }

                // A render tree draws the quads of its nested trees
                if (op.type == DrawOperationType.RenderTree)
                {
                    for (var child = op.firstChild; child != null; child = child.nextSibling)
                    {
                        if (child.hasAtlasBlock)
                            AddForbiddenPage(child.dstAtlasBlock.page);
                    }
                }
            }

            void AddForbiddenPage(int page)
            {
                if (!m_ForbiddenPages.Contains(page))
                    m_ForbiddenPages.Add(page);
            }

            // For Testing.AnyOperationReadsItsOwnPage
            public bool ReadsItsOwnPage(DrawOperation op)
            {
                m_ForbiddenPages.Clear();
                AddReadPages(op);
                return m_ForbiddenPages.Contains(op.dstAtlasBlock.page);
            }
        }
    }
}
