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
using System.Net.Sockets;

namespace UnityEngine.Networking
{

    [NativeHeader("Modules/UNET/UNETManager.h")]
    [NativeHeader("Modules/UNET/UNetTypes.h")]
    [NativeHeader("Modules/UNET/UNETConfiguration.h")]
    [NativeConditional("ENABLE_NETWORK && ENABLE_UNET", true)]
    [Obsolete("The UNET transport will be removed in the future as soon a replacement is ready.")]
    public sealed partial class NetworkTransport
    {
        private NetworkTransport() {}
        static public void Init()
        {
            InitializeClass();
        }

        static public void Init(GlobalConfig config)
        {
            if (config.NetworkEventAvailable != null)
                SetNetworkEventAvailableCallback(config.NetworkEventAvailable);
            if (config.ConnectionReadyForSend != null)
                SetConnectionReadyForSendCallback(config.ConnectionReadyForSend);
            InitializeClassWithConfig(new GlobalConfigInternal(config));
        }

        [FreeFunction("UNETManager::InitializeClass")]
        extern private static void InitializeClass();

        [FreeFunction("UNETManager::InitializeClassWithConfig")]
        extern private static void InitializeClassWithConfig(GlobalConfigInternal config);

        static public void Shutdown()
        {
            Cleanup();
        }

        [Obsolete("This function has been deprecated. Use AssetDatabase utilities instead.")]
        static public string GetAssetId(GameObject go)
        {
            return "";
        }

        static int s_nextSceneId = 1;

        static public void AddSceneId(int id)
        {
            if (id > s_nextSceneId)
            {
                s_nextSceneId = id + 1;
            }
        }

        static public int GetNextSceneId()
        {
            return s_nextSceneId++;
        }

        static public int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, int port, string ip)
        {
            if (topology == null)
                throw new NullReferenceException("topology is not defined");
            CheckTopology(topology);
            return AddHostInternal(new HostTopologyInternal(topology), ip, port, minTimeout, maxTimeout);
        }

