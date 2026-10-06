// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;

namespace UnityEditor.Marketplace;

interface IServiceTokenProvider
{
    Task<string> GetServiceTokenAsync(CancellationToken cancellationToken);
}

class ServiceTokenProvider : IServiceTokenProvider
{
    // Null when there is no Genesis token to exchange, e.g. when signed out.
    public Task<string> GetServiceTokenAsync(CancellationToken cancellationToken)
    {
        return CloudProjectSettings.GetServiceTokenAsync(cancellationToken);
    }
}
