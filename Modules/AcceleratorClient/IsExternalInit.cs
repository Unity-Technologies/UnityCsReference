// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace System.Runtime.CompilerServices
{
    // Polyfill: netstandard2.1 lacks IsExternalInit (net5+), needed for `init` setters/records.
    internal static class IsExternalInit { }
}
