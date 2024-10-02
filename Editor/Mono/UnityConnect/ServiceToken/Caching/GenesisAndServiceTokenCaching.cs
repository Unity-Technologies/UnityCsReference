// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Connect
{
    class GenesisAndServiceTokenCaching : IGenesisAndServiceTokenCaching
    {
        internal const string CacheKey = "Editor.GatewayTokens.Cache";
        readonly TimeSpan m_RefreshGracePeriod = TimeSpan.FromMinutes(30);

        public Tokens LoadCache()
        {
            var serializedTokens = SessionState.GetString(CacheKey, string.Empty);

            if (string.IsNullOrEmpty(serializedTokens))
            {
                return new Tokens();
            }

            var deserializedTokens = Json.Deserialize(serializedTokens) as Dictionary<string,object>;

            if (deserializedTokens == null)
            {
                return new Tokens();
            }

            return new Tokens()
            {
                GenesisToken = deserializedTokens.GetValueOrDefault(nameof(Tokens.GenesisToken))?.ToString(),
                GatewayToken = deserializedTokens.GetValueOrDefault(nameof(Tokens.GatewayToken))?.ToString()
            };
        }

        public void SaveCache(Tokens tokens)
        {
            var serialized = Json.Serialize(tokens);
            SessionState.SetString(CacheKey, serialized);
        }

        public DateTime GetNextRefreshTime(string gatewayToken)
        {
            try
            {
                if (string.IsNullOrEmpty(gatewayToken))
                {
                    return new DateTime();
                }

                var jwt = JsonWebToken.Decode(gatewayToken);
                return jwt.exp - m_RefreshGracePeriod;
            }
            catch
            {
                return new DateTime();
            }
        }
    }
}
