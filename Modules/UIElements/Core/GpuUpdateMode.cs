// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Defines how GPU buffers (vertex/index) are updated.
    /// </summary>
    enum GpuUpdateMode
    {
        /// <summary>
        /// Uses mapped when non-synchronized sub-updates are supported, and otherwise falls back to StagingBuffer (e.g. WebGL, WebGPU).
        /// </summary>
        Default = 0,

        /// <summary>
        /// Persistent GPU buffers are updated through mapping and sub-updates.
        /// </summary>
        /// <remarks>
        /// Data Flow:
        /// 1. User/Temp CPU Buffer (original data, conversion source)
        /// 2. Persistent CPU Buffer (conversion destination)
        /// 3. Persistent GPU Buffer (mapped)
        ///
        /// New data can be written immediately the allocated location on the GPU, provided that it's not been used in
        /// the last 4 frames. Updated data is temporarily allocated at the end of a GPU buffer and moved back to the
        /// original location 4 frames later.
        /// </remarks>
        MappedSubUpdates = 1,

        /// <summary>
        /// Persistent GPU buffers are updated through intermediate CPU and GPU staging buffers.
        /// </summary>
        /// <remarks>
        /// Data Flow:
        /// 1. User/Temp CPU Buffer (original data, conversion source)
        /// 2. Persistent CPU Buffer (conversion destination)
        /// 3. Staging CPU Buffer (GPU update source, packed)
        /// 4. Staging GPU Buffer (GPU update destination, packed)
        /// 5. Persistent GPU Buffer (GPU copy destination)
        ///
        /// New and updated data is copied (packed) into the staging CPU buffer, which is then uploaded to the staging
        /// GPU buffer. Finally GPU copies are scheduled from the staging GPU buffer and the persistent GPU buffer.
        /// Like StagedHybrid, these GPU copies happen on GPU after the GPU is done using the buffers so no fancy
        /// book-keeping is required.
        ///
        /// This approach is preferred on APIs that don't support mapped sub-updates. It is generally less expensive
        /// than re-uploading the entire buffer. However an extra copy can also negatively impact bandwidth.
        /// </remarks>
        StagingBuffer = 2,
    }
}
