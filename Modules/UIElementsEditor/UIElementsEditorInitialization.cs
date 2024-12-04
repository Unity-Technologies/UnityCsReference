// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using Unity.Jobs;
using Unity.Properties.Internal;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.UIElements
{
    static class UIElementsEditorInitialization
    {
        [UsedImplicitly]
        [RequiredByNativeCode(optional:false)]
        public static void InitializeUIElementsEditorManaged()
        {
            var jobHandle = PropertiesEditorInitialization.GetInitializationJobHandle();
            new InitializeUIElementsJob().Schedule(jobHandle);
            try
            {
                UxmlSerializedDataRegistry.RegisterUxmlSerializedDataTypes();
                UxmlSerializedDataRegistry.RegisterCustomDependencies();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
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


