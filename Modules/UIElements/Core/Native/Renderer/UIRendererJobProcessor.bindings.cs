// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using Unity.Jobs;

namespace UnityEngine.UIElements.UIR
{
    [NativeHeader("Modules/UIElements/Core/Native/Renderer/UIRendererJobProcessor.h")]
    static class JobProcessor
    {
        internal extern static JobHandle ScheduleNudgeJobs(IntPtr buffer, int jobCount);
        internal extern static JobHandle ScheduleConvertMeshJobs(IntPtr buffer, int jobCount);
        internal extern static JobHandle ScheduleCopyMeshJobs(IntPtr buffer, int jobCount);
    }
}
