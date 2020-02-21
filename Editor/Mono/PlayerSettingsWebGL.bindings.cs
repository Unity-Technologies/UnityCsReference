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
        Throw,
        Ignore
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
            public extern static bool debugSymbols
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)] get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)] set;
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
        }
    }
}
