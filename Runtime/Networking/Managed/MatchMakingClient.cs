// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking.Types;

namespace UnityEngine.Networking.Match
{
    // returned when you create or join a match (private info)
    //[Serializable] //TODO: enabled this when 64 bit enum issue is resolved
    public class MatchInfo
    {
        public string address { get; private set; }
        public int port { get; private set; }
        public int domain { get; private set; }
        public NetworkID networkId { get; private set; }
        public NetworkAccessToken accessToken { get; private set; }
        public NodeID nodeId { get; private set; }
        public bool usingRelay { get; private set; }

        public MatchInfo()
        {}

        internal MatchInfo(CreateMatchResponse matchResponse)
        {
            address = matchResponse.address;
            port = matchResponse.port;
            domain = matchResponse.domain;
            networkId = matchResponse.networkId;
            accessToken = new NetworkAccessToken(matchResponse.accessTokenString);
            nodeId = matchResponse.nodeId;
            usingRelay = matchResponse.usingRelay;
        }

        internal MatchInfo(JoinMatchResponse matchResponse)
        {
            address = matchResponse.address;
            port = matchResponse.port;
            domain = matchResponse.domain;
            networkId = matchResponse.networkId;
            accessToken = new NetworkAccessToken(matchResponse.accessTokenString);
            nodeId = matchResponse.nodeId;
            usingRelay = matchResponse.usingRelay;
        }

        public override string ToString()
        {
            return UnityString.Format("{0} @ {1}:{2} [{3},{4}]", networkId, address, port, nodeId, usingRelay);
        }
    }

    public class MatchInfoSnapshot
    {
        public NetworkID networkId { get; private set; }
        public NodeID hostNodeId { get; private set; }
        public string name { get; private set; }
        public int averageEloScore { get; private set; }
        public int maxSize { get; private set; }
        public int currentSize { get; private set; }
        public bool isPrivate { get; private set; }

        public Dictionary<string, long> matchAttributes { get; private set; }
        public List<MatchInfoDirectConnectSnapshot> directConnectInfos { get; private set; }


        public class MatchInfoDirectConnectSnapshot
        {
            public NodeID nodeId { get; private set; }
            public string publicAddress { get; private set; }
            public string privateAddress { get; private set; }
            public HostPriority hostPriority { get; private set; }

            public MatchInfoDirectConnectSnapshot()
            {}

            internal MatchInfoDirectConnectSnapshot(MatchDirectConnectInfo matchDirectConnectInfo)
            {
                nodeId = matchDirectConnectInfo.nodeId;
                publicAddress = matchDirectConnectInfo.publicAddress;
                privateAddress = matchDirectConnectInfo.privateAddress;
                hostPriority = matchDirectConnectInfo.hostPriority;
            }
        }

        public MatchInfoSnapshot()
        {}

        internal MatchInfoSnapshot(MatchDesc matchDesc)
        {
            networkId = matchDesc.networkId;
            hostNodeId = matchDesc.hostNodeId;
            name = matchDesc.name;
            averageEloScore = matchDesc.averageEloScore;
            maxSize = matchDesc.maxSize;
            currentSize = matchDesc.currentSize;
            isPrivate = matchDesc.isPrivate;

            matchAttributes = matchDesc.matchAttributes;

            directConnectInfos = new List<MatchInfoDirectConnectSnapshot>();
            foreach (MatchDirectConnectInfo dci in matchDesc.directConnectInfos)
            {
                directConnectInfos.Add(new MatchInfoDirectConnectSnapshot(dci));
            }
        }
    }

    public class NetworkMatch : MonoBehaviour
    {
        public delegate void BasicResponseDelegate(bool success, string extendedInfo);
        public delegate void DataResponseDelegate<T>(bool success, string extendedInfo, T responseData);

        private delegate void InternalResponseDelegate<T, U>(T response, U userCallback);

        private Uri m_BaseUri = new Uri("https://mm.unet.unity3d.com");

        public Uri baseUri { get { return m_BaseUri; } set { m_BaseUri = value; } }

        // The methods that perform operations return an IEnumerator to pass to StartCoroutine.
        // The specified delegate function will be called when the web request completed. For example:
        // - StartCoroutine(myMatchMaker.Create(create, OnMatchCreate));
        // - StartCoroutine(myMatchMaker.JoinMatch(myGuid, myMatch.matchId, "", OnMatchJoined));

        [Obsolete("This function is not used any longer to interface with the matchmaker. Please set up your project by logging in through the editor connect dialog.", true)]
        public void SetProgramAppID(AppID programAppID)
        {
        }

