// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace TreeEditor
{
    public class TreeTriangle
    {
        public TreeTriangle(int material, int v0, int v1, int v2)
        {
            materialIndex = material;
            v[0] = v0;
            v[1] = v1;
            v[2] = v2;
        }

        public TreeTriangle(int material, int v0, int v1, int v2, bool isBillboard)
        {
            this.isBillboard = isBillboard;
            materialIndex = material;
            v[0] = v0;
            v[1] = v1;
            v[2] = v2;
        }

        public TreeTriangle(int material, int v0, int v1, int v2, bool isBillboard, bool tileV, bool isCutout)
        {
            this.tileV = tileV;
            this.isBillboard = isBillboard;
            this.isCutout = isCutout;
            materialIndex = material;
            v[0] = v0;
            v[1] = v1;
            v[2] = v2;
        }

        public void flip()
        {
            int t = v[0];
            v[0] = v[1];
            v[1] = t;
        }

        public bool tileV = false;
        public bool isBillboard = false;
        public bool isCutout = true;
        public int materialIndex = -1;
        public int[] v = new int[3];
    }
}
