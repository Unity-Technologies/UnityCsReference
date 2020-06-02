// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using UnityEngine.Profiling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.AssetImporters;
using Object = UnityEngine.Object;
using UnityEditor.Experimental.AssetImporters;
using UnityEditorInternal;
using Unity.CodeEditor;
using UnityEditor.Profiling;

namespace UnityEditor
{
    // AssetPostprocessor lets you hook into the import pipeline and run scripts prior or after importing assets.
    public partial class AssetPostprocessor
    {
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
        static void PostprocessAllAssets(string[] importedAssets, string[] addedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPathAssets)
        {
            object[] args = { importedAssets, deletedAssets, movedAssets, movedFromPathAssets };
            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                MethodInfo method = assetPostprocessorClass.GetMethod("OnPostprocessAllAssets", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    InvokeMethod(method, args);
                }
            }

            Profiler.BeginSample("SyncVS.PostprocessSyncProject");
            #pragma warning disable 618
            if (ScriptEditorUtility.GetScriptEditorFromPath(CodeEditor.CurrentEditorInstallation) == ScriptEditorUtility.ScriptEditor.Other
                || ScriptEditorUtility.GetScriptEditorFromPath(CodeEditor.CurrentEditorInstallation) == ScriptEditorUtility.ScriptEditor.SystemDefault)
            {
                CodeEditorProjectSync.PostprocessSyncProject(importedAssets, addedAssets, deletedAssets, movedAssets, movedFromPathAssets);
            }
            else
            {
                ///@TODO: we need addedAssets for SyncVS. Make this into a proper API and write tests
                SyncVS.PostprocessSyncProject(importedAssets, addedAssets, deletedAssets, movedAssets, movedFromPathAssets);
            }
            Profiler.EndSample();
        }

