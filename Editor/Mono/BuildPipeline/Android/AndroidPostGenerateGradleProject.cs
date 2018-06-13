// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

namespace UnityEditor.Android
{
    public interface IPostGenerateGradleAndroidProject : IOrderedCallback
    {
        void OnPostGenerateGradleAndroidProject(string path);
    }

    internal static class AndroidBuildPipelineInterfaces
    {
        private static List<IPostGenerateGradleAndroidProject> buildPostProjectGeneratedProcessors;

        internal static void InitializeBuildCallbacks()
        {
            buildPostProjectGeneratedProcessors = new List<IPostGenerateGradleAndroidProject>();

            foreach (var type in EditorAssemblies.GetAllTypesWithInterface<IPostGenerateGradleAndroidProject>())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;
                buildPostProjectGeneratedProcessors.Add(Activator.CreateInstance(type) as IPostGenerateGradleAndroidProject);
            }

            buildPostProjectGeneratedProcessors.Sort(BuildPipelineInterfaces.CompareICallbackOrder);
        }

        internal static void OnGeneratePlatformProjectPostprocess(string path, bool strict)
        {
            if (buildPostProjectGeneratedProcessors != null)
            {
                foreach (var pggapp in buildPostProjectGeneratedProcessors)
                {
                    try
                    {
                        pggapp.OnPostGenerateGradleAndroidProject(path);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        if (strict)
                            throw;
                    }
                }
            }
        }
    }
}
