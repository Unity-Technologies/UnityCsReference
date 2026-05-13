// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

namespace UnityEditor.Mono.Scripting.RestrictedApisValidation;

[RequiredByNativeCode]
record struct RestrictedApiUsage(string ContainingMemberName, string OffendingMemberSignature, string Description, string DocumentationUrl, RestrictedApiSeverity Severity, RestrictedApiKind ApiKind, string DebugInformation)
{
    // Out parameter order must match the GetScriptingArrayElementNoRef specialization in RestrictedApisValidator.cpp exactly; wrong order silently corrupts values.
    [RequiredByNativeCode]
    internal static void DeconstructRestrictedApiUsageArrayElement(RestrictedApiUsage[] array, int index,
        out object containingMemberName, out object offendingMemberSignature, out object description,
        out object documentationUrl, out int severity, out int apiKind, out object debugInformation)
    {
        ref RestrictedApiUsage usage = ref array[index];
        containingMemberName = usage.ContainingMemberName;
        offendingMemberSignature = usage.OffendingMemberSignature;
        description = usage.Description;
        documentationUrl = usage.DocumentationUrl;
        severity = (int)usage.Severity;
        apiKind = (int)usage.ApiKind;
        debugInformation = usage.DebugInformation;
    }
}

[RequiredByNativeCode]
record struct RestrictedApiValidationResult(string AssemblyPath, long TimeInMilliseconds, RestrictedApiUsage[] Usages)
{
    // Out parameter order must match the GetScriptingArrayElementNoRef specialization in RestrictedApisValidator.cpp exactly; wrong order silently corrupts values.
    [RequiredByNativeCode]
    internal static void DeconstructRestrictedApiValidationResultArrayElement(RestrictedApiValidationResult[] array, int index,
        out object assemblyPath, out long timeInMilliseconds, out object usages)
    {
        ref RestrictedApiValidationResult result = ref array[index];
        assemblyPath = result.AssemblyPath;
        timeInMilliseconds = result.TimeInMilliseconds;
        usages = result.Usages;
    }
}

