// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.VFX
{
    [RequiredByNativeCode]
    [NativeType(Header = "Modules/VFX/Public/VFXExpressionNoiseFunctions.h")]
    [StaticAccessor("VFXExpressionNoiseFunctions", StaticAccessorType.DoubleColon)]
    internal class VFXExpressionNoise
    {
        [NativeName("Value::Generate")]
        extern static internal float GenerateValueNoise1D(float coordinate, float amplitude, float frequency, int octaveCount, float persistence);
        [NativeName("Value::Generate")]
        extern static internal float GenerateValueNoise2D(Vector2 coordinate, float amplitude, float frequency, int octaveCount, float persistence);
        [NativeName("Value::Generate")]
        extern static internal float GenerateValueNoise3D(Vector3 coordinate, float amplitude, float frequency, int octaveCount, float persistence);

        [NativeName("Perlin::Generate")]
        extern static internal float GeneratePerlinNoise1D(float coordinate, float amplitude, float frequency, int octaveCount, float persistence);
        [NativeName("Perlin::Generate")]
        extern static internal float GeneratePerlinNoise2D(Vector2 coordinate, float amplitude, float frequency, int octaveCount, float persistence);
        [NativeName("Perlin::Generate")]
        extern static internal float GeneratePerlinNoise3D(Vector3 coordinate, float amplitude, float frequency, int octaveCount, float persistence);

        [NativeName("Simplex::Generate")]
        extern static internal float GenerateSimplexNoise1D(float coordinate, float amplitude, float frequency, int octaveCount, float persistence);
        [NativeName("Simplex::Generate")]
        extern static internal float GenerateSimplexNoise2D(Vector2 coordinate, float amplitude, float frequency, int octaveCount, float persistence);
        [NativeName("Simplex::Generate")]
        extern static internal float GenerateSimplexNoise3D(Vector3 coordinate, float amplitude, float frequency, int octaveCount, float persistence);

        [NativeName("Voro::Generate")]
        extern static internal float GenerateVoroNoise2D(Vector2 coordinate, float amplitude, float frequency, float warp, float smoothness);
    }
}
