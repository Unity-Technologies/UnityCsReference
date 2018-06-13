// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Networking.Types;

// We have a lot of fields we never write to in this file, but they are written to by the serialization system.
#pragma warning disable 649

namespace UnityEngine.Networking.Match
{
    internal abstract class Request
    {
        public static readonly int currentVersion = 3;

        /// <summary>
        /// The UNet web protocol version
        /// </summary>
        public int version { get; set; }

        /// <summary>
        /// The SourceID for the requesting client
        /// </summary>
        public SourceID sourceId { get; set; }

        /// <summary>
        /// The project GUID
        /// </summary>
        public string projectId { get; set; }

        /// <summary>
        /// The AppID for the requesting client.
        /// /// </summary>
        public AppID appId { get; set; }

        /// <summary>
        /// The unique access token that binds a sourceId to a session and authenticate's it to do privileged commands
        /// </summary>
        public string accessTokenString { get; set; }

        /// <summary>
        /// The domain to use for the given appId; Only clients with the same domain can interact with each other
        /// </summary>
        public int domain { get; set; }

        public virtual bool IsValid()
        {
            return sourceId != SourceID.Invalid;
        }

        override public string ToString()
        {
            return UnityString.Format("[{0}]-SourceID:0x{1},projectId:{2},accessTokenString.IsEmpty:{3},domain:{4}", base.ToString(), sourceId.ToString("X"), projectId, string.IsNullOrEmpty(accessTokenString), domain);
        }
    }

    internal interface IResponse
    {
        void SetSuccess();
        void SetFailure(string info);
    }

    [Serializable]
    internal abstract class Response : IResponse
    {
        /// <summary>
        /// Indicator on request success
        public bool success;

        public string extendedInfo;

        public void SetSuccess()
        {
            success = true;
            extendedInfo = "";
        }

        public void SetFailure(string info)
        {
            success = false;
            extendedInfo += info;
        }

        override public string ToString()
        {
            return UnityString.Format("[{0}]-success:{1}-extendedInfo:{2}", base.ToString(), success, extendedInfo);
        }
    }

    internal class BasicResponse : Response
    {
    }

    internal class CreateMatchRequest : Request
    {
        /// <summary>
        /// Name for the match
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Number of nodes the room may have
        /// </summary>
        public uint size { get; set; }

        /// <summary>
        /// Optional string to specify the internet address to connect to this client if it were to host
        /// </summary>
        public string publicAddress { get; set; }

        /// <summary>
        /// Optional string to specify the local network address to connect to this client if it were to host
        /// </summary>
        public string privateAddress { get; set; }

        /// <summary>
        /// Optional int to specify the game specific Elo score for this client. If left 0 for all clients will not affect result set
        /// </summary>
        public int eloScore { get; set; }

        /// <summary>
        /// Indicates if the match should be advertised to others
        /// </summary>
        public bool advertise { get; set; }

        /// <summary>
        /// Optional provided password to limit public joining of a room
        /// </summary>
        public string password { get; set; }

        public Dictionary<string, long> matchAttributes { get; set; }

        override public string ToString()
        {
            return UnityString.Format("[{0}]-name:{1},size:{2},publicAddress:{3},privateAddress:{4},eloScore:{5},advertise:{6},HasPassword:{7},matchAttributes.Count:{8}", base.ToString(), name, size, publicAddress, privateAddress, eloScore, advertise, string.IsNullOrEmpty(password) ? "NO" : "YES", matchAttributes == null ? 0 : matchAttributes.Count);
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && size >= 2
                && (matchAttributes == null ? true : matchAttributes.Count <= 10);
        }
    }

    internal class CreateMatchResponse : BasicResponse
    {
        /// <summary>
        /// IP Address of Host. If self-hosting, ignore.
        /// </summary>
        public string address;

        /// <summary>
        /// Port to connect to at Host with specified IP. If self-hosting, ignore.
        /// </summary>
        public int port;

        /// <summary>
        /// Unity Multiplayer Domain this Response belongs to
        /// </summary>
        public int domain = 0;

        /// <summary>
        /// The match unique identifier for future commands
        /// </summary>

        // This should be a NetworkID, but our serialization system does not support 64-bit enums.
        public ulong networkId;

        /// <summary>
        /// The access token for this client to interact with the match
        /// </summary>
        public string accessTokenString;

        /// <summary>
        /// The client id of the caller for this network
        /// </summary>
        public NodeID nodeId;

        // Designation if a relay server is the destination (not self hosted)
        public bool usingRelay;

        override public string ToString()
        {
            return UnityString.Format("[{0}]-address:{1},port:{2},networkId:0x{3},accessTokenString.IsEmpty:{4},nodeId:0x{5},usingRelay:{6}", base.ToString(), address, port, networkId.ToString("X"), string.IsNullOrEmpty(accessTokenString), nodeId.ToString("X"), usingRelay);
        }
    }

