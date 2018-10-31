// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public partial struct ScriptableRenderContext : IEquatable<ScriptableRenderContext>
    {
        IntPtr m_Ptr;

        AtomicSafetyHandle m_Safety;

        internal ScriptableRenderContext(IntPtr ptr, AtomicSafetyHandle safety)
        {
            m_Ptr = ptr;
            m_Safety = safety;
        }


        public unsafe void BeginRenderPass(int width, int height, int samples, NativeArray<AttachmentDescriptor> attachments, int depthAttachmentIndex = -1)
        {
            Validate();
            BeginRenderPass_Internal(m_Ptr, width, height, samples, (IntPtr)attachments.GetUnsafeReadOnlyPtr(), attachments.Length, depthAttachmentIndex);
        }

        public ScopedRenderPass BeginScopedRenderPass(int width, int height, int samples, NativeArray<AttachmentDescriptor> attachments, int depthAttachmentIndex = -1)
        {
            BeginRenderPass(width, height, samples, attachments, depthAttachmentIndex);
            return new ScopedRenderPass(this);
        }

        public unsafe void BeginSubPass(NativeArray<int> colors, NativeArray<int> inputs, bool isDepthReadOnly = false)
        {
            Validate();
            BeginSubPass_Internal(m_Ptr, (IntPtr)colors.GetUnsafeReadOnlyPtr(), colors.Length, (IntPtr)inputs.GetUnsafeReadOnlyPtr(), inputs.Length, isDepthReadOnly);
        }

        public unsafe void BeginSubPass(NativeArray<int> colors, bool isDepthReadOnly = false)
        {
            Validate();
            BeginSubPass_Internal(m_Ptr, (IntPtr)colors.GetUnsafeReadOnlyPtr(), colors.Length, IntPtr.Zero, 0, isDepthReadOnly);
        }

        public ScopedSubPass BeginScopedSubPass(NativeArray<int> colors, NativeArray<int> inputs, bool isDepthReadOnly = false)
        {
            BeginSubPass(colors, inputs, isDepthReadOnly);
            return new ScopedSubPass(this);
        }

        public ScopedSubPass BeginScopedSubPass(NativeArray<int> colors, bool isDepthReadOnly = false)
        {
            BeginSubPass(colors, isDepthReadOnly);
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

        public void DrawRenderers(CullingResults cullingResults, ref DrawingSettings drawingSettings, ref FilteringSettings filteringSettings)
        {
            Validate();
            cullingResults.Validate();
            DrawRenderers_Internal(cullingResults.ptr, ref drawingSettings, ref filteringSettings, IntPtr.Zero, IntPtr.Zero, 0);
        }

        public unsafe void DrawRenderers(CullingResults cullingResults, ref DrawingSettings drawingSettings, ref FilteringSettings filteringSettings, ref RenderStateBlock stateBlock)
        {
            Validate();
            cullingResults.Validate();
            var renderType = new ShaderTagId();
            fixed(RenderStateBlock* stateBlockPtr = &stateBlock)
            {
                DrawRenderers_Internal(cullingResults.ptr, ref drawingSettings, ref filteringSettings, (IntPtr)(&renderType), (IntPtr)stateBlockPtr, 1);
            }
        }

        public unsafe void DrawRenderers(CullingResults cullingResults, ref DrawingSettings drawingSettings, ref FilteringSettings filteringSettings, NativeArray<ShaderTagId> renderTypes, NativeArray<RenderStateBlock> stateBlocks)
        {
            Validate();
            cullingResults.Validate();
            if (renderTypes.Length != stateBlocks.Length)
                throw new ArgumentException($"Arrays {nameof(renderTypes)} and {nameof(stateBlocks)} should have same length, but {nameof(renderTypes)} had length {renderTypes.Length} while {nameof(stateBlocks)} had length {stateBlocks.Length}.");
            DrawRenderers_Internal(cullingResults.ptr, ref drawingSettings, ref filteringSettings, (IntPtr)renderTypes.GetUnsafeReadOnlyPtr(), (IntPtr)stateBlocks.GetUnsafeReadOnlyPtr(), renderTypes.Length);
        }

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

            Validate();
            ExecuteCommandBuffer_Internal(commandBuffer);
        }

        public void ExecuteCommandBufferAsync(CommandBuffer commandBuffer, ComputeQueueType queueType)
        {
            if (commandBuffer == null)
                throw new ArgumentNullException(nameof(commandBuffer));

            Validate();
            ExecuteCommandBufferAsync_Internal(commandBuffer, queueType);
        }

        public void SetupCameraProperties(Camera camera, bool stereoSetup = false)
        {
            Validate();
            SetupCameraProperties_Internal(camera, stereoSetup);
        }

        public void StereoEndRender(Camera camera)
        {
            Validate();
            StereoEndRender_Internal(camera);
        }

        public void StartMultiEye(Camera camera)
        {
            Validate();
            StartMultiEye_Internal(camera);
        }

        public void StopMultiEye(Camera camera)
        {
            Validate();
            StopMultiEye_Internal(camera);
        }

        public void DrawSkybox(Camera camera)
        {
            Validate();
            DrawSkybox_Internal(camera);
        }

        public unsafe CullingResults Cull(ref ScriptableCullingParameters parameters)
        {
            var results = new CullingResults();
            Internal_Cull(ref parameters, this, (IntPtr)(&results));
            return results;
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
    }
}
