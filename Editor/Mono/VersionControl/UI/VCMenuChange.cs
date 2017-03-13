// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.VersionControl;

namespace UnityEditorInternal.VersionControl
{
    // Menu used when right clicking on change lists only.  As they are single select they will not get mixed up with asset selections.
    public class ChangeSetContextMenu
    {
        // Get the selected change list.  Only one valid at a time
        static ChangeSet GetChangeSet(ChangeSets changes)
        {
            if (changes.Count == 0)
                return null;

            return changes[0];
        }

        //[MenuItem ("CONTEXT/Change/Submit...", true)]
        static bool SubmitTest(int userData)
        {
            ChangeSets sets = ListControl.FromID(userData).SelectedChangeSets;
            return (sets.Count > 0 && Provider.SubmitIsValid(sets[0], null));
        }

        //[MenuItem ("CONTEXT/Change/Submit...", false, 100)]
        static void Submit(int userData)
        {
            ChangeSets set = ListControl.FromID(userData).SelectedChangeSets;
            ChangeSet change = GetChangeSet(set);

            if (change != null)
                WindowChange.Open(change, new AssetList(), true);
        }

        //[MenuItem ("CONTEXT/Change/Revert...", true)]
        static bool RevertTest(int userData)
        {
            ChangeSets list = ListControl.FromID(userData).SelectedChangeSets;
            return list.Count > 0;
        }

        //[MenuItem ("CONTEXT/Change/Revert...", false, 200)]
        static void Revert(int userData)
        {
            ChangeSets set = ListControl.FromID(userData).SelectedChangeSets;
            ChangeSet change = GetChangeSet(set);

            if (change != null)
                WindowRevert.Open(change);
        }

        //[MenuItem ("CONTEXT/Change/Revert Unchanged", true)]
        static bool RevertUnchangedTest(int userData)
        {
            ChangeSets sets = ListControl.FromID(userData).SelectedChangeSets;
            return sets.Count > 0;
        }

        //[MenuItem ("CONTEXT/Change/Revert Unchanged", false, 201)]
        static void RevertUnchanged(int userData)
        {
            ChangeSets sets = ListControl.FromID(userData).SelectedChangeSets;
            Provider.RevertChangeSets(sets, RevertMode.Unchanged).SetCompletionAction(CompletionAction.UpdatePendingWindow);
            Provider.InvalidateCache();
        }

        //[MenuItem ("CONTEXT/Change/Resolve Conflicts...", true)]
        private static bool ResolveTest(int userData)
        {
            return ListControl.FromID(userData).SelectedChangeSets.Count > 0;
        }

        //[MenuItem ("CONTEXT/Change/Resolve Conflicts...", false, 202)]
        private static void Resolve(int userData)
        {
            ChangeSets set = ListControl.FromID(userData).SelectedChangeSets;
            ChangeSet change = GetChangeSet(set);

            if (change != null)
                WindowResolve.Open(change);
        }

        //[MenuItem ("CONTEXT/Change/New Changeset...", true)]
        static bool NewChangeSetTest(int userDatad)
        {
            return Provider.isActive;
        }

        //[MenuItem ("CONTEXT/Change/New Changeset...", false, 300)]
        static void NewChangeSet(int userData)
        {
            WindowChange.Open(new AssetList(), false);
        }

        //[MenuItem ("CONTEXT/Change/Edit Changeset...", true)]
        static bool EditChangeSetTest(int userData)
        {
            ChangeSets set = ListControl.FromID(userData).SelectedChangeSets;
            if (set.Count == 0) return false;
            ChangeSet change = GetChangeSet(set);
            return (change.id != "-1" && Provider.SubmitIsValid(set[0], null));
        }

        //[MenuItem ("CONTEXT/Change/Edit Changeset...", false, 301)]
        static void EditChangeSet(int userData)
        {
            ChangeSets set = ListControl.FromID(userData).SelectedChangeSets;
            ChangeSet change = GetChangeSet(set);

            if (change != null)
                WindowChange.Open(change, new AssetList(), false);
        }

        //[MenuItem ("CONTEXT/Change/Delete Empty Changeset", true)]
        static bool DeleteChangeSetTest(int userData)
        {
            ListControl l = ListControl.FromID(userData);
            ChangeSets set = l.SelectedChangeSets;
            if (set.Count == 0) return false;
            ChangeSet change = GetChangeSet(set);

            if (change.id == "-1")
                return false;

            ListItem item = l.GetChangeSetItem(change);
            // TODO: Make changelist cache nonmanaged side to fix this!
            bool hasAssets = item != null && item.HasChildren && item.FirstChild.Asset != null && item.FirstChild.Name != ListControl.c_emptyChangeListMessage;
            if (!hasAssets)
            {
                Task task = Provider.ChangeSetStatus(change);
                task.Wait();
                hasAssets = task.assetList.Count != 0;
            }

            return !hasAssets && Provider.DeleteChangeSetsIsValid(set);
        }

        //[MenuItem ("CONTEXT/Change/Delete Empty Changeset", false, 302)]
        static void DeleteChangeSet(int userData)
        {
            ChangeSets set = ListControl.FromID(userData).SelectedChangeSets;
            Provider.DeleteChangeSets(set).SetCompletionAction(CompletionAction.UpdatePendingWindow);
        }
    }
}
