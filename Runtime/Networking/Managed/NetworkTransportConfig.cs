// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//part of user api

using System;
using UnityEngineInternal;
using System.Collections.Generic;

namespace UnityEngine.Networking
{
    public enum NetworkEventType
    {
        DataEvent = 0,
        ConnectEvent = 1,
        DisconnectEvent = 2,
        Nothing = 3,
        BroadcastEvent = 4
    }

    public enum QosType
    {
        Unreliable = 0,
        UnreliableFragmented = 1,
        UnreliableSequenced = 2,
        Reliable = 3,
        ReliableFragmented = 4,
        ReliableSequenced = 5,
        StateUpdate = 6,
        ReliableStateUpdate = 7,
        AllCostDelivery = 8,
        UnreliableFragmentedSequenced = 9,
        ReliableFragmentedSequenced = 10
    }

    public enum NetworkError
    {
        Ok = 0x0, //everything good
        WrongHost, //host id mismatch or host not ready yet
        WrongConnection, //connection id mismatch or connection is has not connected yet
        WrongChannel, //channel id mismatch
        NoResources, //system doesn't have resources to accomplish operation
        BadMessage, //received message is malformed
        Timeout,
        //system cannot accomplish operation in time and connection will be closed, or connection to slow, or % of drop packet is high
        MessageToLong,
        //sending message it too long for sending channel qos or providing buffer is to small for receiving message
        WrongOperation, //attempt to start sending message to group before previous sending has not finished
        VersionMismatch, //library version are different for this connection
        CRCMismatch, //quality of service mismatch for this connection
        DNSFailure, //hostname was not able to be resolved to an IP address
        UsageError //if any function is called with the wrong type of arguments or sizes
    }

    public enum ReactorModel
    {
        SelectReactor = 0,
        FixRateReactor = 1
    }

    public enum ConnectionAcksType
    {
        Acks32 = 1,
        Acks64 = 2,
        Acks96 = 3,
        Acks128 = 4
    };

    [Serializable]
    public class ChannelQOS
    {
        [SerializeField]
        internal QosType m_Type;

        public ChannelQOS(QosType value)
        {
            m_Type = value;
        }

        public ChannelQOS()
        {
            m_Type = QosType.Unreliable;
        }

        public ChannelQOS(ChannelQOS channel)
        {
            if (channel == null)
                throw new NullReferenceException("channel is not defined");
            m_Type = channel.m_Type;
        }

        public QosType QOS { get { return m_Type; } }
    }

    //connection configuration proxy class
    //contains connection profile + channel definitions
    //to allow user manipulate channel info via property with parameter (using array interface)
    //all fields are defined for direct access from HLAPI (it it needed)
    [Serializable]
    public class ConnectionConfig
    {
        private  const int g_MinPacketSize = 128;
        [SerializeField]
        private ushort m_PacketSize;
        [SerializeField]
        private ushort m_FragmentSize;
        [SerializeField]
        private uint m_ResendTimeout;
        [SerializeField]
        private uint m_DisconnectTimeout;
        [SerializeField]
        private uint m_ConnectTimeout;
        [SerializeField]
        private uint m_MinUpdateTimeout;
        [SerializeField]
        private uint m_PingTimeout;
        [SerializeField]
        private uint m_ReducedPingTimeout;
        [SerializeField]
        private uint m_AllCostTimeout;
        [SerializeField]
        private byte m_NetworkDropThreshold;
        [SerializeField]
        private byte m_OverflowDropThreshold;
        [SerializeField]
        private byte m_MaxConnectionAttempt;
        [SerializeField]
        private uint m_AckDelay;
        [SerializeField]
        private uint m_SendDelay;
        [SerializeField]
        private ushort m_MaxCombinedReliableMessageSize;
        [SerializeField]
        private ushort m_MaxCombinedReliableMessageCount;
        [SerializeField]
        private ushort m_MaxSentMessageQueueSize;
        [SerializeField]
        private ConnectionAcksType m_AcksType;
        [SerializeField]
        private bool m_UsePlatformSpecificProtocols;
        [SerializeField]
        private uint m_InitialBandwidth;
        [SerializeField]
        private float m_BandwidthPeakFactor;
        [SerializeField]
        private ushort m_WebSocketReceiveBufferMaxSize;
        [SerializeField]
        private uint m_UdpSocketReceiveBufferMaxSize;
        [SerializeField]
        private string m_SSLCertFilePath;
        [SerializeField]
        private string m_SSLPrivateKeyFilePath;
        [SerializeField]
        private string m_SSLCAFilePath;
        [SerializeField]
        internal List<ChannelQOS> m_Channels = new List<ChannelQOS>();