        static public int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, int port)
        {
            return AddHostWithSimulator(topology, minTimeout, maxTimeout, port, null);
        }

        static public int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout)
        {
            return AddHostWithSimulator(topology, minTimeout, maxTimeout, 0, null);
        }

        static public int AddHost(HostTopology topology, int port, string ip)
        {
            return AddHostWithSimulator(topology, 0, 0, port, ip);
        }

        static public int AddHost(HostTopology topology, int port)
        {
            return AddHost(topology, port, null);
        }

        static public int AddHost(HostTopology topology)
        {
            return AddHost(topology, 0, null);
        }

        [FreeFunction("UNETManager::Get()->AddHost", ThrowsException = true)]
        extern private static int AddHostInternal(HostTopologyInternal topologyInt, string ip, int port, int minTimeout, int maxTimeout);

        static public int AddWebsocketHost(HostTopology topology, int port, string ip)
        {
            if (port != 0)
            {
                if (IsPortOpen(ip, port))
                    throw new InvalidOperationException("Cannot open web socket on port " + port + " It has been already occupied.");
            }
            if (topology == null)
                throw new NullReferenceException("topology is not defined");
            CheckTopology(topology);
            return AddWsHostInternal(new HostTopologyInternal(topology), ip, port);
        }

        static public int AddWebsocketHost(HostTopology topology, int port)
        {
            return AddWebsocketHost(topology, port, null);
        }

        [FreeFunction("UNETManager::Get()->AddWsHost", ThrowsException = true)]
        extern private static int AddWsHostInternal(HostTopologyInternal topologyInt, string ip, int port);

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

        //Relay ----
        static public void ConnectAsNetworkHost(int hostId, string address, int port, NetworkID network, SourceID source, NodeID node, out byte error)
        {
            ConnectAsNetworkHostInternal(hostId, address, port, (ulong)network, (ulong)source, (ushort)node, out error);
        }

        [FreeFunction("UNETManager::Get()->ConnectAsNetworkHost", ThrowsException = true)]
        extern private static void ConnectAsNetworkHostInternal(int hostId, string address, int port, ulong network, ulong source, ushort node, out byte error);

        [FreeFunction("UNETManager::Get()->DisconnectNetworkHost", ThrowsException = true)]
        extern static public void DisconnectNetworkHost(int hostId, out byte error);

        static public NetworkEventType ReceiveRelayEventFromHost(int hostId, out byte error)
        {
            return (NetworkEventType)ReceiveRelayEventFromHostInternal(hostId, out error);
        }

        [FreeFunction("UNETManager::Get()->PopRelayHostData", ThrowsException = true)]
        extern static private int ReceiveRelayEventFromHostInternal(int hostId, out byte error);

        static public int ConnectToNetworkPeer(int hostId, string address, int port, int exceptionConnectionId, int relaySlotId, NetworkID network, SourceID source, NodeID node, int bytesPerSec, float bucketSizeFactor, out byte error)
        {
            return ConnectToNetworkPeerInternal(hostId, address, port, exceptionConnectionId, relaySlotId, (ulong)network, (ulong)source, (ushort)node, bytesPerSec, bucketSizeFactor, out error);
        }

        static public int ConnectToNetworkPeer(int hostId, string address, int port, int exceptionConnectionId, int relaySlotId, NetworkID network, SourceID source, NodeID node, out byte error)
        {
            return ConnectToNetworkPeer(hostId, address, port, exceptionConnectionId, relaySlotId, network, source, node, 0, 0, out error);
        }

        [FreeFunction("UNETManager::Get()->ConnectToNetworkPeer", ThrowsException = true)]
        extern private static int ConnectToNetworkPeerInternal(int hostId, string address, int port, int exceptionConnectionId, int relaySlotId, ulong network, ulong source, ushort node, int bytesPerSec, float bucketSizeFactor, out byte error);

        //Statistics ----
        [Obsolete("GetCurrentIncomingMessageAmount has been deprecated.")]
        static public int GetCurrentIncomingMessageAmount()
        {
            return 0;
        }

        ///Function can be used to get total amount of messages waiting for reading
        [Obsolete("GetCurrentOutgoingMessageAmount has been deprecated.")]
        static public int GetCurrentOutgoingMessageAmount()
        {
            return 0;
        }

        [FreeFunction("UNETManager::Get()->GetIncomingMessageQueueSize", ThrowsException = true)]
        extern static public int GetIncomingMessageQueueSize(int hostId, out byte error);


        [FreeFunction("UNETManager::Get()->GetOutgoingMessageQueueSize", ThrowsException = true)]
        extern static public int GetOutgoingMessageQueueSize(int hostId, out byte error);

        [FreeFunction("UNETManager::Get()->GetCurrentRTT", ThrowsException = true)]
        extern static public int GetCurrentRTT(int hostId, int connectionId, out byte error);


        [Obsolete("GetCurrentRtt() has been deprecated.")]
        static public int GetCurrentRtt(int hostId, int connectionId, out byte error)
        {
            return GetCurrentRTT(hostId, connectionId, out error);
        }

        [FreeFunction("UNETManager::Get()->GetIncomingPacketLossCount", ThrowsException = true)]
        extern static public int GetIncomingPacketLossCount(int hostId, int connectionId, out byte error);

        [Obsolete("GetNetworkLostPacketNum() has been deprecated.")]
        static public int GetNetworkLostPacketNum(int hostId, int connectionId, out byte error)
        {
            return GetIncomingPacketLossCount(hostId, connectionId, out error);
        }

        [FreeFunction("UNETManager::Get()->GetIncomingPacketCount", ThrowsException = true)]
        extern static public int GetIncomingPacketCount(int hostId, int connectionId, out byte error);

        [FreeFunction("UNETManager::Get()->GetOutgoingPacketNetworkLossPercent", ThrowsException = true)]
        extern static public int GetOutgoingPacketNetworkLossPercent(int hostId, int connectionId, out byte error);

        [FreeFunction("UNETManager::Get()->GetOutgoingPacketOverflowLossPercent", ThrowsException = true)]
        extern static public int GetOutgoingPacketOverflowLossPercent(int hostId, int connectionId, out byte error);

        [FreeFunction("UNETManager::Get()->GetMaxAllowedBandwidth", ThrowsException = true)]
        extern static public int GetMaxAllowedBandwidth(int hostId, int connectionId, out byte error);

        [FreeFunction("UNETManager::Get()->GetAckBufferCount", ThrowsException = true)]
        extern static public int GetAckBufferCount(int hostId, int connectionId, out byte error);

        [FreeFunction("UNETManager::Get()->GetIncomingPacketDropCountForAllHosts", ThrowsException = true)]
        extern static public int GetIncomingPacketDropCountForAllHosts();

        [FreeFunction("UNETManager::Get()->GetIncomingPacketCountForAllHosts", ThrowsException = true)]
        extern static public int GetIncomingPacketCountForAllHosts();

        [FreeFunction("UNETManager::Get()->GetOutgoingPacketCount", ThrowsException = true)]
        extern static public int GetOutgoingPacketCount();

        [FreeFunction("UNETManager::Get()->GetOutgoingPacketCount", ThrowsException = true)]
        extern static public int GetOutgoingPacketCountForHost(int hostId, out byte error);

        [FreeFunction("UNETManager::Get()->GetOutgoingPacketCount", ThrowsException = true)]
        extern static public int GetOutgoingPacketCountForConnection(int hostId, int connectionId, out byte error);

        [FreeFunction("UNETManager::Get()->GetOutgoingMessageCount", ThrowsException = true)]
        extern static public int GetOutgoingMessageCount();

        [FreeFunction("UNETManager::Get()->GetOutgoingMessageCount", ThrowsException = true)]
        extern static public int GetOutgoingMessageCountForHost(int hostId, out byte error);

        [FreeFunction("UNETManager::Get()->GetOutgoingMessageCount", ThrowsException = true)]
        extern static public int GetOutgoingMessageCountForConnection(int hostId, int connectionId, out byte error);

        [FreeFunction("UNETManager::Get()->GetOutgoingUserBytesCount", ThrowsException = true)]
        extern static public int GetOutgoingUserBytesCount();

        [FreeFunction("UNETManager::Get()->GetOutgoingUserBytesCount", ThrowsException = true)]
        extern static public int GetOutgoingUserBytesCountForHost(int hostId, out byte error);

        [FreeFunction("UNETManager::Get()->GetOutgoingUserBytesCount", ThrowsException = true)]
        extern static public int GetOutgoingUserBytesCountForConnection(int hostId, int connectionId, out byte error);

        [FreeFunction("UNETManager::Get()->GetOutgoingSystemBytesCount", ThrowsException = true)]
        extern static public int GetOutgoingSystemBytesCount();

        [FreeFunction("UNETManager::Get()->GetOutgoingSystemBytesCount", ThrowsException = true)]
        extern static public int GetOutgoingSystemBytesCountForHost(int hostId, out byte error);

        [FreeFunction("UNETManager::Get()->GetOutgoingSystemBytesCount", ThrowsException = true)]
        extern static public int GetOutgoingSystemBytesCountForConnection(int hostId, int connectionId, out byte error);

        [FreeFunction("UNETManager::Get()->GetOutgoingFullBytesCount", ThrowsException = true)]
        extern static public int GetOutgoingFullBytesCount();

        [FreeFunction("UNETManager::Get()->GetOutgoingFullBytesCount", ThrowsException = true)]
        extern static public int GetOutgoingFullBytesCountForHost(int hostId, out byte error);

        [FreeFunction("UNETManager::Get()->GetOutgoingFullBytesCount", ThrowsException = true)]
        extern static public int GetOutgoingFullBytesCountForConnection(int hostId, int connectionId, out byte error);

        [Obsolete("GetPacketSentRate has been deprecated.")]
        static public int GetPacketSentRate(int hostId, int connectionId, out byte error)
        {
            error = 0;
            return 0;
        }

        [Obsolete("GetPacketReceivedRate has been deprecated.")]
        static public int GetPacketReceivedRate(int hostId, int connectionId, out byte error)
        {
            error = 0;
            return 0;
        }

        [Obsolete("GetRemotePacketReceivedRate has been deprecated.")]
        static public int GetRemotePacketReceivedRate(int hostId, int connectionId, out byte error)
        {
            error = 0;
            return 0;
        }

        [Obsolete("GetNetIOTimeuS has been deprecated.")]
        static public int GetNetIOTimeuS()
        {
            return 0;
        }

        [FreeFunction("UNETManager::Get()->GetConnectionInfo", ThrowsException = true)]
        extern static public string GetConnectionInfo(int hostId, int connectionId, out int port, out ulong network, out ushort dstNode, out byte error);
        static public void GetConnectionInfo(int hostId, int connectionId, out string address, out int port, out NetworkID network, out NodeID dstNode, out byte error)
        {
            ulong netw;
            ushort node;
            address = GetConnectionInfo(hostId, connectionId, out port, out netw, out node, out error);
            network = (NetworkID)netw;
            dstNode = (NodeID)node;
        }

        //Timing service API
        [FreeFunction("UNETManager::Get()->GetNetworkTimestamp", ThrowsException = true)]
        extern static public int GetNetworkTimestamp();

        [FreeFunction("UNETManager::Get()->GetRemoteDelayTimeMS", ThrowsException = true)]
        extern static public int GetRemoteDelayTimeMS(int hostId, int connectionId, int remoteTime, out byte error);

        static public bool StartSendMulticast(int hostId, int channelId, byte[] buffer, int size, out byte error)
        {
            return StartSendMulticastInternal(hostId, channelId, buffer, size, out error);
        }

        [FreeFunction("UNETManager::Get()->StartSendMulticast", ThrowsException = true)]
        extern static private bool StartSendMulticastInternal(int hostId, int channelId, [Out] byte[] buffer, int size, out byte error);

        [FreeFunction("UNETManager::Get()->SendMulticast", ThrowsException = true)]
        extern static public bool SendMulticast(int hostId, int connectionId, out byte error);

        [FreeFunction("UNETManager::Get()->FinishSendMulticast", ThrowsException = true)]
        extern static public bool FinishSendMulticast(int hostId, out byte error);

        [FreeFunction("UNETManager::Get()->GetMaxPacketSize", ThrowsException = true)]
        extern static private int GetMaxPacketSize();

        [FreeFunction("UNETManager::Get()->RemoveHost", ThrowsException = true)]
        extern static public bool RemoveHost(int hostId);

        static public bool IsStarted
        {
            get { return IsStartedInternal(); }
        }
        [FreeFunction("UNETManager::IsStarted")]
        extern static private bool IsStartedInternal();

        [FreeFunction("UNETManager::Get()->Connect", ThrowsException = true)]
        extern static public int Connect(int hostId, string address, int port, int exeptionConnectionId, out byte error);

        [FreeFunction("UNETManager::Get()->ConnectWithSimulator", ThrowsException = true)]
        extern static private int ConnectWithSimulatorInternal(int hostId, string address, int port, int exeptionConnectionId, out byte error, ConnectionSimulatorConfigInternal conf);
        static public int ConnectWithSimulator(int hostId, string address, int port, int exeptionConnectionId, out byte error, ConnectionSimulatorConfig conf)
        {
            return ConnectWithSimulatorInternal(hostId, address, port, exeptionConnectionId, out error, new ConnectionSimulatorConfigInternal(conf));
        }

        [FreeFunction("UNETManager::Get()->Disconnect", ThrowsException = true)]
        extern static public bool Disconnect(int hostId, int connectionId, out byte error);

        [FreeFunction("UNETManager::Get()->ConnectSockAddr", ThrowsException = true)]
        extern static private int Internal_ConnectEndPoint(int hostId, [Out] byte[] sockAddrStorage, int sockAddrStorageLen, int exceptionConnectionId, out byte error);
        static public bool Send(int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error)
        {
            if (buffer == null)
                throw new NullReferenceException("send buffer is not initialized");
            return SendWrapper(hostId, connectionId, channelId, buffer, size, out error);
        }

        [FreeFunction("UNETManager::Get()->Send", ThrowsException = true)]
        extern private static bool SendWrapper(int hostId, int connectionId, int channelId, [Out] byte[] buffer, int size, out byte error);
        static public bool QueueMessageForSending(int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error)
        {
            if (buffer == null)
                throw new NullReferenceException("send buffer is not initialized");
            return QueueMessageForSendingWrapper(hostId, connectionId, channelId, buffer, size, out error);
        }

        [FreeFunction("UNETManager::Get()->QueueMessageForSending", ThrowsException = true)]
        extern private static bool QueueMessageForSendingWrapper(int hostId, int connectionId, int channelId, [Out] byte[] buffer, int size, out byte error);

        [FreeFunction("UNETManager::Get()->SendQueuedMessages", ThrowsException = true)]
        extern static public bool SendQueuedMessages(int hostId, int connectionId, out byte error);

        static public NetworkEventType Receive(out int hostId, out int connectionId, out int channelId, byte[] buffer, int bufferSize, out int receivedSize, out byte error)
        {
            return (NetworkEventType)PopData(out hostId, out connectionId, out channelId, buffer, bufferSize, out receivedSize, out error);
        }

        [FreeFunction("UNETManager::Get()->PopData", ThrowsException = true)]
        private static extern int PopData(out int hostId, out int connectionId, out int channelId, [Out] byte[] buffer, int bufferSize, out int receivedSize, out byte error);

        static public NetworkEventType ReceiveFromHost(int hostId, out int connectionId, out int channelId, byte[] buffer, int bufferSize, out int receivedSize, out byte error)
        {
            return (NetworkEventType)PopDataFromHost(hostId, out connectionId, out channelId, buffer, bufferSize, out receivedSize, out error);
        }

        [FreeFunction("UNETManager::Get()->PopDataFromHost", ThrowsException = true)]
        private static extern int PopDataFromHost(int hostId, out int connectionId, out int channelId, [Out] byte[] buffer, int bufferSize, out int receivedSize, out byte error);

        [FreeFunction("UNETManager::Get()->SetPacketStat", ThrowsException = true)]
        extern static public void SetPacketStat(int direction, int packetStatId, int numMsgs, int numBytes);


        [NativeThrows]
        [FreeFunction("UNETManager::SetNetworkEventAvailableCallback")]
        extern static void SetNetworkEventAvailableCallback(Action<int> callback);

        [FreeFunction("UNETManager::Cleanup")]
        extern static void Cleanup();

        [NativeThrows]
        [FreeFunction("UNETManager::SetConnectionReadyForSendCallback")]
        extern static void SetConnectionReadyForSendCallback(Action<int, int> callback);

        [FreeFunction("UNETManager::Get()->NotifyWhenConnectionReadyForSend", ThrowsException = true)]
        extern static public bool NotifyWhenConnectionReadyForSend(int hostId, int connectionId, int notificationLevel, out byte error);

        [FreeFunction("UNETManager::Get()->GetHostPort", ThrowsException = true)]
        extern static public int GetHostPort(int hostId);


        //broadcast
        [FreeFunction("UNETManager::Get()->StartBroadcastDiscoveryWithData", ThrowsException = true)]
        extern static private bool StartBroadcastDiscoveryWithData(int hostId, int broadcastPort, int key, int version, int subversion, [Out] byte[] buffer, int size, int timeout, out byte error);

        [FreeFunction("UNETManager::Get()->StartBroadcastDiscoveryWithoutData", ThrowsException = true)]
        extern private static bool StartBroadcastDiscoveryWithoutData(int hostId, int broadcastPort, int key, int version, int subversion, int timeout, out byte error);
        static public bool StartBroadcastDiscovery(int hostId, int broadcastPort, int key, int version, int subversion, byte[] buffer, int size, int timeout, out byte error)
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

        [FreeFunction("UNETManager::Get()->StopBroadcastDiscovery", ThrowsException = true)]
        extern static public void StopBroadcastDiscovery();

        [FreeFunction("UNETManager::Get()->IsBroadcastDiscoveryRunning", ThrowsException = true)]
        extern static public bool IsBroadcastDiscoveryRunning();

        [FreeFunction("UNETManager::Get()->SetBroadcastCredentials", ThrowsException = true)]
        extern static public void SetBroadcastCredentials(int hostId, int key, int version, int subversion, out byte error);

        [FreeFunction("UNETManager::Get()->GetBroadcastConnectionInfoInternal", ThrowsException = true)]
        extern public static string GetBroadcastConnectionInfo(int hostId, out int port, out byte error);

        static public void GetBroadcastConnectionInfo(int hostId, out string address, out int port, out byte error)
        {
            address = GetBroadcastConnectionInfo(hostId, out port, out error);
        }

        static public void GetBroadcastConnectionMessage(int hostId, byte[] buffer, int bufferSize, out int receivedSize, out byte error)
        {
            GetBroadcastConnectionMessageInternal(hostId, buffer, bufferSize, out  receivedSize, out  error);
        }

        [FreeFunction("UNETManager::SetMulticastLock")]
        extern public static void SetMulticastLock(bool enabled);

        [FreeFunction("UNETManager::Get()->GetBroadcastConnectionMessage", ThrowsException = true)]
        extern static private void GetBroadcastConnectionMessageInternal(int hostId, [Out] byte[] buffer, int bufferSize, out int receivedSize, out byte error);


        //utils
        private static void CheckTopology(HostTopology topology)
        {
            int maxPacketSize = GetMaxPacketSize();
            if (topology.DefaultConfig.PacketSize > maxPacketSize)
                throw new ArgumentOutOfRangeException("Default config: packet size should be less than packet size defined in global config: " + maxPacketSize);
            for (int i = 0; i < topology.SpecialConnectionConfigs.Count; ++i)
            {
                if (topology.SpecialConnectionConfigs[i].PacketSize > maxPacketSize)
                {
                    throw new ArgumentOutOfRangeException("Special config " + i + ": packet size should be less than packet size defined in global config: " + maxPacketSize);
                }
            }
        }

        [FreeFunction("UNETManager::Get()->LoadEncryptionLibrary", ThrowsException = true)]
        extern static private bool LoadEncryptionLibraryInternal(string libraryName);

        public static bool LoadEncryptionLibrary(string libraryName)
        {
            return LoadEncryptionLibraryInternal(libraryName);
        }

        [FreeFunction("UNETManager::Get()->UnloadEncryptionLibrary", ThrowsException = true)]
        extern static private void UnloadEncryptionLibraryInternal();

        public static void UnloadEncryptionLibrary()
        {
            UnloadEncryptionLibraryInternal();
        }

        [FreeFunction("UNETManager::Get()->IsEncryptionActive", ThrowsException = true)]
        extern static private bool IsEncryptionActiveInternal();

        public static bool IsEncryptionActive()
        {
            return IsEncryptionActiveInternal();
        }

        [FreeFunction("UNETManager::Get()->GetEncryptionSafeMaxPacketSize", ThrowsException = true)]
        extern static private short GetEncryptionSafeMaxPacketSizeInternal(short maxPacketSize);

        public static short GetEncryptionSafeMaxPacketSize(short maxPacketSize)
        {
            return GetEncryptionSafeMaxPacketSizeInternal(maxPacketSize);
        }
    }
}

