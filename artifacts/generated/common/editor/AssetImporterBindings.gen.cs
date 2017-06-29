// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using Object = UnityEngine.Object;

namespace UnityEditor
{


public partial class AssetImporter : Object
{
    public extern  string assetPath
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  ulong assetTimeStamp
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  string userData
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  string assetBundleName
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  string assetBundleVariant
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
    extern public void SetAssetBundleNameAndVariant (string assetBundleName, string assetBundleVariant) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  AssetImporter GetAtPath (string path) ;

    public void SaveAndReimport()
        {
            AssetDatabase.ImportAsset(assetPath);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int LocalFileIDToClassID (long fileId) ;

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct SourceAssetIdentifier
    {
        public SourceAssetIdentifier(Object asset)
            {
                if (asset == null)
                {
                    throw new ArgumentNullException("asset");
                }

                this.type = asset.GetType();
                this.name = asset.name;
            }
        
        
        public SourceAssetIdentifier(Type type, string name)
            {
                if (type == null)
                {
                    throw new ArgumentNullException("type");
                }

                if (name == null)
                {
                    throw new ArgumentNullException("name");
                }

                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("The name is empty", "name");
                }

                this.type = type;
                this.name = name;
            }
        
        
        
                    public Type   type;
                    public string name;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void AddRemap (SourceAssetIdentifier identifier, Object externalObject) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool RemoveRemap (SourceAssetIdentifier identifier) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void GetExternalObjects (out SourceAssetIdentifier[] identifiers, out Object[] externalObjects) ;

    public Dictionary<SourceAssetIdentifier, Object> GetExternalObjectMap()
        {
            SourceAssetIdentifier[] identifiers = null;
            Object[] externalObjects = null;
            GetExternalObjects(out identifiers, out externalObjects);

            Dictionary<SourceAssetIdentifier, Object> map = new Dictionary<SourceAssetIdentifier, Object>();

            for (int i = 0; i < identifiers.Length; ++i)
            {
                map.Add(identifiers[i], externalObjects[i]);
            }

            return map;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void RegisterImporter (Type importer, int importerVersion, int queuePos, string fileExt) ;

}

}
