// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

namespace UnityEditor.Mono.Scripting.RestrictedApisValidation;

[RequiredByNativeCode]
record struct RestrictedApiUsage(string ContainingMemberName, string OffendingMemberSignature, string Description, string DocumentationUrl, RestrictedApiSeverity Severity, RestrictedApiKind ApiKind, string DebugInformation);

[RequiredByNativeCode]
record struct RestrictedApiValidationResult(string AssemblyPath, long TimeInMilliseconds, RestrictedApiUsage[] Usages);

