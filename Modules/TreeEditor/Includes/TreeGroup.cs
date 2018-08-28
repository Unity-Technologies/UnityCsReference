// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace TreeEditor
{
    [System.Serializable]
    public class TreeGroup
    {
        static readonly protected bool GenerateDoubleSidedGeometry = true;

        // value greater or equal to 1.0f gives normals pointing behind the face
        static readonly protected float GenerateBendNormalFactor = 0.4f;

        static readonly protected float GenerateBendBillboardNormalFactor = 0.8f;

        public enum LockFlag
        {
            LockPosition = 1,
            LockAlignment = 2,
            LockShape = 4
        }

        public enum BuildFlag
        {
            BuildAmbientOcclusion = 1,
            BuildWeldParts = 2
        }

        public enum DistributionMode
        {
            Random = 0,
            Alternate = 1,
            Opposite = 2,
            Whorled = 3
        }

        [SerializeField]
        private int _uniqueID = -1;
        public int uniqueID
        {
            get
            {
                return _uniqueID;
            }
            set
            {
                // only allow setting if it hasn't been initialized
                if (_uniqueID == -1)
                {
                    _uniqueID = value;
                }
            }
        }

        // The seed for this group
        public int seed = 1234;
        [SerializeField]
        private int _internalSeed = 1234;

        [SerializeField]
        internal string m_Hash;

        // Frequency, ie. the number of nodes that can be generated per node in the parent group..
        public int distributionFrequency = 1;

        // Distribution properties, that modify actual distribution (ie. placement + rotation)
        public DistributionMode distributionMode = DistributionMode.Random;
        public AnimationCurve distributionCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        public int distributionNodes = 5;

        // These only modify Rotation / Scaling..
        // Independent of actual distribution, so we don't need to sync distribution completely when changing these..
        public float distributionTwirl = 0.0f;
        public float distributionPitch = 0.0f;
        public AnimationCurve distributionPitchCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        public float distributionScale = 1.0f;
        public AnimationCurve distributionScaleCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.3f));

        // Animation stuff..
        public bool showAnimationProps = true;
        public float animationPrimary = 0.5f;
        public float animationSecondary = 0.5f;
        public float animationEdge = 1.0f;

        public bool visible = true;

        // Hand edited nodes are locked
        public int lockFlags = 0;

        // Child nodes.. indexed, needs TreeData to resolve reference
        public int[] nodeIDs = new int[0];

        // Parent and child groups.. indexed, needs TreeData to resolve reference
        public int parentGroupID = -1;
        public int[] childGroupIDs = new int[0];

        //
        // Should only be accessed internally and from TreeData
        // Never use from outside!
        //
        [System.NonSerialized]
        internal List<TreeNode> nodes = new List<TreeNode>();
        [System.NonSerialized]
        internal TreeGroup parentGroup = null;
        [System.NonSerialized]
        internal List<TreeGroup> childGroups = new List<TreeGroup>();

        //
        // Required for branches
        //
        virtual public float GetRadiusAtTime(TreeNode node, float t, bool includeModifications)
        {
            return 0.0f;
        }

        //
        // Whether this group allows sub-groups
        //
        virtual public bool CanHaveSubGroups()
        {
            return true;
        }

        //
        // Locks a group for hand-editing..
        //
        public void Lock()
        {
            if (lockFlags == 0)
            {
                //
                // Copy corrected angle to base angle..
                //
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].baseAngle = nodes[i].angle;
                }
            }

            lockFlags = (int)LockFlag.LockShape | (int)LockFlag.LockPosition | (int)LockFlag.LockAlignment;
        }

        public void Unlock()
        {
            lockFlags = 0;
        }

        internal virtual bool HasExternalChanges()
        {
            return false;
        }

        public bool CheckExternalChanges()
        {
            bool hasExternalChanges = HasExternalChanges();

            for (int i = 0; i < childGroups.Count; i++)
            {
                hasExternalChanges |= childGroups[i].CheckExternalChanges();
            }

            return hasExternalChanges;
        }

        //
        // Controls generation of nodes based on frequency
        //
        public void UpdateFrequency(TreeData owner)
        {
            Profiler.BeginSample("UpdateFrequency");

            // Must have at least 1
            if (distributionFrequency < 1)
            {
                distributionFrequency = 1;
            }

            if (parentGroup == null)
            {
                // Only root can have no parent..

                // Must have 1 as distribution frequency
                distributionFrequency = 1;
                if (nodes.Count < 1)
                {
                    owner.AddNode(this, null, false);
                }
            }
            else if ((lockFlags == 0) && (parentGroup != null))
            {
                // Exact count depends on parent
                int newTotalCount = 0;
                for (int n = 0; n < parentGroup.nodes.Count; n++)
                {
                    int tempFrequency = Mathf.RoundToInt(distributionFrequency * parentGroup.nodes[n].GetScale());

                    // make sure there is at least one node..
                    if (tempFrequency < 1)
                    {
                        tempFrequency = 1;
                    }
                    for (int i = 0; i < tempFrequency; i++)
                    {
                        if (newTotalCount < nodes.Count)
                        {
                            owner.SetNodeParent(nodes[newTotalCount], parentGroup.nodes[n]);
                        }
                        else
                        {
                            owner.AddNode(this, parentGroup.nodes[n], false);
                        }

                        newTotalCount++;
                    }
                }

                // remove excess nodes
                if (newTotalCount < nodes.Count)
                {
                    List<TreeNode> killNodes = new List<TreeNode>();
                    for (int i = newTotalCount; i < nodes.Count; i++)
                    {
                        killNodes.Add(nodes[i]);
                    }
                    for (int i = 0; i < killNodes.Count; i++)
                    {
                        owner.DeleteNode(killNodes[i], false);
                    }
                    //owner.ValidateReferences();
                }

                UpdateSeed();
                UpdateDistribution(true, false);
            }

            // Update child groups..
            for (int i = 0; i < childGroups.Count; i++)
            {
                childGroups[i].UpdateFrequency(owner);
            }

            Profiler.EndSample(); // UpdateFrequency
        }

        //
        // Updates the seed
        //
        public void UpdateSeed()
        {
            // Fetch root seed
            TreeGroup gg = this;
            while (gg.parentGroup != null)
            {
                gg = gg.parentGroup;
            }
            int rootSeed = gg.seed;


            _internalSeed = rootSeed + (int)(seed * 1.21f);

            // update kids
            for (int i = 0; i < nodes.Count; i++)
            {
                // magic formula!
                nodes[i].seed = rootSeed + _internalSeed + (int)(i * 3.7482f);
            }

            // update sub-groups
            for (int i = 0; i < childGroups.Count; i++)
            {
                childGroups[i].UpdateSeed();
            }
        }

        public Vector2 ComputeWindFactor(TreeNode node, float offset)
        {
            Vector2 factor;
            if (node.group.parentGroup.GetType() == typeof(TreeGroupRoot))
            {
                factor = new Vector2(0, 0);
            }
            else
            {
                factor = node.parent.group.ComputeWindFactor(node.parent, node.offset);
            }

            float scale = node.GetScale();

            // primary (cubic falloff)
            factor.x += ((offset * offset * offset) * scale * animationPrimary);

            // secondary (square falloff)
            factor.y += ((offset * offset) * scale * animationSecondary);

            return factor;
        }

        private float ComputeOffset(int index, int count, float distributionSum, float distributionStep)
        {
            // sample along distribution curve, until we reach a valid position..
            float current = 0.0f;
            float target = ((index + 1.0f) / (count + 1.0f)) * distributionSum;
            for (float j = 0.0f; j <= 1.0f; j += distributionStep)
            {
                current += Mathf.Clamp01(distributionCurve.Evaluate(j));
                if (current >= target)
                {
                    return j;
                }
            }

            // failed to find a good location
            return (target / distributionSum);
        }

        public float GetRootSpread()
        {
            TreeGroup root = this;
            while (root.parentGroup != null)
            {
                root = root.parentGroup;
            }
            return root.nodes[0].size;
        }

        public Matrix4x4 GetRootMatrix()
        {
            TreeGroup root = this;
            while (root.parentGroup != null)
            {
                root = root.parentGroup;
            }
            return root.nodes[0].matrix;
        }

        //
        // Updates distribution..
        //
        public void UpdateDistribution(bool completeUpdate, bool updateSubGroups)
        {
            Profiler.BeginSample("UpdateDistribution");

            // Seed
            Random.InitState(_internalSeed);

            // Distribution..
            if (completeUpdate)
            {
                // needed for sampling distribution curve in non-random modes
                float distributionSum = 0.0f;
                float[] distributionSamples = new float[100];
                float distributionStep = 1.0f / distributionSamples.Length;

                float distributionDivisor = (float)distributionSamples.Length - 1;

                // sample distribution curve at X locations and store values in distributionSamples
                // store sum of samples in distributionSum
                for (int i = 0; i < distributionSamples.Length; i++)
                {
                    float j = i / distributionDivisor;
                    distributionSamples[i] = Mathf.Clamp01(distributionCurve.Evaluate(j));
                    distributionSum += distributionSamples[i];
                }


                for (int i = 0; i < nodes.Count; i++)
                {
                    TreeNode node = nodes[i];

                    if (lockFlags == 0)
                    {
                        if ((i == 0) && (nodes.Count == 1) &&
                            ((parentGroup == null || parentGroup.GetType() == typeof(TreeGroupRoot))))
                        {
                            // first child of the root is always centered..
                            node.offset = 0.0f;
                            node.baseAngle = 0.0f;
                            node.pitch = 0.0f;
                            node.scale = Mathf.Clamp01(distributionScaleCurve.Evaluate(node.offset)) * distributionScale +
                                (1.0f - distributionScale);
                        }
                        else
                        {
                            // Sum up number of nodes attached to the same parent as this node
                            // and find the index of this node in relation to it's parent
                            int nodeLocalIndex = 0;
                            int nodeLocalCount = 0;

                            for (int j = 0; j < nodes.Count; j++)
                            {
                                if (nodes[j].parentID == node.parentID)
                                {
                                    if (i == j) nodeLocalIndex = nodeLocalCount;
                                    nodeLocalCount++;
                                }
                            }

                            switch (distributionMode)
                            {
                                case DistributionMode.Random:
                                {
                                    // create a random value between 0 and distributionSum
                                    float offset = 0.0f;
                                    float weight = 0.0f;

                                    // hack, since the random values seem to be clustered - investigate!
                                    for (int j = 0; j < 5; j++)
                                    {
                                        weight = Random.value * distributionSum;
                                    }

                                    // go through distributionSamples and subtract each from weight until
                                    // weight <= 0, which means we've hit a good spot
                                    for (int j = 0; j < distributionSamples.Length; j++)
                                    {
                                        offset = j / distributionDivisor;
                                        weight -= distributionSamples[j];
                                        if (weight <= 0.0f) break;
                                    }

                                    // set the offset to the last sampled point
                                    node.baseAngle = Random.value * 360.0f;
                                    node.offset = offset;
                                }
                                break;

                                case DistributionMode.Alternate:
                                {
                                    // sample along distribution curve until we reach the next 'good' offset
                                    float offset = ComputeOffset(nodeLocalIndex, nodeLocalCount, distributionSum, distributionStep);
                                    float angle = 180.0f * nodeLocalIndex;
                                    node.baseAngle = angle + (offset * distributionTwirl * 360.0f);
                                    node.offset = offset;
                                }
                                break;

                                case DistributionMode.Opposite:
                                {
                                    //
                                    // sample along distribution curve until we reach the next 'good' offset
                                    float offset = ComputeOffset(nodeLocalIndex / 2, nodeLocalCount / 2, distributionSum, distributionStep);
                                    float angle = 90.0f * (nodeLocalIndex / 2) + ((nodeLocalIndex % 2) * 180.0f);
                                    node.baseAngle = angle + (offset * distributionTwirl * 360.0f);
                                    node.offset = offset;
                                }
                                break;

                                case DistributionMode.Whorled:
                                {
                                    int clusterCount = distributionNodes;
                                    int clusterIndex = nodeLocalIndex % clusterCount;
                                    int clusterOffset = nodeLocalIndex / clusterCount;
                                    float offset = ComputeOffset(nodeLocalIndex / clusterCount, nodeLocalCount / clusterCount,
                                        distributionSum,
                                        distributionStep);
                                    float angle = ((360.0f / clusterCount) * clusterIndex) +
                                        ((180.0f / clusterCount) * clusterOffset);

                                    node.baseAngle = angle + (offset * distributionTwirl * 360.0f);
                                    node.offset = offset;
                                }
                                break;
                            }
                        }
                    }
                }
            }

            //
            // Distribution properties that do not change position
            for (int n = 0; n < nodes.Count; n++)
            {
                TreeNode node = nodes[n];

                // Update individual node visibility, which has nothing to do with group visibility..
                if (node.parent == null)
                {
                    node.visible = true;
                }
                else
                {
                    node.visible = node.parent.visible;
                    if (node.offset > node.parent.breakOffset)
                    {
                        node.visible = false;
                    }
                }

                if (lockFlags == 0)
                {
                    node.angle = node.baseAngle;
                    node.pitch = Mathf.Clamp(distributionPitchCurve.Evaluate(node.offset), -1.0f, 1.0f) * -75.0f * distributionPitch;
                }
                else
                {
                    node.angle = node.baseAngle;
                }
                node.scale = Mathf.Clamp01(distributionScaleCurve.Evaluate(node.offset)) * distributionScale + (1.0f - distributionScale);
            }

            // Adjust animation props, as they rely on distribution
            if ((parentGroup == null) || (parentGroup.GetType() == typeof(TreeGroupRoot)))
            {
                // no parent .. or attached to the root
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].animSeed = 0.0f;
                }
            }
            else
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].parent == null)
                    {
                        nodes[i].animSeed = 0.0f;
                    }
                    else
                    {
                        if (nodes[i].parent.animSeed == 0.0f)
                        {
                            // new seed
                            nodes[i].animSeed = ((((float)nodes[i].seed) / 9.78f) % 1) + 0.001f;
                        }
                        else
                        {
                            // copy seed
                            nodes[i].animSeed = nodes[i].parent.animSeed;
                        }
                    }
                }
            }


            if (updateSubGroups)
            {
                // update sub-groups
                for (int i = 0; i < childGroups.Count; i++)
                {
                    childGroups[i].UpdateDistribution(completeUpdate, updateSubGroups);
                }
            }

            Profiler.EndSample(); // UpdateDistribution
        }

        virtual public void UpdateParameters()
        {
            // update sub-groups
            for (int i = 0; i < childGroups.Count; i++)
            {
                childGroups[i].UpdateParameters();
            }
        }

        virtual public void BuildAOSpheres(List<TreeAOSphere> aoSpheres)
        {
            Profiler.BeginSample("BuildAOSpheres");

            // update sub-groups
            for (int i = 0; i < childGroups.Count; i++)
            {
                childGroups[i].BuildAOSpheres(aoSpheres);
            }

            Profiler.EndSample(); // BuildAOSpheres
        }

        virtual public void UpdateMesh(List<TreeMaterial> materials, List<TreeVertex> verts, List<TreeTriangle> tris, List<TreeAOSphere> aoSpheres, int buildFlags, float adaptiveQuality, float aoDensity)
        {
            // update sub-groups
            for (int i = 0; i < childGroups.Count; i++)
            {
                childGroups[i].UpdateMesh(materials, verts, tris, aoSpheres, buildFlags, adaptiveQuality, aoDensity);
            }
        }

        virtual public void UpdateMatrix()
        {
            /*
            // update sub-groups
            for (int i = 0; i < subGroups.Length; i++)
            {
                subGroups[i].UpdateMatrix();
            }
             * */
        }

        //
        // Used for mapping materials..
        //
        protected static int GetMaterialIndex(Material m, List<TreeMaterial> materials, bool tileV)
        {
            for (int i = 0; i < materials.Count; i++)
            {
                if (materials[i].material == m)
                {
                    // Tiling overrides non-tiling
                    materials[i].tileV |= tileV;
                    return i;
                }
            }

            // not in list yet, add it..
            TreeMaterial tm = new TreeMaterial();
            tm.material = m;
            tm.tileV = tileV;

            materials.Add(tm);
            return (materials.Count - 1);
        }

        protected static Vector4 CreateTangent(TreeNode node, Quaternion rot, Vector3 normal)
        {
            Vector3 tangent = node.matrix.MultiplyVector(rot * new Vector3(1, 0, 0));
            tangent -= normal * Vector3.Dot(tangent, normal);
            tangent.Normalize();
            return new Vector4(tangent.x, tangent.y, tangent.z, 1);
        }

        //
        // Used for calculating ambient occlusion
        //
        protected static float ComputeAmbientOcclusion(Vector3 pos, Vector3 nor, List<TreeAOSphere> aoSpheres, float aoDensity)
        {
            if (aoSpheres.Count == 0) return 1.0f;

            // simplified model
            float total = 0.0f;
            for (int i = 0; i < aoSpheres.Count; i++)
            {
                total += aoSpheres[i].PointOcclusion(pos, nor) * aoSpheres[i].density * 0.25f;
                //if (total >= 1.0f) return 0.0f;
            }
            return 1.0f - Mathf.Clamp01(total) * aoDensity;

            /*
            // raytracing model
            float total = 0.0f;
            float t = 3.0f;
            float mint = 3.0f;
            Vector3 hit = Vector3.zero;
            Ray ray = new Ray(pos+nor*0.1f,nor);

            float range = 10.0f;
            int samples = 8;

            // cull
            for (int i = 0; i < aoSpheres.Count; i++)
            {
                Vector3 delta = ray.origin - aoSpheres[i].position;
                aoSpheres[i].flag = (delta.sqrMagnitude < ((aoSpheres[i].radius + range)*(aoSpheres[i].radius + range)));
            }

            // trace
            for (int s = 0; s < samples; s++)
            {
                Vector3 direction = Random.onUnitSphere;
                if (Vector3.Dot(direction,nor) < 0.0f)
                {
                    direction *= -1.0f;
                }
                ray.direction = direction;

                t = range;
                mint = range;
                for (int i = 0; i < aoSpheres.Count; i++)
                {
                    if (aoSpheres[i].flag)
                    {
                        if (MathUtils.IntersectRaySphere(ray, aoSpheres[i].position, aoSpheres[i].radius, ref t, ref hit))
                        {
                            // weight according to density..
                            t = Mathf.Lerp(range, t, aoSpheres[i].density);
                            mint = Mathf.Min( t, mint);
                        }
                    }
                }
                total += (mint/range)/samples;
            }
            return total;
            */
        }

        internal virtual string GroupSeedString { get { return null; } }
        internal virtual string FrequencyString { get { return null; } }
        internal virtual string DistributionModeString { get { return null; } }
        internal virtual string TwirlString { get { return null; } }
        internal virtual string WhorledStepString { get { return null; } }
        internal virtual string GrowthScaleString { get { return null; } }
        internal virtual string GrowthAngleString { get { return null; } }

        internal virtual string MainWindString { get { return null; } }
        internal virtual string MainTurbulenceString { get { return null; } }
        internal virtual string EdgeTurbulenceString { get { return null; } }
    }
}
