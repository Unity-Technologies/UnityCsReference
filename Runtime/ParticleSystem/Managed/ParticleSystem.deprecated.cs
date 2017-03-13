// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    partial class ParticleSystem
    {
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
