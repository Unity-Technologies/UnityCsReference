// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR.Implementation
{
    static class CommandGenerator
    {
        static readonly ProfilerMarker k_GenerateEntries = new ProfilerMarker("UIR.GenerateEntries");
        static readonly ProfilerMarker k_ConvertEntriesToCommandsMarker = new ProfilerMarker("UIR.ConvertEntriesToCommands");
        static readonly ProfilerMarker k_GenerateClosingCommandsMarker = new ProfilerMarker("UIR.GenerateClosingCommands");
        static readonly ProfilerMarker k_NudgeVerticesMarker = new ProfilerMarker("UIR.NudgeVertices");
        static readonly ProfilerMarker k_UpdateOpacityIdMarker = new ProfilerMarker("UIR.UpdateOpacityId");

        static void GetVerticesTransformInfo(VisualElement ve, out Matrix4x4 transform)
        {
            if (RenderChainVEData.AllocatesID(ve.renderChainData.transformID) || (ve.renderHints & (RenderHints.GroupTransform)) != 0)
                transform = Matrix4x4.identity;
            else if (ve.renderChainData.boneTransformAncestor != null)
                VisualElement.MultiplyMatrix34(ref ve.renderChainData.boneTransformAncestor.worldTransformInverse, ref ve.worldTransformRef, out transform);
            else if (ve.renderChainData.groupTransformAncestor != null)
                VisualElement.MultiplyMatrix34(ref ve.renderChainData.groupTransformAncestor.worldTransformInverse, ref ve.worldTransformRef, out transform);
            else transform = ve.worldTransform;
            transform.m22 = 1.0f; // Once world-space mode is introduced, this should become conditional
        }

        static bool IsParentOrAncestorOf(this VisualElement ve, VisualElement child)
        {
            // O(n) of tree depth, not very cool
            while (child.hierarchy.parent != null)
            {
                if (child.hierarchy.parent == ve)
                    return true;
                child = child.hierarchy.parent;
            }
            return false;
        }

        public unsafe static UIRStylePainter.ClosingInfo PaintElement(RenderChain renderChain, VisualElement ve, ref ChainBuilderStats stats)
        {
            var device = renderChain.device;

            var isClippingWithStencil = ve.renderChainData.clipMethod == ClipMethod.Stencil;
            var isClippingWithScissors = ve.renderChainData.clipMethod == ClipMethod.Scissor;
            if ((UIRUtility.IsElementSelfHidden(ve) && !isClippingWithStencil && !isClippingWithScissors) || ve.renderChainData.isHierarchyHidden)
            {
                if (ve.renderChainData.data != null)
                {
                    device.Free(ve.renderChainData.data);
                    ve.renderChainData.data = null;
                }
                if (ve.renderChainData.firstCommand != null)
                    ResetCommands(renderChain, ve);

                renderChain.ResetTextures(ve);

                return new UIRStylePainter.ClosingInfo();
            }

            // Retain our command insertion points if possible, to avoid paying the cost of finding them again
            RenderChainCommand oldCmdPrev = ve.renderChainData.firstCommand?.prev;
            RenderChainCommand oldCmdNext = ve.renderChainData.lastCommand?.next;
            RenderChainCommand oldClosingCmdPrev, oldClosingCmdNext;
            bool commandsAndClosingCommandsWereConsecutive = (ve.renderChainData.firstClosingCommand != null) && (oldCmdNext == ve.renderChainData.firstClosingCommand);
            if (commandsAndClosingCommandsWereConsecutive)
            {
                oldCmdNext = ve.renderChainData.lastClosingCommand.next;
                oldClosingCmdPrev = oldClosingCmdNext = null;
            }
            else
            {
                oldClosingCmdPrev = ve.renderChainData.firstClosingCommand?.prev;
                oldClosingCmdNext = ve.renderChainData.lastClosingCommand?.next;
            }
            Debug.Assert(oldCmdPrev?.owner != ve);
            Debug.Assert(oldCmdNext?.owner != ve);
            Debug.Assert(oldClosingCmdPrev?.owner != ve);
            Debug.Assert(oldClosingCmdNext?.owner != ve);

            ResetCommands(renderChain, ve);
            renderChain.ResetTextures(ve);

            k_GenerateEntries.Begin();
            var painter = renderChain.painter;
            painter.Begin(ve);

            if (ve.visible)
            {
                painter.DrawVisualElementBackground();
                painter.DrawVisualElementBorder();
                painter.ApplyVisualElementClipping();

                InvokeGenerateVisualContent(ve, painter.meshGenerationContext);
            }
            else
            {
                // Even though the element hidden, we still have to push the stencil shape or setup the scissors in case any children are visible.
                if (isClippingWithScissors || isClippingWithStencil)
                    painter.ApplyVisualElementClipping();
            }
            k_GenerateEntries.End();

            MeshHandle data = ve.renderChainData.data;

            if (painter.totalVertices > device.maxVerticesPerPage)
            {
                Debug.LogError($"A {nameof(VisualElement)} must not allocate more than {device.maxVerticesPerPage } vertices.");

                if (data != null)
                {
                    device.Free(data);
                    data = null;
                }

                renderChain.ResetTextures(ve);

                // Restart without drawing anything.
                painter.Reset();
                painter.Begin(ve);
            }

            // Convert entries to commands.
            var entries = painter.entries;
            if (entries.Count > 0)
            {
                NativeSlice<Vertex> verts = new NativeSlice<Vertex>();
                NativeSlice<UInt16> indices = new NativeSlice<UInt16>();
                UInt16 indexOffset = 0;

                if (painter.totalVertices > 0)
                    UpdateOrAllocate(ref data, painter.totalVertices, painter.totalIndices, device, out verts, out indices, out indexOffset, ref stats);

                int vertsFilled = 0, indicesFilled = 0;

                RenderChainCommand cmdPrev = oldCmdPrev, cmdNext = oldCmdNext;
                if (oldCmdPrev == null && oldCmdNext == null)
                    FindCommandInsertionPoint(ve, out cmdPrev, out cmdNext);

                // Vertex data, lazily computed
                bool vertexDataComputed = false;
                Matrix4x4 transform = Matrix4x4.identity;
                Color32 xformClipPages = new Color32(0, 0, 0, 0);
                Color32 ids = new Color32(0, 0, 0, 0);
                Color32 addFlags = new Color32(0, 0, 0, 0);
                Color32 opacityPage = new Color32(0, 0, 0, 0);
                Color32 textCoreSettingsPage = new Color32(0, 0, 0, 0);

                k_ConvertEntriesToCommandsMarker.Begin();
                int firstDisplacementUV = -1, lastDisplacementUVPlus1 = -1;
                foreach (var entry in painter.entries)
                {
                    if (entry.vertices.Length > 0 && entry.indices.Length > 0)
                    {
                        if (!vertexDataComputed)
                        {
                            vertexDataComputed = true;
                            GetVerticesTransformInfo(ve, out transform);
                            ve.renderChainData.verticesSpace = transform; // This is the space for the generated vertices below
                        }

                        Color32 transformData = renderChain.shaderInfoAllocator.TransformAllocToVertexData(ve.renderChainData.transformID);
                        Color32 opacityData = renderChain.shaderInfoAllocator.OpacityAllocToVertexData(ve.renderChainData.opacityID);
                        Color32 textCoreSettingsData = renderChain.shaderInfoAllocator.TextCoreSettingsToVertexData(ve.renderChainData.textCoreSettingsID);
                        xformClipPages.r = transformData.r;
                        xformClipPages.g = transformData.g;
                        ids.r = transformData.b;
                        opacityPage.r = opacityData.r;
                        opacityPage.g = opacityData.g;
                        ids.b = opacityData.b;
                        if (entry.isTextEntry)
                        {
                            // It's important to avoid writing these values when the vertices aren't for text,
                            // as these settings are shared with the vector graphics gradients.
                            // The same applies to the CopyTransformVertsPos* methods below.
                            textCoreSettingsPage.r = textCoreSettingsData.r;
                            textCoreSettingsPage.g = textCoreSettingsData.g;
                            ids.a = textCoreSettingsData.b;
                        }

                        Color32 clipRectData = renderChain.shaderInfoAllocator.ClipRectAllocToVertexData(entry.clipRectID);
                        xformClipPages.b = clipRectData.r;
                        xformClipPages.a = clipRectData.g;
                        ids.g = clipRectData.b;
                        addFlags.r = (byte)entry.addFlags;

                        float textureId = entry.texture.ConvertToGpu();

                        // Copy vertices, transforming them as necessary
                        var targetVerticesSlice = verts.Slice(vertsFilled, entry.vertices.Length);

                        if (entry.uvIsDisplacement)
                        {
                            if (firstDisplacementUV < 0)
                            {
                                firstDisplacementUV = vertsFilled;
                                lastDisplacementUVPlus1 = vertsFilled + entry.vertices.Length;
                            }
                            else if (lastDisplacementUVPlus1 == vertsFilled)
                                lastDisplacementUVPlus1 += entry.vertices.Length;
                            else ve.renderChainData.disableNudging = true; // Disjoint displacement UV entries, we can't keep track of them, so disable nudging optimization altogether
                        }

                        int entryIndexCount = entry.indices.Length;
                        int entryIndexOffset = vertsFilled + indexOffset;
                        var targetIndicesSlice = indices.Slice(indicesFilled, entryIndexCount);
                        bool shapeWindingIsClockwise = UIRUtility.ShapeWindingIsClockwise(entry.maskDepth, entry.stencilRef);
                        bool transformFlipsWinding = ve.renderChainData.worldFlipsWinding;

                        var job = new ConvertMeshJobData
                        {
                            vertSrc = (IntPtr)entry.vertices.GetUnsafePtr(),
                            vertDst = (IntPtr)targetVerticesSlice.GetUnsafePtr(),
                            vertCount = targetVerticesSlice.Length,
                            transform = transform,
                            transformUVs = entry.uvIsDisplacement ? 1 : 0,
                            xformClipPages = xformClipPages,
                            ids = ids,
                            addFlags = addFlags,
                            opacityPage = opacityPage,
                            textCoreSettingsPage = textCoreSettingsPage,
                            isText = entry.isTextEntry ? 1 : 0,
                            textureId = textureId,

                            indexSrc = (IntPtr)entry.indices.GetUnsafePtr(),
                            indexDst = (IntPtr)targetIndicesSlice.GetUnsafePtr(),
                            indexCount = targetIndicesSlice.Length,
                            indexOffset = entryIndexOffset,
                            flipIndices = shapeWindingIsClockwise == transformFlipsWinding ? 1 : 0
                        };
                        renderChain.jobManager.Add(ref job);

                        if (entry.isClipRegisterEntry)
                            painter.LandClipRegisterMesh(targetVerticesSlice, targetIndicesSlice, entryIndexOffset);

                        var cmd = InjectMeshDrawCommand(renderChain, ve, ref cmdPrev, ref cmdNext, data, entryIndexCount, indicesFilled, entry.material, entry.texture, entry.stencilRef);
                        if (entry.isTextEntry)
                        {
                            // Set font atlas texture gradient scale
                            cmd.state.sdfScale = entry.fontTexSDFScale;
                        }

                        vertsFilled += entry.vertices.Length;
                        indicesFilled += entryIndexCount;
                    }
                    else if (entry.customCommand != null)
                    {
                        InjectCommandInBetween(renderChain, entry.customCommand, ref cmdPrev, ref cmdNext);
                    }
                    else
                    {
                        Debug.Assert(false); // Unable to determine what kind of command to generate here
                    }
                }

                if (!ve.renderChainData.disableNudging && (firstDisplacementUV >= 0))
                {
                    ve.renderChainData.displacementUVStart = firstDisplacementUV;
                    ve.renderChainData.displacementUVEnd = lastDisplacementUVPlus1;
                }

                k_ConvertEntriesToCommandsMarker.End();
            }
            else if (data != null)
            {
                device.Free(data);
                data = null;
            }
            ve.renderChainData.data = data;

            if (painter.closingInfo.clipperRegisterIndices.Length == 0 && ve.renderChainData.closingData != null)
            {
                // No more closing data needed, so free it now
                device.Free(ve.renderChainData.closingData);
                ve.renderChainData.closingData = null;
            }

            if (painter.closingInfo.needsClosing)
            {
                k_GenerateClosingCommandsMarker.Begin();
                RenderChainCommand cmdPrev = oldClosingCmdPrev, cmdNext = oldClosingCmdNext;
                if (commandsAndClosingCommandsWereConsecutive)
                {
                    cmdPrev = ve.renderChainData.lastCommand;
                    cmdNext = cmdPrev.next;
                }
                else if (cmdPrev == null && cmdNext == null)
                    FindClosingCommandInsertionPoint(ve, out cmdPrev, out cmdNext);

                if (painter.closingInfo.PopDefaultMaterial)
                {
                    var cmd = renderChain.AllocCommand();
                    cmd.type = CommandType.PopDefaultMaterial;
                    cmd.closing = true;
                    cmd.owner = ve;
                    InjectClosingCommandInBetween(renderChain, cmd, ref cmdPrev, ref cmdNext);
                }

                if (painter.closingInfo.blitAndPopRenderTexture)
                {
                    {
                        var cmd = renderChain.AllocCommand();
                        cmd.type = CommandType.BlitToPreviousRT;
                        cmd.closing = true;
                        cmd.owner = ve;
                        cmd.state.material = GetBlitMaterial(ve.subRenderTargetMode);
                        Debug.Assert(cmd.state.material != null);
                        InjectClosingCommandInBetween(renderChain, cmd, ref cmdPrev, ref cmdNext);
                    }

                    {
                        var cmd = renderChain.AllocCommand();
                        cmd.type = CommandType.PopRenderTexture;
                        cmd.closing = true;
                        cmd.owner = ve;
                        InjectClosingCommandInBetween(renderChain, cmd, ref cmdPrev, ref cmdNext);
                    }
                }

                if (painter.closingInfo.clipperRegisterIndices.Length > 0)
                {
                    var cmd = InjectClosingMeshDrawCommand(renderChain, ve, ref cmdPrev, ref cmdNext, null, 0, 0, null, TextureId.invalid, painter.closingInfo.maskStencilRef);
                    painter.LandClipUnregisterMeshDrawCommand(cmd); // Placeholder command that will be filled actually later
                }
                if (painter.closingInfo.popViewMatrix)
                {
                    var cmd = renderChain.AllocCommand();
                    cmd.type = CommandType.PopView;
                    cmd.closing = true;
                    cmd.owner = ve;
                    InjectClosingCommandInBetween(renderChain, cmd, ref cmdPrev, ref cmdNext);
                }
                if (painter.closingInfo.popScissorClip)
                {
                    var cmd = renderChain.AllocCommand();
                    cmd.type = CommandType.PopScissor;
                    cmd.closing = true;
                    cmd.owner = ve;
                    InjectClosingCommandInBetween(renderChain, cmd, ref cmdPrev, ref cmdNext);
                }
                k_GenerateClosingCommandsMarker.End();
            }

            // When we have a closing mesh, we must have an opening mesh. At least we assumed where we decide
            // whether we must nudge or not: we only test whether the opening mesh is non-null.
            Debug.Assert(ve.renderChainData.closingData == null || ve.renderChainData.data != null);

            var closingInfo = painter.closingInfo;
            painter.Reset();
            return closingInfo;
        }

        static void InvokeGenerateVisualContent(VisualElement ve, MeshGenerationContext ctx)
        {
            Painter2D.isPainterActive = true;
            ve.InvokeGenerateVisualContent(ctx);
            Painter2D.isPainterActive = false;
        }

        static Material s_blitMaterial_LinearToGamma;
        static Material s_blitMaterial_GammaToLinear;
        static Material s_blitMaterial_NoChange;
        static Shader s_blitShader;


        static Material CreateBlitShader(float colorConversion)
        {
            if (s_blitShader == null)
                s_blitShader = Shader.Find(Shaders.k_ColorConversionBlit);

            Debug.Assert(s_blitShader != null, "UI Tollkit Render Event: Shader Not found");
            var blitMaterial = new Material(s_blitShader);
            blitMaterial.hideFlags |= HideFlags.DontSaveInEditor;
            blitMaterial.SetFloat("_ColorConversion", colorConversion);
            return blitMaterial;
        }

        static Material GetBlitMaterial(VisualElement.RenderTargetMode mode)
        {
            switch (mode)
            {
                case VisualElement.RenderTargetMode.GammaToLinear:
                    if (s_blitMaterial_GammaToLinear == null)
                        s_blitMaterial_GammaToLinear = CreateBlitShader(-1);
                    return s_blitMaterial_GammaToLinear;

                case VisualElement.RenderTargetMode.LinearToGamma:
                    if (s_blitMaterial_LinearToGamma == null)
                        s_blitMaterial_LinearToGamma = CreateBlitShader(1);
                    return s_blitMaterial_LinearToGamma;

                case VisualElement.RenderTargetMode.NoColorConversion:
                    if (s_blitMaterial_NoChange == null)
                        s_blitMaterial_NoChange = CreateBlitShader(0);
                    return s_blitMaterial_NoChange;

                default:
                    Debug.LogError($"No Shader for Unsupported RenderTargetMode: { mode}");
                    return null;
            }
        }

        public unsafe static void ClosePaintElement(VisualElement ve, UIRStylePainter.ClosingInfo closingInfo, RenderChain renderChain, ref ChainBuilderStats stats)
        {
            if (closingInfo.clipperRegisterIndices.Length > 0)
            {
                NativeSlice<Vertex> verts = new NativeSlice<Vertex>();
                NativeSlice<UInt16> indices = new NativeSlice<UInt16>();
                UInt16 indexOffset = 0;

                // Due to device Update limitations, we cannot share the vertices of the registration mesh. It would be great
                // if we can just point winding-flipped indices towards the same vertices as the registration mesh.
                // For now, we duplicate the registration mesh entirely, wasting a bit of vertex memory
                UpdateOrAllocate(ref ve.renderChainData.closingData, closingInfo.clipperRegisterVertices.Length, closingInfo.clipperRegisterIndices.Length, renderChain.device, out verts, out indices, out indexOffset, ref stats);
                var job = new CopyClosingMeshJobData
                {
                    vertSrc = (IntPtr)closingInfo.clipperRegisterVertices.GetUnsafePtr(),
                    vertDst = (IntPtr)verts.GetUnsafePtr(),
                    vertCount = verts.Length,
                    indexSrc = (IntPtr)closingInfo.clipperRegisterIndices.GetUnsafePtr(),
                    indexDst = (IntPtr)indices.GetUnsafePtr(),
                    indexCount = indices.Length,
                    indexOffset = indexOffset - closingInfo.clipperRegisterIndexOffset
                };
                renderChain.jobManager.Add(ref job);
                closingInfo.clipUnregisterDrawCommand.mesh = ve.renderChainData.closingData;
                closingInfo.clipUnregisterDrawCommand.indexCount = indices.Length;
            }
        }

        static void UpdateOrAllocate(ref MeshHandle data, int vertexCount, int indexCount, UIRenderDevice device, out NativeSlice<Vertex> verts, out NativeSlice<UInt16> indices, out UInt16 indexOffset, ref ChainBuilderStats stats)
        {
            if (data != null)
            {
                // Try to fit within the existing allocation, optionally we can change the condition
                // to be an exact match of size to guarantee continuity in draw ranges
                if (data.allocVerts.size >= vertexCount && data.allocIndices.size >= indexCount)
                {
                    device.Update(data, (uint)vertexCount, (uint)indexCount, out verts, out indices, out indexOffset);
                    stats.updatedMeshAllocations++;
                }
                else
                {
                    // Won't fit in the existing allocated region, free the current one
                    device.Free(data);
                    data = device.Allocate((uint)vertexCount, (uint)indexCount, out verts, out indices, out indexOffset);
                    stats.newMeshAllocations++;
                }
            }
            else
            {
                data = device.Allocate((uint)vertexCount, (uint)indexCount, out verts, out indices, out indexOffset);
                stats.newMeshAllocations++;
            }
        }

        static void CopyTriangleIndicesFlipWindingOrder(NativeSlice<UInt16> source, NativeSlice<UInt16> target, int indexOffset)
        {
            Debug.Assert(source != target); // Not a very robust assert, but readers get the point
            int indexCount = source.Length;
            for (int i = 0; i < indexCount; i += 3)
            {
                // Using a temp variable to make reads from source sequential
                UInt16 t = (UInt16)(source[i] + indexOffset);
                target[i] = (UInt16)(source[i + 1] + indexOffset);
                target[i + 1] = t;
                target[i + 2] = (UInt16)(source[i + 2] + indexOffset);
            }
        }

        static void CopyTriangleIndices(NativeSlice<UInt16> source, NativeSlice<UInt16> target, int indexOffset)
        {
            int indexCount = source.Length;
            for (int i = 0; i < indexCount; i++)
                target[i] = (UInt16)(source[i] + indexOffset);
        }

        public static void UpdateOpacityId(VisualElement ve, RenderChain renderChain)
        {
            k_UpdateOpacityIdMarker.Begin();

            if (ve.renderChainData.data != null)
                DoUpdateOpacityId(ve, renderChain, ve.renderChainData.data);

            if (ve.renderChainData.closingData != null)
                DoUpdateOpacityId(ve, renderChain, ve.renderChainData.closingData);

            k_UpdateOpacityIdMarker.End();
        }

        static void DoUpdateOpacityId(VisualElement ve, RenderChain renderChain, MeshHandle mesh)
        {
            int vertCount = (int)mesh.allocVerts.size;
            NativeSlice<Vertex> oldVerts = mesh.allocPage.vertices.cpuData.Slice((int)mesh.allocVerts.start, vertCount);
            renderChain.device.Update(mesh, (uint)vertCount, out NativeSlice<Vertex> newVerts);
            Color32 opacityData = renderChain.shaderInfoAllocator.OpacityAllocToVertexData(ve.renderChainData.opacityID);
            renderChain.opacityIdAccelerator.CreateJob(oldVerts, newVerts, opacityData, vertCount);
        }

        public static bool NudgeVerticesToNewSpace(VisualElement ve, RenderChain renderChain, UIRenderDevice device)
        {
            k_NudgeVerticesMarker.Begin();

            Debug.Assert(!ve.renderChainData.disableNudging);

            Matrix4x4 newTransform;
            GetVerticesTransformInfo(ve, out newTransform);
            Matrix4x4 nudgeTransform = newTransform * ve.renderChainData.verticesSpace.inverse;

            // Attempt to reconstruct the absolute transform. If the result diverges from the absolute
            // considerably, then we assume that the vertices have become degenerate beyond restoration.
            // In this case we refuse to nudge, and ask for this element to be fully repainted to regenerate
            // the vertices without error.
            const float kMaxAllowedDeviation = 0.0001f;
            Matrix4x4 reconstructedNewTransform = nudgeTransform * ve.renderChainData.verticesSpace;
            float error;
            error = Mathf.Abs(newTransform.m00 - reconstructedNewTransform.m00);
            error += Mathf.Abs(newTransform.m01 - reconstructedNewTransform.m01);
            error += Mathf.Abs(newTransform.m02 - reconstructedNewTransform.m02);
            error += Mathf.Abs(newTransform.m03 - reconstructedNewTransform.m03);
            error += Mathf.Abs(newTransform.m10 - reconstructedNewTransform.m10);
            error += Mathf.Abs(newTransform.m11 - reconstructedNewTransform.m11);
            error += Mathf.Abs(newTransform.m12 - reconstructedNewTransform.m12);
            error += Mathf.Abs(newTransform.m13 - reconstructedNewTransform.m13);
            error += Mathf.Abs(newTransform.m20 - reconstructedNewTransform.m20);
            error += Mathf.Abs(newTransform.m21 - reconstructedNewTransform.m21);
            error += Mathf.Abs(newTransform.m22 - reconstructedNewTransform.m22);
            error += Mathf.Abs(newTransform.m23 - reconstructedNewTransform.m23);
            if (error > kMaxAllowedDeviation)
            {
                k_NudgeVerticesMarker.End();
                return false;
            }

            ve.renderChainData.verticesSpace = newTransform; // This is the new space of the vertices

            var job = new NudgeJobData
            {
                vertsBeforeUVDisplacement = ve.renderChainData.displacementUVStart,
                vertsAfterUVDisplacement = ve.renderChainData.displacementUVEnd,
                transform = nudgeTransform
            };

            PrepareNudgeVertices(ve, device, ve.renderChainData.data, out job.src, out job.dst, out job.count);
            if (ve.renderChainData.closingData != null)
                PrepareNudgeVertices(ve, device, ve.renderChainData.closingData, out job.closingSrc, out job.closingDst, out job.closingCount);

            renderChain.jobManager.Add(ref job);

            k_NudgeVerticesMarker.End();
            return true;
        }

        static unsafe void PrepareNudgeVertices(VisualElement ve, UIRenderDevice device, MeshHandle mesh, out IntPtr src, out IntPtr dst, out int count)
        {
            int vertCount = (int)mesh.allocVerts.size;
            NativeSlice<Vertex> oldVerts = mesh.allocPage.vertices.cpuData.Slice((int)mesh.allocVerts.start, vertCount);
            NativeSlice<Vertex> newVerts;
            device.Update(mesh, (uint)vertCount, out newVerts);

            src = (IntPtr)oldVerts.GetUnsafePtr();
            dst = (IntPtr)newVerts.GetUnsafePtr();
            count = vertCount;
        }

        static RenderChainCommand InjectMeshDrawCommand(RenderChain renderChain, VisualElement ve, ref RenderChainCommand cmdPrev, ref RenderChainCommand cmdNext, MeshHandle mesh, int indexCount, int indexOffset, Material material, TextureId texture, int stencilRef)
        {
            var cmd = renderChain.AllocCommand();
            cmd.type = CommandType.Draw;
            cmd.state = new State { material = material, texture = texture, stencilRef = stencilRef };
            cmd.mesh = mesh;
            cmd.indexOffset = indexOffset;
            cmd.indexCount = indexCount;
            cmd.owner = ve;
            InjectCommandInBetween(renderChain, cmd, ref cmdPrev, ref cmdNext);
            return cmd;
        }

        static RenderChainCommand InjectClosingMeshDrawCommand(RenderChain renderChain, VisualElement ve, ref RenderChainCommand cmdPrev, ref RenderChainCommand cmdNext, MeshHandle mesh, int indexCount, int indexOffset, Material material, TextureId texture, int stencilRef)
        {
            var cmd = renderChain.AllocCommand();
            cmd.type = CommandType.Draw;
            cmd.closing = true;
            cmd.state = new State { material = material, texture = texture, stencilRef = stencilRef };
            cmd.mesh = mesh;
            cmd.indexOffset = indexOffset;
            cmd.indexCount = indexCount;
            cmd.owner = ve;
            InjectClosingCommandInBetween(renderChain, cmd, ref cmdPrev, ref cmdNext);
            return cmd;
        }

        static void FindCommandInsertionPoint(VisualElement ve, out RenderChainCommand prev, out RenderChainCommand next)
        {
            VisualElement prevDrawingElem = ve.renderChainData.prev;

            // This can be potentially O(n) of VE count
            // It is ok to check against lastCommand to mean the presence of closingCommand too, as we
            // require that closing commands only exist if a startup command exists too
            while (prevDrawingElem != null && prevDrawingElem.renderChainData.lastCommand == null)
                prevDrawingElem = prevDrawingElem.renderChainData.prev;

            if (prevDrawingElem != null && prevDrawingElem.renderChainData.lastCommand != null)
            {
                // A previous drawing element can be:
                // A) A previous sibling (O(1) check time)
                // B) A parent/ancestor (O(n) of tree depth check time - meh)
                // C) A child/grand-child of a previous sibling to an ancestor (lengthy check time, so it is left as the only choice remaining after the first two)
                if (prevDrawingElem.hierarchy.parent == ve.hierarchy.parent) // Case A
                    prev = prevDrawingElem.renderChainData.lastClosingOrLastCommand;
                else if (prevDrawingElem.IsParentOrAncestorOf(ve)) // Case B
                    prev = prevDrawingElem.renderChainData.lastCommand;
                else
                {
                    // Case C, get the last command that isn't owned by us, this is to skip potential
                    // closing commands wrapped after the previous drawing element
                    var lastCommand = prevDrawingElem.renderChainData.lastClosingOrLastCommand;
                    for (;;)
                    {
                        prev = lastCommand;
                        lastCommand = lastCommand.next;
                        if (lastCommand == null || (lastCommand.owner == ve) || !lastCommand.closing) // Once again, we assume closing commands cannot exist without opening commands on the element
                            break;
                        if (lastCommand.owner.IsParentOrAncestorOf(ve))
                            break;
                    }
                }

                next = prev.next;
            }
            else
            {
                VisualElement nextDrawingElem = ve.renderChainData.next;
                // This can be potentially O(n) of VE count, very bad.. must adjust
                while (nextDrawingElem != null && nextDrawingElem.renderChainData.firstCommand == null)
                    nextDrawingElem = nextDrawingElem.renderChainData.next;
                next = nextDrawingElem?.renderChainData.firstCommand;
                prev = null;
                Debug.Assert((next == null) || (next.prev == null));
            }
        }

        static void FindClosingCommandInsertionPoint(VisualElement ve, out RenderChainCommand prev, out RenderChainCommand next)
        {
            // Closing commands for a visual element come after the closing commands of the shallowest child
            // If not found, then after the last command of the last deepest child
            // If not found, then after the last command of self

            VisualElement nextDrawingElem = ve.renderChainData.next;

            // Depth first search for the first VE that has a command (i.e. non empty element).
            // This can be potentially O(n) of VE count
            // It is ok to check against lastCommand to mean the presence of closingCommand too, as we
            // require that closing commands only exist if a startup command exists too
            while (nextDrawingElem != null && nextDrawingElem.renderChainData.firstCommand == null)
                nextDrawingElem = nextDrawingElem.renderChainData.next;

            if (nextDrawingElem != null && nextDrawingElem.renderChainData.firstCommand != null)
            {
                // A next drawing element can be:
                // A) A next sibling of ve (O(1) check time)
                // B) A child/grand-child of self (O(n) of tree depth check time - meh)
                // C) A next sibling of a parent/ancestor (lengthy check time, so it is left as the only choice remaining after the first two)
                if (nextDrawingElem.hierarchy.parent == ve.hierarchy.parent) // Case A
                {
                    next = nextDrawingElem.renderChainData.firstCommand;
                    prev = next.prev;
                }
                else if (ve.IsParentOrAncestorOf(nextDrawingElem)) // Case B
                {
                    // Enclose the last deepest drawing child by our closing command
                    for (;;)
                    {
                        prev = nextDrawingElem.renderChainData.lastClosingOrLastCommand;
                        nextDrawingElem = prev.next?.owner;
                        if (nextDrawingElem == null || !ve.IsParentOrAncestorOf(nextDrawingElem))
                            break;
                    }
                    next = prev.next;
                }
                else
                {
                    // Case C, just wrap ourselves
                    prev = ve.renderChainData.lastCommand;
                    next = prev.next;
                }
            }
            else
            {
                prev = ve.renderChainData.lastCommand;
                next = prev.next; // prev should not be null since we don't support closing commands without opening commands too
            }
        }

        static void InjectCommandInBetween(RenderChain renderChain, RenderChainCommand cmd, ref RenderChainCommand prev, ref RenderChainCommand next)
        {
            if (prev != null)
            {
                cmd.prev = prev;
                prev.next = cmd;
            }
            if (next != null)
            {
                cmd.next = next;
                next.prev = cmd;
            }

            VisualElement ve = cmd.owner;
            ve.renderChainData.lastCommand = cmd;
            if (ve.renderChainData.firstCommand == null)
                ve.renderChainData.firstCommand = cmd;
            renderChain.OnRenderCommandAdded(cmd);

            // Adjust the pointers as a facility for later injections
            prev = cmd;
            next = cmd.next;
        }

        static void InjectClosingCommandInBetween(RenderChain renderChain, RenderChainCommand cmd, ref RenderChainCommand prev, ref RenderChainCommand next)
        {
            Debug.Assert(cmd.closing);
            if (prev != null)
            {
                cmd.prev = prev;
                prev.next = cmd;
            }
            if (next != null)
            {
                cmd.next = next;
                next.prev = cmd;
            }

            VisualElement ve = cmd.owner;
            ve.renderChainData.lastClosingCommand = cmd;
            if (ve.renderChainData.firstClosingCommand == null)
                ve.renderChainData.firstClosingCommand = cmd;

            renderChain.OnRenderCommandAdded(cmd);

            // Adjust the pointers as a facility for later injections
            prev = cmd;
            next = cmd.next;
        }

        public static void ResetCommands(RenderChain renderChain, VisualElement ve)
        {
            if (ve.renderChainData.firstCommand != null)
                renderChain.OnRenderCommandsRemoved(ve.renderChainData.firstCommand, ve.renderChainData.lastCommand);

            var prev = ve.renderChainData.firstCommand != null ? ve.renderChainData.firstCommand.prev : null;
            var next = ve.renderChainData.lastCommand != null ? ve.renderChainData.lastCommand.next : null;
            Debug.Assert(prev == null || prev.owner != ve);
            Debug.Assert(next == null || next == ve.renderChainData.firstClosingCommand || next.owner != ve);
            if (prev != null) prev.next = next;
            if (next != null) next.prev = prev;

            if (ve.renderChainData.firstCommand != null)
            {
                var c = ve.renderChainData.firstCommand;
                while (c != ve.renderChainData.lastCommand)
                {
                    var nextC = c.next;
                    renderChain.FreeCommand(c);
                    c = nextC;
                }
                renderChain.FreeCommand(c); // Last command
            }
            ve.renderChainData.firstCommand = ve.renderChainData.lastCommand = null;

            prev = ve.renderChainData.firstClosingCommand != null ? ve.renderChainData.firstClosingCommand.prev : null;
            next = ve.renderChainData.lastClosingCommand != null ? ve.renderChainData.lastClosingCommand.next : null;
            Debug.Assert(prev == null || prev.owner != ve);
            Debug.Assert(next == null || next.owner != ve);
            if (prev != null) prev.next = next;
            if (next != null) next.prev = prev;

            if (ve.renderChainData.firstClosingCommand != null)
            {
                renderChain.OnRenderCommandsRemoved(ve.renderChainData.firstClosingCommand, ve.renderChainData.lastClosingCommand);

                var c = ve.renderChainData.firstClosingCommand;
                while (c != ve.renderChainData.lastClosingCommand)
                {
                    var nextC = c.next;
                    renderChain.FreeCommand(c);
                    c = nextC;
                }
                renderChain.FreeCommand(c); // Last closing command
            }
            ve.renderChainData.firstClosingCommand = ve.renderChainData.lastClosingCommand = null;
        }
    }
}
