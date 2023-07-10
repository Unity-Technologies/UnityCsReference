// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.RenderPipelines.Core.Editor")]
namespace UnityEngine.LightTransport
{
    namespace PostProcessing
    {
        internal interface IProbePostProcessor
        {
            bool Initialize(IDeviceContext context);

            bool ConvolveRadianceToIrradiance(IDeviceContext context, BufferSlice radianceIn, BufferSlice irradianceOut, int probeCount);

            // Unity expects the following of the irradiance SH coefficients:
            // 1) For L0 and L1, they must have the SH standard normalization terms folded into them (to avoid doing this multiplication in shader).
            // 2) They must be divided by π for historical reasons.
            // 3) L1 terms must be in yzx order (rather than standard xyz).
            //    This is flipped back in GetShaderConstantsFromNormalizedSH before passed to shader.
            // 4) For L2 we cannot (always) use 1) due to how these basis functions have more than one term.
            //    In the below we just copy Unity's existing logic which is coupled to GetShaderConstantsFromNormalizedSH.
            //    Because we use fC2, fC3 and fC4 directly, the division by π is not needed.
            bool ConvertToUnityFormat(IDeviceContext context, BufferSlice irradianceIn, BufferSlice irradianceOut, int probeCount);

            bool AddSphericalHarmonicsL2(IDeviceContext context, BufferSlice A, BufferSlice B, BufferSlice sum, int probeCount);
        }

