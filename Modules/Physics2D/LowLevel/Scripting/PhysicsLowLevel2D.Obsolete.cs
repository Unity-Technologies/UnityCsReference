// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace UnityEngine.LowLevelPhysics2D
{
    public readonly partial struct PhysicsWorld
    {
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorld.simulationMode has been deprecated. Please use PhysicsWorld.simulationType instead.", false)]
        public readonly SimulationMode2D simulationMode { get => (SimulationMode2D)simulationType; set => simulationType = (PhysicsWorld.SimulationType)value; }
    }

    public partial struct PhysicsWorldDefinition
    {
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsWorldDefinition.simulationMode has been deprecated. Please use PhysicsWorldDefinition.simulateType instead.", false)]
        public SimulationMode2D simulationMode { readonly get => (SimulationMode2D)simulateType; set => simulateType = (PhysicsWorld.SimulationType)value; }
    }

    public readonly partial struct PhysicsBody
    {
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsBody.bodyType has been deprecated. Please use PhysicsBody.type instead.", false)]
        public readonly RigidbodyType2D bodyType { get => (RigidbodyType2D)type; set => type = (PhysicsBody.BodyType)value; }

        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsBody.bodyConstraints has been deprecated. Please use PhysicsBody.constraints instead.", false)]
        public readonly RigidbodyConstraints2D bodyConstraints { get => (RigidbodyConstraints2D)constraints; set => constraints = (PhysicsBody.BodyConstraints)value; }
    }

    public partial struct PhysicsBodyDefinition
    {
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsBodyDefinition.bodyType has been deprecated. Please use PhysicsBodyDefinition.type instead.", false)]
        public RigidbodyType2D bodyType { readonly get => (RigidbodyType2D)m_BodyType; set => m_BodyType = (PhysicsBody.BodyType)value; }

        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsBodyDefinition.bodyConstraints has been deprecated. Please use PhysicsBodyDefinition.constraints instead.", false)]
        public RigidbodyConstraints2D bodyConstraints { readonly get => (RigidbodyConstraints2D)m_BodyConstraints; set => m_BodyConstraints = (PhysicsBody.BodyConstraints)value; }
    }

    public readonly partial struct PhysicsShape
    {
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsShape.frictionCombine has been deprecated. Please use PhysicsShape.frictionMixing instead.", false)]
        public readonly PhysicsMaterialCombine2D frictionCombine { get => (PhysicsMaterialCombine2D)frictionMixing; set => frictionMixing = (SurfaceMaterial.MixingMode)value; }

        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsShape.bouncinessCombine has been deprecated. Please use PhysicsShape.bouncinessMixing instead.", false)]
        public readonly PhysicsMaterialCombine2D bouncinessCombine { get => (PhysicsMaterialCombine2D)bouncinessMixing; set => bouncinessMixing = (SurfaceMaterial.MixingMode)value; }

        public partial struct SurfaceMaterial
        {
            /// <undoc/>
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("PhysicsShape.SurfaceMaterial.frictionCombine has been deprecated. Please use PhysicsShape.SurfaceMaterial.frictionMixing instead.", false)]
            public PhysicsMaterialCombine2D frictionCombine { readonly get => (PhysicsMaterialCombine2D)frictionMixing; set => frictionMixing = (MixingMode)value; }

            /// <undoc/>
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("PhysicsShape.SurfaceMaterial.bouncinessCombine has been deprecated. Please use PhysicsShape.SurfaceMaterial.bouncinessMixing instead.", false)]
            public PhysicsMaterialCombine2D bouncinessCombine { readonly get => (PhysicsMaterialCombine2D)bouncinessMixing; set => bouncinessMixing = (MixingMode)value; }
        }
    }

    public readonly partial struct PhysicsChain
    {
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsChain.frictionCombine has been deprecated. Please use PhysicsChain.frictionMixing instead.", false)]
        public readonly PhysicsMaterialCombine2D frictionCombine { get => (PhysicsMaterialCombine2D)frictionMixing; set => frictionMixing = (PhysicsShape.SurfaceMaterial.MixingMode)value; }

        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("PhysicsChain.bouncinessCombine has been deprecated. Please use PhysicsChain.bouncinessMixing instead.", false)]
        public readonly PhysicsMaterialCombine2D bouncinessCombine { get => (PhysicsMaterialCombine2D)bouncinessMixing; set => bouncinessMixing = (PhysicsShape.SurfaceMaterial.MixingMode)value; }
    }
}
