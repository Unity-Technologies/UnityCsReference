// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
using System.IO;

namespace TreeEditor
{
    public class TreeData : ScriptableObject
    {
        [SerializeField]
        private int _uniqueID;

        public string materialHash;

        public TreeGroupRoot root;
        public TreeGroupBranch[] branchGroups;
        public TreeGroupLeaf[] leafGroups;
        public TreeNode[] nodes;
        public Mesh mesh;
        public Material optimizedSolidMaterial;
        public Material optimizedCutoutMaterial;
        public bool isInPreviewMode;

        //
        // resolve references..
        //
        public TreeGroup GetGroup(int id)
        {
            if (id == root.uniqueID)
            {
                return root;
            }

            for (int i = 0; i < branchGroups.Length; i++)
            {
                if (branchGroups[i].uniqueID == id)
                {
                    return branchGroups[i];
                }
            }
            for (int i = 0; i < leafGroups.Length; i++)
            {
                if (leafGroups[i].uniqueID == id)
                {
                    return leafGroups[i];
                }
            }
            return null;
        }

        public TreeNode GetNode(int id)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].uniqueID == id)
                {
                    return nodes[i];
                }
            }
            return null;
        }

        //
        // Get at index
        //
        private int GetNodeCount()
        {
            return nodes.Length;
        }

        private TreeNode GetNodeAt(int i)
        {
            if (i >= 0 && i < nodes.Length)
            {
                return nodes[i];
            }
            return null;
        }

        private int GetGroupCount()
        {
            return 1 + branchGroups.Length + leafGroups.Length;
        }

        private TreeGroup GetGroupAt(int i)
        {
            if (i == 0)
            {
                return root;
            }
            i--;

            if (i >= 0 && i < branchGroups.Length)
            {
                return branchGroups[i];
            }
            i -= branchGroups.Length;

            if (i >= 0 && i < leafGroups.Length)
            {
                return leafGroups[i];
            }
            return null;
        }

        //
        // Validate references
        //
        public void ValidateReferences()
        {
            Profiler.BeginSample("ValidateReferences");

            //
            // Fill direct references
            //
            int groupCount = GetGroupCount();
            for (int i = 0; i < groupCount; i++)
            {
                TreeGroup g = GetGroupAt(i);

                g.parentGroup = GetGroup(g.parentGroupID);
                g.childGroups.Clear();
                g.nodes.Clear();

                for (int j = 0; j < g.childGroupIDs.Length; j++)
                {
                    TreeGroup child = GetGroup(g.childGroupIDs[j]);
                    g.childGroups.Add(child);
                }
                for (int j = 0; j < g.nodeIDs.Length; j++)
                {
                    TreeNode node = GetNode(g.nodeIDs[j]);
                    g.nodes.Add(node);
                }
            }

            int nodeCount = GetNodeCount();
            for (int i = 0; i < nodeCount; i++)
            {
                TreeNode n = GetNodeAt(i);
                n.parent = GetNode(n.parentID);
                n.group = GetGroup(n.groupID);
            }
            Profiler.EndSample(); // ValidateReferences
        }

        //
        // Clear all direct references
        //
        public void ClearReferences()
        {
            for (int i = 0; i < branchGroups.Length; i++)
            {
                branchGroups[i].parentGroup = null;
                branchGroups[i].childGroups.Clear();
                branchGroups[i].nodes.Clear();
            }
            for (int i = 0; i < leafGroups.Length; i++)
            {
                leafGroups[i].parentGroup = null;
                leafGroups[i].childGroups.Clear();
                leafGroups[i].nodes.Clear();
            }
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].parent = null;
                nodes[i].group = null;
            }
        }

        //
        // Group management
        //
        public TreeGroup AddGroup(TreeGroup parent, System.Type type)
        {
            TreeGroup g = null;
            if (type == typeof(TreeGroupBranch))
            {
                g = new TreeGroupBranch();
                branchGroups = ArrayAdd(branchGroups, g as TreeGroupBranch);
            }
            else if (type == typeof(TreeGroupLeaf))
            {
                g = new TreeGroupLeaf();
                leafGroups = ArrayAdd(leafGroups, g as TreeGroupLeaf);
            }
            else
            {
                return null;
            }

            // set unique ID
            g.uniqueID = _uniqueID;
            _uniqueID++;

            // set initial properties
            g.parentGroupID = 0;
            g.distributionFrequency = 1;

            // set parent
            SetGroupParent(g, parent);

            // validate hierachy // already done in setgroupparent
            //ValidateReferences();

            return g;
        }

        // Copies public fields from n to n2, deep copy where needed...
        private void CopyFields(object n, object n2)
        {
            if (n.GetType() != n2.GetType())
            {
                return;
            }

            System.Reflection.FieldInfo[] fields = n.GetType().GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].IsPublic)
                {
                    if (fields[i].FieldType == typeof(TreeSpline))
                    {
                        // Tree Spline
                        TreeSpline origSpline = fields[i].GetValue(n) as TreeSpline;
                        fields[i].SetValue(n2, new TreeSpline(origSpline));
                    }
                    else if (fields[i].FieldType == typeof(AnimationCurve))
                    {
                        // Animation curve
                        AnimationCurve origCurve = fields[i].GetValue(n) as AnimationCurve;
                        AnimationCurve newCurve = new AnimationCurve(origCurve.keys);
                        newCurve.postWrapMode = origCurve.postWrapMode;
                        newCurve.preWrapMode = origCurve.preWrapMode;
                        fields[i].SetValue(n2, newCurve);
                    }
                    else
                    {
                        fields[i].SetValue(n2, fields[i].GetValue(n));
                    }
                }
            }
        }

        public TreeGroup DuplicateGroup(TreeGroup g)
        {
            TreeGroup g2 = AddGroup(GetGroup(g.parentGroupID), g.GetType());
            CopyFields(g, g2);

            // Don't do stuff with kids
            g2.childGroupIDs = new int[0];
            g2.nodeIDs = new int[0];

            // Create nodes and clone stuff
            for (int i = 0; i < g.nodeIDs.Length; i++)
            {
                TreeNode n = GetNode(g.nodeIDs[i]);
                TreeNode n2 = AddNode(g2, GetNode(n.parentID));
                CopyFields(n, n2);
                n2.groupID = g2.uniqueID;
            }

            return g2;
        }

        public void DeleteGroup(TreeGroup g)
        {
            // remove nodes
            for (int i = g.nodes.Count - 1; i >= 0; i--)
            {
                DeleteNode(g.nodes[i], false);
            }

            if (g.GetType() == typeof(TreeGroupBranch))
            {
                branchGroups = ArrayRemove(branchGroups, g as TreeGroupBranch);
            }
            else if (g.GetType() == typeof(TreeGroupLeaf))
            {
                leafGroups = ArrayRemove(leafGroups, g as TreeGroupLeaf);
            }

            // unlink..
            SetGroupParent(g, null);
        }

        public void SetGroupParent(TreeGroup g, TreeGroup parent)
        {
            TreeGroup origParent = GetGroup(g.parentGroupID);
            if (origParent != null)
            {
                origParent.childGroupIDs = ArrayRemove(origParent.childGroupIDs, g.uniqueID);
                origParent.childGroups.Remove(g);
            }

            if (parent != null)
            {
                g.parentGroup = parent;
                g.parentGroupID = parent.uniqueID;

                parent.childGroups.Add(g);
                parent.childGroupIDs = ArrayAdd(parent.childGroupIDs, g.uniqueID);
            }
            else
            {
                g.parentGroup = null;
                g.parentGroupID = 0;
            }
            ValidateReferences();

            // update frequency!
            UpdateFrequency(g.uniqueID);
        }

        public void LockGroup(TreeGroup g)
        {
            g.Lock();
        }

        public void UnlockGroup(TreeGroup g)
        {
            g.Unlock();
            UpdateFrequency(g.uniqueID);
        }

        public bool IsAncestor(TreeGroup ancestor, TreeGroup g)
        {
            if (g == null)
            {
                return false;
            }

            TreeGroup parent = GetGroup(g.parentGroupID);
            while (parent != null)
            {
                if (parent == ancestor)
                {
                    return true;
                }
                parent = GetGroup(parent.parentGroupID);
            }

            return false;
        }

        //
        // Node management
        //
        public TreeNode AddNode(TreeGroup g, TreeNode parent)
        {
            return AddNode(g, parent, true);
        }

        public TreeNode AddNode(TreeGroup g, TreeNode parent, bool validate)
        {
            if (g == null)
            {
                return null;
            }

            TreeNode n = new TreeNode();
            n.uniqueID = _uniqueID;
            _uniqueID++;

            // set node parent
            SetNodeParent(n, parent);

            // set group (both ID and temporary direct reference)
            n.groupID = g.uniqueID;
            n.group = g;

            // add to group (both ID and temporary direct reference)
            g.nodeIDs = ArrayAdd(g.nodeIDs, n.uniqueID);
            g.nodes.Add(n);

            nodes = ArrayAdd(nodes, n);

            // validate references
            if (validate)
            {
                ValidateReferences();
            }

            return n;
        }

        public void SetNodeParent(TreeNode n, TreeNode parent)
        {
            if (parent != null)
            {
                n.parentID = parent.uniqueID;
                n.parent = parent;
            }
            else
            {
                n.parentID = 0;
                n.parent = null;
            }
        }

        public void DeleteNode(TreeNode n)
        {
            DeleteNode(n, true);
        }

        public void DeleteNode(TreeNode n, bool validate)
        {
            TreeGroup group = GetGroup(n.groupID);
            if (group != null)
            {
                // remove from this group
                group.nodeIDs = ArrayRemove(group.nodeIDs, n.uniqueID);
                group.nodes.Remove(n);

                // remove attached nodes
                for (int i = 0; i < group.childGroups.Count; i++)
                {
                    TreeGroup kidGroup = group.childGroups[i];

                    // go backwards through list to make deletion a little cleaner. (would require stepping j back on deletion otherwise)
                    for (int j = kidGroup.nodes.Count - 1; j >= 0; j--)
                    {
                        if ((kidGroup.nodes[j] != null) && (kidGroup.nodes[j].parentID == n.uniqueID))
                        {
                            DeleteNode(kidGroup.nodes[j], false);
                        }
                    }
                }
            }

            n.group = null;
            n.groupID = 0;
            n.parent = null;
            n.parentID = 0;

            nodes = ArrayRemove(nodes, n);

            if (validate)
            {
                ValidateReferences();
            }
        }

        public TreeNode DuplicateNode(TreeNode n)
        {
            TreeGroup group = GetGroup(n.groupID);
            if (group == null)
            {
                return null;
            }

            TreeNode n2 = AddNode(group, GetNode(n.parentID));
            CopyFields(n, n2);

            return n2;
        }

        //
        // Initializes data..
        //
        public void Initialize()
        {
            if (root == null)
            {
                branchGroups = new TreeGroupBranch[0];
                leafGroups = new TreeGroupLeaf[0];
                nodes = new TreeNode[0];

                // set unique id counter...
                _uniqueID = 1;

                root = new TreeGroupRoot();
                root.uniqueID = _uniqueID;
                root.distributionFrequency = 1;

                _uniqueID++;

                UpdateFrequency(root.uniqueID);
                AddGroup(root, typeof(TreeGroupBranch));
            }
        }

        // Update seed
        public void UpdateSeed(int id)
        {
            TreeGroup g = GetGroup(id);
            if (g == null)
            {
                return;
            }

            var origState = Random.state;
            ClearReferences();
            ValidateReferences();

            g.UpdateSeed();
            g.UpdateDistribution(true, true);

            ClearReferences();
            Random.state = origState;
        }

        // Update frequency
        public void UpdateFrequency(int id)
        {
            TreeGroup g = GetGroup(id);
            if (g == null)
            {
                return;
            }

            var origState = Random.state;
            ClearReferences();
            ValidateReferences();

            g.UpdateFrequency(this);

            ClearReferences();
            Random.state = origState;
        }

        // Complete update of distribution properties..
        public void UpdateDistribution(int id)
        {
            TreeGroup g = GetGroup(id);
            if (g == null)
            {
                return;
            }

            var origState = Random.state;
            ClearReferences();
            ValidateReferences();

            g.UpdateDistribution(true, true);

            ClearReferences();
            Random.state = origState;
        }

        static public int GetAdaptiveHeightSegments(float h, float adaptiveQuality)
        {
            return (int)Mathf.Max(h * adaptiveQuality, 2);
        }

        static public int GetAdaptiveRadialSegments(float r, float adaptiveQuality)
        {
            int segs = ((int)(r * 24 * adaptiveQuality)) / 2 * 2;
            return (int)Mathf.Clamp(segs, 4, 32);
        }

        static public List<float> GetAdaptiveSamples(TreeGroup group, TreeNode node, float adaptiveQuality)
        {
            List<float> samplePoints = new List<float>();

            if (node.spline == null) return samplePoints;

            // starting point for cap smoothing
            float capStartPoint = 1.0f - node.capRange;

            // set initial sample points
            // one at each spline node
            SplineNode[] nodes = node.spline.GetNodes();
            for (int i = 0; i < nodes.Length; i++)
            {
                // break..
                if (nodes[i].time >= node.breakOffset)
                {
                    samplePoints.Add(node.breakOffset);
                    break;
                }
                else if (nodes[i].time > capStartPoint)
                {
                    samplePoints.Add(capStartPoint);
                    break;
                }
                else
                {
                    samplePoints.Add(nodes[i].time);
                }
            }

            // make sure they are in the correct order
            samplePoints.Sort();

            if (samplePoints.Count < 2)
            {
                return samplePoints;
            }

            //
            // subdivide according to rotation
            // as direction is not precise enough
            // to adjust for small details
            //
            float rr = 1.0f;
            if (group.GetType() == typeof(TreeGroupBranch))
            {
                rr = ((TreeGroupBranch)group).radius;
            }

            // subdivide as nescessary
            float thresholdA = Mathf.Lerp(0.999f, 0.99999f, adaptiveQuality); // for optimize
            float thresholdB = Mathf.Lerp(0.5f, 0.985f, adaptiveQuality); // for subdivision
            float thresholdR = Mathf.Lerp(0.3f * rr, 0.1f * rr, adaptiveQuality); // for radius (un-affected by actual radius, only curve)

            // Maximal number of subdivisions
            int maxDivisions = 200;

            //
            // Sub-Divide:
            // Sub-divide where nescessary
            //
            int first = 0;
            while (first < samplePoints.Count - 1)
            {
                for (int i = first; i < samplePoints.Count - 1; i++)
                {
                    // end rotations for segment
                    Quaternion a = node.spline.GetRotationAtTime(samplePoints[i]);
                    Quaternion b = node.spline.GetRotationAtTime(samplePoints[i + 1]);

                    // rotated up vectors
                    Vector3 upA = a * Vector3.up;
                    Vector3 upB = b * Vector3.up;

                    // rotated right vectors
                    Vector3 rightA = a * Vector3.right;
                    Vector3 rightB = b * Vector3.right;

                    // rotated front vectors
                    Vector3 frontA = a * Vector3.forward;
                    Vector3 frontB = b * Vector3.forward;

                    // radius
                    float radiusA = group.GetRadiusAtTime(node, samplePoints[i], true);
                    float radiusB = group.GetRadiusAtTime(node, samplePoints[i + 1], true);

                    bool needSubDivision = false;

                    // check end rotations against each other
                    if (Vector3.Dot(upA, upB) < thresholdB) needSubDivision = true;
                    if (Vector3.Dot(rightA, rightB) < thresholdB) needSubDivision = true;
                    if (Vector3.Dot(frontA, frontB) < thresholdB) needSubDivision = true;

                    // Check radius difference
                    if (Mathf.Abs(radiusA - radiusB) > thresholdR) needSubDivision = true;

                    if (needSubDivision)
                    {
                        maxDivisions--;
                        if (maxDivisions > 0)
                        {
                            // too much change so insert point in the middle
                            float mid = (samplePoints[i] + samplePoints[i + 1]) * 0.5f;
                            samplePoints.Insert(i + 1, mid);
                            // exit for-loop
                            break;
                        }
                    }

                    // set next as entry point if a sub-division occurs in the next segment
                    first = i + 1;
                }
            }

            //
            // Optimize:
            // Remove unneeded sample points
            //
            for (int i = 0; i < samplePoints.Count - 2; i++)
            {
                // positions
                Vector3 a = node.spline.GetPositionAtTime(samplePoints[i]);
                Vector3 b = node.spline.GetPositionAtTime(samplePoints[i + 1]);
                Vector3 c = node.spline.GetPositionAtTime(samplePoints[i + 2]);

                // radius
                float radiusA = group.GetRadiusAtTime(node, samplePoints[i], true);
                float radiusB = group.GetRadiusAtTime(node, samplePoints[i + 1], true);
                float radiusC = group.GetRadiusAtTime(node, samplePoints[i + 2], true);

                // rotated up vectors
                Vector3 dirAB = (b - a).normalized;
                Vector3 dirAC = (c - a).normalized;

                bool removeMidPoint = false;

                // check directions against each other
                if (Vector3.Dot(dirAB, dirAC) >= thresholdA)
                {
                    removeMidPoint = true;
                }

                if (Mathf.Abs(radiusA - radiusB) > thresholdR) removeMidPoint = false;
                if (Mathf.Abs(radiusB - radiusC) > thresholdR) removeMidPoint = false;

                // remove point inbetween a and b..
                if (removeMidPoint)
                {
                    samplePoints.RemoveAt(i + 1);
                    i--;
                }
            }

            //
            // Smooth capping, requires extra points..
            //
            if (node.capRange > 0.0f)
            {
                int capLoops = 1 + Mathf.CeilToInt(node.capRange * 16.0f * adaptiveQuality);
                for (int i = 0; i < capLoops; i++)
                {
                    float angle = ((float)(i + 1) / (capLoops)) * Mathf.PI * 0.5f;
                    float smooth = Mathf.Sin(angle);
                    float capPoint = capStartPoint + node.capRange * smooth;
                    if (capPoint < node.breakOffset)
                    {
                        samplePoints.Add(capPoint);
                    }
                }

                // make sure they are in the correct order
                samplePoints.Sort();
            }

            //
            // Add end point.. or make sure it is at 1.0, unless spline is broken..
            //
            if (1.0f <= node.breakOffset)
            {
                if (samplePoints[samplePoints.Count - 1] < 1.0f)
                {
                    samplePoints.Add(1.0f);
                }
                else
                {
                    samplePoints[samplePoints.Count - 1] = 1.0f;
                }
            }

            return samplePoints;
        }

        //
        // Generates fast preview
        public void PreviewMesh(Matrix4x4 worldToLocalMatrix, out Material[] outMaterials)
        {
            outMaterials = null;

            if (!mesh)
            {
                Debug.LogError("TreeData must have mesh  assigned");
                return;
            }

            bool origAmbOcc = root.enableAmbientOcclusion;
            float origLOD = root.adaptiveLODQuality;

            root.enableMaterialOptimize = false;
            root.enableWelding = false;
            root.enableAmbientOcclusion = false;
            root.adaptiveLODQuality = 0.0f;

            UpdateMesh(worldToLocalMatrix, out outMaterials);

            // re-enable welding..
            root.enableWelding = true;
            root.enableMaterialOptimize = true;
            root.enableAmbientOcclusion = origAmbOcc;
            root.adaptiveLODQuality = origLOD;

            isInPreviewMode = true;
        }

        public void UpdateMesh(Matrix4x4 worldToLocalMatrix, out Material[] outMaterials)
        {
            outMaterials = null;

            if (!mesh)
            {
                Debug.LogError("TreeData must have mesh  assigned");
                return;
            }

            isInPreviewMode = false;

            // lists to hold materials, vertices and triangles and aospheres
            List<TreeMaterial> materials = new List<TreeMaterial>();
            List<TreeVertex> verts = new List<TreeVertex>();
            List<TreeTriangle> tris = new List<TreeTriangle>();
            List<TreeAOSphere> aoSpheres = new List<TreeAOSphere>();

            // Set build flags..
            int buildFlags = 0;
            if (root.enableAmbientOcclusion)
            {
                buildFlags |= (int)TreeGroup.BuildFlag.BuildAmbientOcclusion;
            }
            if (root.enableWelding)
            {
                buildFlags |= (int)TreeGroup.BuildFlag.BuildWeldParts;
            }

            // Update TreeData...
            UpdateMesh(worldToLocalMatrix, materials, verts, tris, aoSpheres, buildFlags, root.adaptiveLODQuality, root.aoDensity);

            // clear previous mesh data
            if (verts.Count > 65000)
            {
                // Too much to build
                Debug.LogWarning("Tree mesh would exceed maximum vertex limit .. aborting");
                return;
            }
            mesh.Clear();
            if (verts.Count == 0 || tris.Count == 0)
            {
                // Nothing to build
                return;
            }

            // Optimize to use a single material
            OptimizeMaterial(materials, verts, tris);

            Profiler.BeginSample("CopyMeshData");

            // Copy mesh data to static arrays.. surely there must be a faster way to do this
            Vector3[] tmpPos = new Vector3[verts.Count];
            Vector3[] tmpNor = new Vector3[verts.Count];
            Vector2[] tmpUV0 = new Vector2[verts.Count];
            Vector2[] tmpUV1 = new Vector2[verts.Count];
            Vector4[] tmpTan = new Vector4[verts.Count];
            Color[] tmpCol = new Color[verts.Count];

            for (int i = 0; i < verts.Count; i++)
            {
                tmpPos[i] = verts[i].pos;
                tmpNor[i] = verts[i].nor;
                tmpUV0[i] = verts[i].uv0;
                tmpUV1[i] = verts[i].uv1;
                tmpTan[i] = verts[i].tangent;
                tmpCol[i] = verts[i].color;
            }

            // assign vertex properties
            mesh.vertices = tmpPos;
            mesh.normals = tmpNor;
            mesh.uv = tmpUV0;
            mesh.uv2 = tmpUV1;
            mesh.tangents = tmpTan;
            mesh.colors = tmpCol;

            // assign triangles
            int[] tmpTris = new int[tris.Count * 3];

            const int kMaxOptimizedMaterials = 2;
            List<Material> usedOptimizedMaterials = new List<Material>(kMaxOptimizedMaterials);
            mesh.subMeshCount = kMaxOptimizedMaterials;

            for (int mat = 0; mat < kMaxOptimizedMaterials; mat++)
            {
                int triCount = 0;
                for (int i = 0; i < tris.Count; i++)
                {
                    if (tris[i].materialIndex == mat)
                    {
                        tmpTris[triCount + 0] = tris[i].v[0];
                        tmpTris[triCount + 1] = tris[i].v[1];
                        tmpTris[triCount + 2] = tris[i].v[2];
                        triCount += 3;
                    }
                }
                if (triCount > 0)
                {
                    int[] tmpTrisMat = new int[triCount];
                    for (int i = 0; i < triCount; i++)
                    {
                        tmpTrisMat[i] = tmpTris[i];
                    }

                    mesh.SetTriangles(tmpTrisMat, usedOptimizedMaterials.Count);

                    if (mat == 0)
                        usedOptimizedMaterials.Add(optimizedSolidMaterial);
                    else
                        usedOptimizedMaterials.Add(optimizedCutoutMaterial);
                }
            }

            outMaterials = usedOptimizedMaterials.ToArray();
            mesh.subMeshCount = usedOptimizedMaterials.Count;

            Profiler.EndSample(); // CopyMeshData

            // Recompute Bounds
            mesh.RecalculateBounds();
        }

        private static void ExtractOptimizedShaders(List<TreeMaterial> materials, out Shader optimizedSolidShader, out Shader optimizedCutoutShader)
        {
            List<Shader> barkShaders = new List<Shader>();
            List<Shader> leafShaders = new List<Shader>();

            foreach (TreeMaterial treeMaterial in materials)
            {
                Material material = treeMaterial.material;

                if (material && material.shader)
                {
                    if (TreeEditorHelper.IsTreeBarkShader(material.shader))
                        barkShaders.Add(material.shader);
                    else if (TreeEditorHelper.IsTreeLeafShader(material.shader))
                        leafShaders.Add(material.shader);
                }
            }

            optimizedSolidShader = null;
            optimizedCutoutShader = null;

            // Always use Shader.Find() to be able to override the shaders used in
            // the optimized materials, even if the new shaders have the same
            // names as the defaults.
            if (barkShaders.Count > 0)
                optimizedSolidShader = Shader.Find(TreeEditorHelper.GetOptimizedShaderName(barkShaders[0]));

            if (leafShaders.Count > 0)
                optimizedCutoutShader = Shader.Find(TreeEditorHelper.GetOptimizedShaderName(leafShaders[0]));

            // fallback to default shaders
            if (!optimizedSolidShader)
                optimizedSolidShader = TreeEditorHelper.DefaultOptimizedBarkShader;

            if (!optimizedCutoutShader)
                optimizedCutoutShader = TreeEditorHelper.DefaultOptimizedLeafShader;
        }

        public bool OptimizeMaterial(List<TreeMaterial> materials, List<TreeVertex> vertices, List<TreeTriangle> triangles)
        {
            if (!optimizedSolidMaterial || !optimizedCutoutMaterial)
            {
                Debug.LogError("Optimized materials haven't been assigned");
                return false;
            }

            Shader optimizedSolidShader;
            Shader optimizedCutoutShader;
            ExtractOptimizedShaders(materials, out optimizedSolidShader, out optimizedCutoutShader);
            optimizedSolidMaterial.shader = optimizedSolidShader;
            optimizedCutoutMaterial.shader = optimizedCutoutShader;

            //
            // Settings
            //
            int texWidth = 1024;
            int texHeight = 1024;
            int texPadding = 32;

            Profiler.BeginSample("OptimizeMaterial");

            //
            // Compute surface area of each material
            //
            float[] materialArea = new float[materials.Count];

            float totalTileV = 0.0f;
            float totalNonTileV = 0.0f;
            for (int i = 0; i < materials.Count; i++)
            {
                if (!materials[i].tileV)
                {
                    totalNonTileV += 1.0f;
                }
                else
                {
                    totalTileV += 1.0f;
                }
            }

            //totalArea = totalTileV + Mathf.Clamp01(totalNonTileV);

            // Normalize
            for (int i = 0; i < materials.Count; i++)
            {
                if (materials[i].tileV)
                {
                    materialArea[i] = 1.0f;
                }
                else
                {
                    materialArea[i] = 1.0f / totalNonTileV;
                }
                // materialArea[i] /= totalArea;
                // Debug.Log("material " + i + " area = " + materialArea[i] + " tileV = " + materialTileV[i]);
            }

            //
            // Texture atlas, pack it!
            //
            TextureAtlas atlas = new TextureAtlas();
            for (int i = 0; i < materials.Count; i++)
            {
                Texture2D diffuseTex = null;
                Texture2D normalTex = null;
                Texture2D glossTex = null;
                Texture2D translucencyTex = null;
                Texture2D shadowOffsetTex = null;
                Color diffuseColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                float shininess = 0.03f;
                Vector2 uvtiling = new Vector2(1, 1);

                Material m = materials[i].material;

                if (m)
                {
                    if (m.HasProperty("_Color"))
                    {
                        diffuseColor = m.GetColor("_Color");
                    }
                    if (m.HasProperty("_MainTex"))
                    {
                        diffuseTex = (m.mainTexture) as Texture2D;
                        uvtiling = m.GetTextureScale("_MainTex");
                    }
                    if (m.HasProperty("_BumpMap"))
                    {
                        normalTex = (m.GetTexture("_BumpMap")) as Texture2D;
                    }
                    if (m.HasProperty("_GlossMap"))
                    {
                        glossTex = (m.GetTexture("_GlossMap")) as Texture2D;
                    }
                    if (m.HasProperty("_TranslucencyMap"))
                    {
                        translucencyTex = (m.GetTexture("_TranslucencyMap")) as Texture2D;
                    }
                    if (m.HasProperty("_Shininess"))
                    {
                        shininess = m.GetFloat("_Shininess");
                    }
                    if (m.HasProperty("_ShadowOffset"))
                    {
                        shadowOffsetTex = (m.GetTexture("_ShadowOffset")) as Texture2D;
                    }
                }

                shininess = Mathf.Clamp(shininess, 0.03f, 1.0f);

                Vector2 scale = new Vector2(materialArea[i], materialArea[i]);
                // Correct for texture dimensions
                if (diffuseTex)
                {
                    scale.x *= (texWidth / (float)diffuseTex.width);
                    scale.y *= (texHeight / (float)diffuseTex.height);
                }

                bool tile = materials[i].tileV;
                if (!tile)
                {
                    // Ignore texture tiling for non-tiling materials, such as leafs etc.
                    uvtiling = new Vector2(1, 1);
                }
                atlas.AddTexture("tex" + i, diffuseTex, diffuseColor, normalTex, glossTex, translucencyTex, shadowOffsetTex, shininess, scale, tile, uvtiling);
            }
            atlas.Pack(ref texWidth, texHeight, texPadding, true);

            // Update actual textures
            UpdateTextures(atlas, materials);

            // Remap UVs
            Rect uvRect = new Rect();
            Vector2 uvTiling = new Vector2(1, 1);
            int lastMaterialIndex = -1;
            for (int t = 0; t < triangles.Count; t++)
            {
                TreeTriangle triangle = triangles[t];

                // Fetch uv rect if this is a different material from the previous
                if (triangle.materialIndex != lastMaterialIndex)
                {
                    lastMaterialIndex = triangle.materialIndex;
                    uvRect = atlas.GetUVRect("tex" + triangle.materialIndex);
                    uvTiling = atlas.GetTexTiling("tex" + triangle.materialIndex);
                }

                // Remap each uv of the triangle
                for (int v = 0; v < 3; v++)
                {
                    TreeVertex vertex = vertices[triangle.v[v]];
                    if (!vertex.flag)
                    {
                        vertex.uv0.x = uvRect.x + vertex.uv0.x * uvRect.width;
                        vertex.uv0.y = ((uvRect.y + vertex.uv0.y * uvRect.height) * uvTiling.y);
                        vertex.flag = true;
                    }
                }

                // Clean material index
                if (triangle.isCutout)
                {
                    triangle.materialIndex = 1;
                }
                else
                {
                    triangle.materialIndex = 0;
                }
            }

            Profiler.EndSample(); // OptimizeMaterial

            return true;
        }

        static Texture2D[] WriteOptimizedTextures(string treeAssetPath, Texture2D[] textures)
        {
            string[] fileNames = new string[textures.Length];

            string folderPath = Path.Combine(Path.GetDirectoryName(treeAssetPath), Path.GetFileNameWithoutExtension(treeAssetPath) + "_Textures");

            Directory.CreateDirectory(folderPath);

            // Write pngs into asset folder
            for (int i = 0; i < textures.Length; i++)
            {
                byte[] bytes = textures[i].EncodeToPNG();

                fileNames[i] = Path.Combine(folderPath, textures[i].name + ".png");

                File.WriteAllBytes(fileNames[i], bytes);
            }

            // Import them for the first time, using default import settings
            AssetDatabase.Refresh();

            // Modify the import settings for textures
            for (int i = 0; i < textures.Length; i++)
            {
                var textureImporter = AssetImporter.GetAtPath(fileNames[i]) as TextureImporter;
                if (i == 0) // turn on "Alpha Is Transparency" for diffuse
                    textureImporter.alphaIsTransparency = true;
                else // turn off "sRGB" for the other 3 textures
                    textureImporter.sRGBTexture = false;
                AssetDatabase.WriteImportSettingsIfDirty(fileNames[i]);
            }

            // Import for the second time with the new import settings
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string path in fileNames)
                    AssetDatabase.ImportAsset(path);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // Load objects from assets
            for (int i = 0; i < textures.Length; i++)
            {
                textures[i] = AssetDatabase.LoadMainAssetAtPath(fileNames[i]) as Texture2D;
            }

            return textures;
        }

        public bool CheckExternalChanges()
        {
            ValidateReferences();
            return root.CheckExternalChanges();
        }

        private void UpdateShadowTexture(Texture2D shadowTexture, int texWidth, int texHeight)
        {
            if (!shadowTexture)
                return;

            string path = AssetDatabase.GetAssetPath(shadowTexture);
            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            int[] divisors = { 1, 2, 4, 8, 16 };
            int shadowTextureSize = Mathf.Max(8,
                Mathf.ClosestPowerOfTwo(
                    (int)(Mathf.Min(texWidth, texHeight)) / divisors[root.shadowTextureQuality]));

            TextureImporterPlatformSettings platformSettings = textureImporter.GetDefaultPlatformTextureSettings();
            if (shadowTextureSize != platformSettings.maxTextureSize)
            {
                platformSettings.maxTextureSize = shadowTextureSize;
                textureImporter.mipmapEnabled = true;
                textureImporter.SetPlatformTextureSettings(platformSettings);
                AssetDatabase.ImportAsset(path);
            }
        }

        private bool UpdateTextures(TextureAtlas atlas, List<TreeMaterial> materials)
        {
            // early out in preview mode
            if (!root.enableMaterialOptimize)
                return false;

            bool allTexturesAvailable =
                optimizedSolidMaterial.GetTexture("_MainTex") != null &&
                optimizedSolidMaterial.GetTexture("_BumpSpecMap") != null &&
                optimizedSolidMaterial.GetTexture("_TranslucencyMap") != null &&
                optimizedCutoutMaterial.GetTexture("_MainTex") != null &&
                optimizedCutoutMaterial.GetTexture("_ShadowTex") != null &&
                optimizedCutoutMaterial.GetTexture("_BumpSpecMap") != null &&
                optimizedCutoutMaterial.GetTexture("_TranslucencyMap");

            Object[] materialArray = new Object[materials.Count];
            for (int i = 0; i < materials.Count; i++)
            {
                materialArray[i] = materials[i].material;
            }
            string hash = UnityEditorInternal.InternalEditorUtility.CalculateHashForObjectsAndDependencies(materialArray);
            hash += atlas.GetHashCode();

            // early out if the source materials / textures match and all optimized materials are hooked up
            if (materialHash == hash && allTexturesAvailable)
            {
                UpdateShadowTexture(optimizedCutoutMaterial.GetTexture("_ShadowTex") as Texture2D, atlas.atlasWidth, atlas.atlasHeight);
                return false;
            }

            materialHash = hash;

            //
            int texWidth = atlas.atlasWidth;
            int texHeight = atlas.atlasHeight;
            int texPadding = atlas.atlasPadding;

            // create new textures
            Texture2D diffuseTexture = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, true);
            Texture2D shadowTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, true);
            Texture2D normalTexture = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, true);
            Texture2D translucencyTexture = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, true);

            diffuseTexture.name = "diffuse";
            shadowTexture.name = "shadow";
            normalTexture.name = "normal_specular";
            translucencyTexture.name = "translucency_gloss";

            // setup temporary render texure
            SavedRenderTargetState renderTextureState = new SavedRenderTargetState();
            RenderTexture targetTexture = RenderTexture.GetTemporary(texWidth, texHeight, 0, RenderTextureFormat.ARGB32);

            bool oldFog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);

            // setup default colors and textures
            Color defaultDiffuseColor = Color.white;
            Color defaultNormalColor = new Color(0.03f, 0.5f, 0.0f, 0.5f);
            Color realBlack = new Color(0, 0, 0, 0);

            Texture2D defaultDiffuseTex = new Texture2D(1, 1);
            defaultDiffuseTex.SetPixel(0, 0, defaultDiffuseColor);
            defaultDiffuseTex.Apply();

            Texture2D defaultShadowTex = new Texture2D(1, 1, TextureFormat.ARGB32, false, true);
            defaultShadowTex.SetPixel(0, 0, defaultDiffuseColor);
            defaultShadowTex.Apply();

            Texture2D defaultNormalTex = new Texture2D(1, 1, TextureFormat.ARGB32, false, true);
            defaultNormalTex.SetPixel(0, 0, defaultNormalColor);
            defaultNormalTex.Apply();

            Texture2D defaultGlossTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false, true);
            defaultGlossTexture.SetPixel(0, 0, realBlack);
            defaultGlossTexture.Apply();

            Texture2D defaultShadowOffsetTexture = defaultGlossTexture;

            Texture2D defaultTranslucencyTex = new Texture2D(1, 1, TextureFormat.ARGB32, false, true);
            defaultTranslucencyTex.SetPixel(0, 0, Color.white);
            defaultTranslucencyTex.Apply();

            Material textureCombinerMaterial = EditorGUIUtility.LoadRequired("Inspectors/TreeCreator/TreeTextureCombinerMaterial.mat") as Material;
            bool oldSRGBWrite = GL.sRGBWrite;

            // copy textures using atlas coordinates
            for (int blitMode = 0; blitMode < 4; blitMode++)
            {
                RenderTexture.active = targetTexture;
                GL.LoadPixelMatrix(0, texWidth, 0, texHeight);

                textureCombinerMaterial.SetVector("_TexSize", new Vector4(texWidth, texHeight, 0, 0));

                // clear render texture to default colors
                switch (blitMode)
                {
                    case 0:
                        // Normalmap (GA) + Specular (R)
                        GL.sRGBWrite = false;
                        GL.Clear(false, true, defaultNormalColor);
                        break;
                    case 1:
                        // Diffuse (RGB) + Alpha (A)
                        GL.sRGBWrite = true;
                        GL.Clear(false, true, realBlack);
                        break;
                    case 2:
                        // Translucency (RGB) + Gloss (A)
                        GL.sRGBWrite = false;
                        GL.Clear(false, true, realBlack);
                        break;
                    case 3:
                        // Shadow (RGB)
                        GL.sRGBWrite = false;
                        GL.Clear(false, true, realBlack);
                        break;
                }

                // combine textures and colors
                for (int i = 0; i < atlas.nodes.Count; i++)
                {
                    TextureAtlas.TextureNode node = atlas.nodes[i];
                    Rect nodeRect = node.packedRect;

                    Texture rgbtex = null;
                    Texture alphatex = null;
                    Color color = new Color();

                    switch (blitMode)
                    {
                        case 0:
                            rgbtex = node.normalTexture;
                            alphatex = node.shadowOffsetTexture;
                            color = new Color(node.shininess, 0.0f, 0.0f, 0.0f);
                            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                                color.r = Mathf.LinearToGammaSpace(color.r);
                            if (rgbtex == null) rgbtex = defaultNormalTex;
                            if (alphatex == null) alphatex = defaultShadowOffsetTexture;
                            break;
                        case 1:
                            rgbtex = node.diffuseTexture;
                            color = node.diffuseColor;
                            if (rgbtex == null) rgbtex = defaultDiffuseTex;
                            break;
                        case 2:
                            rgbtex = node.translucencyTexture;
                            alphatex = node.glossTexture;
                            if (rgbtex == null) rgbtex = defaultTranslucencyTex;
                            if (alphatex == null) alphatex = defaultGlossTexture;
                            break;
                        case 3:
                            alphatex = node.diffuseTexture;
                            if (alphatex == null) alphatex = defaultDiffuseTex;
                            break;
                    }

                    // edge fill for tiling textures
                    if (node.tileV)
                    {
                        float nodeX = nodeRect.x;
                        float pushX = (texPadding / 2.0f);
                        for (float x = pushX; x > 0.0f; x--)
                        {
                            Rect leftRect = new Rect(nodeRect);
                            Rect rightRect = new Rect(nodeRect);
                            leftRect.x = nodeX - x;
                            rightRect.x = nodeX + x;

                            DrawTexture(leftRect, rgbtex, alphatex, textureCombinerMaterial, color, blitMode);
                            DrawTexture(rightRect, rgbtex, alphatex, textureCombinerMaterial, color, blitMode);
                        }
                    }

                    DrawTexture(nodeRect, rgbtex, alphatex, textureCombinerMaterial, color, blitMode);
                }

                // read screen pixels into textures
                switch (blitMode)
                {
                    case 0:
                        normalTexture.ReadPixels(new Rect(0, 0, texWidth, texHeight), 0, 0);
                        normalTexture.Apply(true);
                        break;
                    case 1:
                        diffuseTexture.ReadPixels(new Rect(0, 0, texWidth, texHeight), 0, 0);
                        diffuseTexture.Apply(true);
                        break;
                    case 2:
                        translucencyTexture.ReadPixels(new Rect(0, 0, texWidth, texHeight), 0, 0);
                        translucencyTexture.Apply(true);
                        break;
                    case 3:
                        shadowTexture.ReadPixels(new Rect(0, 0, texWidth, texHeight), 0, 0);
                        shadowTexture.Apply(true);
                        break;
                }
            }

            GL.sRGBWrite = oldSRGBWrite;
            // flush tempMaterial
            Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
            renderTextureState.Restore();
            optimizedSolidMaterial.SetPass(0);

            // release temporary render target
            RenderTexture.ReleaseTemporary(targetTexture);

            // release temporary textures
            DestroyImmediate(defaultDiffuseTex);
            DestroyImmediate(defaultShadowTex);
            DestroyImmediate(defaultTranslucencyTex);
            DestroyImmediate(defaultGlossTexture);
            DestroyImmediate(defaultNormalTex);

            // write textures to disk
            Texture2D[] textures = new Texture2D[4];
            textures[0] = diffuseTexture;
            textures[1] = normalTexture;
            textures[2] = translucencyTexture;
            textures[3] = shadowTexture;
            textures = WriteOptimizedTextures(AssetDatabase.GetAssetPath(this), textures);

            // destroy the textures, since they have been already written to disk
            DestroyImmediate(diffuseTexture);
            DestroyImmediate(normalTexture);
            DestroyImmediate(translucencyTexture);
            DestroyImmediate(shadowTexture);

            // set texutres on optimized materials
            optimizedSolidMaterial.SetTexture("_MainTex", textures[0]);
            optimizedSolidMaterial.SetTexture("_BumpSpecMap", textures[1]);
            optimizedSolidMaterial.SetTexture("_TranslucencyMap", textures[2]);

            optimizedCutoutMaterial.SetTexture("_MainTex", textures[0]);
            optimizedCutoutMaterial.SetTexture("_BumpSpecMap", textures[1]);
            optimizedCutoutMaterial.SetTexture("_TranslucencyMap", textures[2]);
            optimizedCutoutMaterial.SetTexture("_ShadowTex", textures[3]);

            UpdateShadowTexture(textures[3], atlas.atlasWidth, atlas.atlasHeight);
            return true;
        }

        private void DrawTexture(Rect rect, Texture rgbTexture, Texture alphaTexture, Material material, Color color, int pass)
        {
            material.SetColor("_Color", color);
            material.SetTexture("_RGBSource", rgbTexture);
            material.SetTexture("_AlphaSource", alphaTexture);
            material.SetPass(pass);

            GL.Begin(GL.QUADS);
            GL.TexCoord(new Vector3(0, 0, 0));
            GL.Vertex3(rect.x, rect.y, 0.0f);

            GL.TexCoord(new Vector3(1, 0, 0));
            GL.Vertex3(rect.x + rect.width, rect.y, 0.0f);

            GL.TexCoord(new Vector3(1, 1, 0));
            GL.Vertex3(rect.x + rect.width, rect.y + rect.height, 0.0f);

            GL.TexCoord(new Vector3(0, 1, 0));
            GL.Vertex3(rect.x, rect.y + rect.height, 0.0f);
            GL.End();
        }

        public void UpdateMesh(Matrix4x4 matrix, List<TreeMaterial> materials, List<TreeVertex> verts, List<TreeTriangle> tris, List<TreeAOSphere> aoSpheres, int buildFlags, float adaptiveQuality, float aoDensity)
        {
            // Store original random seed
            var origState = Random.state;

            // set seed for ring loops..
            RingLoop.SetNoiseSeed(root.seed);

            // Clear direct references
            ClearReferences();

            // Validate all references, and fill internal references..
            ValidateReferences();

            // Update Stuff (tm)
            root.UpdateSeed();
            //root.UpdateFrequency(this);
            root.SetRootMatrix(matrix);
            root.UpdateDistribution(false, true);
            root.UpdateParameters();

            // Generate Mesh Data..
            if ((buildFlags & (int)TreeGroup.BuildFlag.BuildAmbientOcclusion) != 0)
            {
                // prep AO
                root.BuildAOSpheres(aoSpheres);
            }
            // gen mesh data
            root.UpdateMesh(materials, verts, tris, aoSpheres, buildFlags, root.adaptiveLODQuality, root.aoDensity);

            // Clear direct references
            ClearReferences();

            // Restore original random seed
            Random.state = origState;
        }

        //
        // Helper function for array stuff.. would be nice to avoid
        //
        private int[] ArrayAdd(int[] array, int value)
        {
            List<int> temp = new List<int>(array);
            temp.Add(value);
            return temp.ToArray();
        }

        private TreeGroup[] ArrayAdd(TreeGroup[] array, TreeGroup value)
        {
            List<TreeGroup> temp = new List<TreeGroup>(array);
            temp.Add(value);
            return temp.ToArray();
        }

        private TreeGroupBranch[] ArrayAdd(TreeGroupBranch[] array, TreeGroupBranch value)
        {
            List<TreeGroupBranch> temp = new List<TreeGroupBranch>(array);
            temp.Add(value);
            return temp.ToArray();
        }

        private TreeGroupLeaf[] ArrayAdd(TreeGroupLeaf[] array, TreeGroupLeaf value)
        {
            List<TreeGroupLeaf> temp = new List<TreeGroupLeaf>(array);
            temp.Add(value);
            return temp.ToArray();
        }

        private TreeNode[] ArrayAdd(TreeNode[] array, TreeNode value)
        {
            List<TreeNode> temp = new List<TreeNode>(array);
            temp.Add(value);
            return temp.ToArray();
        }

        private int[] ArrayRemove(int[] array, int value)
        {
            List<int> temp = new List<int>(array);
            temp.Remove(value);
            return temp.ToArray();
        }

        private TreeGroup[] ArrayRemove(TreeGroup[] array, TreeGroup value)
        {
            List<TreeGroup> temp = new List<TreeGroup>(array);
            temp.Remove(value);
            return temp.ToArray();
        }

        private TreeGroupBranch[] ArrayRemove(TreeGroupBranch[] array, TreeGroupBranch value)
        {
            List<TreeGroupBranch> temp = new List<TreeGroupBranch>(array);
            temp.Remove(value);
            return temp.ToArray();
        }

        private TreeGroupLeaf[] ArrayRemove(TreeGroupLeaf[] array, TreeGroupLeaf value)
        {
            List<TreeGroupLeaf> temp = new List<TreeGroupLeaf>(array);
            temp.Remove(value);
            return temp.ToArray();
        }

        private TreeNode[] ArrayRemove(TreeNode[] array, TreeNode value)
        {
            List<TreeNode> temp = new List<TreeNode>(array);
            temp.Remove(value);
            return temp.ToArray();
        }
    }
}
