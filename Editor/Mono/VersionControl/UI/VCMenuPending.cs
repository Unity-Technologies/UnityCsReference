// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

using UnityEditor.VersionControl;

namespace UnityEditorInternal.VersionControl
{
    // Standard menu used in the change list window when selecting assets.
    public class PendingWindowContextMenu
    {
        //[MenuItem("CONTEXT/Pending/Submit...", true, 100)]
        static bool SubmitTest(int userData)
        {
            return Provider.SubmitIsValid(null, ListControl.FromID(userData).SelectedAssets);
        }

        //[MenuItem ("CONTEXT/Pending/Submit...", false, 100)]
        static void Submit(int userData)
        {
            WindowChange.Open(ListControl.FromID(userData).SelectedAssets, true);
        }

        //[MenuItem("CONTEXT/Pending/Revert...", true, 200)]
        static bool RevertTest(int userData)
        {
            return Provider.RevertIsValid(ListControl.FromID(userData).SelectedAssets, RevertMode.Normal);
        }

        //[MenuItem ("CONTEXT/Pending/Revert...", false, 200)]
        static void Revert(int userData)
        {
            WindowRevert.Open(ListControl.FromID(userData).SelectedAssets);
        }

        //[MenuItem ("CONTEXT/Pending/Revert Unchanged", true, 201)]
        static bool RevertUnchangedTest(int userData)
        {
            return Provider.RevertIsValid(ListControl.FromID(userData).SelectedAssets, RevertMode.Normal);
        }

        //[MenuItem ("CONTEXT/Pending/Revert Unchanged", false, 201)]
        static void RevertUnchanged(int userData)
        {
            AssetList list = ListControl.FromID(userData).SelectedAssets;
            Provider.Revert(list, RevertMode.Unchanged).SetCompletionAction(CompletionAction.UpdatePendingWindow);
            Provider.Status(list);
        }

        //[MenuItem("CONTEXT/Pending/Resolve Conflicts...", true, 202)]
        static bool ResolveTest(int userData)
        {
            return Provider.ResolveIsValid(ListControl.FromID(userData).SelectedAssets);
        }

        //[MenuItem ("CONTEXT/Pending/Resolve Conflicts...", false, 202)]
        static void Resolve(int userData)
        {
            WindowResolve.Open(ListControl.FromID(userData).SelectedAssets);
        }

        //[MenuItem("CONTEXT/Pending/Lock", true, 300)]
        static bool LockTest(int userData)
        {
            return Provider.LockIsValid(ListControl.FromID(userData).SelectedAssets);
        }

        //[MenuItem ("CONTEXT/Pending/Lock", false, 300)]
        static void Lock(int userData)
        {
            AssetList list = ListControl.FromID(userData).SelectedAssets;
            Provider.Lock(list, true).SetCompletionAction(CompletionAction.UpdatePendingWindow);
        }

        //[MenuItem("CONTEXT/Pending/Unlock", true, 301)]
        static bool UnlockTest(int userData)
        {
            return Provider.UnlockIsValid(ListControl.FromID(userData).SelectedAssets);
        }

        //[MenuItem ("CONTEXT/Pending/Unlock", false, 301)]
        static void Unlock(int userData)
        {
            AssetList list = ListControl.FromID(userData).SelectedAssets;
            Provider.Lock(list, false).SetCompletionAction(CompletionAction.UpdatePendingWindow);
        }

        //[MenuItem("CONTEXT/Pending/Diff/Against Head...", true, 400)]
        static bool DiffHeadTest(int userData)
        {
            return Provider.DiffIsValid(ListControl.FromID(userData).SelectedAssets);
        }

        //[MenuItem ("CONTEXT/Pending/Diff/Against Head...", false, 400)]
        static void DiffHead(int userData)
        {
            Provider.DiffHead(ListControl.FromID(userData).SelectedAssets, false);
        }

        //[MenuItem("CONTEXT/Pending/Diff/Against Head with .meta...", true, 401)]
        static bool DiffHeadWithMetaTest(int userData)
        {
            return Provider.DiffIsValid(ListControl.FromID(userData).SelectedAssets);
        }

        //[MenuItem ("CONTEXT/Pending/Diff/Against Head with .meta...", false, 401)]
        static void DiffHeadWithMeta(int userData)
        {
            Provider.DiffHead(ListControl.FromID(userData).SelectedAssets, true);
        }

        //[MenuItem("CONTEXT/Pending/Reveal in Finder", true, 402)]
        static bool ShowInExplorerTest(int userData)
        {
            return (ListControl.FromID(userData)).SelectedAssets.Count > 0;
        }

        //[MenuItem ("CONTEXT/Pending/Reveal in Finder", false, 402)]
        static void ShowInExplorer(int userData)
        {
            if (System.Environment.OSVersion.Platform == System.PlatformID.MacOSX ||
                System.Environment.OSVersion.Platform == System.PlatformID.Unix)
            {
                EditorApplication.ExecuteMenuItem("Assets/Reveal in Finder");
            }
            else
            {
                EditorApplication.ExecuteMenuItem("Assets/Show in Explorer");
            }
        }

        //[MenuItem("CONTEXT/Pending/New Changeset...", true, 501)]
        static bool NewChangeSetTest(int userData)
        {
            return Provider.isActive;
        }

        //[MenuItem ("CONTEXT/Pending/New Changeset...", false, 501)]
        static void NewChangeSet(int userData)
        {
            WindowChange.Open(ListControl.FromID(userData).SelectedAssets, false);
        }
    }
}
