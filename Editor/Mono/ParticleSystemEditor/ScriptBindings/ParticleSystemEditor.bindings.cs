// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [NativeHeader("Runtime/ParticleSystem/ParticleSystem.h")]
    [NativeHeader("Editor/Src/ParticleSystem/ParticleSystemEditor.h")]
    [NativeHeader("ParticleSystemScriptingClasses.h")]
    [StaticAccessor("ParticleSystemEditor", StaticAccessorType.DoubleColon)]
    internal static class ParticleSystemEditorUtils
    {
        internal extern static float simulationSpeed { get; set; }
        internal extern static float playbackTime { get; set; }
        internal extern static bool playbackIsScrubbing { get; set; }
        internal extern static bool playbackIsPlaying { get; set; }
        internal extern static bool playbackIsPaused { get; set; }
        internal extern static bool resimulation { get; set; }
        internal extern static UInt32 previewLayers { get; set; }
        internal extern static bool renderInSceneView { get; set; }
        internal extern static ParticleSystem lockedParticleSystem { get; set; }

        [NativeName("SetPerformCompleteResimulation")]
        extern internal static void PerformCompleteResimulation();

        // Returns the root of the hierarchy of Particle Systems starting from 'ps'.
        public static ParticleSystem GetRoot(ParticleSystem ps)
        {
            if (ps == null)
                return null;

            Transform rootTransform = ps.transform;
            while (rootTransform.parent && rootTransform.parent.gameObject.GetComponent<ParticleSystem>() != null)
                rootTransform = rootTransform.parent;

            return rootTransform.gameObject.GetComponent<ParticleSystem>();
        }
    }

    [NativeHeader("Runtime/ParticleSystem/ParticleSystem.h")]
    [NativeHeader("Editor/Src/ParticleSystem/ParticleSystemEffect.h")]
    [StaticAccessor("ParticleSystemEffect", StaticAccessorType.DoubleColon)]
    internal static class ParticleSystemEffectUtils
    {
        extern internal static string CheckCircularReferences(ParticleSystem subEmitter);

        [NativeName("StopAndClearActive")]
        extern internal static void StopEffect();
    }
}
