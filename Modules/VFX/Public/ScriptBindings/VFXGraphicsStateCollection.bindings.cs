// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using uei = UnityEngine.Internal;
using Unity.Collections;

namespace UnityEngine.VFX
{
    ///<summary>Extension methods for adding <see cref="Rendering.GraphicsStateCollection.GraphicsState">GraphicsStates</see> into a <see cref="Rendering.GraphicsStateCollection" /> from <see cref="VisualEffectAsset" />.</summary>
    [NativeHeader("Modules/VFX/Public/ScriptBindings/VFXGraphicsStateCollectionBindings.h")]
    public static class VFXGraphicsStateCollectionBindings
    {

        ///<summary>Generates and adds new graphics states to the collection from an array of visual effect assets.</summary>
        ///<remarks>This method creates and attempts to add graphics states by processing arrays of assets. This is a convenient way to populate the collection without constructing each 
        ///                <see cref="Rendering.GraphicsStateCollection.GraphicsState" /> object manually. Each graphics state is populated with data from the mesh, render state, and render pass, and sets any remaining fields to their default values. 
        ///                To set remaining fields to specific values instead, use <see cref="AddGraphicsStatesFromReference" />.
        ///
        ///                - When you provide a <see cref="VisualEffectAsset" /> array, this function scans each asset to get pairs of shaders and geometry used in the render outputs.
        ///
        ///                For each individual pair, the function generates graphics states for all combinations of that mesh's submeshes and that material's shader passes. 
        ///                The generated shader variants use the set of <see cref="Material.enabledKeywords">enabled keywords</see> for each <see cref="Material" /> and include the global shader keywords that are **currently enabled** in the active context if they are not explicitly provided.
        ///                Finally, the function will not add a graphics state if an identical one already exists for a given shader variant. 
        ///
        ///                If a list of <see cref="Rendering.GraphicsStateCollection.GraphicsState" /> objects is already available, then <see cref="Rendering.GraphicsStateCollection.AddGraphicsStateForVariant" /> can instead be used to add to the collection.</remarks>
        ///<param name="graphicsStateCollection">The GraphicsStateCollection in which to add the graphics state.</param>
        ///<param name="visualEffectAssets">An array of <see cref="VisualEffectAsset" /> files to scan for each unique mesh and shader pairs.</param>
        ///<param name="samples">The number of samples per pixel in this rendering configuration.</param>
        ///<param name="attachments">The array of color attachments used in this rendering configuration.</param>
        ///<param name="subPasses">The array containing information of each subpass.</param>
        ///<param name="subPassIndex">The index of the active subpass in this rendering configuration.</param>
        ///<param name="depthAttachmentIndex">The index of the attachment to be used as the depth/stencil buffer for this rendering configuration.</param>
        ///<param name="shadingRateIndex">The index of the attachment to be used as the shading rate image for this rendering configuration.</param>
        ///<returns>True if at least one new graphics state was successfully added, false otherwise.</returns>
        ///<seealso cref="AddGraphicsStatesFromReference" />
        ///<seealso cref="CommandBuffer.BeginRenderPass" />
        public static bool AddGraphicsStates(this GraphicsStateCollection graphicsStateCollection, VisualEffectAsset[] visualEffectAssets, int samples, NativeArray<AttachmentDescriptor> attachments, NativeArray<SubPassDescriptor> subPasses,
            [uei.DefaultValue("0")] int subPassIndex = 0, [uei.DefaultValue("-1")] int depthAttachmentIndex = -1, [uei.DefaultValue("-1")] int shadingRateIndex = -1)
        {
            GlobalKeyword[] globalKeywords = Shader.enabledGlobalKeywords;
            return AddGraphicsStates(graphicsStateCollection, visualEffectAssets, globalKeywords, samples, attachments, subPasses, subPassIndex, depthAttachmentIndex, shadingRateIndex);
        }

