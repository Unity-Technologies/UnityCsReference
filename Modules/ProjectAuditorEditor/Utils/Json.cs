// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class Json
    {
        static readonly string kDateFormatString = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Serializes a DateTime object to a string using a UTC format.
        /// </summary>
        /// <param name="dateTime">The DateTime object to serialize.</param>
        /// <returns>A string representing the DateTime object in UTC.</returns>
        public static string SerializeDateTime(DateTime dateTime)
        {
            return dateTime.ToUniversalTime().ToString(kDateFormatString, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Deserializes a string representing a DateTime in UTC to a DateTime object.
        /// </summary>
        /// <param name="utcDateTime">The string representing the DateTime object in UTC.</param>
        /// <returns>A DateTime object converted to local time.</returns>
        public static DateTime DeserializeDateTime(string utcDateTime)
        {
            if (DateTime.TryParseExact(utcDateTime, kDateFormatString, null, DateTimeStyles.None, out var parsedDate))
                return parsedDate.ToLocalTime();
            return new DateTime();
        }

#pragma warning disable CS0649
        private class Wrapper<T>
        {
            public T[] items;
        }
#pragma warning restore CS0649

        public static T[] DeserializeArray<T>(string json)
        {
            return JsonUtility.FromJson<Wrapper<T>>("{\"items\":" + json + "}").items;
        }

        public static T[] DeserializeArrayFromFile<T>(string fileName)
        {
            var fullPath = Path.GetFullPath(fileName);
            var json = File.ReadAllText(fullPath);
            var items = DeserializeArray<T>(json);

            return items;
        }
    }
}
