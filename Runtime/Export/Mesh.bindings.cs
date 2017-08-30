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
            [FreeFunction(Name = "MeshScripting::SetSubMeshCount", HasExplicitThis = true)] set;
        }

        extern public UnityEngine.Rendering.IndexFormat indexFormat
        {
            [NativeMethod(Name = "GetIndexFormat")] get;
            [NativeMethod(Name = "SetIndexFormat")] set;
        }

        [FreeFunction(Name = "MeshScripting::GetIndexStart", HasExplicitThis = true)]
        extern private UInt32 GetIndexStartImpl(int submesh);

        [FreeFunction(Name = "MeshScripting::GetIndexCount", HasExplicitThis = true)]
        extern private UInt32 GetIndexCountImpl(int submesh);

        [NativeMethod(Name = "MeshScripting::GetBaseVertex", IsFreeFunction = true, HasExplicitThis = true)]
        extern private UInt32 GetBaseVertexImpl(int submesh);

        [NativeMethod(Name = "Clear")]                  extern private void ClearImpl(bool keepVertexLayout);
        [NativeMethod(Name = "RecalculateBounds")]      extern private void RecalculateBoundsImpl();
        [NativeMethod(Name = "RecalculateNormals")]     extern private void RecalculateNormalsImpl();
        [NativeMethod(Name = "RecalculateTangents")]    extern private void RecalculateTangentsImpl();
        [NativeMethod(Name = "MarkDynamic")]            extern private void MarkDynamicImpl();
        [NativeMethod(Name = "UploadMeshData")]         extern private void UploadMeshDataImpl(bool markNoLogerReadable);

        [FreeFunction(Name = "MeshScripting::PrintErrorCantAccessChannel", HasExplicitThis = true)]
        extern private void PrintErrorCantAccessChannel(InternalShaderChannel ch);

        // access to native underlying graphics API resources (mostly for native code plugins)
        extern public int vertexBufferCount
        {
            [FreeFunction(Name = "MeshScripting::GetVertexBufferCount", HasExplicitThis = true)] get;
        }

        [NativeThrows]
        [FreeFunction(Name = "MeshScripting::GetNativeVertexBufferPtr", HasExplicitThis = true)]
        extern public IntPtr GetNativeVertexBufferPtr(int index);

        [FreeFunction(Name = "MeshScripting::GetNativeIndexBufferPtr", HasExplicitThis = true)]
        extern public IntPtr GetNativeIndexBufferPtr();

        // blend shapes

        extern public int blendShapeCount {[NativeMethod(Name = "GetBlendShapeChannelCount")] get; }

        [FreeFunction(Name = "MeshScripting::ClearBlendShapes", HasExplicitThis = true)]
        extern public void ClearBlendShapes();

        [FreeFunction(Name = "MeshScripting::GetBlendShapeName", HasExplicitThis = true)]
        extern public string GetBlendShapeName(int shapeIndex);

        [FreeFunction(Name = "MeshScripting::GetBlendShapeIndex", HasExplicitThis = true)]
        extern public int GetBlendShapeIndex(string blendShapeName);

        [FreeFunction(Name = "MeshScripting::GetBlendShapeFrameCount", HasExplicitThis = true)]
        extern public int GetBlendShapeFrameCount(int shapeIndex);

        [FreeFunction(Name = "MeshScripting::GetBlendShapeFrameWeight", HasExplicitThis = true)]
        extern public float GetBlendShapeFrameWeight(int shapeIndex, int frameIndex);
    }

    [NativeHeader("Runtime/Graphics/Mesh/MeshScriptBindings.h")]
    internal struct StaticBatchingHelper
    {
        [FreeFunction("MeshScripting::CombineMeshVerticesForStaticBatching")]
        extern internal static Mesh InternalCombineVertices(MeshSubsetCombineUtility.MeshInstance[] meshes, string meshName);
        [FreeFunction("MeshScripting::CombineMeshIndicesForStaticBatching")]
        extern internal static void InternalCombineIndices(MeshSubsetCombineUtility.SubMeshInstance[] submeshes, Mesh combinedMesh);
    }
}
