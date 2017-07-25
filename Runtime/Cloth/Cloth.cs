// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // The ClothSkinningCoefficient struct is used to set up how a [[Cloth]] component is allowed to move with respect to the [[SkinnedMeshRenderer]] it is attached to.
    [UsedByNativeCode]
    public struct ClothSkinningCoefficient
    {
        //Distance a vertex is allowed to travel from the skinned mesh vertex position.
        public float maxDistance;

        //Definition of a sphere a vertex is not allowed to enter. This allows collision against the animated cloth.
        public float collisionSphereDistance;
    }

    public partial class Cloth
    {
        public void GetVirtualParticleIndices(List<UInt32> indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");

            GetVirtualParticleIndicesMono(indices);
        }

        public void SetVirtualParticleIndices(List<UInt32> indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");

            SetVirtualParticleIndicesMono(indices);
        }

        public void GetVirtualParticleWeights(List<Vector3> weights)
        {
            if (weights == null)
                throw new ArgumentNullException("weights");

            GetVirtualParticleWeightsMono(weights);
        }

        public void SetVirtualParticleWeights(List<Vector3> weights)
        {
            if (weights == null)
                throw new ArgumentNullException("weights");

            SetVirtualParticleWeightsMono(weights);
        }

        public void GetSelfAndInterCollisionIndices(List<UInt32> indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");

            GetSelfAndInterCollisionIndicesMono(indices);
        }

        public void SetSelfAndInterCollisionIndices(List<UInt32> indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");

            SetSelfAndInterCollisionIndicesMono(indices);
        }
    }
} // namespace

