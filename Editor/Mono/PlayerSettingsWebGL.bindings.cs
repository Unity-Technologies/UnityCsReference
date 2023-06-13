// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System;

namespace UnityEditor
{
    public enum WebGLExceptionSupport
    {
        None = 0,
        ExplicitlyThrownExceptionsOnly = 1,
        FullWithoutStacktrace = 2,
        FullWithStacktrace = 3
    }

    public enum WebGLCompressionFormat
    {
        Brotli,
        Gzip,
        Disabled
    }

    public enum WebGLLinkerTarget
    {
        Asm,
        Wasm,
        [Obsolete("WebGLLinkerTarget.Both mode is no longer supported. Instead you can create separate asm.js and WebAssembly builds and download the appropriate one depending on the browser capabilities.", true)]
        Both
    }

    public enum WebGLWasmArithmeticExceptions
    {
        [Obsolete("WebGLWasmArithmeticExceptions.Throw mode is no longer supported. WebAssembly arithmetic exceptions are always ignored.")]
        Throw,
        Ignore
    }

    public enum WebGLDebugSymbolMode
    {
        Off = 0,
        External = 1,
        Embedded = 2
    }

    public enum WebGLMemoryGrowthMode
    {
        None = 0,
        Linear = 1,
        Geometric = 2
    }

    public enum WebGLPowerPreference
    {
        Default = 0,
        LowPower = 1,
        HighPerformance = 2
    }

    public sealed partial class PlayerSettings : UnityEngine.Object
    {
        [NativeHeader("Editor/Mono/PlayerSettingsWebGL.bindings.h")]
        public sealed class WebGL
        {
            [NativeProperty("webGLMemorySize", TargetType.Field)]
            public extern static int memorySize
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLExceptionSupport", TargetType.Field)]
            public extern static WebGLExceptionSupport exceptionSupport
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLDataCaching", TargetType.Field)]
            public extern static bool dataCaching
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("WebGLEmscriptenArgs")]
            public extern static string emscriptenArgs
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("WebGLModulesDirectory")]
            public extern static string modulesDirectory
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("WebGLTemplate")]
            public extern static string template
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLAnalyzeBuildSize", TargetType.Field)]
            public extern static bool analyzeBuildSize
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLUseEmbeddedResources", TargetType.Field)]
            public extern static bool useEmbeddedResources
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [Obsolete("useWasm Property deprecated. Use linkerTarget instead")]
            public static bool useWasm
            {
                get { return linkerTarget != WebGLLinkerTarget.Asm; }
                set { linkerTarget = value ? WebGLLinkerTarget.Both : WebGLLinkerTarget.Asm; }
            }

            [NativeProperty("webGLThreadsSupport", TargetType.Field)]
            public extern static bool threadsSupport
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLLinkerTarget", TargetType.Field)]
            public extern static WebGLLinkerTarget linkerTarget
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLCompressionFormat", TargetType.Field)]
            public extern static WebGLCompressionFormat compressionFormat
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLNameFilesAsHashes", TargetType.Field)]
            public extern static bool nameFilesAsHashes
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLDebugSymbols", TargetType.Field)]
            public extern static WebGLDebugSymbolMode debugSymbolMode
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLShowDiagnostics", TargetType.Field)]
            public extern static bool showDiagnostics
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [Obsolete("debugSymbols Property deprecated. Property has been replaced by debugSymbolMode property.", false)]
            public static bool debugSymbols
            {
                get { return debugSymbolMode != WebGLDebugSymbolMode.Off; }
                set { debugSymbolMode = value ? WebGLDebugSymbolMode.External : WebGLDebugSymbolMode.Off; }
            }

            [Obsolete("wasmStreaming Property deprecated. WebAssembly streaming will be automatically used when decompressionFallback is disabled and vice versa.", true)]
            [NativeProperty("webGLWasmStreaming", TargetType.Field)]
            public extern static bool wasmStreaming
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLDecompressionFallback", TargetType.Field)]
            public extern static bool decompressionFallback
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLWasmArithmeticExceptions", TargetType.Field)]
            public extern static WebGLWasmArithmeticExceptions wasmArithmeticExceptions
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLInitialMemorySize", TargetType.Field)]
            public extern static int initialMemorySize
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLMaximumMemorySize", TargetType.Field)]
            public extern static int maximumMemorySize
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLMemoryGrowthMode", TargetType.Field)]
            public extern static WebGLMemoryGrowthMode memoryGrowthMode
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLMemoryLinearGrowthStep", TargetType.Field)]
            public extern static int linearMemoryGrowthStep
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLMemoryGeometricGrowthStep", TargetType.Field)]
            public extern static float geometricMemoryGrowthStep
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLMemoryGeometricGrowthCap", TargetType.Field)]
            public extern static int memoryGeometricGrowthCap
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLEnableWebGPU", TargetType.Field)]
            public extern static bool enableWebGPU
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLPowerPreference", TargetType.Field)]
            public extern static WebGLPowerPreference powerPreference
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLWebAssemblyTable", TargetType.Field)]
            public extern static bool webAssemblyTable
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLWebAssemblyBigInt", TargetType.Field)]
            public extern static bool webAssemblyBigInt
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }

            [NativeProperty("webGLCloseOnQuit", TargetType.Field)]
            public extern static bool closeOnQuit
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
            }
        }
    }
}
