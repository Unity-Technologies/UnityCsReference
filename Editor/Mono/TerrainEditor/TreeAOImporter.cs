// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    internal class TreeAO
    {
        const int kWorkLayer = 29;
        static bool kDebug = false;
        const float occlusion = .5f;

        static Vector3[] directions;

        private static int PermuteCuboid(Vector3[] dirs, int offset, float x, float y, float z)
        {
            dirs[offset + 0] = new Vector3(+x, +y, +z);
            dirs[offset + 1] = new Vector3(+x, +y, -z);
            dirs[offset + 2] = new Vector3(+x, -y, +z);
            dirs[offset + 3] = new Vector3(+x, -y, -z);
            dirs[offset + 4] = new Vector3(-x, +y, +z);
            dirs[offset + 5] = new Vector3(-x, +y, -z);
            dirs[offset + 6] = new Vector3(-x, -y, +z);
            dirs[offset + 7] = new Vector3(-x, -y, -z);
            return offset + 8;
        }

        public static void InitializeDirections()
        {
            // Vertices of truncated icosahedron (60 vertices).
            // http://en.wikipedia.org/wiki/Truncated_icosahedron
            //
            // Using random set of vertices creates slightly biased
            // set of points that are not centered around zero.
            float f = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

            directions = new Vector3[60];
            directions[0] = new Vector3(0, +1, +3 * f);
            directions[1] = new Vector3(0, +1, -3 * f);
            directions[2] = new Vector3(0, -1, +3 * f);
            directions[3] = new Vector3(0, -1, -3 * f);

            directions[4] = new Vector3(+1, +3 * f, 0);
            directions[5] = new Vector3(+1, -3 * f, 0);
            directions[6] = new Vector3(-1, +3 * f, 0);
            directions[7] = new Vector3(-1, -3 * f, 0);

            directions[8] = new Vector3(+3 * f, 0, +1);
            directions[9] = new Vector3(+3 * f, 0, -1);
            directions[10] = new Vector3(-3 * f, 0, +1);
            directions[11] = new Vector3(-3 * f, 0, -1);

            int offset = 12;
            offset = PermuteCuboid(directions, offset, 2, 1 + 2 * f, f);
            offset = PermuteCuboid(directions, offset, 1 + 2 * f, f, 2);
            offset = PermuteCuboid(directions, offset, f, 2, 1 + 2 * f);
            offset = PermuteCuboid(directions, offset, 1, 2 + f, 2 * f);
            offset = PermuteCuboid(directions, offset, 2 + f, 2 * f, 1);
            offset = PermuteCuboid(directions, offset, 2 * f, 1, 2 + f);

            for (int i = 0; i < directions.Length; i++)
                directions[i] = directions[i].normalized;
        }

        public static void CalcSoftOcclusion(Mesh mesh)
        {
            // Create the helper object
            GameObject go = new GameObject("Test");
            go.layer = kWorkLayer;  // Grab a system layer for this
            MeshFilter mf = (MeshFilter)go.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            go.AddComponent<MeshCollider>();

            // Intialize
            if (directions == null)
                InitializeDirections();

            // Calc weights for all directions
            Vector4[] weights = new Vector4[directions.Length];
            for (int i = 0; i < directions.Length; i++)
            {
                weights[i] = new Vector4(GetWeight(1, directions[i]), GetWeight(2, directions[i]), GetWeight(3, directions[i]), GetWeight(0, directions[i]));
            }

            Vector3[] verts = mesh.vertices;
            Vector4[] sh = new Vector4[verts.Length];
            float totalW = 0;
            for (int i = 0; i <  verts.Length; i += 1)
            {
                Vector4 result = Vector4.zero;
                Vector3 v = go.transform.TransformPoint(verts[i]);

                for (int j = 0; j < directions.Length; j++)
                {
                    float occ = CountIntersections(v, go.transform.TransformDirection(directions[j]), 3);
                    occ = Mathf.Pow(occlusion, occ);

                    result += weights[j] * occ;
                }
                result /= directions.Length;
                totalW += result.w;
                sh[i] = result;
            }
            totalW /= verts.Length;
            for (int i = 0; i < verts.Length; i++)
                sh[i].w -= totalW;
            mesh.tangents = sh;

            Object.DestroyImmediate(go);
        }

        static int CountIntersections(Vector3 v, Vector3 dist, float length)
        {
            v += dist * .01f;
            if (!kDebug)
            {
                return Physics.RaycastAll(v, dist, length, 1 << kWorkLayer).Length +
                    Physics.RaycastAll(v + dist * length, -dist, length, 1 << kWorkLayer).Length;
            }

            RaycastHit[] hits = Physics.RaycastAll(v, dist, length, 1 << kWorkLayer);
            int hitLength = hits.Length;
            float maxDist = 0;
            if (hitLength > 0)
                maxDist = hits[hits.Length - 1].distance;

            hits = Physics.RaycastAll(v + dist * length, -dist, length, 1 << kWorkLayer);
            if (hits.Length > 0)
            {
                float len = length - hits[0].distance;
                if (len > maxDist)
                {
                    maxDist = len;
                }
            }

            return hitLength + hits.Length;
        }

        static float GetWeight(int coeff, Vector3 dir)
        {
            switch (coeff)
            {
                case 0: // Just the constant
                    return .5f; // Average of all dimensions.
                case 1:
                    return .5f * dir.x;
                case 2:
                    return .5f * dir.y;
                case 3:
                    return .5f * dir.z;
            }
            Debug.Log("Only defined up to 3");
            return 0;
        }
    }

    internal class TreeAOImporter : AssetPostprocessor
    {
        void OnPostprocessModel(GameObject root)
        {
            // Check if path contains "AO Tree"
            string lowerPath = assetPath.ToLower();
            if (lowerPath.IndexOf("ambient-occlusion") != -1)
            {
                Component[] filters = root.GetComponentsInChildren(typeof(MeshFilter));
                foreach (MeshFilter filter in filters)
                {
                    if (filter.sharedMesh != null)
                    {
                        Mesh mesh = filter.sharedMesh;

                        // Calculate AO
                        TreeAO.CalcSoftOcclusion(mesh);

                        // Calculate vertex colors for tree waving
                        Bounds bounds = mesh.bounds;
                        Color[] colors = mesh.colors;
                        Vector3[] vertices = mesh.vertices;
                        Vector4[] tangents = mesh.tangents;
                        if (colors.Length == 0)
                        {
                            colors = new Color[mesh.vertexCount];
                            for (int i = 0; i < colors.Length; i++)
                                colors[i] = Color.white;
                        }

                        float maxAO = 0.0F;
                        for (int i = 0; i < tangents.Length; i++)
                            maxAO = Mathf.Max(tangents[i].w, maxAO);

                        float largest = 0.0F;
                        for (int i = 0; i < colors.Length; i++)
                        {
                            Vector2 offset = new Vector2(vertices[i].x, vertices[i].z);
                            float branch = offset.magnitude;
                            largest = Mathf.Max(branch, largest);
                        }
                        for (int i = 0; i < colors.Length; i++)
                        {
                            Vector2 offset = new Vector2(vertices[i].x, vertices[i].z);
                            float branch = offset.magnitude / largest;

                            float height = (vertices[i].y - bounds.min.y) / bounds.size.y;
                            //                  colors[i].a = tangents[i].w * maxAO + height;
                            colors[i].a = (height * branch) * 0.6F + height * 0.5F;
                        }
                        mesh.colors = colors;
                    }
                }
            }
        }
    }
} //namespace
