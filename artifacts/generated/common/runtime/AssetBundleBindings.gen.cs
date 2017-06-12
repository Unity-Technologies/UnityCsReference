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



[StructLayout(LayoutKind.Sequential)]
[RequiredByNativeCode]
public sealed partial class AssetBundleCreateRequest : AsyncOperation
{
    public extern  AssetBundle assetBundle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void DisableCompatibilityChecks () ;

}

[StructLayout(LayoutKind.Sequential)]
[RequiredByNativeCode]
public sealed partial class AssetBundleRequest : AsyncOperation
{
    public extern  Object asset
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  Object[] allAssets
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

public sealed partial class AssetBundle : Object
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void UnloadAllAssetBundles (bool unloadAllObjects) ;

    public static IEnumerable<AssetBundle> GetAllLoadedAssetBundles()
        {
            return (IEnumerable<AssetBundle>)GetAllLoadedAssetBundles_Internal();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  AssetBundle[] GetAllLoadedAssetBundles_Internal () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  AssetBundleCreateRequest LoadFromFileAsync (string path, [uei.DefaultValue("0")]  uint crc , [uei.DefaultValue("0")]  ulong offset ) ;

    [uei.ExcludeFromDocs]
    public static AssetBundleCreateRequest LoadFromFileAsync (string path, uint crc ) {
        ulong offset = 0;
        return LoadFromFileAsync ( path, crc, offset );
    }

    [uei.ExcludeFromDocs]
    public static AssetBundleCreateRequest LoadFromFileAsync (string path) {
        ulong offset = 0;
        uint crc = 0;
        return LoadFromFileAsync ( path, crc, offset );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  AssetBundle LoadFromFile (string path, [uei.DefaultValue("0")]  uint crc , [uei.DefaultValue("0")]  ulong offset ) ;

    [uei.ExcludeFromDocs]
    public static AssetBundle LoadFromFile (string path, uint crc ) {
        ulong offset = 0;
        return LoadFromFile ( path, crc, offset );
    }

    [uei.ExcludeFromDocs]
    public static AssetBundle LoadFromFile (string path) {
        ulong offset = 0;
        uint crc = 0;
        return LoadFromFile ( path, crc, offset );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  AssetBundleCreateRequest LoadFromMemoryAsync (byte[] binary, [uei.DefaultValue("0")]  uint crc ) ;

    [uei.ExcludeFromDocs]
    public static AssetBundleCreateRequest LoadFromMemoryAsync (byte[] binary) {
        uint crc = 0;
        return LoadFromMemoryAsync ( binary, crc );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  AssetBundle LoadFromMemory (byte[] binary, [uei.DefaultValue("0")]  uint crc ) ;

    [uei.ExcludeFromDocs]
    public static AssetBundle LoadFromMemory (byte[] binary) {
        uint crc = 0;
        return LoadFromMemory ( binary, crc );
    }

    [uei.ExcludeFromDocs]
public static AssetBundleCreateRequest LoadFromStreamAsync (System.IO.Stream stream, uint crc ) {
    uint managedReadBufferSize = 0;
    return LoadFromStreamAsync ( stream, crc, managedReadBufferSize );
}

[uei.ExcludeFromDocs]
public static AssetBundleCreateRequest LoadFromStreamAsync (System.IO.Stream stream) {
    uint managedReadBufferSize = 0;
    uint crc = 0;
    return LoadFromStreamAsync ( stream, crc, managedReadBufferSize );
}

public static AssetBundleCreateRequest LoadFromStreamAsync(System.IO.Stream stream, [uei.DefaultValue("0")]  uint crc , [uei.DefaultValue("0")]  uint managedReadBufferSize )
        {
            ManagedStreamHelpers.ValidateLoadFromStream(stream);
            return LoadFromStreamAsyncInternal(stream, crc, managedReadBufferSize);
        }

    
    
    [uei.ExcludeFromDocs]
public static AssetBundle LoadFromStream (System.IO.Stream stream, uint crc ) {
    uint managedReadBufferSize = 0;
    return LoadFromStream ( stream, crc, managedReadBufferSize );
}

[uei.ExcludeFromDocs]
public static AssetBundle LoadFromStream (System.IO.Stream stream) {
    uint managedReadBufferSize = 0;
    uint crc = 0;
    return LoadFromStream ( stream, crc, managedReadBufferSize );
}

public static AssetBundle LoadFromStream(System.IO.Stream stream, [uei.DefaultValue("0")]  uint crc , [uei.DefaultValue("0")]  uint managedReadBufferSize )
        {
            ManagedStreamHelpers.ValidateLoadFromStream(stream);
            return LoadFromStreamInternal(stream, crc, managedReadBufferSize);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  AssetBundleCreateRequest LoadFromStreamAsyncInternal (System.IO.Stream stream, uint crc, [uei.DefaultValue("0")]  uint managedReadBufferSize ) ;

    [uei.ExcludeFromDocs]
    internal static AssetBundleCreateRequest LoadFromStreamAsyncInternal (System.IO.Stream stream, uint crc) {
        uint managedReadBufferSize = 0;
        return LoadFromStreamAsyncInternal ( stream, crc, managedReadBufferSize );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  AssetBundle LoadFromStreamInternal (System.IO.Stream stream, uint crc, [uei.DefaultValue("0")]  uint managedReadBufferSize ) ;

    [uei.ExcludeFromDocs]
    internal static AssetBundle LoadFromStreamInternal (System.IO.Stream stream, uint crc) {
        uint managedReadBufferSize = 0;
        return LoadFromStreamInternal ( stream, crc, managedReadBufferSize );
    }

    public extern  Object mainAsset
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool isStreamedSceneAssetBundle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool Contains (string name) ;

    [System.Obsolete ("Method Load has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAsset instead and check the documentation for details.", true)]
public Object Load(string name) { return null; }
    
    
    [System.Obsolete ("Method Load has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAsset instead and check the documentation for details.", true)]
public T Load<T>(string name) where T : Object { return null; }
    
    
    [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
    [System.Obsolete ("Method Load has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAsset instead and check the documentation for details.", true)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Object Load (string name, Type type) ;

    [System.Obsolete ("Method LoadAsync has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAssetAsync instead and check the documentation for details.", true)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public AssetBundleRequest LoadAsync (string name, Type type) ;

    [System.Obsolete ("Method LoadAll has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAllAssets instead and check the documentation for details.", true)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Object[] LoadAll (Type type) ;

    [System.Obsolete ("Method LoadAll has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAllAssets instead and check the documentation for details.", true)]
public UnityEngine.Object[] LoadAll() { return null; }
    
    
    [System.Obsolete ("Method LoadAll has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAllAssets instead and check the documentation for details.", true)]
public T[] LoadAll<T>() where T : Object { return null; }
    
    
    public Object LoadAsset(string name)
        {
            return LoadAsset(name, typeof(Object));
        }
    
    
    public T LoadAsset<T>(string name) where T : Object
        {
            return (T)LoadAsset(name, typeof(T));
        }
    
    
    [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
    public Object LoadAsset(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAsset_Internal(name, type);
        }
    
    
    [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private Object LoadAsset_Internal (string name, Type type) ;

    public AssetBundleRequest LoadAssetAsync(string name)
        {
            return LoadAssetAsync(name, typeof(UnityEngine.Object));
        }
    
    
    public AssetBundleRequest LoadAssetAsync<T>(string name)
        {
            return LoadAssetAsync(name, typeof(T));
        }
    
    
    public AssetBundleRequest LoadAssetAsync(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetAsync_Internal(name, type);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private AssetBundleRequest LoadAssetAsync_Internal (string name, Type type) ;

    public Object[] LoadAssetWithSubAssets(string name)
        {
            return LoadAssetWithSubAssets(name, typeof(Object));
        }
    
    
    public T[] LoadAssetWithSubAssets<T>(string name) where T : Object
        {
            return Resources.ConvertObjects<T>(LoadAssetWithSubAssets(name, typeof(T)));
        }
    
    
    public Object[] LoadAssetWithSubAssets(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssets_Internal(name, type);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal Object[] LoadAssetWithSubAssets_Internal (string name, Type type) ;

    public AssetBundleRequest LoadAssetWithSubAssetsAsync(string name)
        {
            return LoadAssetWithSubAssetsAsync(name, typeof(UnityEngine.Object));
        }
    
    
    public AssetBundleRequest LoadAssetWithSubAssetsAsync<T>(string name)
        {
            return LoadAssetWithSubAssetsAsync(name, typeof(T));
        }
    
    
    public AssetBundleRequest LoadAssetWithSubAssetsAsync(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssetsAsync_Internal(name, type);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private AssetBundleRequest LoadAssetWithSubAssetsAsync_Internal (string name, Type type) ;

    public UnityEngine.Object[] LoadAllAssets()
        {
            return LoadAllAssets(typeof(UnityEngine.Object));
        }
    
    
    public T[] LoadAllAssets<T>() where T : Object
        {
            return Resources.ConvertObjects<T>(LoadAllAssets(typeof(T)));
        }
    
    
    public UnityEngine.Object[] LoadAllAssets(Type type)
        {
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssets_Internal("", type);
        }
    
    
    public AssetBundleRequest LoadAllAssetsAsync()
        {
            return LoadAllAssetsAsync(typeof(UnityEngine.Object));
        }
    
    
    public AssetBundleRequest LoadAllAssetsAsync<T>()
        {
            return LoadAllAssetsAsync(typeof(T));
        }
    
    
    public AssetBundleRequest LoadAllAssetsAsync(Type type)
        {
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssetsAsync_Internal("", type);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Unload (bool unloadAllLoadedObjects) ;

    [System.Obsolete ("This method is deprecated. Use GetAllAssetNames() instead.")]
public string[] AllAssetNames()
        {
            return GetAllAssetNames();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public string[] GetAllAssetNames () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public string[] GetAllScenePaths () ;

}


}
