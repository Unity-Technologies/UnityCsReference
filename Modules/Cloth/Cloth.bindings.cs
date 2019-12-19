// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Collections.Generic;
using UnityEngine.Internal;
using System.Runtime.InteropServices;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

namespace UnityEngine
{
    [NativeHeader("Modules/Cloth/Cloth.h")]
    [UsedByNativeCode]
    public struct ClothSphereColliderPair
    {
        public SphereCollider first { get; set; }
        public SphereCollider second { get; set; }

        public ClothSphereColliderPair(SphereCollider a)
        {
            // initialize internal fields so that compiler does not complain about using properties before "this" is ready
            first = a;
            second = null;
        }

        public ClothSphereColliderPair(SphereCollider a, SphereCollider b)
        {
            // initialize internal fields so that compiler does not complain about using properties before "this" is ready
            first = a;
            second = b;
        }
    }

    // The ClothSkinningCoefficient struct is used to set up how a [[Cloth]] component is allowed to move with respect to the [[SkinnedMeshRenderer]] it is attached to.
    [UsedByNativeCode]
    public struct ClothSkinningCoefficient
    {
        //Distance a vertex is allowed to travel from the skinned mesh vertex position.
        public float maxDistance;

        //Definition of a sphere a vertex is not allowed to enter. This allows collision against the animated cloth.
        public float collisionSphereDistance;
    }

    [RequireComponent(typeof(Transform), typeof(SkinnedMeshRenderer))]
    [NativeHeader("Modules/Cloth/Cloth.h")]
    [NativeClass("Unity::Cloth")]
    public sealed partial class Cloth : UnityEngine.Component
    {
        extern public Vector3[] vertices {[NativeName("GetPositions")] get; }
        extern public Vector3[] normals {[NativeName("GetNormals")] get; }
        extern public ClothSkinningCoefficient[] coefficients {[NativeName("GetCoefficients")] get; [NativeName("SetCoefficients")] set; }
        extern public CapsuleCollider[] capsuleColliders {[NativeName("GetCapsuleColliders")] get; [NativeName("SetCapsuleColliders")] set; }
        extern public ClothSphereColliderPair[] sphereColliders {[NativeName("GetSphereColliders")] get; [NativeName("SetSphereColliders")] set; }
        extern public float sleepThreshold { get; set; }

        // Bending stiffness of the cloth.
        extern public float bendingStiffness { get; set; }

        // Stretching stiffness of the cloth.
        extern public float stretchingStiffness { get; set; }

        // Damp cloth motion.
        extern public float damping { get; set; }

        // A constant, external acceleration applied to the cloth.
        extern public Vector3 externalAcceleration { get; set; }

        // A random, external acceleration applied to the cloth.
        extern public Vector3 randomAcceleration { get; set; }

        // Should gravity affect the cloth simulation?
        extern public bool useGravity { get; set; }

        // Is this cloth enabled?
        extern public bool enabled { get; set; }

        // The friction of the cloth when colliding with the character.
        extern public float friction { get; set; }

        // How much to increase mass of colliding particles
        extern public float collisionMassScale { get; set; }

        // Enable continuous collision to improve collision stability
        extern public bool enableContinuousCollision { get; set; }

        // Add 1 virtual particle per triangle to improve collision stability
        extern public float useVirtualParticles { get; set; }

        // How much world-space movement of the character will affect cloth vertices.
        extern public float worldVelocityScale { get; set; }

        // How much world-space acceleration of the character will affect cloth vertices.
        extern public float worldAccelerationScale { get; set; }

        extern public float clothSolverFrequency { get; set; }

        [Obsolete("Parameter solverFrequency is obsolete and no longer supported. Please use clothSolverFrequency instead.")]
        public bool solverFrequency
        {
            get { return clothSolverFrequency > 0.0f; }
            set { clothSolverFrequency = value == true ? 120f : 0.0f; }  // use the default value
        }

        extern public bool useTethers { get; set; }

        extern public float stiffnessFrequency { get; set; }

        extern public float selfCollisionDistance { get; set; }

        extern public float selfCollisionStiffness { get; set; }

        extern public void ClearTransformMotion();

        extern public void GetSelfAndInterCollisionIndices([NotNull] List<UInt32> indices);

        extern public void SetSelfAndInterCollisionIndices([NotNull] List<UInt32> indices);

        extern public void GetVirtualParticleIndices([NotNull] List<UInt32> indicesOutList);

        extern public void SetVirtualParticleIndices([NotNull] List<UInt32> indicesIn);

        extern public void GetVirtualParticleWeights([NotNull] List<Vector3> weightsOutList);

        extern public void SetVirtualParticleWeights([NotNull] List<Vector3> weights);

        [Obsolete("useContinuousCollision is no longer supported, use enableContinuousCollision instead")]
        public float useContinuousCollision { get; set; }

        [Obsolete("Deprecated.Cloth.selfCollisions is no longer supported since Unity 5.0.", true)]
        public bool selfCollision { get; }

        extern public void SetEnabledFading(bool enabled, float interpolationTime);

        [ExcludeFromDocs]
        public void SetEnabledFading(bool enabled)
        {
            SetEnabledFading(enabled, 0.5f);
        }

        extern private RaycastHit Raycast(Ray ray, float maxDistance, ref bool hasHit);

        internal bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
            bool hasHit = false;
            hitInfo = Raycast(ray, maxDistance, ref hasHit);
            return hasHit;
        }
    }
}
