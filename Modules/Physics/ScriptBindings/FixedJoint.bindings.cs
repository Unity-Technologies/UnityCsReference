// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/FixedJoint.h")]
    [NativeClass("Unity::FixedJoint")]
    public class FixedJoint : Joint {}
}
