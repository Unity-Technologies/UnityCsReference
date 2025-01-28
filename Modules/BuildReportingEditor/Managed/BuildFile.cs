// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    ///<summary>Contains information about a single file produced by the build process.</summary>
    [NativeType(Header = "Modules/BuildReportingEditor/Public/BuildReport.h")]
    public struct BuildFile
    {
        ///<summary>The unique indentifier of the build file.</summary>
        public uint id { get; }
        ///<summary>The absolute path of the file produced by the build process.</summary>
        public string path { get; }
        ///<summary>The role the file plays in the build output.</summary>
        ///<remarks>Use this field to understand what purpose a file serves within the built player. Common roles for files are captured by the members of the <see cref="CommonRoles" /> class.</remarks>
        public string role { get; }

        ///<summary>The total size of the file, in bytes.</summary>
        [NativeName("totalSize")]
        public ulong size { get; }

        public override string ToString()
        {
            return path;
        }
    }
}
