// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

using UnityEditor.VersionControl;

namespace UnityEditorInternal.VersionControl
{
    // Monitor the behavior of assets.  This is where general unity asset operations are handled
    public class AssetModificationHook
    {
        private static Asset GetStatusCachedIfPossible(string from)
        {
            Asset asset = Provider.CacheStatus(from);
            if (asset == null || asset.IsState(Asset.States.Updating))
            {
                // Fetch status
                Task statusTask = Provider.Status(from, false);
                statusTask.Wait();
                asset = Provider.CacheStatus(from);
            }
            return asset;
        }

        private static Asset GetStatusForceUpdate(string from)
        {
            Task statusTask = Provider.Status(from);
            statusTask.Wait();
            return statusTask.assetList.Count > 0 ? statusTask.assetList[0] : null;
        }

        // Handle asset moving
        public static AssetMoveResult OnWillMoveAsset(string from, string to)
        {
            if (!Provider.enabled)
                return AssetMoveResult.DidNotMove;

            Asset asset = GetStatusCachedIfPossible(from);

            if (asset == null || !asset.IsUnderVersionControl)
                return AssetMoveResult.DidNotMove;

            if (asset.IsState(Asset.States.OutOfSync))
            {
                Debug.LogError("Cannot move version controlled file that is not up to date. Please get latest changes from server");
                return AssetMoveResult.FailedMove;
            }
            else if (asset.IsState(Asset.States.DeletedRemote))
            {
                Debug.LogError("Cannot move version controlled file that is deleted on server. Please get latest changes from server");
                return AssetMoveResult.FailedMove;
            }
            else if (asset.IsState(Asset.States.CheckedOutRemote))
            {
                Debug.LogError("Cannot move version controlled file that is checked out on server. Please get latest changes from server");
                return AssetMoveResult.FailedMove;
            }
            else if (asset.IsState(Asset.States.LockedRemote))
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

            if (!Provider.enabled)
                return true;

            if (string.IsNullOrEmpty(assetPath))
                return true;

            Asset asset = statusOptions == StatusQueryOptions.UseCachedIfPossible ? GetStatusCachedIfPossible(assetPath) : GetStatusForceUpdate(assetPath);

            if (asset == null)
            {
                return false;
            }

            return Provider.IsOpenForEdit(asset);
        }
    }
}
