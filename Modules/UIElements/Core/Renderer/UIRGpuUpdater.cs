// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements.UIR
{
    enum GpuUpdaterType
    {
        // The vertex/index GPU buffers are updated directly through mapped updates. This typically requires some
        // synchronization to avoid updating data that is being used by the GPU.
        Mapped,

        // A GPU staging buffer is mapped and updated with all the changes of the frame. Then, GPU copies are performed
        // from this buffer to the vertex/index GPU buffers. The staging buffer cannot be immediately be reused. This is
        // equivalent to StagedFull with the exception that there is no CPU staging buffer.
        StagedGpuOnly,

        // The changes are written to a CPU staging buffer. The GPU staging buffer is then updated and used as a source
        // for GPU copies to the vertex/index GPU buffers. The GPU copies are performed after the GPU is done using the
        // buffer so no synchronization is required. However the staging buffer cannot be immediately be reused.
        StagedCpuGpu,
    }

    abstract class GpuUpdater : IDisposable
    {
        // Called before the data can be modified
        public abstract void AdvanceFrame();

        // Queues the changes from the DataSet to be sent to the GPU
        public abstract void ProcessDataSet(DataSet dataSet);

        // Completes the update process, ensuring all data has been sent to the GPU
        public abstract void CompleteUpdate();

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (!disposing)
                DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion
    }
}
