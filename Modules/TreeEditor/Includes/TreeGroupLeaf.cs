// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;

namespace TreeEditor
{
    //
    // Leaf group
    //
    [System.Serializable]
    public class TreeGroupLeaf : TreeGroup
    {
        public enum GeometryMode
        {
            PLANE = 0,
            CROSS = 1,
            TRI_CROSS = 2,
            BILLBOARD = 3,
            MESH = 4
        }

        static class Styles
        {
            public static string groupSeedString = LocalizationDatabase.GetLocalizedString("Group Seed|The seed for this group of leaves. Modify to vary procedural generation.");
            public static string frequencyString = LocalizationDatabase.GetLocalizedString("Frequency|Adjusts the number of leaves created for each parent branch.");
            public static string distributionModeString = LocalizationDatabase.GetLocalizedString("Distribution|Select the way the leaves are distributed along their parent.");
            public static string twirlString = LocalizationDatabase.GetLocalizedString("Twirl|Twirl around the parent branch.");
            public static string whorledStepString = LocalizationDatabase.GetLocalizedString("Whorled Step|Defines how many nodes are in each whorled step when using Whorled distribution. For real plants this is normally a Fibonacci number.");
            public static string growthScaleString = LocalizationDatabase.GetLocalizedString("Growth Scale|Defines the scale of nodes along the parent node. Use the curve to adjust and the slider to fade the effect in and out.");
            public static string growthAngleString = LocalizationDatabase.GetLocalizedString("Growth Angle|Defines the initial angle of growth relative to the parent. Use the curve to adjust and the slider to fade the effect in and out.");
            public static string mainWindString = LocalizationDatabase.GetLocalizedString("Main Wind|Primary wind effect. Usually this should be kept as a low value to avoid leaves floating away from the parent branch.");
            public static string mainTurbulenceString = LocalizationDatabase.GetLocalizedString("Main Turbulence|Secondary turbulence effect. For leaves this should usually be kept as a low value.");
            public static string edgeTurbulenceString = LocalizationDatabase.GetLocalizedString("Edge Turbulence|Defines how much wind turbulence occurs along the edges of the leaves.");
        }

        internal static Dictionary<Texture, Vector2[]> s_TextureHulls;
        internal static bool s_TextureHullsDirty;

        //[field: TreeAttribute("Geometry", "category", 0.0f, 1.0f)]
        //public bool showGeometryProps = true;

        //[field: TreeAttribute("GeometryMode", "popup", "Plane,Cross,Tri-Cross,Billboard,Mesh")]
        public int geometryMode = (int)GeometryMode.PLANE;

        //[field: TreeAttribute("Material", "material", 0.0f, 1.0f, "primitiveGeometry")]
        public Material materialLeaf = null;

        //[field: TreeAttribute("Mesh", "mesh", 0.0f, 1.0f, "meshGeometry")]
        public GameObject instanceMesh = null;

        //[field: TreeAttribute("Shape", "category", 0.0f, 1.0f)]
        //public bool showShapeProps = true;

        //[field: TreeAttribute("Size", "minmaxslider", 0.1f, 10.0f)]
        public Vector2 size = Vector2.one;

        //[field: TreeAttribute("PerpendicularAlign", "slider", 0.0f, 1.0f)]
        public float perpendicularAlign = 0.0f;

        //[field: TreeAttribute("HorizontalAlign", "slider", 0.0f, 1.0f)]
        public float horizontalAlign = 0.0f;

        //
        // Leaves cannot have children...
        //
        override public bool CanHaveSubGroups()
        {
            return false;
        }

        override internal bool HasExternalChanges()
        {
            string hash;
            if (geometryMode == (int)GeometryMode.MESH)
            {
                hash = UnityEditorInternal.InternalEditorUtility.CalculateHashForObjectsAndDependencies(new Object[] { instanceMesh });
            }
            else
            {
                hash = UnityEditorInternal.InternalEditorUtility.CalculateHashForObjectsAndDependencies(new Object[] { materialLeaf });
            }

            if (hash != m_Hash)
            {
                m_Hash = hash;
                return true;
            }

            return false;
        }

