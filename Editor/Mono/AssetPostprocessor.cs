// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Object = UnityEngine.Object;
using UnityEditor.Experimental.AssetImporters;

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
                    method.Invoke(null, args);
            }

            ///@TODO: we need addedAssets for SyncVS. Make this into a proper API and write tests
            SyncVS.PostprocessSyncProject(importedAssets, addedAssets, deletedAssets, movedAssets, movedFromPathAssets);
        }

        [RequiredByNativeCode]
        static void PreprocessAssembly(string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPreprocessAssembly", new[] { pathName });
            }
        }

        //This is undocumented, and a "safeguard" for when visualstudio gets a new release that is incompatible with ours, so that users can postprocess our csproj to fix it.
        //(or just completely replace them). Hopefully we'll never need this.
        static internal void CallOnGeneratedCSProjectFiles()
        {
            object[] args = {};
            foreach (var method in AllPostProcessorMethodsNamed("OnGeneratedCSProjectFiles"))
            {
                method.Invoke(null, args);
            }
        }

        //This callback is used by C# code editors to modify the .sln file.
        static internal string CallOnGeneratedSlnSolution(string path, string content)
        {
            foreach (var method in AllPostProcessorMethodsNamed("OnGeneratedSlnSolution"))
            {
                object[] args = { path, content };
                object returnValue = method.Invoke(null, args);

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
                object returnValue = method.Invoke(null, args);

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
                object returnValue = method.Invoke(null, args);

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

        static Type[] GetCachedAssetPostprocessorClasses()
        {
            if (m_PostprocessorClasses == null)
                m_PostprocessorClasses = EditorAssemblies.SubclassesOf(typeof(AssetPostprocessor)).ToArray();
            return m_PostprocessorClasses;
        }

        [RequiredByNativeCode]
        static void InitPostprocessors(AssetImportContext context, string pathName)
        {
            m_ImportProcessors = new ArrayList();

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
                    bool hasPreProcessMethod = type.GetMethod("OnPreprocessModel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    bool hasPostprocessMeshHierarchy = type.GetMethod("OnPostprocessMeshHierarchy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    bool hasPostProcessMethod = type.GetMethod("OnPostprocessModel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    uint version = inst.GetVersion();
                    if (version != 0 && (hasPreProcessMethod || hasPostprocessMeshHierarchy || hasPostProcessMethod))
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
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPreprocessAsset", null);
            }
        }

        [RequiredByNativeCode]
        static void PreprocessMesh(string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPreprocessModel", null);
            }
        }

        [RequiredByNativeCode]
        static void PreprocessSpeedTree(string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPreprocessSpeedTree", null);
            }
        }

        [RequiredByNativeCode]
        static void PreprocessAnimation(string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPreprocessAnimation", null);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessAnimation(GameObject root, AnimationClip clip)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { root, clip };
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPostprocessAnimation", args);
            }
        }

        [RequiredByNativeCode]
        static Material ProcessMeshAssignMaterial(Renderer renderer, Material material)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { material, renderer };
                object assignedMaterial = AttributeHelper.InvokeMemberIfAvailable(inst, "OnAssignMaterialModel", args);
                if (assignedMaterial as Material)
                    return assignedMaterial as Material;
            }

            return null;
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
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { root };
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPostprocessMeshHierarchy", args);
            }
        }

        static void PostprocessMesh(GameObject gameObject)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { gameObject };
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPostprocessModel", args);
            }
        }

        static void PostprocessSpeedTree(GameObject gameObject)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { gameObject };
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPostprocessSpeedTree", args);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessMaterial(Material material)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { material };
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPostprocessMaterial", args);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessGameObjectWithUserProperties(GameObject go, string[] prop_names, object[] prop_values)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { go, prop_names, prop_values };
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPostprocessGameObjectWithUserProperties", args);
            }
        }

        [RequiredByNativeCode]
        static EditorCurveBinding[] PostprocessGameObjectWithAnimatedUserProperties(GameObject go, EditorCurveBinding[] bindings)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { go, bindings };
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPostprocessGameObjectWithAnimatedUserProperties", args);
            }
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
                    if (version != 0 && (hasPreProcessMethod || hasPostProcessMethod))
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
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPreprocessTexture", null);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessTexture(Texture2D tex, string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { tex };
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPostprocessTexture", args);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessCubemap(Cubemap tex, string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { tex };
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPostprocessCubemap", args);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessSprites(Texture2D tex, string pathName, Sprite[] sprites)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { tex, sprites };
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPostprocessSprites", args);
            }
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
                    if (version != 0 && (hasPreProcessMethod || hasPostProcessMethod))
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
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPreprocessAudio", null);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessAudio(AudioClip tex, string pathName)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { tex };
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPostprocessAudio", args);
            }
        }

        [RequiredByNativeCode]
        static void PostprocessAssetbundleNameChanged(string assetPAth, string prevoiusAssetBundleName, string newAssetBundleName)
        {
            object[] args = { assetPAth, prevoiusAssetBundleName, newAssetBundleName };

            foreach (var assetPostprocessorClass in GetCachedAssetPostprocessorClasses())
            {
                var assetPostprocessor = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                AttributeHelper.InvokeMemberIfAvailable(assetPostprocessor, "OnPostprocessAssetbundleNameChanged", args);
            }
        }
    }
}
