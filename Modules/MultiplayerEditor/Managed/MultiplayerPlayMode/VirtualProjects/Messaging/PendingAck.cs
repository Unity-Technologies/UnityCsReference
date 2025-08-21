// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer.PlayMode.Editor
{
    struct PendingAck
    {
        public Action SuccessCallback;
        public Action<string> ErrorCallback;
        public DateTime SentTimestamp;
        public string EventType;
        public string Sender;
    }
}
