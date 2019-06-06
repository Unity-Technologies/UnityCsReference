// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;
using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    internal class UpmEmbedOperation : UpmBaseOperation, IEmbedOperation
    {
        public PackageInfo PackageInfo { get; protected set; }

        public event Action<PackageInfo> OnOperationSuccess = delegate {};

        public void EmbedPackageAsync(PackageInfo packageInfo, Action<PackageInfo> doneCallbackAction = null, Action<Error> errorCallbackAction = null)
        {
            PackageInfo = packageInfo;
            OnOperationError += errorCallbackAction;
            OnOperationSuccess += doneCallbackAction;

            Start();
        }

        protected override Request CreateRequest()
        {
            return Client.Embed(PackageInfo.Name);
        }

        protected override void ProcessData()
        {
            var request = CurrentRequest as EmbedRequest;
            var package = FromUpmPackageInfo(request.Result).FirstOrDefault();
            OnOperationSuccess(package);
        }
    }
}
