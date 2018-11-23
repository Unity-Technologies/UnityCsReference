// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using MinMaxCurve = UnityEngine.ParticleSystem.MinMaxCurve;

namespace UnityEngine
{
    [NativeHeader("ParticleSystemScriptingClasses.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystem.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystemForceField.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystemForceFieldManager.h")]
    [NativeHeader("Modules/ParticleSystem/ScriptBindings/ParticleSystemScriptBindings.h")]
    [RequireComponent(typeof(Transform))]
    public partial class ParticleSystemForceField : Component
    {
        [NativeName("ForceShape")]
        extern public ParticleSystemForceFieldShape shape { get; set; }
        extern public float startRange { get; set; }
        extern public float endRange { get; set; }
        extern public float length { get; set; }
        extern public float gravityFocus { get; set; }
        extern public Vector2 rotationRandomness { get; set; }
        extern public bool multiplyDragByParticleSize { get; set; }
        extern public bool multiplyDragByParticleVelocity { get; set; }
        extern public Texture3D vectorField { get; set; }
        extern public MinMaxCurve directionX { get; set; }
        extern public MinMaxCurve directionY { get; set; }
        extern public MinMaxCurve directionZ { get; set; }
        extern public MinMaxCurve gravity { get; set; }
        extern public MinMaxCurve rotationSpeed { get; set; }
        extern public MinMaxCurve rotationAttraction { get; set; }
        extern public MinMaxCurve drag { get; set; }
        extern public MinMaxCurve vectorFieldSpeed { get; set; }
        extern public MinMaxCurve vectorFieldAttraction { get; set; }

        [StaticAccessor("GetParticleSystemForceFieldManager()", StaticAccessorType.Dot)]
        [NativeMethod("GetForceFields")]
        extern public static ParticleSystemForceField[] FindAll();
    }
}
