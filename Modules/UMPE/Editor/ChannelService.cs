// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Unity.MPE
{
    delegate void ChannelHandler(int clientId, byte[] binaryData);

    internal static partial class ChannelService
    {
        internal static Dictionary<int, List<ChannelHandler>> s_Handlers = new Dictionary<int, List<ChannelHandler>>();

        public static Action GetOrCreateChannel(string channelName, ChannelHandler handler)
        {
            if (Internal_GetOrCreateChannel(channelName) == -1)
            {
                throw new Exception("Cannot create channel: " + channelName);
            }
            return On(channelName, handler);
        }

        public static Action On(string channelName, ChannelHandler handler)
        {
            var channel = GetChannelFromName(channelName);
            if (ChannelInfo.InvalidChannel == channel)
            {
                throw new Exception("Channel doesn't exists or is not open.");
            }

            List<ChannelHandler> handlers = null;
            if (!s_Handlers.TryGetValue(channel.channelId, out handlers))
            {
                handlers = new List<ChannelHandler> { handler };
                s_Handlers.Add(channel.channelId, handlers);
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
                Off(channelName, handler);
            };
        }

        public static void Off(string channelName, ChannelHandler handler)
        {
            var channel = GetChannelFromName(channelName);
            if (ChannelInfo.InvalidChannel == channel)
            {
                return;
            }
            List<ChannelHandler> handlers = null;
            if (s_Handlers.TryGetValue(channel.channelId, out handlers))
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    s_Handlers.Remove(channel.channelId);
                }
            }
        }

        public static void CloseChannel(string channelName)
        {
            var channel = GetChannelFromName(channelName);
            if (ChannelInfo.InvalidChannel == channel)
            {
                return;
            }

            s_Handlers.Remove(channel.channelId);
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
                if (channelInfo.channelName == channelName)
                    return channelInfo;
            }

            return ChannelInfo.InvalidChannel;
        }

        [UsedImplicitly, RequiredByNativeCode]
        private static void IncomingChannelServiceData(int channelId, int clientId, byte[] data)
        {
            List<ChannelHandler> handlers = null;
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

    struct ChannelScope : IDisposable
    {
        private bool m_CloseChannelOnExit;
        private string m_ChannelName;
        private Action m_Off;

        public ChannelScope(string channelName, ChannelHandler handler, bool closeChannelOnExit = true)
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
