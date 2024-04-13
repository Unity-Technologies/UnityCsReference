// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
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

            // Add two sets of SH coefficients together.
            bool AddSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> A, BufferSlice<SphericalHarmonicsL2> B, BufferSlice<SphericalHarmonicsL2> sum, int probeCount);

            // Uniformly scale all SH coefficients.
            bool ScaleSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount, float scale);

            // Spherical Harmonics windowing can be used to reduce ringing artifacts.
            bool WindowSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount);

            // Spherical Harmonics de-ringing can be used to reduce ringing artifacts.
            bool DeringSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount);
        }

        struct SH
        {
            // Notation:
            //                       [L00:  DC]
            //            [L1-1:  x] [L10:   y] [L11:   z]
            // [L2-2: xy] [L2-1: yz] [L20:  zz] [L21:  xz]  [L22:  xx - yy]
            // Underscores are meant as a minus sign in the variable names below.
            public const int L00 = 0;
            public const int L1_1 = 1;
            public const int L10 = 2;
            public const int L11 = 3;
            public const int L2_2 = 4;
            public const int L2_1 = 5;
            public const int L20 = 6;
            public const int L21 = 7;
            public const int L22 = 8;

            // Number of coefficients in the SH L2 basis.
            public const int L2_CoeffCount = 9;
        }

        struct SphericalRadianceToIrradiance
        {
            // aHat is from https://cseweb.ucsd.edu/~ravir/papers/envmap/envmap.pdf and is used to convert spherical radiance to irradiance.
            public const float aHat0 = 3.1415926535897932384626433832795028841971693993751058209749445923f; // π
            public const float aHat1 = 2.0943951023931954923084289221863352561314462662500705473166297282f; // 2π/3
            public const float aHat2 = 0.785398f; // π/4 (see equation 8).
        }

        //[BurstCompile] // TODO: Use burst once it is supported in the editor or we move to a package.
        struct ConvolveJob : IJobParallelFor
        {
            [ReadOnly] public NativeSlice<SphericalHarmonicsL2> Radiances;
            [WriteOnly] public NativeSlice<SphericalHarmonicsL2> Irradiances;
            public void Execute(int probeIdx)
            {
                SphericalHarmonicsL2 radiance = Radiances[probeIdx];
                var irradiance = new SphericalHarmonicsL2();
                for (int rgb = 0; rgb < 3; rgb++)
                {
                    irradiance[rgb, SH.L00] = radiance[rgb, SH.L00] * SphericalRadianceToIrradiance.aHat0;
                    irradiance[rgb, SH.L1_1] = radiance[rgb, SH.L1_1] * SphericalRadianceToIrradiance.aHat1;
                    irradiance[rgb, SH.L10] = radiance[rgb, SH.L10] * SphericalRadianceToIrradiance.aHat1;
                    irradiance[rgb, SH.L11] = radiance[rgb, SH.L11] * SphericalRadianceToIrradiance.aHat1;
                    irradiance[rgb, SH.L2_2] = radiance[rgb, SH.L2_2] * SphericalRadianceToIrradiance.aHat2;
                    irradiance[rgb, SH.L2_1] = radiance[rgb, SH.L2_1] * SphericalRadianceToIrradiance.aHat2;
                    irradiance[rgb, SH.L20] = radiance[rgb, SH.L20] * SphericalRadianceToIrradiance.aHat2;
                    irradiance[rgb, SH.L21] = radiance[rgb, SH.L21] * SphericalRadianceToIrradiance.aHat2;
                    irradiance[rgb, SH.L22] = radiance[rgb, SH.L22] * SphericalRadianceToIrradiance.aHat2;
                };
                Irradiances[probeIdx] = irradiance;
            }
        }
        //[BurstCompile] // TODO: Use burst once it is supported in the editor or we move to a package.
        struct UnityfyJob : IJobParallelFor
        {
            [ReadOnly] public NativeSlice<SphericalHarmonicsL2> IrradianceIn;
            [WriteOnly] public NativeSlice<SphericalHarmonicsL2> IrradianceOut;
            public void Execute(int probeIdx)
            {
                float shY0Normalization = Mathf.Sqrt(1.0f / Mathf.PI) / 2.0f;

                float shY1Normalization = Mathf.Sqrt(3.0f / Mathf.PI) / 2.0f;

                float shY2_2Normalization = Mathf.Sqrt(15.0f / Mathf.PI) / 2.0f;
                float shY2_1Normalization = shY2_2Normalization;
                float shY20Normalization = Mathf.Sqrt(5.0f / Mathf.PI) / 4.0f;
                float shY21Normalization = shY2_2Normalization;
                float shY22Normalization = Mathf.Sqrt(15.0f / Mathf.PI) / 4.0f;

                SphericalHarmonicsL2 irradiance = IrradianceIn[probeIdx];
                var output = new SphericalHarmonicsL2();
                for (int rgb = 0; rgb < 3; ++rgb)
                {
                    // L0
                    output[rgb, SH.L00] = irradiance[rgb, SH.L00];
                    output[rgb, SH.L00] *= shY0Normalization; // 1)
                    output[rgb, SH.L00] /= Mathf.PI; // 2)

                    // L1
                    output[rgb, SH.L1_1] = irradiance[rgb, SH.L10]; // 3)
                    output[rgb, SH.L1_1] *= shY1Normalization; // 1)
                    output[rgb, SH.L1_1] /= Mathf.PI; // 3 )

                    output[rgb, SH.L10] = irradiance[rgb, SH.L11]; // 3)
                    output[rgb, SH.L10] *= shY1Normalization; // 1)
                    output[rgb, SH.L10] /= Mathf.PI; // 2)

                    output[rgb, SH.L11] = irradiance[rgb, SH.L1_1]; // 3)
                    output[rgb, SH.L11] *= shY1Normalization; // 1)
                    output[rgb, SH.L11] /= Mathf.PI; // 2)

                    // L2
                    output[rgb, SH.L2_2] = irradiance[rgb, SH.L2_2];
                    output[rgb, SH.L2_2] *= shY2_2Normalization; // 1)
                    output[rgb, SH.L2_2] /= Mathf.PI; // 2)

                    output[rgb, SH.L2_1] = irradiance[rgb, SH.L2_1];
                    output[rgb, SH.L2_1] *= shY2_1Normalization; // 1)
                    output[rgb, SH.L2_1] /= Mathf.PI; // 2)

                    output[rgb, SH.L20] = irradiance[rgb, SH.L20];
                    output[rgb, SH.L20] *= shY20Normalization; // 1)
                    output[rgb, SH.L20] /= Mathf.PI; // 2)

                    output[rgb, SH.L21] = irradiance[rgb, SH.L21];
                    output[rgb, SH.L21] *= shY21Normalization; // 1)
                    output[rgb, SH.L21] /= Mathf.PI; // 2)

                    output[rgb, SH.L22] = irradiance[rgb, SH.L22];
                    output[rgb, SH.L22] *= shY22Normalization; // 1)
                    output[rgb, SH.L22] /= Mathf.PI; // 2)
                }
                IrradianceOut[probeIdx] = output;
            }
        }
        //[BurstCompile] // TODO: Use burst once it is supported in the editor or we move to a package.
        struct AddSHJob : IJobParallelFor
        {
            [ReadOnly] public NativeSlice<SphericalHarmonicsL2> A;
            [ReadOnly] public NativeSlice<SphericalHarmonicsL2> B;
            [WriteOnly] public NativeSlice<SphericalHarmonicsL2> Sum;
            public void Execute(int probeIdx)
            {
                Sum[probeIdx] = A[probeIdx] + B[probeIdx];
            }
        }
        //[BurstCompile] // TODO: Use burst once it is supported in the editor or we move to a package.
        struct ScaleSHJob : IJobParallelFor
        {
            [ReadOnly] public NativeSlice<SphericalHarmonicsL2> Input;
            [ReadOnly] public float Scale;
            [WriteOnly] public NativeSlice<SphericalHarmonicsL2> Scaled;
            public void Execute(int probeIdx)
            {
                Scaled[probeIdx] = Input[probeIdx] * Scale;
            }
        }
        //[BurstCompile] // TODO: Use burst once it is supported in the editor or we move to a package.
        struct WindowSHJob : IJobParallelFor
        {
            [ReadOnly] public NativeSlice<SphericalHarmonicsL2> Input;
            [WriteOnly] public NativeSlice<SphericalHarmonicsL2> Windowed;
            public void Execute(int probeIdx)
            {
                // Windowing constants from WindowDirectSH in SHDering.cpp
                float[] extraWindow = new float[] { 1.0f, 0.922066f, 0.731864f };
                
                // Apply windowing: Essentially SHConv3 times the window constants
                SphericalHarmonicsL2 sh = Input[probeIdx];
                for (int coefficientIndex = 0; coefficientIndex < SH.L2_CoeffCount; ++coefficientIndex)
                {
                    float window;
                    if (coefficientIndex == 0)
                        window = extraWindow[0];
                    else if (coefficientIndex < 4)
                        window = extraWindow[1];
                    else
                        window = extraWindow[2];
                    sh[0, coefficientIndex] *= window;
                    sh[1, coefficientIndex] *= window;
                    sh[2, coefficientIndex] *= window;
                }
                Windowed[probeIdx] = sh;
            }
        }
        //[BurstCompile] // TODO: Use burst once it is supported in the editor or we move to a package.
        struct DeringSHJob : IJobParallelFor
        {
            [ReadOnly] public NativeSlice<SphericalHarmonicsL2> Input;
            [WriteOnly] public NativeSlice<SphericalHarmonicsL2> Output;
            public void Execute(int probeIdx)
            {
                SphericalHarmonicsL2 sh = Input[probeIdx];
                SphericalHarmonicsL2 output = new SphericalHarmonicsL2();
                unsafe
                {
                    var shInputPtr = (SphericalHarmonicsL2*)UnsafeUtility.AddressOf(ref sh);
                    var shOutputPtr = (SphericalHarmonicsL2*)UnsafeUtility.AddressOf(ref output);
                    bool result = WintermuteContext.DeringSphericalHarmonicsL2Internal(shInputPtr, shOutputPtr, 1);
                    Debug.Assert(result);
                }
                Output[probeIdx] = output;
            }
        }
        public class ReferenceProbePostProcessor : IProbePostProcessor
        {
            public bool Initialize(IDeviceContext context)
            {
                return true;
            }

            public bool ConvolveRadianceToIrradiance(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> radianceIn, BufferSlice<SphericalHarmonicsL2> irradianceOut, int probeCount)
            {
                Debug.Assert(context is ReferenceContext, "Expected ReferenceContext but got something else.");
                if (context is not ReferenceContext refContext)
                    return false;

                NativeArray<byte> radianceNativeArray = refContext.GetNativeArray(radianceIn.Id);
                NativeArray<byte> irradianceNativeArray = refContext.GetNativeArray(irradianceOut.Id);
                NativeArray<SphericalHarmonicsL2> Radiances = radianceNativeArray.Reinterpret<SphericalHarmonicsL2>(1);
                NativeArray<SphericalHarmonicsL2> Irradiances = irradianceNativeArray.Reinterpret<SphericalHarmonicsL2>(1);
                for (int probeIdx = 0; probeIdx < probeCount; ++probeIdx)
                {
                    SphericalHarmonicsL2 radiance = Radiances[probeIdx];
                    var irradiance = new SphericalHarmonicsL2();
                    for (int rgb = 0; rgb < 3; rgb++)
                    {
                        irradiance[rgb, SH.L00] = radiance[rgb, SH.L00] * SphericalRadianceToIrradiance.aHat0;
                        irradiance[rgb, SH.L1_1] = radiance[rgb, SH.L1_1] * SphericalRadianceToIrradiance.aHat1;
                        irradiance[rgb, SH.L10] = radiance[rgb, SH.L10] * SphericalRadianceToIrradiance.aHat1;
                        irradiance[rgb, SH.L11] = radiance[rgb, SH.L11] * SphericalRadianceToIrradiance.aHat1;
                        irradiance[rgb, SH.L2_2] = radiance[rgb, SH.L2_2] * SphericalRadianceToIrradiance.aHat2;
                        irradiance[rgb, SH.L2_1] = radiance[rgb, SH.L2_1] * SphericalRadianceToIrradiance.aHat2;
                        irradiance[rgb, SH.L20] = radiance[rgb, SH.L20] * SphericalRadianceToIrradiance.aHat2;
                        irradiance[rgb, SH.L21] = radiance[rgb, SH.L21] * SphericalRadianceToIrradiance.aHat2;
                        irradiance[rgb, SH.L22] = radiance[rgb, SH.L22] * SphericalRadianceToIrradiance.aHat2;
                    };
                    Irradiances[probeIdx] = irradiance;
                }
                return true;
            }

            public bool ConvertToUnityFormat(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> irradianceIn, BufferSlice<SphericalHarmonicsL2> irradianceOut, int probeCount)
            {
                Debug.Assert(context is ReferenceContext, "Expected ReferenceContext but got something else.");
                if (context is not ReferenceContext refContext)
                    return false;

                float shY0Normalization = Mathf.Sqrt(1.0f / Mathf.PI) / 2.0f;

                float shY1Normalization = Mathf.Sqrt(3.0f / Mathf.PI) / 2.0f;

                float shY2_2Normalization = Mathf.Sqrt(15.0f / Mathf.PI) / 2.0f;
                float shY2_1Normalization = shY2_2Normalization;
                float shY20Normalization = Mathf.Sqrt(5.0f / Mathf.PI) / 4.0f;
                float shY21Normalization = shY2_2Normalization;
                float shY22Normalization = Mathf.Sqrt(15.0f / Mathf.PI) / 4.0f;

                NativeArray<byte> irradianceInNativeArray = refContext.GetNativeArray(irradianceIn.Id);
                NativeArray<byte> irradianceOutNativeArray = refContext.GetNativeArray(irradianceOut.Id);
                NativeArray<SphericalHarmonicsL2> IrradianceIn = irradianceInNativeArray.Reinterpret<SphericalHarmonicsL2>(1);
                NativeArray<SphericalHarmonicsL2> IrradianceOut = irradianceOutNativeArray.Reinterpret<SphericalHarmonicsL2>(1);
                for (int probeIdx = 0; probeIdx < probeCount; ++probeIdx)
                {
                    SphericalHarmonicsL2 irradiance = IrradianceIn[probeIdx];
                    var output = new SphericalHarmonicsL2();
                    for (int rgb = 0; rgb < 3; ++rgb)
                    {
                        // L0
                        output[rgb, SH.L00] = irradiance[rgb, SH.L00];
                        output[rgb, SH.L00] *= shY0Normalization; // 1)
                        output[rgb, SH.L00] /= Mathf.PI; // 2)

                        // L1
                        output[rgb, SH.L1_1] = irradiance[rgb, SH.L10]; // 3)
                        output[rgb, SH.L1_1] *= shY1Normalization; // 1)
                        output[rgb, SH.L1_1] /= Mathf.PI; // 3 )

                        output[rgb, SH.L10] = irradiance[rgb, SH.L11]; // 3)
                        output[rgb, SH.L10] *= shY1Normalization; // 1)
                        output[rgb, SH.L10] /= Mathf.PI; // 2)

                        output[rgb, SH.L11] = irradiance[rgb, SH.L1_1]; // 3)
                        output[rgb, SH.L11] *= shY1Normalization; // 1)
                        output[rgb, SH.L11] /= Mathf.PI; // 2)

                        // L2
                        output[rgb, SH.L2_2] = irradiance[rgb, SH.L2_2];
                        output[rgb, SH.L2_2] *= shY2_2Normalization; // 1)
                        output[rgb, SH.L2_2] /= Mathf.PI; // 2)

                        output[rgb, SH.L2_1] = irradiance[rgb, SH.L2_1];
                        output[rgb, SH.L2_1] *= shY2_1Normalization; // 1)
                        output[rgb, SH.L2_1] /= Mathf.PI; // 2)

                        output[rgb, SH.L20] = irradiance[rgb, SH.L20];
                        output[rgb, SH.L20] *= shY20Normalization; // 1)
                        output[rgb, SH.L20] /= Mathf.PI; // 2)

                        output[rgb, SH.L21] = irradiance[rgb, SH.L21];
                        output[rgb, SH.L21] *= shY21Normalization; // 1)
                        output[rgb, SH.L21] /= Mathf.PI; // 2)

                        output[rgb, SH.L22] = irradiance[rgb, SH.L22];
                        output[rgb, SH.L22] *= shY22Normalization; // 1)
                        output[rgb, SH.L22] /= Mathf.PI; // 2)
                    }
                    IrradianceOut[probeIdx] = output;
                }
                return true;
            }

            public bool AddSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> a, BufferSlice<SphericalHarmonicsL2> b, BufferSlice<SphericalHarmonicsL2> sum, int probeCount)
            {
                Debug.Assert(context is ReferenceContext, "Expected ReferenceContext but got something else.");
                if (context is not ReferenceContext refContext)
                    return false;

                NativeArray<byte> A = refContext.GetNativeArray(a.Id);
                NativeArray<byte> B = refContext.GetNativeArray(b.Id);
                NativeArray<byte> Sum = refContext.GetNativeArray(sum.Id);
                NativeArray<SphericalHarmonicsL2> shA = A.Reinterpret<SphericalHarmonicsL2>(1);
                NativeArray<SphericalHarmonicsL2> shB = B.Reinterpret<SphericalHarmonicsL2>(1);
                NativeArray<SphericalHarmonicsL2> shSum = Sum.Reinterpret<SphericalHarmonicsL2>(1);
                for (int probeIdx = 0; probeIdx < probeCount; ++probeIdx)
                {
                    shSum[probeIdx] = shA[probeIdx] + shB[probeIdx];
                }
                return true;
            }

            public bool ScaleSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount, float scale)
            {
                Debug.Assert(context is ReferenceContext, "Expected ReferenceContext but got something else.");
                if (context is not ReferenceContext refContext)
                    return false;

                NativeArray<byte> sh = refContext.GetNativeArray(shIn.Id);
                NativeArray<byte> output = refContext.GetNativeArray(shOut.Id);
                NativeArray<SphericalHarmonicsL2> shInput = sh.Reinterpret<SphericalHarmonicsL2>(1);
                NativeArray<SphericalHarmonicsL2> shOutput = output.Reinterpret<SphericalHarmonicsL2>(1);
                for (int probeIdx = 0; probeIdx < probeCount; ++probeIdx)
                {
                    shOutput[probeIdx] = shInput[probeIdx] * scale;
                }
                return true;
            }

            public bool WindowSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount)
            {
                Debug.Assert(context is ReferenceContext, "Expected ReferenceContext but got something else.");
                if (context is not ReferenceContext refContext)
                    return false;

                NativeArray<byte> A = refContext.GetNativeArray(shIn.Id);
                NativeArray<byte> B = refContext.GetNativeArray(shOut.Id);
                NativeArray<SphericalHarmonicsL2> shA = A.Reinterpret<SphericalHarmonicsL2>(1);
                NativeArray<SphericalHarmonicsL2> shB = B.Reinterpret<SphericalHarmonicsL2>(1);

                // Windowing constants from WindowDirectSH in SHDering.cpp
                float[] extraWindow = new float[] { 1.0f, 0.922066f, 0.731864f };

                // Apply windowing function to SH coefficients.
                for (int probeIdx = 0; probeIdx < probeCount; ++probeIdx)
                {
                    // Apply windowing: Essentially SHConv3 times the window constants
                    SphericalHarmonicsL2 sh = shA[probeIdx];
                    for (int coefficientIndex = 0; coefficientIndex < SH.L2_CoeffCount; ++coefficientIndex)
                    {
                        float window;
                        if (coefficientIndex == 0)
                            window = extraWindow[0];
                        else if (coefficientIndex < 4)
                            window = extraWindow[1];
                        else
                            window = extraWindow[2];
                        sh[0, coefficientIndex] *= window;
                        sh[1, coefficientIndex] *= window;
                        sh[2, coefficientIndex] *= window;
                    }
                    shB[probeIdx] = sh;
                }
                return true;
            }

            public bool DeringSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount)
            {
                Debug.Assert(context is ReferenceContext, "Expected ReferenceContext but got something else.");
                if (context is not ReferenceContext refContext)
                    return false;

                NativeArray<byte> inputSh = refContext.GetNativeArray(shIn.Id);
                NativeArray<byte> outputSh = refContext.GetNativeArray(shOut.Id);
                NativeArray<SphericalHarmonicsL2> shInput = inputSh.Reinterpret<SphericalHarmonicsL2>(1);
                NativeArray<SphericalHarmonicsL2> shOutput = outputSh.Reinterpret<SphericalHarmonicsL2>(1);
                unsafe
                {
                    for (int probeIdx = 0; probeIdx < probeCount; ++probeIdx)
                    {
                        SphericalHarmonicsL2 sh = shInput[probeIdx];
                        SphericalHarmonicsL2 output = new SphericalHarmonicsL2();
                        var shInputPtr = (SphericalHarmonicsL2*)UnsafeUtility.AddressOf(ref sh);
                        var shOutputPtr = (SphericalHarmonicsL2*)UnsafeUtility.AddressOf(ref output);
                        bool result = WintermuteContext.DeringSphericalHarmonicsL2Internal(shInputPtr, shOutputPtr, 1);
                        Debug.Assert(result);
                        shOutput[probeIdx] = output;
                    }
                }
                return true;
            }

            public void Dispose()
            {
            }
        }
        internal class WintermuteProbePostProcessor : IProbePostProcessor
        {
            public bool Initialize(IDeviceContext context)
            {
                return true;
            }

            public bool ConvolveRadianceToIrradiance(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> radianceIn, BufferSlice<SphericalHarmonicsL2> irradianceOut, int probeCount)
            {
                Debug.Assert(context is WintermuteContext, "Expected WintermuteContext but got something else.");
                if (context is not WintermuteContext wmContext)
                    return false;

                NativeArray<byte> radianceInNativeArray = wmContext.GetNativeArray(radianceIn.Id);
                NativeArray<byte> irradianceOutNativeArray = wmContext.GetNativeArray(irradianceOut.Id);
                var job = new ConvolveJob
                {
                    Radiances = radianceInNativeArray.Reinterpret<SphericalHarmonicsL2>(1),
                    Irradiances = irradianceOutNativeArray.Reinterpret<SphericalHarmonicsL2>(1)
                };
                JobHandle jobHandle = job.Schedule(probeCount, 64);
                jobHandle.Complete();
                return true;
            }

            public bool ConvertToUnityFormat(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> irradianceIn, BufferSlice<SphericalHarmonicsL2> irradianceOut, int probeCount)
            {
                Debug.Assert(context is WintermuteContext, "Expected WintermuteContext but got something else.");
                if (context is not WintermuteContext wmContext)
                    return false;

                NativeArray<byte> irradianceInNativeArray = wmContext.GetNativeArray(irradianceIn.Id);
                NativeArray<byte> irradianceOutNativeArray = wmContext.GetNativeArray(irradianceOut.Id);
                var job = new UnityfyJob
                {
                    IrradianceIn = irradianceInNativeArray.Reinterpret<SphericalHarmonicsL2>(1),
                    IrradianceOut = irradianceOutNativeArray.Reinterpret<SphericalHarmonicsL2>(1)
                };
                JobHandle jobHandle = job.Schedule(probeCount, 64);
                jobHandle.Complete();
                return true;
            }

            public bool AddSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> a, BufferSlice<SphericalHarmonicsL2> b, BufferSlice<SphericalHarmonicsL2> sum, int probeCount)
            {
                Debug.Assert(context is WintermuteContext, "Expected WintermuteContext but got something else.");
                if (context is not WintermuteContext wmContext)
                    return false;

                NativeArray<byte> A = wmContext.GetNativeArray(a.Id);
                NativeArray<byte> B = wmContext.GetNativeArray(b.Id);
                NativeArray<byte> Sum = wmContext.GetNativeArray(sum.Id);
                var job = new AddSHJob
                {
                    A = A.Reinterpret<SphericalHarmonicsL2>(1),
                    B = B.Reinterpret<SphericalHarmonicsL2>(1),
                    Sum = Sum.Reinterpret<SphericalHarmonicsL2>(1)
                };
                JobHandle jobHandle = job.Schedule(probeCount, 64);
                jobHandle.Complete();
                return true;
            }

            public bool ScaleSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount, float scale)
            {
                Debug.Assert(context is WintermuteContext, "Expected WintermuteContext but got something else.");
                if (context is not WintermuteContext wmContext)
                    return false;

                NativeArray<byte> input = wmContext.GetNativeArray(shIn.Id);
                NativeArray<byte> output = wmContext.GetNativeArray(shOut.Id);
                var job = new ScaleSHJob
                {
                    Input = input.Reinterpret<SphericalHarmonicsL2>(1),
                    Scale = scale,
                    Scaled = output.Reinterpret<SphericalHarmonicsL2>(1)
                };
                JobHandle jobHandle = job.Schedule(probeCount, 64);
                jobHandle.Complete();
                return true;
            }

            public bool WindowSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount)
            {
                Debug.Assert(context is WintermuteContext, "Expected WintermuteContext but got something else.");
                if (context is not WintermuteContext wmContext)
                    return false;

                NativeArray<byte> A = wmContext.GetNativeArray(shIn.Id);
                NativeArray<byte> B = wmContext.GetNativeArray(shOut.Id);
                var job = new WindowSHJob
                {
                    Input = A.Reinterpret<SphericalHarmonicsL2>(1),
                    Windowed = B.Reinterpret<SphericalHarmonicsL2>(1)
                };
                JobHandle jobHandle = job.Schedule(probeCount, 64);
                jobHandle.Complete();
                return true;
            }

            public bool DeringSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount)
            {
                Debug.Assert(context is WintermuteContext, "Expected WintermuteContext but got something else.");
                if (context is not WintermuteContext wmContext)
                    return false;

                NativeArray<byte> A = wmContext.GetNativeArray(shIn.Id);
                NativeArray<byte> B = wmContext.GetNativeArray(shOut.Id);
                var job = new DeringSHJob
                {
                    Input = A.Reinterpret<SphericalHarmonicsL2>(1),
                    Output = B.Reinterpret<SphericalHarmonicsL2>(1)
                };
                JobHandle jobHandle = job.Schedule(probeCount, 64);
                jobHandle.Complete();
                return true;
            }

            public void Dispose()
            {
            }
        }
        public class RadeonRaysProbePostProcessor : IProbePostProcessor
        {
            private const int sizeofSphericalHarmonicsL2 = 27 * sizeof(float);

            public bool Initialize(IDeviceContext context)
            {
                Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
                var rrContext = context as RadeonRaysContext;
                return RadeonRaysContext.InitializePostProcessingInternal(rrContext);
            }

            public bool ConvolveRadianceToIrradiance(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> radianceIn, BufferSlice<SphericalHarmonicsL2> irradianceOut, int probeCount)
            {
                Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
                if (context is not RadeonRaysContext rrContext)
                    return false;

                return RadeonRaysContext.ConvolveRadianceToIrradianceInternal(rrContext, radianceIn.Id, irradianceOut.Id, probeCount);
            }

            public bool ConvertToUnityFormat(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> irradianceIn, BufferSlice<SphericalHarmonicsL2> irradianceOut, int probeCount)
            {
                Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
                if (context is not RadeonRaysContext rrContext)
                    return false;

                return RadeonRaysContext.ConvertToUnityFormatInternal(rrContext, irradianceIn.Id, irradianceOut.Id, probeCount);
            }

            public bool AddSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> a, BufferSlice<SphericalHarmonicsL2> b, BufferSlice<SphericalHarmonicsL2> sum, int probeCount)
            {
                Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
                if (context is not RadeonRaysContext rrContext)
                    return false;

                return RadeonRaysContext.AddSphericalHarmonicsL2Internal(rrContext, a.Id, b.Id, sum.Id, probeCount);
            }

            public bool ScaleSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount, float scale)
            {
                Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
                if (context is not RadeonRaysContext rrContext)
                    return false;

                return RadeonRaysContext.ScaleSphericalHarmonicsL2Internal(rrContext, shIn.Id, shOut.Id, probeCount, scale);
            }

            public bool WindowSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount)
            {
                Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
                if (context is not RadeonRaysContext rrContext)
                    return false;

                return RadeonRaysContext.WindowSphericalHarmonicsL2Internal(rrContext, shIn.Id, shOut.Id, probeCount);
            }

            public bool DeringSphericalHarmonicsL2(IDeviceContext context, BufferSlice<SphericalHarmonicsL2> shIn, BufferSlice<SphericalHarmonicsL2> shOut, int probeCount)
            {
                // Read back from GPU memory into CPU memory.
                using var shInputBuffer = new NativeArray<SphericalHarmonicsL2>(probeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                using var shOutputBuffer = new NativeArray<SphericalHarmonicsL2>(probeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                EventID eventId = context.CreateEvent();
                context.ReadBuffer(shIn, shInputBuffer, eventId);
				bool flushResult = context.Flush();
                Debug.Assert(flushResult, "Failed to flush context.");
                bool waitResult = context.Wait(eventId);
                Debug.Assert(waitResult, "Failed to read SH from context.");
                context.DestroyEvent(eventId);

                // Currently windowing is done on CPU since the algorithm is not GPU friendly.
                // Since we aren't attempting to port this to GPU, we are using the jobified CPU version.
                var job = new DeringSHJob
                {
                    Input = shInputBuffer,
                    Output = shOutputBuffer
                };
                JobHandle jobHandle = job.Schedule(probeCount, 64);
                jobHandle.Complete();

                // Write back to GPU.
                eventId = context.CreateEvent();
                context.WriteBuffer(shOut, shOutputBuffer, eventId);
                waitResult = context.Wait(eventId);
                Debug.Assert(waitResult, "Failed to write SH to context.");
                context.DestroyEvent(eventId);
                return true;
            }

            public void Dispose()
            {
            }
        }
    }
}

