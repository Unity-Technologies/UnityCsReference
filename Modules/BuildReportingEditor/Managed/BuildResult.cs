// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Build.Reporting
{
    ///<summary>Describes the outcome of the build process.</summary>
    public enum BuildResult
    {
        ///<summary>Indicates that the outcome of the build is in an unknown state.</summary>
        Unknown      = 0,
        ///<summary>Indicates that the build completed successfully.</summary>
        Succeeded    = 1,
        ///<summary>Indicates that the build failed.</summary>
        Failed       = 2,
        ///<summary>Indicates that the build was cancelled by the user.</summary>
        Cancelled    = 3,
    }
}