        ///<summary>Generates and adds new graphics states to the collection from an array of visual effect assets.</summary>
        ///<remarks>This method creates and attempts to add graphics states by processing arrays of assets. This is a convenient way to populate the collection without constructing each 
        ///                <see cref="Rendering.GraphicsStateCollection.GraphicsState" /> object manually. Each graphics state is populated with data from the mesh, render state, and render pass, and sets any remaining fields to their default values. 
        ///                To set remaining fields to specific values instead, use <see cref="AddGraphicsStatesFromReference" />.
        ///
        ///                - When you provide a <see cref="VisualEffectAsset" /> array, this function scans each asset to get pairs of shaders and geometry used in the render outputs.
        ///
        ///                For each individual pair, the function generates graphics states for all combinations of that mesh's submeshes and that material's shader passes. 
        ///                The generated shader variants use the set of <see cref="Material.enabledKeywords">enabled keywords</see> for each <see cref="Material" /> and include the global shader keywords that are **currently enabled** in the active context if they are not explicitly provided.
        ///                Finally, the function will not add a graphics state if an identical one already exists for a given shader variant. 
        ///
        ///                If a list of <see cref="Rendering.GraphicsStateCollection.GraphicsState" /> objects is already available, then <see cref="Rendering.GraphicsStateCollection.AddGraphicsStateForVariant" /> can instead be used to add to the collection.</remarks>
        ///<param name="graphicsStateCollection">The GraphicsStateCollection in which to add the graphics state.</param>
        ///<param name="visualEffectAssets">An array of <see cref="VisualEffectAsset" /> files to scan for each unique mesh and shader pairs.</param>
        ///<param name="globalKeywords">An array of <see cref="GlobalKeyword" /> objects to use in conjunction with each material's enabled keywords when generating shader variants.</param>
        ///<param name="samples">The number of samples per pixel in this rendering configuration.</param>
        ///<param name="attachments">The array of color attachments used in this rendering configuration.</param>
        ///<param name="subPasses">The array containing information of each subpass.</param>
        ///<param name="subPassIndex">The index of the active subpass in this rendering configuration.</param>
        ///<param name="depthAttachmentIndex">The index of the attachment to be used as the depth/stencil buffer for this rendering configuration.</param>
        ///<param name="shadingRateIndex">The index of the attachment to be used as the shading rate image for this rendering configuration.</param>
        ///<returns>True if at least one new graphics state was successfully added, false otherwise.</returns>
        ///<seealso cref="AddGraphicsStatesFromReference" />
        ///<seealso cref="CommandBuffer.BeginRenderPass" />
        public static bool AddGraphicsStates(this GraphicsStateCollection graphicsStateCollection, VisualEffectAsset[] visualEffectAssets, GlobalKeyword[] globalKeywords, int samples, NativeArray<AttachmentDescriptor> attachments, NativeArray<SubPassDescriptor> subPasses,
            [uei.DefaultValue("0")] int subPassIndex = 0, [uei.DefaultValue("-1")] int depthAttachmentIndex = -1, [uei.DefaultValue("-1")] int shadingRateIndex = -1)
        {
            // First add with untouched globalKeywords
            bool added = AddGraphicsStates_Internal(graphicsStateCollection, visualEffectAssets, globalKeywords, samples, attachments, subPasses, subPassIndex, depthAttachmentIndex, shadingRateIndex);
            // Then add with instancing keyword added or removed
            GlobalKeyword[] patchedGlobalKeywords = GetInstancingPatchedGlobalKeywords(globalKeywords);
            added |= AddGraphicsStates_Internal(graphicsStateCollection, visualEffectAssets, patchedGlobalKeywords, samples, attachments, subPasses, subPassIndex, depthAttachmentIndex, shadingRateIndex);
            return added;
        }