        public ConnectionConfig()
        {
            m_PacketSize = 1440;
            m_FragmentSize = 500;
            m_ResendTimeout = 1200;
            m_DisconnectTimeout = 2000;
            m_ConnectTimeout = 2000;
            m_MinUpdateTimeout = 10;
            m_PingTimeout = 500;
            m_ReducedPingTimeout = 100;
            m_AllCostTimeout = 20;
            m_NetworkDropThreshold = 5;
            m_OverflowDropThreshold = 5;
            m_MaxConnectionAttempt = 10;
            m_AckDelay = 33;
            m_SendDelay = 10;
            m_MaxCombinedReliableMessageSize = 100;
            m_MaxCombinedReliableMessageCount = 10;
            m_MaxSentMessageQueueSize = 512;
            m_AcksType = ConnectionAcksType.Acks32;
            m_UsePlatformSpecificProtocols = false;
            m_InitialBandwidth = 0;
            m_BandwidthPeakFactor = 2.0f;
            m_WebSocketReceiveBufferMaxSize = 0;
            m_UdpSocketReceiveBufferMaxSize = 0;
            m_SSLCertFilePath = null;
            m_SSLPrivateKeyFilePath = null;
            m_SSLCAFilePath = null;
        }

        public ConnectionConfig(ConnectionConfig config)
        {
            if (config == null)
                throw new NullReferenceException("config is not defined");
            m_PacketSize = config.m_PacketSize;
            m_FragmentSize = config.m_FragmentSize;
            m_ResendTimeout = config.m_ResendTimeout;
            m_DisconnectTimeout = config.m_DisconnectTimeout;
            m_ConnectTimeout = config.m_ConnectTimeout;
            m_MinUpdateTimeout = config.m_MinUpdateTimeout;
            m_PingTimeout = config.m_PingTimeout;
            m_ReducedPingTimeout = config.m_ReducedPingTimeout;
            m_AllCostTimeout = config.m_AllCostTimeout;
            m_NetworkDropThreshold = config.m_NetworkDropThreshold;
            m_OverflowDropThreshold = config.m_OverflowDropThreshold;
            m_MaxConnectionAttempt = config.m_MaxConnectionAttempt;
            m_AckDelay = config.m_AckDelay;
            m_SendDelay = config.m_SendDelay;
            m_MaxCombinedReliableMessageSize = config.MaxCombinedReliableMessageSize;
            m_MaxCombinedReliableMessageCount = config.m_MaxCombinedReliableMessageCount;
            m_MaxSentMessageQueueSize = config.m_MaxSentMessageQueueSize;
            m_AcksType = config.m_AcksType;
            m_UsePlatformSpecificProtocols = config.m_UsePlatformSpecificProtocols;
            m_InitialBandwidth = config.m_InitialBandwidth;
            if (m_InitialBandwidth == 0)
            {
                m_InitialBandwidth = (uint)m_PacketSize * 1000 / m_MinUpdateTimeout;
            }
            m_BandwidthPeakFactor = config.m_BandwidthPeakFactor;
            m_WebSocketReceiveBufferMaxSize = config.m_WebSocketReceiveBufferMaxSize;
            m_UdpSocketReceiveBufferMaxSize = config.m_UdpSocketReceiveBufferMaxSize;
            m_SSLCertFilePath = config.m_SSLCertFilePath;
            m_SSLPrivateKeyFilePath = config.m_SSLPrivateKeyFilePath;
            m_SSLCAFilePath = config.m_SSLCAFilePath;
            foreach (var channel in config.m_Channels)
            {
                m_Channels.Add(new ChannelQOS(channel));
            }
        }

        public static void Validate(ConnectionConfig config)
        {
            if (config.m_PacketSize < g_MinPacketSize) //rough estimation of largest header, as it is defined in c++; we do not have access to it
                throw new ArgumentOutOfRangeException("PacketSize should be > " + g_MinPacketSize.ToString());
            if (config.m_FragmentSize >= config.m_PacketSize - g_MinPacketSize)
                throw new ArgumentOutOfRangeException("FragmentSize should be < PacketSize - " + g_MinPacketSize.ToString());
            if (config.m_Channels.Count > 255)
                throw new ArgumentOutOfRangeException("Channels number should be less than 256");
        }

        //properties, it should be defines because set methods (which has not implemented yet) should check parameters correctnesses
        public ushort PacketSize
        {
            get { return m_PacketSize; }
            set
            {
                m_PacketSize = value;
            }
        }

        public ushort FragmentSize
        {
            get { return m_FragmentSize; }
            set { m_FragmentSize = value; }
        }

