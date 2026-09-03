// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.AcceleratorClient
{
    internal static class AcceleratorClientLoadToken
    {
        internal static readonly long LoadToken = DateTime.UtcNow.Ticks;

        internal static long GetLoadToken() => LoadToken;
    }
}