        struct SH
        {
            // Notation:
            //                       [L00:  DC]
            //            [L1-1:  y] [L10:   z] [L11:   x]
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
                SphericalHarmonicsL2 irradiance = IrradianceIn[probeIdx];
                var output = new SphericalHarmonicsL2();
                for (int rgb = 0; rgb < 3; ++rgb)
                {
                    // L0
                    float shY0Normalization = Mathf.Sqrt(1.0f / Mathf.PI) / 2.0f;
                    output[rgb, SH.L00] = irradiance[rgb, SH.L00];
                    output[rgb, SH.L00] *= shY0Normalization; // 1)
                    output[rgb, SH.L00] /= Mathf.PI; // 2)

                    // L1
                    float shY1Normalization = Mathf.Sqrt(3.0f / Mathf.PI) / 2.0f;

                    output[rgb, SH.L1_1] = irradiance[rgb, SH.L1_1]; // 3)
                    output[rgb, SH.L1_1] *= shY1Normalization; // 1)
                    output[rgb, SH.L1_1] /= Mathf.PI; // 3 )

                    output[rgb, SH.L10] = irradiance[rgb, SH.L10]; // 3)
                    output[rgb, SH.L10] *= shY1Normalization; // 1)
                    output[rgb, SH.L10] /= Mathf.PI; // 2)

                    output[rgb, SH.L11] = irradiance[rgb, SH.L11]; // 3)
                    output[rgb, SH.L11] *= shY1Normalization; // 1)
                    output[rgb, SH.L11] /= Mathf.PI; // 2)

                    // L2
                    float fC2 = Mathf.Sqrt(15.0f) / (8.0f * Mathf.Sqrt(Mathf.PI));
                    float fC3 = Mathf.Sqrt(5.0f) / (16.0f * Mathf.Sqrt(Mathf.PI));
                    float fC4 = 0.5f * fC2;

                    output[rgb, SH.L2_2] = irradiance[rgb, SH.L2_2] * fC2; // 4)
                    output[rgb, SH.L2_1] = irradiance[rgb, SH.L2_1] * fC2; // 4)
                    output[rgb, SH.L20] = irradiance[rgb, SH.L20] * fC3; // 4)
                    output[rgb, SH.L21] = irradiance[rgb, SH.L21] * fC2; // 4)
                    output[rgb, SH.L22] = irradiance[rgb, SH.L22] * fC4; // 4)

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
        internal class ReferenceProbePostProcessor : IProbePostProcessor
        {
            public bool Initialize(IDeviceContext context)
            {
                return true;
            }

            public bool ConvolveRadianceToIrradiance(IDeviceContext context, BufferSlice radianceIn, BufferSlice irradianceOut, int probeCount)
            {
                Debug.Assert(context is ReferenceContext, "Expected ReferenceContext but got something else.");
                if (context is not ReferenceContext)
                    return false;

                var refContext = context as ReferenceContext;
                var radianceNativeArray = refContext.GetNativeArray(radianceIn.Id);
                var irradianceNativeArray = refContext.GetNativeArray(irradianceOut.Id);
                var Radiances = radianceNativeArray.Reinterpret<SphericalHarmonicsL2>(1);
                var Irradiances = irradianceNativeArray.Reinterpret<SphericalHarmonicsL2>(1);
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

            public bool ConvertToUnityFormat(IDeviceContext context, BufferSlice irradianceIn, BufferSlice irradianceOut, int probeCount)
            {
                Debug.Assert(context is ReferenceContext, "Expected ReferenceContext but got something else.");
                var refContext = context as ReferenceContext;
                if (context is not ReferenceContext)
                    return false;

                var irradianceInNativeArray = refContext.GetNativeArray(irradianceIn.Id);
                var irradianceOutNativeArray = refContext.GetNativeArray(irradianceOut.Id);
                var IrradianceIn = irradianceInNativeArray.Reinterpret<SphericalHarmonicsL2>(1);
                var IrradianceOut = irradianceOutNativeArray.Reinterpret<SphericalHarmonicsL2>(1);
                for (int probeIdx = 0; probeIdx < probeCount; ++probeIdx)
                {
                    SphericalHarmonicsL2 irradiance = IrradianceIn[probeIdx];
                    var output = new SphericalHarmonicsL2();
                    for (int rgb = 0; rgb < 3; ++rgb)
                    {
                        // L0
                        float shY0Normalization = Mathf.Sqrt(1.0f / Mathf.PI) / 2.0f;
                        output[rgb, SH.L00] = irradiance[rgb, SH.L00];
                        output[rgb, SH.L00] *= shY0Normalization; // 1)
                        output[rgb, SH.L00] /= Mathf.PI; // 2)

                        // L1
                        float shY1Normalization = Mathf.Sqrt(3.0f / Mathf.PI) / 2.0f;

                        output[rgb, SH.L1_1] = irradiance[rgb, SH.L1_1]; // 3)
                        output[rgb, SH.L1_1] *= shY1Normalization; // 1)
                        output[rgb, SH.L1_1] /= Mathf.PI; // 3 )

                        output[rgb, SH.L10] = irradiance[rgb, SH.L10]; // 3)
                        output[rgb, SH.L10] *= shY1Normalization; // 1)
                        output[rgb, SH.L10] /= Mathf.PI; // 2)

                        output[rgb, SH.L11] = irradiance[rgb, SH.L11]; // 3)
                        output[rgb, SH.L11] *= shY1Normalization; // 1)
                        output[rgb, SH.L11] /= Mathf.PI; // 2)

                        // L2
                        float fC2 = Mathf.Sqrt(15.0f) / (8.0f * Mathf.Sqrt(Mathf.PI));
                        float fC3 = Mathf.Sqrt(5.0f) / (16.0f * Mathf.Sqrt(Mathf.PI));
                        float fC4 = 0.5f * fC2;

                        output[rgb, SH.L2_2] = irradiance[rgb, SH.L2_2] * fC2; // 4)
                        output[rgb, SH.L2_1] = irradiance[rgb, SH.L2_1] * fC2; // 4)
                        output[rgb, SH.L20] = irradiance[rgb, SH.L20] * fC3; // 4)
                        output[rgb, SH.L21] = irradiance[rgb, SH.L21] * fC2; // 4)
                        output[rgb, SH.L22] = irradiance[rgb, SH.L22] * fC4; // 4)
                    }
                    IrradianceOut[probeIdx] = output;
                }
                return true;
            }

            public bool AddSphericalHarmonicsL2(IDeviceContext context, BufferSlice a, BufferSlice b, BufferSlice sum, int probeCount)
            {
                Debug.Assert(context is ReferenceContext, "Expected ReferenceContext but got something else.");
                if (context is not ReferenceContext)
                    return false;

                var refContext = context as ReferenceContext;
                var A = refContext.GetNativeArray(a.Id);
                var B = refContext.GetNativeArray(b.Id);
                var Sum = refContext.GetNativeArray(sum.Id);
                var shA = A.Reinterpret<SphericalHarmonicsL2>(1);
                var shB = B.Reinterpret<SphericalHarmonicsL2>(1);
                var shSum = Sum.Reinterpret<SphericalHarmonicsL2>(1);
                for (int probeIdx = 0; probeIdx < probeCount; ++probeIdx)
                {
                    shSum[probeIdx] = shA[probeIdx] + shB[probeIdx];
                }
                return true;
            }
        }
        internal class WintermuteProbePostProcessor : IProbePostProcessor
        {
            public bool Initialize(IDeviceContext context)
            {
                return true;
            }

            public bool ConvolveRadianceToIrradiance(IDeviceContext context, BufferSlice radianceIn, BufferSlice irradianceOut, int probeCount)
            {
                Debug.Assert(context is WintermuteContext, "Expected WintermuteContext but got something else.");
                var wmContext = context as WintermuteContext;
                if (context is not WintermuteContext)
                    return false;

                var radianceInNativeArray = wmContext.GetNativeArray(radianceIn.Id);
                var irradianceOutNativeArray = wmContext.GetNativeArray(irradianceOut.Id);
                var job = new ConvolveJob
                {
                    Radiances = radianceInNativeArray.Reinterpret<SphericalHarmonicsL2>(1),
                    Irradiances = irradianceOutNativeArray.Reinterpret<SphericalHarmonicsL2>(1)
                };
                JobHandle jobHandle = job.Schedule(probeCount, 64);
                jobHandle.Complete();
                return true;
            }

            public bool ConvertToUnityFormat(IDeviceContext context, BufferSlice irradianceIn, BufferSlice irradianceOut, int probeCount)
            {
                Debug.Assert(context is WintermuteContext, "Expected WintermuteContext but got something else.");
                if (context is not WintermuteContext)
                    return false;

                var wmContext = context as WintermuteContext;
                var irradianceInNativeArray = wmContext.GetNativeArray(irradianceIn.Id);
                var irradianceOutNativeArray = wmContext.GetNativeArray(irradianceOut.Id);
                var job = new UnityfyJob
                {
                    IrradianceIn = irradianceInNativeArray.Reinterpret<SphericalHarmonicsL2>(1),
                    IrradianceOut = irradianceOutNativeArray.Reinterpret<SphericalHarmonicsL2>(1)
                };
                JobHandle jobHandle = job.Schedule(probeCount, 64);
                jobHandle.Complete();
                return true;
            }

            public bool AddSphericalHarmonicsL2(IDeviceContext context, BufferSlice a, BufferSlice b, BufferSlice sum, int probeCount)
            {
                Debug.Assert(context is WintermuteContext, "Expected WintermuteContext but got something else.");
                if (context is not WintermuteContext)
                    return false;

                var wmContext = context as WintermuteContext;
                var A = wmContext.GetNativeArray(a.Id);
                var B = wmContext.GetNativeArray(b.Id);
                var Sum = wmContext.GetNativeArray(sum.Id);
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
        }
        internal class RadeonRaysProbePostProcessor : IProbePostProcessor
        {
            private const int sizeofSphericalHarmonicsL2 = 27 * sizeof(float);

            public bool Initialize(IDeviceContext context)
            {
                Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
                var rrContext = context as RadeonRaysContext;
                return RadeonRaysContext.InitializePostProcessingInternal(rrContext);
            }

            public bool ConvolveRadianceToIrradiance(IDeviceContext context, BufferSlice radianceIn, BufferSlice irradianceOut, int probeCount)
            {
                Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
                if (context is not RadeonRaysContext)
                    return false;

                var rrContext = context as RadeonRaysContext;
                return RadeonRaysContext.ConvolveRadianceToIrradianceInternal(rrContext, radianceIn.Id, irradianceOut.Id, probeCount);
            }

            public bool ConvertToUnityFormat(IDeviceContext context, BufferSlice irradianceIn, BufferSlice irradianceOut, int probeCount)
            {
                Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
                if (context is not RadeonRaysContext)
                    return false;

                var rrContext = context as RadeonRaysContext;
                return RadeonRaysContext.ConvertToUnityFormatInternal(rrContext, irradianceIn.Id, irradianceOut.Id, probeCount);
            }

            public bool AddSphericalHarmonicsL2(IDeviceContext context, BufferSlice a, BufferSlice b, BufferSlice sum, int probeCount)
            {
                Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
                if (context is not RadeonRaysContext)
                    return false;

                var rrContext = context as RadeonRaysContext;
                return RadeonRaysContext.AddSphericalHarmonicsL2Internal(rrContext, a.Id, b.Id, sum.Id, probeCount);
            }
        }
    }
}
