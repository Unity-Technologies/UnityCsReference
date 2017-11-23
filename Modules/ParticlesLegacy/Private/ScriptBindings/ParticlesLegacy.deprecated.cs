// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    [Obsolete("This component is part of the legacy particle system, which is deprecated and will be removed in a future release. Use the ParticleSystem component instead.", false)]
    public class ParticleEmitter : Component
    {
    }

    [Obsolete("This component is part of the legacy particle system, which is deprecated and will be removed in a future release. Use the ParticleSystem component instead.", false)]
    [RequireComponent(typeof(Transform))]
    public class EllipsoidParticleEmitter : ParticleEmitter
    {
        internal EllipsoidParticleEmitter() {}
    }

    [Obsolete("This component is part of the legacy particle system, which is deprecated and will be removed in a future release. Use the ParticleSystem component instead.", false)]
    public class MeshParticleEmitter : ParticleEmitter
    {
        internal MeshParticleEmitter() {}
    }

    [Obsolete("This component is part of the legacy particle system, which is deprecated and will be removed in a future release. Use the ParticleSystem component instead.", false)]
    [RequireComponent(typeof(Transform))]
    public class ParticleAnimator : Component
    {
    }

    [Obsolete("This component is part of the legacy particle system, which is deprecated and will be removed in a future release. Use the ParticleSystem component instead.", false)]
    public class ParticleRenderer : Renderer
    {
    }

    [Obsolete("This component is part of the legacy particle system, which is deprecated and will be removed in a future release. Use the ParticleSystem component instead.", false)]
    [RequireComponent(typeof(Transform))]
    internal class WorldParticleCollider : Component
    {
    }
}
