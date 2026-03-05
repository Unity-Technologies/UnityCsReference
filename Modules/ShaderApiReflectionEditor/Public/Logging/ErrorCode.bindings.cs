// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderApiReflection
{
    // NOTE: This enum must remain synchronized with its native counterpart.
    public enum ErrorCode
    {
        Unknown,
        FailedToLex = 0x100,
        UnexpectedEndOfFile = 0x200,
        UnexpectedToken,
        UnmatchedScopeDelimiter,
        UnsupportedShaderSyntax,
        XMLSyntaxError = 0x300,
        NestedHintContainer,
        DeepHint,
        AttributeHint,
        NonexistentParameter,
        MissingParameterName,
        MultiplyDefinedHint = 0x400,
    }
}
