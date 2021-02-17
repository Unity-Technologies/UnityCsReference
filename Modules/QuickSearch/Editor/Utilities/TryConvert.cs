// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Search
{
    internal static class TryConvert
    {
        public static bool ToBool(string value, bool defaultValue = false)
        {
            try
            {
                return Convert.ToBoolean(value);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static float ToFloat(string value, float defaultValue = 0f)
        {
            try
            {
                return Convert.ToSingle(value);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static int ToInt(string value, int defaultValue = 0)
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
    }
}
