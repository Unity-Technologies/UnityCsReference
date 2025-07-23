// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmEmbedOperation : UpmBaseOperation<EmbedRequest>
    {
        public override RefreshOptions refreshOptions { get; }

        public void Embed(string packageName)
        {
            m_PackageIdOrName = packageName;
            Start();
        }

        protected override EmbedRequest CreateRequest()
        {
            return m_ClientProxy.Embed(packageName);
        }
    }
}
