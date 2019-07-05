// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IListOperation : IBaseOperation
    {
        bool OfflineMode { get; set; }
        void GetPackageListAsync(Action<IEnumerable<PackageInfo>> doneCallbackAction, Action<Error> errorCallbackAction = null);
    }
}
