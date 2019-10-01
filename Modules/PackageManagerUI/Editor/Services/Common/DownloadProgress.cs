// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class DownloadProgress
    {
        public enum State
        {
            Started,
            InProgress,
            Completed,
            Decrypting,
            Aborted,
            Error
        }

        public string productId;
        public State state;
        public ulong current;
        public ulong total;
        public string errorMessage;

        public DownloadProgress(string productId)
        {
            this.productId = productId;
            state = State.Started;
            current = 0;
            total = 0;
            errorMessage = string.Empty;
        }
    }
}
