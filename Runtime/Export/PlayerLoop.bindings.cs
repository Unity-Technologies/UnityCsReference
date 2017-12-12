// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine.Experimental.PlayerLoop
{
    [RequiredByNativeCode]
    public struct Initialization
    {
        [RequiredByNativeCode]
        public struct PlayerUpdateTime {}
        [RequiredByNativeCode]
        public struct AsyncUploadTimeSlicedUpdate {}
        [RequiredByNativeCode]
        public struct SynchronizeState {}
        [RequiredByNativeCode]
        public struct SynchronizeInputs {}
        [RequiredByNativeCode]
        public struct XREarlyUpdate {}
    }

    [RequiredByNativeCode]
    public struct EarlyUpdate
    {
        [RequiredByNativeCode]
        public struct PollPlayerConnection {}
        [RequiredByNativeCode]
        public struct ProfilerStartFrame {}
        [RequiredByNativeCode]
        public struct PollHtcsPlayerConnection {}
        [RequiredByNativeCode]
        public struct GpuTimestamp {}
        [RequiredByNativeCode]
        public struct UnityConnectClientUpdate {}
        [RequiredByNativeCode]
        public struct CloudWebServicesUpdate {}
        [RequiredByNativeCode]
        public struct UnityWebRequestUpdate {}
        [RequiredByNativeCode]
        public struct ExecuteMainThreadJobs {}
        [RequiredByNativeCode]
        public struct ProcessMouseInWindow {}
        [RequiredByNativeCode]
        public struct ClearIntermediateRenderers {}
        [RequiredByNativeCode]
        public struct ClearLines {}
        [RequiredByNativeCode]
        public struct PresentBeforeUpdate {}
        [RequiredByNativeCode]
        public struct ResetFrameStatsAfterPresent {}
        [RequiredByNativeCode]
        public struct UpdateAllUnityWebStreams {}
        [RequiredByNativeCode]
        public struct UpdateAsyncReadbackManager {}
        [RequiredByNativeCode]
        public struct UpdateTextureStreamingManager {}
        [RequiredByNativeCode]
        public struct UpdatePreloading {}
        [RequiredByNativeCode]
        public struct RendererNotifyInvisible {}
        [RequiredByNativeCode]
        public struct PlayerCleanupCachedData {}
        [RequiredByNativeCode]
        public struct UpdateMainGameViewRect {}
        [RequiredByNativeCode]
        public struct UpdateCanvasRectTransform {}
        [RequiredByNativeCode]
        public struct UpdateInputManager {}
        [RequiredByNativeCode]
        public struct ProcessRemoteInput {}
        [RequiredByNativeCode]
        public struct XRUpdate {}
        [RequiredByNativeCode]
        public struct ScriptRunDelayedStartupFrame {}
        [RequiredByNativeCode]
        public struct UpdateKinect {}
        [RequiredByNativeCode]
        public struct DeliverIosPlatformEvents {}
        [RequiredByNativeCode]
        public struct DispatchEventQueueEvents {}
        [RequiredByNativeCode]
        public struct DirectorSampleTime {}
        [RequiredByNativeCode]
        public struct PhysicsResetInterpolatedTransformPosition {}
        [RequiredByNativeCode]
        public struct NewInputBeginFrame {}
        [RequiredByNativeCode]
        public struct SpriteAtlasManagerUpdate {}
        [RequiredByNativeCode]
        public struct TangoUpdate {}
        [RequiredByNativeCode]
        public struct PerformanceAnalyticsUpdate {}
    }
    [RequiredByNativeCode]
    public struct FixedUpdate
    {
        [RequiredByNativeCode]
        public struct ClearLines {}
        [RequiredByNativeCode]
        public struct DirectorFixedSampleTime {}
        [RequiredByNativeCode]
        public struct AudioFixedUpdate {}
        [RequiredByNativeCode]
        public struct ScriptRunBehaviourFixedUpdate {}
        [RequiredByNativeCode]
        public struct DirectorFixedUpdate {}
        [RequiredByNativeCode]
        public struct LegacyFixedAnimationUpdate {}
        [RequiredByNativeCode]
        public struct XRFixedUpdate {}
        [RequiredByNativeCode]
        public struct PhysicsFixedUpdate {}
        [RequiredByNativeCode]
        public struct Physics2DFixedUpdate {}
        [RequiredByNativeCode]
        public struct DirectorFixedUpdatePostPhysics {}
        [RequiredByNativeCode]
        public struct ScriptRunDelayedFixedFrameRate {}
        [RequiredByNativeCode]
        public struct ScriptRunDelayedTasks {}
        [RequiredByNativeCode]
        public struct NewInputBeginFixedUpdate {}
        [RequiredByNativeCode]
        public struct NewInputEndFixedUpdate {}
    }
    [RequiredByNativeCode]
    public struct PreUpdate
    {
        [RequiredByNativeCode]
        public struct PhysicsUpdate {}
        [RequiredByNativeCode]
        public struct Physics2DUpdate {}
        [RequiredByNativeCode]
        public struct CheckTexFieldInput {}
        [RequiredByNativeCode]
        public struct IMGUISendQueuedEvents {}
        [RequiredByNativeCode]
        public struct SendMouseEvents {}
        [RequiredByNativeCode]
        public struct AIUpdate {}
        [RequiredByNativeCode]
        public struct WindUpdate {}
        [RequiredByNativeCode]
        public struct UpdateVideo {}
        [RequiredByNativeCode]
        public struct NewInputUpdate {}
    }
    [RequiredByNativeCode]
    public struct Update
    {
        [RequiredByNativeCode]
        public struct ScriptRunBehaviourUpdate {}
        [RequiredByNativeCode]
        public struct DirectorUpdate {}
        [RequiredByNativeCode]
        public struct ScriptRunDelayedDynamicFrameRate {}
    }
    [RequiredByNativeCode]
    public struct PreLateUpdate
    {
        [RequiredByNativeCode]
        public struct AIUpdatePostScript {}
        [RequiredByNativeCode]
        public struct DirectorUpdateAnimationBegin {}
        [RequiredByNativeCode]
        public struct LegacyAnimationUpdate {}
        [RequiredByNativeCode]
        public struct DirectorUpdateAnimationEnd {}
        [RequiredByNativeCode]
        public struct DirectorDeferredEvaluate {}
        [RequiredByNativeCode]
        public struct UpdateNetworkManager {}
        [RequiredByNativeCode]
        public struct UpdateMasterServerInterface {}
        [RequiredByNativeCode]
        public struct UNetUpdate {}
        [RequiredByNativeCode]
        public struct EndGraphicsJobsLate {}
        [RequiredByNativeCode]
        public struct ParticleSystemBeginUpdateAll {}
        [RequiredByNativeCode]
        public struct ScriptRunBehaviourLateUpdate {}
        [RequiredByNativeCode]
        public struct ConstraintManagerUpdate {}
    }
    [RequiredByNativeCode]
    public struct PostLateUpdate
    {
        [RequiredByNativeCode]
        public struct PlayerSendFrameStarted {}
        [RequiredByNativeCode]
        public struct UpdateCanvasRectTransform {}
        [RequiredByNativeCode]
        public struct PlayerUpdateCanvases {}
        [RequiredByNativeCode]
        public struct UpdateAudio {}
        [RequiredByNativeCode]
        public struct UpdateVideo {}
        [RequiredByNativeCode]
        public struct DirectorLateUpdate {}
        [RequiredByNativeCode]
        public struct ScriptRunDelayedDynamicFrameRate {}
        [RequiredByNativeCode]
        public struct ParticlesLegacyUpdateAllParticleSystems {}
        [RequiredByNativeCode]
        public struct ParticleSystemEndUpdateAll {}
        [RequiredByNativeCode]
        public struct UpdateSubstance {}
        [RequiredByNativeCode]
        public struct UpdateCustomRenderTextures {}
        [RequiredByNativeCode]
        public struct UpdateAllRenderers {}
        [RequiredByNativeCode]
        public struct EnlightenRuntimeUpdate {}
        [RequiredByNativeCode]
        public struct UpdateAllSkinnedMeshes {}
        [RequiredByNativeCode]
        public struct ProcessWebSendMessages {}
        [RequiredByNativeCode]
        public struct SortingGroupsUpdate {}
        [RequiredByNativeCode]
        public struct UpdateVideoTextures {}
        [RequiredByNativeCode]
        public struct DirectorRenderImage {}
        [RequiredByNativeCode]
        public struct PlayerEmitCanvasGeometry {}
        [RequiredByNativeCode]
        public struct FinishFrameRendering {}
        [RequiredByNativeCode]
        public struct BatchModeUpdate {}
        [RequiredByNativeCode]
        public struct PlayerSendFrameComplete {}
        [RequiredByNativeCode]
        public struct UpdateCaptureScreenshot {}
        [RequiredByNativeCode]
        public struct PresentAfterDraw {}
        [RequiredByNativeCode]
        public struct ClearImmediateRenderers {}
        [RequiredByNativeCode]
        public struct XRPostPresent {}
        [RequiredByNativeCode]
        public struct UpdateResolution {}
        [RequiredByNativeCode]
        public struct InputEndFrame {}
        [RequiredByNativeCode]
        public struct GUIClearEvents {}
        [RequiredByNativeCode]
        public struct ShaderHandleErrors {}
        [RequiredByNativeCode]
        public struct ResetInputAxis {}
        [RequiredByNativeCode]
        public struct ThreadedLoadingDebug {}
        [RequiredByNativeCode]
        public struct ProfilerSynchronizeStats {}
        [RequiredByNativeCode]
        public struct MemoryFrameMaintenance {}
        [RequiredByNativeCode]
        public struct ExecuteGameCenterCallbacks {}
        [RequiredByNativeCode]
        public struct ProfilerEndFrame {}
        [RequiredByNativeCode]
        public struct PlayerSendFramePostPresent {}
        [RequiredByNativeCode]
        public struct PhysicsSkinnedClothBeginUpdate {}
        [RequiredByNativeCode]
        public struct PhysicsSkinnedClothFinishUpdate {}
        [RequiredByNativeCode]
        public struct TriggerEndOfFrameCallbacks {}
    }
}

