// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    internal class ColliderUtil
    {
        static Vector3 GetCapsuleExtents(CapsuleCollider cc)
        {
            return cc.GetGlobalExtents();
        }

        static Matrix4x4 CalculateCapsuleTransform(CapsuleCollider cc)
        {
            return cc.CalculateTransform();
        }
    }
}