        public Coroutine CreateMatch(string matchName, uint matchSize, bool matchAdvertise, string matchPassword, string publicClientAddress, string privateClientAddress, int eloScoreForMatch, int requestDomain, DataResponseDelegate<MatchInfo> callback)
        {
            if (UnityEngine.Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Debug.LogError("Matchmaking is not supported on WebGL player.");
                return null;
            }
            else
                return CreateMatch(new CreateMatchRequest { name = matchName, size = matchSize, advertise = matchAdvertise, password = matchPassword, publicAddress = publicClientAddress, privateAddress = privateClientAddress, eloScore = eloScoreForMatch, domain = requestDomain }, callback);
        }

        // Begin Create a match
        internal Coroutine CreateMatch(CreateMatchRequest req, DataResponseDelegate<MatchInfo> callback)
        {
            if (callback == null)
            {
                Debug.Log("callback supplied is null, aborting CreateMatch Request.");
                return null;
            }

            Uri uri = new Uri(baseUri, "/json/reply/CreateMatchRequest");
            Debug.Log("MatchMakingClient Create :" + uri);

            var data = new WWWForm();

            data.AddField("version", UnityEngine.Networking.Match.Request.currentVersion);
            data.AddField("projectId", Application.cloudProjectId);
            data.AddField("sourceId", Utility.GetSourceID().ToString());
            data.AddField("accessTokenString", 0); // Set the access token to 0 for any new match request
            data.AddField("domain", req.domain);

            data.AddField("name", req.name);
            data.AddField("size", req.size.ToString());
            data.AddField("advertise", req.advertise.ToString());
            data.AddField("password", req.password);
            data.AddField("publicAddress", req.publicAddress);
            data.AddField("privateAddress", req.privateAddress);
            data.AddField("eloScore", req.eloScore.ToString());

            data.headers["Accept"] = "application/json";

            var client = UnityWebRequest.Post(uri.ToString(), data);
            return StartCoroutine(ProcessMatchResponse<CreateMatchResponse, DataResponseDelegate<MatchInfo>>(client, OnMatchCreate, callback));
        }

        internal virtual void OnMatchCreate(CreateMatchResponse response, DataResponseDelegate<MatchInfo> userCallback)
        {
            if (response.success)
                Utility.SetAccessTokenForNetwork(response.networkId, new NetworkAccessToken(response.accessTokenString));

            userCallback(response.success, response.extendedInfo, new MatchInfo(response));
        }

        public Coroutine JoinMatch(NetworkID netId, string matchPassword, string publicClientAddress, string privateClientAddress, int eloScoreForClient, int requestDomain, DataResponseDelegate<MatchInfo> callback)
        {
            return JoinMatch(new JoinMatchRequest { networkId = netId, password = matchPassword, publicAddress = publicClientAddress, privateAddress = privateClientAddress, eloScore = eloScoreForClient, domain = requestDomain }, callback);
        }

        // Begin joining a match
        internal Coroutine JoinMatch(JoinMatchRequest req, DataResponseDelegate<MatchInfo> callback)
        {
            if (callback == null)
            {
                Debug.Log("callback supplied is null, aborting JoinMatch Request.");
                return null;
            }

            Uri uri = new Uri(baseUri, "/json/reply/JoinMatchRequest");
            Debug.Log("MatchMakingClient Join :" + uri);

            var data = new WWWForm();

            data.AddField("version", UnityEngine.Networking.Match.Request.currentVersion);
            data.AddField("projectId", Application.cloudProjectId);
            data.AddField("sourceId", Utility.GetSourceID().ToString());
            data.AddField("accessTokenString", 0);
            data.AddField("domain", req.domain);

            data.AddField("networkId", req.networkId.ToString());
            data.AddField("password", req.password);
            data.AddField("publicAddress", req.publicAddress);
            data.AddField("privateAddress", req.privateAddress);
            data.AddField("eloScore", req.eloScore.ToString());

            data.headers["Accept"] = "application/json";

            var client = UnityWebRequest.Post(uri.ToString(), data);
            return StartCoroutine(ProcessMatchResponse<JoinMatchResponse, DataResponseDelegate<MatchInfo>>(client, OnMatchJoined, callback));
        }

        internal void OnMatchJoined(JoinMatchResponse response, DataResponseDelegate<MatchInfo> userCallback)
        {
            if (response.success)
                Utility.SetAccessTokenForNetwork(response.networkId, new NetworkAccessToken(response.accessTokenString));

            userCallback(response.success, response.extendedInfo, new MatchInfo(response));
        }

