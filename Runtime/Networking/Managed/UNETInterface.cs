// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Networking.Types;


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

    internal abstract class ResponseBase
    {
        // A local indicator of if the parsing completed successfully
        // This isnt a property because it's not serialized in the JSON from the server as it's an indicator of success in the post server - client parsing.
        public abstract void Parse(object obj);

        // Helper to parse each type correctly
        public string ParseJSONString(string name, object obj, IDictionary<string, object> dictJsonObj)
        {
            if (dictJsonObj.TryGetValue(name, out obj))
                return obj as string;

            throw new FormatException(name + " not found in JSON dictionary");
        }

        // Helper to parse each type correctly
        public short ParseJSONInt16(string name, object obj, IDictionary<string, object> dictJsonObj)
        {
            if (dictJsonObj.TryGetValue(name, out obj))
                return Convert.ToInt16(obj);

            throw new FormatException(name + " not found in JSON dictionary");
        }

        // Helper to parse each type correctly
        public int ParseJSONInt32(string name, object obj, IDictionary<string, object> dictJsonObj)
        {
            if (dictJsonObj.TryGetValue(name, out obj))
                return Convert.ToInt32(obj);

            throw new FormatException(name + " not found in JSON dictionary");
        }

        // Helper to parse each type correctly
        public long ParseJSONInt64(string name, object obj, IDictionary<string, object> dictJsonObj)
        {
            if (dictJsonObj.TryGetValue(name, out obj))
                return Convert.ToInt64(obj);

            throw new FormatException(name + " not found in JSON dictionary");
        }

        // Helper to parse each type correctly
        public ushort ParseJSONUInt16(string name, object obj, IDictionary<string, object> dictJsonObj)
        {
            if (dictJsonObj.TryGetValue(name, out obj))
                return Convert.ToUInt16(obj);

            throw new FormatException(name + " not found in JSON dictionary");
        }

        // Helper to parse each type correctly
        public uint ParseJSONUInt32(string name, object obj, IDictionary<string, object> dictJsonObj)
        {
            if (dictJsonObj.TryGetValue(name, out obj))
                return Convert.ToUInt32(obj);

            throw new FormatException(name + " not found in JSON dictionary");
        }

        // Helper to parse each type correctly
        public ulong ParseJSONUInt64(string name, object obj, IDictionary<string, object> dictJsonObj)
        {
            if (dictJsonObj.TryGetValue(name, out obj))
                return Convert.ToUInt64(obj);

            throw new FormatException(name + " not found in JSON dictionary");
        }

        // Helper to parse each type correctly
        public bool ParseJSONBool(string name, object obj, IDictionary<string, object> dictJsonObj)
        {
            if (dictJsonObj.TryGetValue(name, out obj))
                return Convert.ToBoolean(obj);

            throw new FormatException(name + " not found in JSON dictionary");
        }

        // Helper to parse each type correctly
        public DateTime ParseJSONDateTime(string name, object obj, IDictionary<string, object> dictJsonObj)
        {
            //FIXME: Handle JSON date time format
            throw new FormatException(name + " DateTime not yet supported");
            //if (dictJsonObj.TryGetValue(name, out obj))
            //return Convert.ToDateTime(obj);

            //throw new FormatException(name + " not found in JSON dictionary");
        }

        // Helper to parse each type correctly
        public List<string> ParseJSONListOfStrings(string name, object obj, IDictionary<string, object> dictJsonObj)
        {
            if (dictJsonObj.TryGetValue(name, out obj))
            {
                List<object> objList = obj as List<object>;
                if (objList != null)
                {
                    List<string> retObj = new List<string>();
                    foreach (IDictionary<string, object> elementDict in objList)
                    {
                        foreach (KeyValuePair<string, object> element in elementDict)
                        {
                            string elementString = (string)element.Value;
                            retObj.Add(elementString);
                        }
                    }
                    return retObj;
                }
            }

            throw new FormatException(name + " not found in JSON dictionary");
        }

        // Helper to parse each type correctly
        public List<T> ParseJSONList<T>(string name, object obj, IDictionary<string, object> dictJsonObj) where T : ResponseBase, new()
        {
            if (dictJsonObj.TryGetValue(name, out obj))
            {
                List<object> objList = obj as List<object>;
                if (objList != null)
                {
                    List<T> retObj = new List<T>();
                    foreach (IDictionary<string, object> elementDict in objList)
                    {
                        T element = new T();
                        element.Parse(elementDict);
                        retObj.Add(element);
                    }
                    return retObj;
                }
            }

            throw new FormatException(name + " not found in JSON dictionary");
        }
    }

    internal interface IResponse
    {
        void SetSuccess();
        void SetFailure(string info);
    }

    internal abstract class Response : ResponseBase, IResponse
    {
        /// <summary>
        /// Indicator on request success
        public bool success { get; private set; }

        public string extendedInfo { get; private set; }

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

        public override void Parse(object obj)
        {
            IDictionary<string, object> dictJsonObj = obj as IDictionary<string, object>;
            if (null != dictJsonObj)
            {
                success = ParseJSONBool("success", obj, dictJsonObj);
                extendedInfo = ParseJSONString("extendedInfo", obj, dictJsonObj);

                if (!success)
                {
                    throw new FormatException("FAILURE Returned from server: " + extendedInfo);
                }
            }
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
        public string address { get; set; }

        /// <summary>
        /// Port to connect to at Host with specified IP. If self-hosting, ignore.
        /// </summary>
        public int port { get; set; }

        /// <summary>
        /// Unity Multiplayer Domain this Response belongs to
        /// </summary>
        public int domain { get; set; }

        /// <summary>
        /// The match unique identifier for future commands
        /// </summary>
        public NetworkID networkId { get; set; }

        /// <summary>
        /// The access token for this client to interact with the match
        /// </summary>
        public string accessTokenString { get; set; }

        /// <summary>
        /// The client id of the caller for this network
        /// </summary>
        public NodeID nodeId { get; set; }

        // Designation if a relay server is the destination (not self hosted)
        public bool usingRelay { get; set; }

        override public string ToString()
        {
            return UnityString.Format("[{0}]-address:{1},port:{2},networkId:0x{3},accessTokenString.IsEmpty:{4},nodeId:0x{5},usingRelay:{6}", base.ToString(), address, port, networkId.ToString("X"), string.IsNullOrEmpty(accessTokenString), nodeId.ToString("X"), usingRelay);
        }

        public override void Parse(object obj)
        {
            base.Parse(obj);

            IDictionary<string, object> dictJsonObj = obj as IDictionary<string, object>;
            if (null != dictJsonObj)
            {
                address = ParseJSONString("address", obj, dictJsonObj);
                port = ParseJSONInt32("port", obj, dictJsonObj);
                networkId = (NetworkID)ParseJSONUInt64("networkId", obj, dictJsonObj);
                accessTokenString = ParseJSONString("accessTokenString", obj, dictJsonObj);
                nodeId = (NodeID)ParseJSONUInt16("nodeId", obj, dictJsonObj);
                usingRelay = ParseJSONBool("usingRelay", obj, dictJsonObj);
            }
            else
            {
                throw new FormatException("While parsing JSON response, found obj is not of type IDictionary<string,object>:" + obj.ToString());
            }
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

    internal class JoinMatchResponse : BasicResponse
    {
        /// <summary>
        /// IP Address of Host. If self-hosting, ignore.
        /// </summary>
        public string address { get; set; }

        /// <summary>
        /// Port to connect to at Host with specified IP. If self-hosting, ignore.
        /// </summary>
        public int port { get; set; }

        /// <summary>
        /// Unity Multiplayer Domain this Response belongs to
        /// </summary>
        public int domain { get; set; }

        /// <summary>
        /// The match unique identifier for future commands
        /// </summary>
        public NetworkID networkId { get; set; }

        /// <summary>
        /// The access token for this client to interact with the match
        /// </summary>
        public string accessTokenString { get; set; }

        /// <summary>
        /// The client id of the caller for this network
        /// </summary>
        public NodeID nodeId { get; set; }

        // Designation if a relay server is the destination (not self hosted)
        public bool usingRelay { get; set; }

        override public string ToString()
        {
            return UnityString.Format("[{0}]-address:{1},port:{2},networkId:0x{3},accessTokenString.IsEmpty:{4},nodeId:0x{5},usingRelay:{6}", base.ToString(), address, port, networkId.ToString("X"), string.IsNullOrEmpty(accessTokenString), nodeId.ToString("X"), usingRelay);
        }

        public override void Parse(object obj)
        {
            base.Parse(obj);

            IDictionary<string, object> dictJsonObj = obj as IDictionary<string, object>;
            if (null != dictJsonObj)
            {
                address = ParseJSONString("address", obj, dictJsonObj);
                port = ParseJSONInt32("port", obj, dictJsonObj);
                networkId = (NetworkID)ParseJSONUInt64("networkId", obj, dictJsonObj);
                accessTokenString = ParseJSONString("accessTokenString", obj, dictJsonObj);
                nodeId = (NodeID)ParseJSONUInt16("nodeId", obj, dictJsonObj);
                usingRelay = ParseJSONBool("usingRelay", obj, dictJsonObj);
            }
            else
            {
                throw new FormatException("While parsing JSON response, found obj is not of type IDictionary<string,object>:" + obj.ToString());
            }
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

    internal class DropConnectionResponse : Response
    {
        /// <summary>
        /// The match unique identifier for the match that was requested dropped
        /// </summary>
        public NetworkID networkId { get; set; }

        public NodeID nodeId { get; set; }

        override public string ToString()
        {
            return UnityString.Format("[{0}]-networkId:{1}", base.ToString(), networkId.ToString("X"));
        }

        public override void Parse(object obj)
        {
            base.Parse(obj);

            IDictionary<string, object> dictJsonObj = obj as IDictionary<string, object>;
            if (null != dictJsonObj)
            {
                networkId = (NetworkID)ParseJSONUInt64("networkId", obj, dictJsonObj);
                nodeId = (NodeID)ParseJSONUInt16("nodeId", obj, dictJsonObj);
            }
            else
            {
                throw new FormatException("While parsing JSON response, found obj is not of type IDictionary<string,object>:" + obj.ToString());
            }
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
                && (pageSize >= 1 || pageSize <= 1000)
                && totalFilters <= 10;
        }

        [Obsolete("This bool is deprecated in favor of filterOutPrivateMatches")]
        public bool includePasswordMatches;
    }

    internal class MatchDirectConnectInfo : ResponseBase
    {
        public NodeID nodeId { get; set; }
        public string publicAddress { get; set; }
        public string privateAddress { get; set; }
        public HostPriority hostPriority { get; set; }

        override public string ToString()
        {
            return UnityString.Format("[{0}]-nodeId:{1},publicAddress:{2},privateAddress:{3},hostPriority:{4}", base.ToString(), nodeId, publicAddress, privateAddress, hostPriority);
        }

        public override void Parse(object obj)
        {
            IDictionary<string, object> dictJsonObj = obj as IDictionary<string, object>;
            if (null != dictJsonObj)
            {
                nodeId = (NodeID)ParseJSONUInt16("nodeId", obj, dictJsonObj);
                publicAddress = ParseJSONString("publicAddress", obj, dictJsonObj);
                privateAddress = ParseJSONString("privateAddress", obj, dictJsonObj);
                hostPriority = (HostPriority)ParseJSONInt32("hostPriority", obj, dictJsonObj);
            }
            else
            {
                throw new FormatException("While parsing JSON response, found obj is not of type IDictionary<string,object>:" + obj.ToString());
            }
        }
    }

    internal class MatchDesc : ResponseBase
    {
        public NetworkID networkId { get; set; }
        public string name { get; set; }
        public int averageEloScore { get; set; }
        public int maxSize { get; set; }
        public int currentSize { get; set; }
        public bool isPrivate { get; set; }
        public Dictionary<string, long> matchAttributes { get; set; }
        public NodeID hostNodeId { get; set; }
        public List<MatchDirectConnectInfo> directConnectInfos { get; set; }

        override public string ToString()
        {
            return UnityString.Format("[{0}]-networkId:0x{1},name:{2},averageEloScore:{3},maxSize:{4},currentSize:{5},isPrivate:{6},matchAttributes.Count:{7},hostNodeId:{8},directConnectInfos.Count:{9}", base.ToString(), networkId.ToString("X"), name, averageEloScore, maxSize, currentSize, isPrivate, matchAttributes == null ? 0 : matchAttributes.Count, hostNodeId, directConnectInfos.Count);
        }

        public override void Parse(object obj)
        {
            IDictionary<string, object> dictJsonObj = obj as IDictionary<string, object>;
            if (null != dictJsonObj)
            {
                networkId = (NetworkID)ParseJSONUInt64("networkId", obj, dictJsonObj);
                name = ParseJSONString("name", obj, dictJsonObj);
                averageEloScore = ParseJSONInt32("averageEloScore", obj, dictJsonObj);
                maxSize = ParseJSONInt32("maxSize", obj, dictJsonObj);
                currentSize = ParseJSONInt32("currentSize", obj, dictJsonObj);
                isPrivate = ParseJSONBool("isPrivate", obj, dictJsonObj);
                hostNodeId = (NodeID)ParseJSONUInt16("hostNodeId", obj, dictJsonObj);
                directConnectInfos = ParseJSONList<MatchDirectConnectInfo>("directConnectInfos", obj, dictJsonObj);
            }
            else
            {
                throw new FormatException("While parsing JSON response, found obj is not of type IDictionary<string,object>:" + obj.ToString());
            }
        }
    }

    internal class ListMatchResponse : BasicResponse
    {
        public List<MatchDesc> matches  { get; set; }

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

        public override void Parse(object obj)
        {
            base.Parse(obj);

            IDictionary<string, object> dictJsonObj = obj as IDictionary<string, object>;
            if (null != dictJsonObj)
            {
                matches = ParseJSONList<MatchDesc>("matches", obj, dictJsonObj);
            }
            else
            {
                throw new FormatException("While parsing JSON response, found obj is not of type IDictionary<string,object>:" + obj.ToString());
            }
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
