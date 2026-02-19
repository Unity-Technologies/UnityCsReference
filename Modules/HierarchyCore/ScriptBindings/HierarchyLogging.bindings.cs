// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    [NativeHeader("Modules/HierarchyCore/HierarchyLogging.h")]
    [VisibleToOtherModules("UnityEngine.HierarchyModule", "UnityEditor.HierarchyModule")]
    internal static class HierarchyLogging
    {
        /// <summary>
        /// Set the file path to log to.
        /// </summary>
        /// <param name="path">The file path.</param>
        [Conditional("ENABLE_HIERARCHY_LOGGING")]
        [StaticAccessor("HierarchyLogging", StaticAccessorType.DoubleColon), NativeMethod(IsThreadSafe = true)]
        public static extern void SetLogFile(string path);


        /// <summary>
        /// Write a message to the log file.
        /// </summary>
        /// <param name="message"></param>
        [Conditional("ENABLE_HIERARCHY_LOGGING")]
        [StaticAccessor("HierarchyLogging", StaticAccessorType.DoubleColon), NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public static extern void Log(string message);

        /// <summary>
        /// Synchronizes the file stream with the underlying storage device.
        /// </summary>
        [Conditional("ENABLE_HIERARCHY_LOGGING")]
        [StaticAccessor("HierarchyLogging", StaticAccessorType.DoubleColon), NativeMethod(IsThreadSafe = true)]
        public static extern void Flush();

        /// <summary>
        /// Gets a string representation of the elements.
        /// </summary>
        /// <param name="elements">The elements.</param>
        /// <returns>The combined elements as a string.</returns>
        public static string ToString<T>(T[] elements)
        {
            return ToString(new ReadOnlySpan<T>(elements));
        }

        /// <summary>
        /// Gets a string representation of the elements.
        /// </summary>
        /// <param name="elements">The elements.</param>
        /// <returns>The combined elements as a string.</returns>
        public static string ToString<T>(IEnumerable<T> elements)
        {
            var count = 0;
            foreach (var _ in elements)
                count++;

            var index = 0;
            var array = ArrayPool<T>.Shared.Rent(count);
            var span = array.AsSpan(0, count);
            foreach (var value in elements)
                span[index++] = value;

            var result = ToString<T>(span);
            ArrayPool<T>.Shared.Return(array);
            return result;
        }

        /// <summary>
        /// Gets a string representation of the elements.
        /// </summary>
        /// <param name="elements">The elements.</param>
        /// <returns>The combined elements as a string.</returns>
        public static string ToString<T>(ReadOnlySpan<T> elements)
        {
            return $"[{elements.Length}]{{{Join(", ", elements)}}}";
        }

        /// <summary>
        /// Convert elements to string and join them with a separator.
        /// </summary>
        /// <param name="separator">The separator.</param>
        /// <param name="elements">The elements.</param>
        /// <returns>The combined elements as a string.</returns>
        public static string Join<T>(string separator, T[] elements)
        {
            return Join(separator, new ReadOnlySpan<T>(elements));
        }

        /// <summary>
        /// Convert elements to string and join them with a separator.
        /// </summary>
        /// <param name="separator">The separator.</param>
        /// <param name="elements">The elements.</param>
        /// <returns>The combined elements as a string.</returns>
        public static string Join<T>(string separator, ReadOnlySpan<T> elements)
        {
            var result = new StringBuilder();
            for (var i = 0; i < elements.Length; i++)
            {
                result.Append(elements[i].ToString());
                if (i < elements.Length - 1)
                    result.Append(separator);
            }
            return result.ToString();
        }
    }
}
