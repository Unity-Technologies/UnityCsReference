// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Jobs;
using UnityEngine.Scripting;

namespace Unity.Properties.Internal
{
    static class PropertiesEditorInitialization
    {
        private static bool s_Initialized;
        static JobHandle s_InitializeJobHandle;

        public static JobHandle GetInitializationJobHandle()
        {
            InitializePropertiesEditor();
            return s_InitializeJobHandle;
        }

        [RequiredByNativeCode(optional:false)]
        public static void InitializePropertiesEditor()
        {
            if (s_Initialized)
                return;
            s_InitializeJobHandle = new InitializePropertiesJob().Schedule();
            PropertyBag.AddJobToWaitQueue(s_InitializeJobHandle);
            s_Initialized = true;
        }

        struct InitializePropertiesJob : IJob
        {
            public void Execute()
            {
                Internal.PropertiesInitialization.InitializeProperties();
            }
        }
    }
}
