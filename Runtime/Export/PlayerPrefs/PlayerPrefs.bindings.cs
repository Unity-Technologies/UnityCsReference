// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // This exception is thrown by the [[PlayerPrefs]] class in the Web player if the preference file would exceed the allotted storage space when setting a value.
    public class PlayerPrefsException : Exception
    {
        //*undocumented*
        public PlayerPrefsException(string error) : base(error) {}
    }

    // Stores and accesses player preferences between game sessions.
    [NativeHeader("Runtime/Utilities/PlayerPrefs.h")]
    public class PlayerPrefs
    {
        [NativeMethod("SetInt")]
        extern private static bool TrySetInt(string key, int value);

        [NativeMethod("SetFloat")]
        extern private static bool TrySetFloat(string key, float value);

        [NativeMethod("SetString")]
        extern private static bool TrySetSetString(string key, string value);

        // Sets the value of the preference identified by /key/.
        public static void SetInt(string key, int value) { if (!TrySetInt(key, value)) throw new PlayerPrefsException("Could not store preference value"); }

        // Returns the value corresponding to /key/ in the preference file if it exists.
        public extern static int GetInt(string key, int defaultValue);

        public static int GetInt(string key)
        {
            return GetInt(key, 0);
        }

        // Sets the value of the preference identified by /key/.
        public static void SetFloat(string key, float value) { if (!TrySetFloat(key, value)) throw new PlayerPrefsException("Could not store preference value"); }

        // Returns the value corresponding to /key/ in the preference file if it exists.
        public extern static float GetFloat(string key, float defaultValue);

        public static float GetFloat(string key)
        {
            return GetFloat(key, 0.0f);
        }

        // Sets the value of the preference identified by /key/.
        public static void SetString(string key, string value) { if (!TrySetSetString(key, value)) throw new PlayerPrefsException("Could not store preference value"); }


        // Returns the value corresponding to /key/ in the preference file if it exists.
        public extern static string GetString(string key, string defaultValue);

        public static string GetString(string key)
        {
            return GetString(key, "");
        }

        // Returns true if /key/ exists in the preferences.
        public extern static bool HasKey(string key);

        // Removes /key/ and its corresponding value from the preferences.
        public extern static void DeleteKey(string key);

        // Removes all keys and values from the preferences. Use with caution.
        [NativeMethod("DeleteAllWithCallback")]
        public extern static void DeleteAll();

        // Writes all modified preferences to disk.
        [NativeMethod("Sync")]
        public extern static void Save();

        // NOTE: DisposeSentinel requires access to EditorPrefs but from UnityEngine.dll
        //       (Which cant access UnityEditor.dll)
        //       So we expose the API here. Internal only, users should use the normal EditorPrefs class

        [StaticAccessor("EditorPrefs", StaticAccessorType.DoubleColon)]
        [NativeMethod("SetInt")]
        extern internal static void EditorPrefsSetInt(string key, int value);

        [StaticAccessor("EditorPrefs", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetInt")]
        extern internal static int EditorPrefsGetInt(string key, int defaultValue);
    }
}
