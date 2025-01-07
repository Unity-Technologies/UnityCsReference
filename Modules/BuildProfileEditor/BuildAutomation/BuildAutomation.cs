// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.Build.Profile;

internal class BuildAutomation
{
    const string k_BuildAutomationPackageAssemblyName = "Unity.Services.CloudBuild.Editor";
    const string k_BuildAutomationPackageTypeName = "Unity.Services.CloudBuild.Editor.BuildAutomation";
    const string k_BuildAutomationBuildMethodTypeName = "LaunchBuild";

    internal static void OnCloudBuildClicked(BuildProfile profile)
    {
        var assemblyQualifiedName = $"{k_BuildAutomationPackageTypeName}, {k_BuildAutomationPackageAssemblyName}";
        var packageType = Type.GetType(assemblyQualifiedName);
        var launchBuildMethod = packageType?.GetMethod(
            k_BuildAutomationBuildMethodTypeName,
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(BuildProfile) },
            null);

        if (launchBuildMethod == null)
        {
            Debug.LogError("Failed to locate the LaunchBuild method in the Build Automation Package. Please make sure you have the latest version of the Build Automation Package installed.");
        }

        launchBuildMethod?.Invoke(null, new[] { profile });
    }
}