        override public void UpdateParameters()
        {
            if (lockFlags == 0)
            {
                for (int n = 0; n < nodes.Count; n++)
                {
                    TreeNode node = nodes[n];

                    Random.InitState(node.seed);

                    for (int x = 0; x < 5; x++)
                    {
                        node.scale *= size.x + ((size.y - size.x) * Random.value);
                    }

                    for (int x = 0; x < 5; x++)
                    {
                        float rx = (Random.value - 0.5f) * 180.0f * (1.0f - perpendicularAlign);
                        float ry = (Random.value - 0.5f) * 180.0f * (1.0f - perpendicularAlign);
                        float rz = 0.0f;
                        node.rotation = Quaternion.Euler(rx, ry, rz);
                    }
                }
            }

            UpdateMatrix();

            // Update child groups..
            base.UpdateParameters();
        }

        override public void UpdateMatrix()
        {
            if (parentGroup == null)
            {
                for (int n = 0; n < nodes.Count; n++)
                {
                    TreeNode node = nodes[n];
                    node.matrix = Matrix4x4.identity;
                }
            }
            else
            {
                TreeGroupRoot tgr = parentGroup as TreeGroupRoot;

                for (int n = 0; n < nodes.Count; n++)
                {
                    TreeNode node = nodes[n];

                    Vector3 pos = new Vector3();
                    Quaternion rot = new Quaternion();
                    float rad = 0.0f;

                    float surfaceAngle = 0.0f;

                    if (tgr != null)
                    {
                        // attached to root node
                        float dist = node.offset * GetRootSpread();
                        float ang = node.angle * Mathf.Deg2Rad;
                        pos = new Vector3(Mathf.Cos(ang) * dist, -tgr.groundOffset, Mathf.Sin(ang) * dist);
                        rot = Quaternion.Euler(node.pitch * -Mathf.Sin(ang), 0, node.pitch * Mathf.Cos(ang)) * Quaternion.Euler(0, node.angle, 0);
                    }
                    else
                    {
                        // attached to branch node
                        node.parent.GetPropertiesAtTime(node.offset, out pos, out rot, out rad);
                        surfaceAngle = node.parent.GetSurfaceAngleAtTime(node.offset);
                    }

                    Quaternion angle = Quaternion.Euler(90.0f, node.angle, 0.0f);
                    Matrix4x4 aog = Matrix4x4.TRS(Vector3.zero, angle, Vector3.one);

                    // pitch matrix
                    Matrix4x4 pit = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(node.pitch + surfaceAngle, 0.0f, 0.0f), Vector3.one);

                    node.matrix = node.parent.matrix * Matrix4x4.TRS(pos, rot, Vector3.one) * aog * pit;

                    // spin
                    node.matrix *= Matrix4x4.TRS(new Vector3(0, 0, 0), node.rotation, new Vector3(1, 1, 1));

                    // horizontalize
                    if (horizontalAlign > 0.0f)
                    {
                        Vector4 opos = node.matrix.GetColumn(3);
                        Quaternion targetQuat = Quaternion.Euler(90.0f, node.angle, 0.0f);
                        Quaternion newQuat = Quaternion.Slerp(MathUtils.QuaternionFromMatrix(node.matrix), targetQuat,
                            horizontalAlign);

                        node.matrix = Matrix4x4.TRS(Vector3.zero, newQuat, Vector3.one);
                        node.matrix.SetColumn(3, opos);
                    }

                    Vector3 off;
                    if (geometryMode == (int)GeometryMode.MESH)
                    {
                        off = node.matrix.MultiplyPoint(new Vector3(0, rad, 0));
                        node.matrix = node.matrix * Matrix4x4.Scale(new Vector3(node.scale, node.scale, node.scale));
                    }
                    else
                    {
                        off = node.matrix.MultiplyPoint(new Vector3(0, rad + node.scale, 0));
                    }

                    node.matrix.m03 = off.x;
                    node.matrix.m13 = off.y;
                    node.matrix.m23 = off.z;
                }
            }