        ///<summary>Generates and adds new graphics states from arrays of assets, using a reference graphics state to initialize unspecified values.</summary>
        ///<remarks>This function operates like <see cref="AddGraphicsStates" />, but instead of using default values for <see cref="Rendering.GraphicsStateCollection.GraphicsState" /> fields, 
        ///                it copies values from the provided <c>refState</c> for any fields not determined by the input parameters.</remarks>
        ///<param name="graphicsStateCollection">The GraphicsStateCollection in which to add the graphics state.</param>
        ///<param name="refState">The reference <see cref="Rendering.GraphicsStateCollection.GraphicsState">GraphicsState</see> to use as a template for initializing unspecified values.</param>
        ///<param name="visualEffectAssets">An array of <see cref="VisualEffectAsset" /> files to scan for each unique mesh and shader pairs.</param>
        ///<param name="samples">The number of samples per pixel in this rendering configuration.</param>
        ///<param name="attachments">The array of color attachments used in this rendering configuration.</param>
        ///<param name="subPasses">The array containing information of each subpass.</param>
        ///<param name="subPassIndex">The index of the active subpass in this rendering configuration.</param>
        ///<param name="depthAttachmentIndex">The index of the attachment to be used as the depth/stencil buffer for this rendering configuration.</param>
        ///<param name="shadingRateIndex">The index of the attachment to be used as the shading rate image for this rendering configuration.</param>
        ///<returns>True if at least one new graphics state was successfully added, false otherwise.</returns>
        ///<seealso cref="AddGraphicsStates" />
        ///<seealso cref="CommandBuffer.BeginRenderPass" />
        public static bool AddGraphicsStatesFromReference(this GraphicsStateCollection graphicsStateCollection, GraphicsStateCollection.GraphicsState refState, VisualEffectAsset[] visualEffectAssets, int samples, NativeArray<AttachmentDescriptor> attachments, NativeArray<SubPassDescriptor> subPasses,
            [uei.DefaultValue("0")] int subPassIndex = 0, [uei.DefaultValue("-1")] int depthAttachmentIndex = -1, [uei.DefaultValue("-1")] int shadingRateIndex = -1)
        {
            GlobalKeyword[] globalKeywords = Shader.enabledGlobalKeywords;
            return AddGraphicsStatesFromReference_Internal(graphicsStateCollection, refState, visualEffectAssets, globalKeywords, samples, attachments, subPasses, subPassIndex, depthAttachmentIndex, shadingRateIndex);
        }

        ///<summary>Generates and adds new graphics states from arrays of assets, using a reference graphics state to initialize unspecified values.</summary>
        ///<remarks>This function operates like <see cref="AddGraphicsStates" />, but instead of using default values for <see cref="Rendering.GraphicsStateCollection.GraphicsState" /> fields, 
        ///                it copies values from the provided <c>refState</c> for any fields not determined by the input parameters.</remarks>
        ///<param name="graphicsStateCollection">The GraphicsStateCollection in which to add the graphics state.</param>
        ///<param name="refState">The reference <see cref="Rendering.GraphicsStateCollection.GraphicsState">GraphicsState</see> to use as a template for initializing unspecified values.</param>
        ///<param name="visualEffectAssets">An array of <see cref="VisualEffectAsset" /> files to scan for each unique mesh and shader pairs.</param>
        ///<param name="globalKeywords">An array of <see cref="GlobalKeyword" /> objects to use in conjunction with each material's enabled keywords when generating shader variants.</param>
        ///<param name="samples">The number of samples per pixel in this rendering configuration.</param>
        ///<param name="attachments">The array of color attachments used in this rendering configuration.</param>
        ///<param name="subPasses">The array containing information of each subpass.</param>
        ///<param name="subPassIndex">The index of the active subpass in this rendering configuration.</param>
        ///<param name="depthAttachmentIndex">The index of the attachment to be used as the depth/stencil buffer for this rendering configuration.</param>
        ///<param name="shadingRateIndex">The index of the attachment to be used as the shading rate image for this rendering configuration.</param>
        ///<returns>True if at least one new graphics state was successfully added, false otherwise.</returns>
        ///<seealso cref="AddGraphicsStates" />
        ///<seealso cref="CommandBuffer.BeginRenderPass" />
        public static bool AddGraphicsStatesFromReference(this GraphicsStateCollection graphicsStateCollection, GraphicsStateCollection.GraphicsState refState, VisualEffectAsset[] visualEffectAssets, GlobalKeyword[] globalKeywords, int samples, NativeArray<AttachmentDescriptor> attachments, NativeArray<SubPassDescriptor> subPasses,
            [uei.DefaultValue("0")] int subPassIndex = 0, [uei.DefaultValue("-1")] int depthAttachmentIndex = -1, [uei.DefaultValue("-1")] int shadingRateIndex = -1)
        {
            return AddGraphicsStatesFromReference_Internal(graphicsStateCollection, refState, visualEffectAssets, globalKeywords, samples, attachments, subPasses, subPassIndex, depthAttachmentIndex, shadingRateIndex);
        }

