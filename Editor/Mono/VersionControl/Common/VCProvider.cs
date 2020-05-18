// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Internal;

namespace UnityEditor.VersionControl
{
    [System.Flags]
    public enum CheckoutMode { Asset = 1, Meta = 2, Both = 3, Exact = 4 }

    [System.Flags]
    public enum ResolveMethod { UseMine = 1, UseTheirs = 2, UseMerged }

    [System.Flags]
    [System.Obsolete("MergeMethod is no longer used.")]
    public enum MergeMethod
    {
        MergeNone = 0,
        MergeAll = 1,
        [System.Obsolete("This member is no longer supported (UnityUpgradable) -> MergeNone", true)]
        MergeNonConflicting = 2
    }

    [System.Flags]
    public enum OnlineState { Updating = 0, Online = 1, Offline = 2 }

    [System.Flags]
    public enum RevertMode { Normal = 0, Unchanged = 1, KeepModifications = 2 }

    [System.Flags]
    public enum FileMode { None = 0, Binary = 1, Text = 2 }

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

        //Returns null except where there is an error
        static private Task CheckAndCreateUserSuppliedChangeSet(string changesetID, string description, ref ChangeSet changeset)
        {
            //Options here:
            // ID is default and description is null - use default
            // ID is not default and description is null - use an existing changeset if changesets are supported
            // ID is default but description is not null - create a new changeset if changesets are supported
            // ID is not default and description is not null - This *should* rename an existing changeset to be consistent, but the current plugin doesn't implement this. Throw back an error
            if (string.IsNullOrEmpty(description))
            {
                if (changesetID == ChangeSet.defaultID)
                    //Checkout to default changeset
                    changeset = null;
                else
                {
                    //Check that the changeset exists
                    var task = VerifyChangesetID(changesetID);
                    if (task != null)
                        return task;
                    changeset = new ChangeSet(description, changesetID);
                }
            }
            else
            {
                if (changesetID == ChangeSet.defaultID)
                {
                    //Description is not null but no changeset ID set - create a new changeset unless the VCS provider does not support changesets
                    if (hasChangelistSupport == false)
                        return Internal_ErrorTask(
                            "User-created pre-checkout callback has set a changeset description but the VCS provider does not support changesets");

                    var createChangeSetTask = Internal_Submit(null, null, description, true);
                    createChangeSetTask.Wait();
                    if (createChangeSetTask.success == false)
                        return createChangeSetTask;

                    var changesetsTask = ChangeSets();
                    changesetsTask.Wait();
                    if (changesetsTask.success == false)
                        return changesetsTask;

                    changeset = new ChangeSet();
                    foreach (var queriedChangeset in changesetsTask.changeSets)
                    {
                        //Assuming here that changeset IDs are incremental
                        if (System.Convert.ToInt64(queriedChangeset.id) > System.Convert.ToInt64(changeset.id))
                            changeset = new ChangeSet(description, queriedChangeset.id);
                    }
                }
                else
                    //Description and changeset ID are set - this should rename but this is not currently supported
                    return Internal_ErrorTask("User-created pre-checkout callback has set both a changeset ID and a changeset Description. This is not currently supported.");
            }
            return null;
        }

        static internal AssetList ConsolidateAssetList(AssetList assets, CheckoutMode mode)
        {
            var consolidatedAssetArray = Internal_ConsolidateAssetList(assets.ToArray(), mode);
            var consolidatedAssetList = new AssetList();
            consolidatedAssetList.AddRange(consolidatedAssetArray);
            return consolidatedAssetList;
        }