            // Update child groups..
            base.UpdateMatrix();
        }

        override public void BuildAOSpheres(List<TreeAOSphere> aoSpheres)
        {
            if (visible)
            {
                float scaleFactor = 0.75f;

                for (int n = 0; n < nodes.Count; n++)
                {
                    TreeNode node = nodes[n];

                    if (!node.visible) continue;

                    Vector3 pos = node.matrix.MultiplyPoint(new Vector3(0, 0, 0));
                    float rad = node.scale * scaleFactor;

                    aoSpheres.Add(new TreeAOSphere(pos, rad, 0.5f));
                }
            }

            // Update child groups..
            base.BuildAOSpheres(aoSpheres);
        }

        private static Mesh cloneMesh;
        private static MeshFilter cloneMeshFilter;
        private static Vector3[] cloneVerts;
        private static Vector3[] cloneNormals;
        private static Vector2[] cloneUVs;
        private static Vector4[] cloneTangents;

        override public void UpdateMesh(List<TreeMaterial> materials, List<TreeVertex> verts, List<TreeTriangle> tris, List<TreeAOSphere> aoSpheres, int buildFlags, float adaptiveQuality, float aoDensity)
        {
            if (geometryMode == (int)GeometryMode.MESH)
            {
                // Skip if no instance mesh is selected
                if (instanceMesh != null)
                {
                    cloneMeshFilter = instanceMesh.GetComponent<MeshFilter>();
                    if (cloneMeshFilter != null)
                    {
                        cloneMesh = cloneMeshFilter.sharedMesh;
                        if (cloneMesh != null)
                        {
                            Vector3 meshSize = cloneMesh.bounds.extents;
                            float meshScale = Mathf.Max(meshSize.x, meshSize.z) * 0.5f;
                            cloneVerts = cloneMesh.vertices;
                            cloneNormals = cloneMesh.normals;
                            cloneUVs = cloneMesh.uv;
                            cloneTangents = cloneMesh.tangents;

                            // rescale to fit with size of the planes we're making..
                            for (int i = 0; i < cloneVerts.Length; i++)
                            {
                                cloneVerts[i].x /= meshScale;
                                cloneVerts[i].y /= meshScale;
                                cloneVerts[i].z /= meshScale;
                            }
                        }
                    }

                    if (instanceMesh.GetComponent<Renderer>() != null)
                    {
                        Material[] sharedMaterials = instanceMesh.GetComponent<Renderer>().sharedMaterials;
                        for (int i = 0; i < sharedMaterials.Length; i++)
                        {
                            GetMaterialIndex(sharedMaterials[i], materials, false);
                        }
                    }
                }
            }
            else
            {
                GetMaterialIndex(materialLeaf, materials, false);
            }

            for (int n = 0; n < nodes.Count; n++)
            {
                UpdateNodeMesh(nodes[n], materials, verts, tris, aoSpheres, buildFlags, adaptiveQuality, aoDensity);
            }

            // cloneRenderer = null;
            cloneMesh = null;
            cloneMeshFilter = null;
            cloneVerts = null;
            cloneNormals = null;
            cloneUVs = null;
            cloneTangents = null;

            // Update child groups..
            base.UpdateMesh(materials, verts, tris, aoSpheres, buildFlags, adaptiveQuality, aoDensity);
        }

