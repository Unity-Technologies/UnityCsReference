// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.ShaderVariantAnalyser
{
    [NativeHeader("ShaderVariantAnalyser/DataRecorder.h")]
    class DataRecorder
    {
        [FreeFunction("ShaderVariantAnalyser::DataRecorder::InitializeAt")]
        public extern static void InitializeAt(string folder);

        [FreeFunction("ShaderVariantAnalyser::DataRecorder::FinishWriting")]
        public extern static void FinishWriting();
    }
}
