// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
using System.Collections.Generic;

namespace Unity.AcceleratorClient.LowLevel
{
    // Test facade: Keys are passed as (ulong hi, ulong lo); not a production API.
    internal sealed class SyncClientTestApi : IDisposable
    {
        private readonly KeyValueClient m_Client;

        public SyncClientTestApi(string organization, Guid projectId, string token)
        {
            var options = new AcceleratorV2ClientOptions
            {
                Organization = organization,
                ProjectId = projectId,
                Token = token,
            };
            m_Client = new KeyValueClient(new DefaultKeyValueHandlerFactory(), options);
        }

        public bool Connect(string serverEndpoint) => m_Client.Connect(serverEndpoint).IsSuccess;

        public string? ConnectError(string serverEndpoint)
        {
            var r = m_Client.Connect(serverEndpoint);
            return r.IsSuccess ? null : r.Error.ToString();
        }

        public bool Put(string ns, ulong keyHi, ulong keyLo, byte[] value)
            => m_Client.Put(new Contracts.CacheNamespace(ns), new UInt128(keyHi, keyLo), value).IsSuccess;

        // Throws on any non-miss error so a real failure never masquerades as a miss.
        public byte[]? GetOrNull(string ns, ulong keyHi, ulong keyLo)
        {
            var r = m_Client.GetSingle(new Contracts.CacheNamespace(ns), new UInt128(keyHi, keyLo));
            if (r.IsSuccess)
                return r.Value.Value.ToArray();
            if (r.Error is Contracts.CacheError.NotFound)
                return null;
            throw new Exception("GetSingle failed (not a miss): " + r.Error);
        }

        // Keys and values are parallel-indexed.
        public bool PutBatch(string ns, IReadOnlyList<(ulong hi, ulong lo)> keys, IReadOnlyList<byte[]> values)
        {
            var entries = new List<KeyValuePair<UInt128, ReadOnlyMemory<byte>>>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
                entries.Add(new KeyValuePair<UInt128, ReadOnlyMemory<byte>>(
                    new UInt128(keys[i].hi, keys[i].lo), values[i]));
            return m_Client.PutBatch(new Contracts.CacheNamespace(ns), entries).IsSuccess;
        }

        public void Dispose() { /* KeyValueClient holds no IDisposable resources in the sync surface */ }
    }
}
