// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Connect
{
    partial class ServiceToken
    {
        readonly ITokenExchange m_TokenExchange;
        readonly IGenesisAndServiceTokenCaching m_GenesisAndServiceTokenCaching;
        readonly object m_ExchangeLock = new object();
        Task<string> m_Exchange;
        string m_ExchangeGenesisToken;

        internal static ServiceToken Instance => k_LazyInstance.Value;

        [AutoStaticsCleanupOnCodeReload]
        static Lazy<ServiceToken> k_LazyInstance = new Lazy<ServiceToken>(() =>
        {
            var tokenExchange = new TokenExchange();
            var tokenCaching = new GenesisAndServiceTokenCaching();
            return new ServiceToken(tokenExchange, tokenCaching);
        });

        internal ServiceToken(
            ITokenExchange tokenExchange,
            IGenesisAndServiceTokenCaching genesisAndServiceTokenCaching)
        {
            m_TokenExchange = tokenExchange;
            m_GenesisAndServiceTokenCaching = genesisAndServiceTokenCaching;
        }

        public async Task<string> GetServiceTokenAsync(string genesisToken, CancellationToken cancellationToken = default)
        {
            var cachedTokens = m_GenesisAndServiceTokenCaching.LoadCache();

            var nextRefreshTime = m_GenesisAndServiceTokenCaching.GetNextRefreshTime(cachedTokens.GatewayToken);

            if (genesisToken == cachedTokens.GenesisToken && DateTime.UtcNow < nextRefreshTime)
                return cachedTokens.GatewayToken;

            if (string.IsNullOrEmpty(genesisToken))
            {
                lock (m_ExchangeLock)
                {
                    // An exchange still running for the signed-out user must not write its token
                    m_ExchangeGenesisToken = null;
                    m_GenesisAndServiceTokenCaching.SaveCache(new Tokens { GenesisToken = genesisToken });
                }
                return null;
            }

            Task<string> exchange;
            lock (m_ExchangeLock)
            {
                if (m_Exchange == null || m_Exchange.IsCompleted || m_ExchangeGenesisToken != genesisToken)
                {
                    m_ExchangeGenesisToken = genesisToken;
                    m_Exchange = ExchangeAndCacheAsync(genesisToken);
                }
                exchange = m_Exchange;
            }

            return await WaitForExchangeAsync(exchange, cancellationToken);
        }

        // Callers share the exchange, so none of them can cancel it; the request timeout ends it
        async Task<string> ExchangeAndCacheAsync(string genesisToken)
        {
            var gatewayToken = await m_TokenExchange.GetServiceTokenAsync(genesisToken, CancellationToken.None);

            lock (m_ExchangeLock)
            {
                // An exchange for a newer genesis token owns the cache
                if (m_ExchangeGenesisToken == genesisToken)
                    m_GenesisAndServiceTokenCaching.SaveCache(new Tokens { GenesisToken = genesisToken, GatewayToken = gatewayToken });
            }

            return gatewayToken;
        }

        static async Task<string> WaitForExchangeAsync(Task<string> exchange, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
                return await exchange;

            var cancelled = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
            {
                if (await Task.WhenAny(exchange, cancelled.Task) != exchange)
                    cancellationToken.ThrowIfCancellationRequested();
            }

            return await exchange;
        }
    }
}


