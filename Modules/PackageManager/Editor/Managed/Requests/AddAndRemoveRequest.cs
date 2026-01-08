// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    public sealed partial class AddAndRemoveRequest : Request<PackageCollection>
    {
        [NonSerialized]
        private RequestProgress m_Progress;

        /// <summary>
        /// Event for subscribing to progress updates.
        /// </summary>
        internal event Action<ProgressUpdateEventArgs> progressUpdated
        {
            add => m_Progress.progressUpdated += value;
            remove => m_Progress.progressUpdated -= value;
        }

        /// <summary>
        /// Constructor to support serialization
        /// </summary>
        private AddAndRemoveRequest()
        {
        }

        internal AddAndRemoveRequest(long operationId, NativeStatusCode initialStatus)
            : base(operationId, initialStatus)
        {
            m_Progress = new RequestProgress(Id);
        }

        protected override PackageCollection GetResult()
        {
            var operationStatus = GetOperationData(Id);
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var packageList = operationStatus.packageList.Where(p => p.type != ShimPackageType);
#pragma warning restore RS0030
            return new PackageCollection(packageList, operationStatus.error);
        }
    }
}
