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

    partial class ParticleSystem
    {
        public partial struct MinMaxCurve
        {
            [Obsolete("Please use MinMaxCurve.curveMultiplier instead. (UnityUpgradable) -> UnityEngine.ParticleSystem/MinMaxCurve.curveMultiplier", false)]
            public float curveScalar { get { return m_CurveMultiplier; } set { m_CurveMultiplier = value; } }
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
    }

    static partial class ParticlePhysicsExtensions
    {
        [Obsolete("GetCollisionEvents function using ParticleCollisionEvent[] is deprecated. Use List<ParticleCollisionEvent> instead.", false)]
        public static int GetCollisionEvents(this ParticleSystem ps, GameObject go, ParticleCollisionEvent[] collisionEvents)
        {
            if (go == null) throw new ArgumentNullException("go");
            if (collisionEvents == null) throw new ArgumentNullException("collisionEvents");

            return ParticleSystemExtensionsImpl.GetCollisionEventsDeprecated(ps, go, collisionEvents);
        }
    }

    partial struct ParticleCollisionEvent
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("collider property is deprecated. Use colliderComponent instead, which supports Collider and Collider2D components (UnityUpgradable) -> colliderComponent", true)]
        public Component collider
        {
            get { throw new InvalidOperationException("collider property is deprecated. Use colliderComponent instead, which supports Collider and Collider2D components"); }
        }
    }
}
