// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Profiling.Editor.UI
{
    interface IDetailsProvider
    {
        public struct AssistantRequestContext
        {
            public readonly string Prompt;
            public readonly CpuProfilerAssistantController.CpuProfilerContext Attachment;

            public AssistantRequestContext(string prompt, CpuProfilerAssistantController.CpuProfilerContext attachment)
            {
                Prompt = prompt;
                Attachment = attachment;
            }
        }

        AssistantRequestContext GetAssistantContext(IProfilerCaptureDataService dataService);
        ViewController GetDetailsViewController(IProfilerCaptureDataService dataService);
    }
}