        static private Task CheckCallbackAndCheckout(AssetList assets, CheckoutMode mode, ChangeSet changeset)
        {
            var consolidatedAssetList = assets;
            if (preCheckoutCallback != null)
            {
                consolidatedAssetList = ConsolidateAssetList(assets, mode);
                mode = CheckoutMode.Exact;
                var changesetID = changeset == null ? ChangeSet.defaultID : changeset.id;
                string changesetDescription = null;
                try
                {
                    if (preCheckoutCallback(consolidatedAssetList, ref changesetID, ref changesetDescription) == false)
                        return Internal_WarningTask("User-created pre-checkout callback has blocked this checkout.");
                }
                catch (System.Exception ex)
                {
                    return Internal_ErrorTask("User-created pre-checkout callback has raised an exception and this checkout will be blocked. Exception Message: " + ex.Message);
                }

                var changesetTask = CheckAndCreateUserSuppliedChangeSet(changesetID, changesetDescription, ref changeset);
                if (changesetTask != null)
                    return changesetTask;
            }
            return Internal_Checkout(consolidatedAssetList.ToArray(), mode, changeset);
        }

        static public Task Checkout(AssetList assets, CheckoutMode mode)
        {
            return Checkout(assets, mode, null);
        }

        static public Task Checkout(AssetList assets, CheckoutMode mode, ChangeSet changeset)
        {
            return CheckCallbackAndCheckout(assets, mode, changeset);
        }

        static public Task Checkout(string[] assets, CheckoutMode mode)
        {
            return Checkout(assets, mode, null);
        }

        static public Task Checkout(string[] assets, CheckoutMode mode, ChangeSet changeset)
        {
            var assetList = new AssetList();
            foreach (var path in assets)
            {
                var asset = GetAssetByPath(path);
                if (asset == null)
                {
                    asset = new Asset(path);
                }
            }

            return CheckCallbackAndCheckout(assetList, mode, changeset);
        }

        static public Task Checkout(Object[] assets, CheckoutMode mode)
        {
            return Checkout(assets, mode, null);
        }

        static public Task Checkout(Object[] assets, CheckoutMode mode, ChangeSet changeset)
        {
            var assetList = new AssetList();
            foreach (var o in assets)
            {
                assetList.Add(GetAssetByPath(AssetDatabase.GetAssetPath(o)));
            }

            return CheckCallbackAndCheckout(assetList, mode, changeset);
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
            return Checkout(asset, mode, null);
        }

        static public Task Checkout(Asset asset, CheckoutMode mode, ChangeSet changeset)
        {
            var assetList = new AssetList();
            assetList.Add(asset);

            return CheckCallbackAndCheckout(assetList, mode, changeset);
        }

        static public Task Checkout(string asset, CheckoutMode mode)
        {
            return Checkout(asset, mode, null);
        }

        static public Task Checkout(string asset, CheckoutMode mode, ChangeSet changeset)
        {
            return Checkout(new string[] { asset }, mode, changeset);
        }

        static public Task Checkout(UnityEngine.Object asset, CheckoutMode mode)
        {
            return Checkout(asset, mode, null);
        }

        static public Task Checkout(UnityEngine.Object asset, CheckoutMode mode, ChangeSet changeset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            var vcasset = GetAssetByPath(path);

            var assetList = new AssetList();
            assetList.Add(vcasset);

            return CheckCallbackAndCheckout(assetList, mode, changeset);
        }

