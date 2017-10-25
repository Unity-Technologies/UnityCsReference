// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
    [NativeHeader("Editor/Src/VersionControl/VCProvider_bindings.h")]
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

        [FreeFunction("VersionControlBindings::VCProvider::Internal_CacheStatus")]
        private static extern Asset Internal_CacheStatus(string assetPath);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_Status")]
        private static extern Task Internal_Status(Asset[] assets, bool recursively);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_StatusStrings")]
        private static extern Task Internal_StatusStrings(string[] assetsProjectPaths, bool recursively);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_StatusAbsolutePath")]
        private static extern Task Internal_StatusAbsolutePath(string assetPath);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_CheckoutIsValid")]
        private static extern bool Internal_CheckoutIsValid(Asset[] assets, CheckoutMode mode);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_Checkout")]
        private static extern Task Internal_Checkout(Asset[] assets, CheckoutMode mode);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_CheckoutStrings")]
        private static extern Task Internal_CheckoutStrings(string[] assets, CheckoutMode mode);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_PromptAndCheckoutIfNeeded")]
        private static extern bool Internal_PromptAndCheckoutIfNeeded(string[] assets, string promptIfCheckoutIsNeeded);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_Delete")]
        private static extern Task Internal_Delete(Asset[] assets);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_DeleteAtProjectPath")]
        private static extern Task Internal_DeleteAtProjectPath([NotNull] string assetProjectPath);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_MoveAsStrings")]
        private static extern Task Internal_MoveAsStrings([NotNull] string from, [NotNull] string to);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_AddIsValid")]
        private static extern bool Internal_AddIsValid(Asset[] assets);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_Add")]
        private static extern Task Internal_Add(Asset[] assets, bool recursive);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_DeleteChangeSetsIsValid")]
        private static extern bool Internal_DeleteChangeSetsIsValid(ChangeSet[] changesets);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_DeleteChangeSets")]
        private static extern Task Internal_DeleteChangeSets(ChangeSet[] changesets);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_RevertChangeSets")]
        private static extern Task Internal_RevertChangeSets(ChangeSet[] changesets, RevertMode mode);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_SubmitIsValid")]
        private static extern bool Internal_SubmitIsValid(ChangeSet changeset, Asset[] assets);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_Submit")]
        private static extern Task Internal_Submit(ChangeSet changeset, Asset[] assets, string description, bool saveOnly);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_DiffIsValid")]
        private static extern bool Internal_DiffIsValid(Asset[] assets);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_DiffHead")]
        private static extern Task Internal_DiffHead(Asset[] assets, bool includingMetaFiles);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_ResolveIsValid")]
        private static extern bool Internal_ResolveIsValid(Asset[] assets);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_Resolve")]
        private static extern Task Internal_Resolve(Asset[] assets, ResolveMethod resolveMethod);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_Merge")]
        private static extern Task Internal_Merge(Asset[] assets, MergeMethod mergeMethod);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_LockIsValid")]
        private static extern bool Internal_LockIsValid(Asset[] assets);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_UnlockIsValid")]
        private static extern bool Internal_UnlockIsValid(Asset[] assets);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_Lock")]
        private static extern Task Internal_Lock(Asset[] assets, bool locked);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_RevertIsValid")]
        private static extern bool Internal_RevertIsValid(Asset[] assets, RevertMode revertMode);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_Revert")]
        private static extern Task Internal_Revert(Asset[] assets, RevertMode revertMode);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_GetLatestIsValid")]
        private static extern bool Internal_GetLatestIsValid(Asset[] assets);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_GetLatest")]
        private static extern Task Internal_GetLatest(Asset[] assets);

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_SetFileMode")]
        private static extern Task Internal_SetFileMode(Asset[] assets, FileMode fileMode);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_SetFileModeStrings")]
        private static extern Task Internal_SetFileModeStrings(string[] assets, FileMode fileMode);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_ChangeSetDescription")]
        private static extern Task Internal_ChangeSetDescription([NotNull] ChangeSet changeset);

        [FreeFunction("VersionControlBindings::VCProvider::Internal_ChangeSetStatus")]
        private static extern Task Internal_ChangeSetStatus([NotNull] ChangeSet changeset);

        [FreeFunction("VersionControlBindings::VCProvider::ChangeSets")]
        public static extern Task ChangeSets();

        [NativeThrows]
        [FreeFunction("VersionControlBindings::VCProvider::Internal_ChangeSetMove")]
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

    }
}
