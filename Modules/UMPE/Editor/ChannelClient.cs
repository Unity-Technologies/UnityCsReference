// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Text;
using JetBrains.Annotations;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.MPE
{
    [MovedFrom("Unity.MPE")]
    public partial class ChannelClient
    {
        internal static Dictionary<int, ChannelClient> s_Clients = new Dictionary<int, ChannelClient>();
        List<Action<string>> m_Handlers = new List<Action<string>>();
        List<Action<byte[]>> m_BinaryHandlers = new List<Action<byte[]>>();

        public int clientId { get; }
        public string channelName { get; }
        public bool isAutoTick { get; private set; }

        public bool IsConnected()
        {
            return IsConnected(clientId);
        }

        public void Start(bool autoTick)
        {
            isAutoTick = autoTick;
            Start(clientId, autoTick);
            Tick();
        }

        public void Stop()
        {
            Stop(clientId);
        }

        public void Close()
        {
            m_Handlers = new List<Action<string>>();
            m_BinaryHandlers = new List<Action<byte[]>>();
            Close(channelName);
        }

        public void Tick()
        {
            Tick(clientId);
        }

        public void Send(string data)
        {
            Send(clientId, data);
        }

        public void Send(byte[] data)
        {
            Send(clientId, data);
        }

        public Action RegisterMessageHandler(Action<string> handler)
        {
            if (m_Handlers.Contains(handler))
                throw new Exception("Channel Client Handler already registered");

            m_Handlers.Add(handler);

            return () =>
            {
                UnregisterMessageHandler(handler);
            };
        }

        public void UnregisterMessageHandler(Action<string> handler)
        {
            m_Handlers.Remove(handler);
        }

        public Action RegisterMessageHandler(Action<byte[]> handler)
        {
            if (m_BinaryHandlers.Contains(handler))
                throw new Exception("Channel Client Handler already registered");

            m_BinaryHandlers.Add(handler);

            return () =>
            {
                UnregisterMessageHandler(handler);
            };
        }

        public void UnregisterMessageHandler(Action<byte[]> handler)
        {
            m_BinaryHandlers.Remove(handler);
        }

        public int NewRequestId()
        {
            return NewRequestId(clientId);
        }

        public ChannelClientInfo GetChannelClientInfo()
        {
            return GetChannelClientInfo(clientId);
        }

        public static void Send(int connectionId, byte[] data)
        {
            SendBinary(connectionId, data);
        }

        public static void Close(string channelName)
        {
            var clientId = GetChannelClientInfo(channelName).clientId;
            if (clientId != -1)
            {
                s_Clients.Remove(clientId);
            }
            Stop(clientId);
            Internal_CloseClient(channelName);
        }

        public static ChannelClient GetOrCreateClient(string channelName)
        {
            var id = GetChannelClientInfo(channelName).clientId;
            if (id == -1)
            {
                id = Internal_GetOrCreateClient(channelName);
            }

            if (!s_Clients.ContainsKey(id))
            {
                s_Clients.Add(id, new ChannelClient(channelName, id));
            }

            return s_Clients[id];
        }

        public static void Shutdown()
        {
            Internal_Shutdown();
            s_Clients = new Dictionary<int, ChannelClient>();
        }

        public static ChannelClientInfo GetChannelClientInfo(string channelName)
        {
            return GetChannelClientInfo(ChannelService.ChannelNameToId(channelName));
        }

        private ChannelClient(string channelName, int id)
        {
            this.channelName = channelName;
            clientId = id;
        }

        [UsedImplicitly, RequiredByNativeCode]
        private static void IncomingChannelClientData(int clientId, byte[] data)
        {
            ChannelClient client;
            if (!s_Clients.TryGetValue(clientId, out client))
            {
                return;
            }

            string strData = null;
            foreach (var handler in client.m_Handlers)
            {
                try
                {
                    if (strData == null)
                    {
                        strData = Encoding.UTF8.GetString(data);
                    }

                    handler(strData);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            foreach (var handler in client.m_BinaryHandlers)
            {
                try
                {
                    handler(data);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }

    [MovedFrom("Unity.MPE")]
    public struct ChannelClientScope : IDisposable
    {
        private Action m_Off;
        private bool m_CloseClientOnExit;

        public ChannelClient client { get; private set; }
        public ChannelClientScope(bool autoTick, string channelName, Action<string> handler, bool closeClientOnExit = true)
        {
            m_CloseClientOnExit = closeClientOnExit;
            client = ChannelClient.GetOrCreateClient(channelName);
            m_Off = client.RegisterMessageHandler(handler);
            client.Start(autoTick);
        }

        public ChannelClientScope(bool autoTick, string channelName, Action<byte[]> handler, bool closeClientOnExit = true)
        {
            m_CloseClientOnExit = closeClientOnExit;
            client = ChannelClient.GetOrCreateClient(channelName);
            m_Off = client.RegisterMessageHandler(handler);
            client.Start(autoTick);
        }

        public void Dispose()
        {
            m_Off();
            if (m_CloseClientOnExit)
                client.Close();
        }
    }
}
