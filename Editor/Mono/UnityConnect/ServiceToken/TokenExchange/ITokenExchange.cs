// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;

namespace UnityEditor.Connect
{
    interface ITokenExchange
    {
        Task<string> GetServiceTokenAsync(string genesisToken, CancellationToken cancellationToken);
    }
}
