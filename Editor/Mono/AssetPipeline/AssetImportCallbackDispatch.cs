// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
    // Fans each import callback out to postprocessors and import blocks, and owns the per-import state both need.
    // Scopes are stacked, so a nested import restores the outer one when it ends.
    internal static partial class AssetImportCallbackDispatch
    {
        // Blocks run between the postprocessors ordered below this and those at or above it. The default
        // GetPostprocessOrder() is 0, so a postprocessor opts into running after blocks by returning 1 or more.
        internal const int k_PostprocessOrderThreshold = 1;

        sealed class Scope
        {
            public AssetImporter Importer { get; }
            public AssetImportContext Context { get; }

            public Scope(AssetImporter importer, AssetImportContext context)
            {
                Importer = importer;
                Context = context;
            }
        }

        [AutoStaticsCleanupOnCodeReload]
        static Stack<Scope> s_ScopeStack = new Stack<Scope>();

        static Scope currentScope => s_ScopeStack.Count > 0 ? s_ScopeStack.Peek() : null;

        static AssetImporter currentImporter => currentScope?.Importer;
        static AssetImportContext currentContext => currentScope?.Context;


        static void PushScope(Scope scope)
        {
            s_ScopeStack.Push(scope);
        }

        // A code reload resets the stack, so a cleanup can arrive for a scope that was pushed before the reload.
        static void PopScope()
        {
            if (s_ScopeStack.Count == 0)
                return;

            s_ScopeStack.Pop();
        }

        [RequiredByNativeCode]
        static void InitImportCallbacks(AssetImportContext context, string pathName, Type importerType, AssetImporter importer, double importStartTime)
        {
            PushScope(new Scope(importer, context));
            AssetPostprocessingInternal.InitPostprocessors(context, pathName, importerType, importStartTime);
        }

        // For imports that run postprocessors without an importer instance, such as scene processing.
        // Blocks hang off an importer's collection, so a scope without one reaches no blocks.
        [RequiredByNativeCode]
        static void InitImportCallbacksWithoutImporter(AssetImportContext context, string pathName, Type importerType, double importStartTime)
        {
            PushScope(new Scope(null, context));
            AssetPostprocessingInternal.InitPostprocessors(context, pathName, importerType, importStartTime);
        }

        [RequiredByNativeCode]
        static void InitImportCallbacksForTextureGenerator(string pathName)
        {
            PushScope(new Scope(null, null));
            AssetPostprocessingInternal.InitPostprocessorsForTextureGenerator(pathName);
        }

        [RequiredByNativeCode]
        static void CleanupImportCallbacks()
        {
            try
            {
                AssetPostprocessingInternal.CleanupPostprocessors();
            }
            finally
            {
                PopScope();
            }
        }

        static int RunPostprocessorRange(string methodName, object[] args, int minOrder, int maxOrder, bool analyticsEnabled, ref float duration)
        {
            if (!analyticsEnabled)
            {
                AssetPostprocessingInternal.CallPostProcessMethodsInRange(methodName, args, minOrder, maxOrder);
                return 0;
            }

            return AssetPostprocessingInternal.CallPostProcessMethodsInRange(methodName, args, minOrder, maxOrder, ref duration);
        }

        static int RunEarlyRange(string methodName, object[] args, bool analyticsEnabled, ref float duration)
        {
            return RunPostprocessorRange(methodName, args, int.MinValue, k_PostprocessOrderThreshold - 1, analyticsEnabled, ref duration);
        }

        static int RunLateRange(string methodName, object[] args, bool analyticsEnabled, ref float duration)
        {
            return RunPostprocessorRange(methodName, args, k_PostprocessOrderThreshold, int.MaxValue, analyticsEnabled, ref duration);
        }

        static bool RunEarlyRangeUntilReturnedObjectIsValid<T>(string methodName, object[] args, out T returnedObject, ref float duration) where T : class
        {
            return AssetPostprocessingInternal.CallPostProcessMethodsInRangeUntilReturnedObjectIsValid(methodName, args, int.MinValue, k_PostprocessOrderThreshold - 1, out returnedObject, ref duration);
        }

        static bool RunLateRangeUntilReturnedObjectIsValid<T>(string methodName, object[] args, out T returnedObject, ref float duration) where T : class
        {
            return AssetPostprocessingInternal.CallPostProcessMethodsInRangeUntilReturnedObjectIsValid(methodName, args, k_PostprocessOrderThreshold, int.MaxValue, out returnedObject, ref duration);
        }

        static int RunAllPostprocessors(string methodName, object[] args, bool analyticsEnabled, ref float duration)
        {
            return RunPostprocessorRange(methodName, args, int.MinValue, int.MaxValue, analyticsEnabled, ref duration);
        }

        static bool RunAllPostprocessorsUntilReturnedObjectIsValid<T>(string methodName, object[] args, out T returnedObject, ref float duration) where T : class
        {
            return AssetPostprocessingInternal.CallPostProcessMethodsInRangeUntilReturnedObjectIsValid(methodName, args, int.MinValue, int.MaxValue, out returnedObject, ref duration);
        }

        [RequiredByNativeCode]
        internal static void DispatchPreprocessModel(string pathName)
        {
            object[] args = null;
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPreprocessModel", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPreprocessModel", invocationCount, duration);
        }

        [RequiredByNativeCode]
        internal static void DispatchPreprocessAnimation(string pathName)
        {
            object[] args = null;
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPreprocessAnimation", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPreprocessAnimation", invocationCount, duration);
        }

        [RequiredByNativeCode]
        internal static void DispatchPostprocessModel(GameObject gameObject)
        {
            object[] args = { gameObject };
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPostprocessModel", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPostprocessModel", invocationCount, duration);
        }

        [RequiredByNativeCode]
        internal static void DispatchPostprocessMeshHierarchy(GameObject root)
        {
            object[] args = { root };
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPostprocessMeshHierarchy", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPostprocessMeshHierarchy", invocationCount, duration);
        }

        [RequiredByNativeCode]
        internal static void DispatchPostprocessAnimation(GameObject root, AnimationClip clip)
        {
            object[] args = { root, clip };
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPostprocessAnimation", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPostprocessAnimation", invocationCount, duration);
        }

        [RequiredByNativeCode]
        internal static void DispatchPostprocessMaterial(Material material)
        {
            object[] args = { material };
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPostprocessMaterial", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPostprocessMaterial", invocationCount, duration);
        }

        [RequiredByNativeCode]
        internal static void DispatchPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] animations)
        {
            object[] args = { description, material, animations };
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPreprocessMaterialDescription", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPreprocessMaterialDescription", invocationCount, duration);
        }

        [RequiredByNativeCode]
        internal static void DispatchPreprocessCameraDescription(AssetImportContext assetImportContext, CameraDescription description, Camera camera, AnimationClip[] animations)
        {
            assetImportContext.DependsOnCustomDependency(AssetPostprocessingInternal.kCameraPostprocessorDependencyName);

            object[] args = { description, camera, animations };
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPreprocessCameraDescription", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPreprocessCameraDescription", invocationCount, duration);
        }

        [RequiredByNativeCode]
        internal static void DispatchPreprocessLightDescription(AssetImportContext assetImportContext, LightDescription description, Light light, AnimationClip[] animations)
        {
            assetImportContext.DependsOnCustomDependency(AssetPostprocessingInternal.kLightPostprocessorDependencyName);

            object[] args = { description, light, animations };
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPreprocessLightDescription", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPreprocessLightDescription", invocationCount, duration);
        }

        [RequiredByNativeCode]
        internal static bool DispatchHasAssignMaterial()
        {
            if (AssetPostprocessingInternal.ProcessMeshHasAssignMaterial())
                return true;


            return false;
        }

        [RequiredByNativeCode]
        internal static bool DispatchHasPostprocessGameObjectWithUserProperties()
        {
            if (AssetPostprocessingInternal.HasPostprocessGameObjectWithUserProperties())
                return true;


            return false;
        }

        [RequiredByNativeCode]
        internal static bool DispatchHasPostprocessGameObjectWithAnimatedUserProperties()
        {
            if (AssetPostprocessingInternal.HasPostprocessGameObjectWithAnimatedUserProperties())
                return true;


            return false;
        }

        // The analytics count is postprocessor invocations only, so a material supplied by a block leaves it at 0.
        [RequiredByNativeCode]
        internal static Material DispatchAssignMaterial(Renderer renderer, Material material)
        {
            object[] args = { material, renderer };   // material first
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            bool foundByPostprocessor = RunAllPostprocessorsUntilReturnedObjectIsValid("OnAssignMaterialModel", args, out Material assignedMaterial, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnAssignMaterialModel", foundByPostprocessor ? 1 : 0, duration);

            return assignedMaterial;
        }

        [RequiredByNativeCode]
        internal static void DispatchPostprocessGameObjectWithUserProperties(GameObject root, string[] propNames, object[] propValues)
        {
            object[] args = { root, propNames, propValues };
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPostprocessGameObjectWithUserProperties", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPostprocessGameObjectWithUserProperties", invocationCount, duration);
        }

        // One conversion and one copy-back: the same array threads through both walks and the block step between them.
        [RequiredByNativeCode]
        internal static void DispatchPostprocessGameObjectWithAnimatedUserProperties(GameObject root, IntPtr bindingsPtr)
        {
            var bindings = AnimationUtility.BindingsArrayPtrToBindingsArray(bindingsPtr);
            object[] args = { root, bindings };
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPostprocessGameObjectWithAnimatedUserProperties", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPostprocessGameObjectWithAnimatedUserProperties", invocationCount, duration);

            AnimationUtility.CopyBindingsArrayToBindingsArrayPtr(bindings, bindingsPtr);
        }

        [RequiredByNativeCode]
        internal static void DispatchPreprocessTexture(string pathName, AssetImportContext context)
        {
            context?.DependsOnCustomDependency(AssetPostprocessingInternal.kTexturePreprocessorDependencyName);

            object[] args = null;
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPreprocessTexture", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPreprocessTexture", invocationCount, duration);
        }

        // Note: The postprocess callbacks below are also called from TextureGenerator.GenerateTextureScripting, which pushes a scope with no importer.
        
        [RequiredByNativeCode]
        internal static void DispatchPostprocessTexture(Texture2D tex, string pathName, AssetImportContext context)
        {
            context?.DependsOnCustomDependency(AssetPostprocessingInternal.kTexture2DPostprocessorDependencyName);

            object[] args = { tex };
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPostprocessTexture", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPostprocessTexture", invocationCount, duration);
        }

        [RequiredByNativeCode]
        internal static void DispatchPostprocessCubemap(Cubemap tex, string pathName, AssetImportContext context)
        {
            context?.DependsOnCustomDependency(AssetPostprocessingInternal.kTextureCubePostprocessorDependencyName);

            object[] args = { tex };
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPostprocessCubemap", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPostprocessCubemap", invocationCount, duration);
        }

        [RequiredByNativeCode]
        internal static void DispatchPostprocessTexture3D(Texture3D tex, string pathName, AssetImportContext context)
        {
            context?.DependsOnCustomDependency(AssetPostprocessingInternal.kTexture3DPostprocessorDependencyName);

            object[] args = { tex };
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPostprocessTexture3D", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPostprocessTexture3D", invocationCount, duration);
        }

        [RequiredByNativeCode]
        internal static void DispatchPostprocessTexture2DArray(Texture2DArray tex, string pathName, AssetImportContext context)
        {
            context?.DependsOnCustomDependency(AssetPostprocessingInternal.kTexture2DArrayPostprocessorDependencyName);

            object[] args = { tex };
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPostprocessTexture2DArray", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPostprocessTexture2DArray", invocationCount, duration);
        }

        [RequiredByNativeCode]
        internal static void DispatchPostprocessSprites(Texture2D tex, string pathName, Sprite[] sprites, AssetImportContext context)
        {
            context?.DependsOnCustomDependency(AssetPostprocessingInternal.kTextureSpritePostprocessorDependencyName);

            object[] args = { tex, sprites };
            bool analyticsEnabled = AssetPostprocessingInternal.IsAssetPostprocessorAnalyticsEnabled();
            float duration = 0f;

            int invocationCount = RunAllPostprocessors("OnPostprocessSprites", args, analyticsEnabled, ref duration);

            if (analyticsEnabled)
                AssetPostprocessingInternal.RecordPostProcessMethodCallAnalytics("OnPostprocessSprites", invocationCount, duration);
        }
    }
}
