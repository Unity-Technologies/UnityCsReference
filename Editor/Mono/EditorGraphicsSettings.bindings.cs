// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Inspector.GraphicsSettingsInspectors;
using UnityEditor.Rendering.Settings;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering
{
    [NativeHeader("Editor/Mono/EditorGraphicsSettings.bindings.h")]
    [StaticAccessor("EditorGraphicsSettingsScripting", StaticAccessorType.DoubleColon)]
    public sealed partial class EditorGraphicsSettings
    {
        [NativeName("SetTierSettings")] extern internal static void SetTierSettingsImpl(BuildTargetGroup target, GraphicsTier tier, TierSettings settings);

        extern public static TierSettings GetTierSettings(BuildTargetGroup target, GraphicsTier tier);
        public static TierSettings GetTierSettings(NamedBuildTarget target, GraphicsTier tier) => GetTierSettings(target.ToBuildTargetGroup(), tier);

        extern internal static TierSettings GetCurrentTierSettings();

        extern internal static bool AreTierSettingsAutomatic(BuildTargetGroup target, GraphicsTier tier);
        extern internal static void MakeTierSettingsAutomatic(BuildTargetGroup target, GraphicsTier tier, bool automatic);

        extern internal static void OnUpdateTierSettings(BuildTargetGroup target, bool shouldReloadShaders);

        // we give access to shader settings from both UI and script, and usually script access do not touch Undo system by itself
        // hence we provide small helper for our UI to register Undo changes when needed
        extern internal static void RegisterUndo();

        extern private static AlbedoSwatchInfo[] GetAlbedoSwatches();
        extern private static void SetAlbedoSwatches(AlbedoSwatchInfo[] swatches);

        public static AlbedoSwatchInfo[] albedoSwatches
        {
            get { return GetAlbedoSwatches(); }
            set { SetAlbedoSwatches(value); }
        }

        extern public static BatchRendererGroupStrippingMode batchRendererGroupShaderStrippingMode { get; }
        extern internal static bool activeProfileHasGraphicsSettings { get; set; }

        [NativeName("RegisterRenderPipelineSettings")] static extern bool Internal_TryRegisterRenderPipeline(string renderpipelineName, Object settings);
        [NativeName("UnregisterRenderPipelineSettings")] static extern bool Internal_TryUnregisterRenderPipeline(string renderpipelineName);
        [NativeName("GetSettingsForRenderPipeline")] static extern Object Internal_GetSettingsForRenderPipeline(string renderpipelineName);

        [NativeName("GetSettingsInstanceIDForRenderPipeline")] internal static extern int Internal_GetSettingsInstanceIDForRenderPipeline(string renderpipelineName);

        private static void CheckRenderPipelineType(Type renderPipelineType)
        {
            if (renderPipelineType == null)
                throw new ArgumentNullException(nameof(renderPipelineType));

            if (!typeof(RenderPipeline).IsAssignableFrom(renderPipelineType))
                throw new ArgumentException($"{renderPipelineType} must be a valid {nameof(RenderPipeline)}");
        }

        public static void SetRenderPipelineGlobalSettingsAsset(Type renderPipelineType, RenderPipelineGlobalSettings newSettings)
        {
            //In Worker thread, we cannot update assets
            if (AssetDatabase.IsAssetImportWorkerProcess())
                return;

            CheckRenderPipelineType(renderPipelineType);

            bool globalSettingsAssetChanged;
            if (newSettings != null)
            {
                RenderPipelineGraphicsSettingsManager.PopulateRenderPipelineGraphicsSettings(newSettings);
                globalSettingsAssetChanged = Internal_TryRegisterRenderPipeline(renderPipelineType.FullName, newSettings);
            }
            else
                globalSettingsAssetChanged = Internal_TryUnregisterRenderPipeline(renderPipelineType.FullName);

            if (globalSettingsAssetChanged)
            {
                var rpAsset = RenderPipelineManager.currentPipelineAsset;
                if (rpAsset != null && rpAsset.pipelineType == renderPipelineType)
                    RenderPipelineManager.RecreateCurrentPipeline(rpAsset);
            }

            //Removing a globalSeetings and adding back another one from the same type will cause issue in the Notifier persistency cache
            Notifier.RecomputeDictionary();  

            GraphicsSettingsInspectorUtility.ReloadGraphicsSettingsEditorIfNeeded();
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

        public static IEnumerable<Type> GetSupportedRenderPipelineGraphicsSettingsTypesForPipeline<T>()
            where T : RenderPipelineAsset
        {
            foreach(var info in RenderPipelineGraphicsSettingsManager.FetchRenderPipelineGraphicsSettingInfos(typeof(T)))
                yield return info.type;
        }

        public static void PopulateRenderPipelineGraphicsSettings(RenderPipelineGlobalSettings settings)
        {
            RenderPipelineGraphicsSettingsManager.PopulateRenderPipelineGraphicsSettings(settings);
        }

        [NativeName("GraphicsSettingsCount")] static extern int Internal_GraphicsSettingsCount();
        [NativeName("GetSettingsForRenderPipelineAt")] static extern Object Internal_GetSettingsForRenderPipelineAt(int index);

        internal static void ForEachPipelineSettings(Action<RenderPipelineGlobalSettings> action)
        {
            int count = Internal_GraphicsSettingsCount();
            for (int i = 0; i < count; ++i)
                action?.Invoke(Internal_GetSettingsForRenderPipelineAt(i) as RenderPipelineGlobalSettings);
        }

        public static TSettingsInterfaceType[] GetRenderPipelineSettingsFromInterface<TSettingsInterfaceType>()
            where TSettingsInterfaceType : class, IRenderPipelineGraphicsSettings
        {
            if (!GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out RenderPipelineGlobalSettings asset))
                return new TSettingsInterfaceType[] {};

            if (asset.GetSettingsImplementingInterface<TSettingsInterfaceType>(out var baseSettings))
            {
                return baseSettings.ToArray();
            }

            return new TSettingsInterfaceType[] {};
        }

        public static bool TryGetFirstRenderPipelineSettingsFromInterface<TSettingsInterfaceType>(out TSettingsInterfaceType settings)
            where TSettingsInterfaceType : class, IRenderPipelineGraphicsSettings
        {
            settings = null;

            if (!GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out RenderPipelineGlobalSettings asset))
                return false;

            if (asset.TryGetFirstSettingsImplementingInterface<TSettingsInterfaceType>(out var baseSettings))
            {
                settings = baseSettings;
                return true;
            }

            return false;
        }

        public static bool TryGetRenderPipelineSettingsFromInterface<TSettingsInterfaceType>(out TSettingsInterfaceType[] settings)
            where TSettingsInterfaceType : class, IRenderPipelineGraphicsSettings
        {
            settings = null;

            if (!GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out RenderPipelineGlobalSettings asset))
                return false;

            if (asset.GetSettingsImplementingInterface<TSettingsInterfaceType>(out var baseSettings))
                settings = baseSettings.ToArray();

            return settings != null;
        }

        public static bool TryGetRenderPipelineSettingsFromInterfaceForPipeline<TSettingsInterfaceType, TPipeline>(out TSettingsInterfaceType[] settings)
            where TSettingsInterfaceType : class, IRenderPipelineGraphicsSettings
            where TPipeline : RenderPipeline
        {
            return TryGetRenderPipelineSettingsFromInterfaceForPipeline(typeof(TPipeline), out settings);
        }

        public static bool TryGetRenderPipelineSettingsFromInterfaceForPipeline<TSettingsInterfaceType>(Type renderPipelineType, out TSettingsInterfaceType[] settings)
            where TSettingsInterfaceType : class, IRenderPipelineGraphicsSettings
        {
            settings = null;

            var pipelineGlobalSettings = GraphicsSettings.GetSettingsForRenderPipeline(renderPipelineType);
            if (pipelineGlobalSettings == null)
                return false;

            if (pipelineGlobalSettings.GetSettingsImplementingInterface<TSettingsInterfaceType>(out var baseSettings))
                settings = baseSettings.ToArray();

            return settings != null;
        }

        [RequiredByNativeCode]
        internal static bool IsGlobalSettingsContaining(UnityEngine.Object renderPipelineGlobalSettings, object renderPipelineGraphicsSettings)
        {
            var settings = renderPipelineGraphicsSettings as IRenderPipelineGraphicsSettings;
            var globalSettings = renderPipelineGlobalSettings as RenderPipelineGlobalSettings;
            return globalSettings.ContainsReference(settings);
        }
    }
}
