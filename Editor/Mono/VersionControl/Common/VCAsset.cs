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
            foreach (Asset.States st in states)
            {
                if ((this.state & st) != 0) return true;
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

        internal static string StateToString(Asset.States state)
        {
            if (IsState(state, Asset.States.AddedLocal))
                return "Added Local";

            if (IsState(state, Asset.States.AddedRemote))
                return "Added Remote";

            if (IsState(state, Asset.States.CheckedOutLocal) && !IsState(state, Asset.States.LockedLocal))
                return "Checked Out Local";

            if (IsState(state, Asset.States.CheckedOutRemote) && !IsState(state, Asset.States.LockedRemote))
                return "Checked Out Remote";

            if (IsState(state, Asset.States.Conflicted))
                return "Conflicted";

            if (IsState(state, Asset.States.DeletedLocal))
                return "Deleted Local";

            if (IsState(state, Asset.States.DeletedRemote))
                return "Deleted Remote";

            if (IsState(state, Asset.States.Local))
                return "Local";

            if (IsState(state, Asset.States.LockedLocal))
                return "Locked Local";

            if (IsState(state, Asset.States.LockedRemote))
                return "Locked Remote";

            if (IsState(state, Asset.States.OutOfSync))
                return "Out Of Sync";

            if (IsState(state, Asset.States.Updating))
                return "Updating Status";

            return "";
        }

        internal static string AllStateToString(Asset.States state)
        {
            var sb = new System.Text.StringBuilder();

            if (IsState(state, Asset.States.AddedLocal))
                sb.AppendLine("Added Local");

            if (IsState(state, Asset.States.AddedRemote))
                sb.AppendLine("Added Remote");

            if (IsState(state, Asset.States.CheckedOutLocal))
                sb.AppendLine("Checked Out Local");

            if (IsState(state, Asset.States.CheckedOutRemote))
                sb.AppendLine("Checked Out Remote");

            if (IsState(state, Asset.States.Conflicted))
                sb.AppendLine("Conflicted");

            if (IsState(state, Asset.States.DeletedLocal))
                sb.AppendLine("Deleted Local");

            if (IsState(state, Asset.States.DeletedRemote))
                sb.AppendLine("Deleted Remote");

            if (IsState(state, Asset.States.Local))
                sb.AppendLine("Local");

            if (IsState(state, Asset.States.LockedLocal))
                sb.AppendLine("Locked Local");

            if (IsState(state, Asset.States.LockedRemote))
                sb.AppendLine("Locked Remote");

            if (IsState(state, Asset.States.OutOfSync))
                sb.AppendLine("Out Of Sync");

            if (IsState(state, Asset.States.Synced))
                sb.AppendLine("Synced");

            if (IsState(state, Asset.States.Missing))
                sb.AppendLine("Missing");

            if (IsState(state, Asset.States.ReadOnly))
                sb.AppendLine("ReadOnly");

            return sb.ToString();
        }

        internal string AllStateToString()
        {
            return AllStateToString(this.state);
        }

        internal string StateToString()
        {
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
