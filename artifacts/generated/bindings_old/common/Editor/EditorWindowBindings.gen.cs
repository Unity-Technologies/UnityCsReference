// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using IntPtr = System.IntPtr;
using System;
using UnityEditor.Scripting;

namespace UnityEditor
{


[UsedByNativeCode]
public partial class EditorWindow : ScriptableObject
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void MakeModal (ContainerWindow win) ;

    [uei.ExcludeFromDocs]
public static EditorWindow GetWindow (System.Type t, bool utility , string title ) {
    bool focus = true;
    return GetWindow ( t, utility, title, focus );
}

[uei.ExcludeFromDocs]
public static EditorWindow GetWindow (System.Type t, bool utility ) {
    bool focus = true;
    string title = null;
    return GetWindow ( t, utility, title, focus );
}

[uei.ExcludeFromDocs]
public static EditorWindow GetWindow (System.Type t) {
    bool focus = true;
    string title = null;
    bool utility = false;
    return GetWindow ( t, utility, title, focus );
}

public static EditorWindow GetWindow(System.Type t, [uei.DefaultValue("false")]  bool utility , [uei.DefaultValue("null")]  string title , [uei.DefaultValue("true")]  bool focus )
        {
            return GetWindowPrivate(t, utility, title, focus);
        }

    
    
    [uei.ExcludeFromDocs]
public static EditorWindow GetWindowWithRect (System.Type t, Rect rect, bool utility ) {
    string title = null;
    return GetWindowWithRect ( t, rect, utility, title );
}

[uei.ExcludeFromDocs]
public static EditorWindow GetWindowWithRect (System.Type t, Rect rect) {
    string title = null;
    bool utility = false;
    return GetWindowWithRect ( t, rect, utility, title );
}

public static EditorWindow GetWindowWithRect(System.Type t, Rect rect, [uei.DefaultValue("false")]  bool utility , [uei.DefaultValue("null")]  string title )
        {
            return GetWindowWithRectPrivate(t, rect, utility, title);
        }

    
    
}


} 
