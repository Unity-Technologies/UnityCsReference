// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;

namespace TreeEditor
{
    [System.Serializable]
    public class TreeGroupRoot : TreeGroup
    {
        // These should be propagated to every child..
        public float adaptiveLODQuality = 0.8f;
        public int shadowTextureQuality = 3; // texture resolution / 2^shadowTextureQuality
        public bool enableWelding = true;
        public bool enableAmbientOcclusion = true;
        public bool enableMaterialOptimize = true;

        public float aoDensity = 1.0f;
        public float rootSpread = 5.0f;
        public float groundOffset;

        public Matrix4x4 rootMatrix = Matrix4x4.identity;

        static class Styles
        {
            public static string groupSeedString = LocalizationDatabase.GetLocalizedString("Tree Seed|The global seed that affects the entire tree. Use it to randomize your tree, while keeping the general structure of it.");
        }

        public void SetRootMatrix(Matrix4x4 m)
        {
            rootMatrix = m;

            // Root node needs to remove scale and position...
            rootMatrix.m03 = 0.0f;
            rootMatrix.m13 = 0.0f;
            rootMatrix.m23 = 0.0f;
            rootMatrix = MathUtils.OrthogonalizeMatrix(rootMatrix);
            nodes[0].matrix = rootMatrix;
        }

        override public bool CanHaveSubGroups()
        {
            return true;
        }

        override public void UpdateParameters()
        {
            Profiler.BeginSample("UpdateParameters");

            // Set properties
            nodes[0].size = rootSpread;
            nodes[0].matrix = rootMatrix;

            // Update sub-groups
            base.UpdateParameters();

            Profiler.EndSample(); // UpdateParameters
        }

        internal override string GroupSeedString { get { return Styles.groupSeedString; } }

        internal override string FrequencyString { get { return null; } }

        internal override string DistributionModeString { get { return null; } }

        internal override string TwirlString { get { return null; } }

        internal override string WhorledStepString { get { return null; } }

        internal override string GrowthScaleString { get { return null; } }

        internal override string GrowthAngleString { get { return null; } }

        internal override string MainWindString { get { return null; } }

        internal override string MainTurbulenceString { get { return null; } }

        internal override string EdgeTurbulenceString { get { return null; } }
    }
}
