// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

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

    [Obsolete("Use IPostprocessBuildWithReport instead")]
    public interface IPostprocessBuild : IOrderedCallback
    {
        void OnPostprocessBuild(BuildTarget target, string path);
    }

    public interface IPostprocessBuildWithReport : IOrderedCallback
    {
        void OnPostprocessBuild(BuildReport report);
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

    internal static class BuildPipelineInterfaces
    {
#pragma warning disable 618
        private static List<IPreprocessBuild> buildPreprocessors;
        private static List<IPostprocessBuild> buildPostprocessors;
        private static List<IProcessScene> sceneProcessors;
#pragma warning restore 618

        private static List<IPreprocessBuildWithReport> buildPreprocessorsWithReport;
        private static List<IPostprocessBuildWithReport> buildPostprocessorsWithReport;
        private static List<IProcessSceneWithReport> sceneProcessorsWithReport;

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
            var postProcessBuildAttributeParams = new Type[] { typeof(BuildTarget), typeof(string) };
            foreach (var t in EditorAssemblies.GetAllTypesWithInterface<IOrderedCallback>())
            {
                if (t.IsAbstract || t.IsInterface)
                    continue;

                // Defer creating the instance until we actually add it to one of the lists
                object instance = null;

                if (findBuildProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref buildPreprocessors);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref buildPreprocessorsWithReport);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref buildPostprocessors);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref buildPostprocessorsWithReport);
                }

                if (findSceneProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref sceneProcessors);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref sceneProcessorsWithReport);
                }

                if (findTargetProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref buildTargetProcessors);
                }
            }

            if (findBuildProcessors)
            {
                foreach (var m in EditorAssemblies.GetAllMethodsWithAttribute<Callbacks.PostProcessBuildAttribute>())
                    if (ValidateMethod<Callbacks.PostProcessBuildAttribute>(m, postProcessBuildAttributeParams))
                        AddToList(new AttributeCallbackWrapper(m), ref buildPostprocessorsWithReport);
            }

            if (findSceneProcessors)
            {
                foreach (var m in EditorAssemblies.GetAllMethodsWithAttribute<Callbacks.PostProcessSceneAttribute>())
                    if (ValidateMethod<Callbacks.PostProcessSceneAttribute>(m, Type.EmptyTypes))
                        AddToList(new AttributeCallbackWrapper(m), ref sceneProcessorsWithReport);
            }

            if (buildPreprocessors != null)
                buildPreprocessors.Sort(CompareICallbackOrder);
            if (buildPreprocessorsWithReport != null)
                buildPreprocessorsWithReport.Sort(CompareICallbackOrder);
            if (buildPostprocessors != null)
                buildPostprocessors.Sort(CompareICallbackOrder);
            if (buildPostprocessorsWithReport != null)
                buildPostprocessorsWithReport.Sort(CompareICallbackOrder);
            if (buildTargetProcessors != null)
                buildTargetProcessors.Sort(CompareICallbackOrder);
            if (sceneProcessors != null)
                sceneProcessors.Sort(CompareICallbackOrder);
            if (sceneProcessorsWithReport != null)
                sceneProcessorsWithReport.Sort(CompareICallbackOrder);
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
                buildPreprocessors, bpp => bpp.OnPreprocessBuild(report.summary.platform, report.summary.outputPath),
                buildPreprocessorsWithReport, bpp => bpp.OnPreprocessBuild(report),
                (report.summary.options & BuildOptions.StrictMode) != 0 || (report.summary.assetBundleOptions & BuildAssetBundleOptions.StrictMode) != 0);
#pragma warning restore 618
        }

        [RequiredByNativeCode]
        internal static void OnSceneProcess(UnityEngine.SceneManagement.Scene scene, BuildReport report)
        {
#pragma warning disable 618
            InvokeCallbackInterfacesPair(
                sceneProcessors, spp => spp.OnProcessScene(scene),
                sceneProcessorsWithReport, spp => spp.OnProcessScene(scene, report),
                report && ((report.summary.options & BuildOptions.StrictMode) != 0 || (report.summary.assetBundleOptions & BuildAssetBundleOptions.StrictMode) != 0));
#pragma warning restore 618
        }

        [RequiredByNativeCode]
        internal static void OnBuildPostProcess(BuildReport report)
        {
#pragma warning disable 618
            InvokeCallbackInterfacesPair(
                buildPostprocessors, bpp => bpp.OnPostprocessBuild(report.summary.platform, report.summary.outputPath),
                buildPostprocessorsWithReport, bpp => bpp.OnPostprocessBuild(report),
                (report.summary.options & BuildOptions.StrictMode) != 0 || (report.summary.assetBundleOptions & BuildAssetBundleOptions.StrictMode) != 0);
#pragma warning restore 618
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
            buildPreprocessorsWithReport = null;
            buildPostprocessorsWithReport = null;
            sceneProcessorsWithReport = null;
            previousFlags = BuildCallbacks.None;
        }
    }
}
