// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace UnityEngine.UIElements.UIR
{
    delegate void MeshGenerationCallback(MeshGenerationContext meshGenerationContext, object userData);

    class MeshGenerationDeferrer : IDisposable
    {
        public void AddMeshGenerationJob(JobHandle jobHandle)
        {
            m_Dependencies.Enqueue(jobHandle);
        }

        public void AddMeshGenerationCallback(MeshGenerationCallback callback, object userData, MeshGenerationCallbackType callbackType, bool isJobDependent)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            var callbackInfo = new CallbackInfo { callback = callback, userData = userData };

            if(!isJobDependent)
            {
                switch (callbackType)
                {
                    case MeshGenerationCallbackType.Fork:
                        m_Fork.Enqueue(callbackInfo);
                        break;
                    case MeshGenerationCallbackType.WorkThenFork:
                        m_WorkThenFork.Enqueue(callbackInfo);
                        break;
                    case MeshGenerationCallbackType.Work:
                        m_Work.Enqueue(callbackInfo);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (callbackType)
                {
                    case MeshGenerationCallbackType.Fork:
                        m_JobDependentFork.Enqueue(callbackInfo);
                        break;
                    case MeshGenerationCallbackType.WorkThenFork:
                        m_JobDependentWorkThenFork.Enqueue(callbackInfo);
                        break;
                    case MeshGenerationCallbackType.Work:
                        m_JobDependentWork.Enqueue(callbackInfo);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        struct CallbackInfo
        {
            public MeshGenerationCallback callback;
            public object userData;
        }

        Queue<CallbackInfo> m_Fork = new(32);
        Queue<CallbackInfo> m_WorkThenFork = new(32);
        Queue<CallbackInfo> m_Work = new(32);

        Queue<CallbackInfo> m_JobDependentFork = new(32);
        Queue<CallbackInfo> m_JobDependentWorkThenFork = new(32);
        Queue<CallbackInfo> m_JobDependentWork = new(32);

        Queue<JobHandle> m_Dependencies = new(32);

        JobMerger m_DependencyMerger = new(64);

        public void ProcessDeferredWork(MeshGenerationContext meshGenerationContext)
        {
            while (true)
            {
                // Store the counts of this iteration because callbacks can enqueue more callbacks which should only
                // be processed on the next iteration.
                int forkCount = m_Fork.Count;
                int workThenForkCount = m_WorkThenFork.Count;
                int workCount = m_Work.Count;

                int jobDependentForkCount = m_JobDependentFork.Count;
                int jobDependentWorkThenForkCount = m_JobDependentWorkThenFork.Count;
                int jobDependentWorkCount = m_JobDependentWork.Count;

                int depCount = m_Dependencies.Count;

                if (forkCount + workThenForkCount + workCount + depCount == 0)
                    break;

                for (int i = 0; i < forkCount; ++i)
                {
                    CallbackInfo ci = m_Fork.Dequeue();
                    Invoke(ci, meshGenerationContext);
                }

                for (int i = 0; i < workThenForkCount; ++i)
                {
                    CallbackInfo ci = m_WorkThenFork.Dequeue();
                    Invoke(ci, meshGenerationContext);
                }

                for (int i = 0; i < workCount; ++i)
                {
                    CallbackInfo ci = m_Work.Dequeue();
                    Invoke(ci, meshGenerationContext);
                }

                for (int i = 0; i < depCount; ++i)
                {
                    m_DependencyMerger.Add(m_Dependencies.Dequeue());
                }
                m_DependencyMerger.MergeAndReset().Complete();

                for (int i = 0 ; i < jobDependentForkCount; ++i)
                {
                    CallbackInfo ci = m_JobDependentFork.Dequeue();
                    Invoke(ci, meshGenerationContext);
                }

                for (int i = 0 ; i < jobDependentWorkThenForkCount; ++i)
                {
                    CallbackInfo ci = m_JobDependentWorkThenFork.Dequeue();
                    Invoke(ci, meshGenerationContext);
                }

                for (int i = 0 ; i < jobDependentWorkCount; ++i)
                {
                    CallbackInfo ci = m_JobDependentWork.Dequeue();
                    Invoke(ci, meshGenerationContext);
                }
            }
        }

        static void Invoke(CallbackInfo ci, MeshGenerationContext mgc)
        {
            try
            {
                ci.callback(mgc, ci.userData);

                if (mgc.visualElement != null)
                {
                    Debug.LogWarning($"MeshGenerationContext is assigned to a VisualElement after calling '{ci.callback}'. Did you forget to call '{nameof(MeshGenerationContext.End)}'?");
                    mgc.End();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                m_DependencyMerger.Dispose();
                m_DependencyMerger = null;
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
