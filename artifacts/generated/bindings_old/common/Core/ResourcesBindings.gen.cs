// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.InteropServices;
using UnityEngineInternal;

namespace UnityEngine
{


[StructLayout(LayoutKind.Sequential)]
[RequiredByNativeCode]
public partial class ResourceRequest : AsyncOperation
{
    internal string m_Path;
    internal Type m_Type;
    public Object asset { get { return Resources.Load(m_Path, m_Type); } }
}

public sealed partial class Resources
{
    internal static T[] ConvertObjects<T>(Object[] rawObjects) where T : Object
        {
            if (rawObjects == null) return null;
            T[] typedObjects = new T[rawObjects.Length];
            for (int i = 0; i < typedObjects.Length; i++)
                typedObjects[i] = (T)rawObjects[i];
            return typedObjects;
        }
    
    
    [TypeInferenceRule(TypeInferenceRules.ArrayOfTypeReferencedByFirstArgument)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object[] FindObjectsOfTypeAll (Type type) ;

    public static T[] FindObjectsOfTypeAll<T>() where T : Object
        {
            return ConvertObjects<T>(FindObjectsOfTypeAll(typeof(T)));
        }
    
    
    
    public static Object Load(string path)
        {
            return Load(path, typeof(Object));
        }
    
    
    public static T Load<T>(string path) where T : Object
        {
            return (T)Load(path, typeof(T));
        }
    
    
    [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object Load (string path, Type systemTypeInstance) ;

    public static ResourceRequest LoadAsync(string path)
        {
            return LoadAsync(path, typeof(Object));
        }
    
    
    public static ResourceRequest LoadAsync<T>(string path) where T : Object
        {
            return LoadAsync(path, typeof(T));
        }
    
    
    public static ResourceRequest LoadAsync(string path, Type type)
        {
            ResourceRequest req = LoadAsyncInternal(path, type);
            req.m_Path = path;
            req.m_Type = type;
            return req;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  ResourceRequest LoadAsyncInternal (string path, Type type) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object[] LoadAll (string path, Type systemTypeInstance) ;

    public static Object[] LoadAll(string path)
        {
            return LoadAll(path, typeof(Object));
        }
    
    
    public static T[] LoadAll<T>(string path) where T : Object
        {
            return ConvertObjects<T>(LoadAll(path, typeof(T)));
        }
    
    
    [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object GetBuiltinResource (Type type, string path) ;

    public static T GetBuiltinResource<T>(string path) where T : Object
        {
            return (T)GetBuiltinResource(typeof(T), path);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void UnloadAsset (Object assetToUnload) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  AsyncOperation UnloadUnusedAssets () ;

}

}
