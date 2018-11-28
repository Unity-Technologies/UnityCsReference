// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Events;

namespace UnityEngine.Experimental.Networking.PlayerConnection
{
    public enum ConnectionTarget
    {
        None, // Only to be used from player side. The editor is always connected to itself, unless connected to a player.
        Player,
        Editor,
    }

    public interface IConnectionState : IDisposable
    {
        ConnectionTarget connectedToTarget { get; }
        string connectionName { get; }
    }
}

namespace UnityEngine.Networking.PlayerConnection
{
    [Serializable]
    public class MessageEventArgs
    {
        public int playerId;

        public byte[] data;
    }

    public interface IEditorPlayerConnection
    {
        void Register(Guid messageId, UnityAction<MessageEventArgs> callback);

        void Unregister(Guid messageId, UnityAction<MessageEventArgs> callback);

        void DisconnectAll();

        void RegisterConnection(UnityAction<int> callback);

        void RegisterDisconnection(UnityAction<int> callback);

        void Send(Guid messageId, byte[] data);
    }
}
