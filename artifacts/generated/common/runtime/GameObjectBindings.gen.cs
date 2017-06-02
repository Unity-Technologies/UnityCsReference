// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngineInternal;
using UnityEngine.SceneManagement;

namespace UnityEngine
{


public sealed partial class GameObject : Object
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  GameObject CreatePrimitive (PrimitiveType type) ;

    [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Component GetComponent (Type type) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void GetComponentFastPath (Type type, IntPtr oneFurtherThanResultValue) ;

    [System.Security.SecuritySafeCritical]
    public unsafe T GetComponent<T>()
        {
            var h = new CastHelper<T>();
            GetComponentFastPath(typeof(T), new System.IntPtr(&h.onePointerFurtherThanT));
            return h.t;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal Component GetComponentByName (string type) ;

    
    public Component GetComponent(string type)
        {
            return GetComponentByName(type);
        }
    
    
    [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Component GetComponentInChildren (Type type, bool includeInactive) ;

    [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
    public Component GetComponentInChildren(Type type)
        {
            return GetComponentInChildren(type, false);
        }
    
    
    
    [uei.ExcludeFromDocs]
public T GetComponentInChildren<T> () {
    bool includeInactive = false;
    return GetComponentInChildren<T> ( includeInactive );
}

public T GetComponentInChildren<T>( [uei.DefaultValue("false")] bool includeInactive )
        {
            return (T)(object)GetComponentInChildren(typeof(T), includeInactive);
        }

    
    
    [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Component GetComponentInParent (Type type) ;

    
    public T GetComponentInParent<T>()
        {
            return (T)(object)GetComponentInParent(typeof(T));
        }
    
    public Component[] GetComponents(Type type)
        {
            return (Component[])GetComponentsInternal(type, false, false, true, false, null);
        }
    
    public T[] GetComponents<T>()
        {
            return (T[])GetComponentsInternal(typeof(T), true, false, true, false, null);
        }
    
    public void GetComponents(Type type, List<Component> results)
        {
            GetComponentsInternal(type, false, false, true, false, results);
        }
    
    public void GetComponents<T>(List<T> results)
        {
            GetComponentsInternal(typeof(T), false, false, true, false, results);
        }
    
    [uei.ExcludeFromDocs]
public Component[] GetComponentsInChildren (Type type) {
    bool includeInactive = false;
    return GetComponentsInChildren ( type, includeInactive );
}

public Component[] GetComponentsInChildren(Type type, [uei.DefaultValue("false")]  bool includeInactive )
        {
            return (Component[])GetComponentsInternal(type, false, true, includeInactive, false, null);
        }

    
    public T[] GetComponentsInChildren<T>(bool includeInactive)
        {
            return (T[])GetComponentsInternal(typeof(T), true, true, includeInactive, false, null);
        }
    
    public void GetComponentsInChildren<T>(bool includeInactive, List<T> results)
        {
            GetComponentsInternal(typeof(T), true, true, includeInactive, false, results);
        }
    
    public T[] GetComponentsInChildren<T>()
        {
            return GetComponentsInChildren<T>(false);
        }
    
    public void GetComponentsInChildren<T>(List<T> results)
        {
            GetComponentsInChildren<T>(false, results);
        }
    
    [uei.ExcludeFromDocs]
public Component[] GetComponentsInParent (Type type) {
    bool includeInactive = false;
    return GetComponentsInParent ( type, includeInactive );
}

public Component[] GetComponentsInParent(Type type, [uei.DefaultValue("false")]  bool includeInactive )
        {
            return (Component[])GetComponentsInternal(type, false, true, includeInactive, true, null);
        }

    
    public void GetComponentsInParent<T>(bool includeInactive, List<T> results)
        {
            GetComponentsInternal(typeof(T), true, true, includeInactive, true, results);
        }
    
    public T[] GetComponentsInParent<T>(bool includeInactive)
        {
            return (T[])GetComponentsInternal(typeof(T), true, true, includeInactive, true, null);
        }
    
    public T[] GetComponentsInParent<T>()
        {
            return GetComponentsInParent<T>(false);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private System.Array GetComponentsInternal (Type type, bool useSearchTypeAsArrayReturnType, bool recursive, bool includeInactive, bool reverse, object resultList) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal Component AddComponentInternal (string className) ;

    public extern  Transform transform
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern int layer
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("GameObject.active is obsolete. Use GameObject.SetActive(), GameObject.activeSelf or GameObject.activeInHierarchy.")]
    public extern  bool active
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
    extern public void SetActive (bool value) ;

    public extern  bool activeSelf
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool activeInHierarchy
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("gameObject.SetActiveRecursively() is obsolete. Use GameObject.SetActive(), which is now inherited by children.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetActiveRecursively (bool state) ;

    public extern bool isStatic
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    internal extern  bool isStaticBatchable
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  string tag
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
    extern public bool CompareTag (string tag) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  GameObject FindGameObjectWithTag (string tag) ;

    static public GameObject FindWithTag(string tag)
        {
            return FindGameObjectWithTag(tag);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  GameObject[] FindGameObjectsWithTag (string tag) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SendMessageUpwards (string methodName, [uei.DefaultValue("null")]  object value , [uei.DefaultValue("SendMessageOptions.RequireReceiver")]  SendMessageOptions options ) ;

    [uei.ExcludeFromDocs]
    public void SendMessageUpwards (string methodName, object value ) {
        SendMessageOptions options = SendMessageOptions.RequireReceiver;
        SendMessageUpwards ( methodName, value, options );
    }

    [uei.ExcludeFromDocs]
    public void SendMessageUpwards (string methodName) {
        SendMessageOptions options = SendMessageOptions.RequireReceiver;
        object value = null;
        SendMessageUpwards ( methodName, value, options );
    }

    public void SendMessageUpwards(string methodName, SendMessageOptions options)
        {
            SendMessageUpwards(methodName, null, options);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SendMessage (string methodName, [uei.DefaultValue("null")]  object value , [uei.DefaultValue("SendMessageOptions.RequireReceiver")]  SendMessageOptions options ) ;

    [uei.ExcludeFromDocs]
    public void SendMessage (string methodName, object value ) {
        SendMessageOptions options = SendMessageOptions.RequireReceiver;
        SendMessage ( methodName, value, options );
    }

    [uei.ExcludeFromDocs]
    public void SendMessage (string methodName) {
        SendMessageOptions options = SendMessageOptions.RequireReceiver;
        object value = null;
        SendMessage ( methodName, value, options );
    }

    public void SendMessage(string methodName, SendMessageOptions options)
        {
            SendMessage(methodName, null, options);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void BroadcastMessage (string methodName, [uei.DefaultValue("null")]  object parameter , [uei.DefaultValue("SendMessageOptions.RequireReceiver")]  SendMessageOptions options ) ;

    [uei.ExcludeFromDocs]
    public void BroadcastMessage (string methodName, object parameter ) {
        SendMessageOptions options = SendMessageOptions.RequireReceiver;
        BroadcastMessage ( methodName, parameter, options );
    }

    [uei.ExcludeFromDocs]
    public void BroadcastMessage (string methodName) {
        SendMessageOptions options = SendMessageOptions.RequireReceiver;
        object parameter = null;
        BroadcastMessage ( methodName, parameter, options );
    }

    public void BroadcastMessage(string methodName, SendMessageOptions options)
        {
            BroadcastMessage(methodName, null, options);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private Component Internal_AddComponentWithType (Type componentType) ;

    [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
    public Component AddComponent(Type componentType)
        {
            return Internal_AddComponentWithType(componentType);
        }
    
    public T AddComponent<T>() where T : Component
        {
            return AddComponent(typeof(T)) as T;
        }
    
            public GameObject(string name)
        {
            Internal_CreateGameObject(this, name);
        }
    
            public GameObject()
        {
            Internal_CreateGameObject(this, null);
        }
    
            public GameObject(string name, params Type[] components)
        {
            Internal_CreateGameObject(this, name);
            foreach (Type t in components)
                AddComponent(t);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_CreateGameObject ([Writable] GameObject mono, string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  GameObject Find (string name) ;

    internal Bounds CalculateBounds () {
        Bounds result;
        INTERNAL_CALL_CalculateBounds ( this, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CalculateBounds (GameObject self, out Bounds value);
    public  Scene scene
    {
        get { Scene tmp; INTERNAL_get_scene(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_scene (out Scene value) ;


    
            public GameObject gameObject { get { return this; } }
        }
    
    
}

