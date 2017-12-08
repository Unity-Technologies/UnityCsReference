// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{


public enum RemoveAssetOptions
{
    
    MoveAssetToTrash = 0,
    
    DeleteAssets = 2
}

[Flags]
public enum ImportAssetOptions
{
    Default                     = 0,
    ForceUpdate                 = 1 <<  0,
    ForceSynchronousImport      = 1 <<  3,
    ImportRecursive             = 1 <<  8,
    DontDownloadFromCacheServer = 1 << 13,
    ForceUncompressedImport     = 1 << 14,
}

public enum StatusQueryOptions
{
    ForceUpdate         = 0,
    UseCachedIfPossible = 1,
    UseCachedAsync = 2,
}

public sealed partial class AssetDatabase
{
    public static bool Contains(Object obj)
        {
            return Contains(obj.GetInstanceID());
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool Contains (int instanceID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string CreateFolder (string parentFolder, string newFolderName) ;

    public static bool IsMainAsset(Object obj)
        {
            return IsMainAsset(obj.GetInstanceID());
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetCurrentCacheServerIp () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsMainAsset (int instanceID) ;

    public static bool IsSubAsset(Object obj)
        {
            return IsSubAsset(obj.GetInstanceID());
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsSubAsset (int instanceID) ;

    public static bool IsForeignAsset(Object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj is null");
            return IsForeignAsset(obj.GetInstanceID());
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsForeignAsset (int instanceID) ;

    public static bool IsNativeAsset(Object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj is null");
            return IsNativeAsset(obj.GetInstanceID());
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsNativeAsset (int instanceID) ;

    public static bool IsPackagedAsset(Object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj is null");
            return IsPackagedAsset(obj.GetInstanceID());
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsPackagedAsset (int instanceID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GenerateUniqueAssetPath (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void StartAssetEditing () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void StopAssetEditing () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string ValidateMoveAsset (string oldPath, string newPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string MoveAsset (string oldPath, string newPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string ExtractAsset (Object asset, string newPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string RenameAsset (string pathName, string newName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool MoveAssetToTrash (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool DeleteAsset (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ImportAsset (string path, [uei.DefaultValue("ImportAssetOptions.Default")]  ImportAssetOptions options ) ;

    [uei.ExcludeFromDocs]
    public static void ImportAsset (string path) {
        ImportAssetOptions options = ImportAssetOptions.Default;
        ImportAsset ( path, options );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool CopyAsset (string path, string newPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool WriteImportSettingsIfDirty (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetSubFolders (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsValidFolder (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsPackagedAssetPath (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CreateAsset (Object asset, string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void CreateAssetFromObjects (Object[] assets, string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void AddObjectToAsset (Object objectToAdd, string path) ;

    static public void AddObjectToAsset(Object objectToAdd, Object assetObject)
        {
            AddObjectToAsset_OBJ_Internal(objectToAdd, assetObject);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void AddInstanceIDToAssetWithRandomFileId (int instanceIDToAdd, Object assetObject, bool hide) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void AddObjectToAsset_OBJ_Internal (Object newAsset, Object sameAssetFile) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetMainObject (Object mainObject, string assetPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetAssetPath (Object assetObject) ;

    public static string GetAssetPath(int instanceID)
        {
            return GetAssetPathFromInstanceID(instanceID);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string GetAssetPathFromInstanceID (int instanceID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetMainAssetInstanceID (string assetPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetAssetOrScenePath (Object assetObject) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetTextMetaFilePathFromAssetPath (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetAssetPathFromTextMetaFilePath (string path) ;

    [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object LoadAssetAtPath (string assetPath, Type type) ;

    public static T LoadAssetAtPath<T>(string assetPath) where T : Object
        {
            return (T)LoadAssetAtPath(assetPath, typeof(T));
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object LoadMainAssetAtPath (string assetPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  System.Type GetMainAssetTypeAtPath (string assetPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsMainAssetAtPathLoaded (string assetPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object[] LoadAllAssetRepresentationsAtPath (string assetPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object[] LoadAllAssetsAtPath (string assetPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetAllAssetPaths () ;

    [System.Obsolete ("Please use AssetDatabase.Refresh instead", true)]
public static void RefreshDelayed(ImportAssetOptions options) {}
    
    
    [System.Obsolete ("Please use AssetDatabase.Refresh instead", true)]
public static void RefreshDelayed() {}
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Refresh ( [uei.DefaultValue("ImportAssetOptions.Default")] ImportAssetOptions options ) ;

    [uei.ExcludeFromDocs]
    public static void Refresh () {
        ImportAssetOptions options = ImportAssetOptions.Default;
        Refresh ( options );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool OpenAsset (int instanceID, [uei.DefaultValue("-1")]  int lineNumber ) ;

    [uei.ExcludeFromDocs]
    public static bool OpenAsset (int instanceID) {
        int lineNumber = -1;
        return OpenAsset ( instanceID, lineNumber );
    }

    [uei.ExcludeFromDocs]
public static bool OpenAsset (Object target) {
    int lineNumber = -1;
    return OpenAsset ( target, lineNumber );
}

public static bool OpenAsset(Object target, [uei.DefaultValue("-1")]  int lineNumber )
        {
            if (target)
                return OpenAsset(target.GetInstanceID(), lineNumber);
            else
                return false;
        }

    
    
    static public bool OpenAsset(Object[] objects)
        {
            bool allOpened = true;
            foreach (Object obj in objects)
                if (!OpenAsset(obj))
                    allOpened = false;

            return allOpened;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string AssetPathToGUID (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GUIDToAssetPath (string guid) ;

    public static Hash128 GetAssetDependencyHash (string path) {
        Hash128 result;
        INTERNAL_CALL_GetAssetDependencyHash ( path, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetAssetDependencyHash (string path, out Hash128 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetInstanceIDFromGUID (string guid) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SaveAssets () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Texture GetCachedIcon (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetLabels (Object obj, string[] labels) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void INTERNAL_GetAllLabels (out string[] labels, out float[] scores) ;

    internal static Dictionary<string, float> GetAllLabels()
        {
            string[] labels;
            float[] scores;
            INTERNAL_GetAllLabels(out labels, out scores);

            Dictionary<string, float> res = new Dictionary<string, float>(labels.Length);
            for (int i = 0; i < labels.Length; ++i)
            {
                res[labels[i]] = scores[i];
            }
            return res;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetLabels (Object obj) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ClearLabels (Object obj) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetAllAssetBundleNames () ;

    [System.Obsolete ("Method GetAssetBundleNames has been deprecated. Use GetAllAssetBundleNames instead.")]
public string[] GetAssetBundleNames()
        {
            return GetAllAssetBundleNames();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string[] GetAllAssetBundleNamesWithoutVariant () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string[] GetAllAssetBundleVariants () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetUnusedAssetBundleNames () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool RemoveAssetBundleName (string assetBundleName, bool forceRemove) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RemoveUnusedAssetBundleNames () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetAssetPathsFromAssetBundle (string assetBundleName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetAssetPathsFromAssetBundleAndAssetName (string assetBundleName, string assetName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetImplicitAssetBundleName (string assetPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetImplicitAssetBundleVariantName (string assetPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetAssetBundleDependencies (string assetBundleName, bool recursive) ;

    public static string[] GetDependencies(string pathName)
        {
            return GetDependencies(pathName, true);
        }
    
    
    public static string[] GetDependencies(string pathName, bool recursive)
        {
            string[] input = new string[1];
            input[0] = pathName;
            return GetDependencies(input, recursive);
        }
    
    
    public static string[] GetDependencies(string[] pathNames)
        {
            return GetDependencies(pathNames, true);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetDependencies (string[] pathNames, bool recursive) ;

    public static void ExportPackage(string assetPathName, string fileName)
        {
            string[] input = new string[1];
            input[0] = assetPathName;
            ExportPackage(input, fileName, ExportPackageOptions.Default);
        }
    
    
    public static void ExportPackage(string assetPathName, string fileName, ExportPackageOptions flags)
        {
            string[] input = new string[1];
            input[0] = assetPathName;
            ExportPackage(input, fileName, flags);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ExportPackage (string[] assetPathNames, string fileName, [uei.DefaultValue("ExportPackageOptions.Default")]  ExportPackageOptions flags ) ;

    [uei.ExcludeFromDocs]
    public static void ExportPackage (string[] assetPathNames, string fileName) {
        ExportPackageOptions flags = ExportPackageOptions.Default;
        ExportPackage ( assetPathNames, fileName, flags );
    }

    public static void ImportPackage(string packagePath, bool interactive)
        {
            if (string.IsNullOrEmpty(packagePath))
                throw new ArgumentException("Path can not be empty or null", "packagePath");

            string packageIconPath;
            bool canPerformReInstall;
            ImportPackageItem[] items = PackageUtility.ExtractAndPrepareAssetList(packagePath, out packageIconPath, out canPerformReInstall);

            if (items == null)
                return;

            if (interactive)
                PackageImport.ShowImportPackage(packagePath, items, packageIconPath, canPerformReInstall);
            else
            {
                string packageName = System.IO.Path.GetFileNameWithoutExtension(packagePath);
                PackageUtility.ImportPackageAssets(packageName, items, false);
            }
        }
    
    
    internal static void ImportPackageImmediately(string packagePath)
        {
            if (string.IsNullOrEmpty(packagePath))
                throw new ArgumentException("Path can not be empty or null", "packagePath");

            string packageIconPath;
            bool canPerformReInstall;
            ImportPackageItem[] items = PackageUtility.ExtractAndPrepareAssetList(packagePath, out packageIconPath, out canPerformReInstall);

            if (items == null || items.Length == 0)
                return;

            string packageName = System.IO.Path.GetFileNameWithoutExtension(packagePath);
            PackageUtility.ImportPackageAssetsImmediately(packageName, items, false);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetUniquePathNameAtSelectedPath (string fileName) ;

    [System.Obsolete ("AssetDatabase.IsOpenForEdit without StatusQueryOptions has been deprecated. Use the version with StatusQueryOptions instead. This will always request the cached status (StatusQueryOptions.UseCachedIfPossible)")]
public static bool IsOpenForEdit(UnityEngine.Object assetObject)
        {
            string assetPath = GetAssetOrScenePath(assetObject);
            return IsOpenForEdit(assetPath, StatusQueryOptions.UseCachedIfPossible);
        }
    
    
    public static bool IsOpenForEdit(UnityEngine.Object assetObject, StatusQueryOptions StatusQueryOptions)
        {
            string assetPath = GetAssetOrScenePath(assetObject);
            return IsOpenForEdit(assetPath, StatusQueryOptions);
        }
    
    
    [System.Obsolete ("AssetDatabase.IsOpenForEdit without StatusQueryOptions has been deprecated. Use the version with StatusQueryOptions instead. This will always request the cached status (StatusQueryOptions.UseCachedIfPossible)")]
public static bool IsOpenForEdit(string assetOrMetaFilePath)
        {
            return IsOpenForEdit(assetOrMetaFilePath, StatusQueryOptions.UseCachedIfPossible);
        }
    
    
    public static bool IsOpenForEdit(string assetOrMetaFilePath, StatusQueryOptions StatusQueryOptions)
        {
            string message;
            return IsOpenForEdit(assetOrMetaFilePath, out message, StatusQueryOptions);
        }
    
    
    [System.Obsolete ("AssetDatabase.IsOpenForEdit without StatusQueryOptions has been deprecated. Use the version with StatusQueryOptions instead. This will always request the cached status (StatusQueryOptions.UseCachedIfPossible)")]
public static bool IsOpenForEdit(UnityEngine.Object assetObject, out string message)
        {
            return IsOpenForEdit(assetObject, out message, StatusQueryOptions.UseCachedIfPossible);
        }
    
    
    public static bool IsOpenForEdit(UnityEngine.Object assetObject, out string message, StatusQueryOptions statusOptions)
        {
            string assetPath = GetAssetOrScenePath(assetObject);
            return IsOpenForEdit(assetPath, out message, statusOptions);
        }
    
    
    [System.Obsolete ("AssetDatabase.IsOpenForEdit without StatusQueryOptions has been deprecated. Use the version with StatusQueryOptions instead. This will always request the cached status (StatusQueryOptions.UseCachedIfPossible)")]
public static bool IsOpenForEdit(string assetOrMetaFilePath, out string message)
        {
            return IsOpenForEdit(assetOrMetaFilePath, out message, StatusQueryOptions.UseCachedIfPossible);
        }
    
    
    public static bool IsOpenForEdit(string assetOrMetaFilePath, out string message, StatusQueryOptions statusOptions)
        {
            return AssetModificationProcessorInternal.IsOpenForEdit(assetOrMetaFilePath, out message, statusOptions);
        }
    
    
    [System.Obsolete ("AssetDatabase.IsMetaFileOpenForEdit without StatusQueryOptions has been deprecated. Use the version with StatusQueryOptions instead. This will always request the cached status (StatusQueryOptions.UseCachedIfPossible)")]
public static bool IsMetaFileOpenForEdit(UnityEngine.Object assetObject)
        {
            return IsMetaFileOpenForEdit(assetObject, StatusQueryOptions.UseCachedIfPossible);
        }
    
    
    public static bool IsMetaFileOpenForEdit(UnityEngine.Object assetObject, StatusQueryOptions statusOptions)
        {
            string message;
            return IsMetaFileOpenForEdit(assetObject, out message, statusOptions);
        }
    
    
    [System.Obsolete ("AssetDatabase.IsMetaFileOpenForEdit without StatusQueryOptions has been deprecated. Use the version with StatusQueryOptions instead. This will always request the cached status (StatusQueryOptions.UseCachedIfPossible)")]
public static bool IsMetaFileOpenForEdit(UnityEngine.Object assetObject, out string message)
        {
            return IsMetaFileOpenForEdit(assetObject, out message, StatusQueryOptions.UseCachedIfPossible);
        }
    
    
    public static bool IsMetaFileOpenForEdit(UnityEngine.Object assetObject, out string message, StatusQueryOptions statusOptions)
        {
            string assetPath = GetAssetOrScenePath(assetObject);
            string metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetPath);
            return IsOpenForEdit(metaPath, out message, statusOptions);
        }
    
    
    [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object GetBuiltinExtraResource (Type type, string path) ;

    public static T GetBuiltinExtraResource<T>(string path) where T : Object
        {
            return (T)GetBuiltinExtraResource(typeof(T), path);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string[] CollectAllChildren (string guid, string[] collection) ;

    internal extern static string assetFolderGUID
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool RegisterPackageFolder (string name, string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool UnregisterPackageFolder (string name, string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetPackagesMountPoint () ;

}

}
