// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering
{
    [MovedFrom("UnityEngine.Experimental.Rendering")]
    [NativeHeader("Runtime/Shaders/RayTracing/RayTracingShader.h")]
    [NativeHeader("Runtime/Graphics/RayTracing/RayTracingAccelerationStructure.h")]
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]

    class RayTracingShaderHelpURLAttribute : HelpURLAttribute
    {
        public RayTracingShaderHelpURLAttribute()
            : base(null)
        {
        }
        public override string URL
        {
            get
            {
                return $"https://docs.unity3d.com//{Application.unityVersionVer}.{Application.unityVersionMaj}/Documentation/ScriptReference/Rendering.RayTracingShader.html";
            }
        }
    }

    [RayTracingShaderHelpURLAttribute]
    public sealed partial class RayTracingShader : Object
    {
        public extern float maxRecursionDepth { get; }

        [FreeFunction(Name = "RayTracingShaderScripting::SetFloat", HasExplicitThis = true)]
        extern public void SetFloat(int nameID, float val);

        [FreeFunction(Name = "RayTracingShaderScripting::SetInt", HasExplicitThis = true)]
        extern public void SetInt(int nameID, int val);

        [FreeFunction(Name = "RayTracingShaderScripting::SetVector", HasExplicitThis = true)]
        extern public void SetVector(int nameID, Vector4 val);

        [FreeFunction(Name = "RayTracingShaderScripting::SetMatrix", HasExplicitThis = true)]
        extern public void SetMatrix(int nameID, Matrix4x4 val);

        [FreeFunction(Name = "RayTracingShaderScripting::SetFloatArray", HasExplicitThis = true)]
        extern private void SetFloatArray(int nameID, float[] values);

        [FreeFunction(Name = "RayTracingShaderScripting::SetIntArray", HasExplicitThis = true)]
        extern private void SetIntArray(int nameID, int[] values);

        [FreeFunction(Name = "RayTracingShaderScripting::SetVectorArray", HasExplicitThis = true)]
        extern public void SetVectorArray(int nameID, Vector4[] values);

        [FreeFunction(Name = "RayTracingShaderScripting::SetMatrixArray", HasExplicitThis = true)]
        extern public void SetMatrixArray(int nameID, Matrix4x4[] values);

        [NativeMethod(Name = "RayTracingShaderScripting::SetTexture", HasExplicitThis = true, IsFreeFunction = true)]
        extern public void SetTexture(int nameID, [NotNull] Texture texture);

        [NativeMethod(Name = "RayTracingShaderScripting::SetBuffer", HasExplicitThis = true, IsFreeFunction = true)]
        extern public void SetBuffer(int nameID, [NotNull] ComputeBuffer buffer);

        [NativeMethod(Name = "RayTracingShaderScripting::SetBuffer", HasExplicitThis = true, IsFreeFunction = true)]
        extern private void SetGraphicsBuffer(int nameID, [NotNull] GraphicsBuffer buffer);

        [NativeMethod(Name = "RayTracingShaderScripting::SetBuffer", HasExplicitThis = true, IsFreeFunction = true)]
        extern private void SetGraphicsBufferHandle(int nameID, GraphicsBufferHandle bufferHandle);

        [FreeFunction(Name = "RayTracingShaderScripting::SetConstantBuffer", HasExplicitThis = true)]
        extern private void SetConstantComputeBuffer(int nameID, [NotNull] ComputeBuffer buffer, int offset, int size);

        [FreeFunction(Name = "RayTracingShaderScripting::SetConstantBuffer", HasExplicitThis = true)]
        extern private void SetConstantGraphicsBuffer(int nameID, [NotNull] GraphicsBuffer buffer, int offset, int size);

        [NativeMethod(Name = "RayTracingShaderScripting::SetAccelerationStructure", HasExplicitThis = true, IsFreeFunction = true)]
        extern public void SetAccelerationStructure(int nameID, [NotNull] RayTracingAccelerationStructure accelerationStructure);
        extern public void SetShaderPass(string passName);

        [NativeMethod(Name = "RayTracingShaderScripting::SetTextureFromGlobal", HasExplicitThis = true, IsFreeFunction = true)]
        extern public void SetTextureFromGlobal(int nameID, int globalTextureNameID);

        [NativeMethod(Name = "RayTracingShaderScripting::Dispatch", HasExplicitThis = true, IsFreeFunction = true, ThrowsException = true)]
        extern public void Dispatch(string rayGenFunctionName, int width, int height, int depth, Camera camera = null);

        [NativeMethod(Name = "RayTracingShaderScripting::DispatchIndirect", HasExplicitThis = true, IsFreeFunction = true, ThrowsException = true)]
        extern public void DispatchIndirect(string rayGenFunctionName, [NotNull] GraphicsBuffer argsBuffer, uint argsOffset = 0, Camera camera = null);

        public void SetBuffer(int nameID, GraphicsBuffer buffer)
        {
            SetGraphicsBuffer(nameID, buffer);
        }

        public void SetBuffer(int nameID, GraphicsBufferHandle bufferHandle)
        {
            SetGraphicsBufferHandle(nameID, bufferHandle);
        }

        extern public LocalKeywordSpace keywordSpace { get; }

        [FreeFunction("RayTracingShaderScripting::EnableKeyword", HasExplicitThis = true)]
        extern public void EnableKeyword(string keyword);

        [FreeFunction("RayTracingShaderScripting::DisableKeyword", HasExplicitThis = true)]
        extern public void DisableKeyword(string keyword);

        [FreeFunction("RayTracingShaderScripting::IsKeywordEnabled", HasExplicitThis = true)]
        extern public bool IsKeywordEnabled(string keyword);

        [FreeFunction("RayTracingShaderScripting::EnableKeyword", HasExplicitThis = true)]
        extern private void EnableLocalKeyword(LocalKeyword keyword);

        [FreeFunction("RayTracingShaderScripting::DisableKeyword", HasExplicitThis = true)]
        extern private void DisableLocalKeyword(LocalKeyword keyword);

        [FreeFunction("RayTracingShaderScripting::SetKeyword", HasExplicitThis = true)]
        extern private void SetLocalKeyword(LocalKeyword keyword, bool value);

        [FreeFunction("RayTracingShaderScripting::IsKeywordEnabled", HasExplicitThis = true)]
        extern private bool IsLocalKeywordEnabled(LocalKeyword keyword);

        public void EnableKeyword(in LocalKeyword keyword) { EnableLocalKeyword(keyword); }
        public void DisableKeyword(in LocalKeyword keyword) { DisableLocalKeyword(keyword); }
        public void SetKeyword(in LocalKeyword keyword, bool value) { SetLocalKeyword(keyword, value); }
        public bool IsKeywordEnabled(in LocalKeyword keyword) { return IsLocalKeywordEnabled(keyword); }

        [FreeFunction("RayTracingShaderScripting::GetShaderKeywords", HasExplicitThis = true)] extern private string[] GetShaderKeywords();
        [FreeFunction("RayTracingShaderScripting::SetShaderKeywords", HasExplicitThis = true)] extern private void SetShaderKeywords(string[] names);
        public string[] shaderKeywords { get { return GetShaderKeywords(); } set { SetShaderKeywords(value); } }

        [FreeFunction("RayTracingShaderScripting::GetEnabledKeywords", HasExplicitThis = true)] extern private LocalKeyword[] GetEnabledKeywords();
        [FreeFunction("RayTracingShaderScripting::SetEnabledKeywords", HasExplicitThis = true)] extern private void SetEnabledKeywords(LocalKeyword[] keywords);
        public LocalKeyword[] enabledKeywords { get { return GetEnabledKeywords(); } set { SetEnabledKeywords(value); } }
    }
}
