// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.VersionControl;

namespace UnityEditorInternal.VersionControl
{
    // Top-level VCS menu; set up and invoked from native code when VCS integration is turned on
    class ProjectContextMenu
    {
        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Get Latest" menu handler
        static bool GetLatestTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            return Provider.enabled && Provider.GetLatestIsValid(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Get Latest" menu handler
        static void GetLatest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            Provider.GetLatest(selected).SetCompletionAction(CompletionAction.UpdatePendingWindow);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Submit..." menu handler
        static bool SubmitTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            return Provider.enabled && Provider.SubmitIsValid(null, selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Submit..." menu handler
        static void Submit(MenuCommand cmd)
        {
            InspectorWindow.ApplyChanges();
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            WindowChange.Open(selected, true);
        }

        [Shortcut("Version Control/Submit Changeset")]
        static void Submit(ShortcutArguments args)
        {
            Submit(null);
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
            AssetList selected = Provider.GetAssetListFromSelection();
            Provider.Checkout(selected, CheckoutMode.Both);
        }

        [Shortcut("Version Control/Check Out")]
        static void CheckOut(ShortcutArguments args)
        {
            CheckOut(null);
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

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Mark Add" menu handler
        static bool MarkAddTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            return Provider.enabled && Provider.AddIsValid(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Mark Add" menu handler
        static void MarkAdd(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            Provider.Add(selected, true).SetCompletionAction(CompletionAction.UpdatePendingWindow);
        }

        [Shortcut("Version Control/Mark Add")]
        static void MarkAdd(ShortcutArguments args)
        {
            MarkAdd(null);
        }

        /// Called from native class VCSAssetMenuHandler as "Assets/Version Control/Revert..." menu handler
        static bool RevertTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            return Provider.enabled && Provider.RevertIsValid(selected, RevertMode.Normal);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Revert..." menu handler
        static void Revert(MenuCommand cmd)
        {
            InspectorWindow.ApplyChanges();
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            WindowRevert.Open(selected);
        }

        [Shortcut("Version Control/Revert...")]
        static void Revert(ShortcutArguments args)
        {
            Revert(null);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Revert Unchanged" menu handler
        static bool RevertUnchangedTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            return Provider.enabled && Provider.RevertIsValid(selected, RevertMode.Unchanged);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Revert Unchanged" menu handler
        static void RevertUnchanged(MenuCommand cmd)
        {
            InspectorWindow.ApplyChanges();
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            Provider.Revert(selected, RevertMode.Unchanged).SetCompletionAction(CompletionAction.UpdatePendingWindow);
            Provider.Status(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Diff Against Head..." menu handler
        static bool ResolveTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            return Provider.enabled && Provider.ResolveIsValid(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Diff Against Head..." menu handler
        static void Resolve(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            WindowResolve.Open(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Lock" menu handler
        static bool LockTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            return Provider.enabled && Provider.hasLockingSupport && Provider.LockIsValid(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Lock" menu handler
        static void Lock(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            Provider.Lock(selected, true).SetCompletionAction(CompletionAction.UpdatePendingWindow);
        }

        [Shortcut("Version Control/Lock")]
        static void Lock(ShortcutArguments args)
        {
            Lock(null);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Unlock" menu handler
        static bool UnlockTest(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            return Provider.enabled && Provider.hasLockingSupport && Provider.UnlockIsValid(selected);
        }

        // Called from native class VCSAssetMenuHandler as "Assets/Version Control/Unlock" menu handler
        static void Unlock(MenuCommand cmd)
        {
            AssetList selected = Provider.GetAssetListFromSelection();
            selected = Provider.ConsolidateAssetList(selected, CheckoutMode.Both);
            Provider.Lock(selected, false).SetCompletionAction(CompletionAction.UpdatePendingWindow);
        }

        [Shortcut("Version Control/Unlock")]
        static void Unlock(ShortcutArguments args)
        {
            Unlock(null);
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

        [Shortcut("Version Control/Diff Against Head...")]
        static void DiffHead(ShortcutArguments args)
        {
            DiffHead(null);
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
