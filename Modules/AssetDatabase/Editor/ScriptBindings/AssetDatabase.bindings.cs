// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngineInternal;
using uei = UnityEngine.Internal;
using Object = UnityEngine.Object;
using UnityEngine.Scripting;
using UnityEditor.Experimental;
using UnityEngine.Internal;
using UnityEditor.AssetImporters;

namespace UnityEditor
{
    public enum RemoveAssetOptions
    {
        MoveAssetToTrash = 0,
        DeleteAssets = 2
    }

    // Subset of C++ UpdateAssetOptions in AssetDatabaseStructs.h
    [Flags]
    public enum ImportAssetOptions
    {
        Default                     = 0,       // Default import options.
        ForceUpdate                 = 1 <<  0, // User initiated asset import.
        ForceSynchronousImport      = 1 <<  3, // Import all assets synchronously.
        ImportRecursive             = 1 <<  8, // When a folder is imported, import all its contents as well.
        DontDownloadFromCacheServer = 1 << 13, // Force a full reimport but don't download the assets from the cache server.
        ForceUncompressedImport     = 1 << 14, // Forces asset import as uncompressed for edition facilities.
    }

    public enum StatusQueryOptions
    {
        ForceUpdate         = 0, // Always ask version control for the true status of the file and wait for the response. Recommended for operations that will open a file for edit, or revert, or update a file from version control where you need to know the status of the file accurately.
        UseCachedIfPossible = 1, // Use the cached status of the asset in version control. The version control system will be queried for the first request and then periodically for subsequent requests. Cached status can be queried very quickly, so is recommended for any UI operations where accuracy is not strictly necessary.
        UseCachedAsync = 2, // Use the cached status of the asset in version control. Similar to UseCachedIfPossible, except that it doesn't await a response and will submit a query and return immediately if no cached status is available.
    }

    public enum ForceReserializeAssetsOptions
    {
        ReserializeAssets = 1 << 0,
        ReserializeMetadata = 1 << 1,
        ReserializeAssetsAndMetadata = ReserializeAssets | ReserializeMetadata
    }

    public enum AssetPathToGUIDOptions
    {
        IncludeRecentlyDeletedAssets = 0, // Return a GUID if an asset has been recently deleted.
        OnlyExistingAssets = 1, // Return a GUID only if the asset exists on disk.
    }

    internal enum ImportPackageOptions
    {
        Default = 0,
        NoGUI = 1 << 0,
        ImportDelayed = 1 << 1
    }

    // keep in sync with AssetDatabasePreventExecutionChecks in AssetDatabasePreventExecution.h
    internal enum AssetDatabasePreventExecution
    {
        kNoAssetDatabaseRestriction = 0,
        kImportingAsset = 1 << 0,
        kImportingInWorkerProcess = 1 << 1,
        kPreventCustomDependencyChanges = 1 << 2,
        kGatheringDependenciesFromSourceFile = 1 << 3,
        kPreventForceReserializeAssets = 1 << 4,
        kDomainBackup = 1 << 5,
        kCodeReload = 1 << 6,
    }

    public struct CacheServerConnectionChangedParameters
    {
    }

