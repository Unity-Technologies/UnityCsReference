// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.Build
{
    public interface IOrderedCallback
    {
        int callbackOrder { get; }
    }

    public interface IPreprocessBuild : IOrderedCallback
    {
        void OnPreprocessBuild(BuildTarget target, string path);
    }

    public interface IPostprocessBuild : IOrderedCallback
    {
        void OnPostprocessBuild(BuildTarget target, string path);
    }

    public interface IProcessScene : IOrderedCallback
    {
        void OnProcessScene(UnityEngine.SceneManagement.Scene scene);
    }

    public interface IActiveBuildTargetChanged : IOrderedCallback
    {
        void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget);
    }

    internal static class BuildPipelineInterfaces
    {
        private static List<IPreprocessBuild> buildPreprocessors;
        private static List<IPostprocessBuild> buildPostprocessors;
        private static List<IProcessScene> sceneProcessors;
        private static List<IActiveBuildTargetChanged> buildTargetProcessors;

        [Flags]
        internal enum BuildCallbacks
        {
            None = 0,
            BuildProcessors = 1,
            SceneProcessors = 2,
            BuildTargetProcessors = 4
        }

        //common comparer for all callback types
        static int CompareICallbackOrder(IOrderedCallback a, IOrderedCallback b)
        {
            return a.callbackOrder - b.callbackOrder;
        }

        static void AddToList<T>(object o, ref List<T> list) where T : class
        {
            T inst = o as T;
            if (inst == null)
                return;
            if (list == null)
                list = new List<T>();
            list.Add(inst);
        }

        private class AttributeCallbackWrapper : IPostprocessBuild, IProcessScene, IActiveBuildTargetChanged
        {
            int m_callbackOrder;
            MethodInfo m_method;
            public int callbackOrder { get { return m_callbackOrder; } }

            public AttributeCallbackWrapper(MethodInfo m)
            {
                m_callbackOrder = ((CallbackOrderAttribute)Attribute.GetCustomAttribute(m, typeof(CallbackOrderAttribute))).callbackOrder;
                m_method = m;
            }

            public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
            {
                m_method.Invoke(null, new object[] { previousTarget, newTarget });
            }

            public void OnPostprocessBuild(BuildTarget target, string path)
            {
                m_method.Invoke(null, new object[] { target, path });
            }

            public void OnProcessScene(UnityEngine.SceneManagement.Scene scene)
            {
                m_method.Invoke(null, null);
            }
        }

        static BuildCallbacks previousFlags = BuildCallbacks.None;
        [RequiredByNativeCode]
        internal static void InitializeBuildCallbacks(BuildCallbacks findFlags)
        {
            if (findFlags == previousFlags)
                return;

            CleanupBuildCallbacks();
            previousFlags = findFlags;

            bool findBuildProcessors = (findFlags & BuildCallbacks.BuildProcessors) == BuildCallbacks.BuildProcessors;
            bool findSceneProcessors = (findFlags & BuildCallbacks.SceneProcessors) == BuildCallbacks.SceneProcessors;
            bool findTargetProcessors = (findFlags & BuildCallbacks.BuildTargetProcessors) == BuildCallbacks.BuildTargetProcessors;
            var postProcessBuildAttributeParams = new Type[] { typeof(BuildTarget), typeof(string) };
            foreach (var t in EditorAssemblies.GetAllTypesWithInterface(typeof(IOrderedCallback), true))
            {
                if (t.IsAbstract || t.IsInterface)
                    continue;
                object instance = null;
                if (findBuildProcessors)
                {
                    if (ValidateType<IPreprocessBuild>(t))
                        AddToList(instance = Activator.CreateInstance(t), ref buildPreprocessors);

                    if (ValidateType<IPostprocessBuild>(t))
                        AddToList(instance = instance == null ? Activator.CreateInstance(t) : instance, ref buildPostprocessors);
                }

                if (findSceneProcessors && ValidateType<IProcessScene>(t))
                    AddToList(instance = instance == null ? Activator.CreateInstance(t) : instance, ref sceneProcessors);

                if (findTargetProcessors && ValidateType<IActiveBuildTargetChanged>(t))
                    AddToList(instance = instance == null ? Activator.CreateInstance(t) : instance, ref buildTargetProcessors);
            }

            if (findBuildProcessors)
            {
                foreach (var m in EditorAssemblies.GetAllMethodsWithAttribute(typeof(Callbacks.PostProcessBuildAttribute), true, true, true))
                    if (ValidateMethod<Callbacks.PostProcessBuildAttribute>(m, postProcessBuildAttributeParams))
                        AddToList(new AttributeCallbackWrapper(m), ref buildPostprocessors);
            }

            if (findSceneProcessors)
            {
                foreach (var m in EditorAssemblies.GetAllMethodsWithAttribute(typeof(Callbacks.PostProcessSceneAttribute), true, true, true))
                    if (ValidateMethod<Callbacks.PostProcessSceneAttribute>(m, Type.EmptyTypes))
                        AddToList(new AttributeCallbackWrapper(m), ref sceneProcessors);
            }

            if (buildPreprocessors != null)
                buildPreprocessors.Sort(CompareICallbackOrder);
            if (buildPostprocessors != null)
                buildPostprocessors.Sort(CompareICallbackOrder);
            if (buildTargetProcessors != null)
                buildTargetProcessors.Sort(CompareICallbackOrder);
            if (sceneProcessors != null)
                sceneProcessors.Sort(CompareICallbackOrder);
        }

        internal static bool ValidateType<T>(Type t)
        {
            return (typeof(T).IsAssignableFrom(t) && t != typeof(AttributeCallbackWrapper));
        }

        static bool ValidateMethod<T>(MethodInfo method, Type[] expectedArguments)
        {
            Type attribute = typeof(T);
            if (method.IsDefined(attribute, false))
            {
                // Remove the `Attribute` from the name.
                if (!method.IsStatic)
                {
                    string atributeName = attribute.Name.Replace("Attribute", "");
                    Debug.LogErrorFormat("Method {0} with {1} attribute must be static.", method.Name, atributeName);
                    return false;
                }

                if (method.IsGenericMethod || method.IsGenericMethodDefinition)
                {
                    string atributeName = attribute.Name.Replace("Attribute", "");
                    Debug.LogErrorFormat("Method {0} with {1} attribute cannot be generic.", method.Name, atributeName);
                    return false;
                }

                var parameters = method.GetParameters();
                bool signatureCorrect = parameters.Length == expectedArguments.Length;
                if (signatureCorrect)
                {
                    // Check types match
                    for (int i = 0; i < parameters.Length; ++i)
                    {
                        if (parameters[i].ParameterType != expectedArguments[i])
                        {
                            signatureCorrect = false;
                            break;
                        }
                    }
                }

                if (!signatureCorrect)
                {
                    string atributeName = attribute.Name.Replace("Attribute", "");
                    string expectedArgumentsString = "static void " + method.Name + "(";

                    for (int i = 0; i < expectedArguments.Length; ++i)
                    {
                        expectedArgumentsString += expectedArguments[i].Name;
                        if (i != expectedArguments.Length - 1)
                            expectedArgumentsString += ", ";
                    }
                    expectedArgumentsString += ")";

                    Debug.LogErrorFormat("Method {0} with {1} attribute does not have the correct signature, expected: {2}.", method.Name, atributeName, expectedArgumentsString);
                    return false;
                }
                return true;
            }
            return false;
        }

        [RequiredByNativeCode]
        internal static void OnBuildPreProcess(BuildTarget platform, string path, bool strict)
        {
            if (buildPreprocessors != null)
            {
                foreach (IPreprocessBuild bpp in buildPreprocessors)
                {
                    try
                    {
                        bpp.OnPreprocessBuild(platform, path);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        if (strict)
                            return;
                    }
                }
            }
        }

        [RequiredByNativeCode]
        internal static void OnSceneProcess(UnityEngine.SceneManagement.Scene scene, bool strict)
        {
            if (sceneProcessors != null)
            {
                foreach (IProcessScene spp in sceneProcessors)
                {
                    try
                    {
                        spp.OnProcessScene(scene);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        if (strict)
                            return;
                    }
                }
            }
        }

        [RequiredByNativeCode]
        internal static void OnBuildPostProcess(BuildTarget platform, string path, bool strict)
        {
            if (buildPostprocessors != null)
            {
                foreach (IPostprocessBuild bpp in buildPostprocessors)
                {
                    try
                    {
                        bpp.OnPostprocessBuild(platform, path);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        if (strict)
                            return;
                    }
                }
            }
        }

        [RequiredByNativeCode]
        internal static void OnActiveBuildTargetChanged(BuildTarget previousPlatform, BuildTarget newPlatform)
        {
            if (buildTargetProcessors != null)
            {
                foreach (IActiveBuildTargetChanged abtc in buildTargetProcessors)
                {
                    try
                    {
                        abtc.OnActiveBuildTargetChanged(previousPlatform, newPlatform);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        [RequiredByNativeCode]
        internal static void CleanupBuildCallbacks()
        {
            buildTargetProcessors = null;
            buildPreprocessors = null;
            buildPostprocessors = null;
            sceneProcessors = null;
        }
    }
}
