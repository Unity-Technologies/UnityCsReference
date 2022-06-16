// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace UnityEngine
{
    public partial class Physics
    {
        // Using delegates here instead of Action<T> provides better code completion on user projects (argument names in particular)
        public delegate void ContactEventDelegate(PhysicsScene scene, NativeArray<ContactPairHeader>.ReadOnly headerArray);

        public static event ContactEventDelegate ContactEvent;

        private static readonly Collision s_ReusableCollision = new Collision();

        [RequiredByNativeCode]
        private static unsafe void OnSceneContact(PhysicsScene scene, IntPtr buffer, int count)
        {
            if (count == 0)
                return;

            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ContactPairHeader>(buffer.ToPointer(), count, Allocator.None);

            var safety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);

            Profiling.Profiler.BeginSample("Physics.ContactEvent");

            try
            {
                ContactEvent?.Invoke(scene, array.AsReadOnly());
            }
            catch(Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                Profiling.Profiler.EndSample();
                ReportContacts(array.AsReadOnly());
            }

            AtomicSafetyHandle.Release(safety);
        }

        private static void ReportContacts(NativeArray<ContactPairHeader>.ReadOnly array)
        {
            if (!Physics.invokeCollisionCallbacks)
                return;

            Profiling.Profiler.BeginSample("Physics.InvokeOnCollisionEvents");

            for (int i = 0; i < array.Length; i++)
            {
                ContactPairHeader header = array[i];

                if (header.HasRemovedBody)
                    continue;

                for (int j = 0; j < header.m_NbPairs; j++)
                {
                    ref readonly ContactPair pair = ref header.GetContactPair(j);

                    if (pair.HasRemovedCollider)
                        continue;

                    var actor = header.Body;
                    var otherActor = header.OtherBody;
                    var component = actor != null ? actor : pair.Collider;
                    var otherComponent = otherActor != null ? otherActor : pair.OtherCollider;

                    if (pair.IsCollisionEnter)
                    {
                        Physics.SendOnCollisionEnter(component, GetCollisionToReport(in header, in pair, false));
                        Physics.SendOnCollisionEnter(otherComponent, GetCollisionToReport(in header, in pair, true));
                    }
                    if (pair.IsCollisionStay)
                    {
                        Physics.SendOnCollisionStay(component, GetCollisionToReport(in header, in pair, false));
                        Physics.SendOnCollisionStay(otherComponent, GetCollisionToReport(in header, in pair, true));
                    }
                    if (pair.IsCollisionExit)
                    {
                        Physics.SendOnCollisionExit(component, GetCollisionToReport(in header, in pair, false));
                        Physics.SendOnCollisionExit(otherComponent, GetCollisionToReport(in header, in pair, true));
                    }
                }
            }

            Profiling.Profiler.EndSample();
        }

        private static Collision GetCollisionToReport(in ContactPairHeader header, in ContactPair pair, bool flipped)
        {
            if(reuseCollisionCallbacks)
            {
                // This is required to support mid-callback reuseCollisionCallbacks changes
                s_ReusableCollision.Reuse(in header, in pair);
                s_ReusableCollision.Flipped = flipped;
                return s_ReusableCollision;
            }
            else
            {
                return new Collision(in header, in pair, flipped);
            }
        }
    }

    // See MessageParameters.h
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ContactPairHeader
    {
        internal readonly int m_BodyID;
        internal readonly int m_OtherBodyID;
        internal readonly IntPtr m_StartPtr;
        internal readonly uint m_NbPairs;
        internal readonly CollisionPairHeaderFlags m_Flags;
        internal readonly Vector3 m_RelativeVelocity;

        public int BodyInstanceID => m_BodyID;
        public int OtherBodyInstanceID => m_OtherBodyID;

        public Component Body => Physics.GetBodyByInstanceID(m_BodyID) as Component;
        public Component OtherBody => Physics.GetBodyByInstanceID(m_OtherBodyID) as Component;

        public int PairCount => (int)m_NbPairs;

        internal bool HasRemovedBody => (m_Flags & CollisionPairHeaderFlags.RemovedActor) != 0
                                     || (m_Flags & CollisionPairHeaderFlags.RemovedOtherActor) != 0;

        public unsafe ref readonly ContactPair GetContactPair(int index)
        {
            return ref *GetContactPair_Internal(index);
        }

        internal unsafe ContactPair* GetContactPair_Internal(int index)
        {
            if (index >= m_NbPairs)
                throw new IndexOutOfRangeException("Invalid ContactPair index. Index should be greater than 0 and less than ContactPairHeader.PairCount");

            return (ContactPair*)(m_StartPtr.ToInt64() + index * sizeof(ContactPair));
        }
    }

    // See MessageParameters.h
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe readonly struct ContactPair
    {
        private const uint c_InvalidFaceIndex = 0xffffFFFF;

        internal readonly int m_ColliderID;
        internal readonly int m_OtherColliderID;
        internal readonly IntPtr m_StartPtr;
        internal readonly uint m_NbPoints;
        internal readonly CollisionPairFlags m_Flags;
        internal readonly CollisionPairEventFlags m_Events;
        internal readonly Vector3 m_ImpulseSum;

        public int ColliderInstanceID => m_ColliderID;
        public int OtherColliderInstanceID => m_OtherColliderID;

        public Collider Collider => m_ColliderID == 0 ? null : Physics.GetColliderByInstanceID(m_ColliderID) as Collider;
        public Collider OtherCollider => m_OtherColliderID == 0 ? null : Physics.GetColliderByInstanceID(m_OtherColliderID) as Collider;

        public int ContactCount => (int)m_NbPoints;

        public Vector3 ImpulseSum => m_ImpulseSum;

        public bool IsCollisionEnter => (m_Events & CollisionPairEventFlags.NotifyTouchFound) != 0;
        public bool IsCollisionExit  => (m_Events & CollisionPairEventFlags.NotifyTouchLost) != 0;
        public bool IsCollisionStay  => (m_Events & CollisionPairEventFlags.NotifyTouchPersists) != 0;

        internal bool HasRemovedCollider => (m_Flags & CollisionPairFlags.RemovedShape) != 0
                                         || (m_Flags & CollisionPairFlags.RemovedOtherShape) != 0;

        // Capacity must be extended beforehand!
        extern internal int ExtractContacts(List<ContactPoint> managedContainer, bool flipped);
        extern internal int ExtractContactsArray(ContactPoint[] managedContainer, bool flipped);

        public void CopyToNativeArray(NativeArray<ContactPairPoint> buffer)
        {
            int n = Mathf.Min(buffer.Length, ContactCount);

            for (int i = 0; i < n; i++)
                buffer[i] = GetContactPoint(i);
        }

        public unsafe ref readonly ContactPairPoint GetContactPoint(int index)
        {
            return ref *GetContactPoint_Internal(index);
        }

        public unsafe uint GetContactPointFaceIndex(int contactIndex)
        {
            var index0 = GetContactPoint_Internal(contactIndex)->m_InternalFaceIndex0;
            var index1 = GetContactPoint_Internal(contactIndex)->m_InternalFaceIndex1;

            // Only one index may be valid
            if (index0 != c_InvalidFaceIndex)
                return Physics.TranslateTriangleIndexFromID(m_ColliderID, index0);

            if (index1 != c_InvalidFaceIndex)
                return Physics.TranslateTriangleIndexFromID(m_OtherColliderID, index1);

            return c_InvalidFaceIndex;
        }

        internal unsafe ContactPairPoint* GetContactPoint_Internal(int index)
        {
            if (index >= m_NbPoints)
                throw new IndexOutOfRangeException("Invalid ContactPairPoint index. Index should be greater than 0 and less than ContactPair.ContactCount");

            return (ContactPairPoint*)(m_StartPtr.ToInt64() + index * sizeof(ContactPairPoint));
        }
    }

    // See https://github.com/NVIDIAGameWorks/PhysX/blob/4.1/physx/include/PxSimulationEventCallback.h#L463
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ContactPairPoint
    {
        internal readonly Vector3 m_Position;
        internal readonly float m_Separation;
        internal readonly Vector3 m_Normal;
        internal readonly uint m_InternalFaceIndex0;
        internal readonly Vector3 m_Impulse;
        internal readonly uint m_InternalFaceIndex1;

        public Vector3 Position => m_Position;
        public float Separation => m_Separation;
        public Vector3 Normal => m_Normal;
        public Vector3 Impulse => m_Impulse;
    };

    internal enum CollisionPairHeaderFlags : ushort // Size is important!
    {
        RemovedActor                    = (1 << 0),
        RemovedOtherActor               = (1 << 1)
    };

    internal enum CollisionPairFlags : ushort // Size is important!
    {
        RemovedShape                    = (1 << 0),
        RemovedOtherShape               = (1 << 1),
        ActorPairHasFirstTouch          = (1 << 2),
        ActorPairLostTouch              = (1 << 3),
        InternalHasImpulses             = (1 << 4),
        InternalContactsAreFlipped      = (1 << 5)
    };

    internal enum CollisionPairEventFlags : ushort // Size is important!
    {
        SolveContacts                   = (1 << 0),
        ModifyContacts                  = (1 << 1),
        NotifyTouchFound                = (1 << 2),
        NotifyTouchPersists             = (1 << 3),
        NotifyTouchLost                 = (1 << 4),
        NotifyTouchCCD                  = (1 << 5),
        NotifyThresholdForceFound       = (1 << 6),
        NotifyThresholdForcePersists    = (1 << 7),
        NotifyThresholdForceLost        = (1 << 8),
        NotifyContactPoint              = (1 << 9),
        DetectDiscreteContact           = (1 << 10),
        DetectCCDContact                = (1 << 11),
        PreSolverVelocity               = (1 << 12),
        PostSolverVelocity              = (1 << 13),
        ContactEventPose                = (1 << 14),
        NextFree                        = (1 << 15),
        ContactDefault = SolveContacts | DetectDiscreteContact,
        TriggerDefault = NotifyTouchFound | NotifyTouchLost | DetectDiscreteContact
    };
}

