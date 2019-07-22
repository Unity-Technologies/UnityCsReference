// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor.Build.Reporting
{
    [NativeType(Header = "Modules/BuildReportingEditor/Public/PackedAssets.h")]
    [NativeClass("BuildReporting::PackedAssets")]
    public sealed class PackedAssets : Object
    {
        public uint file
        {
            get { return GetFile(); }
        }

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

        internal extern uint GetFile();

        internal extern string GetShortPath();

        internal extern ulong GetOverhead();

        internal extern PackedAssetInfo[] GetContents();
    }
}
