// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Connect
{
    [Serializable]
    class TokenExchangeRequest
    {
        public string token;

        public TokenExchangeRequest() {}

        public TokenExchangeRequest(string token)
        {
            this.token = token;
        }
    }
}
