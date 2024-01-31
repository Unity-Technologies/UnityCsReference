// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public partial struct ScriptableRenderContext : IEquatable<ScriptableRenderContext>
    {
        static readonly ShaderTagId kRenderTypeTag = new ShaderTagId("RenderType");

        IntPtr m_Ptr;

        AtomicSafetyHandle m_Safety;

        internal ScriptableRenderContext(IntPtr ptr, AtomicSafetyHandle safety)
        {
            m_Ptr = ptr;
            m_Safety = safety;
        }


        public unsafe void BeginRenderPass(int width, int height, int volumeDepth, int samples, NativeArray<AttachmentDescriptor> attachments, int depthAttachmentIndex = -1)
        {
            Validate();
            BeginRenderPass_Internal(m_Ptr, width, height, volumeDepth, samples, (IntPtr)attachments.GetUnsafeReadOnlyPtr(), attachments.Length, depthAttachmentIndex);
        }

        public unsafe void BeginRenderPass(int width, int height, int samples, NativeArray<AttachmentDescriptor> attachments, int depthAttachmentIndex = -1)
        {
            Validate();
            BeginRenderPass_Internal(m_Ptr, width, height, 1, samples, (IntPtr)attachments.GetUnsafeReadOnlyPtr(), attachments.Length, depthAttachmentIndex);
        }

        public ScopedRenderPass BeginScopedRenderPass(int width, int height, int samples, NativeArray<AttachmentDescriptor> attachments, int depthAttachmentIndex = -1)
        {
            BeginRenderPass(width, height, samples, attachments, depthAttachmentIndex);
            return new ScopedRenderPass(this);
        }

        public unsafe void BeginSubPass(NativeArray<int> colors, NativeArray<int> inputs, bool isDepthReadOnly, bool isStencilReadOnly)
        {
            Validate();
            BeginSubPass_Internal(m_Ptr, (IntPtr)colors.GetUnsafeReadOnlyPtr(), colors.Length, (IntPtr)inputs.GetUnsafeReadOnlyPtr(), inputs.Length, isDepthReadOnly, isStencilReadOnly);
        }

        public unsafe void BeginSubPass(NativeArray<int> colors, NativeArray<int> inputs, bool isDepthStencilReadOnly = false)
        {
            Validate();
            BeginSubPass_Internal(m_Ptr, (IntPtr)colors.GetUnsafeReadOnlyPtr(), colors.Length, (IntPtr)inputs.GetUnsafeReadOnlyPtr(), inputs.Length, isDepthStencilReadOnly, isDepthStencilReadOnly);
        }

        public unsafe void BeginSubPass(NativeArray<int> colors, bool isDepthReadOnly, bool isStencilReadOnly)
        {
            Validate();
            BeginSubPass_Internal(m_Ptr, (IntPtr)colors.GetUnsafeReadOnlyPtr(), colors.Length, IntPtr.Zero, 0, isDepthReadOnly, isStencilReadOnly);
        }

        public unsafe void BeginSubPass(NativeArray<int> colors, bool isDepthStencilReadOnly = false)
        {
            Validate();
            BeginSubPass_Internal(m_Ptr, (IntPtr)colors.GetUnsafeReadOnlyPtr(), colors.Length, IntPtr.Zero, 0, isDepthStencilReadOnly, isDepthStencilReadOnly);
        }

        public ScopedSubPass BeginScopedSubPass(NativeArray<int> colors, NativeArray<int> inputs, bool isDepthReadOnly, bool isStencilReadOnly)
        {
            BeginSubPass(colors, inputs, isDepthReadOnly, isStencilReadOnly);
            return new ScopedSubPass(this);
        }

        public ScopedSubPass BeginScopedSubPass(NativeArray<int> colors, NativeArray<int> inputs, bool isDepthStencilReadOnly = false)
        {
            BeginSubPass(colors, inputs, isDepthStencilReadOnly);
            return new ScopedSubPass(this);
        }

        public ScopedSubPass BeginScopedSubPass(NativeArray<int> colors, bool isDepthReadOnly, bool isStencilReadOnly)
        {
            BeginSubPass(colors, isDepthReadOnly, isStencilReadOnly);
            return new ScopedSubPass(this);
        }

        public ScopedSubPass BeginScopedSubPass(NativeArray<int> colors, bool isDepthStencilReadOnly = false)
        {
            BeginSubPass(colors, isDepthStencilReadOnly);
            return new ScopedSubPass(this);
        }

        public void EndSubPass()
        {
            Validate();
            EndSubPass_Internal(m_Ptr);
        }

        public void EndRenderPass()
        {
            Validate();
            EndRenderPass_Internal(m_Ptr);
        }

        public void Submit()
        {
            Validate();
            Submit_Internal();
        }

        public bool SubmitForRenderPassValidation()
        {
            Validate();
            return SubmitForRenderPassValidation_Internal();
        }

        public bool HasInvokeOnRenderObjectCallbacks()
        {
            Validate();
            return HasInvokeOnRenderObjectCallbacks_Internal();
        }

        internal void GetCameras(List<Camera> results)
        {
            Validate();
            GetCameras_Internal(typeof(Camera), results);
        }

        const bool deprecateDrawXmethods = false;

        [Obsolete("DrawRenderers is obsolete and replaced with the RendererList API: construct a RendererList using ScriptableRenderContext.CreateRendererList and execture it using CommandBuffer.DrawRendererList.", deprecateDrawXmethods)]
        public void DrawRenderers(CullingResults cullingResults, ref DrawingSettings drawingSettings, ref FilteringSettings filteringSettings)
        {
            Validate();
            cullingResults.Validate();
            DrawRenderers_Internal(cullingResults.ptr, ref drawingSettings, ref filteringSettings, ShaderTagId.none, false, IntPtr.Zero, IntPtr.Zero, 0);
        }
        [Obsolete("DrawRenderers is obsolete and replaced with the RendererList API: construct a RendererList using ScriptableRenderContext.CreateRendererList and execture it using CommandBuffer.DrawRendererList.", deprecateDrawXmethods)]
        public unsafe void DrawRenderers(CullingResults cullingResults, ref DrawingSettings drawingSettings, ref FilteringSettings filteringSettings, ref RenderStateBlock stateBlock)
        {
            Validate();
            cullingResults.Validate();
            var renderType = new ShaderTagId();
            fixed(RenderStateBlock* stateBlockPtr = &stateBlock)
            {
                DrawRenderers_Internal(cullingResults.ptr, ref drawingSettings, ref filteringSettings, ShaderTagId.none, false, (IntPtr)(&renderType), (IntPtr)stateBlockPtr, 1);
            }
        }
        [Obsolete("DrawRenderers is obsolete and replaced with the RendererList API: construct a RendererList using ScriptableRenderContext.CreateRendererList and execture it using CommandBuffer.DrawRendererList.", deprecateDrawXmethods)]
        public unsafe void DrawRenderers(CullingResults cullingResults, ref DrawingSettings drawingSettings, ref FilteringSettings filteringSettings, NativeArray<ShaderTagId> renderTypes, NativeArray<RenderStateBlock> stateBlocks)
        {
            Validate();
            cullingResults.Validate();
            if (renderTypes.Length != stateBlocks.Length)
                throw new ArgumentException($"Arrays {nameof(renderTypes)} and {nameof(stateBlocks)} should have same length, but {nameof(renderTypes)} had length {renderTypes.Length} while {nameof(stateBlocks)} had length {stateBlocks.Length}.");
            DrawRenderers_Internal(cullingResults.ptr, ref drawingSettings, ref filteringSettings, kRenderTypeTag, false, (IntPtr)renderTypes.GetUnsafeReadOnlyPtr(), (IntPtr)stateBlocks.GetUnsafeReadOnlyPtr(), renderTypes.Length);
        }
        [Obsolete("DrawRenderers is obsolete and replaced with the RendererList API: construct a RendererList using ScriptableRenderContext.CreateRendererList and execture it using CommandBuffer.DrawRendererList.", deprecateDrawXmethods)]
        public unsafe void DrawRenderers(CullingResults cullingResults, ref DrawingSettings drawingSettings, ref FilteringSettings filteringSettings, ShaderTagId tagName, bool isPassTagName, NativeArray<ShaderTagId> tagValues, NativeArray<RenderStateBlock> stateBlocks)
        {
            Validate();
            cullingResults.Validate();
            if (tagValues.Length != stateBlocks.Length)
                throw new ArgumentException($"Arrays {nameof(tagValues)} and {nameof(stateBlocks)} should have same length, but {nameof(tagValues)} had length {tagValues.Length} while {nameof(stateBlocks)} had length {stateBlocks.Length}.");
            DrawRenderers_Internal(cullingResults.ptr, ref drawingSettings, ref filteringSettings, tagName, isPassTagName, (IntPtr)tagValues.GetUnsafeReadOnlyPtr(), (IntPtr)stateBlocks.GetUnsafeReadOnlyPtr(), tagValues.Length);
        }
        [Obsolete("DrawShadows is obsolete and replaced with the RendererList API: construct a RendererList using ScriptableRenderContext.CreateShadowRendererList and execture it using CommandBuffer.DrawRendererList.", deprecateDrawXmethods)]
        public unsafe void DrawShadows(ref ShadowDrawingSettings settings)
        {
            Validate();
            settings.cullingResults.Validate();
            fixed(ShadowDrawingSettings* settingsPtr = &settings)
            {
                DrawShadows_Internal((IntPtr)settingsPtr);
            }
        }

        public void ExecuteCommandBuffer(CommandBuffer commandBuffer)
        {
            if (commandBuffer == null)
                throw new ArgumentNullException(nameof(commandBuffer));
            if (commandBuffer.m_Ptr == IntPtr.Zero)
                throw new ObjectDisposedException(nameof(commandBuffer));

            Validate();
            ExecuteCommandBuffer_Internal(commandBuffer);
        }

        public void ExecuteCommandBufferAsync(CommandBuffer commandBuffer, ComputeQueueType queueType)
        {
            if (commandBuffer == null)
                throw new ArgumentNullException(nameof(commandBuffer));
            if (commandBuffer.m_Ptr == IntPtr.Zero)
                throw new ObjectDisposedException(nameof(commandBuffer));

            Validate();
            ExecuteCommandBufferAsync_Internal(commandBuffer, queueType);
        }

        public void SetupCameraProperties(Camera camera, bool stereoSetup = false)
        {
            SetupCameraProperties(camera, stereoSetup, 0);
        }

        public void SetupCameraProperties(Camera camera, bool stereoSetup, int eye)
        {
            Validate();
            SetupCameraProperties_Internal(camera, stereoSetup, eye);
        }

        public void StereoEndRender(Camera camera)
        {
            StereoEndRender(camera, 0, true);
        }

        public void StereoEndRender(Camera camera, int eye)
        {
            StereoEndRender(camera, eye, true);
        }

        public void StereoEndRender(Camera camera, int eye, bool isFinalPass)
        {
            Validate();
            StereoEndRender_Internal(camera, eye, isFinalPass);
        }

        public void StartMultiEye(Camera camera)
        {
            StartMultiEye(camera, 0);
        }

        public void StartMultiEye(Camera camera, int eye)
        {
            Validate();
            StartMultiEye_Internal(camera, eye);
        }

        public void StopMultiEye(Camera camera)
        {
            Validate();
            StopMultiEye_Internal(camera);
        }
        [Obsolete("DrawSkybox is obsolete and replaced with the RendererList API: construct a RendererList using ScriptableRenderContext.CreateSkyboxRendererList and execture it using CommandBuffer.DrawRendererList.", deprecateDrawXmethods)]
        public void DrawSkybox(Camera camera)
        {
            Validate();
            DrawSkybox_Internal(camera);
        }

        public void InvokeOnRenderObjectCallback()
        {
            Validate();
            InvokeOnRenderObjectCallback_Internal();
        }

        public void DrawGizmos(Camera camera, GizmoSubset gizmoSubset)
        {
            Validate();
            DrawGizmos_Internal(camera, gizmoSubset);
        }

        public void DrawWireOverlay(Camera camera)
        {
            Validate();
            DrawWireOverlay_Impl(camera);
        }

        public void DrawUIOverlay(Camera camera)
        {
            Validate();
            DrawUIOverlay_Internal(camera);
        }

        public unsafe CullingResults Cull(ref ScriptableCullingParameters parameters)
        {
            var results = new CullingResults();
            Internal_Cull(ref parameters, this, (IntPtr)(&results));
            return results;
        }

        // Match with struct in Internal_CullShadowCasters C++ side
        unsafe struct CullShadowCastersContext
        {
            public IntPtr cullResults;
            public ShadowSplitData* splitBuffer;
            public int splitBufferLength;
            public LightShadowCasterCullingInfo* perLightInfos;
            public int perLightInfoCount;
        }

        unsafe void ValidateCullShadowCastersParameters(in CullingResults cullingResults, in ShadowCastersCullingInfos cullingInfos)
        {
            const int MaxSplitCount = 6;

            if (cullingResults.ptr == null)
            {
                throw new UnityException("CullingResults is null");
            }

            // Always valid to provide 0 range
            if (cullingInfos.perLightInfos.Length == 0)
                return;

            if (cullingResults.visibleLights.Length != cullingInfos.perLightInfos.Length)
            {
                throw new UnityException($"CullingResults.visibleLights.Length ({cullingResults.visibleLights.Length}) != ShadowCastersCullingInfos.perLightInfos.Length ({cullingInfos.perLightInfos.Length}). " +
                    "ShadowCastersCullingInfos.perLightInfos must have one entry per visible light.");
            }

            LightShadowCasterCullingInfo* perLightInfosPtr = (LightShadowCasterCullingInfo*)cullingInfos.perLightInfos.GetUnsafeReadOnlyPtr();

            for (int i = 0; i < cullingInfos.perLightInfos.Length; ++i)
            {
                ref readonly LightShadowCasterCullingInfo infos = ref perLightInfosPtr[i];
                RangeInt range = infos.splitRange;
                int begin = range.start;
                int length = range.length;
                int end = begin + length;

                // Null ranges are always valid
                if (begin == 0 && length == 0)
                    continue;

                bool isBeginValid = begin >= 0 && begin <= cullingInfos.splitBuffer.Length;
                bool isLengthValid = length >= 0 && length <= MaxSplitCount;
                bool isEndValid = end >= begin && end <= cullingInfos.splitBuffer.Length;
                bool isRangeValid = isBeginValid && isLengthValid && isEndValid;
                if (!isRangeValid)
                {
                    throw new UnityException($"ShadowCastersCullingInfos.perLightInfos[{i}] is referring to an invalid memory location. " +
                        $"splitRange.start ({range.start}) splitRange.length ({range.length}) " +
                        $"ShadowCastersCullingInfos.splitBuffer.Length ({cullingInfos.splitBuffer.Length}).");
                }

                if (length > 0 && infos.projectionType == BatchCullingProjectionType.Unknown)
                {
                    throw new UnityException($"ShadowCastersCullingInfos.perLightInfos[{i}].projectionType == {infos.projectionType}. "
                        + $"The range however appears to be valid. splitRange.start ({range.start}) splitRange.length ({range.length})\n");
                }
            }
        }

        public unsafe void CullShadowCasters(CullingResults cullingResults, ShadowCastersCullingInfos infos)
        {
            Validate();

            ValidateCullShadowCastersParameters(cullingResults, infos);

            CullShadowCastersContext context = default;
            context.cullResults = cullingResults.ptr;
            context.splitBuffer = (ShadowSplitData*)infos.splitBuffer.GetUnsafePtr();
            context.splitBufferLength = infos.splitBuffer.Length;
            context.perLightInfos = (LightShadowCasterCullingInfo*)infos.perLightInfos.GetUnsafePtr();
            context.perLightInfoCount = infos.perLightInfos.Length;

            Internal_CullShadowCasters(this, (IntPtr)(&context));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void Validate()
        {
            if (m_Ptr.ToInt64() == 0)
                throw new InvalidOperationException($"The {nameof(ScriptableRenderContext)} instance is invalid. This can happen if you construct an instance using the default constructor.");

            try
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"The {nameof(ScriptableRenderContext)} instance is no longer valid. This can happen if you re-use it across multiple frames.", e);
            }
        }

        public bool Equals(ScriptableRenderContext other)
        {
            return m_Ptr.Equals(other.m_Ptr);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ScriptableRenderContext && Equals((ScriptableRenderContext)obj);
        }

        public override int GetHashCode()
        {
            return m_Ptr.GetHashCode();
        }

        public static bool operator==(ScriptableRenderContext left, ScriptableRenderContext right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(ScriptableRenderContext left, ScriptableRenderContext right)
        {
            return !left.Equals(right);
        }

        public unsafe RendererList CreateRendererList(RendererUtils.RendererListDesc desc)
        {
            Validate();
            RendererListParams param = RendererUtils.RendererListDesc.ConvertToParameters(desc);
            var list = CreateRendererList(ref param);
            param.Dispose();
            return list;
        }

        public unsafe RendererList CreateRendererList(ref RendererListParams param)
        {
            Validate();
            param.Validate();

            var list = CreateRendererList_Internal(param.cullingResults.ptr, ref param.drawSettings, ref param.filteringSettings, param.tagName, param.isPassTagName,
                param.tagsValuePtr, param.stateBlocksPtr, param.numStateBlocks);
            return list;
        }

        public unsafe RendererList CreateShadowRendererList(ref ShadowDrawingSettings settings)
        {
            Validate();
            settings.cullingResults.Validate();
            fixed (ShadowDrawingSettings* settingsPtr = &settings)
            {
                return CreateShadowRendererList_Internal((IntPtr)settingsPtr);
            }
        }

        public unsafe RendererList CreateSkyboxRendererList(Camera camera, Matrix4x4 projectionMatrixL, Matrix4x4 viewMatrixL, Matrix4x4 projectionMatrixR, Matrix4x4 viewMatrixR)
        {
            Validate();
            return CreateSkyboxRendererList_Internal(camera, (int)SkyboxXRMode.LegacySinglePass, projectionMatrixL, viewMatrixL, projectionMatrixR, viewMatrixR);
        }
        public RendererList CreateSkyboxRendererList(Camera camera, Matrix4x4 projectionMatrix, Matrix4x4 viewMatrix)
        {
            Validate();
            return CreateSkyboxRendererList_Internal(camera, (int)SkyboxXRMode.Enabled, projectionMatrix, viewMatrix, Matrix4x4.identity, Matrix4x4.identity);
        }
        public RendererList CreateSkyboxRendererList(Camera camera)
        {
            Validate();
            return CreateSkyboxRendererList_Internal(camera, (int)SkyboxXRMode.Off, Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity);
        }

        public RendererList CreateGizmoRendererList(Camera camera, GizmoSubset gizmoSubset)
        {
            Validate();
            return CreateGizmoRendererList_Internal(camera, gizmoSubset);
        }

        public RendererList CreateUIOverlayRendererList(Camera camera)
        {
            Validate();
            return CreateUIOverlayRendererList_Internal(camera, UISubset.All);
        }

        public RendererList CreateUIOverlayRendererList(Camera camera, UISubset uiSubset)
        {
            Validate();
            return CreateUIOverlayRendererList_Internal(camera, uiSubset);
        }

        public RendererList CreateWireOverlayRendererList(Camera camera)
        {
            Validate();
            return CreateWireOverlayRendererList_Internal(camera);
        }

        public unsafe void PrepareRendererListsAsync(List<RendererList> rendererLists)
        {
            Validate();
            PrepareRendererListsAsync_Internal(rendererLists);
        }

        public RendererListStatus QueryRendererListStatus(RendererList rendererList)
        {
            Validate();
            return QueryRendererListStatus_Internal(rendererList);
        }
    }
}
