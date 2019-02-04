// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

namespace UnityEngine.Experimental.TerrainAPI
{
    public static class TerrainCallbacks
    {
        public delegate void HeightmapChangedCallback(Terrain terrain, RectInt heightRegion, bool synched);
        public delegate void TextureChangedCallback(Terrain terrain, string textureName, RectInt texelRegion, bool synched);

        public static event HeightmapChangedCallback heightmapChanged;
        public static event TextureChangedCallback textureChanged;

        [RequiredByNativeCode]
        internal static void InvokeHeightmapChangedCallback(TerrainData terrainData, RectInt heightRegion, bool synched)
        {
            if (heightmapChanged != null)
            {
                foreach (var user in terrainData.users)
                    heightmapChanged.Invoke(user, heightRegion, synched);
            }
        }

        [RequiredByNativeCode]
        internal static void InvokeTextureChangedCallback(TerrainData terrainData, string textureName, RectInt texelRegion, bool synched)
        {
            if (textureChanged != null)
            {
                foreach (var user in terrainData.users)
                    textureChanged.Invoke(user, textureName, texelRegion, synched);
            }
        }
    }
}
