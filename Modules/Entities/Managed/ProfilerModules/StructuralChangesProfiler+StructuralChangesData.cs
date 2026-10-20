// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;

namespace Unity.Entities
{
    partial class StructuralChangesProfiler
    {
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_PROFILER")]
        public readonly struct StructuralChangeData
        {
            public readonly SystemHandle ExecutingSystem;
            public readonly long ElapsedNanoseconds;
            public readonly ulong WorldSequenceNumber;
            public readonly StructuralChangeType Type;

            public StructuralChangeData(long elapsed, StructuralChangeType type, ulong worldSeqNumber, in SystemHandle executingSystem)
            {
                ElapsedNanoseconds = elapsed;
                Type = type;
                WorldSequenceNumber = worldSeqNumber;
                ExecutingSystem = executingSystem;
            }
        }
    }
}
