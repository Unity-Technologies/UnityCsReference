// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IL2CPP.CompilerServices;
using static Unity.Mathematics.math;

namespace Unity.Mathematics
{
    /// <summary>
    /// A static class containing noise functions.
    /// </summary>
    [Il2CppEagerStaticClassConstruction]
    public static partial class noise
    {
        // Modulo 289 without a division (only multiplications)
        static float  mod289(float x)  { return x - floor(x * (1.0f / 289.0f)) * 289.0f; }
        static float2 mod289(float2 x) { return x - floor(x * (1.0f / 289.0f)) * 289.0f; }
        static float3 mod289(float3 x) { return x - floor(x * (1.0f / 289.0f)) * 289.0f; }
        static float4 mod289(float4 x) { return x - floor(x * (1.0f / 289.0f)) * 289.0f; }

        // Modulo 7 without a division
        static float3 mod7(float3 x) { return x - floor(x * (1.0f / 7.0f)) * 7.0f; }
        static float4 mod7(float4 x) { return x - floor(x * (1.0f / 7.0f)) * 7.0f; }

        // Permutation polynomial: (34x^2 + x) math.mod 289
        static float  permute(float x)  { return mod289((34.0f * x + 1.0f) * x); }
        static float3 permute(float3 x) { return mod289((34.0f * x + 1.0f) * x); }
        static float4 permute(float4 x) { return mod289((34.0f * x + 1.0f) * x); }

        static float  taylorInvSqrt(float r)  { return 1.79284291400159f - 0.85373472095314f * r; }
        static float4 taylorInvSqrt(float4 r) { return 1.79284291400159f - 0.85373472095314f * r; }

        static float2 fade(float2 t) { return t*t*t*(t*(t*6.0f-15.0f)+10.0f); }
        static float3 fade(float3 t) { return t*t*t*(t*(t*6.0f-15.0f)+10.0f); }
        static float4 fade(float4 t) { return t*t*t*(t*(t*6.0f-15.0f)+10.0f); }

        static float4 grad4(float j, float4 ip)
        {
            float4 ones = float4(1.0f, 1.0f, 1.0f, -1.0f);
            float3 pxyz = floor(frac(float3(j) * ip.xyz) * 7.0f) * ip.z - 1.0f;
            float  pw   = 1.5f - dot(abs(pxyz), ones.xyz);
            float4 p = float4(pxyz, pw);
            float4 s = float4(p < 0.0f);
            p.xyz = p.xyz + (s.xyz*2.0f - 1.0f) * s.www;
            return p;
        }

        // Hashed 2-D gradients with an extra rotation.
        // (The constant 0.0243902439 is 1/41)
        static float2 rgrad2(float2 p, float rot)
        {
            // For more isotropic gradients, math.sin/math.cos can be used instead.
            float u = permute(permute(p.x) + p.y) * 0.0243902439f + rot; // Rotate by shift
            u = frac(u) * 6.28318530718f; // 2*pi
            return float2(cos(u), sin(u));
        }
    }
}
