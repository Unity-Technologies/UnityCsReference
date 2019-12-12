// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.VFX
{
    [RequiredByNativeCode]
    [NativeType(Header = "Modules/VFX/Public/VFXExpressionNoiseFunctions.h")]
    [StaticAccessor("VFXExpressionNoiseFunctions", StaticAccessorType.DoubleColon)]
    internal class VFXExpressionNoise
    {
        [NativeName("Value::Generate")]
        extern static internal Vector2 GenerateValueNoise1D(float coordinate, float frequency, int octaveCount, float persistence, float lacunarity);
        [NativeName("Value::Generate")]
        extern static internal Vector3 GenerateValueNoise2D(Vector2 coordinate, float frequency, int octaveCount, float persistence, float lacunarity);
        [NativeName("Value::Generate")]
        extern static internal Vector4 GenerateValueNoise3D(Vector3 coordinate, float frequency, int octaveCount, float persistence, float lacunarity);

        [NativeName("Value::GenerateCurl")]
        extern static internal Vector2 GenerateValueCurlNoise2D(Vector2 coordinate, float frequency, int octaveCount, float persistence, float lacunarity);
        [NativeName("Value::GenerateCurl")]
        extern static internal Vector3 GenerateValueCurlNoise3D(Vector3 coordinate, float frequency, int octaveCount, float persistence, float lacunarity);

        [NativeName("Perlin::Generate")]
        extern static internal Vector2 GeneratePerlinNoise1D(float coordinate, float frequency, int octaveCount, float persistence, float lacunarity);
        [NativeName("Perlin::Generate")]
        extern static internal Vector3 GeneratePerlinNoise2D(Vector2 coordinate, float frequency, int octaveCount, float persistence, float lacunarity);
        [NativeName("Perlin::Generate")]
        extern static internal Vector4 GeneratePerlinNoise3D(Vector3 coordinate, float frequency, int octaveCount, float persistence, float lacunarity);

        [NativeName("Perlin::GenerateCurl")]
        extern static internal Vector2 GeneratePerlinCurlNoise2D(Vector2 coordinate, float frequency, int octaveCount, float persistence, float lacunarity);
        [NativeName("Perlin::GenerateCurl")]
        extern static internal Vector3 GeneratePerlinCurlNoise3D(Vector3 coordinate, float frequency, int octaveCount, float persistence, float lacunarity);

        [NativeName("Cellular::Generate")]
        extern static internal Vector2 GenerateCellularNoise1D(float coordinate, float frequency, int octaveCount, float persistence, float lacunarity);
        [NativeName("Cellular::Generate")]
        extern static internal Vector3 GenerateCellularNoise2D(Vector2 coordinate, float frequency, int octaveCount, float persistence, float lacunarity);
        [NativeName("Cellular::Generate")]
        extern static internal Vector4 GenerateCellularNoise3D(Vector3 coordinate, float frequency, int octaveCount, float persistence, float lacunarity);

        [NativeName("Cellular::GenerateCurl")]
        extern static internal Vector2 GenerateCellularCurlNoise2D(Vector2 coordinate, float frequency, int octaveCount, float persistence, float lacunarity);
        [NativeName("Cellular::GenerateCurl")]
        extern static internal Vector3 GenerateCellularCurlNoise3D(Vector3 coordinate, float frequency, int octaveCount, float persistence, float lacunarity);

        [NativeName("Voro::Generate")]
        extern static internal float GenerateVoroNoise2D(Vector2 coordinate, float frequency, float warp, float smoothness);
    }
}
