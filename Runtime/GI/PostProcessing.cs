// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Rendering;

namespace UnityEngine.LightTransport
{
    namespace PostProcessing
    {
        public interface IProbePostProcessor : IDisposable
        {
            // Initialize the post processor.
            bool Initialize(IDeviceContext context);

            // Convolve spherical radiance to irradiance.
            bool ConvolveRadianceToIrradiance(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> radianceIn, BufferSlice<SphericalHarmonicsL2> irradianceOut, int probeCount);

            // Unity expects the following of the irradiance SH coefficients:
            // 1) For L0 and L1, they must have the SH standard normalization terms folded into them (to avoid doing this multiplication in shader).
            // 2) They must be divided by Pi for historical reasons.
            // 3) L1 terms must be in yzx order (rather than standard xyz).
            //    This is flipped back in GetShaderConstantsFromNormalizedSH before passed to shader.
            bool ConvertToUnityFormat(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> irradianceIn, BufferSlice<SphericalHarmonicsL2> irradianceOut, int probeCount);

            // Same as above, but divideByPI controls step 2: pass false to keep textbook irradiance, matching a render
            // pipeline that disables SupportedRenderingFeatures.divideBakedOutputByPI.
            // The default implementation exists to keep pre-existing implementors source and binary compatible;
            // it only supports the historical divide-by-PI convention and fails otherwise.
            bool ConvertToUnityFormat(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> irradianceIn, BufferSlice<SphericalHarmonicsL2> irradianceOut, int probeCount, bool divideByPI)
            {
                if (!divideByPI)
                    return false;

                return ConvertToUnityFormat(context, irradianceIn, irradianceOut, probeCount);
            }

            // Add two sets of SH coefficients together.
            bool AddSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> A, BufferSlice<SphericalHarmonicsL2> B, BufferSlice<SphericalHarmonicsL2> sum, int probeCount);

            // Uniformly scale all SH coefficients.
            bool ScaleSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount, float scale);

            // Spherical Harmonics windowing can be used to reduce ringing artifacts.
            bool WindowSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount);

            // Spherical Harmonics de-ringing can be used to reduce ringing artifacts.
            bool DeringSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount);
        }
    }
}
