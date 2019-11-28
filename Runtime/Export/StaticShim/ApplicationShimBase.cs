// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    internal class ApplicationShimBase : IDisposable
    {
        public void Dispose()
        {
            ShimManager.RemoveShim(this);
        }

        public bool IsActive()
        {
            return ShimManager.IsShimActive(this);
        }

        public void OnLowMemory()
        {
            Application.CallLowMemory();
        }

        public virtual bool isEditor => ApplicationEditor.isEditor;

        public virtual RuntimePlatform platform => ApplicationEditor.platform;

        public virtual bool isMobilePlatform => ApplicationEditor.isMobilePlatform;

        public virtual bool isConsolePlatform => ApplicationEditor.isConsolePlatform;

        public virtual SystemLanguage systemLanguage => ApplicationEditor.systemLanguage;

        public virtual NetworkReachability internetReachability => ApplicationEditor.internetReachability;
    }
}