        private static TreeVertex CreateBillboardVertex(TreeNode node, Quaternion billboardRotation, Vector3 normalBase, float normalFix, Vector3 tangentBase, Vector2 uv)
        {
            TreeVertex vertex = new TreeVertex();

            vertex.pos = node.matrix.MultiplyPoint(Vector3.zero);
            vertex.uv0 = uv;

            uv = 2.0f * uv - Vector2.one;

            // Store billboard spread in the normal,
            // normal will be reconstructed in the vertex shader.
            vertex.nor = (billboardRotation * (new Vector3(uv.x * node.scale, uv.y * node.scale, 0.0f)));
            vertex.nor.z = normalFix;

            // normal
            Vector3 normal = (billboardRotation * (new Vector3(uv.x * normalBase.x, uv.y * normalBase.y, normalBase.z))).normalized;

            // calculate tangent from the normal
            vertex.tangent = (tangentBase - normal * Vector3.Dot(tangentBase, normal)).normalized;
            vertex.tangent.w = 0.0f;

            return vertex;
        }

        private Vector2[] GetPlaneHullVertices(Material mat)
        {
            if (mat == null)
                return null;
            if (!mat.HasProperty("_MainTex"))
                return null;
            Texture tex = mat.mainTexture;
            if (!tex)
                return null;
            if (s_TextureHulls == null || s_TextureHullsDirty)
            {
                s_TextureHulls = new Dictionary<Texture, Vector2[]>();
                s_TextureHullsDirty = false;
            }
            if (s_TextureHulls.ContainsKey(tex))
                return s_TextureHulls[tex];

            Vector2[] textureHull = UnityEditor.MeshUtility.ComputeTextureBoundingHull(tex, 4);

            Vector2 tmp = textureHull[1];
            textureHull[1] = textureHull[3];
            textureHull[3] = tmp;

            s_TextureHulls.Add(tex, textureHull);
            return textureHull;
        }

