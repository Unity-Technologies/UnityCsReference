// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEditor.AI;
using UnityScript.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.AI
{
[MovedFrom("UnityEditor")]
public sealed partial class NavMeshBuilder
{
    public extern static UnityEngine.Object navMeshSettingsObject
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void BuildNavMesh () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void BuildNavMeshAsync () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ClearAllNavMeshes () ;

    public extern static bool isRunning
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Cancel () ;

    internal extern static Object sceneNavMeshData
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public static void BuildNavMeshForMultipleScenes(string[] paths)
        {
            if (paths.Length == 0)
                return;

            for (int i = 0; i < paths.Length; i++)
            {
                for (int j = i + 1; j < paths.Length; j++)
                {
                    if (paths[i] == paths[j])
                        throw new System.Exception("No duplicate scene names are allowed");
                }
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!EditorSceneManager.OpenScene(paths[0]).IsValid())
            {
                throw new System.Exception("Could not open scene: " + paths[0]);
            }
            for (int i = 1; i < paths.Length; ++i)
                EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Additive);

            NavMeshBuilder.BuildNavMesh();
            Object asset = NavMeshBuilder.sceneNavMeshData;

            for (int i = 0; i < paths.Length; ++i)
            {
                if (EditorSceneManager.OpenScene(paths[i]).IsValid())
                {
                    NavMeshBuilder.sceneNavMeshData = asset;
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                }
            }

        }
    
    
}

}
