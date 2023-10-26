// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/SpringJoint.h")]
    [NativeClass("Unity::SpringJoint")]
    public class SpringJoint : Joint
    {
        extern public float spring { get; set; }
        extern public float damper { get; set; }
        extern public float minDistance { get; set; }
        extern public float maxDistance { get; set; }
        extern public float tolerance { get; set; }
    }
}
