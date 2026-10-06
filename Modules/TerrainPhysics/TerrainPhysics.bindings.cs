// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>A heightmap based collider.</summary>
    ///<seealso cref="SphereCollider" />
    ///<seealso cref="CapsuleCollider" />
    ///<seealso cref="PhysicsMaterial" />
    ///<seealso cref="Rigidbody" />
    [NativeHeader("Modules/TerrainPhysics/TerrainCollider.h")]
    [global::UnityEngine.NativeClass("TerrainCollider", PersistentTypeId = 154)]
    [NativeHeader("Modules/Terrain/Public/TerrainData.h")]
    public class TerrainCollider : Collider
    {
        internal TerrainCollider(global::UnityEngine.EntityId id) : base(id) {}
        public TerrainCollider() {}
        ///<summary>The terrain that stores the heightmap.</summary>
        public extern TerrainData terrainData { get; set; }

        extern private RaycastHit Raycast(Ray ray, float maxDistance, bool hitHoles, ref bool hasHit);

        internal bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, bool hitHoles)
        {
            bool hasHit = false;
            hitInfo = Raycast(ray, maxDistance, hitHoles, ref hasHit);
            return hasHit;
        }
    }
}
