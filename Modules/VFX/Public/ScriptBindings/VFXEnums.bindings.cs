// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace UnityEngine.VFX
{
    public enum VFXSpace
    {
        None = -1,
        Local = 0,
        World = 1,
    }

    [Flags]
    internal enum VFXCullingFlags
    {
        CullNone = 0,
        CullSimulation = 1 << 0,
        CullBoundsUpdate = 1 << 1,
        CullDefault = CullSimulation | CullBoundsUpdate,
    }

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
        FrameIndex,
        PlayRate,
        UnscaledDeltaTime,
        ManagerMaxDeltaTime,
        ManagerFixedTimeStep,

        //Game time manager access (built-in values)
        GameDeltaTime,
        GameUnscaledDeltaTime,
        GameSmoothDeltaTime,
        GameTotalTime,
        GameUnscaledTotalTime,
        GameTotalTimeSinceSceneLoad,
        GameTimeScale,

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
        TransformVector4,

        Vector3sToMatrix,
        Vector4sToMatrix,
        MatrixToVector3s,
        MatrixToVector4s,

        // Sampling and baking
        SampleCurve,
        SampleGradient,

        SampleMeshVertexFloat,
        SampleMeshVertexFloat2,
        SampleMeshVertexFloat3,
        SampleMeshVertexFloat4,
        SampleMeshVertexColor,

        SampleMeshIndex,
        VertexBufferFromMesh,
        VertexBufferFromSkinnedMeshRenderer,
        IndexBufferFromMesh,
        MeshFromSkinnedMeshRenderer,
        RootBoneTransformFromSkinnedMeshRenderer,

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

        CastIntToBool,
        CastUintToBool,
        CastFloatToBool,
        CastBoolToInt,
        CastBoolToUint,
        CastBoolToFloat,

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
        ExtractScaledPixelDimensionsFromMainCamera,
        ExtractLensShiftFromMainCamera,
        GetBufferFromMainCamera,
        IsMainCameraOrthographic,
        GetOrthographicSizeFromMainCamera,

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

        // Mesh
        MeshVertexCount,
        MeshChannelOffset,
        MeshChannelInfos,
        MeshVertexStride,
        MeshIndexCount,
        MeshIndexFormat,

        // Buffer
        BufferStride,
        BufferCount,

        TextureWidth,
        TextureHeight,
        TextureDepth,
        TextureFormat,

        // Event attribute in spawner
        ReadEventAttribute,

        // Spawner state accessors
        SpawnerStateNewLoop,
        SpawnerStateLoopState,
        SpawnerStateSpawnCount,
        SpawnerStateDeltaTime,
        SpawnerStateTotalTime,
        SpawnerStateDelayBeforeLoop,
        SpawnerStateLoopDuration,
        SpawnerStateDelayAfterLoop,
        SpawnerStateLoopIndex,
        SpawnerStateLoopCount,
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
        CameraBuffer,
        Matrix4x4,
        Curve,
        ColorGradient,
        Mesh,
        Spline,
        Boolean,
        Buffer,
        SkinnedMeshRenderer
    }

    internal enum VFXTaskType
    {
        None = 0,

        Spawner     = 0x10000000,
        Initialize  = 0x20000000,
        Update      = 0x30000000,
        Output      = 0x40000000,

        // updates
        CameraSort                  = Update | 1,
        PerCameraUpdate             = Update | 2,
        PerCameraSort               = Update | 3, //deprecated (since sortingKeys), here for compatibility
        PerOutputSort               = Update | 4,
        GlobalSort                  = Update | 5,


        // outputs
        ParticlePointOutput         = Output | 0,
        ParticleLineOutput          = Output | 1,
        ParticleQuadOutput          = Output | 2,
        ParticleHexahedronOutput    = Output | 3,
        ParticleMeshOutput          = Output | 4,
        ParticleTriangleOutput      = Output | 5,
        ParticleOctagonOutput       = Output | 6,

        // spawners
        ConstantRateSpawner         = Spawner | 0,
        BurstSpawner                = Spawner | 1,
        PeriodicBurstSpawner        = Spawner | 2,
        VariableRateSpawner         = Spawner | 3,
        CustomCallbackSpawner       = Spawner | 4,
        SetAttributeSpawner         = Spawner | 5,
        EvaluateExpressionsSpawner  = Spawner | 6
    }

    internal enum VFXSystemType
    {
        Spawner,
        Particle,
        Mesh,
        OutputEvent
    }

    internal enum VFXSystemFlag
    {
        SystemDefault = 0,
        SystemHasKill = 1 << 0,
        SystemHasIndirectBuffer = 1 << 1,
        SystemReceivedEventGPU = 1 << 2,
        SystemHasStrips = 1 << 3,
        SystemNeedsComputeBounds = 1 << 4,
        SystemAutomaticBounds = 1 << 5,
        SystemInWorldSpace = 1 << 6,
        SystemHasDirectLink = 1 << 7,
        SystemHasAttributeBuffer = 1 << 8,
        SystemUsesInstancedRendering = 1 << 9,
        SystemIsRayTraced = 1 << 10,
    }

    [Flags]
    internal enum VFXUpdateMode
    {
        FixedDeltaTime = 0,
        DeltaTime = 1 << 0,
        IgnoreTimeScale = 1 << 1,
        ExactFixedTimeStep = 1 << 2,

        //Following line is only for UI compatibility
        //This entry can be removed once a new package has been released
        //It provides a way to access to all option without changing C# package code
        DeltaTimeAndIgnoreTimeScale = DeltaTime | IgnoreTimeScale,
        FixedDeltaAndExactTime = FixedDeltaTime | ExactFixedTimeStep, //Actually equals to ExactFixedTimeStep
        FixedDeltaAndExactTimeAndIgnoreTimeScale = FixedDeltaTime | ExactFixedTimeStep | IgnoreTimeScale
    }

    [Flags]
    public enum VFXCameraBufferTypes
    {
        None = 0,
        Depth = 1 << 0,
        Color = 1 << 1,
        Normal = 1 << 2,
    }

    internal enum VFXInstancingMode
    {
        Disabled = -1,
        [InspectorName("Automatic batch capacity")]
        Auto = 0,
        [InspectorName("Custom batch capacity")]
        Custom
    };

    [Flags]
    internal enum VFXInstancingDisabledReason
    {
        None = 0,
        [Description("A system is using indirect draw.")]
        IndirectDraw = 1 << 0,
        [Description("The effect is using output events.")]
        OutputEvent = 1 << 1,
        [Description("The effect is using GPU events.")]
        GPUEvent = 1 << 2,
        [Description("An Initialize node has Bounds Mode set to 'Automatic'.")]
        AutomaticBounds = 1 << 3,
        [Description("The effect contains a mesh output.")]
        MeshOutput = 1 << 4,
        [Description("The effect has exposed texture, mesh or graphics buffer properties.")]
        ExposedObject = 1 << 5,
        [Description("Unknown reason.")]
        Unknown = -1
    };

    internal enum VFXMainCameraBufferFallback
    {
        NoFallback,
        PreferMainCamera,
        PreferSceneCamera
    };

    internal enum VFXSkinnedMeshFrame
    {
        Current = 0,
        Previous = 1,
    };

    internal enum VFXSkinnedTransform
    {
        LocalRootBoneTransform = 0,
        WorldRootBoneTransform = 1,
    };
}
