// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.MPE
{
    [MovedFrom("Unity.MPE")]
    public static partial class ChannelService
    {
        internal static Dictionary<int, List<Action<int, byte[]>>> s_Handlers = new Dictionary<int, List<Action<int, byte[]>>>();

        public static Action GetOrCreateChannel(string channelName, Action<int, byte[]> handler)
        {
            if (Internal_GetOrCreateChannel(channelName) == -1)
            {
                throw new Exception("Cannot create channel: " + channelName);
            }
            return RegisterMessageHandler(channelName, handler);
        }

        public static Action RegisterMessageHandler(string channelName, Action<int, byte[]> handler)
        {
            var channel = GetChannelFromName(channelName);
            if (ChannelInfo.invalidChannel == channel)
            {
                throw new Exception("Channel doesn't exists or is not open.");
            }

            List<Action<int, byte[]>> handlers = null;
            if (!s_Handlers.TryGetValue(channel.id, out handlers))
            {
                handlers = new List<Action<int, byte[]>> { handler };
                s_Handlers.Add(channel.id, handlers);
            }
            else if (handlers.Contains(handler))
            {
                throw new Exception("Cannot add already existing channel handler for channel: " + channelName);
            }
            else
            {
                handlers.Add(handler);
            }

            return () =>
            {
                UnregisterMessageHandler(channelName, handler);
            };
        }

        public static void UnregisterMessageHandler(string channelName, Action<int, byte[]> handler)
        {
            var channel = GetChannelFromName(channelName);
            if (ChannelInfo.invalidChannel == channel)
            {
                return;
            }
            List<Action<int, byte[]>> handlers = null;
            if (s_Handlers.TryGetValue(channel.id, out handlers))
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    s_Handlers.Remove(channel.id);
                }
            }
        }

        public static void CloseChannel(string channelName)
        {
            var channel = GetChannelFromName(channelName);
            if (ChannelInfo.invalidChannel == channel)
            {
                return;
            }

            s_Handlers.Remove(channel.id);
            Internal_CloseChannel(channelName);
        }

        public static void Broadcast(int channelId, byte[] data)
        {
            BroadcastBinary(channelId, data);
        }

        public static void Send(int connectionId, byte[] data)
        {
            SendBinary(connectionId, data);
        }

        internal static ChannelInfo GetChannelFromName(string channelName)
        {
            var channelInfos = GetChannelList();
            foreach (var channelInfo in channelInfos)
            {
                if (channelInfo.name == channelName)
                    return channelInfo;
            }

            return ChannelInfo.invalidChannel;
        }

        [UsedImplicitly, RequiredByNativeCode]
        private static void IncomingChannelServiceData(int channelId, int clientId, byte[] data)
        {
            List<Action<int, byte[]>> handlers = null;
            if (!s_Handlers.TryGetValue(channelId, out handlers))
            {
                return;
            }

            foreach (var channelHandler in handlers)
            {
                try
                {
                    channelHandler(clientId, data);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }
    }

    [MovedFrom("Unity.MPE")]
    public struct ChannelScope : IDisposable
    {
        private bool m_CloseChannelOnExit;
        private string m_ChannelName;
        private Action m_Off;

        public ChannelScope(string channelName, Action<int, byte[]> handler, bool closeChannelOnExit = true)
        {
            m_CloseChannelOnExit = closeChannelOnExit;
            m_ChannelName = channelName;
            m_Off = ChannelService.GetOrCreateChannel(channelName, handler);
        }

        public void Dispose()
        {
            m_Off();
            if (m_CloseChannelOnExit)
                ChannelService.CloseChannel(m_ChannelName);
        }
    }
}
