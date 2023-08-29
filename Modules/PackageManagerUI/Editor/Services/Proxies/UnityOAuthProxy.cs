// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;
using UnityEditor.Connect;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IUnityOAuthProxy : IService
    {
        void GetAuthorizationCodeAsync(string clientId, Action<UnityOAuth.AuthCodeResponse> callback);
    }

    [ExcludeFromCodeCoverage]
    internal class UnityOAuthProxy : BaseService<IUnityOAuthProxy>, IUnityOAuthProxy
    {
        public void GetAuthorizationCodeAsync(string clientId, Action<UnityOAuth.AuthCodeResponse> callback)
        {
            try
            {
                UnityOAuth.GetAuthorizationCodeAsync(clientId, callback);
            }
            catch (Exception e)
            {
                callback?.Invoke(new UnityOAuth.AuthCodeResponse { Exception = e});
            }
        }
    }
}
