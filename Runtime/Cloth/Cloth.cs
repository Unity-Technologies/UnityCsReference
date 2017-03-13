// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using System.Collections;
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
} // namespace

