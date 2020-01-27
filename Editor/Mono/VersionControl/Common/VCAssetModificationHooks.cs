// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

using UnityEditor.VersionControl;

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
                    // Fetch status
                    Task statusTask = Provider.Status(fromPath, false);
                    statusTask.Wait();
                    if (statusTask.success)
                        asset = Provider.CacheStatus(fromPath);
                    else
                        asset = null;
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
            return task.assetList.Count > 0 ? task.assetList[0] : null;
        }

        static AssetList GetStatusForceUpdate(List<string> fromPaths)
        {
            var task = Provider.Status(fromPaths.ToArray());
            task.Wait();
            return task.success ? task.assetList : null;
        }

        // Handle asset moving
        public static AssetMoveResult OnWillMoveAsset(string from, string to)
        {
            if (!Provider.enabled)
                return AssetMoveResult.DidNotMove;

            Asset asset = GetStatusCachedIfPossible(from, true);

            if (asset == null || !asset.IsUnderVersionControl)
                return AssetMoveResult.DidNotMove;

            Asset.States assetState = asset.state;
            if (Asset.IsState(assetState, Asset.States.OutOfSync))
            {
                Debug.LogError("Cannot move version controlled file that is not up to date. Please get latest changes from server");
                return AssetMoveResult.FailedMove;
            }
            else if (Asset.IsState(assetState, Asset.States.DeletedRemote))
            {
                Debug.LogError("Cannot move version controlled file that is deleted on server. Please get latest changes from server");
                return AssetMoveResult.FailedMove;
            }
            else if (Asset.IsState(assetState, Asset.States.CheckedOutRemote))
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

        // Handle asset deletion
        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            if (!Provider.enabled)
                return AssetDeleteResult.DidNotDelete;

            Task task = Provider.Delete(assetPath);
            task.SetCompletionAction(CompletionAction.UpdatePendingWindow);
            task.Wait();

            // Using DidNotDelete on success to force unity to clean up local files
            return task.success ? AssetDeleteResult.DidNotDelete : AssetDeleteResult.FailedDelete;
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