        internal static bool HandlePreCheckoutCallback(ref string[] paths, ref ChangeSet changeSet)
        {
            if (preCheckoutCallback == null)
                return true;

            var assetList = new AssetList();
            assetList.AddRange(paths.Select(GetAssetByPath));

            try
            {
                var id = changeSet == null ? ChangeSet.defaultID : changeSet.id;
                string desc = null;
                if (preCheckoutCallback(assetList, ref id, ref desc))
                {
                    var task = CheckAndCreateUserSuppliedChangeSet(id, desc, ref changeSet);
                    if (task != null)
                    {
                        Debug.LogError(
                            "Tried to create/rename remote ChangeSet to match the one specified in user-supplied callback but failed with error code: " +
                            task.resultCode);
                        return false;
                    }

                    paths = assetList.Select(asset => asset.path).ToArray();
                }
                else
                {
                    Debug.LogWarning("User-created pre-checkout callback has blocked this checkout.");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("User-created pre-checkout callback has thrown an exception: " + ex.Message);
                return false;
            }

            return true;
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

        private static Task VerifyChangesetID(string changesetID)
        {
            var changesetsTask = ChangeSets();
            changesetsTask.Wait();
            if (changesetsTask.success == false)
            {
                Debug.LogError(string.Format("Tried to validate user-supplied changeset ID {0} but Provider.ChangeSets task failed. See returned Task for details.", changesetID));
                return changesetsTask;
            }
            var idIsValid = false;
            foreach (var changesetToCheck in changesetsTask.changeSets)
            {
                if (changesetToCheck.id == changesetID)
                {
                    idIsValid = true;
                    break;
                }
            }
            if (idIsValid == false)
                return Internal_ErrorTask(string.Format("The supplied changeset ID '{0}' did not match any known outgoing changesets. Aborting Task.", changesetID));

            return null;
        }

        static public Task Submit(ChangeSet changeset, AssetList list, string description, bool saveOnly)
        {
            if (preSubmitCallback != null)
            {
                var changesetID = changeset == null ? ChangeSet.defaultID : changeset.id;
                try
                {
                    if (preSubmitCallback(list, ref changesetID, ref description) == false)
                        return Internal_WarningTask("User-created pre-submit callback has blocked this changeset submission.");
                }
                catch (System.Exception ex)
                {
                    return Internal_ErrorTask("User-created pre-submit callback has raised an exception and this submission will be blocked. Exception Message: " + ex.Message);
                }

                if (changesetID == ChangeSet.defaultID)
                    changeset = null;
                else
                {
                    //Check that the changeset exists
                    var task = VerifyChangesetID(changesetID);
                    if (task != null)
                        return task;
                    changeset = new ChangeSet(description, changesetID);
                }
            }

            return Internal_Submit(changeset, list != null ? list.ToArray() : null, description, saveOnly);
        }

        static public bool DiffIsValid(AssetList assets)
        {
            return Internal_DiffIsValid(assets.ToArray());
        }

        static public Task DiffHead(AssetList assets, bool includingMetaFiles)
        {
            return Internal_DiffHead(assets.ToArray(), includingMetaFiles);
        }

        static public bool ResolveIsValid(AssetList assets)
        {
            return Internal_ResolveIsValid(assets.ToArray());
        }

        static public Task Resolve(AssetList assets, ResolveMethod resolveMethod)
        {
            return Internal_Resolve(assets.ToArray(), resolveMethod);
        }

        static public Task Merge(AssetList assets)
        {
            return Internal_Merge(assets.ToArray());
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [ExcludeFromDocs]
        [System.Obsolete("MergeMethod is no longer used.")]
        static public Task Merge(AssetList assets, MergeMethod method)
        {
            return Internal_Merge(assets.ToArray());
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
            var list = new AssetList();
            foreach (var asset in Internal_GetAssetArrayFromSelection())
            {
                list.Add(asset);
            }

            return list;
        }

        static internal bool NeedToCheckOutBoth(Object asset)
        {
            return AssetDatabase.IsNativeAsset(asset)
                || asset is SceneAsset
                || asset is AssemblyDefinitionAsset
                || asset is GameObject;
        }

        static internal AssetList GetInspectorAssets(Object[] assets)
        {
            AssetList inspectorAssets = new AssetList();

            foreach (var asset in assets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);

                if (NeedToCheckOutBoth(asset))
                {
                    Asset actualAsset = CacheStatus(assetPath);
                    if (actualAsset != null) inspectorAssets.Add(actualAsset);
                }

                Asset metaAsset = CacheStatus(AssetDatabase.GetTextMetaFilePathFromAssetPath(assetPath));
                if (metaAsset != null) inspectorAssets.Add(metaAsset);
            }

            return inspectorAssets;
        }

        public delegate bool PreSubmitCallback(AssetList list, ref string changesetID, ref string changesetDescription);
        static public PreSubmitCallback preSubmitCallback;
        public delegate bool PreCheckoutCallback(AssetList list, ref string changesetID, ref string changesetDescription);
        static public PreCheckoutCallback preCheckoutCallback;
    }
}
