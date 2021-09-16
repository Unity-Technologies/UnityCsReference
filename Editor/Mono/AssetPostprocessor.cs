// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.AssetImporters;
using Object = UnityEngine.Object;
using UnityEditor.Profiling;

namespace UnityEditor
{
    // AssetPostprocessor lets you hook into the import pipeline and run scripts prior or after importing assets.
    public partial class AssetPostprocessor
    {
        internal struct PostprocessorInfo
        {
            public Type Type { get; }
            public string[] Methods { get; }
            public uint Version { get; }
            public int Priority { get; }
            /// <summary>
            /// StaticDependency is true if any method in the postprocessor is not part of the NonAutomaticDependencyMethods list.
            /// This is used to know which PostprocessorInfo should be used for the static importer dependency hash.
            /// </summary>
            public bool StaticDependency { get; }

            public PostprocessorInfo(Type assetPostprocessorType, int importerPriority)
            {
                Type = assetPostprocessorType;
                Methods = null;
                Version = 0;
                StaticDependency = false;
                Priority = importerPriority;
            }

            public PostprocessorInfo(Type assetPostprocessorType, string[] implementedMethods)
            {
                Type = assetPostprocessorType;
                Methods = implementedMethods;
                StaticDependency = Methods.Intersect(AssetPostprocessingInternal.k_NonAutomaticDependencyMethods).Count() != Methods.Length;

                var inst = (AssetPostprocessor)Activator.CreateInstance(assetPostprocessorType);
                Version = inst.GetVersion();
                Priority = inst.GetPostprocessOrder();
            }
        }

        private string m_PathName;
        private AssetImportContext m_Context;

        // The path name of the asset being imported.
        public string assetPath { get { return m_PathName; } set { m_PathName = value; } }

        // The context of the import, used to specify dependencies
        public AssetImportContext context { get { return m_Context; } internal set { m_Context = value; } }

        // Logs an import warning to the console.
        [ExcludeFromDocs]
        public void LogWarning(string warning)
        {
            Object context = null;
            LogWarning(warning, context);
        }

        public void LogWarning(string warning, [DefaultValue("null")]  Object context) { Debug.LogWarning(warning, context); }

        // Logs an import error message to the console.
        [ExcludeFromDocs]
        public void LogError(string warning)
        {
            Object context = null;
            LogError(warning, context);
        }

        public void LogError(string warning, [DefaultValue("null")]  Object context) { Debug.LogError(warning, context); }

        // Returns the version of the asset postprocessor.
        public virtual uint GetVersion() { return 0; }

        // Reference to the asset importer
        public AssetImporter assetImporter { get { return AssetImporter.GetAtPath(assetPath); } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("To set or get the preview, call EditorUtility.SetAssetPreview or AssetPreview.GetAssetPreview instead", true)]
        public Texture2D preview { get { return null; } set {} }

        // Override the order in which importers are processed.
        public virtual int GetPostprocessOrder() { return 0; }

    }

    internal class AssetPostprocessingInternal
    {
        // What is it:
        // Static postprocessor methods always called for each importer that are part of importer static dependency.
        // No new postprocessors should be added to these lists. Please reach out to #devs-import-workflow to talk about new additions.
        internal static readonly string[] k_NonAutomaticDependencyMethods =
        {
            "OnPreprocessAsset",
        };
        static readonly string[] k_ModelImporterPostprocessors =
        {
            "OnPreprocessModel",
            "OnPostprocessMeshHierarchy",
            "OnPostprocessModel",
            "OnPreprocessAnimation",
            "OnPostprocessAnimation",
            "OnPostprocessGameObjectWithAnimatedUserProperties",
            "OnPostprocessGameObjectWithUserProperties",
            "OnPostprocessMaterial",
            "OnAssignMaterialModel",
            "OnPreprocessMaterialDescription",
        };

        static readonly string[] k_DynamicModelImporterPostprocessors =
        {
            "OnPreprocessCameraDescription",
            "OnPreprocessLightDescription"
        };

