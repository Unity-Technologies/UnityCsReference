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
    // Branch group
    //
    [System.Serializable]
    public class TreeGroupBranch : TreeGroup
    {
        private static float spreadMul = 5.0f;

        public enum GeometryMode
        {
            Branch = 0,
            BranchFrond = 1,
            Frond = 2
        }

        static class Styles
        {
            public static string groupSeedString = LocalizationDatabase.GetLocalizedString("Group Seed|The seed for this group of branches. Modify to vary procedural generation.");
            public static string frequencyString = LocalizationDatabase.GetLocalizedString("Frequency|Adjusts the number of branches created for each parent branch.");
            public static string distributionModeString = LocalizationDatabase.GetLocalizedString("Distribution|The way the branches are distributed along their parent.");
            public static string twirlString = LocalizationDatabase.GetLocalizedString("Twirl|Twirl around the parent branch.");
            public static string whorledStepString = LocalizationDatabase.GetLocalizedString("Whorled Step|Defines how many nodes are in each whorled step when using Whorled distribution. For real plants this is normally a Fibonacci number.");
            public static string growthScaleString = LocalizationDatabase.GetLocalizedString("Growth Scale|Defines the scale of nodes along the parent node. Use the curve to adjust and the slider to fade the effect in and out.");
            public static string growthAngleString = LocalizationDatabase.GetLocalizedString("Growth Angle|Defines the initial angle of growth relative to the parent. Use the curve to adjust and the slider to fade the effect in and out.");
            public static string mainWindString = LocalizationDatabase.GetLocalizedString("Main Wind|Primary wind effect. This creates a soft swaying motion and is typically the only parameter needed for primary branches.");
            public static string mainTurbulenceString = LocalizationDatabase.GetLocalizedString("Main Turbulence|Secondary turbulence effect. Produces more stochastic motion, which is individual per branch. Typically used for branches with fronds, such as ferns and palms.");
            public static string edgeTurbulenceString = LocalizationDatabase.GetLocalizedString("Edge Turbulence|Turbulence along the edge of fronds. Useful for ferns, palms, etc.");
        }

        // members
        //[field: TreeAttribute("Geometry", "category", 0.0f, 1.0f)]
        //public bool showGeometryProps = true;

        //[field: TreeAttribute("LODQuality", "slider", 0.0f, 2.0f)]
        public float lodQualityMultiplier = 1.0f;

        //[field: TreeAttribute("GeometryMode", "popup", "Branch Only,Branch + Fronds,Fronds Only")]
        public GeometryMode geometryMode = GeometryMode.Branch;

        // Materials
        //[field: TreeAttribute("BranchMaterial", "material", 0.0f, 1.0f)]
        public Material materialBranch = null;

        //[field: TreeAttribute("BreakMaterial", "material", 0.0f, 1.0f)]
        public Material materialBreak = null;

        //[field: TreeAttribute("FrondMaterial", "material", 0.0f, 1.0f)]
        public Material materialFrond = null;

        //[field: TreeAttribute("Shape", "category", 0.0f, 1.0f)]
        //public bool showShapeProps = true;

        //[field: TreeAttribute("Length", "minmaxslider", 0.1f, 50.0f,"notLocked")]
        public Vector2 height = new Vector2(10.0f, 15.0f);

        //[field: TreeAttribute("Radius", "slider", 0.1f, 5.0f, "radiusCurve", 0.0f, 1.0f, "branchGeometry")]
        public float radius = 0.5f;
        public AnimationCurve radiusCurve = new AnimationCurve(new Keyframe(0, 1, -1, -1), new Keyframe(1, 0, -1, -1));

        //[field: TreeAttribute("IsLengthRelative", "toggle", 0.0f, 1.0f, "branchGeometry")]
        public bool radiusMode = true;

        //[field: TreeAttribute("CapSmoothing", "slider", 0.0f, 1.0f, "branchGeometry")]
        public float capSmoothing = 0.0f;

        // Shape stuff
        //[field: TreeAttribute("Growth", "heading", "all")]
        //public bool showGrowthProps = true;

        //[field: TreeAttribute("Crinklyness", "slider", 0.0f, 1.0f, "crinkCurve", 0.0f, 1.0f, "notLocked")]
        public float crinklyness = 0.1f;
        public AnimationCurve crinkCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f));

        //[field: TreeAttribute("SeekSunGround", "slider", 0.0f, 1.0f, "seekCurve", -1.0f, 1.0f, "notLocked")]
        public float seekBlend = 0.0f;
        public AnimationCurve seekCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f));

        // Surface/vertex noise stuff
        //[field: TreeAttribute("SurfaceNoise", "heading", "all")]
        //public bool showNoiseProps = true;

        //[field: TreeAttribute("Noise", "slider", 0.0f, 1.0f, "noiseCurve", 0.0f, 1.0f, "branchGeometry")]
        public float noise = 0.1f;
        public AnimationCurve noiseCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f));

        //[field: TreeAttribute("NoiseScaleU", "slider", 0.0f, 1.0f, "branchGeometry")]
        public float noiseScaleU = 0.2f;

        //[field: TreeAttribute("NoiseScaleV", "slider", 0.0f, 1.0f, "branchGeometry")]
        public float noiseScaleV = 0.1f;

        // Flares.. Only for root branches/trunks
        //[field: TreeAttribute("Flare", "heading", "root")]
        //public bool showFlareProps = true;

        //[field: TreeAttribute("FlareRadius", "slider", 0.0f, 5.0f, "branchGeometry")]
        public float flareSize = 0.0f;

        //[field: TreeAttribute("FlareHeight", "slider", 0.01f, 1.0f, "branchGeometry")]
        public float flareHeight = 0.1f;

        //[field: TreeAttribute("FlareNoise", "slider", 0.0f, 1.0f, "branchGeometry")]
        public float flareNoise = 0.3f;

        // Welding stuff.. Only for branches that are attached to another branch..
        //[field: TreeAttribute("Welding", "heading", "notRoot")]
        //public bool showWeldProps = true;

        //[field: TreeAttribute("WeldHeight", "slider", 0.01f, 1.0f, "branchGeometry")]
        public float weldHeight = 0.1f;

        //[field: TreeAttribute("WeldSpreadTop", "slider", 0.0f, 1.0f, "branchGeometry")]
        public float weldSpreadTop = 0.0f;

        //[field: TreeAttribute("WeldSpreadBottom", "slider", 0.0f, 1.0f, "branchGeometry")]
        public float weldSpreadBottom = 0.0f;


        // Breaking stuff..
        //[field: TreeAttribute("Breaking", "category", 0.0f, 1.0f)]
        //public bool showBreakProps = true;

        //[field: TreeAttribute("BreakChance", "slider", 0.0f, 1.0f)]
        public float breakingChance = 0.0f;

        //[field: TreeAttribute("BreakLocation", "minmaxslider", 0.0f, 1.0f)]
        public Vector2 breakingSpot = new Vector2(0.4f, 0.6f);

        // Frond stuff..
        //[field: TreeAttribute("Fronds", "category", 0.0f, 1.0f)]
        //public bool showFrondProps = true;

        //[field: TreeAttribute("FrondCount", "intslider", 1.0f, 16.0f)]
        public int frondCount = 1;

        //[field: TreeAttribute("FrondWidth", "slider", 0.1f, 10.0f, "frondCurve", 0.0f, 1.0f)]
        public float frondWidth = 1.0f;
        public AnimationCurve frondCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f));

        //[field: TreeAttribute("FrondRange", "minmaxslider", 0.0f, 1.0f)]
        public Vector2 frondRange = new Vector2(0.1f, 1.0f);

        //[field: TreeAttribute("FrondRotation", "slider", 0.0f, 1.0f)]
        public float frondRotation = 0.0f;

        //[field: TreeAttribute("FrondCrease", "slider", -1.0f, 1.0f)]
        public float frondCrease = 0.0f;

        override internal bool HasExternalChanges()
        {
            string hash;
            List<Object> objects = new List<Object>();

            if (geometryMode == GeometryMode.Branch)
            {
                objects.Add(materialBranch);
                if (breakingChance > 0)
                    objects.Add(materialBreak);
            }
            else if (geometryMode == GeometryMode.BranchFrond)
            {
                objects.Add(materialBranch);
                objects.Add(materialFrond);
                if (breakingChance > 0)
                    objects.Add(materialBreak);
            }
            else if (geometryMode == GeometryMode.Frond)
            {
                objects.Add(materialFrond);
            }

            hash = UnityEditorInternal.InternalEditorUtility.CalculateHashForObjectsAndDependencies(objects.ToArray());

            if (hash != m_Hash)
            {
                m_Hash = hash;
                return true;
            }

            return false;
        }

        private Vector3 GetFlareWeldAtTime(TreeNode node, float time)
        {
            float scale = node.GetScale();

            float flareFactor = 0.0f;
            float spreadFactor = 0.0f;

            // flares
            if (flareHeight > 0.001f)
            {
                float flareDist = ((1.0f - time) - (1.0f - flareHeight)) / flareHeight;
                flareFactor = Mathf.Pow(Mathf.Clamp01(flareDist), 1.5f) * scale;
            }

            // weld spread top/bottom
            if (weldHeight > 0.001f)
            {
                float spreadDist = ((1.0f - time) - (1.0f - weldHeight)) / weldHeight;
                spreadFactor = Mathf.Pow(Mathf.Clamp01(spreadDist), 1.5f) * scale;
            }

            return new Vector3(flareFactor * flareSize, spreadFactor * weldSpreadTop * spreadMul, spreadFactor * weldSpreadBottom * spreadMul);
        }

        override public float GetRadiusAtTime(TreeNode node, float time, bool includeModifications)
        {
            // no radius when only fronds are displayed..
            if (geometryMode == GeometryMode.Frond) return 0.0f;

            float mainRadius = Mathf.Clamp01(radiusCurve.Evaluate(time)) * node.size;

            // Smooth capping
            float capStart = 1.0f - node.capRange;
            if (time > capStart)
            {
                // within cap area
                float angle = Mathf.Acos(Mathf.Clamp01((time - capStart) / node.capRange));
                float roundScale = Mathf.Sin(angle);
                mainRadius *= roundScale;
            }

            // Flare / welding
            if (includeModifications)
            {
                Vector3 flareWeldRad = GetFlareWeldAtTime(node, time);
                mainRadius += Mathf.Max(flareWeldRad.x, Mathf.Max(flareWeldRad.y, flareWeldRad.z) * 0.25f) * 0.1f;
            }

            return mainRadius;
        }

        override public void UpdateParameters()
        {
            //
            // Reset un-needed values
            //
            if ((parentGroup == null) || (parentGroup.GetType() == typeof(TreeGroupRoot)))
            {
                // no spreading for root branches
                weldSpreadTop = 0.0f;
                weldSpreadBottom = 0.0f;

                // no secondary motion for root branches
                animationSecondary = 0.0f;
            }
            else
            {
                // no flares for sub-branches
                flareSize = 0.0f;
            }

            float heightVariation = (height.y - height.x) / height.y;
            for (int n = 0; n < nodes.Count; n++)
            {
                TreeNode node = nodes[n];

                Random.InitState(node.seed);

                // Size variation
                float variationScale = 1.0f;
                for (int x = 0; x < 5; x++)
                {
                    variationScale = 1.0f - (Random.value * heightVariation);
                }

                if (lockFlags == 0)
                {
                    node.scale *= variationScale;
                }

                // breaking..
                float bks = 0.0f;
                for (int x = 0; x < 5; x++)
                {
                    bks = Random.value;
                }
                for (int x = 0; x < 5; x++)
                {
                    // outside range
                    node.breakOffset = 1.0f;

                    // will it break .. will it break..
                    if ((Random.value <= breakingChance) && (breakingChance > 0.0f))
                    {
                        node.breakOffset = breakingSpot.x + ((breakingSpot.y - breakingSpot.x) * bks);
                        if (node.breakOffset < 0.01f)
                        {
                            node.breakOffset = 0.01f;
                        }
                    }
                }

                // compute radius of this node
                if (!(parentGroup is TreeGroupRoot))
                {
                    node.size = radius;
                    if (radiusMode)
                    {
                        // scale according to length
                        node.size *= node.scale;
                    }

                    if ((node.parent != null) && (node.parent.spline != null))
                    {
                        // Clamp to parent
                        node.size = Mathf.Min(node.parent.group.GetRadiusAtTime(node.parent, node.offset, false) * 0.75f,
                            node.size);
                    }
                }
                else if (lockFlags == 0)
                {
                    // special case for root, we don't want the radius to change if it's moved around by hand
                    node.size = radius;
                    if (radiusMode)
                    {
                        // scale according to length
                        node.size *= node.scale;
                    }
                }
                /*
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].triStart = 0;
                    nodes[i].triEnd = 0;
                }
                */
                /*
                // Generate the spline according to the settings
                UpdateSpline( node );

                // Compute smoothing range for cap
                if (capSmoothing < 0.01f)
                {
                    node.capRange = 0.0f;
                }
                else
                {
                    float capRadius = Mathf.Clamp(radiusCurve.Evaluate(1.0f), 0.0f, 1.0f) * node.size;
                    float totalHeight = height * node.GetScale();
                    node.capRange = (capRadius / totalHeight) * capSmoothing * 2.0f;
                }
                */
            }

            // Matrix must be updated before spline generation
            UpdateMatrix();

            // Spline generation
            UpdateSplines();

            // Update child groups..
            base.UpdateParameters();
        }

        public void UpdateSplines()
        {
            for (int n = 0; n < nodes.Count; n++)
            {
                TreeNode node = nodes[n];

                // Generate the spline according to the settings
                UpdateSpline(node);

                // Compute smoothing range for cap
                if (capSmoothing < 0.01f)
                {
                    node.capRange = 0.0f;
                }
                else
                {
                    float capRadius = Mathf.Clamp(radiusCurve.Evaluate(1.0f), 0.0f, 1.0f) * node.size;
                    float totalHeight = Mathf.Max(node.spline.GetApproximateLength(), 0.00001f);
                    node.capRange = (capRadius / totalHeight) * capSmoothing * 2.0f;
                }
            }
        }

        override public void UpdateMatrix()
        {
            if (parentGroup == null)
            {
                // root node, use rootSpread parameter
                for (int i = 0; i < nodes.Count; i++)
                {
                    TreeNode node = nodes[i];
                    /*
                    float dist = node.offset * rootSpread;
                    float ang = node.angle * Mathf.Deg2Rad;
                    Vector3 pos = new Vector3(Mathf.Cos(ang) * dist, 0.0f, Mathf.Sin(ang) * dist);
                    Quaternion rot = Quaternion.Euler(node.pitch*-Mathf.Sin(ang), 0, node.pitch * Mathf.Cos(ang)) * Quaternion.Euler(0, node.angle, 0);
                    node.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
                    */
                    node.matrix = Matrix4x4.identity;
                }
            }
            else if (parentGroup is TreeGroupRoot)
            {
                TreeGroupRoot tgr = parentGroup as TreeGroupRoot;

                // Attached to root node
                for (int i = 0; i < nodes.Count; i++)
                {
                    TreeNode node = nodes[i];
                    float dist = node.offset * GetRootSpread();
                    float ang = node.angle * Mathf.Deg2Rad;
                    Vector3 pos = new Vector3(Mathf.Cos(ang) * dist, -tgr.groundOffset, Mathf.Sin(ang) * dist);
                    Quaternion rot = Quaternion.Euler(node.pitch * -Mathf.Sin(ang), 0, node.pitch * Mathf.Cos(ang)) * Quaternion.Euler(0, node.angle, 0);
                    node.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
                }
            }
            else
            {
                // has a parent so, use that..
                for (int i = 0; i < nodes.Count; i++)
                {
                    TreeNode node = nodes[i];

                    Vector3 pos = new Vector3();
                    Quaternion rot = new Quaternion();
                    float rad = 0.0f;

                    node.parent.GetPropertiesAtTime(node.offset, out pos, out rot, out rad);

                    // x=90 makes sure start growth is perpendicular..
                    Quaternion angle = Quaternion.Euler(90.0f, node.angle, 0);
                    Matrix4x4 aog = Matrix4x4.TRS(Vector3.zero, angle, Vector3.one);

                    // pitch matrix
                    Matrix4x4 pit = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(node.pitch, 0.0f, 0.0f), Vector3.one);

                    node.matrix = node.parent.matrix * Matrix4x4.TRS(pos, rot, Vector3.one) * aog * pit;

                    Vector3 off = node.matrix.MultiplyPoint(new Vector3(0, rad, 0));
                    node.matrix.m03 = off.x;
                    node.matrix.m13 = off.y;
                    node.matrix.m23 = off.z;
                }
            }

            // Update child groups..
            //base.UpdateMatrix();
        }

        override public void BuildAOSpheres(List<TreeAOSphere> aoSpheres)
        {
            float minStepSize = 0.5f;

            if (visible)
            {
                bool includeFronds = (geometryMode != GeometryMode.Branch);
                bool includeBranch = (geometryMode != GeometryMode.Frond);

                for (int n = 0; n < nodes.Count; n++)
                {
                    TreeNode node = nodes[n];

                    if (!node.visible) continue;

                    float scale = node.GetScale();
                    float totalHeight = node.spline.GetApproximateLength();

                    if (totalHeight < minStepSize) totalHeight = minStepSize;

                    float minStep = minStepSize / totalHeight;

                    float t = 0.0f;
                    while (t < 1.0f)
                    {
                        Vector3 pos = node.matrix.MultiplyPoint(node.spline.GetPositionAtTime(t));

                        float rad = 0.0f;

                        if (includeBranch)
                        {
                            rad = GetRadiusAtTime(node, t, false) * 0.95f;
                        }
                        if (includeFronds)
                        {
                            rad = Mathf.Max(rad, 0.95f * (Mathf.Clamp01(frondCurve.Evaluate(t)) * frondWidth * scale));

                            // push position down..
                            pos.y -= rad;
                        }

                        if (rad > 0.01f)
                        {
                            aoSpheres.Add(new TreeAOSphere(pos, rad, 1.0f));
                        }

                        t += Mathf.Max(minStep, rad / totalHeight);
                    }
                }
            }

            // Update child groups..
            base.BuildAOSpheres(aoSpheres);
        }

        override public void UpdateMesh(List<TreeMaterial> materials, List<TreeVertex> verts, List<TreeTriangle> tris, List<TreeAOSphere> aoSpheres, int buildFlags, float adaptiveQuality, float aoDensity)
        {
            //  int materialIndex = GetMaterialIndex(materialBranch, materials);
            //  Debug.Log("branch material index: "+materialIndex);
            // Create mesh for nodes

            if (geometryMode == GeometryMode.Branch || geometryMode == GeometryMode.BranchFrond)
            {
                GetMaterialIndex(materialBranch, materials, true);
                if (breakingChance > 0)
                {
                    GetMaterialIndex(materialBreak, materials, false);
                }
            }
            if (geometryMode == GeometryMode.Frond || geometryMode == GeometryMode.BranchFrond)
            {
                GetMaterialIndex(materialFrond, materials, false);
            }

            // Create mesh for nodes
            for (int i = 0; i < nodes.Count; i++)
            {
                UpdateNodeMesh(nodes[i], materials, verts, tris, aoSpheres, buildFlags, adaptiveQuality, aoDensity);
            }

            // Update child groups..
            base.UpdateMesh(materials, verts, tris, aoSpheres, buildFlags, adaptiveQuality, aoDensity);
        }

        private void UpdateNodeMesh(TreeNode node, List<TreeMaterial> materials, List<TreeVertex> verts, List<TreeTriangle> tris, List<TreeAOSphere> aoSpheres, int buildFlags, float adaptiveQuality, float aoDensity)
        {
            // Clear tri range
            node.triStart = tris.Count;
            node.triEnd = tris.Count;
            node.vertStart = verts.Count;
            node.vertEnd = verts.Count;

            // Check for visibility
            if (!node.visible || !visible) return;

            Profiler.BeginSample("TreeGroupBranch.UpdateNodeMesh");

            int vertOffset = verts.Count;

            float totalHeight = node.spline.GetApproximateLength();// *node.GetScale(); //height * node.GetScale();

            // list to hold the ring loops
            List<RingLoop> ringloops = new List<RingLoop>();

            // Modify LOD to fit local settings
            float lodQuality = Mathf.Clamp01(adaptiveQuality * lodQualityMultiplier);

            // LOD settings
            List<float> heightSamples = TreeData.GetAdaptiveSamples(this, node, lodQuality);
            int radialSamples = TreeData.GetAdaptiveRadialSegments(radius, lodQuality);

            //
            // Parent branch group if any..
            TreeGroupBranch parentBranchGroup = null;
            if ((parentGroup != null) && (parentGroup.GetType() == typeof(TreeGroupBranch)))
            {
                parentBranchGroup = (TreeGroupBranch)parentGroup;
            }

            if ((geometryMode == GeometryMode.BranchFrond) || (geometryMode == GeometryMode.Branch))
            {
                int materialIndex = GetMaterialIndex(materialBranch, materials, true);

                float uvOffset = 0.0f;
                float uvBaseOffset = 0.0f;
                float uvStep = totalHeight / (GetRadiusAtTime(node, 0.0f, false) * Mathf.PI * 2.0f);
                bool uvAdapt = true;

                if (node.parent != null && parentBranchGroup != null)
                {
                    uvBaseOffset = node.offset * node.parent.spline.GetApproximateLength();
                }

                float capStart = 1.0f - node.capRange;
                for (int i = 0; i < heightSamples.Count; i++)
                {
                    float t = heightSamples[i];

                    Vector3 pos = node.spline.GetPositionAtTime(t);
                    Quaternion rot = node.spline.GetRotationAtTime(t);
                    float rad = GetRadiusAtTime(node, t, false);

                    Matrix4x4 m = node.matrix * Matrix4x4.TRS(pos, rot, new Vector3(1, 1, 1));

                    // total offset for wind animation
                    //float totalOffset = (totalOffsetBase + t);

                    // flare / weld spreading
                    Vector3 flareWeldSpread = GetFlareWeldAtTime(node, t);

                    // Do adaptive LOD for ringloops
                    float radModify = Mathf.Max(flareWeldSpread.x, Mathf.Max(flareWeldSpread.y, flareWeldSpread.z) * 0.25f);

                    // keep the same number of vertices per ring for the cap.. to give a nicer result
                    if (t <= capStart)
                    {
                        radialSamples = TreeData.GetAdaptiveRadialSegments(rad + radModify, lodQuality);
                    }

                    // uv offset..
                    if (uvAdapt)
                    {
                        if (i > 0)
                        {
                            float preT = heightSamples[i - 1];
                            float uvDelta = t - preT;
                            float uvRad = (rad + GetRadiusAtTime(node, preT, false)) * 0.5f;
                            uvOffset += (uvDelta * totalHeight) / (uvRad * Mathf.PI * 2.0f);
                        }
                    }
                    else
                    {
                        uvOffset = uvBaseOffset + (t * uvStep);
                    }

                    // wind
                    Vector2 windFactors = ComputeWindFactor(node, t);

                    RingLoop r = new RingLoop();
                    r.Reset(rad, m, uvOffset, radialSamples);
                    r.SetSurfaceAngle(node.GetSurfaceAngleAtTime(t));
                    r.SetAnimationProperties(windFactors.x, windFactors.y, 0.0f, node.animSeed);
                    r.SetSpread(flareWeldSpread.y, flareWeldSpread.z);
                    r.SetNoise(noise * Mathf.Clamp01(noiseCurve.Evaluate(t)), noiseScaleU * 10.0f, noiseScaleV * 10.0f);
                    r.SetFlares(flareWeldSpread.x, flareNoise * 10.0f);

                    int vertStart = verts.Count;
                    r.BuildVertices(verts);
                    int vertEnd = verts.Count;


                    if ((buildFlags & (int)BuildFlag.BuildWeldParts) != 0)
                    {
                        float projectionRange = weldHeight;
                        float projectionBlend = Mathf.Pow(Mathf.Clamp01(((1.0f - t) - (1.0f - weldHeight)) / weldHeight), 1.5f);
                        float invProjectionBlend = 1.0f - projectionBlend;

                        if (t < projectionRange)
                        {
                            if ((node.parent != null) && (node.parent.spline != null))
                            {
                                Ray ray = new Ray();
                                for (int v = vertStart; v < vertEnd; v++)
                                {
                                    ray.origin = verts[v].pos;
                                    ray.direction = m.MultiplyVector(-Vector3.up);

                                    Vector3 origPos = verts[v].pos;
                                    Vector3 origNor = verts[v].nor;

                                    float minDist = -10000.0f;
                                    float maxDist = 100000.0f;
                                    // project vertices onto parent
                                    for (int tri = node.parent.triStart; tri < node.parent.triEnd; tri++)
                                    {
                                        object hit = MathUtils.IntersectRayTriangle(ray, verts[tris[tri].v[0]].pos,
                                            verts[tris[tri].v[1]].pos,
                                            verts[tris[tri].v[2]].pos, true);
                                        if (hit != null)
                                        {
                                            RaycastHit rayHit = ((RaycastHit)hit);
                                            if ((Mathf.Abs(rayHit.distance) < maxDist) && (rayHit.distance > minDist))
                                            {
                                                maxDist = Mathf.Abs(rayHit.distance);
                                                verts[v].nor = (verts[tris[tri].v[0]].nor * rayHit.barycentricCoordinate.x) +
                                                    (verts[tris[tri].v[1]].nor * rayHit.barycentricCoordinate.y) +
                                                    (verts[tris[tri].v[2]].nor * rayHit.barycentricCoordinate.z);

                                                verts[v].nor = (verts[v].nor * projectionBlend) + (origNor * invProjectionBlend);

                                                verts[v].pos = (rayHit.point * projectionBlend) + (origPos * invProjectionBlend);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }


                    ringloops.Add(r);

                    // make sure we cap the puppy..
                    if ((t == 1.0f) && (r.radius > 0.005f))
                    {
                        RingLoop r2 = r.Clone();
                        r2.radius = 0.0f;
                        r2.baseOffset += rad / (Mathf.PI * 2.0f);
                        r2.BuildVertices(verts);
                        ringloops.Add(r2);
                    }
                }

                // Cap
                // if needed..
                if (ringloops.Count > 0)
                {
                    if (ringloops[ringloops.Count - 1].radius > 0.025f)
                    {
                        if (node.breakOffset < 1.0f)
                        {
                            float mappingScale = 1.0f / (radius * Mathf.PI * 2.0f);
                            float tempCapSpherical = 0.0f;
                            float tempCapNoise = 1.0f;
                            int tempCapMapping = 0;
                            Material tempCapMaterial = materialBranch;

                            if (materialBreak != null)
                            {
                                tempCapMaterial = materialBreak;
                            }

                            int capMaterialIndex = GetMaterialIndex(tempCapMaterial, materials, false);

                            ringloops[ringloops.Count - 1].Cap(tempCapSpherical, tempCapNoise, tempCapMapping, mappingScale,
                                verts,
                                tris,
                                capMaterialIndex);
                        }
                    }
                }

                // Build triangles
                // Debug.Log("MAT INDEX: "+materialIndex);
                node.triStart = tris.Count;
                for (int i = 0; i < (ringloops.Count - 1); i++)
                {
                    ringloops[i].Connect(ringloops[i + 1], tris, materialIndex, false, false);
                }
                node.triEnd = tris.Count;

                ringloops.Clear();
            }

            // Build fronds
            float frondMin = Mathf.Min(frondRange.x, frondRange.y);
            float frondMax = Mathf.Max(frondRange.x, frondRange.y);

            // Mapping range.. may be different from min/max if broken..
            float frondMappingMin = frondMin;
            float frondMappingMax = frondMax;

            // Include breaking
            frondMin = Mathf.Clamp(frondMin, 0.0f, node.breakOffset);
            frondMax = Mathf.Clamp(frondMax, 0.0f, node.breakOffset);

            if ((geometryMode == GeometryMode.BranchFrond) || (geometryMode == GeometryMode.Frond))
            {
                if ((frondCount > 0) && (frondMin != frondMax))
                {
                    bool needStartPoint = true;
                    bool needEndPoint = true;

                    for (int i = 0; i < heightSamples.Count; i++)
                    {
                        float t = heightSamples[i];

                        if (t < frondMin)
                        {
                            heightSamples.RemoveAt(i);
                            i--;
                        }
                        else if (t == frondMin)
                        {
                            needStartPoint = false;
                        }
                        else if (t == frondMax)
                        {
                            needEndPoint = false;
                        }
                        else if (t > frondMax)
                        {
                            heightSamples.RemoveAt(i);
                            i--;
                        }
                    }

                    if (needStartPoint)
                    {
                        heightSamples.Insert(0, frondMin);
                    }
                    if (needEndPoint)
                    {
                        heightSamples.Add(frondMax);
                    }

                    int frondMaterialIndex = GetMaterialIndex(materialFrond, materials, false);

                    float capStart = 1.0f - node.capRange;
                    //float capRadius = GetRadiusAtTime(capStart);
                    for (int j = 0; j < frondCount; j++)
                    {
                        float crease = (frondCrease * 90.0f) * Mathf.Deg2Rad;
                        float angle = ((frondRotation * 360.0f) + (((float)j * 180.0f) / frondCount) - 90.0f) * Mathf.Deg2Rad;
                        float angleA = -angle - crease;
                        float angleB = angle - crease;
                        Vector3 directionA = new Vector3(Mathf.Sin(angleA), 0.0f, Mathf.Cos(angleA));
                        Vector3 normalA = new Vector3(directionA.z, 0.0f, -directionA.x);
                        Vector3 directionB = new Vector3(Mathf.Sin(angleB), 0.0f, -Mathf.Cos(angleB));
                        Vector3 normalB = new Vector3(-directionB.z, 0.0f, directionB.x);

                        //float totalOffsetBase = GetTotalOffset();

                        for (int i = 0; i < heightSamples.Count; i++)
                        {
                            float t = heightSamples[i];
                            float v = (t - frondMappingMin) / (frondMappingMax - frondMappingMin);

                            // handle soft capping..
                            float t2 = t;
                            if (t > capStart)
                            {
                                t2 = capStart;

                                float capAngle = Mathf.Acos(Mathf.Clamp01((t - capStart) / node.capRange));
                                float sinCapAngle = Mathf.Sin(capAngle);
                                float cosCapAngle = Mathf.Cos(capAngle) * capSmoothing;

                                directionA = new Vector3(Mathf.Sin(angleA) * sinCapAngle, cosCapAngle, Mathf.Cos(angleA) * sinCapAngle);
                                normalA = new Vector3(directionA.z, directionA.y, -directionA.x);
                                directionB = new Vector3(Mathf.Sin(angleB) * sinCapAngle, cosCapAngle, -Mathf.Cos(angleB) * sinCapAngle);
                                normalB = new Vector3(-directionB.z, directionB.y, directionB.x);
                            }

                            Vector3 normalMid = new Vector3(0, 0, -1);

                            Vector3 pos = node.spline.GetPositionAtTime(t2);
                            Quaternion rot = node.spline.GetRotationAtTime(t);
                            float rad = (Mathf.Clamp01(frondCurve.Evaluate(t)) * frondWidth * node.GetScale());

                            Matrix4x4 m = node.matrix * Matrix4x4.TRS(pos, rot, new Vector3(1, 1, 1));

                            if (GenerateDoubleSidedGeometry)
                            {
                                // Generate double sided geometry to compensate for lack of VFACE shader semantic
                                // Twice the poly count
                                // Split vertices along back seam, to avoid bent normals.. 8 verts instead of 3

                                for (float side = -1; side < 2; side += 2)
                                {
                                    TreeVertex v0 = new TreeVertex();
                                    v0.pos = m.MultiplyPoint(directionA * rad);
                                    v0.nor = m.MultiplyVector(normalA * side).normalized;
                                    v0.tangent = CreateTangent(node, rot, v0.nor);
                                    v0.tangent.w = -side;
                                    v0.uv0 = new Vector2(1.0f, v);

                                    TreeVertex v1 = new TreeVertex();
                                    v1.pos = m.MultiplyPoint(Vector3.zero);
                                    v1.nor = m.MultiplyVector(normalMid * side).normalized;
                                    v1.tangent = CreateTangent(node, rot, v1.nor);
                                    v1.tangent.w = -side;
                                    v1.uv0 = new Vector2(0.5f, v);

                                    TreeVertex v2 = new TreeVertex();
                                    v2.pos = m.MultiplyPoint(Vector3.zero);
                                    v2.nor = m.MultiplyVector(normalMid * side).normalized;
                                    v2.tangent = CreateTangent(node, rot, v2.nor);
                                    v2.tangent.w = -side;
                                    v2.uv0 = new Vector2(0.5f, v);

                                    TreeVertex v3 = new TreeVertex();
                                    v3.pos = m.MultiplyPoint(directionB * rad);
                                    v3.nor = m.MultiplyVector(normalB * side).normalized;
                                    v3.tangent = CreateTangent(node, rot, v3.nor);
                                    v3.tangent.w = -side;
                                    v3.uv0 = new Vector2(0.0f, v);

                                    // Animation properties..
                                    Vector2 windFactors = ComputeWindFactor(node, t);

                                    v0.SetAnimationProperties(windFactors.x, windFactors.y, animationEdge, node.animSeed);
                                    v1.SetAnimationProperties(windFactors.x, windFactors.y, 0.0f, node.animSeed); // no edge flutter for center vertex
                                    v2.SetAnimationProperties(windFactors.x, windFactors.y, 0.0f, node.animSeed); // no edge flutter for center vertex
                                    v3.SetAnimationProperties(windFactors.x, windFactors.y, animationEdge, node.animSeed);

                                    verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
                                }

                                if (i > 0)
                                {
                                    int voffset = verts.Count;

                                    //
                                    // theoretical left side :)
                                    //
                                    // back
                                    TreeTriangle tri0 = new TreeTriangle(frondMaterialIndex, voffset - 4, voffset - 3, voffset - 11);
                                    TreeTriangle tri1 = new TreeTriangle(frondMaterialIndex, voffset - 4, voffset - 11, voffset - 12);
                                    tri0.flip(); tri1.flip();

                                    // front
                                    TreeTriangle tri2 = new TreeTriangle(frondMaterialIndex, voffset - 8, voffset - 7, voffset - 15);
                                    TreeTriangle tri3 = new TreeTriangle(frondMaterialIndex, voffset - 8, voffset - 15, voffset - 16);

                                    tris.Add(tri0); tris.Add(tri1);
                                    tris.Add(tri2); tris.Add(tri3);

                                    //
                                    // theoretical right side :)
                                    //
                                    // front
                                    TreeTriangle tri4 = new TreeTriangle(frondMaterialIndex, voffset - 2, voffset - 9, voffset - 1);
                                    TreeTriangle tri5 = new TreeTriangle(frondMaterialIndex, voffset - 2, voffset - 10, voffset - 9);

                                    // back
                                    TreeTriangle tri6 = new TreeTriangle(frondMaterialIndex, voffset - 6, voffset - 13, voffset - 5);
                                    TreeTriangle tri7 = new TreeTriangle(frondMaterialIndex, voffset - 6, voffset - 14, voffset - 13);
                                    tri6.flip(); tri7.flip();

                                    tris.Add(tri4); tris.Add(tri5);
                                    tris.Add(tri6); tris.Add(tri7);
                                }
                            }
                            else
                            {
                                // Single sided geometry .. we'll keep this for later

                                TreeVertex v0 = new TreeVertex();
                                v0.pos = m.MultiplyPoint(directionA * rad);
                                v0.nor = m.MultiplyVector(normalA).normalized;
                                v0.uv0 = new Vector2(0.0f, v);

                                TreeVertex v1 = new TreeVertex();
                                v1.pos = m.MultiplyPoint(Vector3.zero);
                                v1.nor = m.MultiplyVector(Vector3.back).normalized;
                                v1.uv0 = new Vector2(0.5f, v);

                                TreeVertex v2 = new TreeVertex();
                                v2.pos = m.MultiplyPoint(directionB * rad);
                                v2.nor = m.MultiplyVector(normalB).normalized;
                                v2.uv0 = new Vector2(1.0f, v);

                                // Animation properties..
                                Vector2 windFactors = ComputeWindFactor(node, t);

                                v0.SetAnimationProperties(windFactors.x, windFactors.y, animationEdge, node.animSeed);
                                v1.SetAnimationProperties(windFactors.x, windFactors.y, 0.0f, node.animSeed); // no edge flutter for center vertex
                                v2.SetAnimationProperties(windFactors.x, windFactors.y, animationEdge, node.animSeed);

                                verts.Add(v0); verts.Add(v1); verts.Add(v2);

                                if (i > 0)
                                {
                                    int voffset = verts.Count;

                                    TreeTriangle tri0 = new TreeTriangle(frondMaterialIndex, voffset - 2, voffset - 3, voffset - 6);
                                    TreeTriangle tri1 = new TreeTriangle(frondMaterialIndex, voffset - 2, voffset - 6, voffset - 5);
                                    tris.Add(tri0); tris.Add(tri1);

                                    TreeTriangle tri2 = new TreeTriangle(frondMaterialIndex, voffset - 2, voffset - 4, voffset - 1);
                                    TreeTriangle tri3 = new TreeTriangle(frondMaterialIndex, voffset - 2, voffset - 5, voffset - 4);
                                    tris.Add(tri2); tris.Add(tri3);
                                }
                            }
                        }
                    }
                }
            }

            // compute ambient occlusion..
            if ((buildFlags & (int)BuildFlag.BuildAmbientOcclusion) != 0)
            {
                for (int i = vertOffset; i < verts.Count; i++)
                {
                    verts[i].SetAmbientOcclusion(ComputeAmbientOcclusion(verts[i].pos, verts[i].nor, aoSpheres, aoDensity));
                }
            }

            node.vertEnd = verts.Count;

            Profiler.EndSample(); // TreeGroupBranch.UpdateNodeMesh
        }

        public void UpdateSpline(TreeNode node)
        {
            // locked due to user editing...
            if (lockFlags != 0)
            {
                return;
            }

            Random.InitState(node.seed);

            if (node.spline == null)
            {
                TreeSpline spline = new TreeSpline();
                node.spline = spline;

                // Add asset to database..
                // UnityEditor.AssetDatabase.AddObjectToAsset(spline, this);
            }

            float totalHeight = height.y * node.GetScale();

            float stepSize = 1.0f;
            int count = (int)Mathf.Round(totalHeight / stepSize);

            float curHeight = 0.0f;

            Quaternion r = Quaternion.identity;
            Vector3 p = new Vector3(0, 0, 0);

            Matrix4x4 worldUp = (node.matrix * GetRootMatrix()).inverse;

            Quaternion rotPhotropism = MathUtils.QuaternionFromMatrix(worldUp) * Quaternion.Euler(0.0f, node.angle, 0.0f);
            Quaternion rotGravitropism = MathUtils.QuaternionFromMatrix(worldUp) * Quaternion.Euler(-180.0f, node.angle, 0.0f);

            node.spline.Reset();
            node.spline.AddPoint(p, 0.0f);

            for (int i = 0; i < count; i++)
            {
                float stepPos = stepSize;
                if (i == (count - 1))
                {
                    stepPos = totalHeight - curHeight;
                }
                curHeight += stepPos;
                float curTime = curHeight / totalHeight;

                // seek towards sun or ground..
                float seekValue = Mathf.Clamp(seekCurve.Evaluate(curTime), -1.0f, 1.0f);
                float seekSun = Mathf.Clamp01(seekValue) * seekBlend;
                float seekGround = Mathf.Clamp01(-seekValue) * seekBlend;
                r = Quaternion.Slerp(r, rotPhotropism, seekSun);
                r = Quaternion.Slerp(r, rotGravitropism, seekGround);

                // crinkle
                float crinkle = crinklyness * Mathf.Clamp01(crinkCurve.Evaluate(curTime));
                Quaternion c = Quaternion.Euler(new Vector3(180.0f * (Random.value - 0.5f), node.angle, 180.0f * (Random.value - 0.5f)));
                r = Quaternion.Slerp(r, c, crinkle);

                // advance position
                p += r * (new Vector3(0.0f, stepPos, 0.0f));

                node.spline.AddPoint(p, curTime);
            }

            // Rethink time and rotations
            node.spline.UpdateTime();
            node.spline.UpdateRotations();
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
