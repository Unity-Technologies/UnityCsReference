// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    internal static class TimeAgo
    {
        const int k_Second = 1;
        const int k_Minute = 60 * k_Second;
        const int k_Hour = 60 * k_Minute;
        const int k_Day = 24 * k_Hour;
        const int k_Month = 30 * k_Day;

        public static string GetString(DateTime dateTime)
        {
            var ts = new TimeSpan(DateTime.UtcNow.Ticks - dateTime.ToUniversalTime().Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * k_Minute)
                return "less than a minute ago";

            if (delta < 2 * k_Minute)
                return "a minute ago";

            if (delta < 45 * k_Minute)
                return ts.Minutes + " minutes ago";

            if (delta < 90 * k_Minute)
                return "an hour ago";

            if (delta < 24 * k_Hour)
                return ts.Hours + " hours ago";

            if (delta < 48 * k_Hour)
                return "yesterday";

            if (delta < 30 * k_Day)
                return ts.Days + " days ago";

            if (delta < 12 * k_Month)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "a month ago" : months + " months ago";
            }

            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            return years <= 1 ? "one year ago" : years + " years ago";
        }
    }
}