        ///<summary>Generates and adds new graphics states from arrays of assets, using a reference graphics state to initialize unspecified values.</summary>
        ///<remarks>This function operates like <see cref="AddGraphicsStates" />, but instead of using default values for <see cref="Rendering.GraphicsStateCollection.GraphicsState" /> fields, 
        ///                it copies values from the provided <c>refState</c> for any fields not determined by the input parameters.</remarks>
        ///<param name="graphicsStateCollection">The GraphicsStateCollection in which to add the graphics state.</param>
        ///<param name="refState">The reference <see cref="Rendering.GraphicsStateCollection.GraphicsState">GraphicsState</see> to use as a template for initializing unspecified values.</param>
        ///<param name="visualEffectAssets">An array of <see cref="VisualEffectAsset" /> files to scan for each unique mesh and shader pairs.</param>
        ///<returns>True if at least one new graphics state was successfully added, false otherwise.</returns>
        ///<seealso cref="AddGraphicsStates" />
        ///<seealso cref="CommandBuffer.BeginRenderPass" />
        public static bool AddGraphicsStatesFromReference(this GraphicsStateCollection graphicsStateCollection, GraphicsStateCollection.GraphicsState refState, VisualEffectAsset[] visualEffectAssets)
        {
            return AddGraphicsStatesFromReference(graphicsStateCollection, refState, visualEffectAssets, Shader.enabledGlobalKeywords);
        }

        ///<summary>Generates and adds new graphics states from arrays of assets, using a reference graphics state to initialize unspecified values.</summary>
        ///<remarks>This function operates like <see cref="AddGraphicsStates" />, but instead of using default values for <see cref="Rendering.GraphicsStateCollection.GraphicsState" /> fields, 
        ///                it copies values from the provided <c>refState</c> for any fields not determined by the input parameters.</remarks>
        ///<param name="graphicsStateCollection">The GraphicsStateCollection in which to add the graphics state.</param>
        ///<param name="refState">The reference <see cref="Rendering.GraphicsStateCollection.GraphicsState">GraphicsState</see> to use as a template for initializing unspecified values.</param>
        ///<param name="visualEffectAssets">An array of <see cref="VisualEffectAsset" /> files to scan for each unique mesh and shader pairs.</param>
        ///<param name="globalKeywords">An array of <see cref="GlobalKeyword" /> objects to use in conjunction with each material's enabled keywords when generating shader variants.</param>
        ///<returns>True if at least one new graphics state was successfully added, false otherwise.</returns>
        ///<seealso cref="AddGraphicsStates" />
        ///<seealso cref="CommandBuffer.BeginRenderPass" />
        public static bool AddGraphicsStatesFromReference(this GraphicsStateCollection graphicsStateCollection, GraphicsStateCollection.GraphicsState refState, VisualEffectAsset[] visualEffectAssets, GlobalKeyword[] globalKeywords)
        {
            int samples = refState.sampleCount;
            AttachmentDescriptor[] attachments = refState.attachments;
            SubPassDescriptor[] subPasses = refState.subPasses;
            int subPassIndex = refState.subPassIndex;
            int depthAttachmentIndex = refState.depthAttachmentIndex;
            int shadingRateIndex = refState.shadingRateIndex;

            // First add with untouched globalKeywords
            bool added = AddGraphicsStatesFromReference_Internal(graphicsStateCollection, refState, visualEffectAssets, globalKeywords, samples, attachments, subPasses, subPassIndex, depthAttachmentIndex, shadingRateIndex);
            // Then add with instancing keyword added or removed
            GlobalKeyword[] patchedGlobalKeywords = GetInstancingPatchedGlobalKeywords(globalKeywords);
            added |= AddGraphicsStatesFromReference_Internal(graphicsStateCollection, refState, visualEffectAssets, patchedGlobalKeywords, samples, attachments, subPasses, subPassIndex, depthAttachmentIndex, shadingRateIndex);
            return added;
        }

        ///<summary>Adds to the collection the compute shader variants that the visual effect assets dispatch.</summary>
        ///<remarks>For each compute shader task in each <see cref="VisualEffectAsset" />, this method adds the variant for kernel 0 with the compute shader's enabled local keywords and the global shader keywords that are currently enabled in the active context.
        ///                For update tasks that can compute ray tracing bounding boxes, it adds the variants both with and without the <c>VFX_COMPUTE_AABBS</c> keyword.
        ///                The method doesn't add compute shaders that the Visual Effect Graph runtime shares between all effects, such as sorting and indirect argument shaders. To record them, trace the collection at runtime.
        ///                The method doesn't add a variant that the collection already contains.</remarks>
        ///<param name="graphicsStateCollection">The GraphicsStateCollection in which to add the compute shader variants.</param>
        ///<param name="visualEffectAssets">An array of <see cref="VisualEffectAsset" /> files to scan for compute shaders.</param>
        ///<returns>True if at least one new compute shader variant was successfully added, false otherwise.</returns>
        ///<seealso cref="Rendering.GraphicsStateCollection.AddComputeVariant" />
        public static bool AddComputeVariants(this GraphicsStateCollection graphicsStateCollection, VisualEffectAsset[] visualEffectAssets)
        {
            return AddComputeVariants_Internal(graphicsStateCollection, visualEffectAssets, Shader.enabledGlobalKeywords);
        }

