// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // This function exists because UnityEngine.dll is compiled against .NET 3.5, but .NET Core removes all the overloads
    // except this one. So to prevent our code compiling against the (string, object, object) version and use the params
    // version instead, we reroute through this.
    // TODO: remove this when the dependency goes away.
    internal sealed partial class UnityString
    {
        public static string Format(string fmt, params object[] args)
        {
            return String.Format(fmt, args);
        }
    }
}