        public uint ResendTimeout
        {
            get { return m_ResendTimeout; }
            set { m_ResendTimeout = value; }
        }

        public uint DisconnectTimeout
        {
            get { return m_DisconnectTimeout; }
            set { m_DisconnectTimeout = value; }
        }

        public uint ConnectTimeout
        {
            get { return m_ConnectTimeout; }
            set { m_ConnectTimeout = value; }
        }

        public uint MinUpdateTimeout
        {
            get { return m_MinUpdateTimeout; }
            set
            {
                if (value == 0)
                    throw new ArgumentOutOfRangeException("Minimal update timeout should be > 0");
                m_MinUpdateTimeout = value;
            }
        }

        public uint PingTimeout
        {
            get { return m_PingTimeout; }
            set { m_PingTimeout = value; }
        }

        public uint ReducedPingTimeout
        {
            get { return m_ReducedPingTimeout; }
            set { m_ReducedPingTimeout = value; }
        }

        public uint AllCostTimeout
        {
            get { return m_AllCostTimeout; }
            set { m_AllCostTimeout = value; }
        }

        public byte NetworkDropThreshold
        {
            get { return m_NetworkDropThreshold; }
            set { m_NetworkDropThreshold = value; }
        }

        public byte OverflowDropThreshold
        {
            get { return m_OverflowDropThreshold; }
            set { m_OverflowDropThreshold = value; }
        }

        public byte MaxConnectionAttempt
        {
            get { return m_MaxConnectionAttempt; }
            set { m_MaxConnectionAttempt = value; }
        }

        public uint AckDelay
        {
            get { return m_AckDelay; }
            set { m_AckDelay = value; }
        }

        public uint SendDelay
        {
            get { return m_SendDelay; }
            set { m_SendDelay = value; }
        }

        public ushort MaxCombinedReliableMessageSize
        {
            get { return m_MaxCombinedReliableMessageSize; }
            set { m_MaxCombinedReliableMessageSize = value; }
        }

        public ushort MaxCombinedReliableMessageCount
        {
            get { return m_MaxCombinedReliableMessageCount; }
            set { m_MaxCombinedReliableMessageCount = value; }
        }

        public ushort MaxSentMessageQueueSize
        {
            get { return m_MaxSentMessageQueueSize; }
            set { m_MaxSentMessageQueueSize = value; }
        }

        public ConnectionAcksType AcksType
        {
            get { return m_AcksType; }
            set { m_AcksType = value; }
        }

        [Obsolete("IsAcksLong is deprecated. Use AcksType = ConnectionAcksType.Acks64", false)]
        public bool IsAcksLong
        {
            get { return m_AcksType != ConnectionAcksType.Acks32; }
            set
            {
                if (value && m_AcksType == ConnectionAcksType.Acks32)
                    m_AcksType = ConnectionAcksType.Acks64;
                else if (!value)
                    m_AcksType = ConnectionAcksType.Acks32;
            }
        }

        public bool UsePlatformSpecificProtocols
        {
            get { return m_UsePlatformSpecificProtocols; }
            set
            {
                if ((value == true) && (UnityEngine.Application.platform != RuntimePlatform.PS4) && (UnityEngine.Application.platform != RuntimePlatform.PSP2))
                    throw new ArgumentOutOfRangeException("Platform specific protocols are not supported on this platform");

                m_UsePlatformSpecificProtocols = value;
            }
        }

        public uint InitialBandwidth
        {
            get { return m_InitialBandwidth; }
            set { m_InitialBandwidth = value; }
        }

        public float BandwidthPeakFactor
        {
            get { return m_BandwidthPeakFactor; }
            set { m_BandwidthPeakFactor = value; }
        }

        public ushort WebSocketReceiveBufferMaxSize
        {
            get { return m_WebSocketReceiveBufferMaxSize; }
            set { m_WebSocketReceiveBufferMaxSize = value; }
        }
        public uint UdpSocketReceiveBufferMaxSize
        {
            get { return m_UdpSocketReceiveBufferMaxSize; }
            set { m_UdpSocketReceiveBufferMaxSize = value; }
        }

        public string SSLCertFilePath
        {
            get { return m_SSLCertFilePath; }
            set { m_SSLCertFilePath = value; }
        }

        public string SSLPrivateKeyFilePath
        {
            get { return m_SSLPrivateKeyFilePath; }
            set { m_SSLPrivateKeyFilePath = value; }
        }

        public string SSLCAFilePath
        {
            get { return m_SSLCAFilePath; }
            set { m_SSLCAFilePath = value; }
        }

        //channels
        public int ChannelCount
        {
            get { return m_Channels.Count; }
        }