        ///<summary>Adds to the collection the compute shader variants that the visual effect assets dispatch.</summary>
        ///<remarks>This method operates like <see cref="AddComputeVariants(GraphicsStateCollection, VisualEffectAsset[])" />, but uses the provided global keywords instead of the global shader keywords that are currently enabled.</remarks>
        ///<param name="graphicsStateCollection">The GraphicsStateCollection in which to add the compute shader variants.</param>
        ///<param name="visualEffectAssets">An array of <see cref="VisualEffectAsset" /> files to scan for compute shaders.</param>
        ///<param name="globalKeywords">An array of <see cref="GlobalKeyword" /> objects to use with each compute shader's enabled local keywords when the method generates compute shader variants.</param>
        ///<returns>True if at least one new compute shader variant was successfully added, false otherwise.</returns>
        ///<seealso cref="Rendering.GraphicsStateCollection.AddComputeVariant" />
        public static bool AddComputeVariants(this GraphicsStateCollection graphicsStateCollection, VisualEffectAsset[] visualEffectAssets, GlobalKeyword[] globalKeywords)
        {
            return AddComputeVariants_Internal(graphicsStateCollection, visualEffectAssets, globalKeywords);
        }

        private static GlobalKeyword[] GetInstancingPatchedGlobalKeywords(GlobalKeyword[] globalKeywords)
        {
            GlobalKeyword instancingKeyword = new GlobalKeyword("INSTANCING_ON");
            int instancingIndex = -1;
            for(int i = 0; i < globalKeywords.Length; i++)
            {
                if (globalKeywords[i].Equals(instancingKeyword))
                {
                    instancingIndex = i;
                    break;
                }
            }
            if(instancingIndex != -1) // Remove the instancing keyword
            {
                GlobalKeyword[] patchedGlobalKeywords = new GlobalKeyword[globalKeywords.Length - 1];
                int dstIndex = 0;
                for(int i = 0; i < globalKeywords.Length; i++)
                {
                    if(i != instancingIndex)
                        patchedGlobalKeywords[dstIndex++] = globalKeywords[i];
                }
                return patchedGlobalKeywords;
            }
            else // Add the instancing keyword
            {
                GlobalKeyword[] patchedGlobalKeywords = new GlobalKeyword[globalKeywords.Length + 1];
                globalKeywords.CopyTo(patchedGlobalKeywords, 0);
                patchedGlobalKeywords[globalKeywords.Length] = instancingKeyword;
                return patchedGlobalKeywords;
            }
        }

        [FreeFunction(Name = "VFXGraphicsStateCollectionBindings::AddGraphicsStates", HasExplicitThis = false)]
        private static extern bool AddGraphicsStates_Internal(this GraphicsStateCollection graphicsStateCollection, VisualEffectAsset[] visualEffectAssets, GlobalKeyword[] globalKeywords, int samples, ReadOnlySpan<AttachmentDescriptor> attachments,
            ReadOnlySpan<SubPassDescriptor> subPasses, int subPassIndex, int depthAttachmentIndex, int shadingRateIndex);

        [FreeFunction(Name = "VFXGraphicsStateCollectionBindings::AddGraphicsStatesFromReference", HasExplicitThis = false)]
        private static extern bool AddGraphicsStatesFromReference_Internal(this GraphicsStateCollection graphicsStateCollection, GraphicsStateCollection.GraphicsState refState, VisualEffectAsset[] visualEffectAssets, GlobalKeyword[] globalKeywords, int samples, ReadOnlySpan<AttachmentDescriptor> attachments,
            ReadOnlySpan<SubPassDescriptor> subPasses, int subPassIndex, int depthAttachmentIndex, int shadingRateIndex);

        [FreeFunction(Name = "VFXGraphicsStateCollectionBindings::AddComputeVariants", HasExplicitThis = false)]
        private static extern bool AddComputeVariants_Internal(this GraphicsStateCollection graphicsStateCollection, VisualEffectAsset[] visualEffectAssets, GlobalKeyword[] globalKeywords);
    }
}
