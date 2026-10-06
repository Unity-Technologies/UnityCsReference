// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>A sphere-shaped primitive collider.</summary>
    ///<seealso cref="BoxCollider" />
    ///<seealso cref="CapsuleCollider" />
    ///<seealso cref="PhysicsMaterial" />
    ///<seealso cref="Rigidbody" />
    [RequireComponent(typeof(Transform))]
    [global::UnityEngine.NativeClass("SphereCollider", PersistentTypeId = 135)]
    [NativeHeader("Modules/Physics/SphereCollider.h")]
    public class SphereCollider : Collider
    {
        internal SphereCollider(global::UnityEngine.EntityId id) : base(id) {}
        public SphereCollider() {}
        ///<summary>The center of the sphere in the object's local space.</summary>
        extern public Vector3 center { get; set; }
        ///<summary>The radius of the sphere measured in the object's local space.</summary>
        ///<remarks>The sphere radius will be scaled by the transform's scale.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// //Always attach a SphereCollider component to your GameObject
        ///public class Example : MonoBehaviour
        ///{
        ///    //This declares your SphereCollider
        ///    SphereCollider myCollider;
        ///
        ///    void Start()
        ///    {
        ///        //Assigns the attached SphereCollider to myCollider
        ///        myCollider = GetComponent<SphereCollider>();
        ///    }
        ///
        ///    void OnTriggerEnter(Collider other)
        ///    {
        ///        //This increases the Collider radius when the GameObject collides with a trigger Collider
        ///        myCollider.radius += 2f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float radius { get; set; }
    }
}
