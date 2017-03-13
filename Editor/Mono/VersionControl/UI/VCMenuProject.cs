// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

using UnityEditor.VersionControl;

namespace UnityEditorInternal.VersionControl
{
    // Menu popup for the main unity project window.  Items are greyed out when not available to help with usability.
    public class ProjectContextMenu
    {
        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Get Latest" menu handler
        static bool GetLatestTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            return Provider.enabled && Provider.GetLatestIsValid(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Get Latest" menu handler
        static void GetLatest(MenuCommand cmd)
        {
            AssetList list = Provider.GetAssetListFromSelection();
            Provider.GetLatest(list).SetCompletionAction(CompletionAction.UpdatePendingWindow);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Submit..." menu handler
        static bool SubmitTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            return Provider.enabled && Provider.SubmitIsValid(null, selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Submit..." menu handler
        static void Submit(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            WindowChange.Open(selected, true);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Check Out" menu handler
        static bool CheckOutTest(MenuCommand cmd)
        {
            // TODO: Retrieve CheckoutMode from settings (depends on asset type; native vs. imported)
            AssetList selected = Provider.GetAssetListFromSelection();
            return Provider.enabled && Provider.CheckoutIsValid(selected, CheckoutMode.Both);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Check Out" menu handler
        static void CheckOut(MenuCommand cmd)
        {
            // TODO: Retrieve CheckoutMode from settings (depends on asset type; native vs. imported)
            AssetList list = Provider.GetAssetListFromSelection();
            Provider.Checkout(list, CheckoutMode.Both);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Check Out (Other)/Only asset file" menu handler
        static bool CheckOutAssetTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            return Provider.enabled && Provider.CheckoutIsValid(selected, CheckoutMode.Asset);
        }

        /// Called from native class VCSAssetMenuHandler as "Assets/Version Control/Check Out (Other)/Only asset file" menu handler
        static void CheckOutAsset(MenuCommand cmd)
        {
            AssetList list = Provider.GetAssetListFromSelection();
            Provider.Checkout(list, CheckoutMode.Asset);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Check Out (Other)/Only .meta file" menu handler
        static bool CheckOutMetaTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            return Provider.enabled && Provider.CheckoutIsValid(selected, CheckoutMode.Meta);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Check Out (Other)/Only .meta file" menu handler
        static void CheckOutMeta(MenuCommand cmd)
        {
            AssetList list = Provider.GetAssetListFromSelection();
            Provider.Checkout(list, CheckoutMode.Meta);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Check Out (Other)/Both" menu handler
        static bool CheckOutBothTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            return Provider.enabled && Provider.CheckoutIsValid(selected, CheckoutMode.Both);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Check Out (Other)/Both" menu handler
        static void CheckOutBoth(MenuCommand cmd)
        {
            AssetList list = Provider.GetAssetListFromSelection();
            Provider.Checkout(list, CheckoutMode.Both);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Mark Add" menu handler
        static bool MarkAddTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            return Provider.enabled && Provider.AddIsValid(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Mark Add" menu handler
        static void MarkAdd(MenuCommand cmd)
        {
            AssetList list = Provider.GetAssetListFromSelection();
            Provider.Add(list, true).SetCompletionAction(CompletionAction.UpdatePendingWindow);
        }

        /// Called from native class VCSAssetMenuHandler as "Assets/Version Control/Revert..." menu handler
        static bool RevertTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            return Provider.enabled && Provider.RevertIsValid(selected, RevertMode.Normal);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Revert..." menu handler
        static void Revert(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            WindowRevert.Open(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Revert Unchanged" menu handler
        static bool RevertUnchangedTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            return Provider.enabled && Provider.RevertIsValid(selected, RevertMode.Normal);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Revert Unchanged" menu handler
        static void RevertUnchanged(MenuCommand cmd)
        {
            AssetList list = Provider.GetAssetListFromSelection();
            Provider.Revert(list, RevertMode.Unchanged).SetCompletionAction(CompletionAction.UpdatePendingWindow);
            Provider.Status(list);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Diff Against Head..." menu handler
        static bool ResolveTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            return Provider.enabled && Provider.ResolveIsValid(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Diff Against Head..." menu handler
        static void Resolve(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            WindowResolve.Open(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Lock" menu handler
        static bool LockTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            return Provider.enabled && Provider.LockIsValid(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Lock" menu handler
        static void Lock(MenuCommand cmd)
        {
            AssetList list = Provider.GetAssetListFromSelection();
            Provider.Lock(list, true).SetCompletionAction(CompletionAction.UpdatePendingWindow);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Unlock" menu handler
        static bool UnlockTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            return Provider.enabled && Provider.UnlockIsValid(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Unlock" menu handler
        static void Unlock(MenuCommand cmd)
        {
            AssetList list = Provider.GetAssetListFromSelection();
            Provider.Lock(list, false).SetCompletionAction(CompletionAction.UpdatePendingWindow);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Diff/Against Head..." menu handler
        static bool DiffHeadTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            return Provider.enabled && Provider.DiffIsValid(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Diff/Against Head..." menu handler
        static void DiffHead(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            Provider.DiffHead(selected, false);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Diff/Against Head with .meta..." menu handler
        static bool DiffHeadWithMetaTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            return Provider.enabled && Provider.DiffIsValid(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Diff/Against Head with .meta..." menu handler
        static void DiffHeadWithMeta(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            Provider.DiffHead(selected, true);
        }
    }
}
