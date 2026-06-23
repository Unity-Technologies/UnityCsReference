// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Internal;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    [NativeHeader("Runtime/Camera/ReflectionProbes.h")]
    public sealed partial class ReflectionProbe : Behaviour
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("type property has been deprecated. Starting with Unity 5.4, the only supported reflection probe type is Cube.", true)]
        [NativeName("ProbeType")]
        public extern ReflectionProbeType type { get; set; }

        [NativeName("BoxSize")]
        public extern Vector3 size { get; set; }

        [NativeName("BoxOffset")]
        public extern Vector3 center { get; set; }

        [NativeName("Near")]
        public extern float nearClipPlane { get; set; }

        [NativeName("Far")]
        public extern float farClipPlane { get; set; }

        [NativeName("IntensityMultiplier")]
        public extern float intensity { get; set; }

        [NativeName("GlobalAABB")]
        public extern Bounds bounds { get; }

        [NativeName("HDR")]
        public extern bool hdr { get; set; }

        [NativeName("RenderDynamicObjects")]
        public extern bool renderDynamicObjects { get; set; }

        public extern float shadowDistance { get; set; }
        public extern int resolution { get; set; }
        public extern int cullingMask { get; set; }
        public extern ReflectionProbeClearFlags clearFlags { get; set; }
        public extern Color backgroundColor { get; set; }
        public extern float blendDistance { get; set; }
        public extern bool boxProjection { get; set; }
        public extern ReflectionProbeMode mode { get; set; }
        public extern int importance { get; set; }
        public extern ReflectionProbeRefreshMode refreshMode { get; set; }
        public extern ReflectionProbeTimeSlicingMode timeSlicingMode { get; set; }
        public extern Texture bakedTexture { get; set; }
        public extern Texture customBakedTexture { get; set; }
        public extern RenderTexture realtimeTexture { get; set; }
        public extern Texture texture { get; }
        public extern Vector4 textureHDRDecodeValues {[NativeName("CalculateHDRDecodeValues")] get; }

        public extern void Reset();

        public int RenderProbe()
        {
            return RenderProbe(null);
        }

        public int RenderProbe([DefaultValue("null")] RenderTexture targetTexture)
        {
            return ScheduleRender(timeSlicingMode, targetTexture);
        }

        public extern bool IsFinishedRendering(int renderId);

        private extern int ScheduleRender(ReflectionProbeTimeSlicingMode timeSlicingMode, RenderTexture targetTexture);

        [NativeHeader("Runtime/Camera/CubemapGPUUtility.h")]
        [FreeFunction("CubemapGPUBlend")]
        public static extern bool BlendCubemap(Texture src, Texture dst, float blend, RenderTexture target);

        [StaticAccessor("GetReflectionProbes()")]
        [NativeMethod("UpdateSampleData")]
        public static extern void UpdateCachedState();

        [StaticAccessor("GetReflectionProbes()")]
        public static extern int minBakedCubemapResolution { get; }

        [StaticAccessor("GetReflectionProbes()")]
        public static extern int maxBakedCubemapResolution { get; }

        [StaticAccessor("GetReflectionProbes()")]
        public static extern Vector4 defaultTextureHDRDecodeValues { get; }

        [StaticAccessor("GetReflectionProbes()")]
        public static extern Texture defaultTexture { get; }

        // Keep in synch with ReflectionProbeScriptingEvent from ReflectionProbes.h
        public enum ReflectionProbeEvent
        {
            // New reflection probe was created
            ReflectionProbeAdded = 0,

            // Reflection probe was removed/disabled
            ReflectionProbeRemoved = 1,
        }

        [AutoStaticsCleanupOnCodeReload]
        public static event Action<ReflectionProbe, ReflectionProbeEvent> reflectionProbeChanged;
        [UnityEngine.Scripting.RequiredByNativeCode]
        private static void CallReflectionProbeEvent(ReflectionProbe probe, ReflectionProbeEvent probeEvent)
        {
            var callback = reflectionProbeChanged;
            if (callback != null)
                callback(probe, probeEvent);
        }

        [Obsolete("ReflectionProbe.defaultReflectionSet has been deprecated. Use ReflectionProbe.defaultReflectionTexture. (UnityUpgradable) -> UnityEngine.ReflectionProbe.defaultReflectionTexture", true)]
        public static event Action<Cubemap> defaultReflectionSet
        {
            add
            {
                 throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        [AutoStaticsCleanupOnCodeReload]
        internal static Action<Texture> s_DefaultReflectionTexture;

        public static event Action<Texture> defaultReflectionTexture
        {
            add
            {
                if (!(s_DefaultReflectionTexture?.GetInvocationList().ContainsByEquals(value) ?? false))
                    s_DefaultReflectionTexture += value;
            }
            remove
            {
                s_DefaultReflectionTexture -= value;
            }
        }

        [UnityEngine.Scripting.RequiredByNativeCode]
        private static void CallSetDefaultReflection(Texture defaultReflectionCubemap)
        {
            s_DefaultReflectionTexture?.Invoke(defaultReflectionCubemap);
        }
    }
}
