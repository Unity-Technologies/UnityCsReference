// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    [Obsolete("ParticleSystemEmissionType no longer does anything. Time and Distance based emission are now both always active.", false)]
    public enum ParticleSystemEmissionType
    {
        Time = 0,
        Distance = 1
    }

    public partial class ParticleSystem
    {
        public partial struct MinMaxCurve
        {
            [Obsolete("Please use MinMaxCurve.curveMultiplier instead. (UnityUpgradable) -> UnityEngine.ParticleSystem/MinMaxCurve.curveMultiplier", false)]
            public float curveScalar { get { return m_CurveMultiplier; } set { m_CurveMultiplier = value; } }
        }

        public partial struct MainModule
        {
            [Obsolete("Please use flipRotation instead. (UnityUpgradable) -> UnityEngine.ParticleSystem/MainModule.flipRotation", false)]
            public float randomizeRotationDirection { get { return flipRotation; } set { flipRotation = value; } }
        }

        public partial struct EmissionModule
        {
            [Obsolete("ParticleSystemEmissionType no longer does anything. Time and Distance based emission are now both always active.", false)]
            public ParticleSystemEmissionType type { get { return ParticleSystemEmissionType.Time; } set {} }
            [Obsolete("rate property is deprecated. Use rateOverTime or rateOverDistance instead.", false)]
            public MinMaxCurve rate { get { return rateOverTime; } set { rateOverTime = value; } }
            [Obsolete("rateMultiplier property is deprecated. Use rateOverTimeMultiplier or rateOverDistanceMultiplier instead.", false)]
            public float rateMultiplier { get { return rateOverTimeMultiplier; } set { rateOverTimeMultiplier = value; } }
        }

        public partial struct ShapeModule
        {
            [Obsolete("Please use scale instead. (UnityUpgradable) -> UnityEngine.ParticleSystem/ShapeModule.scale", false)]
            public Vector3 box { get { return GetScale(m_ParticleSystem); } set { SetScale(m_ParticleSystem, value); } }
            // Scale. (Meshes)
            [Obsolete("meshScale property is deprecated.Please use scale instead.", false)]
            public float meshScale { get { return scale.x; } set { scale = new Vector3(value, value, value); } }
            [Obsolete("randomDirection property is deprecated.Use randomDirectionAmount instead.", false)]
            public bool randomDirection { get { return (randomDirectionAmount >= 0.5f); } set { randomDirectionAmount = value ? 1.0f : 0.0f; } }
        }

        public partial struct CollisionModule
        {
            [Obsolete("enableInteriorCollisions property is deprecated and is no longer required and has no effect on the particle system.", false)]
            public bool enableInteriorCollisions { get { return GetEnableInteriorCollisions(m_ParticleSystem); } set { SetEnableInteriorCollisions(m_ParticleSystem, value); } }
        }

        public partial struct SubEmittersModule
        {
            [Obsolete("birth0 property is deprecated.Use AddSubEmitter, RemoveSubEmitter, SetSubEmitterSystem and GetSubEmitterSystem instead.", false)]
            public ParticleSystem birth0 { get { return GetBirth(m_ParticleSystem, 0); } set { SetBirth(m_ParticleSystem, 0, value); } }
            [Obsolete("birth1 property is deprecated.Use AddSubEmitter, RemoveSubEmitter, SetSubEmitterSystem and GetSubEmitterSystem instead.", false)]
            public ParticleSystem birth1 { get { return GetBirth(m_ParticleSystem, 1); } set { SetBirth(m_ParticleSystem, 1, value); } }
            [Obsolete("collision0 property is deprecated.Use AddSubEmitter, RemoveSubEmitter, SetSubEmitterSystem and GetSubEmitterSystem instead.", false)]
            public ParticleSystem collision0 { get { return GetCollision(m_ParticleSystem, 0); } set { SetCollision(m_ParticleSystem, 0, value); } }
            [Obsolete("collision1 property is deprecated.Use AddSubEmitter, RemoveSubEmitter, SetSubEmitterSystem and GetSubEmitterSystem instead.", false)]
            public ParticleSystem collision1 { get { return GetCollision(m_ParticleSystem, 1); } set { SetCollision(m_ParticleSystem, 1, value); } }
            [Obsolete("death0 property is deprecated.Use AddSubEmitter, RemoveSubEmitter, SetSubEmitterSystem and GetSubEmitterSystem instead.", false)]
            public ParticleSystem death0 { get { return GetDeath(m_ParticleSystem, 0); } set { SetDeath(m_ParticleSystem, 0, value); } }
            [Obsolete("death1 property is deprecated.Use AddSubEmitter, RemoveSubEmitter, SetSubEmitterSystem and GetSubEmitterSystem instead.", false)]
            public ParticleSystem death1 { get { return GetDeath(m_ParticleSystem, 1); } set { SetDeath(m_ParticleSystem, 1, value); } }
        }

        public partial struct Particle
        {
            [Obsolete("Please use Particle.remainingLifetime instead. (UnityUpgradable) -> UnityEngine.ParticleSystem/Particle.remainingLifetime", false)]
            public float lifetime { get { return remainingLifetime; } set { remainingLifetime = value; } }
            [Obsolete("randomValue property is deprecated.Use randomSeed instead to control random behavior of particles.", false)]
            public float randomValue { get { return BitConverter.ToSingle(BitConverter.GetBytes(m_RandomSeed), 0); } set { m_RandomSeed = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0); } }
            [Obsolete("size property is deprecated.Use startSize or GetCurrentSize() instead.", false)]
            public float size { get { return startSize; } set { startSize = value; } }
            [Obsolete("color property is deprecated.Use startColor or GetCurrentColor() instead.", false)]
            public Color32 color { get { return startColor; } set { startColor = value; } }
        }

        [Obsolete("ParticleSystem.CollisionEvent has been deprecated. Use ParticleCollisionEvent instead (UnityUpgradable)", true)]
        public struct CollisionEvent
        {
            public Vector3 intersection { get { return default(Vector3); } }
            public Vector3 normal { get { return default(Vector3); } }
            public Vector3 velocity { get { return default(Vector3); } }
            public Component collider { get { return default(Component); } }
        }

        [Obsolete("safeCollisionEventSize has been deprecated. Use GetSafeCollisionEventSize() instead (UnityUpgradable) -> ParticlePhysicsExtensions.GetSafeCollisionEventSize(UnityEngine.ParticleSystem)", false)]
        public int safeCollisionEventSize { get { return ParticleSystemExtensionsImpl.GetSafeCollisionEventSize(this); } }

        [Obsolete("Emit with specific parameters is deprecated. Pass a ParticleSystem.EmitParams parameter instead, which allows you to override some/all of the emission properties", false)]
        public void Emit(Vector3 position, Vector3 velocity, float size, float lifetime, Color32 color)
        {
            ParticleSystem.Particle particle = new ParticleSystem.Particle();
            particle.position = position;
            particle.velocity = velocity;
            particle.lifetime = lifetime;
            particle.startLifetime = lifetime;
            particle.startSize = size;
            particle.rotation3D = Vector3.zero;
            particle.angularVelocity3D = Vector3.zero;
            particle.startColor = color;
            particle.randomSeed = 5;
            Internal_EmitOld(ref particle);
        }

        [Obsolete("Emit with a single particle structure is deprecated. Pass a ParticleSystem.EmitParams parameter instead, which allows you to override some/all of the emission properties", false)]
        public void Emit(ParticleSystem.Particle particle)
        {
            Internal_EmitOld(ref particle);
        }

        [Obsolete("startDelay property is deprecated.Use main.startDelay or main.startDelayMultiplier instead.", false)]
        public float startDelay { get { return main.startDelayMultiplier; } set { var m = main; m.startDelayMultiplier = value; } }

        [Obsolete("loop property is deprecated.Use main.loop instead.", false)]
        public bool loop { get { return main.loop; } set { var m = main; m.loop = value; } }

        [Obsolete("playOnAwake property is deprecated.Use main.playOnAwake instead.", false)]
        public bool playOnAwake { get { return main.playOnAwake; } set { var m = main; m.playOnAwake = value; } }

        [Obsolete("duration property is deprecated.Use main.duration instead.", false)]
        public float duration { get { return main.duration; } }

        [Obsolete("playbackSpeed property is deprecated.Use main.simulationSpeed instead.", false)]
        public float playbackSpeed { get { return main.simulationSpeed; } set { var m = main; m.simulationSpeed = value; } }

        [Obsolete("enableEmission property is deprecated.Use emission.enabled instead.", false)]
        public bool enableEmission { get { return emission.enabled; } set { var em = emission; em.enabled = value; } }

        [Obsolete("emissionRate property is deprecated.Use emission.rateOverTime, emission.rateOverDistance, emission.rateOverTimeMultiplier or emission.rateOverDistanceMultiplier instead.", false)]
        public float emissionRate { get { return emission.rateOverTimeMultiplier; } set { var em = emission; em.rateOverTime = value; } }

        [Obsolete("startSpeed property is deprecated.Use main.startSpeed or main.startSpeedMultiplier instead.", false)]
        public float startSpeed { get { return main.startSpeedMultiplier; } set { var m = main; m.startSpeedMultiplier = value; } }

        [Obsolete("startSize property is deprecated.Use main.startSize or main.startSizeMultiplier instead.", false)]
        public float startSize { get { return main.startSizeMultiplier; } set { var m = main; m.startSizeMultiplier = value; } }

        [Obsolete("startColor property is deprecated.Use main.startColor instead.", false)]
        public Color startColor { get { return main.startColor.color; } set { var m = main; m.startColor = value; } }

        [Obsolete("startRotation property is deprecated.Use main.startRotation or main.startRotationMultiplier instead.", false)]
        public float startRotation { get { return main.startRotationMultiplier; } set { var m = main; m.startRotationMultiplier = value; } }

        [Obsolete("startRotation3D property is deprecated.Use main.startRotationX, main.startRotationY and main.startRotationZ instead. (Or main.startRotationXMultiplier, main.startRotationYMultiplier and main.startRotationZMultiplier).", false)]
        public Vector3 startRotation3D { get { return new Vector3(main.startRotationXMultiplier, main.startRotationYMultiplier, main.startRotationZMultiplier); } set { var m = main; m.startRotationXMultiplier = value.x; m.startRotationYMultiplier = value.y; m.startRotationZMultiplier = value.z; } }

        [Obsolete("startLifetime property is deprecated.Use main.startLifetime or main.startLifetimeMultiplier instead.", false)]
        public float startLifetime { get { return main.startLifetimeMultiplier; } set { var m = main; m.startLifetimeMultiplier = value; } }

        [Obsolete("gravityModifier property is deprecated.Use main.gravityModifier or main.gravityModifierMultiplier instead.", false)]
        public float gravityModifier { get { return main.gravityModifierMultiplier; } set { var m = main; m.gravityModifierMultiplier = value; } }

        [Obsolete("maxParticles property is deprecated.Use main.maxParticles instead.", false)]
        public int maxParticles { get { return main.maxParticles; } set { var m = main; m.maxParticles = value; } }

        [Obsolete("simulationSpace property is deprecated.Use main.simulationSpace instead.", false)]
        public ParticleSystemSimulationSpace simulationSpace { get { return main.simulationSpace; } set { var m = main; m.simulationSpace = value; } }

        [Obsolete("scalingMode property is deprecated.Use main.scalingMode instead.", false)]
        public ParticleSystemScalingMode scalingMode { get { return main.scalingMode; } set { var m = main; m.scalingMode = value; } }
    }

    public static partial class ParticlePhysicsExtensions
    {
        [Obsolete("GetCollisionEvents function using ParticleCollisionEvent[] is deprecated. Use List<ParticleCollisionEvent> instead.", false)]
        public static int GetCollisionEvents(this ParticleSystem ps, GameObject go, ParticleCollisionEvent[] collisionEvents)
        {
            if (go == null) throw new ArgumentNullException("go");
            if (collisionEvents == null) throw new ArgumentNullException("collisionEvents");

            return ParticleSystemExtensionsImpl.GetCollisionEventsDeprecated(ps, go, collisionEvents);
        }
    }

    public partial struct ParticleCollisionEvent
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("collider property is deprecated. Use colliderComponent instead, which supports Collider and Collider2D components (UnityUpgradable) -> colliderComponent", true)]
        public Component collider
        {
            get { throw new InvalidOperationException("collider property is deprecated. Use colliderComponent instead, which supports Collider and Collider2D components"); }
        }
    }
}
