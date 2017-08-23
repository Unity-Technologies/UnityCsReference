// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/Mesh/MeshScriptBindings.h")]
    public sealed partial class Mesh
    {
        extern public   bool isReadable {[NativeMethod(Name = "GetIsReadable")] get; }
        extern internal bool canAccess  {[NativeMethod(Name = "CanAccessFromScript")] get; }

        extern public int vertexCount   {[NativeMethod(Name = "GetVertexCount")] get; }
        extern public int subMeshCount
        {
            [NativeMethod(Name = "GetSubMeshCount")] get;
            [NativeMethod(Name = "MeshScripting::SetSubMeshCount", IsFreeFunction = true, HasExplicitThis = true)] set;
        }

        extern public UnityEngine.Rendering.IndexFormat indexFormat
        {
            [NativeMethod(Name = "GetIndexFormat")] get;
            [NativeMethod(Name = "SetIndexFormat")] set;
        }

        [NativeMethod(Name = "MeshScripting::GetIndexStart", IsFreeFunction = true, HasExplicitThis = true)]
        extern private UInt32 GetIndexStartImpl(int submesh);

        [NativeMethod(Name = "MeshScripting::GetIndexCount", IsFreeFunction = true, HasExplicitThis = true)]
        extern private UInt32 GetIndexCountImpl(int submesh);

        [NativeMethod(Name = "MeshScripting::GetBaseVertex", IsFreeFunction = true, HasExplicitThis = true)]
        extern private UInt32 GetBaseVertexImpl(int submesh);

        [NativeMethod(Name = "Clear")]                  extern private void ClearImpl(bool keepVertexLayout);
        [NativeMethod(Name = "RecalculateBounds")]      extern private void RecalculateBoundsImpl();
        [NativeMethod(Name = "RecalculateNormals")]     extern private void RecalculateNormalsImpl();
        [NativeMethod(Name = "RecalculateTangents")]    extern private void RecalculateTangentsImpl();
        [NativeMethod(Name = "MarkDynamic")]            extern private void MarkDynamicImpl();
        [NativeMethod(Name = "UploadMeshData")]         extern private void UploadMeshDataImpl(bool markNoLogerReadable);

        [NativeMethod(Name = "MeshScripting::PrintErrorCantAccessChannel", IsFreeFunction = true, HasExplicitThis = true)]
        extern private void PrintErrorCantAccessChannel(InternalShaderChannel ch);

        // access to native underlying graphics API resources (mostly for native code plugins)
        extern public int vertexBufferCount
        {
            [NativeMethod(Name = "MeshScripting::GetVertexBufferCount", IsFreeFunction = true, HasExplicitThis = true)] get;
        }

        [NativeThrows]
        [NativeMethod(Name = "MeshScripting::GetNativeVertexBufferPtr", IsFreeFunction = true, HasExplicitThis = true)]
        extern public IntPtr GetNativeVertexBufferPtr(int index);

        [NativeMethod(Name = "MeshScripting::GetNativeIndexBufferPtr", IsFreeFunction = true, HasExplicitThis = true)]
        extern public IntPtr GetNativeIndexBufferPtr();

        // blend shapes

        extern public int blendShapeCount {[NativeMethod(Name = "GetBlendShapeChannelCount")] get; }

        [NativeMethod(Name = "MeshScripting::ClearBlendShapes", IsFreeFunction = true, HasExplicitThis = true)]
        extern public void ClearBlendShapes();

        [NativeMethod(Name = "MeshScripting::GetBlendShapeName", IsFreeFunction = true, HasExplicitThis = true)]
        extern public string GetBlendShapeName(int shapeIndex);

        [NativeMethod(Name = "MeshScripting::GetBlendShapeIndex", IsFreeFunction = true, HasExplicitThis = true)]
        extern public int GetBlendShapeIndex(string blendShapeName);

        [NativeMethod(Name = "MeshScripting::GetBlendShapeFrameCount", IsFreeFunction = true, HasExplicitThis = true)]
        extern public int GetBlendShapeFrameCount(int shapeIndex);

        [NativeMethod(Name = "MeshScripting::GetBlendShapeFrameWeight", IsFreeFunction = true, HasExplicitThis = true)]
        extern public float GetBlendShapeFrameWeight(int shapeIndex, int frameIndex);
    }
}
