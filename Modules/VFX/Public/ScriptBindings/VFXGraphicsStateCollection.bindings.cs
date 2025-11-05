// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using uei = UnityEngine.Internal;
using Unity.Collections;

namespace UnityEngine.VFX
{
    [NativeHeader("Modules/VFX/Public/ScriptBindings/VFXGraphicsStateCollectionBindings.h")]
    public static class VFXGraphicsStateCollectionBindings
    {

        public static bool AddGraphicsStates(this GraphicsStateCollection graphicsStateCollection, VisualEffectAsset[] visualEffectAssets, int samples, NativeArray<AttachmentDescriptor> attachments, NativeArray<SubPassDescriptor> subPasses,
            [uei.DefaultValue("0")] int subPassIndex = 0, [uei.DefaultValue("-1")] int depthAttachmentIndex = -1, [uei.DefaultValue("-1")] int shadingRateIndex = -1)
        {
            GlobalKeyword[] globalKeywords = Shader.enabledGlobalKeywords;
            return AddGraphicsStates(graphicsStateCollection, visualEffectAssets, globalKeywords, samples, attachments, subPasses, subPassIndex, depthAttachmentIndex, shadingRateIndex);
        }

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

        public static bool AddGraphicsStatesFromReference(this GraphicsStateCollection graphicsStateCollection, GraphicsStateCollection.GraphicsState refState, VisualEffectAsset[] visualEffectAssets, int samples, NativeArray<AttachmentDescriptor> attachments, NativeArray<SubPassDescriptor> subPasses,
            [uei.DefaultValue("0")] int subPassIndex = 0, [uei.DefaultValue("-1")] int depthAttachmentIndex = -1, [uei.DefaultValue("-1")] int shadingRateIndex = -1)
        {
            GlobalKeyword[] globalKeywords = Shader.enabledGlobalKeywords;
            return AddGraphicsStatesFromReference_Internal(graphicsStateCollection, refState, visualEffectAssets, globalKeywords, samples, attachments, subPasses, subPassIndex, depthAttachmentIndex, shadingRateIndex);
        }

        public static bool AddGraphicsStatesFromReference(this GraphicsStateCollection graphicsStateCollection, GraphicsStateCollection.GraphicsState refState, VisualEffectAsset[] visualEffectAssets, GlobalKeyword[] globalKeywords, int samples, NativeArray<AttachmentDescriptor> attachments, NativeArray<SubPassDescriptor> subPasses,
            [uei.DefaultValue("0")] int subPassIndex = 0, [uei.DefaultValue("-1")] int depthAttachmentIndex = -1, [uei.DefaultValue("-1")] int shadingRateIndex = -1)
        {
            return AddGraphicsStatesFromReference_Internal(graphicsStateCollection, refState, visualEffectAssets, globalKeywords, samples, attachments, subPasses, subPassIndex, depthAttachmentIndex, shadingRateIndex);
        }

        public static bool AddGraphicsStatesFromReference(this GraphicsStateCollection graphicsStateCollection, GraphicsStateCollection.GraphicsState refState, VisualEffectAsset[] visualEffectAssets)
        {
            return AddGraphicsStatesFromReference(graphicsStateCollection, refState, visualEffectAssets, Shader.enabledGlobalKeywords);
        }

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
    }
}
