// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityEditor.Connect
{
    class ServiceToken
    {
        readonly ITokenExchange m_TokenExchange;
        readonly IGenesisAndServiceTokenCaching m_GenesisAndServiceTokenCaching;
        readonly DateTime m_DateTime;

        internal static ServiceToken Instance => k_LazyInstance.Value;

        static readonly Lazy<ServiceToken> k_LazyInstance = new Lazy<ServiceToken>(() =>
        {
            var cloudEnvironmentConfigProvider = new CloudEnvironmentConfigProvider();
            var tokenExchange = new TokenExchange(cloudEnvironmentConfigProvider);
            var tokenCaching = new GenesisAndServiceTokenCaching();
            return new ServiceToken(tokenExchange, tokenCaching);
        });

        internal ServiceToken(
            ITokenExchange tokenExchange,
            IGenesisAndServiceTokenCaching genesisAndServiceTokenCaching)
        {
            m_TokenExchange = tokenExchange;
            m_GenesisAndServiceTokenCaching = genesisAndServiceTokenCaching;
            m_DateTime = DateTime.UtcNow;
        }

        public async Task<string> GetServiceTokenAsync(string genesisToken, CancellationToken cancellationToken = default)
        {
            Tokens cachedTokens = new();
            await AsyncUtils.RunNextActionOnMainThread(() => cachedTokens = m_GenesisAndServiceTokenCaching.LoadCache());

            var nextRefreshTime = m_GenesisAndServiceTokenCaching.GetNextRefreshTime(cachedTokens.GatewayToken);

            if (genesisToken != cachedTokens.GenesisToken || m_DateTime.ToUniversalTime() >= nextRefreshTime)
            {
                if (!string.IsNullOrEmpty(genesisToken))
                {
                    try
                    {
                        cachedTokens.GatewayToken =
                            await m_TokenExchange.GetServiceTokenAsync(genesisToken, cancellationToken);
                    }
                    catch
                    {
                        cachedTokens.GatewayToken = null;
                        throw;
                    }
                }
                else
                {
                    cachedTokens.GatewayToken = null;
                }

                cachedTokens.GenesisToken = genesisToken;
            }

            await AsyncUtils.RunNextActionOnMainThread(() => m_GenesisAndServiceTokenCaching.SaveCache(cachedTokens));
            return cachedTokens.GatewayToken;
        }
    }
}


