// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Scripting;

namespace UnityEditor.Networking.PlayerConnection
{
    [Serializable]
    public class ConnectedPlayer
    {
        public ConnectedPlayer()
        {
        }

        public ConnectedPlayer(int playerId)
        {
            m_PlayerId = playerId;
        }

        [SerializeField]
        private int m_PlayerId;
        public int PlayerId
        {
            get { return m_PlayerId; }
        }
    }

    [Serializable]
    public class EditorConnection : ScriptableSingleton<EditorConnection>, IEditorPlayerConnection
    {
        internal static IPlayerEditorConnectionNative connectionNative;

        [SerializeField]
        private PlayerEditorConnectionEvents m_PlayerEditorConnectionEvents = new PlayerEditorConnectionEvents();

        [SerializeField]
        private List<int> m_connectedPlayers = new List<int>();

        public List<ConnectedPlayer> ConnectedPlayers
        {
            get { return m_connectedPlayers.Select(x => new ConnectedPlayer(x)).ToList(); }
        }

        public void Initialize()
        {
            GetEditorConnectionNativeApi().Initialize();
        }

        private void Cleanup()
        {
            UnregisterAllPersistedListeners(m_PlayerEditorConnectionEvents.connectionEvent);
            UnregisterAllPersistedListeners(m_PlayerEditorConnectionEvents.disconnectionEvent);
            m_PlayerEditorConnectionEvents.messageTypeSubscribers.Clear();
        }

        private void UnregisterAllPersistedListeners(UnityEventBase connectionEvent)
        {
            var persistentEventCount = connectionEvent.GetPersistentEventCount();
            for (int i = 0; i < persistentEventCount; i++)
            {
                connectionEvent.UnregisterPersistentListener(i);
            }
        }

        private IPlayerEditorConnectionNative GetEditorConnectionNativeApi()
        {
            return connectionNative ?? new EditorConnectionInternal();
        }

        public void Register(Guid messageId, UnityAction<MessageEventArgs> callback)
        {
            if (messageId == Guid.Empty)
            {
                throw new ArgumentException("Cant be Guid.Empty", "messageId");
            }

            if (!m_PlayerEditorConnectionEvents.messageTypeSubscribers.Any(x => x.MessageTypeId == messageId))
            {
                GetEditorConnectionNativeApi().RegisterInternal(messageId);
            }

            m_PlayerEditorConnectionEvents.AddAndCreate(messageId)
            .AddPersistentListener(callback, UnityEventCallState.EditorAndRuntime);
        }

        public void Unregister(Guid messageId, UnityAction<MessageEventArgs> callback)
        {
            m_PlayerEditorConnectionEvents.UnregisterManagedCallback(messageId, callback);
            if (!m_PlayerEditorConnectionEvents.messageTypeSubscribers.Any(x => x.MessageTypeId == messageId))
            {
                GetEditorConnectionNativeApi().UnregisterInternal(messageId);
            }
        }

        public void RegisterConnection(UnityAction<int> callback)
        {
            foreach (var playerId in m_connectedPlayers)
            {
                callback.Invoke(playerId);
            }
            m_PlayerEditorConnectionEvents.connectionEvent.AddPersistentListener(callback, UnityEventCallState.EditorAndRuntime);
        }

        public void RegisterDisconnection(UnityAction<int> callback)
        {
            m_PlayerEditorConnectionEvents.disconnectionEvent.AddPersistentListener(callback, UnityEventCallState.EditorAndRuntime);
        }

        public void Send(Guid messageId, byte[] data, int playerId)
        {
            if (messageId == Guid.Empty)
            {
                throw new ArgumentException("Cant be Guid.Empty", "messageId");
            }

            GetEditorConnectionNativeApi().SendMessage(messageId, data, playerId);
        }

        public void Send(Guid messageId, byte[] data)
        {
            Send(messageId, data, 0);
        }

        [RequiredByNativeCode]
        private static void MessageCallbackInternal(IntPtr data, UInt64 size, UInt64 guid, string messageId)
        {
            byte[] bytes = null;
            if (size > 0)
            {
                bytes = new byte[size];
                Marshal.Copy(data, bytes, 0, unchecked((int)size));
            }
            instance.m_PlayerEditorConnectionEvents.InvokeMessageIdSubscribers(new Guid(messageId), bytes, (int)guid);
        }

        [RequiredByNativeCode]
        private static void ConnectedCallbackInternal(int playerId)
        {
            instance.m_connectedPlayers.Add(playerId);
            instance.m_PlayerEditorConnectionEvents.connectionEvent.Invoke(playerId);
        }

        [RequiredByNativeCode]
        private static void DisconnectedCallback(int playerId)
        {
            instance.m_connectedPlayers.Remove(playerId);
            instance.m_PlayerEditorConnectionEvents.disconnectionEvent.Invoke(playerId);

            if (!instance.ConnectedPlayers.Any())
            {
                instance.Cleanup();
            }
        }
    }
}
