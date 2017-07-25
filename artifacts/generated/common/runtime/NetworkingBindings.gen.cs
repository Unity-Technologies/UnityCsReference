// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;



using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngineInternal;

namespace UnityEngine
{



public enum RPCMode
{
    
    Server = 0,
    
    Others = 1,
    
    OthersBuffered = 5,
    
    All = 2,
    
    AllBuffered = 6
}

public enum ConnectionTesterStatus
{
    
    Error = -2,
    
    Undetermined = -1,
    [System.Obsolete ("No longer returned, use newer connection tester enums instead.")]
    PrivateIPNoNATPunchthrough = 0,
    [System.Obsolete ("No longer returned, use newer connection tester enums instead.")]
    PrivateIPHasNATPunchThrough = 1,
    
    PublicIPIsConnectable = 2,
    
    PublicIPPortBlocked = 3,
    
    PublicIPNoServerStarted = 4,
    
    LimitedNATPunchthroughPortRestricted = 5,
    
    LimitedNATPunchthroughSymmetric = 6,
    
    NATpunchthroughFullCone = 7,
    
    NATpunchthroughAddressRestrictedCone = 8
}

public enum NetworkConnectionError
{
    
    NoError = 0,
    
    RSAPublicKeyMismatch = 21,
    
    InvalidPassword = 23,
    
    ConnectionFailed = 15,
    
    TooManyConnectedPlayers = 18,
    
    ConnectionBanned = 22,
    
    AlreadyConnectedToServer = 16,
    
    AlreadyConnectedToAnotherServer = -1,
    
    CreateSocketOrThreadFailure = -2,
    
    IncorrectParameters = -3,
    
    EmptyConnectTarget = -4,
    
    InternalDirectConnectFailed = -5,
    
    NATTargetNotConnected = 69,
    
    NATTargetConnectionLost = 71,
    
    NATPunchthroughFailed = 73
}

public enum NetworkDisconnection
{
    
    LostConnection = 20,
    
    Disconnected = 19
}

public enum MasterServerEvent
{
    
    RegistrationFailedGameName = 0,
    
    RegistrationFailedGameType = 1,
    
    RegistrationFailedNoServer = 2,
    
    RegistrationSucceeded = 3,
    
    HostListReceived = 4
}

public enum NetworkStateSynchronization
{
    
    Off = 0,
    
    ReliableDeltaCompressed = 1,
    
    Unreliable = 2
}

public enum NetworkPeerType
{
    
    Disconnected = 0,
    
    Server = 1,
    
    Client = 2,
    
    Connecting = 3
}

public enum NetworkLogLevel
{
    
    Off = 0,
    
    Informational = 1,
    
