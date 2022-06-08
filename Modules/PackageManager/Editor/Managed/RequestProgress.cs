// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.PackageManager
{
    [NativeHeader("Modules/PackageManager/Editor/PackageManagerApi.h")]
    [StaticAccessor("PackageManager::Api", StaticAccessorType.DoubleColon)]
    internal class RequestProgress
    {
        private protected delegate void RequestProgressCallback(IntPtr progressUpdatesPtr);

        private protected static extern void SetProgressDelegate(long operationId, RequestProgressCallback progressDelegate);

        private protected static extern void ClearProgressDelegate(long operationId);

        private Action<ProgressUpdateEventArgs>  m_ProgressUpdate;

        private readonly object m_SubscribeLock = new object();

        private long m_OperationId;

        /// <summary>
        /// Event for subscribing to progress updates.
        /// </summary>
        public event Action<ProgressUpdateEventArgs> progressUpdated
        {
            add
            {
                lock (m_SubscribeLock)
                {
                    if (m_ProgressUpdate == null)
                    {
                        SetProgressDelegate(m_OperationId, OnNativeProgress);
                    }

                    m_ProgressUpdate += value;
                }
            }

            remove
            {
                lock (m_SubscribeLock)
                {
                    m_ProgressUpdate -= value;

                    if (m_ProgressUpdate == null)
                    {
                        ClearProgressDelegate(m_OperationId);
                    }
                }
            }
        }

        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManagerCommon.h")]
        [StaticAccessor("PackageManager::PackageProgress", StaticAccessorType.DoubleColon)]
        private static extern PackageProgress[] Internal_GetPackageProgressArray(IntPtr nativeHandle);

        private void OnNativeProgress(IntPtr progressUpdatesPtr)
        {
            var progressUpdates = Internal_GetPackageProgressArray(progressUpdatesPtr);

            InvokeProgressUpdated(new ProgressUpdateEventArgs(progressUpdates));
        }

        private void InvokeProgressUpdated(ProgressUpdateEventArgs e)
        {
            Action<ProgressUpdateEventArgs> handler = m_ProgressUpdate;
            handler?.Invoke(e);
        }

        /// <summary>
        /// Constructor to support serialization.  Internal to prevent
        /// API consumers to extend the class.
        /// </summary>
        internal RequestProgress()
            : base()
        {
        }

        internal RequestProgress(long operationId)
        {
            m_OperationId = operationId;
        }
    }
}
