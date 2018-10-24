// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    internal class PaintTreesDetailsContext
    {
        private static Terrain[] s_Nbrs = new Terrain[8];
        private static Vector2[] s_Uvs = new Vector2[8];

        public Terrain[] terrains = new Terrain[4];
        public Vector2[] uvs = new Vector2[4];

        private PaintTreesDetailsContext() {}

        public static PaintTreesDetailsContext Create(Terrain terrain, Vector2 uv)
        {
            s_Nbrs[0] = terrain.leftNeighbor;
            s_Nbrs[1] = terrain.leftNeighbor ? terrain.leftNeighbor.topNeighbor : (terrain.topNeighbor ? terrain.topNeighbor.leftNeighbor : null);
            s_Nbrs[2] = terrain.topNeighbor;
            s_Nbrs[3] = terrain.rightNeighbor ? terrain.rightNeighbor.topNeighbor : (terrain.topNeighbor ? terrain.topNeighbor.rightNeighbor : null);
            s_Nbrs[4] = terrain.rightNeighbor;
            s_Nbrs[5] = terrain.rightNeighbor ? terrain.rightNeighbor.bottomNeighbor : (terrain.bottomNeighbor ? terrain.bottomNeighbor.rightNeighbor : null);
            s_Nbrs[6] = terrain.bottomNeighbor;
            s_Nbrs[7] = terrain.leftNeighbor ? terrain.leftNeighbor.bottomNeighbor : (terrain.bottomNeighbor ? terrain.bottomNeighbor.leftNeighbor : null);

            s_Uvs[0] = new Vector2(uv.x + 1.0f, uv.y);
            s_Uvs[1] = new Vector2(uv.x + 1.0f, uv.y - 1.0f);
            s_Uvs[2] = new Vector2(uv.x, uv.y - 1.0f);
            s_Uvs[3] = new Vector2(uv.x - 1.0f, uv.y - 1.0f);
            s_Uvs[4] = new Vector2(uv.x - 1.0f, uv.y);
            s_Uvs[5] = new Vector2(uv.x - 1.0f, uv.y + 1.0f);
            s_Uvs[6] = new Vector2(uv.x, uv.y + 1.0f);
            s_Uvs[7] = new Vector2(uv.x + 1.0f, uv.y + 1.0f);

            PaintTreesDetailsContext ctx = new PaintTreesDetailsContext();
            ctx.terrains[0] = terrain;
            ctx.uvs[0] = uv;

            bool left = uv.x < 0.5f;
            bool right = !left;
            bool bottom = uv.y < 0.5f;
            bool top = !bottom;

            int t = 0;
            if (right && top)
                t = 2;
            else if (right && bottom)
                t = 4;
            else if (left && bottom)
                t = 6;

            for (int i = 1; i < 4; ++i, t = (t + 1) % 8)
            {
                ctx.terrains[i] = s_Nbrs[t];
                ctx.uvs[i] = s_Uvs[t];
            }

            return ctx;
        }
    }
}
