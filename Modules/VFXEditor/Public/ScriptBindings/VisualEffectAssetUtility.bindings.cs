// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    internal struct VisualEffectAssetDesc
    {
        public VFXExpressionSheet sheet;
        public VFXEditorSystemDesc[] systemDesc;
        public VFXEventDesc[] eventDesc;
        public VFXGPUBufferDesc[] gpuBufferDesc;
        public VFXCPUBufferDesc[] cpuBufferDesc;
        public VFXTemporaryGPUBufferDesc[] temporaryBufferDesc;
        public VFXShaderSourceDesc[] shaderSourceDesc;
        public VFXRendererSettings rendererSettings;
        public VFXInstancingDisabledReason instancingDisabledReason;
        public VFXCompilationMode compilationMode;

        public uint version;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct VisualEffectAssetDescInternal
    {
        public VFXExpressionSheetInternal sheet;
        public VFXEditorSystemDesc[] systemDesc;
        public VFXEventDesc[] eventDesc;
        public VFXGPUBufferDesc[] gpuBufferDesc;
        public VFXCPUBufferDesc[] cpuBufferDesc;
        public VFXTemporaryGPUBufferDesc[] temporaryBufferDesc;
        public VFXShaderSourceDesc[] shaderSourceDesc;
        public VFXRendererSettings rendererSettings;
        public VFXInstancingDisabledReason instancingDisabledReason;
        public VFXCompilationMode compilationMode;

        public uint version;
    }

    [NativeHeader("Modules/VFXEditor/Public/VisualEffectAssetUtility.h")]
    internal static class VisualEffectAssetUtility
    {
        private static VisualEffectAssetDescInternal ConvertDescToInternal(VisualEffectAssetDesc desc)
        {
            var internalSheet = new VFXExpressionSheetInternal();
            internalSheet.expressions = desc.sheet.expressions;
            internalSheet.expressionsPerSpawnEventAttribute = desc.sheet.expressionsPerSpawnEventAttribute;
            internalSheet.exposed = desc.sheet.exposed;
            internalSheet.values = VisualEffectResource.CreateValueSheet(desc.sheet.values);

            var internalDesc = new VisualEffectAssetDescInternal()
            {
                sheet = internalSheet,
                systemDesc = desc.systemDesc,
                eventDesc = desc.eventDesc,
                gpuBufferDesc = desc.gpuBufferDesc,
                cpuBufferDesc = desc.cpuBufferDesc,
                temporaryBufferDesc = desc.temporaryBufferDesc,
                shaderSourceDesc = desc.shaderSourceDesc,
                rendererSettings = desc.rendererSettings,
                instancingDisabledReason = desc.instancingDisabledReason,
                compilationMode = desc.compilationMode,
                version = desc.version,
            };
            return internalDesc;
        }

        public static void SetVisualEffectAssetDesc(VisualEffectAsset asset, VisualEffectAssetDesc desc)
        {
            var internalDesc = ConvertDescToInternal(desc);
            SetVisualEffectAssetDesc(asset, internalDesc);
        }

        [NativeThrows]
        private static extern void SetVisualEffectAssetDesc([NotNull] VisualEffectAsset asset, VisualEffectAssetDescInternal desc);

        public static void SetValueSheet(VisualEffectAsset asset, VFXExpressionValueContainerDesc[] values)
        {
            var valueSheet = VisualEffectResource.CreateValueSheet(values);
            SetValueSheet(asset, valueSheet);
        }

        [NativeThrows]
        private static extern void SetValueSheet([NotNull] VisualEffectAsset asset, VFXExpressionValuesSheetInternal valueSheet);

        public static VisualEffectAsset CreateVisualEffectAsset(AssetImporters.AssetImportContext context, VisualEffectAssetDesc desc)
        {
            var internalDesc = ConvertDescToInternal(desc);
            return CreateVisualEffectAsset(context, internalDesc);
        }

        [NativeThrows]
        private static extern VisualEffectAsset CreateVisualEffectAsset(AssetImporters.AssetImportContext context, VisualEffectAssetDescInternal runtimeData);

        public static extern VFXCompilationMode GetCompilationMode([NotNull] VisualEffectAsset asset);
    }
}
