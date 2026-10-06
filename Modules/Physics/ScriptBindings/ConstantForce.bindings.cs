// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>A force applied constantly.</summary>
    ///<remarks>This is a small physics utility class used to apply a continous force to an object.
    ///
    ///<see cref="Rigidbody.AddForce" /> applies a force to the <see cref="Rigidbody" /> only for one frame, thus you have to keep calling the function.
    ///ConstantForce on the other hand will apply the force every frame until you change the force or torque to a new value.</remarks>
    ///<seealso cref="Rigidbody" />
    [RequireComponent(typeof(Rigidbody))]
    [global::UnityEngine.NativeClass("ConstantForce", PersistentTypeId = 75)]
    [NativeHeader("Modules/Physics/ConstantForce.h")]
    public class ConstantForce : Behaviour
    {
        internal ConstantForce(global::UnityEngine.EntityId id) : base(id) {}
        public ConstantForce() {}
        ///<summary>The force applied to the rigidbody every frame.</summary>
        extern public Vector3 force { get; set; }
        ///<summary>The torque applied to the rigidbody every frame.</summary>
        extern public Vector3 torque { get; set; }
        ///<summary>The force - relative to the rigid bodies coordinate system - applied every frame.</summary>
        extern public Vector3 relativeForce { get; set; }
        ///<summary>The torque - relative to the rigid bodies coordinate system - applied every frame.</summary>
        extern public Vector3 relativeTorque { get; set; }
    }
}
