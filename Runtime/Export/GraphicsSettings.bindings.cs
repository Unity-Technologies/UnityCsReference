// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Camera/GraphicsSettings.h")]
    [StaticAccessor("GetGraphicsSettings()", StaticAccessorType.Dot)]
    public sealed partial class GraphicsSettings : Object
    {
        private GraphicsSettings() {}

        extern public static TransparencySortMode   transparencySortMode { get; set; }
        extern public static Vector3                transparencySortAxis { get; set; }

        extern public static bool lightsUseLinearIntensity   { get; set; }
        extern public static bool lightsUseColorTemperature  { get; set; }
        extern public static bool useScriptableRenderPipelineBatching { get; set; }

        extern public static bool HasShaderDefine(GraphicsTier tier, BuiltinShaderDefine defineHash);
        public static bool HasShaderDefine(BuiltinShaderDefine defineHash)
        {
            return HasShaderDefine(Graphics.activeTier, defineHash);
        }

        [NativeName("RenderPipeline")] extern private static ScriptableObject INTERNAL_renderPipelineAsset { get; set; }
        public static RenderPipelineAsset renderPipelineAsset
        {
            get { return INTERNAL_renderPipelineAsset as RenderPipelineAsset; }
            set { INTERNAL_renderPipelineAsset = value; }
        }

        [FreeFunction] extern internal static Object GetGraphicsSettings();

        [NativeName("SetShaderModeScript")]   extern static public void                 SetShaderMode(BuiltinShaderType type, BuiltinShaderMode mode);
        [NativeName("GetShaderModeScript")]   extern static public BuiltinShaderMode    GetShaderMode(BuiltinShaderType type);

        [NativeName("SetCustomShaderScript")] extern static public void     SetCustomShader(BuiltinShaderType type, Shader shader);
        [NativeName("GetCustomShaderScript")] extern static public Shader   GetCustomShader(BuiltinShaderType type);
    }
}
