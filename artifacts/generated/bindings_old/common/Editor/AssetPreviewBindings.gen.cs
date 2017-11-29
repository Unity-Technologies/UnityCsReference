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

namespace UnityEditor
{


public sealed partial class AssetPreview
{
    const int kSharedClientID = 0;
    
    
    
    public static Texture2D GetAssetPreview(Object asset)
        {
            if (asset != null)
                return GetAssetPreview(asset.GetInstanceID());
            else
                return null;
        }
    
    
    internal static Texture2D GetAssetPreview(int instanceID)
        {
            return GetAssetPreview(instanceID, kSharedClientID);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Texture2D GetAssetPreview (int instanceID, int clientID) ;

    public static bool IsLoadingAssetPreview(int instanceID)
        {
            return IsLoadingAssetPreview(instanceID, kSharedClientID);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsLoadingAssetPreview (int instanceID, int clientID) ;

    public static bool IsLoadingAssetPreviews()
        {
            return IsLoadingAssetPreviews(kSharedClientID);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsLoadingAssetPreviews (int clientID) ;

    internal static bool HasAnyNewPreviewTexturesAvailable()
        {
            return HasAnyNewPreviewTexturesAvailable(kSharedClientID);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool HasAnyNewPreviewTexturesAvailable (int clientID) ;

    public static void SetPreviewTextureCacheSize(int size)
        {
            SetPreviewTextureCacheSize(size, kSharedClientID);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetPreviewTextureCacheSize (int size, int clientID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void ClearTemporaryAssetPreviews () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void DeletePreviewTextureManagerByID (int clientID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Texture2D GetMiniThumbnail (Object obj) ;

    public static Texture2D GetMiniTypeThumbnail(Type type)
        {
            Texture2D tex;
            if (typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(type))
                tex = EditorGUIUtility.LoadIcon(type.FullName.Replace('.', '/') + " Icon");
            else
                tex = INTERNAL_GetMiniTypeThumbnailFromType(type);
            return tex;
        }
    
    
    internal static Texture2D GetMiniTypeThumbnail(Object obj)
        {
            return INTERNAL_GetMiniTypeThumbnailFromObject(obj);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Texture2D GetMiniTypeThumbnailFromClassID (int classID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Texture2D INTERNAL_GetMiniTypeThumbnailFromObject (Object monoObj) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Texture2D INTERNAL_GetMiniTypeThumbnailFromType (Type managedType) ;

}

}
