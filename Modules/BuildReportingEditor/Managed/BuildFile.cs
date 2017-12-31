// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    [NativeType(Header = "Modules/BuildReportingEditor/Public/BuildReport.h")]
    public struct BuildFile
    {
        internal uint id { get; }
        public string path { get; }
        public string role { get; }

        [NativeName("totalSize")]
        public ulong size { get; }

        public override string ToString()
        {
            return path;
        }
    }
}
