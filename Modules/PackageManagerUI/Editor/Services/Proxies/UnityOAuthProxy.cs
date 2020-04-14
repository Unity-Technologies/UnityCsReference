// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Connect;

namespace UnityEditor.PackageManager.UI
{
    internal class UnityOAuthProxy
    {
        public virtual void GetAuthorizationCodeAsync(string clientId, Action<UnityOAuth.AuthCodeResponse> callback)
        {
            UnityOAuth.GetAuthorizationCodeAsync(clientId, callback);
        }
    }
}
