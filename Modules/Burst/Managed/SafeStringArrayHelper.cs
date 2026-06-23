// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;

namespace Unity.Burst
{
    internal static class SafeStringArrayHelper
    {
        // Methods to help when needing to serialise arrays of strings safely
        public static string SerialiseStringArraySafe(string[] array)
        {
            var s = new StringBuilder();
            foreach (var entry in array)
            {
                s.Append($"{Encoding.UTF8.GetByteCount(entry)}]");
                s.Append(entry);
            }
            return s.ToString();
        }

        public static string[] DeserialiseStringArraySafe(string input)
        {
            // Safer method of serialisation (len]path) e.g. "5]frank8]c:\\billy"  ( new [] {"frank","c:\\billy"} )

            // Since the len part of `len]path` is specified in bytes we'll be working on a byte array instead
            // of a string, because functions like Substring expects char offsets and number of chars.
            var bytes = Encoding.UTF8.GetBytes(input);
            var listFolders = new List<string>();
            var index = 0;
            var length = bytes.Length;
            while (index < length)
            {
                int len = 0;
                // Read the decimal encoded length, terminated by an ']'
                while (true)
                {
                    if (index >= length)
                    {
                        throw new FormatException($"Invalid input `{input}`: reached end while reading length");
                    }

                    var d = bytes[index];

                    if (d == ']')
                    {
                        index++;
                        break;
                    }

                    if (d < '0' || d > '9')
                    {
                        throw new FormatException(
                            $"Invalid input `{input}` at {index}: Got non-digit character while reading length");
                    }

                    len = len * 10 + (d - '0');

                    index++;
                }

                listFolders.Add(Encoding.UTF8.GetString(bytes, index, len));
                index += len;
            }

            return listFolders.ToArray();
        }
    }
}
