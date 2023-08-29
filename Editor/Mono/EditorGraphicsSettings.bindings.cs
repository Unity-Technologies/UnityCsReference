// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build;
using UnityEditor.Inspector.GraphicsSettingsInspectors;
using UnityEditor.Rendering.Settings;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering
{
    [NativeHeader("Editor/Mono/EditorGraphicsSettings.bindings.h")]
    [StaticAccessor("EditorGraphicsSettingsScripting", StaticAccessorType.DoubleColon)]
    public sealed partial class EditorGraphicsSettings
    {
        [NativeName("SetTierSettings")] extern internal static void SetTierSettingsImpl(BuildTargetGroup target, GraphicsTier tier, TierSettings settings);

        extern public   static TierSettings GetTierSettings(BuildTargetGroup target, GraphicsTier tier);
        public static TierSettings GetTierSettings(NamedBuildTarget target, GraphicsTier tier) => GetTierSettings(target.ToBuildTargetGroup(), tier);

        extern internal static TierSettings GetCurrentTierSettings();

        extern internal static bool AreTierSettingsAutomatic(BuildTargetGroup target, GraphicsTier tier);
        extern internal static void MakeTierSettingsAutomatic(BuildTargetGroup target, GraphicsTier tier, bool automatic);

        extern internal static void OnUpdateTierSettings(BuildTargetGroup target, bool shouldReloadShaders);

        // we give access to shader settings from both UI and script, and usually script access do not touch Undo system by itself
        // hence we provide small helper for our UI to register Undo changes when needed
        extern internal static void RegisterUndo();

        extern private static AlbedoSwatchInfo[] GetAlbedoSwatches();
        extern private static void               SetAlbedoSwatches(AlbedoSwatchInfo[] swatches);

        public static AlbedoSwatchInfo[] albedoSwatches
        {
            get { return GetAlbedoSwatches(); }
            set { SetAlbedoSwatches(value); }
        }

        extern public static BatchRendererGroupStrippingMode batchRendererGroupShaderStrippingMode { get; }

        [NativeName("RegisterRenderPipelineSettings")] static extern void Internal_RegisterRenderPipeline(string renderpipelineName, Object settings);
        [NativeName("UnregisterRenderPipelineSettings")] static extern void Internal_UnregisterRenderPipeline(string renderpipelineName);
        [NativeName("GetSettingsForRenderPipeline")] static extern Object Internal_GetSettingsForRenderPipeline(string renderpipelineName);

        private static void CheckRenderPipelineType(Type renderPipelineType)
        {
            if (renderPipelineType == null)
                throw new ArgumentNullException(nameof(renderPipelineType));

            if (!typeof(RenderPipeline).IsAssignableFrom(renderPipelineType))
                throw new ArgumentException($"{renderPipelineType} must be a valid {nameof(RenderPipeline)}");
        }

        public static void SetRenderPipelineGlobalSettingsAsset(Type renderPipelineType, RenderPipelineGlobalSettings newSettings)
        {
            CheckRenderPipelineType(renderPipelineType);

            if (newSettings != null)
            {
                RenderPipelineGraphicsSettingsManager.PopulateRenderPipelineGraphicsSettings(newSettings);
                Internal_RegisterRenderPipeline(renderPipelineType.FullName, newSettings);
            }
            else
                Internal_UnregisterRenderPipeline(renderPipelineType.FullName);

            GraphicsSettingsUtils.ReloadGraphicsSettingsEditor();
        }

        public static void SetRenderPipelineGlobalSettingsAsset<T>(RenderPipelineGlobalSettings newSettings)
            where T : RenderPipeline
        {
            SetRenderPipelineGlobalSettingsAsset(typeof(T), newSettings);
        }

        public static RenderPipelineGlobalSettings GetRenderPipelineGlobalSettingsAsset(Type renderPipelineType)
        {
            CheckRenderPipelineType(renderPipelineType);
            return Internal_GetSettingsForRenderPipeline(renderPipelineType.FullName) as RenderPipelineGlobalSettings;
        }

        public static RenderPipelineGlobalSettings GetRenderPipelineGlobalSettingsAsset<T>()
            where T : RenderPipeline
        {
            return GetRenderPipelineGlobalSettingsAsset(typeof(T));
        }

        public static bool TryGetRenderPipelineSettingsForPipeline<TSettings, TPipeline>(out TSettings settings)
            where TSettings : class, IRenderPipelineGraphicsSettings
            where TPipeline : RenderPipeline
        {
            return TryGetRenderPipelineSettingsForPipeline(typeof(TPipeline), out settings);
        }

        public static bool TryGetRenderPipelineSettingsForPipeline<TSettings>(Type renderPipelineType, out TSettings settings)
            where TSettings : class, IRenderPipelineGraphicsSettings
        {
            settings = null;

            var pipelineGlobalSettings = GraphicsSettings.GetSettingsForRenderPipeline(renderPipelineType);
            if (pipelineGlobalSettings == null)
                return false;

            if (pipelineGlobalSettings.TryGet(typeof(TSettings), out var baseSettings))
                settings = baseSettings as TSettings;

            return settings != null;
        }
    }
}
