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
using UnityEngineInternal;
using UnityEditorInternal;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace UnityEditor
{


internal sealed partial class AssetStoreUtils
{
    private const string kAssetStoreUrl = "https://shawarma.unity3d.com";
    
    
    public delegate void DownloadDoneCallback(string package_id, string message, int bytes, int total);
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Download (string id, string url, string[] destination, string key, string jsonData, bool resumeOK, [uei.DefaultValue("null")]  DownloadDoneCallback doneCallback ) ;

    [uei.ExcludeFromDocs]
    public static void Download (string id, string url, string[] destination, string key, string jsonData, bool resumeOK) {
        DownloadDoneCallback doneCallback = null;
        Download ( id, url, destination, key, jsonData, resumeOK, doneCallback );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string CheckDownload (string id, string url, string[] destination, string key) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RegisterDownloadDelegate (ScriptableObject d) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void UnRegisterDownloadDelegate (ScriptableObject d) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetLoaderPath () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void UpdatePreloading () ;

    public static string GetOfflinePath()
        {
            return System.Uri.EscapeUriString(EditorApplication.applicationContentsPath + "/Resources/offline.html");
        }
    
    
    public static string GetAssetStoreUrl()
        {
            return kAssetStoreUrl;
        }
    
    
    public static string GetAssetStoreSearchUrl()
        {
            return GetAssetStoreUrl().Replace("https", "http"); 
        }
    
    
}

}
