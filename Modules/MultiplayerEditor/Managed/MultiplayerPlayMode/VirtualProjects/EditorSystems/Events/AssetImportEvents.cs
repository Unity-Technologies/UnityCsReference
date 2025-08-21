// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class AssetImportEvents
    {
        public event Action<bool, int> RequestImport;
        public void InvokeRequestImport(bool didDomainReload, int numAssetsChanged)
        {
            RequestImport?.Invoke(didDomainReload, numAssetsChanged);
        }
    }
}
