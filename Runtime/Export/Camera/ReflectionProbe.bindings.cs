// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Internal;
using System.Collections.Generic;
using System.Linq;

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

        public static event Action<ReflectionProbe, ReflectionProbeEvent> reflectionProbeChanged;
        [UnityEngine.Scripting.RequiredByNativeCode]
        private static void CallReflectionProbeEvent(ReflectionProbe probe, ReflectionProbeEvent probeEvent)
        {
            var callback = reflectionProbeChanged;
            if (callback != null)
                callback(probe, probeEvent);
        }

        // This is a temporary solution for the deprecation of defaultReflectionSet (in favor of defaultReflectionTexture)
        // As this is a breaking API change, we hook callback registering to the old event and also invoke them when invoking the new event.
        // This will make sure we invoke both the old and the new event handlers
        // To be removed once we fully deprecate/remove defaultReflectionSet
        static Dictionary<int, Action<Texture>> registeredDefaultReflectionSetActions = new Dictionary<int, Action<Texture>>();
        static List<Action<Texture>> registeredDefaultReflectionTextureActions = new List<Action<Texture>>();

        [Obsolete("ReflectionProbe.defaultReflectionSet has been deprecated. Use ReflectionProbe.defaultReflectionTexture. (UnityUpgradable) -> UnityEngine.ReflectionProbe.defaultReflectionTexture", false)]
        public static event Action<Cubemap> defaultReflectionSet
        {
            add
            {
                // Avoids the same handler being added to old/new event.
                // This assumes we'll not have multiple threads trying to register for the event concurrently; if that may hapen then we need to protect this (lock(registeredDefaultReflectionTextureActions) { ... })
                if (registeredDefaultReflectionTextureActions.Any(h => h.Method == value.Method))
                {
                    return;
                }
                // Every time someone registers for listening for OldEvent we have an allocation.
                Action<Texture> f = (b) =>
                {
                    if (b is Cubemap d)
                        value(d);
                };

                defaultReflectionTexture += f;

                // This assumes we'll not have multiple threads trying to register for the event concurrently; if that may hapen then we need to protect this assignment (lock(registeredDefaultReflectionSetActions) { ... })
                registeredDefaultReflectionSetActions[value.Method.GetHashCode()] = f;
            }

            remove
            {
                if (registeredDefaultReflectionSetActions.TryGetValue(value.Method.GetHashCode(), out var f))
                {
                    defaultReflectionTexture -= f;
                    registeredDefaultReflectionSetActions.Remove(value.Method.GetHashCode());
                }
            }
        }
        public static event Action<Texture> defaultReflectionTexture
        {
            add
            {
                // Avoids the same handler being added to old/new event.
                if (registeredDefaultReflectionTextureActions.Any(h => h.Method == value.Method)
                    || registeredDefaultReflectionSetActions.ContainsKey(value.Method.GetHashCode()))
                {
                    return;
                }

                registeredDefaultReflectionTextureActions.Add(value);
            }

            remove
            {
                registeredDefaultReflectionTextureActions.Remove(value);
            }
        }
        [UnityEngine.Scripting.RequiredByNativeCode]
        private static void CallSetDefaultReflection(Texture defaultReflectionCubemap)
        {
            foreach (var callback in registeredDefaultReflectionTextureActions)
                callback(defaultReflectionCubemap);
        }
    }
}