namespace UnityEngine.Experimental.LowLevel
{
    [NativeType(Header = "Runtime/Misc/PlayerLoop.h")]
    [RequiredByNativeCode]

    struct PlayerLoopSystemInternal
    {
        public Type type;
        public PlayerLoopSystem.UpdateFunction updateDelegate;
        public IntPtr updateFunction;
        public IntPtr loopConditionFunction;
        public int numSubSystems;
    }

    public struct PlayerLoopSystem
    {
        public Type type;
        public PlayerLoopSystem[] subSystemList;
        public UpdateFunction updateDelegate;
        public IntPtr updateFunction;
        public IntPtr loopConditionFunction;

        public delegate void UpdateFunction();
    }

    public class PlayerLoop
    {
        public static PlayerLoopSystem GetDefaultPlayerLoop()
        {
            var intSys = GetDefaultPlayerLoopInternal();
            var offset = 0;
            return InternalToPlayerLoopSystem(intSys, ref offset);
        }

        public static void SetPlayerLoop(PlayerLoopSystem loop)
        {
            var intSys = new List<PlayerLoopSystemInternal>();
            PlayerLoopSystemToInternal(loop, ref intSys);
            SetPlayerLoopInternal(intSys.ToArray());
        }

        static int PlayerLoopSystemToInternal(PlayerLoopSystem sys, ref List<PlayerLoopSystemInternal> internalSys)
        {
            var idx = internalSys.Count;
            var newSys = new PlayerLoopSystemInternal
            {
                type = sys.type,
                updateDelegate = sys.updateDelegate,
                updateFunction = sys.updateFunction,
                loopConditionFunction = sys.loopConditionFunction,
                numSubSystems = 0
            };
            internalSys.Add(newSys);
            if (sys.subSystemList != null)
            {
                for (int i = 0; i < sys.subSystemList.Length; ++i)
                {
                    newSys.numSubSystems += PlayerLoopSystemToInternal(sys.subSystemList[i], ref internalSys);
                }
            }
            internalSys[idx] = newSys;
            return newSys.numSubSystems + 1;
        }

        static PlayerLoopSystem InternalToPlayerLoopSystem(PlayerLoopSystemInternal[] internalSys, ref int offset)
        {
            var sys = new PlayerLoopSystem
            {
                type = internalSys[offset].type,
                updateDelegate = internalSys[offset].updateDelegate,
                updateFunction = internalSys[offset].updateFunction,
                loopConditionFunction = internalSys[offset].loopConditionFunction,
                subSystemList = null
            };

            var idx = offset++;
            if (internalSys[idx].numSubSystems > 0)
            {
                var subsys = new List<PlayerLoopSystem>();
                while (offset <= idx + internalSys[idx].numSubSystems)
                    subsys.Add(InternalToPlayerLoopSystem(internalSys, ref offset));
                sys.subSystemList = subsys.ToArray();
            }

            return sys;
        }


        [NativeMethod(IsFreeFunction = true)]
        private static extern PlayerLoopSystemInternal[] GetDefaultPlayerLoopInternal();
        [NativeMethod(IsFreeFunction = true)]
        private static extern void SetPlayerLoopInternal(PlayerLoopSystemInternal[] loop);
    }
}
