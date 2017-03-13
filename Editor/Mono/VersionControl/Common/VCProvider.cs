// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
namespace UnityEditor.VersionControl
{
    [System.Flags]
    public enum CheckoutMode { Asset = 1, Meta = 2, Both = 3, Exact = 4 };

    [System.Flags]
    public enum ResolveMethod { UseMine = 1, UseTheirs = 2, UseMerged };

    [System.Flags]
    public enum MergeMethod
    {
        MergeNone = 0,
        MergeAll = 1,
        [System.Obsolete("This member is no longer supported (UnityUpgradable) -> MergeNone", true)]
        MergeNonConflicting = 2
    };

    [System.Flags]
    public enum OnlineState { Updating = 0, Online = 1, Offline = 2 };

    [System.Flags]
    public enum RevertMode { Normal = 0, Unchanged = 1, KeepModifications = 2 };

    [System.Flags]
    public enum FileMode { None = 0, Binary = 1, Text = 2 };

    public partial class Provider
    {
        //*Undocumented
        static internal Asset CacheStatus(string assetPath)
        {
            return Internal_CacheStatus(assetPath);
        }

        static public Task Status(AssetList assets)
        {
            return Internal_Status(assets.ToArray(), true);
        }

        static public Task Status(Asset asset)
        {
            return Internal_Status(new Asset[] { asset }, true);
        }

        static public Task Status(AssetList assets, bool recursively)
        {
            return Internal_Status(assets.ToArray(), recursively);
        }

        static public Task Status(Asset asset, bool recursively)
        {
            return Internal_Status(new Asset[] { asset }, recursively);
        }

        static public Task Status(string[] assets)
        {
            return Internal_StatusStrings(assets, true);
        }

        static public Task Status(string[] assets, bool recursively)
        {
            return Internal_StatusStrings(assets, recursively);
        }

        static public Task Status(string asset)
        {
            return Internal_StatusStrings(new string[] { asset }, true);
        }

        static public Task Status(string asset, bool recursively)
        {
            return Internal_StatusStrings(new string[] { asset }, recursively);
        }

        static public Task Move(string from, string to)
        {
            return Internal_MoveAsStrings(from, to);
        }

        static public bool CheckoutIsValid(AssetList assets)
        {
            return CheckoutIsValid(assets, CheckoutMode.Exact);
        }

        static public bool CheckoutIsValid(AssetList assets, CheckoutMode mode)
        {
            return Internal_CheckoutIsValid(assets.ToArray(), mode);
        }

        static public Task Checkout(AssetList assets, CheckoutMode mode)
        {
            return Internal_Checkout(assets.ToArray(), mode);
        }

        static public Task Checkout(string[] assets, CheckoutMode mode)
        {
            return Internal_CheckoutStrings(assets, mode);
        }

        static public Task Checkout(UnityEngine.Object[] assets, CheckoutMode mode)
        {
            AssetList assetList = new AssetList();
            foreach (Object o in assets)
            {
                string path = AssetDatabase.GetAssetPath(o);
                Asset asset = GetAssetByPath(path);
                assetList.Add(asset);
            }

            return Internal_Checkout(assetList.ToArray(), mode);
        }

        static public bool CheckoutIsValid(Asset asset)
        {
            return CheckoutIsValid(asset, CheckoutMode.Exact);
        }

        static public bool CheckoutIsValid(Asset asset, CheckoutMode mode)
        {
            return Internal_CheckoutIsValid(new Asset[] { asset }, mode);
        }

        static public Task Checkout(Asset asset, CheckoutMode mode)
        {
            return Internal_Checkout(new Asset[] { asset }, mode);
        }

        static public Task Checkout(string asset, CheckoutMode mode)
        {
            return Internal_CheckoutStrings(new string[] { asset }, mode);
        }

        static public Task Checkout(UnityEngine.Object asset, CheckoutMode mode)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            Asset vcasset = GetAssetByPath(path);
            return Internal_Checkout(new Asset[] { vcasset }, mode);
        }

        //*Undocumented
        static internal bool PromptAndCheckoutIfNeeded(string[] assets, string promptIfCheckoutIsNeeded)
        {
            return Internal_PromptAndCheckoutIfNeeded(assets, promptIfCheckoutIsNeeded);
        }

        static public Task Delete(string assetProjectPath)
        {
            return Internal_DeleteAtProjectPath(assetProjectPath);
        }

        static public Task Delete(AssetList assets)
        {
            return Internal_Delete(assets.ToArray());
        }

        static public Task Delete(Asset asset)
        {
            return Internal_Delete(new Asset[] { asset });
        }

        static public bool AddIsValid(AssetList assets)
        {
            return Internal_AddIsValid(assets.ToArray());
        }

        static public Task Add(AssetList assets, bool recursive)
        {
            return Internal_Add(assets.ToArray(), recursive);
        }

        static public Task Add(Asset asset, bool recursive)
        {
            return Internal_Add(new Asset[] { asset }, recursive);
        }

        static public bool DeleteChangeSetsIsValid(ChangeSets changesets)
        {
            return Internal_DeleteChangeSetsIsValid(changesets.ToArray());
        }

        static public Task DeleteChangeSets(ChangeSets changesets)
        {
            return Internal_DeleteChangeSets(changesets.ToArray());
        }