    Full = 3
}

[RequiredByNativeCode(Optional = true)]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct NetworkPlayer
{
    internal int index;
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string Internal_GetIPAddress (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetPort (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string Internal_GetExternalIP () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetExternalPort () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string Internal_GetLocalIP () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetLocalPort () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetPlayerIndex () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string Internal_GetGUID (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string Internal_GetLocalGUID () ;

    public NetworkPlayer(string ip, int port)
        {
            Debug.LogError("Not yet implemented");
            index = 0;
        }
    
    
    public static bool operator==(NetworkPlayer lhs, NetworkPlayer rhs)
        {
            return lhs.index == rhs.index;
        }
    
    
    public static bool operator!=(NetworkPlayer lhs, NetworkPlayer rhs)
        {
            return lhs.index != rhs.index;
        }
    
    
    public override int GetHashCode()
        {
            return index.GetHashCode();
        }
    
    
    public override bool Equals(object other)
        {
            if (!(other is NetworkPlayer)) return false;

            NetworkPlayer rhs = (NetworkPlayer)other;
            return rhs.index == index;
        }
    
    
    public string ipAddress { get { if (index == Internal_GetPlayerIndex()) return Internal_GetLocalIP(); else return Internal_GetIPAddress(index); } }
    
    
    public int port { get { if (index == Internal_GetPlayerIndex()) return Internal_GetLocalPort(); else return Internal_GetPort(index); } }
    
    
    public string guid { get { if (index == Internal_GetPlayerIndex()) return Internal_GetLocalGUID(); else return Internal_GetGUID(index); } }
    
    
    override public string ToString() { return index.ToString(); }
    
    
    public string externalIP { get { return Internal_GetExternalIP(); } }
    
    
    public int externalPort { get { return Internal_GetExternalPort(); } }
    
    
    static internal NetworkPlayer unassigned { get { NetworkPlayer val; val.index = -1; return val; } }
    
    
}

[RequiredByNativeCode(Optional = true)]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct NetworkViewID
{
    int a;
    int b;
    int c;
    
    
    public static NetworkViewID unassigned
    {
        get { NetworkViewID tmp; INTERNAL_get_unassigned(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_unassigned (out NetworkViewID value) ;


    internal static bool Internal_IsMine (NetworkViewID value) {
        return INTERNAL_CALL_Internal_IsMine ( ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_IsMine (ref NetworkViewID value);
    internal static void Internal_GetOwner (NetworkViewID value, out NetworkPlayer player) {
        INTERNAL_CALL_Internal_GetOwner ( ref value, out player );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_GetOwner (ref NetworkViewID value, out NetworkPlayer player);
    internal static string Internal_GetString (NetworkViewID value) {
        return INTERNAL_CALL_Internal_GetString ( ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static string INTERNAL_CALL_Internal_GetString (ref NetworkViewID value);
    internal static bool Internal_Compare (NetworkViewID lhs, NetworkViewID rhs) {
        return INTERNAL_CALL_Internal_Compare ( ref lhs, ref rhs );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_Compare (ref NetworkViewID lhs, ref NetworkViewID rhs);
    public static bool operator==(NetworkViewID lhs, NetworkViewID rhs)
        {
            return Internal_Compare(lhs, rhs);
        }
    
    
    public static bool operator!=(NetworkViewID lhs, NetworkViewID rhs)
        {
            return !Internal_Compare(lhs, rhs);
        }
    
    
    public override int GetHashCode()
        {
            return a ^ b ^ c;
        }
    
    
    public override bool Equals(object other)
        {
            if (!(other is NetworkViewID)) return false;

            NetworkViewID rhs = (NetworkViewID)other;
            return Internal_Compare(this, rhs);
        }
    
    
    public bool isMine
        {
            get
            { return Internal_IsMine(this); } }
    
    
    public NetworkPlayer owner  { get { NetworkPlayer p; Internal_GetOwner(this, out p); return p; } }
    
    
    override public string ToString()  { return Internal_GetString(this); }
    
    
}

[System.Obsolete ("Unity Multiplayer and NetworkIdentity component instead.")]
public sealed partial class NetworkView : Behaviour
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_RPC (NetworkView view, string name, RPCMode mode, object[] args) ;

    private static void Internal_RPC_Target (NetworkView view, string name, NetworkPlayer target, object[] args) {
        INTERNAL_CALL_Internal_RPC_Target ( view, name, ref target, args );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_RPC_Target (NetworkView view, string name, ref NetworkPlayer target, object[] args);
    [System.Obsolete ("NetworkView RPC functions are deprecated. Refer to the new Multiplayer Networking system.")]
public void RPC(string name, RPCMode mode, params object[] args) { Internal_RPC(this, name, mode, args); }
    
    
    [System.Obsolete ("NetworkView RPC functions are deprecated. Refer to the new Multiplayer Networking system.")]
public void RPC(string name, NetworkPlayer target, params object[] args) { Internal_RPC_Target(this, name, target, args); }
    
    
    public extern Component observed
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  NetworkStateSynchronization stateSynchronization
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_GetViewID (out NetworkViewID viewID) ;

    private void Internal_SetViewID (NetworkViewID viewID) {
        INTERNAL_CALL_Internal_SetViewID ( this, ref viewID );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_SetViewID (NetworkView self, ref NetworkViewID viewID);
    public NetworkViewID viewID { get { NetworkViewID val; Internal_GetViewID(out val); return val; } set  { Internal_SetViewID(value); } }
    
    
    public extern int group
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public bool isMine { get { return viewID.isMine; } }
    
    
    public NetworkPlayer owner { get { return viewID.owner; } }
    
    
    public bool SetScope (NetworkPlayer player, bool relevancy) {
        return INTERNAL_CALL_SetScope ( this, ref player, relevancy );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_SetScope (NetworkView self, ref NetworkPlayer player, bool relevancy);
    public static NetworkView Find (NetworkViewID viewID) {
        return INTERNAL_CALL_Find ( ref viewID );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static NetworkView INTERNAL_CALL_Find (ref NetworkViewID viewID);
}

public sealed partial class Network
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  NetworkConnectionError InitializeServer (int connections, int listenPort, bool useNat) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  NetworkConnectionError Internal_InitializeServerDeprecated (int connections, int listenPort) ;

    [System.Obsolete ("Use the IntializeServer(connections, listenPort, useNat) function instead")]
public static NetworkConnectionError InitializeServer(int connections, int listenPort) { return Internal_InitializeServerDeprecated(connections, listenPort); }
    
    
    public extern static string incomingPassword
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static NetworkLogLevel logLevel
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void InitializeSecurity () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  NetworkConnectionError Internal_ConnectToSingleIP (string IP, int remotePort, int localPort, [uei.DefaultValue("\"\"")]  string password ) ;

    [uei.ExcludeFromDocs]
    private static NetworkConnectionError Internal_ConnectToSingleIP (string IP, int remotePort, int localPort) {
        string password = "";
        return Internal_ConnectToSingleIP ( IP, remotePort, localPort, password );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  NetworkConnectionError Internal_ConnectToGuid (string guid, string password) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  NetworkConnectionError Internal_ConnectToIPs (string[] IP, int remotePort, int localPort, [uei.DefaultValue("\"\"")]  string password ) ;

    [uei.ExcludeFromDocs]
    private static NetworkConnectionError Internal_ConnectToIPs (string[] IP, int remotePort, int localPort) {
        string password = "";
        return Internal_ConnectToIPs ( IP, remotePort, localPort, password );
    }

    [uei.ExcludeFromDocs]
public static NetworkConnectionError Connect (string IP, int remotePort) {
    string password = "";
    return Connect ( IP, remotePort, password );
}

public static NetworkConnectionError Connect(string IP, int remotePort, [uei.DefaultValue("\"\"")]  string password ) { return Internal_ConnectToSingleIP(IP, remotePort, 0, password); }

    
    
    [uei.ExcludeFromDocs]
public static NetworkConnectionError Connect (string[] IPs, int remotePort) {
    string password = "";
    return Connect ( IPs, remotePort, password );
}

public static NetworkConnectionError Connect(string[] IPs, int remotePort, [uei.DefaultValue("\"\"")]  string password )
        {
            return Internal_ConnectToIPs(IPs, remotePort, 0, password);
        }

    
    
    [uei.ExcludeFromDocs]
public static NetworkConnectionError Connect (string GUID) {
    string password = "";
    return Connect ( GUID, password );
}

public static NetworkConnectionError Connect(string GUID, [uei.DefaultValue("\"\"")]  string password )
        {
            return Internal_ConnectToGuid(GUID, password);
        }

    
    
    [uei.ExcludeFromDocs]
public static NetworkConnectionError Connect (HostData hostData) {
    string password = "";
    return Connect ( hostData, password );
}

public static NetworkConnectionError Connect(HostData hostData, [uei.DefaultValue("\"\"")]  string password )
        {
            if (hostData == null)
                throw new NullReferenceException();
            if (hostData.guid.Length > 0 && hostData.useNat)
                return Connect(hostData.guid, password);
            else
                return Connect(hostData.ip, hostData.port, password);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Disconnect ( [uei.DefaultValue("200")] int timeout ) ;

    [uei.ExcludeFromDocs]
    public static void Disconnect () {
        int timeout = 200;
        Disconnect ( timeout );
    }

    public static void CloseConnection (NetworkPlayer target, bool sendDisconnectionNotification) {
        INTERNAL_CALL_CloseConnection ( ref target, sendDisconnectionNotification );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CloseConnection (ref NetworkPlayer target, bool sendDisconnectionNotification);
    public extern static NetworkPlayer[] connections
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetPlayer () ;

    public static NetworkPlayer player { get { NetworkPlayer np; np.index = Internal_GetPlayer(); return np; } }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_AllocateViewID (out NetworkViewID viewID) ;

    public static NetworkViewID AllocateViewID() { NetworkViewID val; Internal_AllocateViewID(out val); return val; }
    
    
    [TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
    public static Object Instantiate (Object prefab, Vector3 position, Quaternion rotation, int group) {
        return INTERNAL_CALL_Instantiate ( prefab, ref position, ref rotation, group );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Object INTERNAL_CALL_Instantiate (Object prefab, ref Vector3 position, ref Quaternion rotation, int group);
    public static void Destroy (NetworkViewID viewID) {
        INTERNAL_CALL_Destroy ( ref viewID );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Destroy (ref NetworkViewID viewID);
    public static void Destroy(GameObject gameObject)
        {
            if (gameObject != null)
            {
                #pragma warning disable 0618
                NetworkView view = gameObject.GetComponent<NetworkView>();
                #pragma warning restore 0618
                if (view != null)
                    Destroy(view.viewID);
                else
                    Debug.LogError("Couldn't destroy game object because no network view is attached to it.", gameObject);
            }
        }
    
    
    public static void DestroyPlayerObjects (NetworkPlayer playerID) {
        INTERNAL_CALL_DestroyPlayerObjects ( ref playerID );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DestroyPlayerObjects (ref NetworkPlayer playerID);
    private static void Internal_RemoveRPCs (NetworkPlayer playerID, NetworkViewID viewID, uint channelMask) {
        INTERNAL_CALL_Internal_RemoveRPCs ( ref playerID, ref viewID, channelMask );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_RemoveRPCs (ref NetworkPlayer playerID, ref NetworkViewID viewID, uint channelMask);
    public static void RemoveRPCs(NetworkPlayer playerID) { Internal_RemoveRPCs(playerID, NetworkViewID.unassigned, 0xFFFFFFFF); }
    public static void RemoveRPCs(NetworkPlayer playerID, int group) { Internal_RemoveRPCs(playerID, NetworkViewID.unassigned, (uint)(1 << group)); }
    public static void RemoveRPCs(NetworkViewID viewID) { Internal_RemoveRPCs(NetworkPlayer.unassigned, viewID, 0xFFFFFFFF); }
    public static  void RemoveRPCsInGroup(int group) { Internal_RemoveRPCs(NetworkPlayer.unassigned, NetworkViewID.unassigned, (uint)(1 << group)); }
    
    
    public extern static bool isClient
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool isServer
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static NetworkPeerType peerType
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetLevelPrefix (int prefix) ;

    public static int GetLastPing (NetworkPlayer player) {
        return INTERNAL_CALL_GetLastPing ( ref player );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetLastPing (ref NetworkPlayer player);
    public static int GetAveragePing (NetworkPlayer player) {
        return INTERNAL_CALL_GetAveragePing ( ref player );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetAveragePing (ref NetworkPlayer player);
    public extern static float sendRate
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool isMessageQueueRunning
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public static void SetReceivingEnabled (NetworkPlayer player, int group, bool enabled) {
        INTERNAL_CALL_SetReceivingEnabled ( ref player, group, enabled );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetReceivingEnabled (ref NetworkPlayer player, int group, bool enabled);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetSendingGlobal (int group, bool enabled) ;

    private static void Internal_SetSendingSpecific (NetworkPlayer player, int group, bool enabled) {
        INTERNAL_CALL_Internal_SetSendingSpecific ( ref player, group, enabled );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_SetSendingSpecific (ref NetworkPlayer player, int group, bool enabled);
    public static void SetSendingEnabled(int group, bool enabled) { Internal_SetSendingGlobal(group, enabled); }
    
    
    public static void SetSendingEnabled(NetworkPlayer player, int group, bool enabled) { Internal_SetSendingSpecific(player, group, enabled); }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_GetTime (out double t) ;

    public static double time {  get { double t; Internal_GetTime(out t); return t; } }
    
    
    public extern static int minimumAllocatableViewIDs
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("No longer needed. This is now explicitly set in the InitializeServer function call. It is implicitly set when calling Connect depending on if an IP/port combination is used (useNat=false) or a GUID is used(useNat=true).")]
    public extern static bool useNat
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static string natFacilitatorIP
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static int natFacilitatorPort
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  ConnectionTesterStatus TestConnection ( [uei.DefaultValue("false")] bool forceTest ) ;

    [uei.ExcludeFromDocs]
    public static ConnectionTesterStatus TestConnection () {
        bool forceTest = false;
        return TestConnection ( forceTest );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  ConnectionTesterStatus TestConnectionNAT ( [uei.DefaultValue("false")] bool forceTest ) ;

    [uei.ExcludeFromDocs]
    public static ConnectionTesterStatus TestConnectionNAT () {
        bool forceTest = false;
        return TestConnectionNAT ( forceTest );
    }

    public extern static string connectionTesterIP
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static int connectionTesterPort
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool HavePublicAddress () ;

    public extern static int maxConnections
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static string proxyIP
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static int proxyPort
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool useProxy
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static string proxyPassword
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

[RequiredByNativeCode(Optional = true)]
public sealed partial class BitStream
{
    internal IntPtr m_Ptr;
    
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Serializeb (ref int value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Serializec (ref char value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Serializes (ref short value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Serializei (ref int value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Serializef (ref float value, float maximumDelta) ;

    private void Serializeq (ref Quaternion value, float maximumDelta) {
        INTERNAL_CALL_Serializeq ( this, ref value, maximumDelta );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Serializeq (BitStream self, ref Quaternion value, float maximumDelta);
    private void Serializev (ref Vector3 value, float maximumDelta) {
        INTERNAL_CALL_Serializev ( this, ref value, maximumDelta );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Serializev (BitStream self, ref Vector3 value, float maximumDelta);
    private void Serializen (ref NetworkViewID viewID) {
        INTERNAL_CALL_Serializen ( this, ref viewID );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Serializen (BitStream self, ref NetworkViewID viewID);
    public void Serialize(ref bool value) { int cross = value ? 1 : 0; Serializeb(ref cross); value = cross == 0 ? false : true; }
    public void Serialize(ref char value) { Serializec(ref value); }
    public void Serialize(ref short value) { Serializes(ref value); }
    public void Serialize(ref int value) { Serializei(ref value); }
    [uei.ExcludeFromDocs]
public void Serialize (ref float value) {
    float maxDelta = 0.00001F;
    Serialize ( ref value, maxDelta );
}

public void Serialize(ref float value, [uei.DefaultValue("0.00001F")]  float maxDelta ) { Serializef(ref value, maxDelta); }

    [uei.ExcludeFromDocs]
public void Serialize (ref Quaternion value) {
    float maxDelta = 0.00001F;
    Serialize ( ref value, maxDelta );
}

public void Serialize(ref Quaternion value, [uei.DefaultValue("0.00001F")]  float maxDelta ) { Serializeq(ref value, maxDelta); }

    [uei.ExcludeFromDocs]
public void Serialize (ref Vector3 value) {
    float maxDelta = 0.00001F;
    Serialize ( ref value, maxDelta );
}

public void Serialize(ref Vector3 value, [uei.DefaultValue("0.00001F")]  float maxDelta ) { Serializev(ref value, maxDelta); }

    public void Serialize(ref NetworkPlayer value) { int index = value.index; Serializei(ref index); value.index = index; }
    public void Serialize(ref NetworkViewID viewID) { Serializen(ref viewID); }
    
    
    public extern  bool isReading
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool isWriting
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Serialize (ref string value) ;

}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[RequiredByNativeCode(Optional = true)]
[System.Obsolete ("NetworkView RPC functions are deprecated. Refer to the new Multiplayer Networking system.")]
public sealed partial class RPC : Attribute
{
}

[RequiredByNativeCode(Optional = true)]
[StructLayout(LayoutKind.Sequential)]
public sealed partial class HostData
{
    private int    m_Nat;
    private string m_GameType;
    private string m_GameName;
    private int    m_ConnectedPlayers;
    private int    m_PlayerLimit;
    private string[] m_IP;
    private int    m_Port;
    private int    m_PasswordProtected;
    private string m_Comment;
    private string m_GUID;
    
    
    
    public bool   useNat   { get { return m_Nat != 0; } set { m_Nat = value ? 1 : 0; } }
    public string gameType { get { return m_GameType; } set { m_GameType = value; } }
    
    
    public string gameName { get { return m_GameName; } set { m_GameName = value; } }
    
    
    public int    connectedPlayers { get { return m_ConnectedPlayers; } set { m_ConnectedPlayers = value; } }
    
    
    public int    playerLimit { get { return m_PlayerLimit; } set { m_PlayerLimit = value; } }
    public string[] ip { get { return m_IP; } set { m_IP = value; } }
    
    
    public int    port  { get { return m_Port; } set { m_Port = value; } }
    
    
    public bool   passwordProtected { get { return m_PasswordProtected != 0; } set { m_PasswordProtected = value ? 1 : 0; } }
    
    
    public string comment { get { return m_Comment; } set { m_Comment = value; } }
    
    
    public string guid { get { return m_GUID; } set { m_GUID = value; } }
}

public sealed partial class MasterServer
{
    public extern static string ipAddress
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static int port
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RequestHostList (string gameTypeName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  HostData[] PollHostList () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RegisterHost (string gameTypeName, string gameName, [uei.DefaultValue("\"\"")]  string comment ) ;

    [uei.ExcludeFromDocs]
    public static void RegisterHost (string gameTypeName, string gameName) {
        string comment = "";
        RegisterHost ( gameTypeName, gameName, comment );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void UnregisterHost () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ClearHostList () ;

    public extern static int updateRate
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool dedicatedServer
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

[RequiredByNativeCode(Optional = true)]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct NetworkMessageInfo
{
    
            double        m_TimeStamp;
            NetworkPlayer m_Sender;
            NetworkViewID m_ViewID;
    
    
    public double timestamp { get { return m_TimeStamp; }  }
    
    
    public NetworkPlayer sender { get { return m_Sender; }  }
    
            #pragma warning disable 0618
    public NetworkView networkView
        {
            get
            {
                if (m_ViewID == NetworkViewID.unassigned)
                {
                    Debug.LogError("No NetworkView is assigned to this NetworkMessageInfo object. Note that this is expected in OnNetworkInstantiate().");
                    return NullNetworkView();
                }

                return NetworkView.Find(m_ViewID);
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal NetworkView NullNetworkView () ;

}

}
