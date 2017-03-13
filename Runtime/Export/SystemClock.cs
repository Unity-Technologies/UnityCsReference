// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    internal class SystemClock
    {
        static readonly DateTime s_Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime now
        {
            get { return DateTime.Now; }
        }

        public static long ToUnixTimeMilliseconds(DateTime date)
        {
            return Convert.ToInt64((date.ToUniversalTime() - s_Epoch).TotalMilliseconds);
        }

        public static long ToUnixTimeSeconds(DateTime date)
        {
            return Convert.ToInt64((date.ToUniversalTime() - s_Epoch).TotalSeconds);
        }
    }
}
