// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [NativeHeader("Modules/PackageManager/Editor/PackageManagerApi.h")]
    [StaticAccessor("PackageManager::Api", StaticAccessorType.DoubleColon)]
    internal class RequestProgress
    {
        private static extern void SetProgressHandler(long operationId, RequestProgress requestProgress);

        private static extern void ClearProgressHandler(long operationId);

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
                        SetProgressHandler(m_OperationId, this);
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
                        ClearProgressHandler(m_OperationId);
                    }
                }
            }
        }

        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManagerCommon.h")]
        [StaticAccessor("PackageManager::PackageProgress", StaticAccessorType.DoubleColon)]
        private static extern PackageProgress[] Internal_GetPackageProgressArray(IntPtr nativeHandle);

        [RequiredByNativeCode]
        internal static void OnNativeProgress(RequestProgress requestProgress, IntPtr progressUpdatesPtr)
        {
            var progressUpdates = Internal_GetPackageProgressArray(progressUpdatesPtr);

            requestProgress.InvokeProgressUpdated(new ProgressUpdateEventArgs(progressUpdates));
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
