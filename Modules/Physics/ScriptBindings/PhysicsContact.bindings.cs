// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    public partial class Physics
    {
        // Using delegates here instead of Action<T> provides better code completion on user projects (argument names in particular)
        ///<param name="scene">The physics scene that the contacts belong to.</param>
        ///<param name="headerArray">A contact buffer where all the contact data of the previous simulation step is stored.</param>
        public delegate void ContactEventDelegate(PhysicsScene scene, NativeArray<ContactPairHeader>.ReadOnly headerArray);

        ///<summary>Subscribe to this event to read all collisions that occurred during the physics simulation step.</summary>
        ///<remarks>
        ///  <para>Each subscriber to this event gets invoked with a physics scene and a native array of <see cref="ContactPairHeader" />s. Each <see cref="ContactPairHeader" /> contains an array of <see cref="ContactPair" />s and each <see cref="ContactPair" /> contains an array of <see cref="ContactPairPoint" />s.
        ///
        ///You can use this event to speed up contact processing as it's a lot faster than <see cref="M:UnityEngine.MonoBehaviour.OnCollisionEnter(UnityEngine.Collision)" /> and other messages. You can also use this event to schedule jobs that use the provided native array. Jobs that are scheduled from this event must be completed before the next <see cref="Physics.Simulate" />, <see cref="PhysicsScene.Simulate" />, or <see cref="PhysicsScene.RunSimulationStages" /> with the <see cref="SimulationStage.RunSimulation" /> stage call. By default a good place to complete these jobs is <see cref="M:UnityEngine.MonoBehaviour.FixedUpdate" />.
        ///
        ///Notes:
        ///
        ///- Only Collider collisions are reported in this event and no trigger events will appear in the provided buffer.
        ///- All the data in the provided buffer is read-only. No writes are permited.
        ///- The event is invoked after the transform sync.
        ///- To receive contacts from a Collider, set the <see cref="Collider.providesContacts" /> property to <c>true</c> or attach a <see cref="MonoBehaviour" /> script with an OnCollisionStay method.</para>
        ///  <para>This script reads all the contacts in the buffer and computes the average normal for each ContactPairHeader. Then applies a force based on the result.</para>
        ///</remarks>
        ///<example nocheck="true">
        ///  <code><![CDATA[using System.Collections.Generic;
        ///using Unity.Collections;
        ///using Unity.Jobs;
        ///using UnityEngine;
        ///
        ///public class BounceScript : MonoBehaviour
        ///{
        ///    [SerializeField]
        ///    private float m_ImpulseMultiplier = 5f;
        ///
        ///    private struct JobResultStruct
        ///    {
        ///        public EntityId thisEntityId;
        ///        public EntityId otherEntityId;
        ///        public Vector3 averageNormal;
        ///    }
        ///
        ///    private NativeArray<JobResultStruct> m_ResultsArray;
        ///    private int m_Count;
        ///    private JobHandle m_JobHandle;
        ///
        ///    private readonly Dictionary<EntityId, Rigidbody> m_RigidbodyMapping = new Dictionary<EntityId, Rigidbody>();
        ///
        ///    private void OnEnable()
        ///    {
        ///        m_ResultsArray = new NativeArray<JobResultStruct>(16, Allocator.Persistent);
        ///
        ///        Physics.ContactEvent += Physics_ContactEvent;
        ///
        ///        var allRBs = FindObjectsByType<Rigidbody>();
        ///        foreach (var rb in allRBs)
        ///            m_RigidbodyMapping.Add(rb.GetEntityId(), rb);
        ///    }
        ///
        ///    private void OnDisable()
        ///    {
        ///        m_JobHandle.Complete();
        ///        m_ResultsArray.Dispose();
        ///
        ///        Physics.ContactEvent -= Physics_ContactEvent;
        ///
        ///        m_RigidbodyMapping.Clear();
        ///    }
        ///
        ///    private void FixedUpdate()
        ///    {
        ///        m_JobHandle.Complete(); // The buffer is valid until the next Physics.Simulate() call. Be it internal or manual
        ///
        ///        // Do something with the contact data.
        ///        // E.g. Add force based on the average contact normal for that body
        ///        for (int i = 0; i < m_Count; i++)
        ///        {
        ///            var result = m_ResultsArray[i];
        ///
        ///            // TryGetValue skips bodies that the mapping doesn't contain, either because they were
        ///            // created after OnEnable or because that side of the pair has no body (EntityId.None).
        ///            // The bool check then skips any body that has been destroyed since OnEnable.
        ///            if (m_RigidbodyMapping.TryGetValue(result.thisEntityId, out var rb0) && rb0)
        ///                rb0.AddForce(result.averageNormal * m_ImpulseMultiplier, ForceMode.Impulse);
        ///
        ///            if (m_RigidbodyMapping.TryGetValue(result.otherEntityId, out var rb1) && rb1)
        ///                rb1.AddForce(result.averageNormal * -m_ImpulseMultiplier, ForceMode.Impulse);
        ///        }
        ///    }
        ///
        ///    private void Physics_ContactEvent(PhysicsScene scene, NativeArray<ContactPairHeader>.ReadOnly pairHeaders)
        ///    {
        ///        int n = pairHeaders.Length;
        ///
        ///        if (m_ResultsArray.Length < n)
        ///        {
        ///            m_ResultsArray.Dispose();
        ///            m_ResultsArray = new NativeArray<JobResultStruct>(Mathf.NextPowerOfTwo(n), Allocator.Persistent);
        ///        }
        ///
        ///        m_Count = n;
        ///
        ///        AddForceJob job = new AddForceJob()
        ///        {
        ///            pairHeaders = pairHeaders,
        ///            resultsArray = m_ResultsArray
        ///        };
        ///
        ///        m_JobHandle = job.Schedule(n, 256);
        ///    }
        ///
        ///    private struct AddForceJob : IJobParallelFor
        ///    {
        ///        [ReadOnly]
        ///        public NativeArray<ContactPairHeader>.ReadOnly pairHeaders;
        ///
        ///        public NativeArray<JobResultStruct> resultsArray;
        ///
        ///        public void Execute(int index)
        ///        {
        ///            Vector3 averageNormal = Vector3.zero;
        ///            int count = 0;
        ///
        ///            for (int j = 0; j < pairHeaders[index].pairCount; j++)
        ///            {
        ///                ref readonly var pair = ref pairHeaders[index].GetContactPair(j);
        ///
        ///                if (pair.IsCollisionExit)
        ///                    continue;
        ///
        ///                for (int k = 0; k < pair.ContactCount; k++)
        ///                {
        ///                    ref readonly var contact = ref pair.GetContactPoint(k);
        ///                    averageNormal += contact.Normal;
        ///                }
        ///
        ///                count += pair.ContactCount;
        ///            }
        ///
        ///            if (count != 0)
        ///                averageNormal /= (float)count;
        ///
        ///            JobResultStruct result = new JobResultStruct()
        ///            {
        ///                thisEntityId = pairHeaders[index].bodyEntityId,
        ///                otherEntityId = pairHeaders[index].otherBodyEntityId,
        ///                averageNormal = averageNormal
        ///            };
        ///
        ///            resultsArray[index] = result;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [AutoStaticsCleanupOnCodeReload]
        public static event ContactEventDelegate ContactEvent;

        [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.Clear)]
        private static readonly Collision s_ReusableCollision = new Collision();

        static readonly ProfilerMarker s_ContactEventMarker = new ProfilerMarker("Physics.ContactEvent");
        static readonly ProfilerMarker s_InvokeOnCollisionEventsMarker = new ProfilerMarker("Physics.InvokeOnCollisionEvents");

        [RequiredByNativeCode]
        private static unsafe void OnSceneContact(PhysicsScene scene, IntPtr buffer, int count)
        {
            if (count == 0)
                return;

            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ContactPairHeader>(buffer.ToPointer(), count, Allocator.None);

            var safety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);

            try
            {
                using (s_ContactEventMarker.Auto())
                {
                    ContactEvent?.Invoke(scene, array.AsReadOnly());
                }
            }
            catch(Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                ReportContacts(array.AsReadOnly());
            }

            AtomicSafetyHandle.Release(safety);
        }

        private static void ReportContacts(NativeArray<ContactPairHeader>.ReadOnly array)
        {
            if (!Physics.invokeCollisionCallbacks)
                return;

            using (s_InvokeOnCollisionEventsMarker.Auto())
            {
                for (int i = 0; i < array.Length; i++)
                {
                    ContactPairHeader header = array[i];

                    if (header.hasRemovedBody)
                        continue;

                    for (int j = 0; j < header.m_NbPairs; j++)
                    {
                        ref readonly ContactPair pair = ref header.GetContactPair(j);

                        if (pair.hasRemovedCollider)
                            continue;

                        var actor = header.body;
                        var otherActor = header.otherBody;
                        var component = actor != null ? actor : pair.collider;
                        var otherComponent = otherActor != null ? otherActor : pair.otherCollider;

                        if(!component || !otherComponent)
                            continue;

                        if (pair.isCollisionEnter)
                        {
                            Physics.SendOnCollisionEnter(component, GetCollisionToReport(in header, in pair, false));
                            Physics.SendOnCollisionEnter(otherComponent, GetCollisionToReport(in header, in pair, true));
                        }
                        if (pair.isCollisionStay)
                        {
                            Physics.SendOnCollisionStay(component, GetCollisionToReport(in header, in pair, false));
                            Physics.SendOnCollisionStay(otherComponent, GetCollisionToReport(in header, in pair, true));
                        }
                        if (pair.isCollisionExit)
                        {
                            Physics.SendOnCollisionExit(component, GetCollisionToReport(in header, in pair, false));
                            Physics.SendOnCollisionExit(otherComponent, GetCollisionToReport(in header, in pair, true));
                        }
                    }
                }
            }
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
    ///<summary>A header struct which contains colliding bodies.</summary>
    ///<remarks>This struct contains an array of <see cref="ContactPair" />s that can be retrieved with the <see cref="GetContactPair" /> method.</remarks>
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public readonly partial struct ContactPairHeader
    {
        internal readonly EntityId m_BodyID;
        internal readonly EntityId m_OtherBodyID;
        internal readonly IntPtr m_StartPtr;
        internal readonly uint m_NbPairs;
        internal readonly CollisionPairHeaderFlags m_Flags;
        internal readonly Vector3 m_ThisBodyLinearVelocity;
        internal readonly Vector3 m_ThisBodyAngularVelocity;
        internal readonly Vector3 m_OtherBodyLinearVelocity;
        internal readonly Vector3 m_OtherBodyAngularVelocity;

        ///<summary>Instance ID of the first <see cref="Rigidbody" /> or the <see cref="ArticulationBody" /> in the pair.</summary>
        [Obsolete("bodyInstanceID is deprecated, use bodyEntityId instead.", true)]
        public int bodyInstanceID => m_BodyID;
        ///<summary>Instance ID of the second <see cref="Rigidbody" /> or the <see cref="ArticulationBody" /> in the pair.</summary>
        [Obsolete("otherBodyInstanceID is deprecated, use otherBodyEntityId instead.", true)]
        public int otherBodyInstanceID => m_OtherBodyID;

        ///<summary>EntityId of the first <see cref="Rigidbody" /> or the <see cref="ArticulationBody" /> in the pair.</summary>
        public EntityId bodyEntityId => m_BodyID;
        ///<summary>EntityId of the second <see cref="Rigidbody" /> or the <see cref="ArticulationBody" /> in the pair.</summary>
        public EntityId otherBodyEntityId => m_OtherBodyID;

        ///<summary>The first <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> in the pair.</summary>
        ///<remarks>Use with the <c>as</c> keyword to determine the actual type of the component.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example
        ///{
        ///    public Rigidbody ExtractRigidbodyFromContactPairHeader(ContactPairHeader header)
        ///    {
        ///        return header.body as Rigidbody;
        ///    }
        ///
        ///    public ArticulationBody ExtractArticulationBodyFromContactPairHeader(ContactPairHeader header)
        ///    {
        ///        return header.body as ArticulationBody;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Component body => Physics.GetBodyByInstanceID(m_BodyID) as Component;
        ///<summary>The second <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> in the pair.</summary>
        ///<remarks>Use with the <c>as</c> keyword to determine the actual type of the component.</remarks>
        public Component otherBody => Physics.GetBodyByInstanceID(m_OtherBodyID) as Component;

        ///<summary>Linear velocity of the first <see cref="Rigidbody" /> or the <see cref="ArticulationBody" /> in the pair at the moment of collision.</summary>
        ///<remarks>Returns <see cref="Vector3.zero" /> if no <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> is attached to the first shape in the pair.</remarks>
        public Vector3 bodyLinearVelocity => m_ThisBodyLinearVelocity;
        ///<summary>Angular velocity of the first <see cref="Rigidbody" /> or the <see cref="ArticulationBody" /> in the pair at the moment of collision.</summary>
        ///<remarks>Returns <see cref="Vector3.zero" /> if no <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> is attached to the first shape in the pair.</remarks>
        public Vector3 bodyAngularVelocity => m_ThisBodyAngularVelocity;

        ///<summary>Linear velocity of the second <see cref="Rigidbody" /> or the <see cref="ArticulationBody" /> in the pair at the moment of collision.</summary>
        ///<remarks>Returns <see cref="Vector3.zero" /> if no <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> is attached to the second shape in the pair.</remarks>
        public Vector3 otherBodyLinearVelocity => m_OtherBodyLinearVelocity;
        ///<summary>Angular velocity of the second <see cref="Rigidbody" /> or the <see cref="ArticulationBody" /> in the pair at the moment of collision.</summary>
        ///<remarks>Returns <see cref="Vector3.zero" /> if no <see cref="Rigidbody" /> or <see cref="ArticulationBody" /> is attached to the second shape in the pair.</remarks>
        public Vector3 otherBodyAngularVelocity => m_OtherBodyAngularVelocity;

        ///<summary>Number of <see cref="ContactPair" />s that this header contains.</summary>
        public int pairCount => (int)m_NbPairs;

        internal bool hasRemovedBody => (m_Flags & CollisionPairHeaderFlags.RemovedActor) != 0
                                     || (m_Flags & CollisionPairHeaderFlags.RemovedOtherActor) != 0;

        ///<summary>Gets the <see cref="ContactPair" /> at <c>index</c> of this pair header.</summary>
        ///<remarks>Can be used with the /ref readonly/ keyword to avoid copying the whole struct.</remarks>
        ///<param name="index">The <see cref="ContactPair" /> index.</param>
        ///<returns>A reference or a copy of the <see cref="ContactPair" /> struct at <c>index</c>.</returns>
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
    ///<summary>A pair of Colliders that belong to the bodies in the parent <see cref="ContactPairHeader" /> struct.</summary>
    ///<remarks>Contains an array of <see cref="ContactPairPoint" />s that can be retrieved using the <see cref="GetContactPoint" /> method.</remarks>
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe readonly partial struct ContactPair
    {
        private const uint c_InvalidFaceIndex = 0xffffFFFF;

        internal readonly EntityId m_ColliderID;
        internal readonly EntityId m_OtherColliderID;
        internal readonly IntPtr m_StartPtr;
        internal readonly uint m_NbPoints;
        internal readonly CollisionPairFlags m_Flags;
        internal readonly CollisionPairEventFlags m_Events;
        internal readonly Vector3 m_ImpulseSum;

        ///<summary>Instance ID of the first Collider in the <see cref="ContactPair" />.</summary>
        [Obsolete("colliderInstanceID is deprecated, use colliderEntityId instead.", true)]
        public int colliderInstanceID => m_ColliderID;
        ///<summary>Instance ID of the second Collider in the <see cref="ContactPair" />.</summary>
        [Obsolete("otherColliderInstanceID is deprecated, use otherColliderEntityId instead.", true)]
        public int otherColliderInstanceID => m_OtherColliderID;

        ///<summary>EntityId of the first Collider in the <see cref="ContactPair" />.</summary>
        public EntityId colliderEntityId => m_ColliderID;
        ///<summary>EntityId of the second Collider in the <see cref="ContactPair" />.</summary>
        public EntityId otherColliderEntityId => m_OtherColliderID;

        ///<summary>The first <see cref="Collider" /> component of the <see cref="ContactPair" />.</summary>
        ///<remarks>This property is only accessible from the main thread.</remarks>
        public Collider collider => m_ColliderID == EntityId.None ? null : Physics.GetColliderByInstanceID(m_ColliderID) as Collider;
        ///<summary>The second <see cref="Collider" /> component of the <see cref="ContactPair" />.</summary>
        ///<remarks>This property is only accessible from the main thread.</remarks>
        public Collider otherCollider => m_OtherColliderID == EntityId.None ? null : Physics.GetColliderByInstanceID(m_OtherColliderID) as Collider;

        ///<summary>The number of <see cref="ContactPairPoint" />s that this pair contains.</summary>
        public int contactCount => (int)m_NbPoints;

        ///<summary>Total impulse sum of the pair.</summary>
        ///<remarks>Equivalent to <see cref="Collision.impulse" />.</remarks>
        public Vector3 impulseSum => m_ImpulseSum;

        ///<summary>Whether or not this pair is equivalent to a pair reported in <see cref="M:UnityEngine.MonoBehaviour.OnCollisionEnter(UnityEngine.Collision)" /> events.</summary>
        public bool isCollisionEnter => (m_Events & CollisionPairEventFlags.NotifyTouchFound) != 0;
        ///<summary>Whether or not this pair is equivalent to a pair reported in <see cref="M:UnityEngine.MonoBehaviour.OnCollisionExit(UnityEngine.Collision)" /> events.</summary>
        public bool isCollisionExit => (m_Events & CollisionPairEventFlags.NotifyTouchLost) != 0;
        ///<summary>Whether or not this pair is equivalent to a pair reported in <see cref="M:UnityEngine.MonoBehaviour.OnCollisionStay(UnityEngine.Collision)" /> events.</summary>
        public bool isCollisionStay  => (m_Events & CollisionPairEventFlags.NotifyTouchPersists) != 0;

        internal bool hasRemovedCollider => (m_Flags & CollisionPairFlags.RemovedShape) != 0
                                         || (m_Flags & CollisionPairFlags.RemovedOtherShape) != 0;

        // Capacity must be extended beforehand!
        internal int ExtractContacts(List<ContactPoint> managedContainer, bool flipped)
        {
            int size = (int)Math.Min(managedContainer.Capacity, m_NbPoints);
            managedContainer.Clear();

            for (int i = 0; i < size; ++i)
            {
                ref readonly ContactPairPoint nativePoint = ref GetContactPoint(i);
                var contactPoint = new ContactPoint()
                {
                    m_Point = nativePoint.position,
                    m_Impulse = nativePoint.impulse,
                    m_Separation = nativePoint.separation,
                };

                if (flipped)
                {
                    contactPoint.m_Normal = -nativePoint.normal;
                    contactPoint.m_ThisColliderEntityId = m_OtherColliderID;
                    contactPoint.m_OtherColliderEntityId = m_ColliderID;
                }
                else
                {
                    contactPoint.m_Normal = nativePoint.normal;
                    contactPoint.m_ThisColliderEntityId = m_ColliderID;
                    contactPoint.m_OtherColliderEntityId = m_OtherColliderID;
                }

                managedContainer.Add(contactPoint);
            }

            return size;
        }

        internal int ExtractContactsArray(ContactPoint[] managedContainer, bool flipped)
        {
            int size = (int)Math.Min(managedContainer.Length, m_NbPoints);

            for (int i = 0; i < size; ++i)
            {
                ref readonly ContactPairPoint nativePoint = ref GetContactPoint(i);
                var contactPoint = new ContactPoint()
                {
                    m_Point = nativePoint.position,
                    m_Impulse = nativePoint.impulse,
                    m_Separation = nativePoint.separation,
                };

                if (flipped)
                {
                    contactPoint.m_Normal = -nativePoint.normal;
                    contactPoint.m_ThisColliderEntityId = m_OtherColliderID;
                    contactPoint.m_OtherColliderEntityId = m_ColliderID;
                }
                else
                {
                    contactPoint.m_Normal = nativePoint.normal;
                    contactPoint.m_ThisColliderEntityId = m_ColliderID;
                    contactPoint.m_OtherColliderEntityId = m_OtherColliderID;
                }

                managedContainer[i] = contactPoint;
            }
            return size;
        }

        ///<summary>Copies the internal <see cref="ContactPairPoint" /> buffer to the provided <c>buffer</c>.</summary>
        ///<param name="buffer">A native buffer that will be filled with <see cref="ContactPairPoint" /> data.</param>
        public void CopyToNativeArray(NativeArray<ContactPairPoint> buffer)
        {
            int n = Mathf.Min(buffer.Length, contactCount);

            for (int i = 0; i < n; i++)
                buffer[i] = GetContactPoint(i);
        }

        ///<summary>Gets the <see cref="ContactPairPoint" /> at the provided <c>index</c> of this pair.</summary>
        ///<remarks>Can be used with the /ref readonly/ keywords to avoid copying the whole struct on the stack.</remarks>
        ///<param name="index">The <see cref="ContactPairPoint" /> index.</param>
        ///<returns>A reference or a copy of the <see cref="ContactPairPoint" /> struct at <c>index</c>.</returns>
        public unsafe ref readonly ContactPairPoint GetContactPoint(int index)
        {
            return ref *GetContactPoint_Internal(index);
        }

        ///<summary>Get the index of a face that a particular contact point belongs to in this <see cref="ContactPairPoint" />.</summary>
        ///<remarks>Only valid if one of the colliders in this pair is a non-convex MeshCollider. In this case, that collider will always be the second shape of the pair. A value of 0xffffFFFF indicates an error, which could be none of the Colliders were non-convex MeshColliders, or that the contact point index was out of bounds. Use this with <see cref="Mesh.triangles" />.</remarks>
        ///<param name="contactIndex">The <see cref="ContactPairPoint" /> index.</param>
        ///<returns>Index of a face this contact point belongs to.</returns>
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
    ///<summary>A readonly struct describing a contact point between two <see cref="Collider" />s.</summary>
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public readonly partial struct ContactPairPoint
    {
        internal readonly Vector3 m_Position;
        internal readonly float m_Separation;
        internal readonly Vector3 m_Normal;
        internal readonly uint m_InternalFaceIndex0;
        internal readonly Vector3 m_Impulse;
        internal readonly uint m_InternalFaceIndex1;

        ///<summary>The position of the contact point between the Colliders, in world space.</summary>
        public Vector3 position => m_Position;
        ///<summary>The distance between the edges of Colliders at the contact point.</summary>
        public float separation => m_Separation;
        ///<summary>Normal of the contact point.</summary>
        ///<remarks>The normal direction points from the second Collider to the first Collider.</remarks>
        public Vector3 normal => m_Normal;
        ///<summary>The impulse applied to this contact pair to resolve the collision.</summary>
        ///<remarks>To work out the force applied you can divide the impulse by the last frame's simulation time step.</remarks>
        public Vector3 impulse => m_Impulse;
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