        public Coroutine DestroyMatch(NetworkID netId, int requestDomain, BasicResponseDelegate callback)
        {
            return DestroyMatch(new DestroyMatchRequest { networkId = netId, domain = requestDomain }, callback);
        }

        // Start to destroy a match
        internal Coroutine DestroyMatch(DestroyMatchRequest req, BasicResponseDelegate callback)
        {
            if (callback == null)
            {
                Debug.Log("callback supplied is null, aborting DestroyMatch Request.");
                return null;
            }

            Uri uri = new Uri(baseUri, "/json/reply/DestroyMatchRequest");
            Debug.Log("MatchMakingClient Destroy :" + uri.ToString());

            var data = new WWWForm();

            data.AddField("version", UnityEngine.Networking.Match.Request.currentVersion);
            data.AddField("projectId", Application.cloudProjectId);
            data.AddField("sourceId", Utility.GetSourceID().ToString());
            data.AddField("accessTokenString", Utility.GetAccessTokenForNetwork(req.networkId).GetByteString());
            data.AddField("domain", req.domain);

            data.AddField("networkId", req.networkId.ToString());

            data.headers["Accept"] = "application/json";

            var client = UnityWebRequest.Post(uri.ToString(), data);
            return StartCoroutine(ProcessMatchResponse<BasicResponse, BasicResponseDelegate>(client, OnMatchDestroyed, callback));
        }

        internal void OnMatchDestroyed(BasicResponse response, BasicResponseDelegate userCallback)
        {
            userCallback(response.success, response.extendedInfo);
        }

        public Coroutine DropConnection(NetworkID netId, NodeID dropNodeId, int requestDomain, BasicResponseDelegate callback)
        {
            return DropConnection(new DropConnectionRequest { networkId = netId, nodeId = dropNodeId, domain = requestDomain }, callback);
        }

        // Start to drop a connection from a match
        internal Coroutine DropConnection(DropConnectionRequest req, BasicResponseDelegate callback)
        {
            if (callback == null)
            {
                Debug.Log("callback supplied is null, aborting DropConnection Request.");
                return null;
            }

            Uri uri = new Uri(baseUri, "/json/reply/DropConnectionRequest");
            Debug.Log("MatchMakingClient DropConnection :" + uri);

            var data = new WWWForm();

            data.AddField("version", UnityEngine.Networking.Match.Request.currentVersion);
            data.AddField("projectId", Application.cloudProjectId);
            data.AddField("sourceId", Utility.GetSourceID().ToString());
            data.AddField("accessTokenString", Utility.GetAccessTokenForNetwork(req.networkId).GetByteString());
            data.AddField("domain", req.domain);

            data.AddField("networkId", req.networkId.ToString());
            data.AddField("nodeId", req.nodeId.ToString());

            data.headers["Accept"] = "application/json";

            var client = UnityWebRequest.Post(uri.ToString(), data);
            return StartCoroutine(ProcessMatchResponse<DropConnectionResponse, BasicResponseDelegate>(client, OnDropConnection, callback));
        }

        internal void OnDropConnection(DropConnectionResponse response, BasicResponseDelegate userCallback)
        {
            userCallback(response.success, response.extendedInfo);
        }

        public Coroutine ListMatches(int startPageNumber, int resultPageSize, string matchNameFilter, bool filterOutPrivateMatchesFromResults, int eloScoreTarget, int requestDomain, DataResponseDelegate<List<MatchInfoSnapshot>> callback)
        {
            if (UnityEngine.Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Debug.LogError("Matchmaking is not supported on WebGL player.");
                return null;
            }
            else
                return ListMatches(new ListMatchRequest { pageNum = startPageNumber, pageSize = resultPageSize, nameFilter = matchNameFilter, filterOutPrivateMatches = filterOutPrivateMatchesFromResults, eloScore = eloScoreTarget, domain = requestDomain }, callback);
        }

