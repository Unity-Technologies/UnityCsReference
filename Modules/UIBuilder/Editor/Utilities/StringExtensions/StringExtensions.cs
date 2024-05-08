// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEditor.UIElements;

namespace Unity.UI.Builder
{
    internal static class StringExtensions
    {
        public static string RemoveExtraWhitespace(this string str)
        {
            using var builderHandle = StringBuilderPool.Get(out var builder);

            var strSpan = str.AsSpan().Trim();

            var space = false;

            for (var i = 0; i < strSpan.Length; ++i)
            {
                var c = strSpan[i];
                if (c == ' ')
                {
                    if (space)
                        continue;

                    space = true;
                    builder.Append(' ');
                }
                else
                {
                    space = false;
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }
    }
}
