// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using uei = UnityEngine.Internal;
using Unity.Jobs;

namespace UnityEngine.Experimental.Rendering
{
    [NativeHeader("Runtime/Graphics/GraphicsStateCollection.h")]
    public sealed partial class GraphicsStateCollection : Object
    {
        public partial struct GraphicsState
        {
            extern public void SetMeshData(Mesh mesh, int submesh, [uei.DefaultValue("null")] Renderer renderer = null);
            [NativeName("SetRenderPassData")] extern private void SetRenderPassData_Internal(int samples, ReadOnlySpan<AttachmentDescriptor> attachments, ReadOnlySpan<SubPassDescriptor> subPasses, int subPassIndex, int depthAttachmentIndex, int shadingRateIndex);
            public void SetRenderPassData(int samples, NativeArray<AttachmentDescriptor> attachments, NativeArray<SubPassDescriptor> subPasses, [uei.DefaultValue("0")] int subPassIndex = 0, [uei.DefaultValue("-1")] int depthAttachmentIndex = -1, [uei.DefaultValue("-1")] int shadingRateIndex = -1)
            {
                SetRenderPassData_Internal(samples, attachments, subPasses, subPassIndex, depthAttachmentIndex, shadingRateIndex);
            }
            extern public void SetRenderStateData(Shader shader, PassIdentifier passId);
        }

        extern public bool BeginTrace();
        extern public void EndTrace();
        extern public bool isTracing { [NativeName("IsTracing")] get; }
        extern public int version { [NativeName("GetVersion")] get; [NativeName("SetVersion")] set; }
        extern public GraphicsDeviceType graphicsDeviceType { [NativeName("GetDeviceRenderer")] get; [NativeName("SetDeviceRenderer")] set; }
        extern public RuntimePlatform runtimePlatform { [NativeName("GetRuntimePlatform")] get; [NativeName("SetRuntimePlatform")] set; }
        extern public string qualityLevelName { [NativeName("GetQualityLevelName")] get; [NativeName("SetQualityLevelName")] set; }
        extern public bool LoadFromFile(string filePath);
        extern public bool SaveToFile(string filePath);
        extern public bool SendToEditor(string fileName);
        [NativeName("Warmup")] extern public JobHandle WarmUp(JobHandle dependency = new JobHandle());
        [NativeName("WarmupProgressively")] extern public JobHandle WarmUpProgressively(int count, JobHandle dependency = new JobHandle());
        extern public int totalGraphicsStateCount { get; }
        extern public int completedWarmupCount { get; }
        extern public bool isWarmedUp { [NativeName("IsWarmedUp")] get; }

        extern private void GetVariants([Out] ShaderVariant[] results);
        public void GetVariants(List<ShaderVariant> results)
        {
            if (results == null)
                throw new ArgumentNullException("The result shader variant list cannot be null.");
            results.Clear();
            NoAllocHelpers.EnsureListElemCount(results, variantCount);
            GetVariants(NoAllocHelpers.ExtractArrayFromList(results));
        }
        extern private void GetGraphicsStatesForVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords, [Out] GraphicsState[] results);
        public void GetGraphicsStatesForVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords, List<GraphicsState> results)
        {
            if (results == null)
                throw new ArgumentNullException("The result graphics state list cannot be null.");
            results.Clear();
            NoAllocHelpers.EnsureListElemCount(results, GetGraphicsStateCountForVariant(shader, passId, keywords));
            GetGraphicsStatesForVariant(shader, passId, keywords, NoAllocHelpers.ExtractArrayFromList(results));
        }
        extern public int variantCount { get; }
        extern public int GetGraphicsStateCountForVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords);

        public bool AddVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords) { return AddVariantByShader(shader, passId, keywords); }
        public bool AddVariant(Material mat, PassIdentifier passId) { return AddVariantByMaterial(mat, passId); }
        [NativeName("AddVariant")] extern private bool AddVariantByShader(Shader shader, PassIdentifier passId, LocalKeyword[] keywords);
        [NativeName("AddVariant")] extern private bool AddVariantByMaterial(Material mat, PassIdentifier passId);
        extern public bool AddVariants(Material mat, [uei.DefaultValue("-1")] int subshaderIndex = -1);

        public bool RemoveVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords) { return RemoveVariantByShader(shader, passId, keywords); }
        public bool RemoveVariant(Material mat, PassIdentifier passId) { return RemoveVariantByMaterial(mat, passId); }
        [NativeName("RemoveVariant")] extern private bool RemoveVariantByShader(Shader shader, PassIdentifier passId, LocalKeyword[] keywords);
        [NativeName("RemoveVariant")] extern private bool RemoveVariantByMaterial(Material mat, PassIdentifier passId);

        public bool ContainsVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords) { return ContainsVariantByShader(shader, passId, keywords); }
        public bool ContainsVariant(Material mat, PassIdentifier passId) { return ContainsVariantByMaterial(mat, passId); }
        [NativeName("ContainsVariant")] extern private bool ContainsVariantByShader(Shader shader, PassIdentifier passId, LocalKeyword[] keywords);
        [NativeName("ContainsVariant")] extern private bool ContainsVariantByMaterial(Material mat, PassIdentifier passId);


        extern public void ClearVariants();
        extern public bool AddGraphicsStateForVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords, GraphicsState setup);
        extern public bool RemoveGraphicsStatesForVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords);
        extern public bool CopyGraphicsStatesForVariant(Shader srcShader, PassIdentifier srcPassId, LocalKeyword[] srcKeywords,
                                                        Shader dstShader, PassIdentifier dstPassId, LocalKeyword[] dstKeywords);

        [NativeName("CreateFromScript")] extern private static void Internal_Create([Writable] GraphicsStateCollection gsc);
    }
}
