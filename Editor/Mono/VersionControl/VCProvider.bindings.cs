// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEditor.VersionControl
{
    // This MUST be kept synchronized with the VCS_PROVIDER_UNSET_IDENTIFIER enum in VCProvider.cpp
    enum VCSProviderIdentifier
    {
        UnsetIdentifier = -1
    }

    // Keep internal and undocumented until we expose more functionality
    [NativeHeader("Editor/Src/VersionControl/VC_bindings.h")]
    [NativeHeader("Editor/Src/VersionControl/VCProvider.h")]
    [NativeHeader("Editor/Src/VersionControl/VCPlugin.h")]
    [NativeHeader("Editor/Src/VersionControl/VCTask.h")]
    [NativeHeader("Editor/Src/VersionControl/VCCache.h")]
    public partial class Provider
    {
        [StaticAccessor("GetVCProvider()", StaticAccessorType.Dot)]
        public static extern bool enabled
        {
            [NativeMethod("Enabled")]
            get;
        }

        [StaticAccessor("GetVCProvider()", StaticAccessorType.Dot)]
        public static extern bool isActive
        {
            [NativeMethod("IsActive")]
            get;
        }

        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        private struct Traits
        {
            public bool requiresNetwork;
            public bool enablesCheckout;
            public bool enablesVersioningFolders;
            public bool enablesChangelists;
            public bool enablesLocking;
        }

        private static extern Traits activeTraits
        {
            [FreeFunction("VersionControlBindings::VCProvider::GetActiveTraits")]
            get;
        }

        public static bool requiresNetwork
        {
            get { return activeTraits.requiresNetwork; }
        }

        public static bool hasChangelistSupport
        {
            get { return activeTraits.enablesChangelists; }
        }

        public static bool hasCheckoutSupport
        {
            get { return activeTraits.enablesCheckout; }
        }

        public static bool hasLockingSupport
        {
            get { return activeTraits.enablesLocking; }
        }

        public static bool isVersioningFolders
        {
            get { return activeTraits.enablesVersioningFolders; }
        }

        [StaticAccessor("GetVCProvider()", StaticAccessorType.Dot)]
        public static extern OnlineState onlineState
        {
            [NativeMethod("GetOnlineState")]
            get;
        }

        [StaticAccessor("GetVCProvider()", StaticAccessorType.Dot)]
        public static extern string offlineReason
        {
            [NativeMethod("OfflineReason")]
            get;
        }

        public static extern Task activeTask
        {
            [FreeFunction("VersionControlBindings::VCProvider::GetActiveTask")]
            get;
        }

        [StaticAccessor("GetVCProvider()", StaticAccessorType.Dot)]
        internal static extern Texture2D overlayAtlas
        {
            [NativeMethod("GetOverlayAtlas")]
            get;
        }

        [FreeFunction("VersionControlBindings::VCProvider::GetAtlasRectForState")]
        internal static extern Rect GetAtlasRectForState(int state);

        [FreeFunction("VersionControlBindings::VCProvider::GetActivePlugin")]
        public static extern Plugin GetActivePlugin();

        [FreeFunction("VersionControlBindings::VCProvider::GetActiveConfigFields")]
        public static extern ConfigField[] GetActiveConfigFields();

        [StaticAccessor("GetVCProvider()", StaticAccessorType.Dot)]
        internal static extern bool IsCustomCommandEnabled(string name);

        [StaticAccessor("GetVCProvider()", StaticAccessorType.Dot)]
        internal static extern CustomCommand[] customCommands
        {
            [NativeMethod("GetMonoCustomCommands")]
            get;
        }

        [StaticAccessor("GetVCProvider()", StaticAccessorType.Dot)]
        internal static extern void ClearCustomCommands();

        [FreeFunction("VersionControlBindings::VCProvider::Internal_CacheStatus")]
        private static extern Asset Internal_CacheStatus(string assetPath);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_Status", ThrowsException = true)]
        private static extern Task Internal_Status(Asset[] assets, bool recursively);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_StatusStrings")]
        private static extern Task Internal_StatusStrings(string[] assetsProjectPaths, bool recursively);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_StatusAbsolutePath")]
        private static extern Task Internal_StatusAbsolutePath(string assetPath);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_CheckoutIsValid", ThrowsException = true)]
        private static extern bool Internal_CheckoutIsValid(Asset[] assets, CheckoutMode mode);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_Checkout", ThrowsException = true)]
        private static extern Task Internal_Checkout(Asset[] assets, CheckoutMode mode, ChangeSet changeset);

        [FreeFunction("VersionControlBindings::VCProvider::MakeEditable")]
        internal static extern bool MakeEditableImpl(string[] assets, string prompt, ChangeSet changeSet, [Out] List<string> outNotEditablePathsList);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_Delete", ThrowsException = true)]
        private static extern Task Internal_Delete(Asset[] assets);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_DeleteAtProjectPath")]
        private static extern Task Internal_DeleteAtProjectPath([NotNull] string assetProjectPath);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_MoveAsStrings")]
        private static extern Task Internal_MoveAsStrings([NotNull] string from, [NotNull] string to);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_AddIsValid", ThrowsException = true)]
        private static extern bool Internal_AddIsValid(Asset[] assets);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_Add", ThrowsException = true)]
        private static extern Task Internal_Add(Asset[] assets, bool recursive);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_DeleteChangeSetsIsValid", ThrowsException = true)]
        private static extern bool Internal_DeleteChangeSetsIsValid(ChangeSet[] changesets);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_DeleteChangeSets", ThrowsException = true)]
        private static extern Task Internal_DeleteChangeSets(ChangeSet[] changesets);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_RevertChangeSets", ThrowsException = true)]
        private static extern Task Internal_RevertChangeSets(ChangeSet[] changesets, RevertMode mode);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_SubmitIsValid", ThrowsException = true)]
        private static extern bool Internal_SubmitIsValid(ChangeSet changeset, Asset[] assets);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_Submit", ThrowsException = true)]
        private static extern Task Internal_Submit(ChangeSet changeset, Asset[] assets, string description, bool saveOnly);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_DiffIsValid", ThrowsException = true)]
        private static extern bool Internal_DiffIsValid(Asset[] assets);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_DiffHead", ThrowsException = true)]
        private static extern Task Internal_DiffHead(Asset[] assets, bool includingMetaFiles);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_ResolveIsValid", ThrowsException = true)]
        private static extern bool Internal_ResolveIsValid(Asset[] assets);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_Resolve", ThrowsException = true)]
        private static extern Task Internal_Resolve(Asset[] assets, ResolveMethod resolveMethod);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_Merge", ThrowsException = true)]
        private static extern Task Internal_Merge(Asset[] assets);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_LockIsValid", ThrowsException = true)]
        private static extern bool Internal_LockIsValid(Asset[] assets);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_UnlockIsValid", ThrowsException = true)]
        private static extern bool Internal_UnlockIsValid(Asset[] assets);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_Lock", ThrowsException = true)]
        private static extern Task Internal_Lock(Asset[] assets, bool locked);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_RevertIsValid", ThrowsException = true)]
        private static extern bool Internal_RevertIsValid(Asset[] assets, RevertMode revertMode);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_Revert", ThrowsException = true)]
        private static extern Task Internal_Revert(Asset[] assets, RevertMode revertMode);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_GetLatestIsValid", ThrowsException = true)]
        private static extern bool Internal_GetLatestIsValid(Asset[] assets);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_GetLatest", ThrowsException = true)]
        private static extern Task Internal_GetLatest(Asset[] assets);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_SetFileMode", ThrowsException = true)]
        private static extern Task Internal_SetFileMode(Asset[] assets, FileMode fileMode);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_SetFileModeStrings")]
        private static extern Task Internal_SetFileModeStrings(string[] assets, FileMode fileMode);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_ChangeSetDescription")]
        private static extern Task Internal_ChangeSetDescription([NotNull] ChangeSet changeset);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_ChangeSetStatus")]
        private static extern Task Internal_ChangeSetStatus([NotNull] ChangeSet changeset);

        [FreeFunction("VersionControlBindings::VCProvider::ChangeSets")]
        public static extern Task ChangeSets();

        [FreeFunction("VersionControlBindings::VCProvider::Internal_ChangeSetMove", ThrowsException = true)]
        private static extern Task Internal_ChangeSetMove(Asset[] assets, [NotNull] ChangeSet target);

        [FreeFunction("VersionControlBindings::VCProvider::Incoming")]
        public static extern Task Incoming();

        [FreeFunction("VersionControlBindings::VCProvider::Internal_IncomingChangeSetAssets")]
        private static extern Task Internal_IncomingChangeSetAssets([NotNull] ChangeSet changeset);

        [FreeFunction("VersionControlBindings::VCProvider::UpdateSettings")]
        public static extern Task UpdateSettings();

        [FreeFunction("VersionControlBindings::VCProvider::GetAssetByPath")]
        public static extern Asset GetAssetByPath(string unityPath);

        [FreeFunction("VersionControlBindings::VCProvider::GetAssetByGUID")]
        public static extern Asset GetAssetByGUID(string guid);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_GetAssetArrayFromSelection")]
        private static extern Asset[] Internal_GetAssetArrayFromSelection();

        [FreeFunction("VersionControlBindings::VCProvider::IsOpenForEdit")]
        public static extern bool IsOpenForEdit([NotNull] Asset asset);

        [StaticAccessor("GetVCProvider()", StaticAccessorType.Dot)]
        internal static extern int GenerateID();

        [StaticAccessor("GetVCCache()", StaticAccessorType.Dot)]
        [NativeMethod("Clear")]
        public static extern void ClearCache();

        [StaticAccessor("GetVCCache()", StaticAccessorType.Dot)]
        [NativeMethod("Invalidate")]
        internal static extern void InvalidateCache();


        [FreeFunction("VersionControlBindings::VCProvider::Internal_CreateWarningTask")]
        public static extern Task Internal_WarningTask([NotNull] string message);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_CreateErrorTask")]
        public static extern Task Internal_ErrorTask([NotNull] string message);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_ConsolidateAssetList", ThrowsException = true)]
        private static extern Asset[] Internal_ConsolidateAssetList(Asset[] assets, CheckoutMode mode);

        [StaticAccessor("VCProvider", StaticAccessorType.DoubleColon)]
        [NativeMethod("ShouldAddMetaFile")]
        static internal extern bool PathHasMetaFile(string path);

        [StaticAccessor("VCProvider", StaticAccessorType.DoubleColon)]
        [NativeMethod("ShouldPathBeVersioned")]
        static internal extern bool PathIsVersioned(string path);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_WaitForRelatedTasks", ThrowsException = true)]
        internal static extern void WaitForRelatedTasks(Asset[] assets);

        internal static void WaitForRelatedTasks(string[] assets) => WaitForRelatedTasks(System.Array.ConvertAll(assets, a => new Asset(a)));

        internal static void WaitForRelatedTasks(Asset asset) => WaitForRelatedTasks(new[] { asset });

        internal static void WaitForRelatedTasks(string asset) => WaitForRelatedTasks(new[] { new Asset(asset) });
    }
}
