// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;


namespace UnityEngine
{
    [NativeHeader("Modules/TerrainPhysics/TerrainCollider.h")]
    [NativeHeader("Modules/Terrain/Public/TerrainData.h")]
    public class TerrainCollider : Collider
    {
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

