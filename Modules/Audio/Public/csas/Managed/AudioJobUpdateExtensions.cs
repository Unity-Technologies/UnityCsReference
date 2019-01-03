// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Experimental.Audio
{
    internal static class AudioJobUpdateExtensions
    {
        public unsafe struct AudioJobUpdateStructProduce<Updater, Params, Provs, Job>
            where Params  : struct, IConvertible
            where Provs   : struct, IConvertible
            where Job     : struct, IAudioJob<Params, Provs>
            where Updater : struct, IAudioJobUpdate<Params, Provs, Job>
        {
            // These structures are allocated but never freed at program termination
            static void* m_JobReflectionData;

            public static void Initialize(out void* jobRefData)
            {
                if (m_JobReflectionData == null)
                    m_JobReflectionData = (void*)JobsUtility.CreateJobReflectionData(typeof(Updater), JobType.Single, (ExecuteJobFunction)Execute);

                jobRefData = m_JobReflectionData;
            }

            delegate void ExecuteJobFunction(ref Updater updateJobData, ref Job jobData, IntPtr unused1, IntPtr unused2, ref JobRanges ranges, int ignored2);

            public static void Execute(ref Updater updateJobData, ref Job jobData, IntPtr dspNodePtr, IntPtr unused2, ref JobRanges ranges, int ignored2)
            {
                var context = new ResourceContext
                {
                    m_DSPNodePtr = (void*)dspNodePtr.ToPointer()
                };

                updateJobData.Update(ref jobData, context);
            }
        }

        public static unsafe void GetReflectionData<Updater, Params, Provs, Job>(out void* jobRefData)
            where Params  : struct, IConvertible
            where Provs   : struct, IConvertible
            where Job     : struct, IAudioJob<Params, Provs>
            where Updater : struct, IAudioJobUpdate<Params, Provs, Job>
        {
            AudioJobUpdateStructProduce<Updater, Params, Provs, Job>.Initialize(out jobRefData);
        }
    }
}

