// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Physics/BoxCollider.h")]
    public partial class BoxCollider : Collider
    {
        extern public Vector3 center { get; set; }
        extern public Vector3 size { get; set; }
    }
}