        public byte AddChannel(QosType value)
        {
            if (m_Channels.Count > 255)
                throw new ArgumentOutOfRangeException("Channels Count should be less than 256");
            if (!Enum.IsDefined(typeof(QosType), value))
            {
                throw new ArgumentOutOfRangeException("requested qos type doesn't exist: " + (int)value);
            }
            ChannelQOS channel = new ChannelQOS(value);
            m_Channels.Add(channel);
            return (byte)(m_Channels.Count - 1);
        }

        public QosType GetChannel(byte idx)
        {
            if (idx >= m_Channels.Count)
                throw new ArgumentOutOfRangeException("requested index greater than maximum channels count");
            return m_Channels[idx].QOS;
        }

        public List<ChannelQOS> Channels { get { return m_Channels; } }
    }

    //--- topology described current network topology for the host
    //it includes maximum default connections with them configs (parameters + channels configuration)
    //and array of special connection (with special configuration)
    //AddSpecialConnection will return connection id which user should use when he call connect to identify special configuration for this connection
    [Serializable]
    public class HostTopology
    {
        [SerializeField]
        private ConnectionConfig m_DefConfig = null;
        [SerializeField]
        private int m_MaxDefConnections = 0;
        [SerializeField]
        private List<ConnectionConfig> m_SpecialConnections = new List<ConnectionConfig>();
        [SerializeField]
        private ushort m_ReceivedMessagePoolSize = 1024;   //TODO: default values should be initialized from c++ default constants
        [SerializeField]
        private ushort m_SentMessagePoolSize = 1024;
        [SerializeField]
        private float m_MessagePoolSizeGrowthFactor = 0.75f;

        public HostTopology(ConnectionConfig defaultConfig, int maxDefaultConnections)
        {
            if (defaultConfig == null)
                throw new NullReferenceException("config is not defined");
            if (maxDefaultConnections <= 0)
                throw new ArgumentOutOfRangeException("maxConnections", "Number of connections should be > 0");
            if (maxDefaultConnections >= 65535)
                throw new ArgumentOutOfRangeException("maxConnections", "Number of connections should be < 65535");
            ConnectionConfig.Validate(defaultConfig);
            m_DefConfig = new ConnectionConfig(defaultConfig);
            m_MaxDefConnections = maxDefaultConnections;
        }

        private HostTopology() {}

        public ConnectionConfig DefaultConfig
        {
            get { return m_DefConfig; }
        }
        public int MaxDefaultConnections
        {
            get { return m_MaxDefConnections; }
        }
        public int SpecialConnectionConfigsCount
        {
            get { return m_SpecialConnections.Count; }
        }

        public List<ConnectionConfig> SpecialConnectionConfigs
        {
            get { return m_SpecialConnections; }
        }
        public ConnectionConfig GetSpecialConnectionConfig(int i)
        {
            if (i > m_SpecialConnections.Count || i == 0)
                throw new ArgumentException("special configuration index is out of valid range");
            return m_SpecialConnections[i - 1];
        }

        public ushort ReceivedMessagePoolSize
        {
            get { return m_ReceivedMessagePoolSize; }
            set { m_ReceivedMessagePoolSize = value; }
        }

        public ushort SentMessagePoolSize
        {
            get { return m_SentMessagePoolSize; }
            set { m_SentMessagePoolSize = value; }
        }

        public float MessagePoolSizeGrowthFactor
        {
            get { return m_MessagePoolSizeGrowthFactor; }
            set
            {
                if (value <= 0.5 || value > 1.0)
                    throw new ArgumentException("pool growth factor should be varied between 0.5 and 1.0");
                m_MessagePoolSizeGrowthFactor = value;
            }
        }

        public int AddSpecialConnectionConfig(ConnectionConfig config)
        {
            if (m_MaxDefConnections + m_SpecialConnections.Count + 1 >= 65535)
                throw new ArgumentOutOfRangeException("maxConnections", "Number of connections should be < 65535");
            m_SpecialConnections.Add(new ConnectionConfig(config));
            return m_SpecialConnections.Count;
        }
    }

    [Serializable]
    public class GlobalConfig
    {
        private const uint g_MaxTimerTimeout = 12000;         //before changing check UNETConfiguration.h file
        private const uint g_MaxNetSimulatorTimeout = 12000;
        private const ushort g_MaxHosts = 128;
        [SerializeField]
        private uint   m_ThreadAwakeTimeout;
        [SerializeField]
        private ReactorModel m_ReactorModel;
        [SerializeField]
        private ushort m_ReactorMaximumReceivedMessages;
        [SerializeField]
        private ushort m_ReactorMaximumSentMessages;
        [SerializeField]
        private ushort m_MaxPacketSize;
        [SerializeField]
        private ushort m_MaxHosts;
        [SerializeField]
        private byte m_ThreadPoolSize;
        [SerializeField]
        private uint m_MinTimerTimeout;
        [SerializeField]
        private uint m_MaxTimerTimeout;
        [SerializeField]
        private uint m_MinNetSimulatorTimeout;
        [SerializeField]
        private uint m_MaxNetSimulatorTimeout;

