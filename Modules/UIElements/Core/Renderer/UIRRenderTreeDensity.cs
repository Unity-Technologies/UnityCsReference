// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements.UIR
{
    // Texels per point a filtered subtree is rendered at, before visibility and atlas limits
    static class RenderTreeDensity
    {
        // Steps of 2^(1/4) so a scale animation reuses a few texture sizes instead of allocating one per frame
        const float k_StepsPerOctave = 4;

        const float k_ScaleTolerance = 1e-4f;

        // The displayed density, never below the panel's
        public static Vector2 Compute(Vector2 worldScale, ReadOnlySpan<UnmanagedFilterFunction> filters, float panelPixelsPerPoint)
        {
            var panelDensity = new Vector2(panelPixelsPerPoint, panelPixelsPerPoint);
            var scale = new Vector2(QuantizeScaleUp(worldScale.x), QuantizeScaleUp(worldScale.y));

            // Only blur() softens the whole subtree (drop-shadow draws the content sharp on top), so only it may lower the resolution
            float blurSigma = BackdropFilterHelper.ComputeMaxChainSigma(filters, includeDropShadow: false);
            int downscaleShift = BackdropFilterHelper.ComputeDownscaleShift(blurSigma, scale, panelPixelsPerPoint);
            return Vector2.Max(panelDensity, Vector2.Scale(panelDensity, scale) / (1 << downscaleShift));
        }

        // Quantized down, so a capped element that animates still reuses a few texture sizes
        public static Vector2 LowerToFit(Vector2 density, Vector2 floor, Vector2 pointSize, Vector2 limit)
        {
            if (pointSize.x * density.x > limit.x)
                density.x = floor.x * QuantizeRatioDown(limit.x / pointSize.x / floor.x);
            if (pointSize.y * density.y > limit.y)
                density.y = floor.y * QuantizeRatioDown(limit.y / pointSize.y / floor.y);
            return density;
        }

        // Integer scales stay exact so they composite 1:1
        static float QuantizeScaleUp(float scale)
        {
            if (scale <= 1 + k_ScaleTolerance)
                return 1;
            float step = Mathf.Pow(2, Mathf.Ceil(Mathf.Log(scale, 2) * k_StepsPerOctave - k_ScaleTolerance) / k_StepsPerOctave);
            return Mathf.Min(step, Mathf.Ceil(scale - k_ScaleTolerance));
        }

        static float QuantizeRatioDown(float ratio)
        {
            if (ratio <= 1)
                return 1;
            float step = Mathf.Pow(2, Mathf.Floor(Mathf.Log(ratio, 2) * k_StepsPerOctave) / k_StepsPerOctave);
            return Mathf.Max(step, Mathf.Floor(ratio));
        }
    }
}
