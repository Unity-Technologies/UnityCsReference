// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/ConstantForce.h")]
    public class ConstantForce : Behaviour
    {
        extern public Vector3 force { get; set; }
        extern public Vector3 torque { get; set; }
        extern public Vector3 relativeForce { get; set; }
        extern public Vector3 relativeTorque { get; set; }
    }
}