    internal class JoinMatchRequest : Request
    {
        /// <summary>
        /// The networkId to join
        /// </summary>
        public NetworkID networkId { get; set; }

        /// <summary>
        /// Optional string to specify the internet address to connect to this client if it were to host
        /// </summary>
        public string publicAddress { get; set; }

        /// <summary>
        /// Optional string to specify the local network address to connect to this client if it were to host
        /// </summary>
        public string privateAddress { get; set; }

        /// <summary>
        /// Optional int to specify the game specific Elo score for this client. If left 0 for all clients will not affect result set
        /// </summary>
        public int eloScore { get; set; }

        /// <summary>
        /// Optional provided password if required by match
        /// </summary>
        public string password { get; set; }

        override public string ToString()
        {
            return UnityString.Format("[{0}]-networkId:0x{1},publicAddress:{2},privateAddress:{3},eloScore:{4},HasPassword:{5}", base.ToString(), networkId.ToString("X"), publicAddress, privateAddress, eloScore, string.IsNullOrEmpty(password) ? "NO" : "YES");
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && networkId != NetworkID.Invalid;
        }
    }

    [Serializable]
    internal class JoinMatchResponse : BasicResponse
    {
        /// <summary>
        /// IP Address of Host. If self-hosting, ignore.
        /// </summary>
        public string address;

        /// <summary>
        /// Port to connect to at Host with specified IP. If self-hosting, ignore.
        /// </summary>
        public int port;

        /// <summary>
        /// Unity Multiplayer Domain this Response belongs to
        /// </summary>
        public int domain = 0;

        /// <summary>
        /// The match unique identifier for future commands
        /// </summary>

        // This should be a NetworkID, but our serialization system does not support 64-bit enums.
        public ulong networkId;

        /// <summary>
        /// The access token for this client to interact with the match
        /// </summary>
        public string accessTokenString;

        /// <summary>
        /// The client id of the caller for this network
        /// </summary>
        public NodeID nodeId;

        // Designation if a relay server is the destination (not self hosted)
        public bool usingRelay;

        override public string ToString()
        {
            return UnityString.Format("[{0}]-address:{1},port:{2},networkId:0x{3},accessTokenString.IsEmpty:{4},nodeId:0x{5},usingRelay:{6}", base.ToString(), address, port, networkId.ToString("X"), string.IsNullOrEmpty(accessTokenString), nodeId.ToString("X"), usingRelay);
        }
    }

    internal class DestroyMatchRequest : Request
    {
        public NetworkID networkId { get; set; }

        override public string ToString()
        {
            return UnityString.Format("[{0}]-networkId:0x{1}", base.ToString(), networkId.ToString("X"));
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && networkId != NetworkID.Invalid;
        }
    }

    internal class DropConnectionRequest : Request
    {
        public NetworkID networkId { get; set; }

        public NodeID nodeId { get; set; }

        override public string ToString()
        {
            return UnityString.Format("[{0}]-networkId:0x{1},nodeId:0x{2}", base.ToString(), networkId.ToString("X"), nodeId.ToString("X"));
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && networkId != NetworkID.Invalid
                && nodeId != NodeID.Invalid;
        }
    }

    [Serializable]
    internal class DropConnectionResponse : Response
    {
        /// <summary>
        /// The match unique identifier for the match that was requested dropped
        /// </summary>

        // This should be a NetworkID, but our serialization system does not support 64-bit enums.
        public ulong networkId;

        public NodeID nodeId;

        override public string ToString()
        {
            return UnityString.Format("[{0}]-networkId:{1}", base.ToString(), networkId.ToString("X"));
        }
    }

    internal class ListMatchRequest : Request
    {
        /// <summary>
        /// Number of matches showing per page
        /// </summary>
        public int pageSize { get; set; }
        /// <summary>
        /// Page number to fetch, 1 based. (1 will return the first page)
        /// </summary>
        public int pageNum { get; set; }

        /// <summary>
        /// Basic string filtering mechanism
        /// </summary>
        public string nameFilter { get; set; }

        /// <summary>
        /// Determines if passworded matches are included in results. False means return everything, True means return only matches with no password
        /// </summary>
        public bool filterOutPrivateMatches { get; set; }

        /// <summary>
        /// Determines the elo score target to order the returned list. Matches closer to the target score will appear first.
        /// </summary>
        public int eloScore { get; set; }

