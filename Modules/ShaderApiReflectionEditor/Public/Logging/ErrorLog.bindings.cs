// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderApiReflection
{
    public struct SourceLocation
    {
        // Public API

        public string FilePath { get; private set; }
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

    public struct LogMessage
    {
        // Public API

        // NOTE: This enum must remain synchronized with its native counterpart.
        public enum Severity
        {
            Warning,
            Error,
        }

        public Severity MessageSeverity { get; private set; }
        public ErrorCode ErrorCode { get; private set; }
        public SourceLocation Location { get; private set; }
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
