// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor.Build.Reporting
{
    [NativeType(Header = "Modules/BuildReportingEditor/Public/PackedAssets.h")]
    [NativeClass("BuildReporting::PackedAssets")]
    public sealed class PackedAssets : Object
    {
        private const string fileObsoleteMessage = "Report file index is no longer available. To find the matching report file for a particular asset the recommended way is to do a filename lookup in the report.";
        [Obsolete(fileObsoleteMessage, true)]
        public uint file => throw new NotSupportedException(fileObsoleteMessage);

        public string shortPath
        {
            get { return GetShortPath(); }
        }

        public ulong overhead
        {
            get { return GetOverhead(); }
        }

        public PackedAssetInfo[] contents
        {
            get { return GetContents(); }
        }

        internal extern string GetShortPath();

        internal extern ulong GetOverhead();

        internal extern PackedAssetInfo[] GetContents();
    }
}
