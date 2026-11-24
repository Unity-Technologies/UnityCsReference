// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal static class ViolationExtension
{
    public static bool IsEquivalentTo(this Violation violation, Violation otherViolation)
    {
        return (violation.scopePatternExpression ?? string.Empty) == (otherViolation.scopePatternExpression ?? string.Empty)
               && (violation.message ?? string.Empty) == (otherViolation.message ?? string.Empty)
               && (violation.readMoreLink ?? string.Empty) == (otherViolation.readMoreLink ?? string.Empty);
    }
}
