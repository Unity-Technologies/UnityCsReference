// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Jobs;
using Unity.Properties;
using Unity.Properties.Internal;
using UnityEngine.Scripting;

namespace UnityEditor.UIElements
{
    static class UIElementsEditorInitialization
    {
        [RequiredByNativeCode(optional:false)]
        public static void InitializeUIElementsEditorManaged()
        {
            var jobHandle = PropertiesEditorInitialization.GetInitializationJobHandle();
            var handle = new InitializeUIElementsJob().Schedule(jobHandle);
            JobHandle.ScheduleBatchedJobs();
        }

        struct InitializeUIElementsJob : IJob
        {
            public void Execute()
            {
                UnityEngine.UIElements.UIElementsInitialization.RegisterBuiltInPropertyBags();
            }
        }
    }
}


