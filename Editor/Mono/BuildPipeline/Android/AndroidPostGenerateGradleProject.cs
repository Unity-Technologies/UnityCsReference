// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

namespace UnityEditor.Android
{
    /// <summary>
    /// Implement this interface to receive a callback after the Android Gradle project is generated. Inherited from UnityEditor.Build.IOrderedCallback.
    /// </summary>
    public interface IPostGenerateGradleAndroidProject : IOrderedCallback
    {
        /// <summary>
        /// Implement this function to receive a callback after the Android Gradle project is generated and before the build process begins.
        /// </summary>
        /// 
        /// <remarks>
        /// Use this function to modify the generated Gradle files before the build process begins.
        /// 
        /// **Note**: To compile the script for this function as an Editor script and prevent any compilation errors related to the `UnityEditor.Android` namespace, use one of these methods:
        /// 
        ///* Place the script in the `Assets/Editor` folder of your project.
        ///* &lt;a href="../Manual/assembly-definitions-creating.html#create-editor-assembly"&gt;Create an assembly definition file&lt;/a&gt; that allows you to place the script in any folder of your project.
        /// </remarks>
        /// <param name="path">The path to the root of the Unity library Gradle project. Note: when exporting the project, this parameter holds the path to the Unity library in the folder specified for export.</param>
        /// <example>
        /// <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/Android/IPostGenerateGradleAndroidProject_OnPostGenerateGradleAndroidProject.cs"/>
        ///</example>
        void OnPostGenerateGradleAndroidProject(string path);
    }
}
