// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngineInternal;
using UnityEngine.Networking.Types;
using System.Collections.Generic;
using System.Net.Sockets;

namespace UnityEngine.Networking
{


public sealed partial class ConnectionSimulatorConfig : IDisposable
{
    
            #pragma warning disable 649 
            internal IntPtr m_Ptr;
            #pragma warning restore 649
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public ConnectionSimulatorConfig (int outMinDelay, int outAvgDelay, int inMinDelay, int inAvgDelay, float packetLossPercentage) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Dispose () ;

    ~ConnectionSimulatorConfig() { Dispose(); }
}

internal sealed partial class ConnectionConfigInternal : IDisposable
{
    
            #pragma warning disable 649 
            internal IntPtr m_Ptr;
            #pragma warning restore 649
    
    
    private ConnectionConfigInternal()
        {}
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitWrapper () ;

    public ConnectionConfigInternal(ConnectionConfig config)
        {
            if (config == null)
                throw new NullReferenceException("config is not defined");
            InitWrapper();
            InitPacketSize(config.PacketSize);
            InitFragmentSize(config.FragmentSize);
            InitResendTimeout(config.ResendTimeout);
            InitDisconnectTimeout(config.DisconnectTimeout);
            InitConnectTimeout(config.ConnectTimeout);
            InitMinUpdateTimeout(config.MinUpdateTimeout);
            InitPingTimeout(config.PingTimeout);
            InitReducedPingTimeout(config.ReducedPingTimeout);
            InitAllCostTimeout(config.AllCostTimeout);
            InitNetworkDropThreshold(config.NetworkDropThreshold);
            InitOverflowDropThreshold(config.OverflowDropThreshold);
            InitMaxConnectionAttempt(config.MaxConnectionAttempt);
            InitAckDelay(config.AckDelay);
            InitSendDelay(config.SendDelay);
            InitMaxCombinedReliableMessageSize(config.MaxCombinedReliableMessageSize);
            InitMaxCombinedReliableMessageCount(config.MaxCombinedReliableMessageCount);
            InitMaxSentMessageQueueSize(config.MaxSentMessageQueueSize);
            InitAcksType((int)config.AcksType);
            InitUsePlatformSpecificProtocols(config.UsePlatformSpecificProtocols);
            InitInitialBandwidth(config.InitialBandwidth);
            InitBandwidthPeakFactor(config.BandwidthPeakFactor);
            InitWebSocketReceiveBufferMaxSize(config.WebSocketReceiveBufferMaxSize);
            InitUdpSocketReceiveBufferMaxSize(config.UdpSocketReceiveBufferMaxSize);
            if (config.SSLCertFilePath != null)
            {
                int len = InitSSLCertFilePath(config.SSLCertFilePath);
                if (len != 0)
                    throw new ArgumentOutOfRangeException("SSLCertFilePath cannot be > than " + len.ToString());
            }
            if (config.SSLPrivateKeyFilePath != null)
            {
                int len = InitSSLPrivateKeyFilePath(config.SSLPrivateKeyFilePath);
                if (len != 0)
                    throw new ArgumentOutOfRangeException("SSLPrivateKeyFilePath cannot be > than " + len.ToString());
            }
            if (config.SSLCAFilePath != null)
            {
                int len = InitSSLCAFilePath(config.SSLCAFilePath);
                if (len != 0)
                    throw new ArgumentOutOfRangeException("SSLCAFilePath cannot be > than " + len.ToString());
            }


            for (byte i = 0; i < config.ChannelCount; ++i)
            {
                AddChannel(config.GetChannel(i));
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public byte AddChannel (QosType value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public QosType GetChannel (int i) ;

    public extern  int ChannelSize
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitPacketSize (ushort value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitFragmentSize (ushort value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitResendTimeout (uint value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitDisconnectTimeout (uint value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitConnectTimeout (uint value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitMinUpdateTimeout (uint value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitPingTimeout (uint value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitReducedPingTimeout (uint value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitAllCostTimeout (uint value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitNetworkDropThreshold (byte value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitOverflowDropThreshold (byte value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitMaxConnectionAttempt (byte value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitAckDelay (uint value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitSendDelay (uint value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitMaxCombinedReliableMessageSize (ushort value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitMaxCombinedReliableMessageCount (ushort value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitMaxSentMessageQueueSize (ushort value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitAcksType (int value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitUsePlatformSpecificProtocols (bool value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitInitialBandwidth (uint value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitBandwidthPeakFactor (float value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitWebSocketReceiveBufferMaxSize (ushort value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitUdpSocketReceiveBufferMaxSize (uint value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int InitSSLCertFilePath (string value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int InitSSLPrivateKeyFilePath (string value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int InitSSLCAFilePath (string value) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Dispose () ;

    ~ConnectionConfigInternal() { Dispose(); }
}

internal sealed partial class HostTopologyInternal : IDisposable
{
    
            #pragma warning disable 649 
            internal IntPtr m_Ptr;
            #pragma warning restore 649
    
    
    public HostTopologyInternal(HostTopology topology)
        {
            ConnectionConfigInternal config = new ConnectionConfigInternal(topology.DefaultConfig);
            InitWrapper(config, topology.MaxDefaultConnections);
            for (int i = 1; i <= topology.SpecialConnectionConfigsCount; ++i)
            {
                ConnectionConfig conf = topology.GetSpecialConnectionConfig(i);
                ConnectionConfigInternal confInt = new ConnectionConfigInternal(conf);
                AddSpecialConnectionConfig(confInt);
            }
            InitOtherParameters(topology);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitWrapper (ConnectionConfigInternal config, int maxDefaultConnections) ;

    int AddSpecialConnectionConfig(ConnectionConfigInternal config)
        {
            return AddSpecialConnectionConfigWrapper(config);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int AddSpecialConnectionConfigWrapper (ConnectionConfigInternal config) ;

    void InitOtherParameters(HostTopology topology)
        {
            InitReceivedPoolSize(topology.ReceivedMessagePoolSize);
            InitSentMessagePoolSize(topology.SentMessagePoolSize);
            InitMessagePoolSizeGrowthFactor(topology.MessagePoolSizeGrowthFactor);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitReceivedPoolSize (ushort pool) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitSentMessagePoolSize (ushort pool) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitMessagePoolSizeGrowthFactor (float factor) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Dispose () ;

    ~HostTopologyInternal() { Dispose(); }
}

internal sealed partial class GlobalConfigInternal : IDisposable
{
    
            #pragma warning disable 649 
            internal IntPtr m_Ptr;
            #pragma warning restore 649
    
    
    public GlobalConfigInternal(GlobalConfig config)
        {
            InitWrapper();
            InitThreadAwakeTimeout(config.ThreadAwakeTimeout);
            InitReactorModel((byte)config.ReactorModel);
            InitReactorMaximumReceivedMessages(config.ReactorMaximumReceivedMessages);
            InitReactorMaximumSentMessages(config.ReactorMaximumSentMessages);
            InitMaxPacketSize(config.MaxPacketSize);
            InitMaxHosts(config.MaxHosts);
            if (config.ThreadPoolSize == 0 || config.ThreadPoolSize > 254)
                throw new ArgumentOutOfRangeException("Worker thread pool size should be >= 1 && < 254 (for server only)");
            InitThreadPoolSize(config.ThreadPoolSize);
            InitMinTimerTimeout(config.MinTimerTimeout);
            InitMaxTimerTimeout(config.MaxTimerTimeout);
            InitMinNetSimulatorTimeout(config.MinNetSimulatorTimeout);
            InitMaxNetSimulatorTimeout(config.MaxNetSimulatorTimeout);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitWrapper () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitThreadAwakeTimeout (uint ms) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitReactorModel (byte model) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitReactorMaximumReceivedMessages (ushort size) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitReactorMaximumSentMessages (ushort size) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitMaxPacketSize (ushort size) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitMaxHosts (ushort size) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitThreadPoolSize (byte size) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitMinTimerTimeout (uint ms) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitMaxTimerTimeout (uint ms) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitMinNetSimulatorTimeout (uint ms) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitMaxNetSimulatorTimeout (uint ms) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Dispose () ;

    ~GlobalConfigInternal() { Dispose(); }
}

public sealed partial class NetworkTransport
{
    private NetworkTransport() {}
    
    
    static public void Init()
        {
            InitWithNoParameters();
        }
    
    
    static public void Init(GlobalConfig config)
        {
            InitWithParameters(new GlobalConfigInternal(config));
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void InitWithNoParameters () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void InitWithParameters (GlobalConfigInternal config) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Shutdown () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetAssetId (GameObject go) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void AddSceneId (int id) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetNextSceneId () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ConnectAsNetworkHost (int hostId, string address, int port, NetworkID network, SourceID source, NodeID node, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DisconnectNetworkHost (int hostId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  NetworkEventType ReceiveRelayEventFromHost (int hostId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int ConnectToNetworkPeer (int hostId, string address, int port, int exceptionConnectionId, int relaySlotId, NetworkID network, SourceID source, NodeID node, int bytesPerSec, float bucketSizeFactor, out byte error) ;

    public static int ConnectToNetworkPeer(int hostId, string address, int port, int exceptionConnectionId, int relaySlotId, NetworkID network, SourceID source, NodeID node, out byte error)
        {
            return ConnectToNetworkPeer(hostId, address, port, exceptionConnectionId, relaySlotId, network, source, node, 0, 0, out error);
        }
    
    
    
    [Obsolete("GetCurrentIncomingMessageAmount has been deprecated.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetCurrentIncomingMessageAmount () ;

    [Obsolete("GetCurrentOutgoingMessageAmount has been deprecated.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetCurrentOutgoingMessageAmount () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetIncomingMessageQueueSize (int hostId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingMessageQueueSize (int hostId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetCurrentRTT (int hostId, int connectionId, out byte error) ;

    [Obsolete("GetCurrentRtt() has been deprecated.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetCurrentRtt (int hostId, int connectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetIncomingPacketLossCount (int hostId, int connectionId, out byte error) ;

    [Obsolete("GetNetworkLostPacketNum() has been deprecated.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetNetworkLostPacketNum (int hostId, int connectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetIncomingPacketCount (int hostId, int connectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingPacketNetworkLossPercent (int hostId, int connectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingPacketOverflowLossPercent (int hostId, int connectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetMaxAllowedBandwidth (int hostId, int connectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetAckBufferCount (int hostId, int connectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetIncomingPacketDropCountForAllHosts () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetIncomingPacketCountForAllHosts () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingPacketCount () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingPacketCountForHost (int hostId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingPacketCountForConnection (int hostId, int connectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingMessageCount () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingMessageCountForHost (int hostId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingMessageCountForConnection (int hostId, int connectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingUserBytesCount () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingUserBytesCountForHost (int hostId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingUserBytesCountForConnection (int hostId, int connectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingSystemBytesCount () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingSystemBytesCountForHost (int hostId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingSystemBytesCountForConnection (int hostId, int connectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingFullBytesCount () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingFullBytesCountForHost (int hostId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetOutgoingFullBytesCountForConnection (int hostId, int connectionId, out byte error) ;

    [Obsolete("GetPacketSentRate has been deprecated.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetPacketSentRate (int hostId, int connectionId, out byte error) ;

    [Obsolete("GetPacketReceivedRate has been deprecated.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetPacketReceivedRate (int hostId, int connectionId, out byte error) ;

    [Obsolete("GetRemotePacketReceivedRate has been deprecated.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetRemotePacketReceivedRate (int hostId, int connectionId, out byte error) ;

    [Obsolete("GetNetIOTimeuS has been deprecated.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetNetIOTimeuS () ;

    public static void GetConnectionInfo(int hostId, int connectionId, out string address, out int port, out NetworkID network, out NodeID dstNode, out byte error)
        {
            ulong netw;
            ushort node;
            address = GetConnectionInfo(hostId, connectionId, out port, out netw, out node, out error);
            network = (NetworkID)netw;
            dstNode = (NodeID)node;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetConnectionInfo (int hostId, int connectionId, out int port, out ulong network, out ushort dstNode, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetNetworkTimestamp () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetRemoteDelayTimeMS (int hostId, int connectionId, int remoteTime, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool StartSendMulticast (int hostId, int channelId, byte[] buffer, int size, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool SendMulticast (int hostId, int connectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool FinishSendMulticast (int hostId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int GetMaxPacketSize () ;

    private static void CheckTopology(HostTopology topology)
        {
            int maxPacketSize = GetMaxPacketSize();
            if (topology.DefaultConfig.PacketSize > maxPacketSize)
                throw new ArgumentOutOfRangeException("Default config: packet size should be less than packet size defined in global config: " + maxPacketSize.ToString());
            for (int i = 0; i < topology.SpecialConnectionConfigs.Count; ++i)
            {
                if (topology.SpecialConnectionConfigs[i].PacketSize > maxPacketSize)
                {
                    throw new ArgumentOutOfRangeException("Special config " + i.ToString() + ": packet size should be less than packet size defined in global config: " + maxPacketSize.ToString());
                }
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int AddWsHostWrapper (HostTopologyInternal topologyInt, string ip, int port) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int AddWsHostWrapperWithoutIp (HostTopologyInternal topologyInt, int port) ;

    
    private static bool IsPortOpen(string ip, int port)
        {
            TimeSpan timeout = TimeSpan.FromMilliseconds(500);
            string testedEndpoint = (ip == null) ? "127.0.0.1" : ip;
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(testedEndpoint, port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(timeout);

                    if (!success)
                    {
                        return false;
                    }
                    client.EndConnect(result);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    
    
    
    [uei.ExcludeFromDocs]
public static int AddWebsocketHost (HostTopology topology, int port) {
    string ip = null;
    return AddWebsocketHost ( topology, port, ip );
}

public static int AddWebsocketHost(HostTopology topology, int port, [uei.DefaultValue("null")]  string ip )
        {
            if (port != 0)
            {
                if (IsPortOpen(ip, port))
                    throw new InvalidOperationException("Cannot open web socket on port " + port + " It has been already occupied.");
            }
            if (topology == null)
                throw new NullReferenceException("topology is not defined");
            CheckTopology(topology);
            if (ip == null)
                return AddWsHostWrapperWithoutIp(new HostTopologyInternal(topology), port);
            else
                return AddWsHostWrapper(new HostTopologyInternal(topology), ip, port);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int AddHostWrapper (HostTopologyInternal topologyInt, string ip, int port, int minTimeout, int maxTimeout) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int AddHostWrapperWithoutIp (HostTopologyInternal topologyInt, int port, int minTimeout, int maxTimeout) ;

    [uei.ExcludeFromDocs]
public static int AddHost (HostTopology topology, int port ) {
    string ip = null;
    return AddHost ( topology, port, ip );
}

[uei.ExcludeFromDocs]
public static int AddHost (HostTopology topology) {
    string ip = null;
    int port = 0;
    return AddHost ( topology, port, ip );
}

public static int AddHost(HostTopology topology, [uei.DefaultValue("0")]  int port , [uei.DefaultValue("null")]  string ip )
        {
            if (topology == null)
                throw new NullReferenceException("topology is not defined");
            CheckTopology(topology);
            if (ip == null)
                return AddHostWrapperWithoutIp(new HostTopologyInternal(topology), port, 0, 0);
            else
                return AddHostWrapper(new HostTopologyInternal(topology), ip, port, 0, 0);

        }

    
    
    [uei.ExcludeFromDocs]
public static int AddHostWithSimulator (HostTopology topology, int minTimeout, int maxTimeout, int port ) {
    string ip = null;
    return AddHostWithSimulator ( topology, minTimeout, maxTimeout, port, ip );
}

[uei.ExcludeFromDocs]
public static int AddHostWithSimulator (HostTopology topology, int minTimeout, int maxTimeout) {
    string ip = null;
    int port = 0;
    return AddHostWithSimulator ( topology, minTimeout, maxTimeout, port, ip );
}

public static int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, [uei.DefaultValue("0")]  int port , [uei.DefaultValue("null")]  string ip )
        {
            if (topology == null)
                throw new NullReferenceException("topology is not defined");
            if (ip == null)
                return AddHostWrapperWithoutIp(new HostTopologyInternal(topology), port, minTimeout, maxTimeout);
            else
                return AddHostWrapper(new HostTopologyInternal(topology), ip, port, minTimeout, maxTimeout);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool RemoveHost (int hostId) ;

    public extern static bool IsStarted
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int Connect (int hostId, string address, int port, int exeptionConnectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_ConnectEndPoint (int hostId, IntPtr sockAddrStorage, int sockAddrStorageLen, int exceptionConnectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int ConnectWithSimulator (int hostId, string address, int port, int exeptionConnectionId, out byte error, ConnectionSimulatorConfig conf) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool Disconnect (int hostId, int connectionId, out byte error) ;

    public static bool Send(int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error)
        {
            if (buffer == null)
                throw new NullReferenceException("send buffer is not initialized");
            return SendWrapper(hostId, connectionId, channelId, buffer, size, out error);
        }
    
    
    public static bool QueueMessageForSending(int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error)
        {
            if (buffer == null)
                throw new NullReferenceException("send buffer is not initialized");
            return QueueMessageForSendingWrapper(hostId, connectionId, channelId, buffer, size, out error);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool SendQueuedMessages (int hostId, int connectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool SendWrapper (int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool QueueMessageForSendingWrapper (int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool NotifyConnectionSendable (int hostId, int connectionId, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  NetworkEventType Receive (out int hostId, out int connectionId, out int channelId, byte[] buffer, int bufferSize, out int receivedSize, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  NetworkEventType ReceiveFromHost (int hostId, out int connectionId, out int channelId, byte[] buffer, int bufferSize, out int receivedSize, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetPacketStat (int direction, int packetStatId, int numMsgs, int numBytes) ;

    public static bool StartBroadcastDiscovery(int hostId, int broadcastPort, int key, int version, int subversion, byte[] buffer, int size, int timeout, out byte error)
        {
            if (buffer != null)
            {
                if (buffer.Length < size)
                    throw new ArgumentOutOfRangeException("Size: " + size + " > buffer.Length " + buffer.Length);
                if (size == 0)
                    throw new ArgumentOutOfRangeException("Size is zero while buffer exists, please pass null and 0 as buffer and size parameters");
            }
            if (buffer == null)
                return StartBroadcastDiscoveryWithoutData(hostId, broadcastPort, key, version, subversion, timeout, out error);
            else
                return StartBroadcastDiscoveryWithData(hostId, broadcastPort, key, version, subversion, buffer, size, timeout, out error);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool StartBroadcastDiscoveryWithoutData (int hostId, int broadcastPort, int key, int version, int subversion,  int timeout, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool StartBroadcastDiscoveryWithData (int hostId, int broadcastPort, int key, int version, int subversion, byte[] buffer, int size, int timeout, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void StopBroadcastDiscovery () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsBroadcastDiscoveryRunning () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetBroadcastCredentials (int hostId, int key, int version, int subversion, out byte error) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetBroadcastConnectionInfo (int hostId, out int port, out byte error) ;

    public static void GetBroadcastConnectionInfo(int hostId, out string address, out int port, out byte error)
        {
            address = GetBroadcastConnectionInfo(hostId, out port, out error);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void GetBroadcastConnectionMessage (int hostId, byte[] buffer, int bufferSize, out int receivedSize, out byte error) ;

}

}

namespace UnityEngine.WebGL
{
}
