// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/Mesh/MeshScriptBindings.h")]
    public sealed partial class Mesh
    {
        [FreeFunction("MeshScripting::CreateMesh")] extern private static void Internal_Create([Writable] Mesh mono);

        public Mesh()
        {
            Internal_Create(this);
        }

        [FreeFunction("MeshScripting::MeshFromInstanceId")] extern internal static Mesh FromInstanceID(int id);


        // triangles/indices

        extern public UnityEngine.Rendering.IndexFormat indexFormat { get; set; }

        [FreeFunction(Name = "MeshScripting::GetIndexStart", HasExplicitThis = true)]
        extern private UInt32 GetIndexStartImpl(int submesh);

        [FreeFunction(Name = "MeshScripting::GetIndexCount", HasExplicitThis = true)]
        extern private UInt32 GetIndexCountImpl(int submesh);

        [FreeFunction(Name = "MeshScripting::GetBaseVertex", HasExplicitThis = true)]
        extern private UInt32 GetBaseVertexImpl(int submesh);

        [FreeFunction(Name = "MeshScripting::GetTriangles", HasExplicitThis = true)]
        extern private int[] GetTrianglesImpl(int submesh, bool applyBaseVertex);

        [FreeFunction(Name = "MeshScripting::GetIndices", HasExplicitThis = true)]
        extern private int[] GetIndicesImpl(int submesh, bool applyBaseVertex);

        [FreeFunction(Name = "SetMeshIndicesFromScript", HasExplicitThis = true)]
        extern private void SetIndicesImpl(int submesh, MeshTopology topology, System.Array indices, int arraySize, bool calculateBounds, int baseVertex);

        [FreeFunction(Name = "MeshScripting::ExtractTrianglesToArray", HasExplicitThis = true)]
        extern private void GetTrianglesNonAllocImpl([Out] int[] values, int submesh, bool applyBaseVertex);

        [FreeFunction(Name = "MeshScripting::ExtractIndicesToArray", HasExplicitThis = true)]
        extern private void GetIndicesNonAllocImpl([Out] int[] values, int submesh, bool applyBaseVertex);

        // component (channels) setters/getters helpers

        [FreeFunction(Name = "MeshScripting::PrintErrorCantAccessChannel", HasExplicitThis = true)]
        extern private void PrintErrorCantAccessChannel(InternalShaderChannel ch);

        [FreeFunction(Name = "MeshScripting::HasChannel", HasExplicitThis = true)]
        extern internal bool HasChannel(InternalShaderChannel ch);

        [FreeFunction(Name = "SetMeshComponentFromArrayFromScript", HasExplicitThis = true)]
        extern private void SetArrayForChannelImpl(InternalShaderChannel channel, InternalVertexChannelType format, int dim, System.Array values, int arraySize);

        [FreeFunction(Name = "AllocExtractMeshComponentFromScript", HasExplicitThis = true)]
        extern private System.Array GetAllocArrayFromChannelImpl(InternalShaderChannel channel, InternalVertexChannelType format, int dim);

        [FreeFunction(Name = "ExtractMeshComponentFromScript", HasExplicitThis = true)]
        extern private void GetArrayFromChannelImpl(InternalShaderChannel channel, InternalVertexChannelType format, int dim, System.Array values);

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

        [FreeFunction(Name = "GetBlendShapeFrameVerticesFromScript", HasExplicitThis = true)]
        extern public void GetBlendShapeFrameVertices(int shapeIndex, int frameIndex, Vector3[] deltaVertices, Vector3[] deltaNormals, Vector3[] deltaTangents);

        [FreeFunction(Name = "AddBlendShapeFrameFromScript", HasExplicitThis = true)]
        extern public void AddBlendShapeFrame(string shapeName, float frameWeight, Vector3[] deltaVertices, Vector3[] deltaNormals, Vector3[] deltaTangents);

        // skinning

        extern private int GetBoneWeightCount();
        [NativeName("BoneWeightsFromScript")] extern public BoneWeight[] boneWeights { get; set; }

        extern private int GetBindposeCount();
        [NativeName("BindPosesFromScript")] extern public Matrix4x4[] bindposes { get; set; }

        [FreeFunction(Name = "MeshScripting::ExtractBoneWeightsIntoArray", HasExplicitThis = true)]
        extern private void GetBoneWeightsNonAllocImpl([Out] BoneWeight[] values);

        [FreeFunction(Name = "MeshScripting::ExtractBindPosesIntoArray", HasExplicitThis = true)]
        extern private void GetBindposesNonAllocImpl([Out] Matrix4x4[] values);

        // random things

        extern public   bool isReadable {[NativeMethod("GetIsReadable")] get; }
        extern internal bool canAccess  {[NativeMethod("CanAccessFromScript")] get; }

        extern public int vertexCount   {[NativeMethod("GetVertexCount")] get; }
        extern public int subMeshCount
        {
            [NativeMethod(Name = "GetSubMeshCount")] get;
            [FreeFunction(Name = "MeshScripting::SetSubMeshCount", HasExplicitThis = true)] set;
        }

        extern public Bounds bounds { get; set; }

        [NativeMethod("Clear")]                 extern private void ClearImpl(bool keepVertexLayout);
        [NativeMethod("RecalculateBounds")]     extern private void RecalculateBoundsImpl();
        [NativeMethod("RecalculateNormals")]    extern private void RecalculateNormalsImpl();
        [NativeMethod("RecalculateTangents")]   extern private void RecalculateTangentsImpl();
        [NativeMethod("MarkDynamic")]           extern private void MarkDynamicImpl();
        [NativeMethod("UploadMeshData")]        extern private void UploadMeshDataImpl(bool markNoLongerReadable);

        [FreeFunction(Name = "MeshScripting::GetPrimitiveType", HasExplicitThis = true)]
        extern private MeshTopology GetTopologyImpl(int submesh);


        [FreeFunction(Name = "MeshScripting::CombineMeshes", HasExplicitThis = true)]
        extern private void CombineMeshesImpl(CombineInstance[] combine, bool mergeSubMeshes, bool useMatrices, bool hasLightmapData);
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
