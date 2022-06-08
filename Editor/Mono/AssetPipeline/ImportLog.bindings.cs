// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor.AssetImporters
{
    // Subset of C++ LogMessageFlags in LogAssert.h
    [Flags]
    public enum ImportLogFlags
    {
        None = 0, // kNoLogMessageFlags
        Error = 1 << 6, // kAssetImportError
        Warning = 1 << 7 // kAssetImportWarning
    };

    [NativeHeader("Editor/Src/AssetPipeline/ImportLog.h")]
    [NativeHeader("Editor/Src/AssetPipeline/ImportLog.bindings.h")]
    [ExcludeFromObjectFactory]
    public sealed class ImportLog : Object
    {
        internal struct Filters
        {
            public const string SearchToken = "i";
            public const string AllIssuesStr = "all";
            public const string ErrorsStr = "errors";
            public const string WarningsStr = "warnings";
        }

        [NativeType(CodegenOptions.Custom, "MonoImportLogEntry")]
        public struct ImportLogEntry
        {
            public string message;
            public ImportLogFlags flags;
            public int line;
            public string file;

            public UnityEngine.Object context
            {
                get => Object.FindObjectFromInstanceID(instanceID);
                set => instanceID = value.GetInstanceID();
            }

            internal int instanceID;
        };

        public extern ImportLogEntry[] logEntries
        {
            [NativeMethod("GetLogEntries")]
            get;
        }

        [NativeMethod("PrintToConsole")]
        internal extern void PrintToConsole();
    }
}
