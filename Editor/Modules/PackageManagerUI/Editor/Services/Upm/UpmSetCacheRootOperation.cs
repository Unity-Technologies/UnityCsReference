// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmSetCacheRootOperation : UpmBaseOperation<SetCacheRootRequest>
    {
        [SerializeField]
        protected string m_Path;
        public string path => m_Path;

        public override RefreshOptions refreshOptions => RefreshOptions.None;

        public void SetCacheRoot(string path)
        {
            m_Path = path;
            Start();
        }

        protected override SetCacheRootRequest CreateRequest()
        {
            return m_ClientProxy.SetCacheRoot(m_Path);
        }
    }
}