        static readonly string[] k_TextureImporterPostprocessors =
        {
            "OnPreprocessTexture",
            "OnPostprocessTexture",
            "OnPostprocessCubemap",
            "OnPostprocessSprites",
            "OnPostprocessTexture3D",
            "OnPostprocessTexture2DArray"
        };
        static readonly string[] k_IHVImporterPostprocessors =
        {
            "OnPostprocessTexture",
        };
        static readonly string[] k_AudioImporterPostprocessors =
        {
            "OnPreprocessAudio",
            "OnPostprocessAudio",
        };
        static readonly string[] k_SpeedTreeImporterPostprocessors =
        {
            "OnPreprocessSpeedTree",
            "OnPostprocessSpeedTree",
        };
        static readonly string[] k_PrefabImporterPostprocessors =
        {
            "OnPostprocessPrefab",
        };
        static readonly string[] k_CameraPostprocessors =
        {
            "OnPreprocessCameraDescription",
        };
        static readonly string[] k_LightPostprocessors =
        {
            "OnPreprocessLightDescription",
        };

        static Dictionary<string, string[]> s_PostprocessorMethodsByDependencyKey;
        static Dictionary<Type, string[]> s_StaticPostprocessorMethodsByImporterType;
        static Dictionary<Type, string[]> s_DynamicPostprocessorMethodsByImporterType;

        static AssetPostprocessingInternal()
        {
            s_StaticPostprocessorMethodsByImporterType = new Dictionary<Type, string[]>();
            s_StaticPostprocessorMethodsByImporterType.Add(typeof(ModelImporter), k_ModelImporterPostprocessors);
            s_StaticPostprocessorMethodsByImporterType.Add(typeof(TextureImporter), k_TextureImporterPostprocessors);
            s_StaticPostprocessorMethodsByImporterType.Add(typeof(IHVImageFormatImporter), k_IHVImporterPostprocessors);
            s_StaticPostprocessorMethodsByImporterType.Add(typeof(SpeedTreeImporter), k_SpeedTreeImporterPostprocessors);
            s_StaticPostprocessorMethodsByImporterType.Add(typeof(AudioImporter), k_AudioImporterPostprocessors);
            s_StaticPostprocessorMethodsByImporterType.Add(typeof(PrefabImporter), k_PrefabImporterPostprocessors);

            s_DynamicPostprocessorMethodsByImporterType = new Dictionary<Type, string[]>();
            s_DynamicPostprocessorMethodsByImporterType.Add(typeof(ModelImporter), k_DynamicModelImporterPostprocessors);

            s_PostprocessorMethodsByDependencyKey = new Dictionary<string, string[]>();
            s_PostprocessorMethodsByDependencyKey.Add(kCameraPostprocessorDependencyName, k_CameraPostprocessors);
            s_PostprocessorMethodsByDependencyKey.Add(kLightPostprocessorDependencyName, k_LightPostprocessors);
        }

        [Serializable]
        class AssetPostProcessorAnalyticsData
        {
            public string importActionId;
            public List<AssetPostProcessorMethodCallAnalyticsData> postProcessorCalls = new List<AssetPostProcessorMethodCallAnalyticsData>();
        }

        [Serializable]
        struct AssetPostProcessorMethodCallAnalyticsData
        {
            public string methodName;
            public float duration_sec;
            public int invocationCount;
        }

        static void LogPostProcessorMissingDefaultConstructor(Type type)
        {
            Debug.LogErrorFormat("{0} requires a default constructor to be used as an asset post processor", type);
        }

        [RequiredByNativeCode]
        // Postprocess on all assets once an automatic import has completed
        static void PostprocessAllAssets(string[] importedAssets, string[] addedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPathAssets, bool didDomainReload)
        {
            object[] args = { importedAssets, deletedAssets, movedAssets, movedFromPathAssets };

            object[] argsWithDidDomainReload = { importedAssets, deletedAssets, movedAssets, movedFromPathAssets, didDomainReload};
            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                const string methodName = "OnPostprocessAllAssets";
                MethodInfo method = assetPostprocessorClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(string).MakeArrayType(), typeof(string).MakeArrayType(), typeof(string).MakeArrayType(), typeof(string).MakeArrayType() }, null);

