// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEngine
{
    [NativeHeader("Runtime/ParticleSystem/ParticleSystemRenderer.h")]
    [NativeHeader("ParticleSystemScriptingClasses.h")]
    [RequireComponent(typeof(Transform))]
    public partial class ParticleSystemRenderer : Renderer
    {
        [NativeName("RenderAlignment")]
        extern public ParticleSystemRenderSpace alignment { get; set; }
        extern public ParticleSystemRenderMode renderMode { get; set; }
        extern public ParticleSystemSortMode sortMode { get; set; }

        extern public float lengthScale { get; set; }
        extern public float velocityScale { get; set; }
        extern public float cameraVelocityScale { get; set; }

        extern public float normalDirection { get; set; }
        extern public float sortingFudge { get; set; }
        extern public float minParticleSize { get; set; }
        extern public float maxParticleSize { get; set; }
        extern public Vector3 pivot { get; set; }
        extern public SpriteMaskInteraction maskInteraction { get; set; }
        extern public Material trailMaterial { get; set; }
        extern public bool enableGPUInstancing { get; set; }

        extern internal bool editorEnabled { get; set; }
    }
}
