// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text;
using UnityEngine.Bindings;

namespace Unity.Scripting.LowLevel;

[VisibleToOtherModules]
internal static partial class Debug
{
    [VisibleToOtherModules]
    internal static unsafe void LogWarning(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        fixed (byte* ptr = bytes)
        {
            LogWarning(ptr);
        }
    }

    [VisibleToOtherModules]
    internal static unsafe void LogError(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        fixed (byte* ptr = bytes)
        {
            LogError(ptr);
        }
    }

    [VisibleToOtherModules]
    internal static unsafe void LogAssertion(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        fixed (byte* ptr = bytes)
        {
            LogAssertion(ptr);
        }
    }
}
