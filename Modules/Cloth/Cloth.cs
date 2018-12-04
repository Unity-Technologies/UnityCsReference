// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public sealed partial class Cloth
    {
        public void GetVirtualParticleIndices(List<UInt32> indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");

            Internal_GetVirtualParticleIndices(indices);
        }

        public void SetVirtualParticleIndices(List<UInt32> indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");

            Internal_SetVirtualParticleIndices(indices);
        }

        public void GetVirtualParticleWeights(List<Vector3> weights)
        {
            if (weights == null)
                throw new ArgumentNullException("weights");

            Internal_GetVirtualParticleWeights(weights);
        }

        public void SetVirtualParticleWeights(List<Vector3> weights)
        {
            if (weights == null)
                throw new ArgumentNullException("weights");

            Internal_SetVirtualParticleWeights(weights);
        }

        public void GetSelfAndInterCollisionIndices(List<UInt32> indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");

            Internal_GetSelfAndInterCollisionIndices(indices);
        }

        public void SetSelfAndInterCollisionIndices(List<UInt32> indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");

            Internal_SetSelfAndInterCollisionIndices(indices);
        }
    }
} // namespace

