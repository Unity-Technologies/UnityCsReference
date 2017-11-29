// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor
{


public sealed partial class ObjectNames
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string NicifyVariableName (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetClassName (Object obj) ;

    internal static string GetTypeName(Object obj)
        {
            if (obj == null)
                return "Object";

            string pathLower = AssetDatabase.GetAssetPath(obj).ToLower();
            if (pathLower.EndsWith(".unity"))
                return "Scene";
            else if (pathLower.EndsWith(".guiskin"))
                return "GUI Skin";
            else if (System.IO.Directory.Exists(AssetDatabase.GetAssetPath(obj)))
                return "Folder";
            else if (obj.GetType() == typeof(Object))
                return System.IO.Path.GetExtension(pathLower) + " File";
            return ObjectNames.GetClassName(obj);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetDragAndDropTitle (Object obj) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetNameSmart (Object obj, string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetNameSmartWithInstanceID (int instanceID, string name) ;

    [System.Obsolete ("Please use NicifyVariableName instead")]
public static string MangleVariableName(string name) { return NicifyVariableName(name); }
    
    
    [System.Obsolete ("Please use GetInspectorTitle instead")]
public static string GetPropertyEditorTitle(Object obj) { return GetInspectorTitle(obj); }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetUniqueName (string[] existingNames, string name) ;

}


}
