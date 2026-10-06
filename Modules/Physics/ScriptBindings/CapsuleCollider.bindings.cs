// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>A capsule-shaped primitive collider.</summary>
    ///<remarks>Capsules are cylinders with a half-sphere at each end.</remarks>
    ///<seealso cref="BoxCollider" />
    ///<seealso cref="SphereCollider" />
    ///<seealso cref="PhysicsMaterial" />
    ///<seealso cref="Rigidbody" />
    [RequireComponent(typeof(Transform))]
    [global::UnityEngine.NativeClass("CapsuleCollider", PersistentTypeId = 136)]
    [NativeHeader("Modules/Physics/CapsuleCollider.h")]
    public class CapsuleCollider : Collider
    {
        internal CapsuleCollider(global::UnityEngine.EntityId id) : base(id) {}
        public CapsuleCollider() {}
        ///<summary>The center of the capsule, measured in the object's local space.</summary>
        extern public Vector3 center { get; set; }
        ///<summary>The radius of the sphere, measured in the object's local space.</summary>
        ///<remarks>The capsule's radius will be scaled by the transform's scale.</remarks>
        extern public float radius { get; set; }
        ///<summary>The height of the capsule measured in the object's local space.</summary>
        ///<remarks>The capsule's height will be scaled by the transform's scale.
        ///**Note:** The height is the actual height including the half-spheres at each end.</remarks>
        extern public float height { get; set; }
        ///<summary>The direction of the capsule.</summary>
        ///<remarks>The value can be 0, 1 or 2 corresponding to the X, Y and Z axes, respectively.</remarks>
        extern public int direction { get; set; }
    }
}