        /// <summary>
        /// Long list of attributes to match against.
        /// Only matches containing values less than entries in this dictionary and like keys specified will be considered.
        /// Leave this blank to not filter. Maximum of 10 toal filter entries across all dictionaries in this request.
        /// Additional name wildcards will be ignored
        /// </summary>
        public Dictionary<string, long> matchAttributeFilterLessThan { get; set; }

        /// <summary>
        /// Long list of attributes to match against.
        /// Only matches containing values equal to entries in this dictionary and like keys specified will be considered.
        /// Leave this blank to not filter. Maximum of 10 toal filter entries across all dictionaries in this request.
        /// Additional name wildcards will be ignored
        /// </summary>
        public Dictionary<string, long> matchAttributeFilterEqualTo { get; set; }

        /// <summary>
        /// Long list of attributes to match against.
        /// Only matches containing values greater than entries in this dictionary and like keys specified will be considered.
        /// Leave this blank to not filter. Maximum of 10 toal filter entries across all dictionaries in this request.
        /// Additional name wildcards will be ignored
        /// </summary>
        public Dictionary<string, long> matchAttributeFilterGreaterThan { get; set; }

        override public string ToString()
        {
            return UnityString.Format("[{0}]-pageSize:{1},pageNum:{2},nameFilter:{3}, filterOutPrivateMatches:{4}, eloScore:{5}, matchAttributeFilterLessThan.Count:{6}, matchAttributeFilterEqualTo.Count:{7}, matchAttributeFilterGreaterThan.Count:{8}", base.ToString(), pageSize, pageNum, nameFilter, filterOutPrivateMatches, eloScore, matchAttributeFilterLessThan == null ? 0 : matchAttributeFilterLessThan.Count, matchAttributeFilterEqualTo == null ? 0 : matchAttributeFilterEqualTo.Count, matchAttributeFilterGreaterThan == null ? 0 : matchAttributeFilterGreaterThan.Count);
        }

        public override bool IsValid()
        {
            int totalFilters = matchAttributeFilterLessThan == null ? 0 : matchAttributeFilterLessThan.Count;
            totalFilters += matchAttributeFilterEqualTo == null ? 0 : matchAttributeFilterEqualTo.Count;
            totalFilters += matchAttributeFilterGreaterThan == null ? 0 : matchAttributeFilterGreaterThan.Count;
            return base.IsValid()
                && pageSize >= 1 && pageSize <= 1000
                && totalFilters <= 10;
        }

        [Obsolete("This bool is deprecated in favor of filterOutPrivateMatches")]
        public bool includePasswordMatches;
    }

    [Serializable]
    internal class MatchDirectConnectInfo
    {
        public NodeID nodeId;
        public string publicAddress;
        public string privateAddress;
        public HostPriority hostPriority;

        override public string ToString()
        {
            return UnityString.Format("[{0}]-nodeId:{1},publicAddress:{2},privateAddress:{3},hostPriority:{4}", base.ToString(), nodeId, publicAddress, privateAddress, hostPriority);
        }
    }

    [Serializable]
    internal class MatchDesc
    {
        // This should be a NetworkID, but our serialization system does not support 64-bit enums.
        public ulong networkId;
        public string name;
        public int averageEloScore;
        public int maxSize;
        public int currentSize;
        public bool isPrivate;
        public Dictionary<string, long> matchAttributes;
        public NodeID hostNodeId;
        public List<MatchDirectConnectInfo> directConnectInfos;

        override public string ToString()
        {
            return UnityString.Format("[{0}]-networkId:0x{1},name:{2},averageEloScore:{3},maxSize:{4},currentSize:{5},isPrivate:{6},matchAttributes.Count:{7},hostNodeId:{8},directConnectInfos.Count:{9}", base.ToString(), networkId.ToString("X"), name, averageEloScore, maxSize, currentSize, isPrivate, matchAttributes == null ? 0 : matchAttributes.Count, hostNodeId, directConnectInfos.Count);
        }
    }

    [Serializable]
    internal class ListMatchResponse : BasicResponse
    {
        public List<MatchDesc> matches;

        public ListMatchResponse()
        {
            matches = new List<MatchDesc>();
        }

        public ListMatchResponse(List<MatchDesc> otherMatches)
        {
            matches = otherMatches;
        }

        override public string ToString()
        {
            return UnityString.Format("[{0}]-matches.Count:{1}", base.ToString(), (matches == null ? 0 : matches.Count));
        }
    }

    internal class CreateOrJoinMatchRequest : CreateMatchRequest
    {
    }

    internal class SetMatchAttributesRequest : Request
    {
        public NetworkID networkId { get; set; }
        public bool isListed { get; set; }

        override public string ToString()
        {
            return UnityString.Format("[{0}]-networkId:{1},isListed:{2}", base.ToString(), networkId.ToString("X"), isListed);
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && networkId != NetworkID.Invalid;
        }
    }
}
