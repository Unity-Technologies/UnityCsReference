// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngineInternal;
using UnityEngine.Networking.Types;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.Networking
{
#pragma warning disable 618

    [NativeHeader("Runtime/Networking/UNETManager.h")]
    [NativeHeader("Runtime/Networking/UNetTypes.h")]
    [NativeHeader("Runtime/Networking/UNETConfiguration.h")]
    [NativeConditional("ENABLE_NETWORK && ENABLE_UNET", true)]

    [StructLayout(LayoutKind.Sequential)]
    internal class ConnectionConfigInternal : IDisposable
    {
        public IntPtr m_Ptr;
        public ConnectionConfigInternal(ConnectionConfig config)
        {
            if (config == null)
                throw new NullReferenceException("config is not defined");
            m_Ptr = InternalCreate();
            if (!SetPacketSize(config.PacketSize))
                throw new ArgumentOutOfRangeException("PacketSize is too small");
            this.FragmentSize = config.FragmentSize;
            this.ResendTimeout = config.ResendTimeout;
            this.DisconnectTimeout = config.DisconnectTimeout;
            this.ConnectTimeout = config.ConnectTimeout;
            this.MinUpdateTimeout = config.MinUpdateTimeout;
            this.PingTimeout = config.PingTimeout;
            this.ReducedPingTimeout = config.ReducedPingTimeout;
            this.AllCostTimeout = config.AllCostTimeout;
            this.NetworkDropThreshold = config.NetworkDropThreshold;
            this.OverflowDropThreshold = config.OverflowDropThreshold;
            this.MaxConnectionAttempt = config.MaxConnectionAttempt;
            this.AckDelay = config.AckDelay;
            this.SendDelay = config.SendDelay;
            this.MaxCombinedReliableMessageSize = config.MaxCombinedReliableMessageSize;
            this.MaxCombinedReliableMessageCount = config.MaxCombinedReliableMessageCount;
            this.MaxSentMessageQueueSize = config.MaxSentMessageQueueSize;
            this.AcksType = (byte)config.AcksType;
            this.UsePlatformSpecificProtocols = config.UsePlatformSpecificProtocols;
            this.InitialBandwidth = config.InitialBandwidth;
            this.BandwidthPeakFactor = config.BandwidthPeakFactor;
            this.WebSocketReceiveBufferMaxSize = config.WebSocketReceiveBufferMaxSize;
            this.UdpSocketReceiveBufferMaxSize = config.UdpSocketReceiveBufferMaxSize;
            if (config.SSLCertFilePath != null)
            {
                int len = SetSSLCertFilePath(config.SSLCertFilePath);
                if (len != 0)
                    throw new ArgumentOutOfRangeException("SSLCertFilePath cannot be > than " + len.ToString());
            }
            if (config.SSLPrivateKeyFilePath != null)
            {
                int len = SetSSLPrivateKeyFilePath(config.SSLPrivateKeyFilePath);
                if (len != 0)
                    throw new ArgumentOutOfRangeException("SSLPrivateKeyFilePath cannot be > than " + len.ToString());
            }
            if (config.SSLCAFilePath != null)
            {
                int len = SetSSLCAFilePath(config.SSLCAFilePath);
                if (len != 0)
                    throw new ArgumentOutOfRangeException("SSLCAFilePath cannot be > than " + len.ToString());
            }
            for (byte i = 0; i < config.ChannelCount; ++i)
            {
                AddChannel((byte)config.GetChannel(i));
            }
            for (byte i = 0; i < config.SharedOrderChannelCount; ++i)
            {
                IList<byte> sharedOrderChannelsList = config.GetSharedOrderChannels(i);
                byte[] sharedOrderChannelsArray = new byte[sharedOrderChannelsList.Count];
                sharedOrderChannelsList.CopyTo(sharedOrderChannelsArray, 0);
                MakeChannelsSharedOrder(sharedOrderChannelsArray);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Ptr != IntPtr.Zero)
            {
                InternalDestroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        ~ConnectionConfigInternal()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                InternalDestroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        private static extern IntPtr InternalCreate();
        [NativeMethod(IsThreadSafe = true)]
        private static extern void InternalDestroy(IntPtr ptr);
        public extern byte AddChannel(int value);
        public extern bool SetPacketSize(ushort value);

        [NativeProperty("m_ProtocolRequired.m_FragmentSize", TargetType.Field)] private extern ushort FragmentSize { set; }
        [NativeProperty("m_ProtocolRequired.m_ResendTimeout", TargetType.Field)] private extern uint ResendTimeout { set; }
        [NativeProperty("m_ProtocolRequired.m_DisconnectTimeout", TargetType.Field)] private extern uint DisconnectTimeout { set; }
        [NativeProperty("m_ProtocolRequired.m_ConnectTimeout", TargetType.Field)] private extern uint ConnectTimeout { set; }
        [NativeProperty("m_ProtocolOptional.m_MinUpdateTimeout", TargetType.Field)] private extern uint MinUpdateTimeout { set; }
        [NativeProperty("m_ProtocolRequired.m_PingTimeout", TargetType.Field)] private extern uint PingTimeout { set; }
        [NativeProperty("m_ProtocolRequired.m_ReducedPingTimeout", TargetType.Field)] private extern uint ReducedPingTimeout { set; }
        [NativeProperty("m_ProtocolRequired.m_AllCostTimeout", TargetType.Field)] private extern uint AllCostTimeout { set; }
        [NativeProperty("m_ProtocolOptional.m_NetworkDropThreshold", TargetType.Field)] private extern byte NetworkDropThreshold { set; }
        [NativeProperty("m_ProtocolOptional.m_OverflowDropThreshold", TargetType.Field)] private extern byte OverflowDropThreshold { set; }
        [NativeProperty("m_ProtocolOptional.m_MaxConnectionAttempt", TargetType.Field)] private extern byte MaxConnectionAttempt { set; }
        [NativeProperty("m_ProtocolOptional.m_AckDelay", TargetType.Field)] private extern uint AckDelay { set; }
        [NativeProperty("m_ProtocolOptional.m_SendDelay", TargetType.Field)] private extern uint SendDelay { set; }
        [NativeProperty("m_ProtocolOptional.m_MaxCombinedReliableMessageSize", TargetType.Field)] private extern ushort MaxCombinedReliableMessageSize { set; }
        [NativeProperty("m_ProtocolOptional.m_MaxCombinedReliableMessageAmount", TargetType.Field)] private extern ushort MaxCombinedReliableMessageCount { set; }
        [NativeProperty("m_ProtocolOptional.m_MaxSentMessageQueueSize", TargetType.Field)] private extern ushort MaxSentMessageQueueSize { set; }
        [NativeProperty("m_ProtocolRequired.m_AcksType", TargetType.Field)] private extern byte AcksType { set; }
        [NativeProperty("m_ProtocolRequired.m_UsePlatformSpecificProtocols", TargetType.Field)] private extern bool UsePlatformSpecificProtocols { set; }
        [NativeProperty("m_ProtocolOptional.m_InitialBandwidth", TargetType.Field)] private extern uint InitialBandwidth { set; }
        [NativeProperty("m_ProtocolOptional.m_BandwidthPeakFactor", TargetType.Field)]  private extern float BandwidthPeakFactor { set; }
        [NativeProperty("m_ProtocolOptional.m_WebSocketReceiveBufferMaxSize", TargetType.Field)] private extern ushort WebSocketReceiveBufferMaxSize { set; }
        [NativeProperty("m_ProtocolOptional.m_UdpSocketReceiveBufferMaxSize", TargetType.Field)] private extern uint UdpSocketReceiveBufferMaxSize { set; }

        [NativeMethod("SetSSLCertFilePath")]
        public extern int SetSSLCertFilePath(string value);
        [NativeMethod("SetSSLPrivateKeyFilePath")]
        public extern int SetSSLPrivateKeyFilePath(string value);
        [NativeMethod("SetSSLCAFilePath")]
        public extern int SetSSLCAFilePath(string value);

        [NativeMethod("MakeChannelsSharedOrder")]
        private extern bool MakeChannelsSharedOrder(byte[] values);
    }
    [NativeHeader("Runtime/Networking/UNETConfiguration.h")]
    [NativeConditional("ENABLE_NETWORK && ENABLE_UNET", true)]
    internal class HostTopologyInternal : IDisposable
    {
        public IntPtr m_Ptr;
        public HostTopologyInternal(HostTopology topology)
        {
            ConnectionConfigInternal config = new ConnectionConfigInternal(topology.DefaultConfig);
            m_Ptr = InternalCreate(config, topology.MaxDefaultConnections);
            for (int i = 1; i <= topology.SpecialConnectionConfigsCount; ++i)
            {
                ConnectionConfig conf = topology.GetSpecialConnectionConfig(i);
                ConnectionConfigInternal confInt = new ConnectionConfigInternal(conf);
                AddSpecialConnectionConfig(confInt);
            }
            this.ReceivedMessagePoolSize = topology.ReceivedMessagePoolSize;
            this.SentMessagePoolSize = topology.SentMessagePoolSize;
            this.MessagePoolSizeGrowthFactor = topology.MessagePoolSizeGrowthFactor;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Ptr != IntPtr.Zero)
            {
                InternalDestroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        ~HostTopologyInternal()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                InternalDestroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        private static extern IntPtr InternalCreate(ConnectionConfigInternal config, int maxDefaultConnections);
        [NativeMethod(IsThreadSafe = true)]
        private static extern void InternalDestroy(IntPtr ptr);
        public extern ushort AddSpecialConnectionConfig(ConnectionConfigInternal config);
        [NativeProperty("m_ReceivedMessagePoolSize", TargetType.Field)] private extern ushort ReceivedMessagePoolSize { set; }
        [NativeProperty("m_SentMessagePoolSize", TargetType.Field)] private extern ushort SentMessagePoolSize { set; }
        [NativeProperty("m_MessagePoolSizeGrowthFactor", TargetType.Field)] private extern float MessagePoolSizeGrowthFactor { set; }
    }

    [NativeHeader("Runtime/Networking/UNETConfiguration.h")]
    [NativeConditional("ENABLE_NETWORK && ENABLE_UNET", true)]
    internal class ConnectionSimulatorConfigInternal : IDisposable
    {
        public IntPtr m_Ptr;
        public ConnectionSimulatorConfigInternal(ConnectionSimulatorConfig config)
        {
            m_Ptr = InternalCreate(config.m_OutMinDelay, config.m_OutAvgDelay, config.m_InMinDelay, config.m_InAvgDelay, config.m_PacketLossPercentage);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Ptr != IntPtr.Zero)
            {
                InternalDestroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        ~ConnectionSimulatorConfigInternal()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                InternalDestroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        private static extern IntPtr InternalCreate(int outMinDelay, int outAvgDelay, int inMinDelay, int inAvgDelay, float packetLossPercentage);
        [NativeMethod(IsThreadSafe = true)]
        private static extern void InternalDestroy(IntPtr ptr);
    }

    [NativeHeader("Runtime/Networking/UNETConfiguration.h")]
    [NativeConditional("ENABLE_NETWORK && ENABLE_UNET", true)]
    internal class GlobalConfigInternal : IDisposable
    {
        public IntPtr m_Ptr;
        public GlobalConfigInternal(GlobalConfig config)
        {
            if (config == null)
                throw new NullReferenceException("config is not defined");
            m_Ptr = InternalCreate();
            this.ThreadAwakeTimeout = config.ThreadAwakeTimeout;
            this.ReactorModel = (byte)config.ReactorModel;
            this.ReactorMaximumReceivedMessages = config.ReactorMaximumReceivedMessages;
            this.ReactorMaximumSentMessages = config.ReactorMaximumSentMessages;
            this.MaxPacketSize = config.MaxPacketSize;
            this.MaxHosts = config.MaxHosts;
            if (config.ThreadPoolSize == 0 || config.ThreadPoolSize > 254)
                throw new ArgumentOutOfRangeException("Worker thread pool size should be >= 1 && < 254 (for server only)");
            byte threadPoolSize = config.ThreadPoolSize;
            if (config.ThreadPoolSize > 1)
            {
                Debug.LogWarning("Worker thread pool size can be > 1 only for server platforms: Win, OSX or Linux");
                threadPoolSize = 1;
            }
            this.ThreadPoolSize = threadPoolSize;
            this.MinTimerTimeout = config.MinTimerTimeout;
            this.MaxTimerTimeout = config.MaxTimerTimeout;
            this.MinNetSimulatorTimeout = config.MinNetSimulatorTimeout;
            this.MaxNetSimulatorTimeout = config.MaxNetSimulatorTimeout;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Ptr != IntPtr.Zero)
            {
                InternalDestroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        ~GlobalConfigInternal()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                InternalDestroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        private static extern IntPtr InternalCreate();
        [NativeMethod(IsThreadSafe = true)]
        private static extern void InternalDestroy(IntPtr ptr);

        [NativeProperty("m_ThreadAwakeTimeout", TargetType.Field)] private extern uint ThreadAwakeTimeout { set; }
        [NativeProperty("m_ReactorModel", TargetType.Field)] private extern byte ReactorModel { set; }
        [NativeProperty("m_ReactorMaximumReceivedMessages", TargetType.Field)] private extern ushort ReactorMaximumReceivedMessages { set; }
        [NativeProperty("m_ReactorMaximumSentMessages", TargetType.Field)] private extern ushort ReactorMaximumSentMessages { set; }
        [NativeProperty("m_MaxPacketSize", TargetType.Field)] private extern ushort MaxPacketSize { set; }
        [NativeProperty("m_MaxHosts", TargetType.Field)] private extern ushort MaxHosts { set; }
        [NativeProperty("m_ThreadPoolSize", TargetType.Field)] private extern byte ThreadPoolSize { set; }

        [NativeProperty("m_MinTimerTimeout", TargetType.Field)] private extern uint MinTimerTimeout { set; }
        [NativeProperty("m_MaxTimerTimeout", TargetType.Field)] private extern uint MaxTimerTimeout { set; }
        [NativeProperty("m_MinNetSimulatorTimeout", TargetType.Field)] private extern uint MinNetSimulatorTimeout { set; }
        [NativeProperty("m_MaxNetSimulatorTimeout", TargetType.Field)] private extern uint MaxNetSimulatorTimeout { set; }
    }

#pragma warning restore 618
}
