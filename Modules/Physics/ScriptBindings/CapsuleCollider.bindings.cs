// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Physics/CapsuleCollider.h")]
    public class CapsuleCollider : Collider
    {
        extern public Vector3 center { get; set; }
        extern public float radius { get; set; }
        extern public float height { get; set; }
        extern public int direction { get; set; }
    }
}