        [RequiredByNativeCode]
        static void PreprocessAssembly(string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                InvokeMethodIfAvailable(inst, "OnPreprocessAssembly", new[] { pathName });
            }
        }

        //This is undocumented, and a "safeguard" for when visualstudio gets a new release that is incompatible with ours, so that users can postprocess our csproj to fix it.
        //(or just completely replace them). Hopefully we'll never need this.
        static internal void CallOnGeneratedCSProjectFiles()
        {
            object[] args = {};
            foreach (var method in AllPostProcessorMethodsNamed("OnGeneratedCSProjectFiles"))
            {
                InvokeMethod(method, args);
            }
        }

        //This callback is used by C# code editors to modify the .sln file.
        static internal string CallOnGeneratedSlnSolution(string path, string content)
        {
            foreach (var method in AllPostProcessorMethodsNamed("OnGeneratedSlnSolution"))
            {
                object[] args = { path, content };
                object returnValue = InvokeMethod(method, args);

                if (method.ReturnType == typeof(string))
                    content = (string)returnValue;
            }

            return content;
        }

        // This callback is used by C# code editors to modify the .csproj files.
        static internal string CallOnGeneratedCSProject(string path, string content)
        {
            foreach (var method in AllPostProcessorMethodsNamed("OnGeneratedCSProject"))
            {
                object[] args = { path, content };
                object returnValue = InvokeMethod(method, args);

                if (method.ReturnType == typeof(string))
                    content = (string)returnValue;
            }

            return content;
        }

        //This callback is used by UnityVS to take over project generation from unity
        static internal bool OnPreGeneratingCSProjectFiles()
        {
            object[] args = {};
            bool result = false;
            foreach (var method in AllPostProcessorMethodsNamed("OnPreGeneratingCSProjectFiles"))
            {
                object returnValue = InvokeMethod(method, args);

                if (method.ReturnType == typeof(bool))
                    result = result | (bool)returnValue;
            }
            return result;
        }

        private static IEnumerable<MethodInfo> AllPostProcessorMethodsNamed(string callbackName)
        {
            return GetCachedAssetPostprocessorClasses().Select(assetPostprocessorClass => assetPostprocessorClass.GetMethod(callbackName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)).Where(method => method != null);
        }

        internal class CompareAssetImportPriority : IComparer
        {
            int IComparer.Compare(System.Object xo, System.Object yo)
            {
                int x = ((AssetPostprocessor)xo).GetPostprocessOrder();
                int y = ((AssetPostprocessor)yo).GetPostprocessOrder();
                return x.CompareTo(y);
            }
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

        internal class PostprocessStack
        {
            internal ArrayList m_ImportProcessors = null;
        }

        static ArrayList m_PostprocessStack = null;
        static ArrayList m_ImportProcessors = null;
        static Type[] m_PostprocessorClasses = null;
        static string m_MeshProcessorsHashString = null;
        static string m_TextureProcessorsHashString = null;
        static string m_AudioProcessorsHashString = null;
        static string m_SpeedTreeProcessorsHashString = null;
        static string m_PrefabProcessorsHashString = null;

        static Type[] GetCachedAssetPostprocessorClasses()
        {
            if (m_PostprocessorClasses == null)
                m_PostprocessorClasses = TypeCache.GetTypesDerivedFrom<AssetPostprocessor>().ToArray();
            return m_PostprocessorClasses;
        }

        [RequiredByNativeCode]
        static void InitPostprocessors(AssetImportContext context, string pathName)
        {
            m_ImportProcessors = new ArrayList();
            var analyticsEvent = new AssetPostProcessorAnalyticsData();
            analyticsEvent.importActionId = ((int)Math.Floor(AssetImporter.GetAtPath(pathName).GetImportStartTime() * 1000)).ToString();
            s_AnalyticsEventsStack.Push(analyticsEvent);

            // @TODO: This is just a temporary workaround for the import settings.
            // We should add importers to the asset, persist them and show an inspector for them.
            foreach (Type assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                try
                {
                    var assetPostprocessor = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                    assetPostprocessor.assetPath = pathName;
                    assetPostprocessor.context = context;
                    m_ImportProcessors.Add(assetPostprocessor);
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

            m_ImportProcessors.Sort(new CompareAssetImportPriority());

            // Setup postprocessing stack to support rentrancy (Import asset immediate)
            PostprocessStack postStack = new PostprocessStack();
            postStack.m_ImportProcessors = m_ImportProcessors;
            if (m_PostprocessStack == null)
                m_PostprocessStack = new ArrayList();
            m_PostprocessStack.Add(postStack);
        }

        [RequiredByNativeCode]
        static void CleanupPostprocessors()
        {
            if (m_PostprocessStack != null)
            {
                m_PostprocessStack.RemoveAt(m_PostprocessStack.Count - 1);
                if (m_PostprocessStack.Count != 0)
                {
                    PostprocessStack postStack = (PostprocessStack)m_PostprocessStack[m_PostprocessStack.Count - 1];
                    m_ImportProcessors = postStack.m_ImportProcessors;
                }
            }

            if (s_AnalyticsEventsStack.Peek().postProcessorCalls.Count != 0)
                EditorAnalytics.SendAssetPostprocessorsUsage(s_AnalyticsEventsStack.Peek());

            s_AnalyticsEventsStack.Pop();
        }

        static bool ImplementsAnyOfTheses(Type type, string[] methods)
        {
            foreach (var method in methods)
            {
                if (type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null)
                    return true;
            }
            return false;
        }

        [RequiredByNativeCode]
        static string GetMeshProcessorsHashString()
        {
            if (m_MeshProcessorsHashString != null)
                return m_MeshProcessorsHashString;

            var versionsByType = new SortedList<string, uint>();

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                try
                {
                    var inst = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                    var type = inst.GetType();
                    bool hasAnyPostprocessMethod = ImplementsAnyOfTheses(type, new[]
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
                        "OnPreprocessMaterialDescription"
                    });
                    uint version = inst.GetVersion();
                    if (hasAnyPostprocessMethod)
                    {
                        versionsByType.Add(type.FullName, version);
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
            m_MeshProcessorsHashString = BuildHashString(versionsByType);
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
        static void PreprocessMesh(string pathName)
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

            var versionsByType = new SortedList<string, uint>();

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                try
                {
                    var inst = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                    var type = inst.GetType();
                    bool hasPreProcessMethod = type.GetMethod("OnPreprocessTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    bool hasPostProcessMethod = (type.GetMethod("OnPostprocessTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null) ||
                        (type.GetMethod("OnPostprocessCubemap", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null);
                    uint version = inst.GetVersion();
                    if (hasPreProcessMethod || hasPostProcessMethod)
                    {
                        versionsByType.Add(type.FullName, version);
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

            m_TextureProcessorsHashString = BuildHashString(versionsByType);
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

            var versionsByType = new SortedList<string, uint>();

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                try
                {
                    var inst = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                    var type = inst.GetType();
                    bool hasPreProcessMethod = type.GetMethod("OnPreprocessAudio", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    bool hasPostProcessMethod = type.GetMethod("OnPostprocessAudio", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    uint version = inst.GetVersion();
                    if (hasPreProcessMethod || hasPostProcessMethod)
                    {
                        versionsByType.Add(type.FullName, version);
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

            m_AudioProcessorsHashString = BuildHashString(versionsByType);
            return m_AudioProcessorsHashString;
        }

        [RequiredByNativeCode]
        static void PreprocessAudio(string pathName)
        {
            CallPostProcessMethods("OnPreprocessAudio", null);
        }

        static Stack<AssetPostProcessorAnalyticsData> s_AnalyticsEventsStack = new Stack<AssetPostProcessorAnalyticsData>();

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

            var versionsByType = new SortedList<string, uint>();

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                try
                {
                    var inst = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                    var type = inst.GetType();
                    bool hasPostProcessMethod = type.GetMethod("OnPostprocessPrefab", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    uint version = inst.GetVersion();
                    if (version != 0 && hasPostProcessMethod)
                    {
                        versionsByType.Add(type.FullName, version);
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

            m_PrefabProcessorsHashString = BuildHashString(versionsByType);
            return m_PrefabProcessorsHashString;
        }

        [RequiredByNativeCode]
        static void PostprocessPrefab(GameObject prefabAssetRoot)
        {
            object[] args = { prefabAssetRoot };
            CallPostProcessMethods("OnPostprocessPrefab", args);
        }

        [RequiredByNativeCode]
        static void PostprocessAssetbundleNameChanged(string assetPath, string prevoiusAssetBundleName, string newAssetBundleName)
        {
            object[] args = { assetPath, prevoiusAssetBundleName, newAssetBundleName };

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

            var versionsByType = new SortedList<string, uint>();

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                try
                {
                    var inst = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                    var type = inst.GetType();
                    bool hasPreProcessMethod = type.GetMethod("OnPreprocessSpeedTree", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    bool hasPostProcessMethod = type.GetMethod("OnPostprocessSpeedTree", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    uint version = inst.GetVersion();
                    if (hasPreProcessMethod || hasPostProcessMethod)
                    {
                        versionsByType.Add(type.FullName, version);
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

            m_SpeedTreeProcessorsHashString = BuildHashString(versionsByType);
            return m_SpeedTreeProcessorsHashString;
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
            using (new EditorPerformanceTracker(method.DeclaringType.Name + "." + method.Name))
            {
                res = method.Invoke(null, args);
            }

            return res;
        }

        static bool InvokeMethodIfAvailable(object target, string methodName, object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                using (new EditorPerformanceTracker(target.GetType().Name + "." + methodName))
                {
                    method.Invoke(target, args);
                }

                return true;
            }
            return false;
        }

        static bool InvokeMethodIfAvailable<T>(object target, string methodName, object[] args, ref T returnedObject) where T : class
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                using (new EditorPerformanceTracker(target.GetType().Name + "." + methodName))
                {
                    returnedObject = method.Invoke(target, args) as T;
                }

                return true;
            }
            return false;
        }
    }
}
