// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine.Scripting;

namespace UnityEditor.Build
{
    public interface IOrderedCallback
    {
        int callbackOrder { get; }
    }

    [Obsolete("Use IPreprocessBuildWithReport instead")]
    public interface IPreprocessBuild : IOrderedCallback
    {
        void OnPreprocessBuild(BuildTarget target, string path);
    }

    public interface IPreprocessBuildWithReport : IOrderedCallback
    {
        void OnPreprocessBuild(BuildReport report);
    }

    public interface IFilterBuildAssemblies : IOrderedCallback
    {
        string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies);
    }

    [Obsolete("Use IPostprocessBuildWithReport instead")]
    public interface IPostprocessBuild : IOrderedCallback
    {
        void OnPostprocessBuild(BuildTarget target, string path);
    }

    public interface IPostprocessBuildWithReport : IOrderedCallback
    {
        void OnPostprocessBuild(BuildReport report);
    }

    public interface IPostBuildPlayerScriptDLLs : IOrderedCallback
    {
        void OnPostBuildPlayerScriptDLLs(BuildReport report);
    }

    [Obsolete("Use IProcessSceneWithReport instead")]
    public interface IProcessScene : IOrderedCallback
    {
        void OnProcessScene(UnityEngine.SceneManagement.Scene scene);
    }

    public interface IProcessSceneWithReport : IOrderedCallback
    {
        void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report);
    }

    public interface IActiveBuildTargetChanged : IOrderedCallback
    {
        void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget);
    }

    public interface IPreprocessShaders : IOrderedCallback
    {
        void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data);
    }

    internal static class BuildPipelineInterfaces
    {
        internal class Processors
        {
#pragma warning disable 618
            public List<IPreprocessBuild> buildPreprocessors;
            public List<IPostprocessBuild> buildPostprocessors;
            public List<IProcessScene> sceneProcessors;
#pragma warning restore 618

            public List<IPreprocessBuildWithReport> buildPreprocessorsWithReport;
            public List<IPostprocessBuildWithReport> buildPostprocessorsWithReport;
            public List<IProcessSceneWithReport> sceneProcessorsWithReport;

            public List<IFilterBuildAssemblies> filterBuildAssembliesProcessor;
            public List<IActiveBuildTargetChanged> buildTargetProcessors;
            public List<IPreprocessShaders> shaderProcessors;
            public List<IPostBuildPlayerScriptDLLs> buildPlayerScriptDLLProcessors;
        }

        private static Processors m_Processors;
        internal static Processors processors
        {
            get
            {
                m_Processors = m_Processors ?? new Processors();
                return m_Processors;
            }
            set { m_Processors = value; }
        }

        [Flags]
        internal enum BuildCallbacks
        {
            None = 0,
            BuildProcessors = 1,
            SceneProcessors = 2,
            BuildTargetProcessors = 4,
            FilterAssembliesProcessors = 8,
            ShaderProcessors = 16,
            BuildPlayerScriptDLLProcessors = 32
        }

        //common comparer for all callback types
        internal static int CompareICallbackOrder(IOrderedCallback a, IOrderedCallback b)
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

        static void AddToListIfTypeImplementsInterface<T>(Type t, ref object o, ref List<T> list) where T : class
        {
            if (!ValidateType<T>(t))
                return;

            if (o == null)
                o = Activator.CreateInstance(t);
            AddToList(o, ref list);
        }

        private class AttributeCallbackWrapper : IPostprocessBuildWithReport, IProcessSceneWithReport, IActiveBuildTargetChanged
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

            public void OnPostprocessBuild(BuildReport report)
            {
                m_method.Invoke(null, new object[] { report.summary.platform, report.summary.outputPath });
            }

            public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report)
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

            bool findBuildProcessors = (findFlags & BuildCallbacks.BuildProcessors) == BuildCallbacks.BuildProcessors;
            bool findSceneProcessors = (findFlags & BuildCallbacks.SceneProcessors) == BuildCallbacks.SceneProcessors;
            bool findTargetProcessors = (findFlags & BuildCallbacks.BuildTargetProcessors) == BuildCallbacks.BuildTargetProcessors;
            bool findFilterProcessors = (findFlags & BuildCallbacks.FilterAssembliesProcessors) == BuildCallbacks.FilterAssembliesProcessors;
            bool findShaderProcessors = (findFlags & BuildCallbacks.ShaderProcessors) == BuildCallbacks.ShaderProcessors;
            bool findBuildPlayerScriptDLLsProcessors = (findFlags & BuildCallbacks.BuildPlayerScriptDLLProcessors) == BuildCallbacks.BuildPlayerScriptDLLProcessors;

            var postProcessBuildAttributeParams = new Type[] { typeof(BuildTarget), typeof(string) };
            foreach (var t in EditorAssemblies.GetAllTypesWithInterface<IOrderedCallback>())
            {
                if (t.IsAbstract || t.IsInterface)
                    continue;

                // Defer creating the instance until we actually add it to one of the lists
                object instance = null;

                if (findBuildProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildPreprocessors);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildPreprocessorsWithReport);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildPostprocessors);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildPostprocessorsWithReport);
                }

                if (findSceneProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.sceneProcessors);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.sceneProcessorsWithReport);
                }

                if (findTargetProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildTargetProcessors);
                }

                if (findFilterProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.filterBuildAssembliesProcessor);
                }

                if (findShaderProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.shaderProcessors);
                }

                if (findBuildPlayerScriptDLLsProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildPlayerScriptDLLProcessors);
                }
            }

            if (findBuildProcessors)
            {
                foreach (var m in EditorAssemblies.GetAllMethodsWithAttribute<Callbacks.PostProcessBuildAttribute>())
                    if (ValidateMethod<Callbacks.PostProcessBuildAttribute>(m, postProcessBuildAttributeParams))
                        AddToList(new AttributeCallbackWrapper(m), ref processors.buildPostprocessorsWithReport);
            }

            if (findSceneProcessors)
            {
                foreach (var m in EditorAssemblies.GetAllMethodsWithAttribute<Callbacks.PostProcessSceneAttribute>())
                    if (ValidateMethod<Callbacks.PostProcessSceneAttribute>(m, Type.EmptyTypes))
                        AddToList(new AttributeCallbackWrapper(m), ref processors.sceneProcessorsWithReport);
            }

            if (processors.buildPreprocessors != null)
                processors.buildPreprocessors.Sort(CompareICallbackOrder);
            if (processors.buildPreprocessorsWithReport != null)
                processors.buildPreprocessorsWithReport.Sort(CompareICallbackOrder);
            if (processors.buildPostprocessors != null)
                processors.buildPostprocessors.Sort(CompareICallbackOrder);
            if (processors.buildPostprocessorsWithReport != null)
                processors.buildPostprocessorsWithReport.Sort(CompareICallbackOrder);
            if (processors.buildTargetProcessors != null)
                processors.buildTargetProcessors.Sort(CompareICallbackOrder);
            if (processors.sceneProcessors != null)
                processors.sceneProcessors.Sort(CompareICallbackOrder);
            if (processors.sceneProcessorsWithReport != null)
                processors.sceneProcessorsWithReport.Sort(CompareICallbackOrder);
            if (processors.filterBuildAssembliesProcessor != null)
                processors.filterBuildAssembliesProcessor.Sort(CompareICallbackOrder);
            if (processors.shaderProcessors != null)
                processors.shaderProcessors.Sort(CompareICallbackOrder);
            if (processors.buildPlayerScriptDLLProcessors != null)
                processors.buildPlayerScriptDLLProcessors.Sort(CompareICallbackOrder);
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

        private static bool InvokeCallbackInterfacesPair<T1, T2>(List<T1> oneInterfaces, Action<T1> invocationOne, List<T2> twoInterfaces, Action<T2> invocationTwo, bool exitOnFailure) where T1 : IOrderedCallback where T2 : IOrderedCallback
        {
            if (oneInterfaces == null && twoInterfaces == null)
                return true;

            // We want to walk both interface lists and invoke the callbacks, but if we just did the whole of list 1 followed by the whole of list 2, the ordering would be wrong.
            // So, we have to walk both lists simultaneously, calling whichever callback has the lower ordering value
            IEnumerator<T1> e1 = (oneInterfaces != null) ? (IEnumerator<T1>)oneInterfaces.GetEnumerator() : null;
            IEnumerator<T2> e2 = (twoInterfaces != null) ? (IEnumerator<T2>)twoInterfaces.GetEnumerator() : null;
            if (e1 != null && !e1.MoveNext())
                e1 = null;
            if (e2 != null && !e2.MoveNext())
                e2 = null;

            while (e1 != null || e2 != null)
            {
                try
                {
                    if (e1 != null && (e2 == null || e1.Current.callbackOrder < e2.Current.callbackOrder))
                    {
                        var callback = e1.Current;
                        if (!e1.MoveNext())
                            e1 = null;
                        invocationOne(callback);
                    }
                    else if (e2 != null)
                    {
                        var callback = e2.Current;
                        if (!e2.MoveNext())
                            e2 = null;
                        invocationTwo(callback);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    if (exitOnFailure)
                        return false;
                }
            }

            return true;
        }

        [RequiredByNativeCode]
        internal static void OnBuildPreProcess(BuildReport report)
        {
#pragma warning disable 618
            InvokeCallbackInterfacesPair(
                processors.buildPreprocessors, bpp => bpp.OnPreprocessBuild(report.summary.platform, report.summary.outputPath),
                processors.buildPreprocessorsWithReport, bpp => bpp.OnPreprocessBuild(report),
                (report.summary.options & BuildOptions.StrictMode) != 0 || (report.summary.assetBundleOptions & BuildAssetBundleOptions.StrictMode) != 0);
#pragma warning restore 618
        }

        [RequiredByNativeCode]
        internal static void OnSceneProcess(UnityEngine.SceneManagement.Scene scene, BuildReport report)
        {
#pragma warning disable 618
            InvokeCallbackInterfacesPair(
                processors.sceneProcessors, spp => spp.OnProcessScene(scene),
                processors.sceneProcessorsWithReport, spp => spp.OnProcessScene(scene, report),
                report && ((report.summary.options & BuildOptions.StrictMode) != 0 || (report.summary.assetBundleOptions & BuildAssetBundleOptions.StrictMode) != 0));
#pragma warning restore 618
        }

        [RequiredByNativeCode]
        internal static void OnBuildPostProcess(BuildReport report)
        {
#pragma warning disable 618
            InvokeCallbackInterfacesPair(
                processors.buildPostprocessors, bpp => bpp.OnPostprocessBuild(report.summary.platform, report.summary.outputPath),
                processors.buildPostprocessorsWithReport, bpp => bpp.OnPostprocessBuild(report),
                (report.summary.options & BuildOptions.StrictMode) != 0 || (report.summary.assetBundleOptions & BuildAssetBundleOptions.StrictMode) != 0);
#pragma warning restore 618
        }

        [RequiredByNativeCode]
        internal static void OnActiveBuildTargetChanged(BuildTarget previousPlatform, BuildTarget newPlatform)
        {
            if (processors.buildTargetProcessors != null)
            {
                foreach (IActiveBuildTargetChanged abtc in processors.buildTargetProcessors)
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
        internal static ShaderCompilerData[] OnPreprocessShaders(Shader shader, ShaderSnippetData snippet, ShaderCompilerData[] data)
        {
            var dataList = data.ToList();
            if (processors.shaderProcessors != null)
            {
                foreach (IPreprocessShaders abtc in processors.shaderProcessors)
                {
                    try
                    {
                        abtc.OnProcessShader(shader, snippet, dataList);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            return dataList.ToArray();
        }

        [RequiredByNativeCode]
        internal static void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
            if (processors.buildPlayerScriptDLLProcessors != null)
            {
                foreach (var step in processors.buildPlayerScriptDLLProcessors)
                {
                    try
                    {
                        step.OnPostBuildPlayerScriptDLLs(report);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        if ((report.summary.options & BuildOptions.StrictMode) != 0 || (report.summary.assetBundleOptions & BuildAssetBundleOptions.StrictMode) != 0)
                            return;
                    }
                }
            }
        }

        [RequiredByNativeCode]
        internal static string[] FilterAssembliesIncludedInBuild(BuildOptions buildOptions, string[] assemblies)
        {
            if (processors.filterBuildAssembliesProcessor == null)
            {
                return assemblies;
            }

            string[] startAssemblies = assemblies;
            string[] filteredAssemblies = assemblies;


            foreach (var filteredAssembly in processors.filterBuildAssembliesProcessor)
            {
                int assemblyCount = filteredAssemblies.Length;
                filteredAssemblies = filteredAssembly.OnFilterAssemblies(buildOptions, filteredAssemblies);
                if (filteredAssemblies.Length > assemblyCount)
                {
                    throw new Exception("More Assemblies in the list than delivered. Only filtering, not adding extra assemblies");
                }
            }

            if (!filteredAssemblies.All(x => startAssemblies.Contains(x)))
            {
                throw new Exception("New Assembly names are in the list. Only filtering are allowed");
            }

            return filteredAssemblies;
        }

        [RequiredByNativeCode]
        internal static void CleanupBuildCallbacks()
        {
            processors.buildTargetProcessors = null;
            processors.buildPreprocessors = null;
            processors.buildPostprocessors = null;
            processors.sceneProcessors = null;
            processors.buildPreprocessorsWithReport = null;
            processors.buildPostprocessorsWithReport = null;
            processors.sceneProcessorsWithReport = null;
            processors.filterBuildAssembliesProcessor = null;
            processors.shaderProcessors = null;
            processors.buildPlayerScriptDLLProcessors = null;
            previousFlags = BuildCallbacks.None;
        }
    }
}
