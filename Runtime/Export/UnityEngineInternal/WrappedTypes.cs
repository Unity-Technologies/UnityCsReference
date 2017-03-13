// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
namespace UnityEngineInternal
{
    // This is a solution to problem where we cannot use:
    // * System.Collections.Stack on WP8 and Metro, because it was stripped from .NET
    // * System.Collections.Generic.Stack cannot use on iOS because it creates a dependency to System.dll thus increasing overall executable size
    public class GenericStack : System.Collections.Stack
    {
    }
}
