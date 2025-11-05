// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

namespace UnityEditor.Mono.Scripting.RestrictedApisValidation;

[RequiredByNativeCode]
enum RestrictedApiSeverity
{
    Hidden = 0,
    Information,
    Warning,
    Error,
}

[RequiredByNativeCode]
enum RestrictedApiKind
{
    Type = 0,
    Method
}
