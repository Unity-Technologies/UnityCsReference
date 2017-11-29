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
using UnityEditor.Animations;

namespace UnityEditor
{


public enum DragAndDropVisualMode
{
    
    None = 0,
    
    Copy = 1,
    
    Link = 2,
    
    Move = 16,
    
    Generic = 4,
    
    Rejected = 32
}

public sealed partial class DragAndDrop
{
    internal static bool HandleDelayedDrag(Rect position, int id, Object objectToDrag)
        {
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition) && evt.clickCount == 1)
                    {
                        if (evt.button == 0 && !(Application.platform == RuntimePlatform.OSXEditor && evt.control == true))
                        {
                            GUIUtility.hotControl = id;
                            DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), id);
                            delay.mouseDownPosition = evt.mousePosition;
                            return true;
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), id);
                        if (delay.CanStartDrag())
                        {
                            GUIUtility.hotControl = 0;
                            DragAndDrop.PrepareStartDrag();
                            Object[] references = { objectToDrag };
                            DragAndDrop.objectReferences = references;
                            DragAndDrop.StartDrag(ObjectNames.GetDragAndDropTitle(objectToDrag));
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }
    
    
    public static void PrepareStartDrag()
        {
            ms_GenericData = null;
            PrepareStartDrag_Internal();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void PrepareStartDrag_Internal () ;

    public static void StartDrag(string title)
        {
            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
            {
                StartDrag_Internal(title);
            }
            else
            {
                Debug.LogError("Drags can only be started from MouseDown or MouseDrag events");
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void StartDrag_Internal (string title) ;

    public extern static Object[] objectReferences
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static string[] paths
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static DragAndDropVisualMode visualMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void AcceptDrag () ;

    [RequiredByNativeCode]
    private static bool HasGenericDragData()
        {
            return ms_GenericData != null;
        }
    
    
    public static object GetGenericData(string type)
        {
            if (ms_GenericData != null && ms_GenericData.Contains(type))
                return ms_GenericData[type];
            else
                return null;
        }
    
    
    public static void SetGenericData(string type, object data)
        {
            if (ms_GenericData == null)
                ms_GenericData =  new Hashtable();
            ms_GenericData[type] = data;
        }
    
    
    public extern static int activeControlID
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    static Hashtable ms_GenericData;
    
    
}


}