                if (method != null)
                {
                    if (importedAssets.Length != 0 || addedAssets.Length != 0 || deletedAssets.Length != 0 || movedAssets.Length != 0 || movedFromPathAssets.Length != 0)
                        using (new EditorPerformanceMarker($"{assetPostprocessorClass.Name}.{methodName}", assetPostprocessorClass).Auto())
                            InvokeMethod(method, args);
                }
                else
                {
                    // OnPostprocessAllAssets with didDomainReload parameter
                    method = assetPostprocessorClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(string).MakeArrayType(), typeof(string).MakeArrayType(), typeof(string).MakeArrayType(), typeof(string).MakeArrayType(), typeof(bool)}, null);
                    if (method != null)
                    {
                        using (new EditorPerformanceMarker($"{assetPostprocessorClass.Name}.{methodName}", assetPostprocessorClass).Auto())
                            InvokeMethod(method, argsWithDidDomainReload);
                    }
                }
            }

            using (new EditorPerformanceMarker("SyncVS.PostprocessSyncProject").Auto())
                CodeEditorProjectSync.PostprocessSyncProject(importedAssets, addedAssets, deletedAssets, movedAssets, movedFromPathAssets);
        }

        internal class CompareAssetImportPriority : IComparer<AssetPostprocessor.PostprocessorInfo>, IComparer<AssetPostprocessor>
        {
            public int Compare(AssetPostprocessor.PostprocessorInfo x, AssetPostprocessor.PostprocessorInfo y)
            {
                int xo = x.Priority;
                int yo = y.Priority;

                var compare = xo.CompareTo(yo);
                if (compare == 0)
                {
                    compare = x.Type.FullName.CompareTo(y.Type.FullName);
                    if (compare == 0)
                        compare = x.Type.AssemblyQualifiedName.CompareTo(y.Type.AssemblyQualifiedName);
                }
                return compare;
            }

            public int Compare(AssetPostprocessor x, AssetPostprocessor y)
            {
                var xi = new AssetPostprocessor.PostprocessorInfo(x.GetType(), x.GetPostprocessOrder());
                var yi = new AssetPostprocessor.PostprocessorInfo(y.GetType(), y.GetPostprocessOrder());
                return Compare(xi, yi);
            }
        }

        private static string BuildStaticDependencyHashString(SortedSet<AssetPostprocessor.PostprocessorInfo> list)
        {
            var hashStr = "";
            foreach (var info in list)
            {
                if (info.StaticDependency)
                {
                    hashStr += info.Type.AssemblyQualifiedName;
                    hashStr += '.';
                    hashStr += info.Version;
                    hashStr += '|';
                }
            }

            return hashStr;
        }

        private static string BuildHashString(SortedList<string, uint> list)
        {
            var hashStr = "";
            foreach (var pair in list)
            {
                hashStr += pair.Key;
                hashStr += '.';
                hashStr += pair.Value;
                hashStr += '|';
            }

            return hashStr;
        }

        internal const string kCameraPostprocessorDependencyName = "postprocessor/camera";
        internal const string kLightPostprocessorDependencyName = "postprocessor/light";

        static Stack<SortedSet<AssetPostprocessor>> m_PostprocessStack = null;
        static SortedSet<AssetPostprocessor> m_ImportProcessors = null;

        static Type[] m_PostprocessorClasses = null;
        static string m_MeshProcessorsHashString = null;
        static string m_TextureProcessorsHashString = null;
        static string m_AudioProcessorsHashString = null;
        static string m_SpeedTreeProcessorsHashString = null;
        static string m_PrefabProcessorsHashString = null;
        static string m_CameraProcessorsHashString = null;
        static string m_LightProcessorsHashString = null;

        static Dictionary<Type, SortedSet<AssetPostprocessor.PostprocessorInfo>> s_StaticPostprocessorsPerImporterType = new Dictionary<Type, SortedSet<AssetPostprocessor.PostprocessorInfo>>();
        static Dictionary<Type, SortedSet<AssetPostprocessor.PostprocessorInfo>> s_DynamicPostprocessorsPerImporterType = new Dictionary<Type, SortedSet<AssetPostprocessor.PostprocessorInfo>>();

        static Stack<AssetPostProcessorAnalyticsData> s_AnalyticsEventsStack = new Stack<AssetPostProcessorAnalyticsData>();

        static Type[] GetCachedAssetPostprocessorClasses()
        {
            if (m_PostprocessorClasses == null)
                m_PostprocessorClasses = TypeCache.GetTypesDerivedFrom<AssetPostprocessor>().ToArray();
            return m_PostprocessorClasses;
        }

        [RequiredByNativeCode]
        static void InitPostprocessorsForTextureGenerator(string pathName)
        {
            var analyticsEvent = new AssetPostProcessorAnalyticsData();
            analyticsEvent.importActionId = "None";
            s_AnalyticsEventsStack.Push(analyticsEvent);

            m_ImportProcessors = new SortedSet<AssetPostprocessor>(new CompareAssetImportPriority());
            foreach (var postprocessorInfo in GetSortedStaticPostprocessorTypes(typeof(TextureImporter)))
            {
                var assetPostprocessor = (AssetPostprocessor)Activator.CreateInstance(postprocessorInfo.Type);
                assetPostprocessor.assetPath = pathName;
                assetPostprocessor.context = null;
                m_ImportProcessors.Add(assetPostprocessor);
            }

            foreach (var postprocessorInfo in GetSortedDynamicPostprocessorTypes(typeof(TextureImporter)))
            {
                var assetPostprocessor = (AssetPostprocessor)Activator.CreateInstance(postprocessorInfo.Type);
                assetPostprocessor.assetPath = pathName;
                assetPostprocessor.context = null;
                m_ImportProcessors.Add(assetPostprocessor);
            }

            // Setup postprocessing stack to support reentrancy (Import asset immediate)
            if (m_PostprocessStack == null)
                m_PostprocessStack = new Stack<SortedSet<AssetPostprocessor>>();
            m_PostprocessStack.Push(m_ImportProcessors);
        }

        [RequiredByNativeCode]
        static void InitPostprocessors(AssetImportContext context, string pathName)
        {
            var importer = AssetImporter.GetAtPath(pathName);

            var analyticsEvent = new AssetPostProcessorAnalyticsData();
            analyticsEvent.importActionId = ((int)Math.Floor(importer.GetImportStartTime() * 1000)).ToString();
            s_AnalyticsEventsStack.Push(analyticsEvent);

            m_ImportProcessors = new SortedSet<AssetPostprocessor>(new CompareAssetImportPriority());
            foreach (var postprocessorInfo in GetSortedStaticPostprocessorTypes(importer.GetType()))
            {
                var assetPostprocessor = (AssetPostprocessor)Activator.CreateInstance(postprocessorInfo.Type);
                assetPostprocessor.assetPath = pathName;
                assetPostprocessor.context = context;
                m_ImportProcessors.Add(assetPostprocessor);
            }

            foreach (var postprocessorInfo in GetSortedDynamicPostprocessorTypes(importer.GetType()))
            {
                var assetPostprocessor = (AssetPostprocessor)Activator.CreateInstance(postprocessorInfo.Type);
                assetPostprocessor.assetPath = pathName;
                assetPostprocessor.context = context;
                m_ImportProcessors.Add(assetPostprocessor);
            }

            // Setup postprocessing stack to support reentrancy (Import asset immediate)
            if (m_PostprocessStack == null)
                m_PostprocessStack = new Stack<SortedSet<AssetPostprocessor>>();
            m_PostprocessStack.Push(m_ImportProcessors);
        }

        [RequiredByNativeCode]
        static void CleanupPostprocessors()
        {
            if (m_PostprocessStack != null)
            {
                m_PostprocessStack.Pop();
                m_ImportProcessors = m_PostprocessStack.Count > 0 ? m_PostprocessStack.Peek() : null;
            }

            if (s_AnalyticsEventsStack.Count > 0)
            {
                var lastEvent = s_AnalyticsEventsStack.Pop();
                if (lastEvent.postProcessorCalls.Count > 0)
                    EditorAnalytics.SendAssetPostprocessorsUsage(lastEvent);
            }
        }

        static bool ImplementsAnyOfTheses(Type type, IEnumerable<string> methods, out List<string> usedMethods)
        {
            usedMethods = new List<string>(methods.Where(method => type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null));
            return usedMethods.Count > 0;
        }

        /*
         * Returns the list of actual dynamic postprocessor methods for a particular asset.
         * Note: That where the asset is not yet imported, this list will be empty.
         */
        internal static SortedSet<AssetPostprocessor.PostprocessorInfo> GetSortedDynamicPostprocessorsForAsset(string path)
        {
            var list = new SortedSet<AssetPostprocessor.PostprocessorInfo>(new CompareAssetImportPriority());

            var guid = AssetDatabase.GUIDFromAssetPath(path);
            if (guid.Empty())
                return list;

            //Artifact Infos may contains multiple artifacts, associated with different version of the object (E.g. Main, Preview etc.)
            var artifactInfos = AssetDatabase.GetArtifactInfos(guid);

            var allMethodsNames = new List<string>();
            foreach (var info in artifactInfos)
            {
                if (!info.isCurrentArtifact)
                    continue;

                foreach (var kvp in info.dependencies)
                {
                    if (kvp.Value.type == ArtifactInfoDependencyType.Dynamic)
                    {
                        //Try to retrieve Postprocessor Methods associated with the supplied Dependency keys
                        string dependencyName = kvp.Key.Replace(ArtifactDifferenceReporter.kEnvironment_CustomDependency + "/", "");
                        if (s_PostprocessorMethodsByDependencyKey.TryGetValue(dependencyName, out var methodNames))
                            allMethodsNames.AddRange(methodNames);
                    }
                }
            }

            if (allMethodsNames.Count == 0)
                return list;

            /*
            * The asset has dynamic dependencies to an Asset Postprocessor, so let's find any Postprocessors which
            * implements those methods.
            */
            var distinctMethodNames = allMethodsNames.Distinct();
            foreach (Type assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                if (ImplementsAnyOfTheses(assetPostprocessorClass, distinctMethodNames, out var methods))
                {
                    if (assetPostprocessorClass.GetConstructors().Any(t => t.GetParameters().Count() == 0))
                        list.Add(new AssetPostprocessor.PostprocessorInfo(assetPostprocessorClass, methods.ToArray()));
                    else
                        LogPostProcessorMissingDefaultConstructor(assetPostprocessorClass);
                }
            }

            return list;
        }

        internal static SortedSet<AssetPostprocessor.PostprocessorInfo> GetSortedStaticPostprocessorTypes(Type importer)
        {
            var defaultMethods = new string[]
            {
                "OnPreprocessAsset"
            };
            return GetSortedPostprocessorTypes(importer, s_StaticPostprocessorMethodsByImporterType, defaultMethods,
                s_StaticPostprocessorsPerImporterType);
        }

        /*
         * Returns the list of *possible* dynamic postprocessor methods associated with a particular importer type.
         * See also: GetSortedDynamicPostprocessorsForAsset, to get the actual postprocessor methods for a given asset.
         */
        internal static SortedSet<AssetPostprocessor.PostprocessorInfo> GetSortedDynamicPostprocessorTypes(Type importer)
        {
            return GetSortedPostprocessorTypes(importer, s_DynamicPostprocessorMethodsByImporterType, new string[0],
                s_DynamicPostprocessorsPerImporterType);
        }

        static SortedSet<AssetPostprocessor.PostprocessorInfo> GetSortedPostprocessorTypes(Type importer, Dictionary<Type, string[]> postprocessorMethodsByImporterType, string[] defaultMethods, Dictionary<Type, SortedSet<AssetPostprocessor.PostprocessorInfo>> cache)
        {
            if (cache.TryGetValue(importer, out var cachedPostprocessors))
                return cachedPostprocessors;

            var list = new SortedSet<AssetPostprocessor.PostprocessorInfo>(new CompareAssetImportPriority());
            var allMethodsNames = defaultMethods.ToList();
            var methodsType = importer;
            while (methodsType != null && methodsType != typeof(AssetImporter))
            {
                if (postprocessorMethodsByImporterType.TryGetValue(methodsType, out var methodNames))
                    allMethodsNames.AddRange(methodNames);
                methodsType = methodsType.BaseType;
            }

            foreach (Type assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                if (ImplementsAnyOfTheses(assetPostprocessorClass, allMethodsNames, out var methods))
                {
                    if (assetPostprocessorClass.GetConstructors().Any(t => t.GetParameters().Count() == 0))
                        list.Add(new AssetPostprocessor.PostprocessorInfo(assetPostprocessorClass, methods.ToArray()));
                    else
                        LogPostProcessorMissingDefaultConstructor(assetPostprocessorClass);
                }
            }

            cache.Add(importer, list);

            return list;
        }

        [RequiredByNativeCode]
        static string GetMeshProcessorsHashString()
        {
            if (m_MeshProcessorsHashString != null)
                return m_MeshProcessorsHashString;

            m_MeshProcessorsHashString = BuildStaticDependencyHashString(GetSortedStaticPostprocessorTypes(typeof(ModelImporter)));
            return m_MeshProcessorsHashString;
        }

        [RequiredByNativeCode]
        static void PreprocessAsset()
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                InvokeMethodIfAvailable(inst, "OnPreprocessAsset", null);
            }
        }

        [RequiredByNativeCode]
        static void PreprocessModel(string pathName)
        {
            CallPostProcessMethods("OnPreprocessModel", null);
        }

        [RequiredByNativeCode]
        static void PreprocessSpeedTree(string pathName)
        {
            CallPostProcessMethods("OnPreprocessSpeedTree", null);
        }

        [RequiredByNativeCode]
        static void PreprocessAnimation(string pathName)
        {
            CallPostProcessMethods("OnPreprocessAnimation", null);
        }

        [RequiredByNativeCode]
        static void PostprocessAnimation(GameObject root, AnimationClip clip)
        {
            object[] args = { root, clip };
            CallPostProcessMethods("OnPostprocessAnimation", args);
        }

        [RequiredByNativeCode]
        static Material ProcessMeshAssignMaterial(Renderer renderer, Material material)
        {
            object[] args = { material, renderer };
            Material assignedMaterial;
            CallPostProcessMethodsUntilReturnedObjectIsValid("OnAssignMaterialModel", args, out assignedMaterial);

            return assignedMaterial;
        }

        [RequiredByNativeCode]
        static bool ProcessMeshHasAssignMaterial()
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                if (inst.GetType().GetMethod("OnAssignMaterialModel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null)
                    return true;
            }

            return false;
        }

        [RequiredByNativeCode]
        static void PostprocessMeshHierarchy(GameObject root)
        {
            object[] args = { root };
            CallPostProcessMethods("OnPostprocessMeshHierarchy", args);
        }

        static void PostprocessMesh(GameObject gameObject)
        {
            object[] args = { gameObject };
            CallPostProcessMethods("OnPostprocessModel", args);
        }

        static void PostprocessSpeedTree(GameObject gameObject)
        {
            object[] args = { gameObject };
            CallPostProcessMethods("OnPostprocessSpeedTree", args);
        }

        [RequiredByNativeCode]
        static void PostprocessMaterial(Material material)
        {
            object[] args = { material };
            CallPostProcessMethods("OnPostprocessMaterial", args);
        }

        [RequiredByNativeCode]
        static void PreprocessCameraDescription(AssetImportContext assetImportContext, CameraDescription description, Camera camera, AnimationClip[] animations)
        {
            assetImportContext.DependsOnCustomDependency(kCameraPostprocessorDependencyName);
            object[] args = { description, camera, animations };
            CallPostProcessMethods("OnPreprocessCameraDescription", args);
        }

        [RequiredByNativeCode]
        static void PreprocessLightDescription(AssetImportContext assetImportContext, LightDescription description, Light light, AnimationClip[] animations)
        {
            assetImportContext.DependsOnCustomDependency(kLightPostprocessorDependencyName);
            object[] args = { description, light, animations };
            CallPostProcessMethods("OnPreprocessLightDescription", args);
        }

        [RequiredByNativeCode]
        static void PreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] animations)
        {
            object[] args = { description, material, animations };
            CallPostProcessMethods("OnPreprocessMaterialDescription", args);
        }

        [RequiredByNativeCode]
        static void PostprocessGameObjectWithUserProperties(GameObject go, string[] prop_names, object[] prop_values)
        {
            object[] args = { go, prop_names, prop_values };
            CallPostProcessMethods("OnPostprocessGameObjectWithUserProperties", args);
        }

        [RequiredByNativeCode]
        static EditorCurveBinding[] PostprocessGameObjectWithAnimatedUserProperties(GameObject go, EditorCurveBinding[] bindings)
        {
            object[] args = { go, bindings };
            CallPostProcessMethods("OnPostprocessGameObjectWithAnimatedUserProperties", args);
            return bindings;
        }

        [RequiredByNativeCode]
        static string GetTextureProcessorsHashString()
        {
            if (m_TextureProcessorsHashString != null)
                return m_TextureProcessorsHashString;

            m_TextureProcessorsHashString = BuildStaticDependencyHashString(GetSortedStaticPostprocessorTypes(typeof(TextureImporter)));
            return m_TextureProcessorsHashString;
        }

        [RequiredByNativeCode]
        static void PreprocessTexture(string pathName)
        {
            CallPostProcessMethods("OnPreprocessTexture", null);
        }

        [RequiredByNativeCode]
        static void PostprocessTexture(Texture2D tex, string pathName)
        {
            object[] args = { tex };
            CallPostProcessMethods("OnPostprocessTexture", args);
        }

        [RequiredByNativeCode]
        static void PostprocessCubemap(Cubemap tex, string pathName)
        {
            object[] args = { tex };
            CallPostProcessMethods("OnPostprocessCubemap", args);
        }

        [RequiredByNativeCode]
        static void PostprocessTexture3D(Texture3D tex, string pathName)
        {
            object[] args = { tex };
            CallPostProcessMethods("OnPostprocessTexture3D", args);
        }

        [RequiredByNativeCode]
        static void PostprocessTexture2DArray(Texture2DArray tex, string pathName)
        {
            object[] args = { tex };
            CallPostProcessMethods("OnPostprocessTexture2DArray", args);
        }

        [RequiredByNativeCode]
        static void PostprocessSprites(Texture2D tex, string pathName, Sprite[] sprites)
        {
            object[] args = { tex, sprites };
            CallPostProcessMethods("OnPostprocessSprites", args);
        }

        [RequiredByNativeCode]
        static string GetAudioProcessorsHashString()
        {
            if (m_AudioProcessorsHashString != null)
                return m_AudioProcessorsHashString;

            m_AudioProcessorsHashString = BuildStaticDependencyHashString(GetSortedStaticPostprocessorTypes(typeof(AudioImporter)));
            return m_AudioProcessorsHashString;
        }

        [RequiredByNativeCode]
        static void PreprocessAudio(string pathName)
        {
            CallPostProcessMethods("OnPreprocessAudio", null);
        }

        [RequiredByNativeCode]
        static void PostprocessAudio(AudioClip clip, string pathName)
        {
            object[] args = { clip };
            CallPostProcessMethods("OnPostprocessAudio", args);
        }

        [RequiredByNativeCode]
        static string GetPrefabProcessorsHashString()
        {
            if (m_PrefabProcessorsHashString != null)
                return m_PrefabProcessorsHashString;
            m_PrefabProcessorsHashString = BuildStaticDependencyHashString(GetSortedStaticPostprocessorTypes(typeof(PrefabImporter)));
            return m_PrefabProcessorsHashString;
        }

        [RequiredByNativeCode]
        static void PostprocessPrefab(GameObject prefabAssetRoot)
        {
            object[] args = { prefabAssetRoot };
            CallPostProcessMethods("OnPostprocessPrefab", args);
        }

        [RequiredByNativeCode]
        static void PostprocessAssetbundleNameChanged(string assetPath, string previousAssetBundleName, string newAssetBundleName)
        {
            object[] args = { assetPath, previousAssetBundleName, newAssetBundleName };

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                var assetPostprocessor = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                InvokeMethodIfAvailable(assetPostprocessor, "OnPostprocessAssetbundleNameChanged", args);
            }
        }

        [RequiredByNativeCode]
        static string GetSpeedTreeProcessorsHashString()
        {
            if (m_SpeedTreeProcessorsHashString != null)
                return m_SpeedTreeProcessorsHashString;
            m_SpeedTreeProcessorsHashString = BuildStaticDependencyHashString(GetSortedStaticPostprocessorTypes(typeof(SpeedTreeImporter)));
            return m_SpeedTreeProcessorsHashString;
        }

        [InitializeOnLoadMethod]
        static void RefreshCustomDependencies()
        {
            AssetDatabase.RegisterCustomDependency(kCameraPostprocessorDependencyName, Hash128.Compute(GetCameraProcessorsHashString()));
            AssetDatabase.RegisterCustomDependency(kLightPostprocessorDependencyName, Hash128.Compute(GetLightProcessorsHashString()));
        }

        static void GetProcessorHashString(string methodName, ref string hashString)
        {
            if (hashString != null)
                return;

            var versionsByType = new SortedList<string, uint>();

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                try
                {
                    if (assetPostprocessorClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null)
                    {
                        var inst = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                        uint version = inst.GetVersion();
                        versionsByType.Add(assetPostprocessorClass.FullName, version);
                    }
                }
                catch (MissingMethodException)
                {
                    LogPostProcessorMissingDefaultConstructor(assetPostprocessorClass);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            hashString = BuildHashString(versionsByType);
        }

        [RequiredByNativeCode]
        static string GetCameraProcessorsHashString()
        {
            GetProcessorHashString("OnPreprocessCameraDescription", ref m_CameraProcessorsHashString);
            return m_CameraProcessorsHashString;
        }

        [RequiredByNativeCode]
        static string GetLightProcessorsHashString()
        {
            GetProcessorHashString("OnPreprocessLightDescription", ref m_LightProcessorsHashString);
            return m_LightProcessorsHashString;
        }

        static bool IsAssetPostprocessorAnalyticsEnabled()
        {
            return EditorAnalytics.enabled;
        }

        static void CallPostProcessMethodsUntilReturnedObjectIsValid<T>(string methodName, object[] args, out T returnedObject) where T : class
        {
            returnedObject = default(T);
            int invocationCount = 0;
            float startTime = Time.realtimeSinceStartup;

            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                if (InvokeMethodIfAvailable(inst, methodName, args, ref returnedObject))
                {
                    invocationCount++;
                    break;
                }
            }

            if (IsAssetPostprocessorAnalyticsEnabled() && invocationCount > 0)
            {
                var methodCallAnalytics = new AssetPostProcessorMethodCallAnalyticsData();
                methodCallAnalytics.invocationCount = invocationCount;
                methodCallAnalytics.methodName = methodName;
                methodCallAnalytics.duration_sec = Time.realtimeSinceStartup - startTime;
                s_AnalyticsEventsStack.Peek().postProcessorCalls.Add(methodCallAnalytics);
            }
        }

        static void CallPostProcessMethods(string methodName, object[] args)
        {
            if (m_ImportProcessors == null)
            {
                throw new Exception("m_ImportProcessors is null, InitPostProcessors should be called before any of the post process methods are called.");
            }

            if (IsAssetPostprocessorAnalyticsEnabled())
            {
                int invocationCount = 0;
                float startTime = Time.realtimeSinceStartup;
                foreach (AssetPostprocessor inst in m_ImportProcessors)
                {
                    if (InvokeMethodIfAvailable(inst, methodName, args))
                        invocationCount++;
                }

                if (invocationCount > 0)
                {
                    var methodCallAnalytics = new AssetPostProcessorMethodCallAnalyticsData();
                    methodCallAnalytics.invocationCount = invocationCount;
                    methodCallAnalytics.methodName = methodName;
                    methodCallAnalytics.duration_sec = Time.realtimeSinceStartup - startTime;
                    s_AnalyticsEventsStack.Peek().postProcessorCalls.Add(methodCallAnalytics);
                }
            }
            else
            {
                foreach (AssetPostprocessor inst in m_ImportProcessors)
                {
                    InvokeMethodIfAvailable(inst, methodName, args);
                }
            }
        }

        static object InvokeMethod(MethodInfo method, object[] args)
        {
            object res = null;
            using (new EditorPerformanceMarker(method.DeclaringType.Name + "." + method.Name, method.DeclaringType).Auto())
            {
                res = method.Invoke(null, args);
            }

            return res;
        }

        static bool InvokeMethodIfAvailable(object target, string methodName, object[] args)
        {
            var type = target.GetType();
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                using (new EditorPerformanceMarker(type.Name + "." + methodName, type).Auto())
                {
                    method.Invoke(target, args);
                }

                return true;
            }
            return false;
        }

        static bool InvokeMethodIfAvailable<T>(object target, string methodName, object[] args, ref T returnedObject) where T : class
        {
            var type = target.GetType();
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                using (new EditorPerformanceMarker(type.Name + "." + methodName, type).Auto())
                {
                    returnedObject = method.Invoke(target, args) as T;
                }

                return returnedObject != null;
            }
            return false;
        }
    }
}