        static internal Task RevertChangeSets(ChangeSets changesets, RevertMode mode)
        {
            return Internal_RevertChangeSets(changesets.ToArray(), mode);
        }

        static public bool SubmitIsValid(ChangeSet changeset, AssetList assets)
        {
            return Internal_SubmitIsValid(changeset, assets != null ? assets.ToArray() : null);
        }

        static public Task Submit(ChangeSet changeset, AssetList list, string description, bool saveOnly)
        {
            return Internal_Submit(changeset, list != null ? list.ToArray() : null, description, saveOnly);
        }

        static public bool DiffIsValid(AssetList assets)
        {
            Asset[] a = assets.ToArray();
            return Internal_DiffIsValid(a);
        }

        static public Task DiffHead(AssetList assets, bool includingMetaFiles)
        {
            return Internal_DiffHead(assets.ToArray(), includingMetaFiles);
        }

        static public bool ResolveIsValid(AssetList assets)
        {
            Asset[] a = assets.ToArray();
            return Internal_ResolveIsValid(a);
        }

        static public Task Resolve(AssetList assets, ResolveMethod resolveMethod)
        {
            return Internal_Resolve(assets.ToArray(), resolveMethod);
        }

        static public Task Merge(AssetList assets, MergeMethod method)
        {
            return Internal_Merge(assets.ToArray(), method);
        }

        static public bool LockIsValid(AssetList assets)
        {
            return Internal_LockIsValid(assets.ToArray());
        }

        static public bool LockIsValid(Asset asset)
        {
            return Internal_LockIsValid(new Asset[] { asset });
        }

        static public bool UnlockIsValid(AssetList assets)
        {
            return Internal_UnlockIsValid(assets.ToArray());
        }

        static public bool UnlockIsValid(Asset asset)
        {
            return Internal_UnlockIsValid(new Asset[] { asset });
        }

        static public Task Lock(AssetList assets, bool locked)
        {
            return Internal_Lock(assets.ToArray(), locked);
        }

        static public Task Lock(Asset asset, bool locked)
        {
            return Internal_Lock(new Asset[] { asset }, locked);
        }

        static public bool RevertIsValid(AssetList assets, RevertMode mode)
        {
            return Internal_RevertIsValid(assets.ToArray(), mode);
        }

        static public Task Revert(AssetList assets, RevertMode mode)
        {
            return Internal_Revert(assets.ToArray(), mode);
        }

        static public bool RevertIsValid(Asset asset, RevertMode mode)
        {
            return Internal_RevertIsValid(new Asset[] { asset }, mode);
        }

        static public Task Revert(Asset asset, RevertMode mode)
        {
            return Internal_Revert(new Asset[] { asset }, mode);
        }

        static public bool GetLatestIsValid(AssetList assets)
        {
            return Internal_GetLatestIsValid(assets.ToArray());
        }

        static public bool GetLatestIsValid(Asset asset)
        {
            return Internal_GetLatestIsValid(new Asset[] { asset });
        }

        static public Task GetLatest(AssetList assets)
        {
            return Internal_GetLatest(assets.ToArray());
        }

        static public Task GetLatest(Asset asset)
        {
            return Internal_GetLatest(new Asset[] { asset });
        }

        static internal Task SetFileMode(AssetList assets, FileMode mode)
        {
            return Internal_SetFileMode(assets.ToArray(), mode);
        }

        static internal Task SetFileMode(string[] assets, FileMode mode)
        {
            return Internal_SetFileModeStrings(assets, mode);
        }

        static public Task ChangeSetDescription(ChangeSet changeset)
        {
            return Internal_ChangeSetDescription(changeset);
        }

        static public Task ChangeSetStatus(ChangeSet changeset)
        {
            return Internal_ChangeSetStatus(changeset);
        }

        static public Task ChangeSetStatus(string changesetID)
        {
            ChangeSet cl = new ChangeSet("", changesetID);
            return Internal_ChangeSetStatus(cl);
        }

        static public Task IncomingChangeSetAssets(ChangeSet changeset)
        {
            return Internal_IncomingChangeSetAssets(changeset);
        }

        static public Task IncomingChangeSetAssets(string changesetID)
        {
            ChangeSet cl = new ChangeSet("", changesetID);
            return Internal_IncomingChangeSetAssets(cl);
        }

        static public Task ChangeSetMove(AssetList assets, ChangeSet changeset)
        {
            return Internal_ChangeSetMove(assets.ToArray(), changeset);
        }

        static public Task ChangeSetMove(Asset asset, ChangeSet changeset)
        {
            return Internal_ChangeSetMove(new Asset[] { asset }, changeset);
        }

        static public Task ChangeSetMove(AssetList assets, string changesetID)
        {
            ChangeSet cl = new ChangeSet("", changesetID);
            return Internal_ChangeSetMove(assets.ToArray(), cl);
        }

        static public Task ChangeSetMove(Asset asset, string changesetID)
        {
            ChangeSet cl = new ChangeSet("", changesetID);
            return Internal_ChangeSetMove(new Asset[] { asset }, cl);
        }

        static public AssetList GetAssetListFromSelection()
        {
            AssetList list = new AssetList();
            Asset[] assets = Internal_GetAssetArrayFromSelection();
            foreach (Asset asset in assets)
            {
                list.Add(asset);
            }

            return list;
        }
    }
}
