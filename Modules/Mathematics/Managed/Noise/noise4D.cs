// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Description : Array and textureless GLSL 2D/3D/4D simplex
//               noise functions.
//      Author : Ian McEwan, Ashima Arts.
//  Maintainer : stegu
//     Lastmath.mod : 20110822 (ijm)
//     License : Copyright (C) 2011 Ashima Arts. All rights reserved.
//               Distributed under the MIT License. See LICENSE file.
//               https://github.com/ashima/webgl-noise
//               https://github.com/stegu/webgl-noise
//

using static Unity.Mathematics.math;

namespace Unity.Mathematics
{
    public static partial class noise
    {
        /// <summary>
        /// Simplex noise.
        /// </summary>
        /// <param name="v">Input coordinate.</param>
        /// <returns>Noise value.</returns>
        public static float snoise(float4 v)
        {
            // (math.sqrt(5) - 1)/4 = F4, used once below
            const float F4 = 0.309016994374947451f;
            float4 C = float4( 0.138196601125011f,  // (5 - math.sqrt(5))/20  G4
                0.276393202250021f,  // 2 * G4
                0.414589803375032f,  // 3 * G4
                -0.447213595499958f); // -1 + 4 * G4

            // First corner
            float4 i  = floor(v + dot(v, float4(F4)) );
            float4 x0 = v -   i + dot(i, C.xxxx);

            // Other corners

            // Rank sorting originally contributed by Bill Licea-Kane, AMD (formerly ATI)
            float4 i0 = float4(0.0f);
            float3 isX = step( x0.yzw, x0.xxx );
            float3 isYZ = step( x0.zww, x0.yyz );
            //  i0.x = math.dot( isX, float3( 1.0 ) );
            i0.x = isX.x + isX.y + isX.z;
            i0.yzw = 1.0f - isX;
            //  i0.y += math.dot( isYZ.xy, float2( 1.0 ) );
            i0.y += isYZ.x + isYZ.y;
            i0.zw += 1.0f - isYZ.xy;
            i0.z += isYZ.z;
            i0.w += 1.0f - isYZ.z;

            // i0 now contains the unique values 0,1,2,3 in each channel
            float4 i3 = clamp( i0, 0.0f, 1.0f );
            float4 i2 = clamp( i0-1.0f, 0.0f, 1.0f );
            float4 i1 = clamp( i0-2.0f, 0.0f, 1.0f );

            //  x0 = x0 - 0.0 + 0.0 * C.xxxx
            //  x1 = x0 - i1  + 1.0 * C.xxxx
            //  x2 = x0 - i2  + 2.0 * C.xxxx
            //  x3 = x0 - i3  + 3.0 * C.xxxx
            //  x4 = x0 - 1.0 + 4.0 * C.xxxx
            float4 x1 = x0 - i1 + C.xxxx;
            float4 x2 = x0 - i2 + C.yyyy;
            float4 x3 = x0 - i3 + C.zzzz;
            float4 x4 = x0 + C.wwww;

            // Permutations
            i = mod289(i);
            float j0 = permute( permute( permute( permute(i.w) + i.z) + i.y) + i.x);
            float4 j1 = permute( permute( permute( permute (
                                                       i.w + float4(i1.w, i2.w, i3.w, 1.0f ))
                                                   + i.z + float4(i1.z, i2.z, i3.z, 1.0f ))
                                          + i.y + float4(i1.y, i2.y, i3.y, 1.0f ))
                                 + i.x + float4(i1.x, i2.x, i3.x, 1.0f ));

            // Gradients: 7x7x6 points over a cube, mapped onto a 4-cross polytope
            // 7*7*6 = 294, which is close to the ring size 17*17 = 289.
            float4 ip = float4(1.0f/294.0f, 1.0f/49.0f, 1.0f/7.0f, 0.0f) ;

            float4 p0 = grad4(j0,   ip);
            float4 p1 = grad4(j1.x, ip);
            float4 p2 = grad4(j1.y, ip);
            float4 p3 = grad4(j1.z, ip);
            float4 p4 = grad4(j1.w, ip);

            // Normalise gradients
            float4 norm = taylorInvSqrt(float4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
            p0 *= norm.x;
            p1 *= norm.y;
            p2 *= norm.z;
            p3 *= norm.w;
            p4 *= taylorInvSqrt(dot(p4,p4));

            // Mix contributions from the five corners
            float3 m0 = max(0.6f - float3(dot(x0,x0), dot(x1,x1), dot(x2,x2)), 0.0f);
            float2 m1 = max(0.6f - float2(dot(x3,x3), dot(x4,x4)            ), 0.0f);
            m0 = m0 * m0;
            m1 = m1 * m1;
            return 49.0f * ( dot(m0*m0, float3( dot( p0, x0 ), dot( p1, x1 ), dot( p2, x2 )))
                            + dot(m1*m1, float2( dot( p3, x3 ), dot( p4, x4 ) ) ) ) ;

        }
    }
}
