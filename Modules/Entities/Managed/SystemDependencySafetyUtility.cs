// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Entities
{
    unsafe static class SystemDependencySafetyUtility
    {
        public struct SafetyErrorDetails
        {
            internal int m_ProblematicSystemId;
            internal TypeIndex m_ProblematicTypeIndex;
            internal int m_ReaderIndex;
            internal AtomicSafetyHandle m_ProblematicHandle;
            internal bool IsWrite => m_ReaderIndex == -1;

            internal static string GetSystemTypeNameFromSystemId(int systemId)
            {
                SystemState* ptr = World.FindSystemStateForId(systemId);

                if (ptr == null)
                {
                    return "unknown";
                }

                var managed = ptr->ManagedSystem;
                if (managed != null)
                {
                    return TypeManager.GetSystemName(managed.GetType()).ToString();
                }

                return ptr->DebugName.ToString();
            }

            internal string FormatToString()
            {
                string systemName = GetSystemTypeNameFromSystemId(m_ProblematicSystemId);
                var type = m_ProblematicTypeIndex;
                AtomicSafetyHandle h = m_ProblematicHandle;
                string errorTail =
                    "but that type was not assigned to the Dependency property. To ensure correct behavior of other systems, " +
                    "the job or a dependency must be assigned to the Dependency property before returning from the OnUpdate method.";

                string verb = IsWrite ? "writes" : "reads";
                var jobName = IsWrite ? AtomicSafetyHandle.GetWriterName(h) : AtomicSafetyHandle.GetReaderName(h, m_ReaderIndex);

                return $"The system {systemName} {verb} {TypeManager.GetType(type)} via {jobName} {errorTail}";
            }
        }

        internal static bool FindSystemSchedulingErrors(
            int currentSystemId, ref UnsafeList<TypeIndex> readingSystems, ref UnsafeList<TypeIndex> writingSystems,
            ComponentDependencyManager* dependencyManager, out SafetyErrorDetails details)
        {
            details = default;

            const int kMaxHandles = 256;
            JobHandle* handles = stackalloc JobHandle[kMaxHandles];
            int* systemIds = stackalloc int[kMaxHandles];

            int mappingCount = Math.Min(JobsUtility.GetSystemIdMappings(handles, systemIds, kMaxHandles), kMaxHandles);

            // Filter out jobs created by current system.
            for (int i = 0; i < mappingCount; ++i)
            {
                if (systemIds[i] == currentSystemId)
                {
                    systemIds[i] = systemIds[mappingCount - 1];
                    handles[i] = handles[mappingCount - 1];
                    --mappingCount;
                }
            }

            // Check that all reading and writing jobs are a dependency of the output job, to
            // catch systems that forget to add one of their jobs to the dependency graph.
            //
            // Note that this check is not strictly needed as we would catch the mistake anyway later,
            // but checking it here means we can flag the system that has the mistake, rather than some
            // other (innocent) system that is doing things correctly.

            //@TODO: It is not ideal that we call m_SafetyManager.GetDependency,
            //       it can result in JobHandle.CombineDependencies calls.
            //       Which seems like debug code might have side-effects

            for (var index = 0; index < readingSystems.Length; index++)
            {
                var type = readingSystems.Ptr[index];
                if (CheckJobDependencies(handles, systemIds, mappingCount, ref details, type, dependencyManager))
                    return true;
            }

            for (var index = 0; index < writingSystems.Length; index++)
            {
                var type = writingSystems.Ptr[index];
                if (CheckJobDependencies(handles, systemIds, mappingCount, ref details, type, dependencyManager))
                    return true;
            }

            return false;
        }

        static bool CheckJobDependencies(JobHandle* handles, int* systemIds, int mappingCount, ref SafetyErrorDetails details, TypeIndex type, ComponentDependencyManager* dependencyManager)
        {
            var h = dependencyManager->Safety.GetSafetyHandle(type, true);

            var readerCount = AtomicSafetyHandle.GetReaderArray(h, 0, IntPtr.Zero);
            JobHandle* readers = stackalloc JobHandle[readerCount];

            AtomicSafetyHandle.GetReaderArray(h, readerCount, (IntPtr)readers);

            for (var i = 0; i < readerCount; ++i)
            {
                if (!dependencyManager->HasReaderOrWriterDependency(type, readers[i]))
                {
                    details.m_ProblematicSystemId = FindSystemId(readers[i], systemIds, handles, mappingCount);
                    details.m_ProblematicTypeIndex = type;
                    details.m_ProblematicHandle = h;
                    details.m_ReaderIndex = i;
                    if (details.m_ProblematicSystemId != -1)
                        return true;
                }
            }

            if (!dependencyManager->HasReaderOrWriterDependency(type, AtomicSafetyHandle.GetWriter(h)))
            {
                details.m_ProblematicSystemId = FindSystemId(AtomicSafetyHandle.GetWriter(h), systemIds, handles, mappingCount);
                details.m_ProblematicTypeIndex = type;
                details.m_ProblematicHandle = h;
                details.m_ReaderIndex = -1;
                if (details.m_ProblematicSystemId != -1)
                    return true;
            }

            return false;
        }

        private static int FindSystemId(JobHandle jobHandle, int* systemIds, JobHandle* handles, int mappingCount)
        {
            for (int i = 0; i < mappingCount; ++i)
            {
                JobHandle candidate = handles[i];
                if (candidate.Equals(jobHandle))
                    return systemIds[i];
            }

            return -1;
        }
    }
}
