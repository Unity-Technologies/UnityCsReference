// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEditor
{
    // Stores and accesses Unity editor preferences.
    [NativeHeader("Runtime/Utilities/PlayerPrefs.h")]
    public sealed class EditorPrefs
    {
        // Sends events whenever EditorPrefs values are updated
        // NOTE: This is a quick solution for accessing editor prefs from background threads,
        //  long term solution is to make editor prefs read/writes threadsafe
        internal delegate void ValueWasUpdated(string key);
        internal static event ValueWasUpdated onValueWasUpdated;

        // Sets the value of the preference identified by /key/.
        [NativeMethod("SetInt")]
        static extern void SetInt_Internal(string key, int value);
        public static void SetInt(string key, int value)
        {
            SetInt_Internal(key, value);
            onValueWasUpdated?.Invoke(key);
        }

        // Returns the value corresponding to /key/ in the preference file if it exists.
        public static extern int GetInt(string key, [DefaultValue("0")] int defaultValue);
        public static int GetInt(string key)
        {
            return GetInt(key, 0);
        }

        // Sets the value of the preference identified by /key/.
        [NativeMethod("SetFloat")]
        static extern void SetFloat_Internal(string key, float value);
        public static void SetFloat(string key, float value)
        {
            SetFloat_Internal(key, value);
            onValueWasUpdated?.Invoke(key);
        }

        // Returns the value corresponding to /key/ in the preference file if it exists.
        public static extern float GetFloat(string key, [DefaultValue("0.0F")] float defaultValue);
        public static float GetFloat(string key)
        {
            return GetFloat(key, 0.0F);
        }

        // Sets the value of the preference identified by /key/.
        [NativeMethod("SetString")]
        static extern void SetString_Internal(string key, string value);
        public static void SetString(string key, string value)
        {
            SetString_Internal(key, value);
            onValueWasUpdated?.Invoke(key);
        }

        // Returns the value corresponding to /key/ in the preference file if it exists.
        public static extern string GetString(string key, [DefaultValue("\"\"")] string defaultValue);
        public static string GetString(string key)
        {
            return GetString(key, "");
        }

        // Sets the value of the preference identified by /key/.
        [NativeMethod("SetBool")]
        static extern void SetBool_Internal(string key, bool value);
        public static void SetBool(string key, bool value)
        {
            SetBool_Internal(key, value);
            onValueWasUpdated?.Invoke(key);
        }

        // Returns the value corresponding to /key/ in the preference file if it exists.
        public static extern bool GetBool(string key, [DefaultValue("false")] bool defaultValue);
        public static bool GetBool(string key)
        {
            return GetBool(key, false);
        }

        // Returns true if /key/ exists in the preferences.
        public static extern bool HasKey(string key);

        // Removes /key/ and its corresponding value from the preferences.
        public static extern void DeleteKey(string key);

        // Removes all keys and values from the preferences. Use with caution.
        public static extern void DeleteAll();
    }
}
