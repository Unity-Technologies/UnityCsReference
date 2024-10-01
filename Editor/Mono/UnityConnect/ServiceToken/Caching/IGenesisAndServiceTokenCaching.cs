// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Connect
{
    interface IGenesisAndServiceTokenCaching
    {
        public Tokens LoadCache();
        public void SaveCache(Tokens tokens);
        public DateTime GetNextRefreshTime(string gatewayToken);
    }
}
