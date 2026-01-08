// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// Cellular noise ("Worley noise") in 3D in GLSL.
// Copyright (c) Stefan Gustavson 2011-04-19. All rights reserved.
// This code is released under the conditions of the MIT license.
// See LICENSE file for details.
// https://github.com/stegu/webgl-noise

using static Unity.Mathematics.math;

namespace Unity.Mathematics
{
    public static partial class noise
    {
        /// <summary>
        /// 3D Cellular noise ("Worley noise") with a 2x2x2 search window.
        /// </summary>
        /// <remarks>
        /// Faster than using 3x3x3, at the expense of some pattern artifacts. F2 is often wrong and has sharp discontinuities. If you need a smooth F2, use the slower 3x3x3 version.
        /// </remarks>
        /// <param name="P">A point in 3D space.</param>
        /// <returns>Feature points. F1 is in the x component, F2 in the y component.</returns>
        public static float2 cellular2x2x2(float3 P)
        {
            const float K = 0.142857142857f; // 1/7
            const float Ko = 0.428571428571f; // 1/2-K/2
            const float K2 = 0.020408163265306f; // 1/(7*7)
            const float Kz = 0.166666666667f; // 1/6
            const float Kzo = 0.416666666667f; // 1/2-1/6*2
            const float jitter = 0.8f; // smaller jitter gives less errors in F2

            float3 Pi = mod289(floor(P));
            float3 Pf = frac(P);
            float4 Pfx = Pf.x + float4(0.0f, -1.0f, 0.0f, -1.0f);
            float4 Pfy = Pf.y + float4(0.0f, 0.0f, -1.0f, -1.0f);
            float4 p = permute(Pi.x + float4(0.0f, 1.0f, 0.0f, 1.0f));
            p = permute(p + Pi.y + float4(0.0f, 0.0f, 1.0f, 1.0f));
            float4 p1 = permute(p + Pi.z); // z+0
            float4 p2 = permute(p + Pi.z + float4(1.0f,1.0f,1.0f,1.0f)); // z+1
            float4 ox1 = frac(p1 * K) - Ko;
            float4 oy1 = mod7(floor(p1 * K)) * K - Ko;
            float4 oz1 = floor(p1 * K2) * Kz - Kzo; // p1 < 289 guaranteed
            float4 ox2 = frac(p2 * K) - Ko;
            float4 oy2 = mod7(floor(p2 * K)) * K - Ko;
            float4 oz2 = floor(p2 * K2) * Kz - Kzo;
            float4 dx1 = Pfx + jitter * ox1;
            float4 dy1 = Pfy + jitter * oy1;
            float4 dz1 = Pf.z + jitter * oz1;
            float4 dx2 = Pfx + jitter * ox2;
            float4 dy2 = Pfy + jitter * oy2;
            float4 dz2 = Pf.z - 1.0f + jitter * oz2;
            float4 d1 = dx1 * dx1 + dy1 * dy1 + dz1 * dz1; // z+0
            float4 d2 = dx2 * dx2 + dy2 * dy2 + dz2 * dz2; // z+1

            // Sort out the two smallest distances (F1, F2)

            // Do it right and sort out both F1 and F2
            float4 d = min(d1,d2); // F1 is now in d
            d2 = max(d1,d2); // Make sure we keep all candidates for F2
            d.xy = (d.x < d.y) ? d.xy : d.yx; // Swap smallest to d.x
            d.xz = (d.x < d.z) ? d.xz : d.zx;
            d.xw = (d.x < d.w) ? d.xw : d.wx; // F1 is now in d.x
            d.yzw = min(d.yzw, d2.yzw); // F2 now not in d2.yzw
            d.y = min(d.y, d.z); // nor in d.z
            d.y = min(d.y, d.w); // nor in d.w
            d.y = min(d.y, d2.x); // F2 is now in d.y
            return sqrt(d.xy); // F1 and F2
        }
    }
}
