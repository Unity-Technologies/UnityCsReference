// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // <OUTDATED>
    // This function exists because UnityEngine.dll is compiled against .NET 3.5, but .NET Core removes all the overloads
    // except this one. So to prevent our code compiling against the (string, object, object) version and use the params
    // version instead, we reroute through this.
    // </OUTDATED>

    // This was added to support .net Core in UWP Metro Apps. We no longer require this wrapper so it can be removed

    // TODO: remove this once all package references have been removed as it prevents some overloads being used which increases the amount of cases where a params array is allocated when this function is called
    [VisibleToOtherModules]
    internal sealed partial class UnityString
    {
        [Obsolete("UnityString.Format is redundant and will be removed in a future version. Please move to using modern C# string interpolation or string.Format. (UnityUpgradable) -> [netstandard] System.String.Format(*)")]
        public static string Format(string fmt, params object[] args)
        {
            return String.Format(CultureInfo.InvariantCulture.NumberFormat, fmt, args);
        }
    }
}
