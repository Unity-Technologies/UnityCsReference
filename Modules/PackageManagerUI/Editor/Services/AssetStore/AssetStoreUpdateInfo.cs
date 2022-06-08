// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class AssetStoreUpdateInfo
    {
        [Flags]
        internal enum UpdateStatus
        {
            None = 0,
            UpdateChecked = 1 << 0,
            CanUpdate = 1 << 1,
            CanDowngrade = 1 << 2
        }

        public string productId;
        public string uploadId;

        public UpdateStatus updateStatus;
        public bool updateInfoFetched => (updateStatus & UpdateStatus.UpdateChecked) != 0;
        public bool canUpdateOrDowngrade => (updateStatus & (UpdateStatus.CanUpdate | UpdateStatus.CanDowngrade)) != 0;
        public bool canUpdate => (updateStatus & UpdateStatus.CanUpdate) != 0;
        public bool canDowngrade => (updateStatus & UpdateStatus.CanDowngrade) != 0;
    }
}
