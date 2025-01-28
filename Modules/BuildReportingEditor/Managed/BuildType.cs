// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Build.Reporting
{
    ///<summary>Build type.</summary>
    ///<seealso cref="BuildSummary.buildType" />
    [Flags]
    public enum BuildType
    {
        ///<summary>Indicates a Player build.</summary>
        Player = 1,
        ///<summary>Indicates an Asset Bundle build.</summary>
        AssetBundle = 2
    }
}