        private void UpdateNodeMesh(TreeNode node, List<TreeMaterial> materials, List<TreeVertex> verts, List<TreeTriangle> tris, List<TreeAOSphere> aoSpheres, int buildFlags, float adaptiveQuality, float aoDensity)
        {
            node.triStart = tris.Count;
            node.triEnd = tris.Count;
            node.vertStart = verts.Count;
            node.vertEnd = verts.Count;

            // Check for visibility..
            if (!node.visible || !visible) return;

            Profiler.BeginSample("TreeGroupLeaf.UpdateNodeMesh");

            Vector2 windFactors = ComputeWindFactor(node, node.offset);

            if (geometryMode == (int)GeometryMode.MESH)
            {
                // Exit if no instance mesh is selected
                if (cloneMesh == null)
                {
                    //    Debug.LogError("No cloneMesh");
                    return;
                }
                if (cloneVerts == null)
                {
                    //     Debug.LogError("No cloneVerts");
                    return;
                }
                if (cloneNormals == null)
                {
                    //    Debug.LogError("No cloneNormals");
                    return;
                }
                if (cloneTangents == null)
                {
                    //   Debug.LogError("No cloneTangents");
                    return;
                }
                if (cloneUVs == null)
                {
                    //   Debug.LogError("No cloneUVs");
                    return;
                }

                Matrix4x4 cloneMatrix = instanceMesh.transform.localToWorldMatrix;
                Matrix4x4 tformMatrix = node.matrix * cloneMatrix;

                int vertOffset = verts.Count;

                float dist = 5.0f;

                // copy verts
                for (int i = 0; i < cloneVerts.Length; i++)
                {
                    TreeVertex v0 = new TreeVertex();
                    v0.pos = tformMatrix.MultiplyPoint(cloneVerts[i]);
                    v0.nor = tformMatrix.MultiplyVector(cloneNormals[i]).normalized;
                    v0.uv0 = new Vector2(cloneUVs[i].x, cloneUVs[i].y);
                    Vector3 tangent = tformMatrix.MultiplyVector(new Vector3(cloneTangents[i].x, cloneTangents[i].y, cloneTangents[i].z)).normalized;
                    v0.tangent = new Vector4(tangent.x, tangent.y, tangent.z, cloneTangents[i].w);

                    // wind
                    float windEdge = (cloneVerts[i].magnitude / dist) * animationEdge;
                    v0.SetAnimationProperties(windFactors.x, windFactors.y, windEdge, node.animSeed);

                    // AO
                    if ((buildFlags & (int)BuildFlag.BuildAmbientOcclusion) != 0)
                    {
                        v0.SetAmbientOcclusion(ComputeAmbientOcclusion(v0.pos, v0.nor, aoSpheres, aoDensity));
                    }

                    verts.Add(v0);
                }

                // copy tris
                for (int s = 0; s < cloneMesh.subMeshCount; s++)
                {
                    int[] instanceTris = cloneMesh.GetTriangles(s);

                    int materialIndex;
                    if (instanceMesh.GetComponent<Renderer>() != null && s < instanceMesh.GetComponent<Renderer>().sharedMaterials.Length)
                    {
                        materialIndex = GetMaterialIndex(instanceMesh.GetComponent<Renderer>().sharedMaterials[s], materials, false);
                    }
                    else
                    {
                        materialIndex = GetMaterialIndex(null, materials, false);
                    }

                    for (int i = 0; i < instanceTris.Length; i += 3)
                    {
                        TreeTriangle t0 = new TreeTriangle(materialIndex, instanceTris[i] + vertOffset, instanceTris[i + 1] + vertOffset, instanceTris[i + 2] + vertOffset);
                        tris.Add(t0);
                    }
                }
            }
            else if (geometryMode == (int)GeometryMode.BILLBOARD)
            {
                // rotation
                Vector3 eulerRot = node.rotation.eulerAngles;
                eulerRot.z = eulerRot.x * 2.0f;
                eulerRot.x = 0.0f;
                eulerRot.y = 0.0f;
                Quaternion billboardRotation = Quaternion.Euler(eulerRot);

                // normal
                Vector3 normalBase = new Vector3(GenerateBendBillboardNormalFactor, GenerateBendBillboardNormalFactor, 1.0f);

                Vector3 tangentBase = billboardRotation * new Vector3(1, 0, 0);
                float normalFix = node.scale / (GenerateBendBillboardNormalFactor * GenerateBendBillboardNormalFactor);

                TreeVertex v0 = CreateBillboardVertex(node, billboardRotation, normalBase, normalFix, tangentBase, new Vector2(0, 1));
                TreeVertex v1 = CreateBillboardVertex(node, billboardRotation, normalBase, normalFix, tangentBase, new Vector2(0, 0));
                TreeVertex v2 = CreateBillboardVertex(node, billboardRotation, normalBase, normalFix, tangentBase, new Vector2(1, 0));
                TreeVertex v3 = CreateBillboardVertex(node, billboardRotation, normalBase, normalFix, tangentBase, new Vector2(1, 1));

                // wind
                v0.SetAnimationProperties(windFactors.x, windFactors.y, animationEdge, node.animSeed);
                v1.SetAnimationProperties(windFactors.x, windFactors.y, animationEdge, node.animSeed);
                v2.SetAnimationProperties(windFactors.x, windFactors.y, animationEdge, node.animSeed);
                v3.SetAnimationProperties(windFactors.x, windFactors.y, animationEdge, node.animSeed);

                if ((buildFlags & (int)BuildFlag.BuildAmbientOcclusion) != 0)
                {
                    //  Vector3 pushU = Vector3.up * internalSize;
                    Vector3 pushR = Vector3.right * node.scale;
                    Vector3 pushF = Vector3.forward * node.scale;

                    float a = 0.0f; // ComputeAmbientOcclusion(partID, v0.pos + pushU, Vector3.up, aoSpheres);
                    //a += ComputeAmbientOcclusion(partID, v0.pos - pushU, -Vector3.up, aoSpheres);
                    a = ComputeAmbientOcclusion(v0.pos + pushR, Vector3.right, aoSpheres, aoDensity);
                    a += ComputeAmbientOcclusion(v0.pos - pushR, -Vector3.right, aoSpheres, aoDensity);
                    a += ComputeAmbientOcclusion(v0.pos + pushF, Vector3.forward, aoSpheres, aoDensity);
                    a += ComputeAmbientOcclusion(v0.pos - pushF, -Vector3.forward, aoSpheres, aoDensity);
                    a /= 4.0f;

                    v0.SetAmbientOcclusion(a);
                    v1.SetAmbientOcclusion(a);
                    v2.SetAmbientOcclusion(a);
                    v3.SetAmbientOcclusion(a);
                }

                int index0 = verts.Count;
                verts.Add(v0);
                verts.Add(v1);
                verts.Add(v2);
                verts.Add(v3);

                int materialIndex = GetMaterialIndex(materialLeaf, materials, false);

                tris.Add(new TreeTriangle(materialIndex, index0, index0 + 2, index0 + 1, true));
                tris.Add(new TreeTriangle(materialIndex, index0, index0 + 3, index0 + 2, true));
            }
            else
            {
                // plane, cross, tri-cross

                int planes = 0;
                switch ((GeometryMode)geometryMode)
                {
                    case GeometryMode.PLANE:
                        planes = 1;
                        break;
                    case GeometryMode.CROSS:
                        planes = 2;
                        break;
                    case GeometryMode.TRI_CROSS:
                        planes = 3;
                        break;
                }

                int materialIndex = GetMaterialIndex(materialLeaf, materials, false);

                Vector2[] rawHull = new Vector2[]
                {
                    new Vector2(0, 1),
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(1, 1)
                };
                Vector2[] textureHull = GetPlaneHullVertices(materialLeaf);
                if (textureHull == null)
                    textureHull = rawHull;
                float ns = node.scale;
                Vector3[] positionsRaw = new Vector3[]
                {
                    new Vector3(-ns, 0f, -ns),
                    new Vector3(-ns, 0f,  ns),
                    new Vector3(ns, 0f,  ns),
                    new Vector3(ns, 0f, -ns)
                };

                Vector3 normal = new Vector3(GenerateBendNormalFactor, 1.0f - GenerateBendNormalFactor, GenerateBendNormalFactor);

                Vector3[] normalsRaw = new Vector3[]
                {
                    new Vector3(-normal.x, normal.y, -normal.z).normalized,
                    new Vector3(-normal.x, normal.y, 0).normalized, // note z always 0
                    new Vector3(normal.x, normal.y, 0).normalized, // note z always 0
                    new Vector3(normal.x, normal.y, -normal.z).normalized
                };


                for (int ipl = 0; ipl < planes; ipl++)
                {
                    Quaternion rot = Quaternion.Euler(new Vector3(90, 0, 0));
                    switch (ipl)
                    {
                        case 1:
                            rot = Quaternion.Euler(new Vector3(90, 90, 0));
                            break;
                        case 2:
                            rot = Quaternion.Euler(new Vector3(0, 90, 0));
                            break;
                    }

                    TreeVertex[] tv = new TreeVertex[8]
                    {
                        new TreeVertex(), new TreeVertex(), new TreeVertex(), new TreeVertex(), // initial quad
                        new TreeVertex(), new TreeVertex(), new TreeVertex(), new TreeVertex() // from bounding hull
                    };

                    for (int i = 0; i < 4; ++i)
                    {
                        tv[i].pos = node.matrix.MultiplyPoint(rot * positionsRaw[i]);
                        tv[i].nor = node.matrix.MultiplyVector(rot * normalsRaw[i]);
                        tv[i].tangent = CreateTangent(node, rot, tv[i].nor);
                        tv[i].uv0 = textureHull[i];
                        tv[i].SetAnimationProperties(windFactors.x, windFactors.y, animationEdge, node.animSeed);
                        if ((buildFlags & (int)BuildFlag.BuildAmbientOcclusion) != 0)
                        {
                            tv[i].SetAmbientOcclusion(ComputeAmbientOcclusion(tv[i].pos, tv[i].nor, aoSpheres, aoDensity));
                        }
                    }

                    // now lerp positions into correct placed based on the bounding hull
                    for (int i = 0; i < 4; ++i)
                    {
                        tv[i + 4].Lerp4(tv, textureHull[i]);
                        tv[i + 4].uv0 = tv[i].uv0;
                        tv[i + 4].uv1 = tv[i].uv1;
                        tv[i + 4].flag = tv[i].flag;
                    }

                    int index0 = verts.Count;
                    for (int i = 0; i < 4; ++i)
                        verts.Add(tv[i + 4]);

                    tris.Add(new TreeTriangle(materialIndex, index0, index0 + 1, index0 + 2));
                    tris.Add(new TreeTriangle(materialIndex, index0, index0 + 2, index0 + 3));

                    Vector3 faceNormal = node.matrix.MultiplyVector(rot * new Vector3(0, 1, 0));

                    if (GenerateDoubleSidedGeometry)
                    {
                        // Duplicate vertices with mirrored normal and tangent
                        TreeVertex[] tv2 = new TreeVertex[8]
                        {
                            new TreeVertex(), new TreeVertex(), new TreeVertex(), new TreeVertex(), // initial quad
                            new TreeVertex(), new TreeVertex(), new TreeVertex(), new TreeVertex() // from bounding hull
                        };
                        for (int i = 0; i < 4; ++i)
                        {
                            tv2[i].pos = tv[i].pos;
                            tv2[i].nor = Vector3.Reflect(tv[i].nor, faceNormal);
                            tv2[i].tangent = Vector3.Reflect(tv[i].tangent, faceNormal);
                            tv2[i].tangent.w = -1;
                            tv2[i].uv0 = tv[i].uv0;
                            tv2[i].SetAnimationProperties(windFactors.x, windFactors.y, animationEdge, node.animSeed);
                            if ((buildFlags & (int)BuildFlag.BuildAmbientOcclusion) != 0)
                            {
                                tv2[i].SetAmbientOcclusion(ComputeAmbientOcclusion(tv2[i].pos, tv2[i].nor, aoSpheres, aoDensity));
                            }
                        }
                        // now lerp positions into correct placed based on the bounding hull
                        for (int i = 0; i < 4; ++i)
                        {
                            tv2[i + 4].Lerp4(tv2, textureHull[i]);
                            tv2[i + 4].uv0 = tv2[i].uv0;
                            tv2[i + 4].uv1 = tv2[i].uv1;
                            tv2[i + 4].flag = tv2[i].flag;
                        }

                        int index4 = verts.Count;
                        for (int i = 0; i < 4; ++i)
                            verts.Add(tv2[i + 4]);
                        tris.Add(new TreeTriangle(materialIndex, index4, index4 + 2, index4 + 1));
                        tris.Add(new TreeTriangle(materialIndex, index4, index4 + 3, index4 + 2));
                    }
                }
            }

            node.triEnd = tris.Count;
            node.vertEnd = verts.Count;

            Profiler.EndSample(); // TreeGroupLeaf.UpdateNodeMesh
        }

        internal override string GroupSeedString { get { return Styles.groupSeedString; } }

        internal override string FrequencyString { get { return Styles.frequencyString; } }

        internal override string DistributionModeString { get { return Styles.distributionModeString; } }

        internal override string TwirlString { get { return Styles.twirlString; } }

        internal override string WhorledStepString { get { return Styles.whorledStepString; } }

        internal override string GrowthScaleString { get { return Styles.growthScaleString; } }

        internal override string GrowthAngleString { get { return Styles.growthAngleString; } }

        internal override string MainWindString { get { return Styles.mainWindString; } }

        internal override string MainTurbulenceString { get { return Styles.mainTurbulenceString; } }

        internal override string EdgeTurbulenceString { get { return Styles.edgeTurbulenceString; } }
    }
}
