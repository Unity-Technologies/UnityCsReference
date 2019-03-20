// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal interface IAddOperation : IBaseOperation
    {
        event Action<PackageInfo> OnOperationSuccess;

        PackageInfo PackageInfo { get; }

        void AddPackageAsync(PackageInfo packageInfo, Action<PackageInfo> doneCallbackAction = null,  Action<Error> errorCallbackAction = null);

        void AddPackageAsync(string packageId, Action<PackageInfo> doneCallbackAction = null,  Action<Error> errorCallbackAction = null);
    }
}
