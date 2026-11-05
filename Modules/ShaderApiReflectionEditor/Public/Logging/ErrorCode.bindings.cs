// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderApiReflection
{
    // NOTE: This enum must remain synchronized with its native counterpart.
    ///<summary>Uniquely identifies a type of error which may occur when generating reflection information.</summary>
    ///<remarks>Each value of this enum represents a different type of error, the details of which may be found in <see cref="LogMessage.Text" />.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Modules/ShaderApiReflectionEditor/Tests/UTFTests/FunctionalTests/Editor/Documentation/LogMessageExample.cs}]]></code>
    ///</example>
    ///<seealso cref="LogMessage.ErrorCode" />
    public enum ErrorCode
    {
        ///<summary>An unspecified error occurred.</summary>
        Unknown,
        ///<summary>The shader source code failed to tokenize.</summary>
        FailedToLex = 0x100,
        ///<summary>The shader parser reached the end-of-file marker while searching for a token.</summary>
        UnexpectedEndOfFile = 0x200,
        ///<summary>The shader parser expected one token but found another.</summary>
        UnexpectedToken,
        ///<summary>The shader source code lacked a closing brace.</summary>
        UnmatchedScopeDelimiter,
        ///<summary>The shader source code made use of a language construct not supported by the shader API reflection system.</summary>
        UnsupportedShaderSyntax,
        ///<summary>A comment decorating a shader function contained malformed XML.</summary>
        XMLSyntaxError = 0x300,
        ///<summary>A hint tag was nested within another hint tag.</summary>
        NestedHintContainer,
        ///<summary>A hint tag was nested within another hint tag.</summary>
        DeepHint,
        ///<summary>A hint tag used an XML attribute.</summary>
        AttributeHint,
        ///<summary>An XML tag referenced a nonexisted function parameter.</summary>
        NonexistentParameter,
        ///<summary>An XML tag which requires a function parameter name lacked one.</summary>
        MissingParameterName,
        ///<summary>A hint was defined multiple times.</summary>
        MultiplyDefinedHint = 0x400,
    }
}
