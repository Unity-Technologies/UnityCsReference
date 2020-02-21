// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

using UnityEditor.VersionControl;
using UnityEngine.Assertions;

namespace UnityEditorInternal.VersionControl
{
    // Monitor the behavior of assets.  This is where general unity asset operations are handled
    public class AssetModificationHook
    {
        static Asset GetStatusCachedIfPossible(string fromPath, bool synchronous)
        {
            Asset asset = Provider.CacheStatus(fromPath);
            if (asset == null || asset.IsState(Asset.States.Updating))
            {
                if (synchronous)
                {
                    UnityEngine.Profiling.Profiler.BeginSample("VersionControl.GetCachedStatus");
                    // Fetch status
                    Task statusTask = Provider.Status(fromPath, false);
                    statusTask.Wait();
                    if (statusTask.success)
                        asset = Provider.CacheStatus(fromPath);
                    else
                        asset = null;
                    UnityEngine.Profiling.Profiler.EndSample();
                }
            }
            return asset;
        }

        static AssetList GetStatusCachedIfPossible(List<string> fromPaths, bool synchronous)
        {
            var assets = new AssetList {Capacity = fromPaths.Count};
            foreach (var path in fromPaths)
                assets.Add(GetStatusCachedIfPossible(path, synchronous));
            return assets;
        }

        static Asset GetStatusForceUpdate(string fromPath)
        {
            var task = Provider.Status(fromPath);
            task.Wait();
            var taskResultList = task.assetList;
            return taskResultList.Count > 0 ? taskResultList[0] : null;
        }

        static AssetList GetStatusForceUpdate(List<string> fromPaths)
        {
            var task = Provider.Status(fromPaths.ToArray());
            task.Wait();
            if (!task.success)
                return null;

            // Status task might return more items in the list (e.g. meta files),
            // and return them out of order too. Make sure to return proper sized
            // and in-order list back.
            var taskResultList = task.assetList;
            var result = new AssetList {Capacity = fromPaths.Count};
            result.AddRange(fromPaths.Select(path => taskResultList.SingleOrDefault(a => a.path == path)));
            return result;
        }

        // Handle asset moving
        public static AssetMoveResult OnWillMoveAsset(string from, string to)
        {
            if (!Provider.enabled || EditorUserSettings.WorkOffline)
                return AssetMoveResult.DidNotMove;

            if (!Provider.PathIsVersioned(from))
                return AssetMoveResult.DidNotMove;
            if (!Provider.PathIsVersioned(to))
                return AssetMoveResult.DidNotMove;

            if (InternalEditorUtility.isHumanControllingUs && Directory.Exists(from) && !EditorUtility.DisplayDialog("Confirm version control operation", L10n.Tr($"You are about to move or rename a folder that is under version control.\n\nFrom:\t{from}\nTo:\t{to}\n\nAre you sure you want to perform this action?"), "Yes", "No"))
            {
                return AssetMoveResult.FailedMove;
            }

            Asset asset = GetStatusCachedIfPossible(from, true);

            if (asset == null || !asset.IsUnderVersionControl)
                return AssetMoveResult.DidNotMove;

            Asset.States assetState = asset.state;

            Asset metaAsset = Provider.GetAssetByPath(asset.metaPath);
            Asset.States metaState = metaAsset.state;

            Asset.States states = Asset.States.OutOfSync | Asset.States.DeletedRemote | Asset.States.CheckedOutRemote;

            bool userAllowedMove = false;
            if (Asset.IsState(assetState, states) || Asset.IsState(metaState, states))
                userAllowedMove = AllowUserOverrideMovingUnsyncedFiles(asset, metaAsset);

            if (Asset.IsState(assetState, Asset.States.OutOfSync) && !userAllowedMove)
            {
                Debug.LogError("Cannot move version controlled file that is not up to date. Please get latest changes from server");
                return AssetMoveResult.FailedMove;
            }
            else if (Asset.IsState(assetState, Asset.States.DeletedRemote) && !userAllowedMove)
            {
                Debug.LogError("Cannot move version controlled file that is deleted on server. Please get latest changes from server");
                return AssetMoveResult.FailedMove;
            }
            else if (Asset.IsState(assetState, Asset.States.CheckedOutRemote) && !userAllowedMove)
            {
                Debug.LogError("Cannot move version controlled file that is checked out on server. Please get latest changes from server");
                return AssetMoveResult.FailedMove;
            }
            else if (Asset.IsState(assetState, Asset.States.LockedRemote))
            {
                Debug.LogError("Cannot move version controlled file that is locked on server. Please get latest changes from server");
                return AssetMoveResult.FailedMove;
            }

            // Perform the actual move
            Task task = Provider.Move(from, to);
            task.Wait();

            return task.success ? (AssetMoveResult)task.resultCode : AssetMoveResult.FailedMove;
        }

        static void AppendStateToString(ref string states, string append)
        {
            states = states.Replace(" and ", ", ");
            if (states.Length > 0)
                states = states + " and " + append;
            else
                states = append;
        }

