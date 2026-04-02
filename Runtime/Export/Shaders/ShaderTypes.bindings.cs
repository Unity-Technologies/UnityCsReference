// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine.Bindings;

namespace UnityEngine
{
namespace Shaders
{
    public enum ShaderType
    {
        Graphics, FirstStage = Graphics,
        Compute,
        RayTracing,
        // Surface is hidden

        Count = 4,
    }

    public enum ShaderStage
    {
        Vertex, FirstStage = Vertex,
        Fragment,
        Hull,
        Domain,
        Geometry,
        Compute,
        RayTracing,

        Count,
        GraphicsStageCount = Compute,
    }

    [Flags]
    public enum ShaderStageFlags
    {
        None = 0,

        Vertex = 1 << ShaderStage.Vertex,
        Fragment = 1 << ShaderStage.Fragment,
        Hull = 1 << ShaderStage.Hull,
        Domain = 1 << ShaderStage.Domain,
        Geometry = 1 << ShaderStage.Geometry,
        Compute = 1 << ShaderStage.Compute,
        RayTracing = 1 << ShaderStage.RayTracing,

        Basic = Vertex | Fragment,
        Tessellation = Hull | Domain,
        Graphics = Basic | Tessellation | Geometry,

        Any = Graphics | Compute | RayTracing,
    }

    [Flags]
    public enum ShaderTypeFlags
    {
        None = 0,

        Graphics = 1 << ShaderType.Graphics,
        Compute = 1 << ShaderType.Compute,
        RayTracing = 1 << ShaderType.RayTracing,

        Any = Graphics | Compute | RayTracing,
    }

    [NativeHeader("Modules/ShaderRuntime/Public/ShaderTypes.h")]
    public sealed class Utility
    {
        extern public static bool IsShaderStageEnabled(ShaderStageFlags flags, ShaderStage stage);
        extern public static bool IsShaderTypeEnabled(ShaderTypeFlags flags, ShaderType type);
        extern public static ShaderStageFlags ShaderStageToFlags(ShaderStage stage);
        extern public static ShaderTypeFlags ShaderTypeToFlags(ShaderType type);
        public static ShaderStage GetPreviousStage(ShaderStage stage) { return --stage; }
        public static ShaderStage GetNextStage(ShaderStage stage) { return ++stage; }
    }
} // namespace Shaders
} // namespace UnityEngine
