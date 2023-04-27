// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.LowLevelPhysics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ImmediateTransform
    {
        private Quaternion m_Rotation;
        private Vector3 m_Position;

        public Quaternion Rotation { get { return m_Rotation; } set { m_Rotation = value; } }
        public Vector3 Position { get { return m_Position; } set { m_Position = value; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImmediateContact
    {
        private Vector3 m_Normal;
        private float m_Separation;
        private Vector3 m_Point;

        private float m_MaxImpulse;
        private Vector3 m_TargetVel;
        private float m_StaticFriction;
        private byte m_MaterialFlags;
        private byte m_Pad;
        private ushort m_InternalUse;
        private uint m_InternalFaceIndex1;
        private float m_DynamicFriction;
        private float m_Restitution;

        public Vector3 Normal { get { return m_Normal; } set { m_Normal = value; } }
        public float Separation { get { return m_Separation; } set { m_Separation = value; } }
        public Vector3 Point { get { return m_Point; } set { m_Point = value; } }
    }

    [NativeHeader("Modules/Physics/ImmediatePhysics.h")]
    public static class ImmediatePhysics
    {
        [FreeFunction("Physics::Immediate::GenerateContacts", isThreadSafe: true)]
        private static unsafe extern int GenerateContacts_Native(void* geom1, void* geom2, void* xform1, void* xform2, int numPairs, void* contacts, int contactArrayLength, void* sizes, int sizesArrayLength, float contactDistance);

        public unsafe static int GenerateContacts(NativeArray<GeometryHolder>.ReadOnly geom1, NativeArray<GeometryHolder>.ReadOnly geom2,
            NativeArray<ImmediateTransform>.ReadOnly xform1, NativeArray<ImmediateTransform>.ReadOnly xform2, int pairCount,
            NativeArray<ImmediateContact> outContacts, NativeArray<int> outContactCounts, float contactDistance = 0.01f)
        {
            if (geom1.Length < pairCount ||
                geom2.Length < pairCount ||
                xform1.Length < pairCount ||
                xform2.Length < pairCount)
                throw new ArgumentException("Provided geometry or transform arrays are not large enough to fit the count of pairs.");

            if (pairCount > outContactCounts.Length)
                throw new ArgumentException("The output contact counts array is not big enough. The size of the array needs to match or exceed the amount of pairs.");

            if (contactDistance <= 0)
                throw new ArgumentException("Contact distance must be positive and not equal to zero.");

            return GenerateContacts_Native(
                geom1.GetUnsafeReadOnlyPtr(),
                geom2.GetUnsafeReadOnlyPtr(),
                xform1.GetUnsafeReadOnlyPtr(),
                xform2.GetUnsafeReadOnlyPtr(),
                pairCount,
                outContacts.GetUnsafePtr(),
                outContacts.Length,
                outContactCounts.GetUnsafePtr(),
                outContactCounts.Length,
                contactDistance);
        }
    }
}
