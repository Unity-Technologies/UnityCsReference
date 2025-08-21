// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// This class is used to store changes to a number of rendering quality settings that are applied when using
    /// the Universal Render Pipeline.
    /// </summary>
    public static class AdaptivePerformanceRenderSettings
    {
        private static float s_MaxShadowDistanceMultiplier = 1;
        private static float s_ShadowResolutionMultiplier = 1;
        private static float s_RenderScaleMultiplier = 1;
        private static float s_DecalsMaxDistance = 1000;

        /// <summary>
        /// Amount to multiply the main lights shadowmap resolution. Values are clamped between 0 and 1.
        /// </summary>
        public static float MainLightShadowmapResolutionMultiplier
        {
            get { return s_ShadowResolutionMultiplier; }
            set { s_ShadowResolutionMultiplier = Mathf.Clamp01(value); }
        }

        /// <summary>
        /// Adjust the drawdistance for decals.
        /// </summary>
        public static float DecalsDrawDistance
        {
            get { return s_DecalsMaxDistance; }
            set { s_DecalsMaxDistance = value; }
        }

        /// <summary>
        /// Adjust the number of shadow cascades for the main camera in the scene.
        /// </summary>
        public static int MainLightShadowCascadesCountBias
        {
            get;
            set;
        }

        /// <summary>
        /// Adjust the quality setting of shadows.
        /// </summary>
        public static int ShadowQualityBias
        {
            get;
            set;
        }
        /// <summary>
        /// Adjust the size of lookup tables that are used for color grading.
        /// </summary>
        public static float LutBias
        {
            get;
            set;
        }
        /// <summary>
        /// Adjust how far in the distance shadows will be rendered. Values are clamped between 0 and 1.
        /// </summary>
        public static float MaxShadowDistanceMultiplier
        {
            get { return s_MaxShadowDistanceMultiplier; }
            set { s_MaxShadowDistanceMultiplier = Mathf.Clamp01(value); }
        }
        /// <summary>
        /// Lower the resolution of the main camera to reduce fillrate and GPU load.
        /// </summary>
        public static float RenderScaleMultiplier
        {
            get { return s_RenderScaleMultiplier; }
            set { s_RenderScaleMultiplier = Mathf.Clamp01(value); }
        }
        /// <summary>
        /// Adjust the quality of MSAA.
        /// </summary>
        public static int AntiAliasingQualityBias
        {
            get;
            set;
        }
        /// <summary>
        /// Whether dynamic batching should be used when rendering multiple objects that share the same material.
        /// Useful on hardware that does not support instancing.
        /// </summary>
        public static bool SkipDynamicBatching
        {
            get;
            set;
        }
        /// <summary>
        /// Whether depth-based sorting should be enabled.
        /// When enabled, there is a higher load on the CPU but less rendering overdraw.
        /// When disabled, there is less CPU pressure but more overdraw.
        /// </summary>
        public static bool SkipFrontToBackSorting
        {
            get;
            set;
        }

        /// <summary>
        /// Whether transparent objects should be rendered
        /// When enabled, there is less rendering overdraw, but entire objects can disappear.
        /// </summary>
        public static bool SkipTransparentObjects
        {
            get;
            set;
        }
    }
}