    [RequiredByNativeCode]
    internal class AssetDatabaseLoadOperationHelper
    {
        // When the load operation completes this is invoked to hold the result so that it doesn't
        // get garbage collected
        [RequiredByNativeCode]
        public static void SetAssetDatabaseLoadObjectResult(AssetDatabaseLoadOperation op, UnityEngine.Object result)
        {
            op.m_Result = result;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public class AssetDatabaseLoadOperation : AsyncOperation
    {
        internal Object m_Result;
        public UnityEngine.Object LoadedObject { get { return m_Result; } }

        public AssetDatabaseLoadOperation() { }

        private AssetDatabaseLoadOperation(IntPtr ptr) : base(ptr)
        { }

        new internal static class BindingsMarshaller
        {
            public static AssetDatabaseLoadOperation ConvertToManaged(IntPtr ptr) => new AssetDatabaseLoadOperation(ptr);
        }
    }

    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabase.h")]
    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseUtility.h")]
    [NativeHeader("Modules/AssetDatabase/Editor/ScriptBindings/AssetDatabase.bindings.h")]
    [NativeHeader("NativeKernel/Core/PreventExecutionInState.h")]
    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabasePreventExecution.h")]
    [NativeHeader("Modules/AssetDatabase/Editor/V2/AssetDatabaseProfiler.h")]
    [NativeHeader("Modules/AssetPackageEditor/AssetPackage.h")]
    [NativeHeader("Editor/Src/VersionControl/VC_bindings.h")]
    [NativeHeader("Editor/Src/Application/ApplicationFunctions.h")]
    [StaticAccessor("AssetDatabaseBindings", StaticAccessorType.DoubleColon)]
    public partial class AssetDatabase
    {
        private const string kPreventExecutionDuringImportHowToFixMsg = "Please make sure this function is not called from ScriptedImporters or PostProcessors, as it is a source of non-determinism.";
        internal const string kPreventExecutionDuringCodeReloadHowToFixMsg = "Please make sure this function is not called from code that runs during code reload (e.g. [OnCodeLoaded])";

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static bool CanGetAssetMetaInfo(string path);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static void RegisterAssetFolder(string path, bool immutable, string guid);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static void UnregisterAssetFolder(string path);

        // used by integration tests
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static void RegisterRedirectedAssetFolder(string mountPoint, string folder, string physicalPath, bool immutable, string guid);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static void UnregisterRedirectedAssetFolder(string mountPoint, string folder);
        [FreeFunction("AssetDatabase::GetHashOfRootFolders")]
        extern internal static Hash128 GetHashOfRootFolders();

        // This will return all registered roots, i.e. Assets/, Packages/** (all registered package roots), Workspaces/, etc.
        [FreeFunction("AssetDatabase::GetAssetRootFolders")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static string[] GetAssetRootFolders();

        // returns true if the folder is known by the asset database
        // rootFolder is true if the path is a registered root folder
        // immutable is true when the root of the path was registered with the immutable flag (e.g. shared package)
        // asset folders marked immutable are not modified by the asset database
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool TryGetAssetFolderInfo(string path, out bool rootFolder, out bool immutable);

        public static bool Contains(Object obj) { return Contains(obj.GetEntityId()); }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool Contains(EntityId entityId);

        [System.Obsolete(@"Please use Contains(EntityId) with the EntityId type instead.", true)]
        public static bool Contains(int instanceID) => Contains((EntityId)instanceID);

        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string CreateFolder(string parentFolder, string newFolderName);

        public static bool IsMainAsset(Object obj) { return IsMainAsset(obj.GetEntityId()); }

        [FreeFunction("AssetDatabase::IsMainAsset")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool IsMainAsset(EntityId entityId);

        [System.Obsolete(@"Please use IsMainAsset(EntityId) with the EntityId type instead.", true)]
        public static bool IsMainAsset(int instanceID) => IsMainAsset((EntityId)instanceID);

        public static bool IsSubAsset(Object obj) { return IsSubAsset(obj.GetEntityId()); }

        [FreeFunction("AssetDatabase::IsSubAsset")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool IsSubAsset(EntityId entityId);

        [System.Obsolete(@"Please use IsSubAsset(EntityId) with the EntityId type instead.", true)]
        public static bool IsSubAsset(int instanceID) => IsSubAsset((EntityId)instanceID);

        public static bool IsForeignAsset(Object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj is null");
            return IsForeignAsset(obj.GetEntityId());
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool IsForeignAsset(EntityId entityId);

        [System.Obsolete(@"Please use IsForeignAsset(EntityId) with the EntityId type instead.", true)]
        public static bool IsForeignAsset(int instanceID) => IsForeignAsset((EntityId)instanceID);

        public static bool IsNativeAsset(Object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj is null");
            return IsNativeAsset(obj.GetEntityId());
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool IsNativeAsset(EntityId entityId);

        [System.Obsolete(@"Please use IsNativeAsset(EntityId) with the EntityId type instead.", true)]
        public static bool IsNativeAsset(int instanceID) => IsNativeAsset((EntityId)instanceID);

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static int GetScriptableObjectsWithMissingScriptCount(string assetPath);

        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static int RemoveScriptableObjectsWithMissingScript(string assetPath);

        [FreeFunction()]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string GetCurrentCacheServerIp();

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string GenerateUniqueAssetPath(string path);

        [FreeFunction("AssetDatabase::StartAssetImporting")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void StartAssetEditing();

        [FreeFunction("AssetDatabase::StopAssetImporting")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void StopAssetEditing();

        // A class used for Starting/Stopping asset editing. Let's a user start/stop using RAII
        public class AssetEditingScope : IDisposable
        {
            private bool disposed = false;

            public AssetEditingScope()
            {
                AssetDatabase.StartAssetEditing();
            }

            ~AssetEditingScope()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                // We've already been disposed.
                if (this.disposed)
                    return;

                // disposing is only false when the user forgot to Dispose() it properly
                // so we should warn them about not properly disposing it as it will freeze the editor.
                if (!disposing) {
                    // It would be cool to inform the user about where new AssetEditingScope was called,
                    // but I'm unsure how to do that correctly/efficiently, so we'll just warn the user.
                    Debug.LogWarning(
                        "AssetEditingScope.Dispose() was never called on an instance of AssetEditingScope. " +
                        "This could freeze the editor for a short while. Check out the documentation for more info."
                    );
                } else {
                    // StopAssetEditing isn't threadsafe, so we don't call it from the finalizer (as that is in a different thread)
                    AssetDatabase.StopAssetEditing();
                }
            }
        }

        [FreeFunction("AssetDatabase::UnloadAllFileStreams")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void ReleaseCachedFileHandles();

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string ValidateMoveAsset(string oldPath, string newPath);

        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        extern public static string MoveAsset(string oldPath, string newPath);

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string ExtractAsset(Object asset, string newPath);

        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string RenameAsset(string pathName, string newName);

        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool MoveAssetToTrash(string path);

        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern private static bool DeleteAssetsCommon(string[] paths, [Out] List<string> outFailedPaths, bool moveAssetsToTrash);

        public static bool MoveAssetsToTrash(string[] paths, List<string> outFailedPaths)
        {
            if (paths == null)
                throw new ArgumentNullException(nameof(paths));
            if (outFailedPaths == null)
                throw new ArgumentNullException(nameof(outFailedPaths));
            return DeleteAssetsCommon(paths, outFailedPaths, true);
        }

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool DeleteAsset(string path);

        public static bool DeleteAssets(string[] paths, List<string> outFailedPaths)
        {
            if (paths == null)
                throw new ArgumentNullException(nameof(paths));
            if (outFailedPaths == null)
                throw new ArgumentNullException(nameof(outFailedPaths));
            return DeleteAssetsCommon(paths, outFailedPaths, false);
        }

        [uei.ExcludeFromDocs] public static void ImportAsset(string path) { ImportAsset(path, ImportAssetOptions.Default); }
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void ImportAsset(string path, [uei.DefaultValue("ImportAssetOptions.Default")] ImportAssetOptions options);

        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool CopyAsset(string path, string newPath);

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool CopyAssets(string[] paths, string[] newPaths);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool WriteImportSettingsIfDirty(string path);

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string[] GetSubFolders([NotNull] string path);

        [FreeFunction("AssetDatabase::IsFolderAsset")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool IsValidFolder(string path);

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kGatheringDependenciesFromSourceFile, PreventExecutionSeverity.PreventExecution_ManagedException, "Assets may not be created during gathering of import dependencies")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void CreateAsset([NotNull] Object asset, string path);

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_Warning)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern static internal void CreateAssetFromObjects(Object[] assets, string path);

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void AddObjectToAsset([NotNull] Object objectToAdd, string path);
        static public void AddObjectToAsset(Object objectToAdd, Object assetObject) { AddObjectToAsset_Obj(objectToAdd, assetObject); }

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException, "AssetDatabase.AddObjectToAsset() was called as part of running an import in a worker process.")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern private static void AddObjectToAsset_Obj([NotNull] Object newAsset, [NotNull] Object sameAssetFile);

        [System.Obsolete(@"Please use AddEntityIdToAssetWithRandomFileId instead.", true)]
        static internal void AddInstanceIDToAssetWithRandomFileId(int instanceIDToAdd, Object assetObject, bool hide) => AddEntityIdToAssetWithRandomFileId(instanceIDToAdd, assetObject, hide);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern static internal void AddEntityIdToAssetWithRandomFileId(EntityId entityIdToAdd, Object assetObject, bool hide);

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void SetMainObject([NotNull] Object mainObject, string assetPath);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string GetAssetPath(Object assetObject);

        [System.Obsolete(@"Please use GetAssetPath(EntityId) with the EntityId parameter type instead.", true)]
        public static string GetAssetPath(int instanceID) { return GetAssetPathFromEntityId(instanceID); }
        public static string GetAssetPath(EntityId entityId) { return GetAssetPathFromEntityId(entityId); }

        [FreeFunction("::GetAssetPathFromEntityId")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern private static string GetAssetPathFromEntityId(EntityId entityId);

        [System.Obsolete(@"Please use GetMainAssetEntityId instead.", true)]
        internal static int GetMainAssetInstanceID(string assetPath) => GetMainAssetEntityId(assetPath);
        [System.Obsolete(@"Please use GetMainAssetOrInProgressProxyEntityId instead.", true)]
        internal static int GetMainAssetOrInProgressProxyInstanceID(string assetPath) => GetMainAssetOrInProgressProxyEntityId(assetPath);

        [VisibleToOtherModules("UnityEditor.ProjectAuditorModule")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static EntityId GetMainAssetEntityId(string assetPath);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static EntityId GetMainAssetOrInProgressProxyEntityId(string assetPath);

        [FreeFunction("::GetAssetOrScenePath")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string GetAssetOrScenePath(Object assetObject);

        [FreeFunction("AssetDatabase::TextMetaFilePathFromAssetPath")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string GetTextMetaFilePathFromAssetPath(string path);

        [FreeFunction("AssetDatabase::AssetPathFromTextMetaFilePath")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string GetAssetPathFromTextMetaFilePath(string path);

        [NativeMethod(ThrowsException = true)]
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kGatheringDependenciesFromSourceFile, PreventExecutionSeverity.PreventExecution_ManagedException, "Assets may not be loaded while dependencies are being gathered, as these assets may not have been imported yet.")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kDomainBackup, PreventExecutionSeverity.PreventExecution_ManagedException, "Assets may not be loaded while domain backup is running, as this will change the underlying state.")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static Object LoadAssetAtPath(string assetPath, Type type);

        public static T LoadAssetAtPath<T>(string assetPath) where T : Object
        {
            return (T)LoadAssetAtPath(assetPath, typeof(T));
        }

        [NativeMethod(ThrowsException = true)]
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kGatheringDependenciesFromSourceFile, PreventExecutionSeverity.PreventExecution_ManagedException, "Assets may not be loaded while dependencies are being gathered, as these assets may not have been imported yet.")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kDomainBackup, PreventExecutionSeverity.PreventExecution_ManagedException, "Assets may not be loaded while domain backup is running, as this will change the underlying state.")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static Object LoadAssetByGUID(GUID assetGUID, Type type);

        public static T LoadAssetByGUID<T>(GUID assetGUID) where T : Object
        {
            return (T)LoadAssetByGUID(assetGUID, typeof(T));
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kGatheringDependenciesFromSourceFile, PreventExecutionSeverity.PreventExecution_ManagedException, "Assets may not be loaded while dependencies are being gathered, as these assets may not have been imported yet.")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static Object LoadMainAssetAtPath(string assetPath);

        [FreeFunction("AssetDatabase::GetMainAssetObject")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kGatheringDependenciesFromSourceFile, PreventExecutionSeverity.PreventExecution_ManagedException, "Assets may not be loaded while dependencies are being gathered, as these assets may not have been imported yet.")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static Object LoadMainAssetAtGUID(GUID assetGUID);

        [FreeFunction("AssetDatabase::EntityIdsToGUIDs")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static void EntityIdsToGUIDs(IntPtr entityIdsPtr, IntPtr guidsPtr, int len);


        [System.Obsolete(@"Please use EntityIDsToGUIDs() instead.", true)]
        public unsafe static void InstanceIDsToGUIDs(NativeArray<int> instanceIDs, NativeArray<GUID> guidsOut)
        {
            if (!instanceIDs.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(instanceIDs));

            if (!guidsOut.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(guidsOut));

            if (instanceIDs.Length != guidsOut.Length)
                throw new ArgumentException("instanceIDs and guidsOut size mismatch!");

            // This obsolete method is only valid so long as EntityId is still 4 bytes (size of int)
            // EntityId is now 8 bytes, but we use GetRawData() to get the lower 32 bits for compatibility
            Debug.Assert(sizeof(ulong) == sizeof(EntityId));

            EntityIdsToGUIDs((IntPtr)instanceIDs.GetUnsafeReadOnlyPtr(), (IntPtr)guidsOut.GetUnsafePtr(), instanceIDs.Length);
        }

        public unsafe static void EntityIdsToGUIDs(NativeArray<EntityId> entityIds, NativeArray<GUID> guidsOut)
        {
            if (!entityIds.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(entityIds));

            if (!guidsOut.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(guidsOut));

            if (entityIds.Length != guidsOut.Length)
                throw new ArgumentException("entityIds and guidsOut size mismatch!");

            EntityIdsToGUIDs((IntPtr)entityIds.GetUnsafeReadOnlyPtr(), (IntPtr)guidsOut.GetUnsafePtr(), entityIds.Length);
        }

        [FreeFunction("AssetDatabase::ReserveMonoScriptEntityId")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static EntityId ReserveMonoScriptEntityId(GUID guid);

        [System.Obsolete(@"Please use ReserveMonoScriptEntityId() instead.", false)]
        internal static int ReserveMonoScriptInstanceID(GUID guid) => ReserveMonoScriptEntityId(guid);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static System.Type GetMainAssetTypeAtPath(string assetPath);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static System.Type GetMainAssetTypeFromGUID(GUID guid);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static System.Type GetTypeFromPathAndFileID(string assetPath, long localIdentifierInFile);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool IsMainAssetAtPathLoaded(string assetPath);

        [PreventExecutionInState(AssetDatabasePreventExecution.kGatheringDependenciesFromSourceFile, PreventExecutionSeverity.PreventExecution_ManagedException, "Assets may not be loaded while dependencies are being gathered, as these assets may not have been imported yet.")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static Object[] LoadAllAssetRepresentationsAtPath(string assetPath);

        [PreventExecutionInState(AssetDatabasePreventExecution.kGatheringDependenciesFromSourceFile, PreventExecutionSeverity.PreventExecution_ManagedException, "Assets may not be loaded while dependencies are being gathered, as these assets may not have been imported yet.")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static Object[] LoadAllAssetsAtPath(string assetPath);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string[] GetAllAssetPaths();

        [uei.ExcludeFromDocs] public static void Refresh() { Refresh(ImportAssetOptions.Default); }

        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void Refresh([uei.DefaultValue("ImportAssetOptions.Default")] ImportAssetOptions options);

        [FreeFunction("::CanOpenAssetInEditor")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool CanOpenAssetInEditor(EntityId entityId);
        [System.Obsolete(@"Please use CanOpenAssetInEditor(EntityId) with the EntityId parameter type instead.", true)]
        public static bool CanOpenAssetInEditor(int instanceID) => CanOpenAssetInEditor((EntityId)instanceID);

        [uei.ExcludeFromDocs] public static bool OpenAsset(EntityId entityId) { return OpenAsset(entityId, -1, -1); }

        [System.Obsolete(@"Please use OpenAsset(EntityId) with the EntityId parameter type instead.", true)]
        [uei.ExcludeFromDocs] public static bool OpenAsset(int instanceID) { return OpenAsset((EntityId)instanceID, -1, -1); }

        public static bool OpenAsset(EntityId entityId, [uei.DefaultValue("-1")] int lineNumber) { return OpenAsset(entityId, lineNumber, -1); }

        [System.Obsolete(@"Please use OpenAsset(EntityId, int) with the EntityId parameter type instead.", true)]
        public static bool OpenAsset(int instanceID, [uei.DefaultValue("-1")] int lineNumber) { return OpenAsset((EntityId)instanceID, lineNumber, -1); }

        [FreeFunction("::OpenAsset")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool OpenAsset(EntityId entityId, int lineNumber, int columnNumber);

        [System.Obsolete(@"Please use OpenAsset(EntityId, int, int) with the EntityId parameter type instead.", true)]
        public static bool OpenAsset(int instanceID, int lineNumber, int columnNumber) => OpenAsset((EntityId)instanceID, lineNumber, columnNumber);

        [uei.ExcludeFromDocs] public static bool OpenAsset(Object target) { return OpenAsset(target, -1); }
        public static bool OpenAsset(Object target, [uei.DefaultValue("-1")] int lineNumber) { return OpenAsset(target, lineNumber, -1); }

        static public bool OpenAsset(Object target, int lineNumber, int columnNumber)
        {
            if (target)
                return OpenAsset(target.GetEntityId(), lineNumber, columnNumber);
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

        [FreeFunction("AssetDatabase::GetAssetOrigin")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern private static AssetOrigin GetAssetOrigin_Internal(GUID guid);

        internal static AssetOrigin GetAssetOrigin(GUID guid)
        {
            return GetAssetOrigin_Internal(guid);
        }
        internal static AssetOrigin GetAssetOrigin(string guid)
        {
            return GetAssetOrigin_Internal(new GUID(guid));
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static string GUIDToAssetPath_Internal(GUID guid);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static GUID AssetPathToGUID_Internal(string path);

        public static string GUIDToAssetPath(string guid)
        {
            return GUIDToAssetPath_Internal(new GUID(guid));
        }

        public static string GUIDToAssetPath(GUID guid)
        {
            return GUIDToAssetPath_Internal(guid);
        }

        public static GUID GUIDFromAssetPath(string path)
        {
            return AssetPathToGUID_Internal(path);
        }

        public static string AssetPathToGUID(string path)
        {
            return AssetPathToGUID(path, AssetPathToGUIDOptions.IncludeRecentlyDeletedAssets);
        }

        public static string AssetPathToGUID(string path, [DefaultValue("AssetPathToGUIDOptions.IncludeRecentlyDeletedAssets")] AssetPathToGUIDOptions options)
        {
            GUID guid;

            switch (options)
            {
                case AssetPathToGUIDOptions.OnlyExistingAssets:
                    guid = GUIDFromExistingAssetPath(path);
                    break;
                default:
                    guid = AssetPathToGUID_Internal(path);
                    break;
            }

            return guid.Empty() ? "" : guid.ToString();
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool AssetPathExists(string path);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static Hash128 GetAssetDependencyHash(GUID guid);

        public static Hash128 GetAssetDependencyHash(string path)
        {
            return GetAssetDependencyHash(GUIDFromAssetPath(path));
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static Hash128 GetSourceAssetFileHash(string guid);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static Hash128 GetSourceAssetMetaFileHash(string guid);

        [FreeFunction("AssetDatabase::SaveAssets")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void SaveAssets();

        [FreeFunction("AssetDatabase::SaveAssetIfDirty")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void SaveAssetIfDirty(GUID guid);

        public static void SaveAssetIfDirty(Object obj)
        {
            string guidString;
            long localID;

            if (TryGetGUIDAndLocalFileIdentifier(obj.GetEntityId(), out guidString, out localID))
                SaveAssetIfDirty(new GUID(guidString));
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static Texture GetCachedIcon(string path);

        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void SetLabels(Object obj, string[] labels);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern private static void GetAllLabelsImpl([Out] List<string> labelsList, [Out] List<float> scoresList);

        internal static Dictionary<string, float> GetAllLabels()
        {
            var labelsList = new List<string>();
            var scoresList = new List<float>();
            GetAllLabelsImpl(labelsList, scoresList);

            Dictionary<string, float> res = new Dictionary<string, float>(labelsList.Count);
            for (int i = 0; i < labelsList.Count; ++i)
            {
                res[labelsList[i]] = scoresList[i];
            }
            return res;
        }

        [FreeFunction("AssetDatabase::GetLabels")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern private static string[] GetLabelsInternal(GUID guid);
        public static string[] GetLabels(GUID guid)
        {
            return GetLabelsInternal(guid);
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string[] GetLabels(Object obj);

        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void ClearLabels(Object obj);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string[] GetAllAssetBundleNames();

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static string[] GetAllAssetBundleNamesWithoutVariant();

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static string[] GetAllAssetBundleVariants();

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string[] GetUnusedAssetBundleNames();

        [FreeFunction("AssetDatabase::RemoveAssetBundleByName")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static bool RemoveAssetBundleName(string assetBundleName, bool forceRemove);

        [FreeFunction("AssetDatabase::RemoveUnusedAssetBundleNames")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void RemoveUnusedAssetBundleNames();

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string[] GetAssetPathsFromAssetBundle(string assetBundleName);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string[] GetAssetPathsFromAssetBundleAndAssetName(string assetBundleName, string assetName);
        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string GetImplicitAssetBundleName(string assetPath);

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string GetImplicitAssetBundleVariantName(string assetPath);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string[] GetAssetBundleDependencies(string assetBundleName, bool recursive);

        public static string[] GetDependencies(string pathName) { return GetDependencies(pathName, true); }
        public static string[] GetDependencies(string pathName, bool recursive)
        {
            string[] input = new string[1];
            input[0] = pathName;
            return GetDependencies(input, recursive);
        }

        public static string[] GetDependencies(string[] pathNames) { return GetDependencies(pathNames, true); }
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static string[] GetDependencies(string[] pathNames, bool recursive);

        [Obsolete("Use UnityEditor.AssetPackage.Package.Export(ExportPackageParameters) instead.", false)]
        public static void ExportPackage(string assetPathName, string fileName)
        {
            string[] input = new string[1];
            input[0] = assetPathName;
            ExportPackage(input, fileName, ExportPackageOptions.Default);
        }

        [Obsolete("Use UnityEditor.AssetPackage.Package.Export(ExportPackageParameters) instead.", false)]
        public static void ExportPackage(string assetPathName, string fileName, ExportPackageOptions flags)
        {
            string[] input = new string[1];
            input[0] = assetPathName;
            ExportPackage(input, fileName, flags);
        }

        [Obsolete("Use UnityEditor.AssetPackage.Package.Export(ExportPackageParameters) instead.", false)]
        public static void ExportPackage(string[] assetPathNames, string fileName, [uei.DefaultValue("ExportPackageOptions.Default")] ExportPackageOptions flags)
        {
            ExportPackage(assetPathNames, fileName, "", flags);
        }

        [Obsolete("Use UnityEditor.AssetPackage.Package.Export(ExportPackageParameters) instead.", false)]
        public static void ExportPackage(string assetPathName, string fileName, string ownerOrgId, [uei.DefaultValue("ExportPackageOptions.Default")] ExportPackageOptions flags)
        {
            string[] input = new string[1];
            input[0] = assetPathName;
            ExportPackage(input, fileName, ownerOrgId, flags);
        }

        [Obsolete("Use UnityEditor.AssetPackage.Package.Export(ExportPackageParameters) instead.", false)]
        [uei.ExcludeFromDocs] public static void ExportPackage(string[] assetPathNames, string fileName) { ExportPackage(assetPathNames, fileName, ExportPackageOptions.Default); }

        [Obsolete("Use UnityEditor.AssetPackage.Package.Export(ExportPackageParameters) instead.", false)]
        public static void ExportPackage(string[] assetPathNames, string fileName, string ownerOrgId, [uei.DefaultValue("ExportPackageOptions.Default")] ExportPackageOptions flags)
        {
            UnityEditor.AssetPackage.Package.Export(new UnityEditor.AssetPackage.ExportPackageParameters(assetPathNames, fileName, ownerOrgId, flags));
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static string GetUniquePathNameAtSelectedPath(string fileName);

        [uei.ExcludeFromDocs]
        public static bool CanOpenForEdit(UnityEngine.Object assetObject)
        {
            return CanOpenForEdit(assetObject, StatusQueryOptions.UseCachedIfPossible);
        }

        public static bool CanOpenForEdit(UnityEngine.Object assetObject, [uei.DefaultValue("StatusQueryOptions.UseCachedIfPossible")] StatusQueryOptions statusOptions)
        {
            string assetPath = GetAssetOrScenePath(assetObject);
            return CanOpenForEdit(assetPath, statusOptions);
        }

        [uei.ExcludeFromDocs]
        public static bool CanOpenForEdit(string assetOrMetaFilePath)
        {
            return CanOpenForEdit(assetOrMetaFilePath, StatusQueryOptions.UseCachedIfPossible);
        }

        public static bool CanOpenForEdit(string assetOrMetaFilePath, [uei.DefaultValue("StatusQueryOptions.UseCachedIfPossible")] StatusQueryOptions statusOptions)
        {
            string message;
            return CanOpenForEdit(assetOrMetaFilePath, out message, statusOptions);
        }

        [uei.ExcludeFromDocs]
        public static bool CanOpenForEdit(UnityEngine.Object assetObject, out string message)
        {
            return CanOpenForEdit(assetObject, out message, StatusQueryOptions.UseCachedIfPossible);
        }

        public static bool CanOpenForEdit(UnityEngine.Object assetObject, out string message, [uei.DefaultValue("StatusQueryOptions.UseCachedIfPossible")] StatusQueryOptions statusOptions)
        {
            string assetPath = GetAssetOrScenePath(assetObject);
            return CanOpenForEdit(assetPath, out message, statusOptions);
        }

        [uei.ExcludeFromDocs]
        public static bool CanOpenForEdit(string assetOrMetaFilePath, out string message)
        {
            return CanOpenForEdit(assetOrMetaFilePath, out message, StatusQueryOptions.UseCachedIfPossible);
        }

        public static bool CanOpenForEdit(string assetOrMetaFilePath, out string message, [uei.DefaultValue("StatusQueryOptions.UseCachedIfPossible")] StatusQueryOptions statusOptions)
        {
            return AssetModificationProcessorInternal.CanOpenForEdit(assetOrMetaFilePath, out message, statusOptions);
        }

        [uei.ExcludeFromDocs] public static bool IsOpenForEdit(UnityEngine.Object assetObject)
        {
            return IsOpenForEdit(assetObject, StatusQueryOptions.UseCachedIfPossible);
        }

        public static bool IsOpenForEdit(UnityEngine.Object assetObject, [uei.DefaultValue("StatusQueryOptions.UseCachedIfPossible")] StatusQueryOptions statusOptions)
        {
            string assetPath = GetAssetOrScenePath(assetObject);
            return IsOpenForEdit(assetPath, statusOptions);
        }

        [uei.ExcludeFromDocs] public static bool IsOpenForEdit(string assetOrMetaFilePath)
        {
            return IsOpenForEdit(assetOrMetaFilePath, StatusQueryOptions.UseCachedIfPossible);
        }

        public static bool IsOpenForEdit(string assetOrMetaFilePath, [uei.DefaultValue("StatusQueryOptions.UseCachedIfPossible")] StatusQueryOptions statusOptions)
        {
            string message;
            return IsOpenForEdit(assetOrMetaFilePath, out message, statusOptions);
        }

        [uei.ExcludeFromDocs] public static bool IsOpenForEdit(UnityEngine.Object assetObject, out string message)
        {
            return IsOpenForEdit(assetObject, out message, StatusQueryOptions.UseCachedIfPossible);
        }

        public static bool IsOpenForEdit(UnityEngine.Object assetObject, out string message, [uei.DefaultValue("StatusQueryOptions.UseCachedIfPossible")] StatusQueryOptions statusOptions)
        {
            string assetPath = GetAssetOrScenePath(assetObject);
            return IsOpenForEdit(assetPath, out message, statusOptions);
        }

        [uei.ExcludeFromDocs] public static bool IsOpenForEdit(string assetOrMetaFilePath, out string message)
        {
            return IsOpenForEdit(assetOrMetaFilePath, out message, StatusQueryOptions.UseCachedIfPossible);
        }

        public static bool IsOpenForEdit(string assetOrMetaFilePath, out string message, [uei.DefaultValue("StatusQueryOptions.UseCachedIfPossible")] StatusQueryOptions statusOptions)
        {
            return AssetModificationProcessorInternal.IsOpenForEdit(assetOrMetaFilePath, out message, statusOptions);
        }

        [uei.ExcludeFromDocs] public static bool IsMetaFileOpenForEdit(UnityEngine.Object assetObject)
        {
            return IsMetaFileOpenForEdit(assetObject, StatusQueryOptions.UseCachedIfPossible);
        }

        public static bool IsMetaFileOpenForEdit(UnityEngine.Object assetObject, [uei.DefaultValue("StatusQueryOptions.UseCachedIfPossible")] StatusQueryOptions statusOptions)
        {
            string message;
            return IsMetaFileOpenForEdit(assetObject, out message, statusOptions);
        }

        [uei.ExcludeFromDocs] public static bool IsMetaFileOpenForEdit(UnityEngine.Object assetObject, out string message)
        {
            return IsMetaFileOpenForEdit(assetObject, out message, StatusQueryOptions.UseCachedIfPossible);
        }

        public static bool IsMetaFileOpenForEdit(UnityEngine.Object assetObject, out string message, [uei.DefaultValue("StatusQueryOptions.UseCachedIfPossible")] StatusQueryOptions statusOptions)
        {
            string assetPath = GetAssetOrScenePath(assetObject);
            string metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetPath);
            return IsOpenForEdit(metaPath, out message, statusOptions);
        }

        public static T GetBuiltinExtraResource<T>(string path) where T : Object
        {
            return (T)GetBuiltinExtraResource(typeof(T), path);
        }

        [NativeMethod(ThrowsException = true)]
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static Object GetBuiltinExtraResource(Type type, string path);

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static string[] CollectAllChildren(string guid, string[] collection);

        internal extern static string assetFolderGUID
        {
            [FreeFunction("AssetDatabaseBindings::GetAssetFolderGUID")]
            [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
            get;
        }

        [FreeFunction("AssetDatabase::IsV1Enabled")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static bool IsV1Enabled();

        [FreeFunction("AssetDatabase::IsV2Enabled")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static bool IsV2Enabled();

        [FreeFunction("AssetDatabase::CloseCachedFiles")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static void CloseCachedFiles();

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static string[] GetSourceAssetImportDependenciesAsGUIDs(string path);

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static string[] GetImportedAssetImportDependenciesAsGUIDs(string path);

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static string[] GetGuidOfPathLocationImportDependencies(string path);

        [FreeFunction("AssetDatabase::ReSerializeAssetsForced")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kPreventForceReserializeAssets, PreventExecutionSeverity.PreventExecution_ManagedException, "Consider calling ForceReserializeAssets from menu style entry point.")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern private static void ForceReserializeAssets(GUID[] guids, ForceReserializeAssetsOptions options);

        public static void ForceReserializeAssets(IEnumerable<string> assetPaths, ForceReserializeAssetsOptions options = ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata)
        {
            if (EditorApplication.isPlaying)
                throw new Exception("AssetDatabase.ForceReserializeAssets cannot be used when in play mode");

            HashSet<GUID> guidList = new HashSet<GUID>();

            foreach (string path in assetPaths)
            {
                if (path == "")
                    continue;

                if (InternalEditorUtility.IsUnityExtensionRegistered(path))
                    continue;

                bool rootFolder, readOnly;
                bool validPath = TryGetAssetFolderInfo(path, out rootFolder, out readOnly);
                if (validPath && (rootFolder || readOnly))
                    continue;

                GUID guid = GUIDFromExistingAssetPath(path);

                if (!guid.Empty())
                {
                    guidList.Add(guid);
                }
                else
                {
                    if (File.Exists(path))
                    {
                        Debug.LogWarningFormat("Cannot reserialize file \"{0}\": the file is not in the AssetDatabase. Skipping.", path);
                    }
                    else
                    {
                        Debug.LogWarningFormat("Cannot reserialize file \"{0}\": the file does not exist. Skipping.", path);
                    }
                }
            }

            GUID[] guids = new GUID[guidList.Count];
            guidList.CopyTo(guids);
            ForceReserializeAssets(guids, options);
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static System.Type GetTypeFromVisibleGUIDAndLocalFileIdentifier(GUID guid, long localId);

        [FreeFunction("AssetDatabase::GetGUIDAndLocalIdentifierInFile")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern private static bool GetGUIDAndLocalIdentifierInFile(EntityId entityId, out GUID outGuid, out long outLocalId);

        public static bool TryGetGUIDAndLocalFileIdentifier(Object obj, out string guid, out long localId)
        {
            return TryGetGUIDAndLocalFileIdentifier(obj.GetEntityId(), out guid, out localId);
        }

        [System.Obsolete(@"Please use TryGetGUIDAndLocalFileIdentifier(EntityId, out string, out long) with the EntityId type instead.", true)]
        public static bool TryGetGUIDAndLocalFileIdentifier(int instanceID, out string guid, out long localId)
        {
            GUID uguid;
            bool res = GetGUIDAndLocalIdentifierInFile(instanceID, out uguid, out localId);
            guid = uguid.ToString();
            return res;
        }

        public static bool TryGetGUIDAndLocalFileIdentifier(EntityId entityId, out string guid, out long localId)
        {
            GUID uguid;
            bool res = GetGUIDAndLocalIdentifierInFile(entityId, out uguid, out localId);
            guid = uguid.ToString();
            return res;
        }

        public static bool TryGetGUIDAndLocalFileIdentifier<T>(LazyLoadReference<T> assetRef, out string guid, out long localId) where T : UnityEngine.Object
        {
            return TryGetGUIDAndLocalFileIdentifier((EntityId)assetRef.entityId, out guid, out localId);
        }

        public static void ForceReserializeAssets()
        {
            ForceReserializeAssets(GetAllAssetPaths());
        }

        [FreeFunction("AssetDatabase::RemoveObjectFromAsset")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringImportHowToFixMsg)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void RemoveObjectFromAsset([NotNull] Object objectToRemove);

        [PreventExecutionInState(AssetDatabasePreventExecution.kGatheringDependenciesFromSourceFile, PreventExecutionSeverity.PreventExecution_ManagedException, "Cannot call AssetDatabase.LoadObjectAsync during the gathering of import dependencies.")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException, "Cannot use AssetDatabase.LoadObjectAsync while assets are importing.")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static AssetDatabaseLoadOperation LoadObjectAsync(string assetPath, long localId);

        [FreeFunction("AssetDatabase::GUIDFromExistingAssetPath")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static GUID GUIDFromExistingAssetPath(string path);

        [Obsolete("Use UnityEditor.AssetPackage.Package.Import(string packagePath, bool interactive) instead.", false)]
        public static void ImportPackage(string packagePath, bool interactive)
        {
            UnityEditor.AssetPackage.Package.Import(packagePath, interactive);
        }

        [FreeFunction("ApplicationDisallowAutoRefresh")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public static extern void DisallowAutoRefresh();

        [FreeFunction("ApplicationAllowAutoRefresh")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public static extern void AllowAutoRefresh();

        [FreeFunction("ApplicationDisableUpdating")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        internal static extern void DisableUpdating();

        [FreeFunction("ApplicationEnableUpdating")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        internal static extern void EnableUpdating(bool forceSceneUpdate);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static UInt32 GlobalArtifactDependencyVersion
        {
            [FreeFunction("AssetDatabase::GetGlobalArtifactDependencyVersion")]
            [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
            get;
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static UInt32 GlobalArtifactProcessedVersion
        {
            [FreeFunction("AssetDatabase::GetGlobalArtifactProcessedVersion")]
            [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
            get;
        }

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private extern static ArtifactInfo[] GetArtifactInfos_Internal(GUID guid);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private extern static ArtifactInfo[] GetCurrentRevisions_Internal(GUID[] guids);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private extern static ArtifactInfo[] GetImportActivityWindowStartupData_Internal(ImportActivityWindowStartupData dataType);

        internal static ArtifactInfo[] GetCurrentRevisions(GUID[] guids)
        {
            var artifactInfos = GetCurrentRevisions_Internal(guids);
            return artifactInfos;
        }

        internal static ArtifactInfo[] GetImportActivityWindowStartupData(ImportActivityWindowStartupData dataType)
        {
            return GetImportActivityWindowStartupData_Internal(dataType);
        }

        internal static ArtifactInfo[] GetArtifactInfos(GUID guid)
        {
            var artifactInfos = GetArtifactInfos_Internal(guid);
            return artifactInfos;
        }

        [FreeFunction("AssetDatabase::ClearImporterOverride")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static void ClearImporterOverride(string path);

        [FreeFunction("AssetDatabase::IsCacheServerEnabled")]
                [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static bool IsCacheServerEnabled();

        [FreeFunction("AssetDatabase::SetImporterOverride")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static void SetImporterOverrideInternal(string path, System.Type importer);

        public static void SetImporterOverride<T>(string path)
            where T : AssetImporter
        {
            if (GUIDFromExistingAssetPath(path).Empty())
            {
                Debug.LogError(
                    $"Cannot set Importer override at \"{path}\". No Asset found at that path.");
                return;
            }

            var availableImporters = GetAvailableImporters(path);
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (availableImporters.Contains(typeof(T)))
#pragma warning restore UA2001
            {
                SetImporterOverrideInternal(path, typeof(T));
            }
            else
            {
                if (GetDefaultImporter(path) == typeof(T))
                {
                    ClearImporterOverride(path);
                    Debug.LogWarning("This usage is deprecated. Use ClearImporterOverride to revert to the default Importer instead.");
                }
                else
                {
                    Debug.LogError(
                        $"Cannot set Importer override at {path} because {typeof(T).Name} is not a valid Importer for this asset.");
                }
            }
        }

        [FreeFunction("AssetDatabase::GetImporterOverride")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static System.Type GetImporterOverride(string path);

        [FreeFunction("AssetDatabase::GetAvailableImporters")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static Type[] GetAvailableImporters(string path);

        [FreeFunction("AssetDatabase::GetDefaultImporter")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern public static Type GetDefaultImporter(string path);

        [FreeFunction("RefreshProfiler::EnableVerboseProfiling")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        internal static extern bool EnableVerboseProfiling(bool enable);

        [FreeFunction("AcceleratorClientCanConnectTo")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static bool CanConnectToCacheServer(string ip, UInt16 port);

        [FreeFunction("RefreshSettings")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private extern static void _RefreshSettings();
        public static void RefreshSettings() => _RefreshSettings();

        public static event Action<CacheServerConnectionChangedParameters> cacheServerConnectionChanged;
        [RequiredByNativeCode]
        private static void OnCacheServerConnectionChanged()
        {
            if (cacheServerConnectionChanged != null)
            {
                CacheServerConnectionChangedParameters param;
                cacheServerConnectionChanged(param);
            }
        }

        [FreeFunction("AcceleratorClientIsConnected")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private extern static bool _IsConnectedToCacheServer();
        public static bool IsConnectedToCacheServer() => _IsConnectedToCacheServer();

        [FreeFunction("AcceleratorClientResetReconnectTimer")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static void ResetCacheServerReconnectTimer();

        [FreeFunction("AcceleratorClientCloseConnection")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static void CloseCacheServerConnection();

        [FreeFunction()]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static string GetCacheServerAddress();

        [FreeFunction()]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static UInt16 GetCacheServerPort();

        [FreeFunction("AssetDatabase::GetCacheServerNamespacePrefix")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static string GetCacheServerNamespacePrefix();

        [FreeFunction("AssetDatabase::GetCacheServerEnableDownload")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static bool GetCacheServerEnableDownload();

        [FreeFunction("AssetDatabase::GetCacheServerEnableUpload")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static bool GetCacheServerEnableUpload();

        [FreeFunction("AssetDatabase::WaitForPendingCacheServerRequestsToComplete")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private extern static void _WaitForPendingCacheServerRequestsToComplete();
        internal static void WaitForPendingCacheServerRequestsToComplete() => _WaitForPendingCacheServerRequestsToComplete();

        [FreeFunction("AssetDatabase::IsCacheServerImportResultCachingEnabled")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static bool IsCacheServerImportResultCachingEnabled();

        [FreeFunction("AssetDatabase::IsDirectoryMonitoringEnabled")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static bool IsDirectoryMonitoringEnabled();

        [FreeFunction("AssetDatabase::RegisterCustomDependency")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kPreventCustomDependencyChanges, PreventExecutionSeverity.PreventExecution_ManagedException, "Custom dependencies can only be added when the AssetDatabase is not importing.")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException, "Custom dependencies can only be added when the AssetDatabase is not importing.")]
        public extern static void RegisterCustomDependency(string dependency, Hash128 hashOfValue);

        [FreeFunction("AssetDatabase::UnregisterCustomDependencyPrefixFilter")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kPreventCustomDependencyChanges, PreventExecutionSeverity.PreventExecution_ManagedException, "Custom dependencies can only be removed when the AssetDatabase is not importing.")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingInWorkerProcess, PreventExecutionSeverity.PreventExecution_ManagedException, "Custom dependencies can only be removed when the AssetDatabase is not importing.")]
        public extern static UInt32 UnregisterCustomDependencyPrefixFilter(string prefixFilter);

        [FreeFunction("AssetDatabase::IsAssetImportProcess")]
        public extern static bool IsAssetImportWorkerProcess();

        [FreeFunction("AssetDatabase::GetImporterType")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static Type GetImporterType(GUID guid);

        [FreeFunction("AssetDatabase::GetImporterTypes")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public static extern Type[] GetImporterTypes(ReadOnlySpan<GUID> guids);

        //Since extern method overloads are not supported
        //this is the name we pick, but users end up being able
        //to call either of the overloads
        [FreeFunction("AssetDatabase::GetImporterTypesAtPaths")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private static extern Type[] GetImporterTypesAtPaths(string[] paths);

        public static Type GetImporterType(string assetPath)
        {
            return GetImporterTypeAtPath(assetPath);
        }

        //Since extern method overloads are not supported
        //this is the name we pick, but users end up being able
        //to call either of the overloads
        [FreeFunction("AssetDatabase::GetImporterTypeAtPath")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private static extern Type GetImporterTypeAtPath(string assetPath);

        public static Type[] GetImporterTypes(string[] paths)
        {
            return GetImporterTypesAtPaths(paths);
        }

        [RequiredByNativeCode]
        static string[] OnSourceAssetsModified(string[] changedAssets, string[] addedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var assetMoveInfo = new AssetMoveInfo[movedAssets.Length];
            Debug.Assert(movedAssets.Length == movedFromAssetPaths.Length);
            for (int i = 0; i < movedAssets.Length; i++)
                assetMoveInfo[i] = new AssetMoveInfo(movedFromAssetPaths[i], movedAssets[i]);

            var assetsReportedChanged = new HashSet<string>();

            foreach (Type type in TypeCache.GetTypesDerivedFrom<AssetsModifiedProcessor>())
            {
                var assetPostprocessor = Activator.CreateInstance(type) as AssetsModifiedProcessor;
                assetPostprocessor.assetsReportedChanged = assetsReportedChanged;
                assetPostprocessor.Internal_OnAssetsModified(changedAssets, addedAssets, deletedAssets, assetMoveInfo);
                assetPostprocessor.assetsReportedChanged = null;
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return assetsReportedChanged.ToArray();
#pragma warning restore UA2001
        }

        public enum RefreshImportMode
        {
            InProcess = 0,
            OutOfProcessPerQueue = 1
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static RefreshImportMode ActiveRefreshImportMode
        {
            [FreeFunction("AssetDatabase::GetRefreshImportMode")]
            [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
            get;

            [FreeFunction("AssetDatabase::SetRefreshImportMode")]
            [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
            set;
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static int DesiredWorkerCount
        {
            [FreeFunction("AssetDatabase::GetDesiredWorkerCount")]
            [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
            get;

            [FreeFunction("AssetDatabase::SetDesiredWorkerCount")]
            [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
            set;
        }

        [FreeFunction("AssetDatabase::ForceToDesiredWorkerCount")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static void ForceToDesiredWorkerCount();

        [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseTypes.h")]
        [RequiredByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WorkerStats
        {
            public int resettingWorkerCount;
            public int desiredWorkerCount;
            public int idleWorkerCount;
            public int importingWorkerCount;
            public int connectingWorkerCount;
            public int operationalWorkerCount;
            public int suspendedWorkerCount;
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        internal extern static WorkerStats GetWorkerStats();

        // Binding only created for testing
        internal static int TestOnlyDeleteAllNonPrimaryArtifacts(Type[] importers, bool deleteUnusedContentFiles)
        {
            return DeleteAllNonPrimaryArtifacts_Importer(importers, deleteUnusedContentFiles);
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private extern static int DeleteAllNonPrimaryArtifacts_Importer(Type[] importers, bool deleteUnusedContentFiles);

        // Binding only created for testing
        internal static int TestOnlyDeleteAllNonPrimaryArtifacts(ReadOnlySpan<ArtifactKey> artifactKeys, bool deleteUnusedContentFiles)
        {
            return DeleteAllNonPrimaryArtifacts_ImportAddress(artifactKeys, deleteUnusedContentFiles);
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private extern static int DeleteAllNonPrimaryArtifacts_ImportAddress(ReadOnlySpan<ArtifactKey> artifactKeys, bool deleteUnusedContentFiles);

        internal enum ImportWorkerModeFlags
        {
            kNoFlags                        = 0,
            kProfile                        = 1 << 0,
            kSafeMode                       = 1 << 1,
        };

        // Import Worker Mode binding is just for testing
        [FreeFunction("AssetDatabase::SetImportWorkerModeFlags")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        internal extern static void SetImportWorkerModeFlags(ImportWorkerModeFlags flags);

        [FreeFunction("AssetDatabase::ClearImportWorkerModeFlags")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        internal extern static void ClearImportWorkerModeFlags(ImportWorkerModeFlags flags);

        [FreeFunction("AssetDatabase::GetImportWorkerModeFlags")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, kPreventExecutionDuringCodeReloadHowToFixMsg)]
        internal extern static ImportWorkerModeFlags GetImportWorkerModeFlags();
    }
}
