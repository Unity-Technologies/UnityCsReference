// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System.Reflection;

namespace Unity.Properties.Internal
{
    internal static class ReflectionUtilities
    {
        public static string SanitizeMemberName(MemberInfo info)
        {
            return info.Name
                .Replace(".", "_")
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace("+", "_");
        }
    }
}
