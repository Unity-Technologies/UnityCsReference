// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

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

        public string packageId;
        public State state;
        public ulong current;
        public ulong total;
        public string message;

        public DownloadProgress(string packageId)
        {
            this.packageId = packageId;
            state = State.Started;
            current = 0;
            total = 0;
            message = string.Empty;
        }
    }
}
