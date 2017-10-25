// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;


namespace UnityEngine
{
    [NativeHeader("Runtime/TerrainPhysics/TerrainCollider.h")]
    [NativeHeader("Modules/Terrain/Public/TerrainData.h")]
    public class TerrainCollider : Collider
    {
        public extern TerrainData terrainData { get; set; }
    }
}

