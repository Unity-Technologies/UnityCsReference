// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor;

internal interface IScriptingPlatformProperties : IPlatformProperties
{
    /// <summary>
    /// Points to the CoreCLR BCL Directory
    /// </summary>
    public string CoreCLRBCLDirectory => throw new NotSupportedException("CoreCLR is not supported for this platform.");

    /// <summary>
    /// Points to the IL2CPP directory
    /// </summary>
    public string IL2CPPBCLDirectory => throw new NotSupportedException("IL2CPP is not supported for this platform.");

}
