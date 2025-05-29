// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections;
using UnityEngine.Pool;
using UnityEngine.Scripting;

namespace UnityEngine.Rendering
{
    public static partial class RenderPipelineManager
    {
        private static bool s_CleanUpPipeline = false;

        private static RenderPipelineAsset s_CurrentPipelineAsset;

        internal static RenderPipelineAsset currentPipelineAsset => s_CurrentPipelineAsset;
        static RenderPipeline s_CurrentPipeline = null;
        static bool s_PendingRPAssignationToRaise = false;
        public static RenderPipeline currentPipeline
        {
            get => s_CurrentPipeline;
            private set
            {
                s_CurrentPipeline = value;
                if (s_PendingRPAssignationToRaise)
                {
                    s_PendingRPAssignationToRaise = false;
                    activeRenderPipelineTypeChanged?.Invoke();
                }
            }
        }

        public static event Action<ScriptableRenderContext, List<Camera>> beginContextRendering;
        public static event Action<ScriptableRenderContext, List<Camera>> endContextRendering;
        public static event Action<ScriptableRenderContext, Camera> beginCameraRendering;
        public static event Action<ScriptableRenderContext, Camera> endCameraRendering;

        public static event Action activeRenderPipelineTypeChanged;
        public static event Action<RenderPipelineAsset, RenderPipelineAsset> activeRenderPipelineAssetChanged;

        public static event Action activeRenderPipelineCreated;
        public static event Action activeRenderPipelineDisposed;
        public static bool pipelineSwitchCompleted => ReferenceEquals(s_CurrentPipelineAsset, GraphicsSettings.currentRenderPipeline) && !IsPipelineRequireCreation();

        internal static void BeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            beginContextRendering?.Invoke(context, cameras);
#pragma warning disable CS0618
            beginFrameRendering?.Invoke(context, cameras.ToArray());
#pragma warning restore CS0618
        }

        internal static void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            beginCameraRendering?.Invoke(context, camera);
        }

        internal static void EndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
#pragma warning disable CS0618
            endFrameRendering?.Invoke(context, cameras.ToArray());
#pragma warning restore CS0618
            endContextRendering?.Invoke(context, cameras);
        }

        internal static void EndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            endCameraRendering?.Invoke(context, camera);
        }

        [RequiredByNativeCode]
        static void OnActiveRenderPipelineAssetChanged(ScriptableObject from, ScriptableObject to, bool raiseTypeChanged)
        {
            var fromRPAsset = from as RenderPipelineAsset;
            var toRPAsset = to as RenderPipelineAsset;
            activeRenderPipelineAssetChanged?.Invoke(fromRPAsset, toRPAsset);
            if (raiseTypeChanged)
            {
                // At this point we know that the RenderPipelineAsset is switching to one of a different type.
                // This should call activeRenderPipelineTypeChanged. But prior, it was only called if RP was already assigned.
                // Depending on which view / preview / camera rendering exists:
                // Sometimes, pipeline creation will be triggered before this event.
                // Sometimes, it is after.
                // And sometimes the creation fails and delays this more.
                // Resyncing everything:
                Type targetRPType = toRPAsset == null ? null : toRPAsset.pipelineType;
                if (currentPipeline?.GetType() != targetRPType)
                    s_PendingRPAssignationToRaise = true;
                else
                    activeRenderPipelineTypeChanged?.Invoke();
            }
        }

        [RequiredByNativeCode]
        internal static void HandleRenderPipelineChange(RenderPipelineAsset pipelineAsset)
        {
            bool hasRPAssetChanged = !ReferenceEquals(s_CurrentPipelineAsset, pipelineAsset);
            if (s_CleanUpPipeline || hasRPAssetChanged)
            {
                // Required because when switching to a RenderPipeline asset for the first time
                // it will call OnValidate on the new asset before cleaning up the old one. Thus we
                // reset the rebuild in order to cleanup properly.
                CleanupRenderPipeline();
                s_CurrentPipelineAsset = pipelineAsset;
            }
        }

        [RequiredByNativeCode]
        internal static void RecreateCurrentPipeline(RenderPipelineAsset pipelineAsset)
        {
            if (s_CurrentPipelineAsset == pipelineAsset)
            {
                s_CleanUpPipeline = true;
            }
        }

        [RequiredByNativeCode]
        internal static void CleanupRenderPipeline()
        {
            if (!isCurrentPipelineValid)
                return;

            // This check prevents shader reloading through Builtin whenever we switch from one RP to another RP
            if (GraphicsSettings.currentRenderPipeline == null)
                Shader.globalRenderPipeline = string.Empty;

            activeRenderPipelineDisposed?.Invoke();
            currentPipeline.Dispose();
            currentPipeline = null;
            s_CleanUpPipeline = false;
            s_CurrentPipelineAsset = null;
            SupportedRenderingFeatures.active = null;
        }

        [RequiredByNativeCode]
        static void DoRenderLoop_Internal
        (
            RenderPipelineAsset pipelineAsset,
            IntPtr loopPtr,
            Object renderRequest
            , AtomicSafetyHandle safety
        )
        {
            if (!TryPrepareRenderPipeline(pipelineAsset))
                return;

            var loop = new ScriptableRenderContext(loopPtr
                , safety
                );

            using (ListPool<Camera>.Get(out var cameras))
            {
                loop.GetCameras(cameras);
                if (renderRequest == null)
                    currentPipeline.InternalRender(loop, cameras);
                else
                    currentPipeline.InternalProcessRenderRequests(loop, cameras[0], renderRequest);
            }
        }

        internal static bool TryPrepareRenderPipeline(RenderPipelineAsset pipelineAsset)
        {
            HandleRenderPipelineChange(pipelineAsset);

            if (!IsPipelineRequireCreation())
                return currentPipeline != null;

            currentPipeline = s_CurrentPipelineAsset.InternalCreatePipeline();
            Shader.globalRenderPipeline = s_CurrentPipelineAsset.renderPipelineShaderTag;
            activeRenderPipelineCreated?.Invoke();
            return currentPipeline != null;
        }

        internal static bool isCurrentPipelineValid => currentPipeline is { disposed: false };
        static bool IsPipelineRequireCreation() => s_CurrentPipelineAsset != null && (currentPipeline == null || currentPipeline.disposed);
    }
}
