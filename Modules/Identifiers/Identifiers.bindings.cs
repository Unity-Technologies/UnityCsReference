// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Identifiers
{
    [NativeHeader("Modules/Identifiers/Identifiers.h")]
    public static class Identifiers
    {
        public static string installationId => GetInstallationId();

        [FreeFunction("UnityEngine_Identifiers_GetInstallationId")]
        extern static string GetInstallationId();
    }
}

