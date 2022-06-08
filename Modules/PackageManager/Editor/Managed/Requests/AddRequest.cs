// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.Requests
{
    [Serializable]
    public sealed partial class AddRequest : Request<PackageInfo>
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
        private AddRequest()
            : base()
        {
        }

        internal AddRequest(long operationId, NativeStatusCode initialStatus)
            : base(operationId, initialStatus)
        {
            m_Progress = new RequestProgress(Id);
        }

        protected override PackageInfo GetResult()
        {
            return GetOperationData(Id);
        }
    }
}
