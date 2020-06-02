// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.VersionControl
{
    public partial class Asset
    {
        internal static bool IsState(Asset.States isThisState, Asset.States partOfThisState)
        {
            return (isThisState & partOfThisState) != 0;
        }

        public bool IsState(Asset.States state)
        {
            return IsState(this.state, state);
        }

        public bool IsOneOfStates(Asset.States[] states)
        {
            var localState = this.state;
            foreach (Asset.States st in states)
            {
                if ((localState & st) != 0) return true;
            }
            return false;
        }

        internal bool IsUnderVersionControl
        {
            get { return IsState(Asset.States.Synced) || IsState(Asset.States.OutOfSync) || IsState(Asset.States.AddedLocal); }
        }

        public void Edit()
        {
            UnityEngine.Object load = Load();

            if (load != null)
                AssetDatabase.OpenAsset(load);
        }

        public UnityEngine.Object Load()
        {
            if (state == States.DeletedLocal || isMeta)
            {
                return null;
            }

            // Standard asset loading
            return AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
        }

        internal static string StateToString(States state)
        {
            if (IsState(state, States.AddedLocal))
                return "Added Local";

            if (IsState(state, States.AddedRemote))
                return "Added Remote";

            if (IsState(state, States.DeletedRemote))
                return "Deleted Remote";

            if (IsState(state, States.CheckedOutLocal) && !IsState(state, States.LockedLocal))
                return "Checked Out Local";

            if (IsState(state, States.CheckedOutRemote) && !IsState(state, States.LockedRemote))
                return "Checked Out Remote";

            if (IsState(state, States.Conflicted))
                return "Conflicted";

            if (IsState(state, States.DeletedLocal))
                return "Deleted Local";

            if (IsState(state, States.Local) && !(IsState(state, States.OutOfSync) || IsState(state, States.Synced)))
                return "Local";

            if (IsState(state, States.LockedLocal))
                return "Locked Local";

            if (IsState(state, States.LockedRemote))
                return "Locked Remote";

            if (IsState(state, States.OutOfSync))
                return "Out Of Sync";

            if (IsState(state, States.Updating))
                return "Updating Status";

            return "";
        }

        internal static string AllStateToString(States state)
        {
            var sb = new System.Text.StringBuilder();

            if (IsState(state, States.AddedLocal))
                sb.Append("Added Local, ");

            if (IsState(state, States.AddedRemote))
                sb.Append("Added Remote, ");

            if (IsState(state, States.CheckedOutLocal))
                sb.Append("Checked Out Local, ");

            if (IsState(state, States.CheckedOutRemote))
                sb.Append("Checked Out Remote, ");

            if (IsState(state, States.Conflicted))
                sb.Append("Conflicted, ");

            if (IsState(state, States.DeletedLocal))
                sb.Append("Deleted Local, ");

            if (IsState(state, States.DeletedRemote))
                sb.Append("Deleted Remote, ");

            if (IsState(state, States.Local))
                sb.Append("Local, ");

            if (IsState(state, States.LockedLocal))
                sb.Append("Locked Local, ");

            if (IsState(state, States.LockedRemote))
                sb.Append("Locked Remote, ");

            if (IsState(state, States.OutOfSync))
                sb.Append("Out Of Sync, ");

            if (IsState(state, States.Synced))
                sb.Append("Synced, ");

            if (IsState(state, States.Missing))
                sb.Append("Missing, ");

            if (IsState(state, States.ReadOnly))
                sb.Append("ReadOnly, ");

            if (IsState(state, States.Unversioned))
                sb.Append("Unversioned, ");

            if (IsState(state, States.Exclusive))
                sb.Append("Exclusive, ");

            // remove trailing ", " if had any
            if (sb.Length > 2)
                sb.Remove(sb.Length - 2, 2);

            return sb.ToString();
        }

        internal string StateToString()
        {
            if (isFolder && !isMeta && !Provider.isVersioningFolders)
                return string.Empty;
            return StateToString(this.state);
        }

        public string prettyPath
        {
            get
            {
                return path;
            }
        }
    }
}
