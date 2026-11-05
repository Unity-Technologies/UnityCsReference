// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderApiReflection
{
    ///<summary>Identifies a specific part of a source code file.</summary>
    ///<remarks>When used with a <see cref="LogMessage" />, represents the location in the source code the message refers to.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Modules/ShaderApiReflectionEditor/Tests/UTFTests/FunctionalTests/Editor/Documentation/LogMessageExample.cs}]]></code>
    ///</example>
    ///<seealso cref="LogMessage.Location" />
    public struct SourceLocation
    {
        // Public API

        ///<summary>The project-relative path to a source code file.</summary>
        public string FilePath { get; private set; }
        ///<summary>The line number within a source code file.</summary>
        public UInt32 Line { get; private set; }

        // Private API

        [NativeHeader("Modules/ShaderApiReflectionEditor/Public/Logging/ErrorLog.h")]
        [NativeClass("ShaderApiReflection::Logging::SourceLocation")]
        internal struct MarshalledType
        {
            public string m_File;
            public UInt32 m_Position;
        }

        internal SourceLocation(MarshalledType nativeData)
        {
            FilePath = nativeData.m_File;
            Line = nativeData.m_Position + 1; // Internally, errors are zero-indexed
        }
    }

    ///<summary>Information about a problem that arose while reflecting shader code.</summary>
    ///<remarks>Use this struct to retrieve a data representation of any console messages logged while importing a shader include.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Modules/ShaderApiReflectionEditor/Tests/UTFTests/FunctionalTests/Editor/Documentation/LogMessageExample.cs}]]></code>
    ///</example>
    ///<seealso cref="ShaderIncludeReflection.LogMessages" />
    public struct LogMessage
    {
        // Public API

        // NOTE: This enum must remain synchronized with its native counterpart.
        ///<summary>The degree of failure associated with a log message.</summary>
        ///<remarks>Indicates whether a log message is an error or warning.</remarks>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Modules/ShaderApiReflectionEditor/Tests/UTFTests/FunctionalTests/Editor/Documentation/LogMessageExample.cs}]]></code>
        ///</example>
        ///<seealso cref="LogMessage" />
        public enum Severity
        {
            ///<summary>Indicates the message is a warning.</summary>
            Warning,
            ///<summary>Indicates the message is an error.</summary>
            Error,
        }

        ///<summary>The message's severity.</summary>
        public Severity MessageSeverity { get; private set; }
        ///<summary>The message's error code.</summary>
        public ErrorCode ErrorCode { get; private set; }
        ///<summary>The message's location.</summary>
        public SourceLocation Location { get; private set; }
        ///<summary>The message's details.</summary>
        public string Text { get; private set; }

        // Private API

        [NativeHeader("Modules/ShaderApiReflectionEditor/Public/Logging/ErrorLog.h")]
        [NativeClass("ShaderApiReflection::Logging::Message")]
        internal struct MarshalledType
        {
            public Severity m_Severity;
            public ErrorCode m_Code;
            public SourceLocation.MarshalledType m_Location;
            public string m_Text;
        }

        internal LogMessage(MarshalledType nativeData)
        {
            MessageSeverity = nativeData.m_Severity;
            ErrorCode = nativeData.m_Code;
            Location = new SourceLocation(nativeData.m_Location);
            Text = nativeData.m_Text;
        }
    }
}
