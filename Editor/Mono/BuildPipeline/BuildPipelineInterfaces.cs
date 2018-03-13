// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Linq;
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

        //this variable is reinitialized on domain reload so any calls to Init after a domain reload will set things up correctly
        static BuildCallbacks previousFlags = BuildCallbacks.None;
        [RequiredByNativeCode]
        internal static void InitializeBuildCallbacks(BuildCallbacks findFlags)
        {
            if (findFlags == previousFlags)
                return;

            CleanupBuildCallbacks();
            previousFlags = findFlags;

            var excludedAssemblies = new HashSet<string>();
            excludedAssemblies.Add("UnityEditor");
            excludedAssemblies.Add("UnityEngine.UI");
            excludedAssemblies.Add("Unity.PackageManager");
            excludedAssemblies.Add("UnityEngine.Networking");
            excludedAssemblies.Add("nunit.framework");
            excludedAssemblies.Add("UnityEditor.TreeEditor");
            excludedAssemblies.Add("UnityEditor.Graphs");
            excludedAssemblies.Add("UnityEditor.UI");
            excludedAssemblies.Add("UnityEditor.TestRunner");
            excludedAssemblies.Add("UnityEngine.TestRunner");
            excludedAssemblies.Add("UnityEngine.HoloLens");
            excludedAssemblies.Add("SyntaxTree.VisualStudio.Unity.Bridge");
            excludedAssemblies.Add("UnityEditor.Android.Extensions");
            bool findBuildProcessors = (findFlags & BuildCallbacks.BuildProcessors) == BuildCallbacks.BuildProcessors;
            bool findSceneProcessors = (findFlags & BuildCallbacks.SceneProcessors) == BuildCallbacks.SceneProcessors;
            bool findTargetProcessors = (findFlags & BuildCallbacks.BuildTargetProcessors) == BuildCallbacks.BuildTargetProcessors;
            var methodBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var postProcessBuildAttributeParams = new Type[] { typeof(BuildTarget), typeof(string) };

            for (int ai = 0; ai < EditorAssemblies.loadedAssemblies.Length; ai++)
            {
                var assembly = EditorAssemblies.loadedAssemblies[ai];
                bool assemblyMayHaveAttributes = !excludedAssemblies.Contains(assembly.FullName.Substring(0, assembly.FullName.IndexOf(',')));
                Type[] types = null;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }
                for (int ti = 0; ti < types.Length; ti++)
                {
                    var t = types[ti];
                    if (t == null)
                        continue;

                    object instance = null;
                    bool isIOrderedCallback = false;
                    if (findBuildProcessors)
                    {
                        isIOrderedCallback = typeof(IOrderedCallback).IsAssignableFrom(t);
                        if (isIOrderedCallback)
                        {
                            if (ValidateType<IPreprocessBuild>(t))
                            {
                                instance = Activator.CreateInstance(t);
                                AddToList(instance, ref buildPreprocessors);
                            }
                            if (ValidateType<IPostprocessBuild>(t))
                            {
                                instance = instance == null ? Activator.CreateInstance(t) : instance;
                                AddToList(instance, ref buildPostprocessors);
                            }
                        }
                    }
                    if (findSceneProcessors)
                    {
                        if (!findBuildProcessors || isIOrderedCallback)
                        {
                            if (ValidateType<IProcessScene>(t))
                            {
                                instance = instance == null ? Activator.CreateInstance(t) : instance;
                                AddToList(instance, ref sceneProcessors);
                            }
                        }
                    }
                    if (findTargetProcessors)
                    {
                        if (!findBuildProcessors || isIOrderedCallback)
                        {
                            if (ValidateType<IActiveBuildTargetChanged>(t))
                            {
                                instance = instance == null ? Activator.CreateInstance(t) : instance;
                                AddToList(instance, ref buildTargetProcessors);
                            }
                        }
                    }

                    if (!assemblyMayHaveAttributes)
                        continue;
                    foreach (MethodInfo m in t.GetMethods(methodBindingFlags))
                    {
                        //this skips all property getters/setters and operator overloads
                        if (m.IsSpecialName)
                            continue;
                        if (findBuildProcessors && ValidateMethod<Callbacks.PostProcessBuildAttribute>(m, postProcessBuildAttributeParams))
                            AddToList(new AttributeCallbackWrapper(m), ref buildPostprocessors);

                        if (findSceneProcessors && ValidateMethod<Callbacks.PostProcessSceneAttribute>(m, Type.EmptyTypes))
                            AddToList(new AttributeCallbackWrapper(m), ref sceneProcessors);
                    }
                }
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
            return (!t.IsInterface && !t.IsAbstract && typeof(T).IsAssignableFrom(t) && t != typeof(AttributeCallbackWrapper));
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
            previousFlags = BuildCallbacks.None;
        }
    }
}
