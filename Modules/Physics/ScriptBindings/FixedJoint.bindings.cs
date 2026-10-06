// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>The Fixed joint groups together 2 rigidbodies, making them stick together in their bound position.</summary>
    ///<seealso cref="CharacterJoint" />
    ///<seealso cref="HingeJoint" />
    ///<seealso cref="SpringJoint" />
    ///<seealso cref="ConfigurableJoint" />
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/FixedJoint.h")]
    [NativeClass("Unity::FixedJoint", PersistentTypeId = 138)]
    public class FixedJoint : Joint
    {
        internal FixedJoint(global::UnityEngine.EntityId id) : base(id) {}
        public FixedJoint() {}
    }
}
