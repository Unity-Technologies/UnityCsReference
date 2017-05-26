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

namespace UnityEngine
{
[RequiredByNativeCode]
public partial class Component : Object
{
    public extern  Transform transform
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  GameObject gameObject
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
    public Component GetComponent(Type type)
        {
            return gameObject.GetComponent(type);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void GetComponentFastPath (System.Type type, IntPtr oneFurtherThanResultValue) ;

    [System.Security.SecuritySafeCritical]
    public unsafe T GetComponent<T>()
        {
            var h = new CastHelper<T>();
            GetComponentFastPath(typeof(T), new System.IntPtr(&h.onePointerFurtherThanT));
            return h.t;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Component GetComponent (string type) ;

    [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
    public Component GetComponentInChildren(Type t, bool includeInactive)
        {
            return gameObject.GetComponentInChildren(t, includeInactive);
        }
    
    
    [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
    public Component GetComponentInChildren(Type t)
        {
            return GetComponentInChildren(t, false);
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

    
    [uei.ExcludeFromDocs]
public Component[] GetComponentsInChildren (Type t) {
    bool includeInactive = false;
    return GetComponentsInChildren ( t, includeInactive );
}

public Component[] GetComponentsInChildren(Type t, [uei.DefaultValue("false")]  bool includeInactive )
        {
            return gameObject.GetComponentsInChildren(t, includeInactive);
        }

    
    public T[] GetComponentsInChildren<T>(bool includeInactive)
        {
            return gameObject.GetComponentsInChildren<T>(includeInactive);
        }
    
    public void GetComponentsInChildren<T>(bool includeInactive, List<T> result)
        {
            gameObject.GetComponentsInChildren<T>(includeInactive, result);
        }
    
    public T[] GetComponentsInChildren<T>()
        {
            return GetComponentsInChildren<T>(false);
        }
    
    public void GetComponentsInChildren<T>(List<T> results)
        {
            GetComponentsInChildren<T>(false, results);
        }
    
    
    [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
    public Component GetComponentInParent(Type t)
        {
            return gameObject.GetComponentInParent(t);
        }
    
    public T GetComponentInParent<T>()
        {
            return (T)(object)GetComponentInParent(typeof(T));
        }
    
    [uei.ExcludeFromDocs]
public Component[] GetComponentsInParent (Type t) {
    bool includeInactive = false;
    return GetComponentsInParent ( t, includeInactive );
}

public Component[] GetComponentsInParent(Type t, [uei.DefaultValue("false")]  bool includeInactive )
        {
            return gameObject.GetComponentsInParent(t, includeInactive);
        }

    
    public T[] GetComponentsInParent<T>(bool includeInactive)
        {
            return gameObject.GetComponentsInParent<T>(includeInactive);
        }
    
    public void GetComponentsInParent<T>(bool includeInactive, List<T> results)
        {
            gameObject.GetComponentsInParent(includeInactive, results);
        }
    
    public T[] GetComponentsInParent<T>()
        {
            return GetComponentsInParent<T>(false);
        }
    
    public Component[] GetComponents(Type type)
        {
            return gameObject.GetComponents(type);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void GetComponentsForListInternal (Type searchType, object resultList) ;

    
    public void GetComponents(Type type, List<Component> results)
        {
            GetComponentsForListInternal(type, results);
        }
    
    public void GetComponents<T>(List<T> results)
        {
            GetComponentsForListInternal(typeof(T), results);
        }
    
            public string tag
        {
            get { return gameObject.tag; }
            set { gameObject.tag = value; }
        }
    
    public T[] GetComponents<T>()
        {
            return gameObject.GetComponents<T>();
        }
    
    public bool CompareTag(string tag)
        {
            return gameObject.CompareTag(tag);
        }
    
    
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
    
    
}

}
