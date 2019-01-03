// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.VFX
{
    [Flags]
    internal enum VFXCullingFlags
    {
        CullNone = 0,
        CullSimulation = 1 << 0,
        CullBoundsUpdate = 1 << 1,
        CullDefault = CullSimulation | CullBoundsUpdate,
    };

    internal enum VFXExpressionOperation
    {
        // no-op
        None,

        // Value, combine, extract
        Value,

        Combine2f,
        Combine3f,
        Combine4f,

        ExtractComponent,

        // built-in values
        DeltaTime,
        TotalTime,
        SystemSeed,
        LocalToWorld,
        WorldToLocal,

        // float math operations
        // unary
        Sin,
        Cos,
        Tan,
        ASin,
        ACos,
        ATan,
        Abs,
        Sign,
        Saturate,
        Ceil,
        Round,
        Frac,
        Floor,
        Log2,
        // binary
        Mul,
        Divide,
        Add,
        Subtract,
        Min,
        Max,
        Pow,
        ATan2,

        // matrix operations
        TRSToMatrix,
        InverseMatrix,
        InverseTRSMatrix,
        TransposeMatrix,
        ExtractPositionFromMatrix,
        ExtractAnglesFromMatrix,
        ExtractScaleFromMatrix,

        TransformMatrix,
        TransformPos,
        TransformVec,
        TransformDir,

        Vector3sToMatrix,
        Vector4sToMatrix,
        MatrixToVector3s,
        MatrixToVector4s,

        // Sampling and baking
        SampleCurve,
        SampleGradient,
        BakeCurve,
        BakeGradient,

        // Bit wise operations
        BitwiseLeftShift,
        BitwiseRightShift,
        BitwiseOr,
        BitwiseAnd,
        BitwiseXor,
        BitwiseComplement,

        // Cast operations
        CastUintToFloat,
        CastIntToFloat,
        CastFloatToUint,
        CastIntToUint,
        CastFloatToInt,
        CastUintToInt,

        // Color transformations
        RGBtoHSV,
        HSVtoRGB,

        // Flow
        Condition,
        Branch,

        // Random
        GenerateRandom,
        GenerateFixedRandom,

        // Camera operations
        ExtractMatrixFromMainCamera,
        ExtractFOVFromMainCamera,
        ExtractNearPlaneFromMainCamera,
        ExtractFarPlaneFromMainCamera,
        ExtractAspectRatioFromMainCamera,
        ExtractPixelDimensionsFromMainCamera,

        GetBufferFromMainCamera,

        // Logical operations
        LogicalAnd,
        LogicalOr,
        LogicalNot,

        // Noise
        ValueNoise1D,
        ValueNoise2D,
        ValueNoise3D,
        ValueCurlNoise2D,
        ValueCurlNoise3D,

        PerlinNoise1D,
        PerlinNoise2D,
        PerlinNoise3D,
        PerlinCurlNoise2D,
        PerlinCurlNoise3D,

        CellularNoise1D,
        CellularNoise2D,
        CellularNoise3D,
        CellularCurlNoise2D,
        CellularCurlNoise3D,

        VoroNoise2D,
    }

    internal enum VFXValueType
    {
        None,
        Float,
        Float2,
        Float3,
        Float4,
        Int32,
        Uint32,
        Texture2D,
        Texture2DArray,
        Texture3D,
        TextureCube,
        TextureCubeArray,
        Matrix4x4,
        Curve,
        ColorGradient,
        Mesh,
        Spline,
        Boolean
    }

    internal enum VFXTaskType
    {
        None = 0,

        Spawner     = 0x10000000,
        Initialize  = 0x20000000,
        Update      = 0x30000000,
        Output      = 0x40000000,

        // updates
        CameraSort                  = Update | 1, // TMP

        // outputs
        ParticlePointOutput         = Output | 0,
        ParticleLineOutput          = Output | 1,
        ParticleQuadOutput          = Output | 2,
        ParticleHexahedronOutput    = Output | 3,
        ParticleMeshOutput          = Output | 4,

        // spawners
        ConstantRateSpawner         = Spawner | 0,
        BurstSpawner                = Spawner | 1,
        PeriodicBurstSpawner        = Spawner | 2,
        VariableRateSpawner         = Spawner | 3,
        CustomCallbackSpawner       = Spawner | 4,
        SetAttributeSpawner         = Spawner | 5,
    };

    internal enum VFXSystemType
    {
        Spawner,
        Particle,
        Mesh
    }

    internal enum VFXSystemFlag
    {
        SystemDefault = 0,
        SystemHasKill = 1 << 0,
        SystemHasIndirectBuffer = 1 << 1,
        SystemReceivedEventGPU = 1 << 2
    }

    internal enum VFXUpdateMode
    {
        FixedDeltaTime,
        DeltaTime,
    }

    [Flags]
    public enum VFXCameraBufferTypes
    {
        None = 0,
        Depth = 1 << 0,
        Color = 1 << 1,
        Normal = 1 << 2,
    }
}