        // Start getting a list of matches
        internal Coroutine ListMatches(ListMatchRequest req, DataResponseDelegate<List<MatchInfoSnapshot>> callback)
        {
            if (callback == null)
            {
                Debug.Log("callback supplied is null, aborting ListMatch Request.");
                return null;
            }

            Uri uri = new Uri(baseUri, "/json/reply/ListMatchRequest");
            Debug.Log("MatchMakingClient ListMatches :" + uri);

            var data = new WWWForm();

            data.AddField("version", UnityEngine.Networking.Match.Request.currentVersion);
            data.AddField("projectId", Application.cloudProjectId);
            data.AddField("sourceId", Utility.GetSourceID().ToString());
            data.AddField("accessTokenString", 0); // Set access token to 0 for list requests
            data.AddField("domain", req.domain);

            data.AddField("pageSize", req.pageSize);
            data.AddField("pageNum", req.pageNum);
            data.AddField("nameFilter", req.nameFilter);
            data.AddField("filterOutPrivateMatches", req.filterOutPrivateMatches.ToString());
            data.AddField("eloScore", req.eloScore.ToString());

            data.headers["Accept"] = "application/json";

            var client = UnityWebRequest.Post(uri.ToString(), data);
            return StartCoroutine(ProcessMatchResponse<ListMatchResponse, DataResponseDelegate<List<MatchInfoSnapshot>>>(client, OnMatchList, callback));
        }

        internal void OnMatchList(ListMatchResponse response, DataResponseDelegate<List<MatchInfoSnapshot>> userCallback)
        {
            List<MatchInfoSnapshot> matchInfoList = new List<MatchInfoSnapshot>();
            foreach (MatchDesc match in response.matches)
            {
                matchInfoList.Add(new MatchInfoSnapshot(match));
            }

            userCallback(response.success, response.extendedInfo, matchInfoList);
        }

        public Coroutine SetMatchAttributes(NetworkID networkId, bool isListed, int requestDomain, BasicResponseDelegate callback)
        {
            return SetMatchAttributes(new SetMatchAttributesRequest { networkId = networkId, isListed = isListed, domain = requestDomain }, callback);
        }

        // Set attributes for matches in progress
        internal Coroutine SetMatchAttributes(SetMatchAttributesRequest req, BasicResponseDelegate callback)
        {
            if (callback == null)
            {
                Debug.Log("callback supplied is null, aborting SetMatchAttributes Request.");
                return null;
            }

            Uri uri = new Uri(baseUri, "/json/reply/SetMatchAttributesRequest");
            Debug.Log("MatchMakingClient SetMatchAttributes :" + uri);

            var data = new WWWForm();

            data.AddField("version", UnityEngine.Networking.Match.Request.currentVersion);
            data.AddField("projectId", Application.cloudProjectId);
            data.AddField("sourceId", Utility.GetSourceID().ToString());
            data.AddField("accessTokenString", Utility.GetAccessTokenForNetwork(req.networkId).GetByteString());
            data.AddField("domain", req.domain);

            data.AddField("networkId", req.networkId.ToString());
            data.AddField("isListed", req.isListed.ToString());

            data.headers["Accept"] = "application/json";

            var client = UnityWebRequest.Post(uri.ToString(), data);
            return StartCoroutine(ProcessMatchResponse<BasicResponse, BasicResponseDelegate>(client, OnSetMatchAttributes, callback));
        }

        internal void OnSetMatchAttributes(BasicResponse response, BasicResponseDelegate userCallback)
        {
            userCallback(response.success, response.extendedInfo);
        }

        // ------------ private functions below ----------------------


        // Coroutine for getting a match info response
        private IEnumerator ProcessMatchResponse<JSONRESPONSE, USERRESPONSEDELEGATETYPE>(UnityWebRequest client, InternalResponseDelegate<JSONRESPONSE, USERRESPONSEDELEGATETYPE> internalCallback, USERRESPONSEDELEGATETYPE userCallback) where JSONRESPONSE : Response, new()
        {
            // wait for request to complete
            yield return client.SendWebRequest();

            JSONRESPONSE jsonInterface = new JSONRESPONSE();

            if (!(client.isNetworkError || client.isHttpError))
            {
                object o;
                if (SimpleJson.SimpleJson.TryDeserializeObject(client.downloadHandler.text, out o))
                {
                    IDictionary<string, object> dictJsonObj = o as IDictionary<string, object>;
                    if (null != dictJsonObj)
                    {
                        // Catch exception and error handling below will print out some debug info
                        // Callback will be called properly with failure info
                        try
                        {
                            jsonInterface.Parse(o);
                        }
                        catch (FormatException exception)
                        {
                            jsonInterface.SetFailure(UnityString.Format("FormatException:[{0}] ", exception.ToString()));
                        }
                    }
                }
            }
            else
            {
                jsonInterface.SetFailure(UnityString.Format("Request error:[{0}] Raw response:[{1}]", client.error, client.downloadHandler.text));
            }

            client.Dispose();

            internalCallback(jsonInterface, userCallback);
        }
    }
}

