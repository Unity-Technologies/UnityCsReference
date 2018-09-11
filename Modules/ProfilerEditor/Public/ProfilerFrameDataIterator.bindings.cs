// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEditorInternal
{
    [NativeHeader("Modules/ProfilerEditor/ProfilerHistory/ProfilerFrameData.h")]
    [StructLayout(LayoutKind.Sequential)]
    public partial class ProfilerFrameDataIterator : IDisposable
    {
        private IntPtr m_Ptr;

        public ProfilerFrameDataIterator()
        {
            m_Ptr = Internal_Create();
        }

        private void FreeNativeResources()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            FreeNativeResources();
            GC.SuppressFinalize(this);
        }

        ~ProfilerFrameDataIterator()
        {
            FreeNativeResources();
        }

        [NativeMethod("GetNext")]
        public extern bool Next(bool enterChildren);

        public extern int GetThreadCount(int frame);

        public extern double GetFrameStartS(int frame);
        public extern int GetGroupCount(int frame);

        public extern string GetGroupName();

        public extern string GetThreadName();

        public extern void SetRoot(int frame, int threadIdx);

        public extern int group
        {
            get;
        }

        public extern int depth
        {
            get;
        }

        /// <summary>
        /// The maximal depth of the stacked samples. This count includes the thread root as well as counters.
        /// </summary>
        public extern int maxDepth
        {
            get;
        }

        public extern string path
        {
            [NativeMethod("GetFunctionPath")]
            get;
        }

        public extern string name
        {
            [NativeMethod("GetFunctionName")]
            get;
        }

        public extern int sampleId
        {
            get;
        }

        [Obsolete("Use instanceId instead", false)]
        public int id
        {
            get { return instanceId; }
        }

        public extern int instanceId
        {
            get;
        }

        public extern float frameTimeMS
        {
            get;
        }

        public extern float startTimeMS
        {
            get;
        }

        public extern float durationMS
        {
            get;
        }

        public extern string extraTooltipInfo
        {
            [NativeMethod("GetExtraTooltipInfo")]
            get;
        }

        extern internal bool GetSampleCallstack(out string resolvedStack);

        private static extern IntPtr Internal_Create();

        [ThreadSafe]
        private static extern void Internal_Destroy(IntPtr ptr);
    }
}
