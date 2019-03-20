// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI
{
    internal class UpmSearchOperation : UpmBaseOperation, ISearchOperation
    {
        [SerializeField]
        private Action<IEnumerable<PackageInfo>> _doneCallbackAction;

        private string _packageNameOrId;

        public void GetAllPackageAsync(string packageNameOrId = null, Action<IEnumerable<PackageInfo>> doneCallbackAction = null, Action<Error> errorCallbackAction = null)
        {
            _doneCallbackAction = doneCallbackAction;
            OnOperationError += errorCallbackAction;
            _packageNameOrId = packageNameOrId;

            Start();
        }

        protected override Request CreateRequest()
        {
            if (string.IsNullOrEmpty(_packageNameOrId))
                return Client.SearchAll();
            else
                return Client.Search(_packageNameOrId);
        }

        protected override void ProcessData()
        {
            var request = CurrentRequest as SearchRequest;
            var packages = new List<PackageInfo>();
            foreach (var upmPackage in request.Result)
            {
                var packageInfos = FromUpmPackageInfo(upmPackage, false);
                packages.AddRange(packageInfos);
            }
            _doneCallbackAction(packages);
        }
    }
}
