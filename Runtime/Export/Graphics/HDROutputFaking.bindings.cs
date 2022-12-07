// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using UnityEngine.Internal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

[assembly: InternalsVisibleTo("TestRuntime")]
[assembly: InternalsVisibleTo("TestRuntime.FakingHDR")]
namespace UnityEngine.Internal
{
    [NativeHeader("Runtime/GfxDevice/HDROutputSettings.h")]
    [ExcludeFromDocs]
    internal static class InternalHDROutputFaking
    {
        [FreeFunction("HDROutputSettingsBindings::SetFakeHDROutputEnabled")]
        [ExcludeFromDocs]
        extern internal static void SetEnabled(bool enabled);
    }
}