        public GlobalConfig()
        {
            m_ThreadAwakeTimeout             = 1; //1 ms
            m_ReactorModel                   = ReactorModel.SelectReactor;
            m_ReactorMaximumReceivedMessages = 1024;
            m_ReactorMaximumSentMessages     = 1024;
            m_MaxPacketSize                  = 2000; //2K
            m_MaxHosts                       = 16;
            m_ThreadPoolSize                 = 1;
            m_MinTimerTimeout                = 1;
            m_MaxTimerTimeout                = g_MaxTimerTimeout;
            m_MinNetSimulatorTimeout         = 1;
            m_MaxNetSimulatorTimeout         = g_MaxNetSimulatorTimeout;
        }

        public uint ThreadAwakeTimeout
        {
            get { return m_ThreadAwakeTimeout; }
            set
            {
                if (value == 0)
                    throw new ArgumentOutOfRangeException("Minimal thread awake timeout should be > 0");
                m_ThreadAwakeTimeout = value;
            }
        }

        public ReactorModel ReactorModel
        {
            get { return m_ReactorModel; }
            set { m_ReactorModel = value; }
        }

        public ushort ReactorMaximumReceivedMessages
        {
            get { return m_ReactorMaximumReceivedMessages; }
            set { m_ReactorMaximumReceivedMessages = value; }
        }

        public ushort ReactorMaximumSentMessages
        {
            get { return m_ReactorMaximumSentMessages; }
            set { m_ReactorMaximumSentMessages = value; }
        }

        public ushort MaxPacketSize
        {
            get { return m_MaxPacketSize; }
            set { m_MaxPacketSize = value; }
        }

        public ushort MaxHosts
        {
            get { return m_MaxHosts; }
            set
            {
                if (value == 0)
                    throw new ArgumentOutOfRangeException("MaxHosts", "Maximum hosts number should be > 0");
                if (value > g_MaxHosts)
                    throw new ArgumentOutOfRangeException("MaxHosts", "Maximum hosts number should be <= " + g_MaxHosts.ToString());
                m_MaxHosts = value;
            }
        }

        public byte ThreadPoolSize
        {
            get { return m_ThreadPoolSize; }
            set { m_ThreadPoolSize = value; }
        }
        public uint MinTimerTimeout
        {
            get { return m_MinTimerTimeout; }
            set
            {
                if (value > MaxTimerTimeout)
                    throw new ArgumentOutOfRangeException("MinTimerTimeout should be < MaxTimerTimeout");
                if (value == 0)
                    throw new ArgumentOutOfRangeException("MinTimerTimeout should be > 0");
                m_MinTimerTimeout = value;
            }
        }
        public uint MaxTimerTimeout
        {
            get { return m_MaxTimerTimeout; }
            set
            {
                if (value == 0)
                    throw new ArgumentOutOfRangeException("MaxTimerTimeout should be > 0");
                if (value > g_MaxTimerTimeout)
                    throw new ArgumentOutOfRangeException("MaxTimerTimeout should be <=" + g_MaxTimerTimeout.ToString());
                m_MaxTimerTimeout = value;
            }
        }

        public uint MinNetSimulatorTimeout
        {
            get { return m_MinNetSimulatorTimeout; }
            set
            {
                if (value > MaxNetSimulatorTimeout)
                    throw new ArgumentOutOfRangeException("MinNetSimulatorTimeout should be < MaxTimerTimeout");
                if (value == 0)
                    throw new ArgumentOutOfRangeException("MinNetSimulatorTimeout should be > 0");
                m_MinNetSimulatorTimeout = value;
            }
        }
        public uint MaxNetSimulatorTimeout
        {
            get { return m_MaxNetSimulatorTimeout; }
            set
            {
                if (value == 0)
                    throw new ArgumentOutOfRangeException("MaxNetSimulatorTimeout should be > 0");
                if (value > g_MaxNetSimulatorTimeout)
                    throw new ArgumentOutOfRangeException("MaxNetSimulatorTimeout should be <=" + g_MaxNetSimulatorTimeout.ToString());
                m_MaxNetSimulatorTimeout = value;
            }
        }
    }
}
