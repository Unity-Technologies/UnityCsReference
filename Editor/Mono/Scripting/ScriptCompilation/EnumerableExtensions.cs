// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

static class EnumerableExtensions
{
    public static string SeparateWith(this IEnumerable<string> values, string separator)
    {
        return string.Join(separator, values);
    }
}
