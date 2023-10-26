// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/Physics/Collider.h")]
    public partial class Collider : Component
    {
        extern public bool enabled { get; set; }
        extern public Rigidbody attachedRigidbody { [NativeMethod("GetRigidbody")] get; }
        extern public ArticulationBody attachedArticulationBody { [NativeMethod("GetArticulationBody")] get; }
        extern public bool isTrigger { get; set; }
        extern public float contactOffset { get; set; }
        extern public Vector3 ClosestPoint(Vector3 position);
        extern public Bounds bounds { get; }
        extern public bool hasModifiableContacts { get; set; }
        extern public bool providesContacts { get; set; }
        extern public int layerOverridePriority { get; set; }
        extern public LayerMask excludeLayers { get; set; }
        extern public LayerMask includeLayers { get; set; }
        extern public LowLevelPhysics.GeometryHolder GeometryHolder { get; }

        public T GetGeometry<T>() where T : struct, LowLevelPhysics.IGeometry
        {
            return GeometryHolder.As<T>();
        }

        [NativeMethod("Material")]
        extern public PhysicsMaterial sharedMaterial { get; set; }
        extern public PhysicsMaterial material
        {
            [NativeMethod("GetClonedMaterial")]
            get;
            [NativeMethod("SetMaterial")]
            set;
        }

        extern private RaycastHit Raycast(Ray ray, float maxDistance, ref bool hasHit);

        public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
            bool hasHit = false;
            hitInfo = Raycast(ray, maxDistance, ref hasHit);
            return hasHit;
        }

        [NativeName("ClosestPointOnBounds")]
        extern private void Internal_ClosestPointOnBounds(Vector3 point, ref Vector3 outPos, ref float distance);

        public Vector3 ClosestPointOnBounds(Vector3 position)
        {
            float dist = 0f;
            Vector3 outpos = Vector3.zero;
            Internal_ClosestPointOnBounds(position, ref outpos, ref dist);
            return outpos;
        }
    }
}