        static bool AllowUserOverrideMovingUnsyncedFiles(Asset asset, Asset metaAsset)
        {
            if (Application.isBatchMode)
                return false;

            string state = "";
            if (asset.IsState(Asset.States.OutOfSync) | metaAsset.IsState(Asset.States.OutOfSync))
                AppendStateToString(ref state, "Out of Sync");
            if (asset.IsState(Asset.States.DeletedRemote) | metaAsset.IsState(Asset.States.DeletedRemote))
                AppendStateToString(ref state, "Deleted remotely");
            if (asset.IsState(Asset.States.CheckedOutRemote) | metaAsset.IsState(Asset.States.CheckedOutRemote))
                AppendStateToString(ref state, "Checked out remotely");

            string title = "Confirm move";
            string message = "The files you are trying to move or rename are " + state + ". This may cause synchronization issues, would you like to proceed anyway?";

            if (EditorUtility.DisplayDialog(title, message, "OK", "Cancel"))
                return true;
            else
                return false;
        }

        // Handle asset deletion
        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            if (!Provider.enabled || EditorUserSettings.WorkOffline)
                return AssetDeleteResult.DidNotDelete;

            if (!Provider.PathIsVersioned(assetPath))
                return AssetDeleteResult.DidNotDelete;

            Task task = Provider.Delete(assetPath);
            task.SetCompletionAction(CompletionAction.UpdatePendingWindow);
            task.Wait();

            if (task.success)
            {
                // We need to check if the provider deleted the file itself or not.
                return File.Exists(assetPath) ? AssetDeleteResult.DidNotDelete : AssetDeleteResult.DidDelete;
            }
            return AssetDeleteResult.FailedDelete;
        }

        //NOTE: this now assumes that version control is on and we are not working offline. Also all paths are expected to be versioned
        public static void OnWillDeleteAssets(string[] assetPaths, AssetDeleteResult[] deletionResults, RemoveAssetOptions option)
        {
            Assert.IsTrue(deletionResults.Length == assetPaths.Length);

            //NOTE: we only submit assets for deletion in batches because PlasticSCM will time out the
            // connection to the provider process with too many assets
            int deletionBatchSize = 1000;
            for (int batchStart = 0; batchStart < assetPaths.Length; batchStart += deletionBatchSize)
            {
                var deleteAssetList = new AssetList();
                for (int i = batchStart; i < batchStart + deletionBatchSize && i < assetPaths.Length; i++)
                    deleteAssetList.Add(Provider.GetAssetByPath(assetPaths[i]));

                Task task = Provider.Delete(deleteAssetList);

                task.SetCompletionAction(CompletionAction.UpdatePendingWindow);
                task.Wait();

                if (task.success)
                {
                    for (int i = batchStart; i < batchStart + deleteAssetList.Count(); i++)
                        deletionResults[i] = File.Exists(assetPaths[i]) ? AssetDeleteResult.DidNotDelete : AssetDeleteResult.DidDelete;
                }
                else
                {
                    //NOTE: we most likely don't know which assets failed to actually be deleted
                    for (int i = batchStart; i < batchStart + deleteAssetList.Count(); i++)
                        deletionResults[i] = AssetDeleteResult.FailedDelete;
                    ;
                }
            }
        }

        public static bool IsOpenForEdit(string assetPath, out string message, StatusQueryOptions statusOptions)
        {
            message = "";

            if (!Provider.enabled || EditorUserSettings.WorkOffline)
                return true;

            if (string.IsNullOrEmpty(assetPath))
                return true;

            Asset asset;
            if (statusOptions == StatusQueryOptions.UseCachedIfPossible || statusOptions == StatusQueryOptions.UseCachedAsync)
                asset = GetStatusCachedIfPossible(assetPath, statusOptions == StatusQueryOptions.UseCachedIfPossible);
            else
                asset = GetStatusForceUpdate(assetPath);

            if (asset == null)
            {
                if (Provider.onlineState == OnlineState.Offline && Provider.offlineReason != string.Empty)
                    message = Provider.offlineReason;
                return false;
            }

            return Provider.IsOpenForEdit(asset);
        }

        internal static void IsOpenForEdit(List<string> assetPaths, List<string> outNotOpenPaths, StatusQueryOptions statusOptions)
        {
            if (!Provider.enabled || EditorUserSettings.WorkOffline || assetPaths == null || assetPaths.Count == 0)
                return; // everything is editable

            // paths that are empty/null are considered to be editable, so remove them from consideration
            assetPaths = assetPaths.Where(p => !string.IsNullOrEmpty(p)).ToList();

            AssetList assets;
            if (statusOptions == StatusQueryOptions.UseCachedIfPossible || statusOptions == StatusQueryOptions.UseCachedAsync)
                assets = GetStatusCachedIfPossible(assetPaths, statusOptions == StatusQueryOptions.UseCachedIfPossible);
            else
                assets = GetStatusForceUpdate(assetPaths);

            if (assets == null)
            {
                // nothing is editable (we might be disconnected)
                outNotOpenPaths.AddRange(assetPaths);
                return;
            }

            for (var i = 0; i < assetPaths.Count; ++i)
            {
                var asset = assets[i];
                if (asset == null || !Provider.IsOpenForEdit(asset))
                    outNotOpenPaths.Add(assetPaths[i]);
            }
        }
    }
}
