// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEditor
{
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
            foreach (var assetPostprocessorClass in EditorAssemblies.SubclassesOf(typeof(AssetPostprocessor)))
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
            return EditorAssemblies.SubclassesOf(typeof(AssetPostprocessor)).Select(assetPostprocessorClass => assetPostprocessorClass.GetMethod(callbackName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)).Where(method => method != null);
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

        internal class PostprocessStack
        {
            internal ArrayList m_ImportProcessors = null;
        }

        static ArrayList m_PostprocessStack = null;
        static ArrayList m_ImportProcessors = null;
        static ArrayList m_PostprocessorClasses = null;

        static ArrayList GetCachedAssetPostprocessorClasses()
        {
            if (m_PostprocessorClasses == null)
            {
                m_PostprocessorClasses = new ArrayList();
                foreach (var type in EditorAssemblies.SubclassesOf(typeof(AssetPostprocessor)))
                {
                    m_PostprocessorClasses.Add(type);
                }
            }
            return m_PostprocessorClasses;
        }

        [RequiredByNativeCode]
        static void InitPostprocessors(string pathName)
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
        static uint[] GetMeshProcessorVersions()
        {
            List<uint> versions = new List<uint>();

            foreach (var assetPostprocessorClass in EditorAssemblies.SubclassesOf(typeof(AssetPostprocessor)))
            {
                try
                {
                    var inst = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                    var type = inst.GetType();
                    bool hasPreProcessMethod = type.GetMethod("OnPreprocessModel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    bool hasProcessMeshAssignMethod = type.GetMethod("OnProcessMeshAssingModel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    bool hasPostProcessMethod = type.GetMethod("OnPostprocessModel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    uint version = inst.GetVersion();
                    if (version != 0 && (hasPreProcessMethod || hasProcessMeshAssignMethod || hasPostProcessMethod))
                    {
                        versions.Add(version);
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

            return versions.ToArray();
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
        static uint[] GetTextureProcessorVersions()
        {
            List<uint> versions = new List<uint>();

            foreach (var assetPostprocessorClass in EditorAssemblies.SubclassesOf(typeof(AssetPostprocessor)))
            {
                try
                {
                    var inst = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                    var type = inst.GetType();
                    bool hasPreProcessMethod = type.GetMethod("OnPreprocessTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    bool hasPostProcessMethod = type.GetMethod("OnPostprocessTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
                    uint version = inst.GetVersion();
                    if (version != 0 && (hasPreProcessMethod || hasPostProcessMethod))
                    {
                        versions.Add(version);
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

            return versions.ToArray();
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
        static void PostprocessSprites(Texture2D tex, string pathName, Sprite[] sprites)
        {
            foreach (AssetPostprocessor inst in m_ImportProcessors)
            {
                object[] args = { tex, sprites };
                AttributeHelper.InvokeMemberIfAvailable(inst, "OnPostprocessSprites", args);
            }
        }

        [RequiredByNativeCode]
        static uint[] GetAudioProcessorVersions()
        {
            List<uint> versions = new List<uint>();

            foreach (var assetPostprocessorClass in EditorAssemblies.SubclassesOf(typeof(AssetPostprocessor)))
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
                        versions.Add(version);
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

            return versions.ToArray();
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

            foreach (var assetPostprocessorClass in EditorAssemblies.SubclassesOf(typeof(AssetPostprocessor)))
            {
                var assetPostprocessor = Activator.CreateInstance(assetPostprocessorClass) as AssetPostprocessor;
                AttributeHelper.InvokeMemberIfAvailable(assetPostprocessor, "OnPostprocessAssetbundleNameChanged", args);
            }
        }
    }
}
