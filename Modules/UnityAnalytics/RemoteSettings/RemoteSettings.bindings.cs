// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using uei = UnityEngine.Internal;


namespace UnityEngine
{
    [NativeHeader("Modules/UnityAnalytics/RemoteSettings/RemoteSettings.h")]
    [NativeHeader("UnityAnalyticsScriptingClasses.h")]
    public static class RemoteSettings
    {
        public delegate void UpdatedEventHandler();
        public static event UpdatedEventHandler Updated;
        public static event Action BeforeFetchFromServer;
        public static event Action<bool, bool, int> Completed;

        [RequiredByNativeCode]
        internal static void RemoteSettingsUpdated(bool wasLastUpdatedFromServer)
        {
            var handler = Updated;
            if (handler != null)
                handler();
        }

        [RequiredByNativeCode]
        internal static void RemoteSettingsBeforeFetchFromServer()
        {
            var handler = BeforeFetchFromServer;
            if (handler != null)
                handler();
        }

        [RequiredByNativeCode]
        internal static void RemoteSettingsUpdateCompleted(bool wasLastUpdatedFromServer, bool settingsChanged, int response)
        {
            var handler = Completed;
            if (handler != null)
                handler(wasLastUpdatedFromServer, settingsChanged, response);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Calling CallOnUpdate() is not necessary any more and should be removed. Use RemoteSettingsUpdated instead", true)]
        public static void CallOnUpdate()
        {
            throw new NotSupportedException("Calling CallOnUpdate() is not necessary any more and should be removed.");
        }

        // Forces an update of the remote config from the server.
        public extern static void ForceUpdate();

        // updated from remote config server.
        public extern static bool WasLastUpdatedFromServer();

        // Returns the value corresponding to /key/ in the preference file if it exists.
        [uei.ExcludeFromDocs]
        public static int GetInt(string key) { return GetInt(key, 0); }
        public extern static int GetInt(string key, [UnityEngine.Internal.DefaultValue("0")] int defaultValue);

        // Returns the value corresponding to /key/ in the preference file if it exists.
        [uei.ExcludeFromDocs]
        public static long GetLong(string key) { return GetLong(key, 0); }
        public extern static long GetLong(string key, [UnityEngine.Internal.DefaultValue("0")] long defaultValue);

        // Returns the value corresponding to /key/ in the preference file if it exists.
        [uei.ExcludeFromDocs]
        public static float GetFloat(string key) { return GetFloat(key, 0.0F); }
        public extern static float GetFloat(string key, [UnityEngine.Internal.DefaultValue("0.0F")] float defaultValue);

        // Returns the value corresponding to /key/ in the preference file if it exists.
        [uei.ExcludeFromDocs]
        public static string GetString(string key) { return GetString(key, ""); }
        public extern static string GetString(string key, [UnityEngine.Internal.DefaultValue("\"\"")] string defaultValue);

        // Returns the value corresponding to /key/ in the preference file if it exists.
        [uei.ExcludeFromDocs]
        public static bool GetBool(string key) { return GetBool(key, false); }
        public extern static bool GetBool(string key, [UnityEngine.Internal.DefaultValue("false")] bool defaultValue);

        // Returns true if /key/ exists in the preferences.
        public extern static bool HasKey(string key);

        public extern static int GetCount();

        public extern static string[] GetKeys();

        public static T GetObject<T>(string key = "") { return (T)GetObject(typeof(T), key); }

        public static object GetObject(Type type, string key = "")
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (type.IsAbstract || type.IsSubclassOf(typeof(UnityEngine.Object)))
                throw new ArgumentException("Cannot deserialize to new instances of type '" + type.Name + ".'");

            return GetAsScriptingObject(type, null, key);
        }

        public static object GetObject(string key, object defaultValue)
        {
            if (defaultValue == null)
                throw new ArgumentNullException("defaultValue");

            Type type = defaultValue.GetType();
            if (type.IsAbstract || type.IsSubclassOf(typeof(UnityEngine.Object)))
                throw new ArgumentException("Cannot deserialize to new instances of type '" + type.Name + ".'");

            return GetAsScriptingObject(type, defaultValue, key);
        }

        internal static extern object GetAsScriptingObject(Type t, object defaultValue, string key);

        public static IDictionary<string, object> GetDictionary(string key = "")
        {
            UseSafeLock();
            IDictionary<string, object> dict = RemoteConfigSettingsHelper.GetDictionary(GetSafeTopMap(), key);
            ReleaseSafeLock();
            return dict;
        }

        internal extern static void UseSafeLock();
        internal extern static void ReleaseSafeLock();
        internal extern static IntPtr GetSafeTopMap();
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityAnalytics/RemoteSettings/RemoteSettings.h")]
    [NativeHeader("UnityAnalyticsScriptingClasses.h")]
    [uei.ExcludeFromDocs]
    public class RemoteConfigSettings : IDisposable
    {
        [System.NonSerialized]
        internal IntPtr m_Ptr;

        public event Action<bool> Updated;

        private RemoteConfigSettings() {}

        public RemoteConfigSettings(string configKey)
        {
            m_Ptr = Internal_Create(this, configKey);
            Updated = null;
        }

        ~RemoteConfigSettings()
        {
            Destroy();
        }

        void Destroy()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        internal static extern IntPtr Internal_Create(RemoteConfigSettings rcs, string configKey);
        [ThreadSafe]
        internal static extern void Internal_Destroy(IntPtr ptr);

        [RequiredByNativeCode]
        internal static void RemoteConfigSettingsUpdated(RemoteConfigSettings rcs, bool wasLastUpdatedFromServer)
        {
            var handler = rcs.Updated;
            if (handler != null)
                handler(wasLastUpdatedFromServer);
        }

        public extern static bool QueueConfig(string name, object param, int ver = 1, string prefix = "");

        public extern static bool SendDeviceInfoInConfigRequest();

        public extern static void AddSessionTag(string tag);

        // Forces an update of the remote config from the server.
        public extern void ForceUpdate();

        // updated from remote config server.
        public extern bool WasLastUpdatedFromServer();

        // Returns the value corresponding to /key/ in the preference file if it exists.
        [uei.ExcludeFromDocs]
        public int GetInt(string key) { return GetInt(key, 0); }
        public extern int GetInt(string key, [UnityEngine.Internal.DefaultValue("0")] int defaultValue);

        // Returns the value corresponding to /key/ in the preference file if it exists.
        [uei.ExcludeFromDocs]
        public long GetLong(string key) { return GetLong(key, 0); }
        public extern long GetLong(string key, [UnityEngine.Internal.DefaultValue("0")] long defaultValue);

        // Returns the value corresponding to /key/ in the preference file if it exists.
        [uei.ExcludeFromDocs]
        public float GetFloat(string key) { return GetFloat(key, 0.0F); }
        public extern float GetFloat(string key, [UnityEngine.Internal.DefaultValue("0.0F")] float defaultValue);

        // Returns the value corresponding to /key/ in the preference file if it exists.
        [uei.ExcludeFromDocs]
        public string GetString(string key) { return GetString(key, ""); }
        public extern string GetString(string key, [UnityEngine.Internal.DefaultValue("\"\"")] string defaultValue);

        // Returns the value corresponding to /key/ in the preference file if it exists.
        [uei.ExcludeFromDocs]
        public bool GetBool(string key) { return GetBool(key, false); }
        public extern bool GetBool(string key, [UnityEngine.Internal.DefaultValue("false")] bool defaultValue);

        // Returns true if /key/ exists in the preferences.
        public extern bool HasKey(string key);

        public extern int GetCount();

        public extern string[] GetKeys();

        public T GetObject<T>(string key = "") { return (T)GetObject(typeof(T), key); }

        public object GetObject(Type type, string key = "")
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (type.IsAbstract || type.IsSubclassOf(typeof(UnityEngine.Object)))
                throw new ArgumentException("Cannot deserialize to new instances of type '" + type.Name + ".'");

            return GetAsScriptingObject(type, null, key);
        }

        public object GetObject(string key, object defaultValue)
        {
            if (defaultValue == null)
                throw new ArgumentNullException("defaultValue");

            Type type = defaultValue.GetType();
            if (type.IsAbstract || type.IsSubclassOf(typeof(UnityEngine.Object)))
                throw new ArgumentException("Cannot deserialize to new instances of type '" + type.Name + ".'");

            return GetAsScriptingObject(type, defaultValue, key);
        }

        internal extern object GetAsScriptingObject(Type t, object defaultValue, string key);

        public IDictionary<string, object> GetDictionary(string key = "")
        {
            UseSafeLock();
            IDictionary<string, object> dict = RemoteConfigSettingsHelper.GetDictionary(GetSafeTopMap(), key);
            ReleaseSafeLock();
            return dict;
        }

        internal extern void UseSafeLock();
        internal extern void ReleaseSafeLock();
        internal extern IntPtr GetSafeTopMap();
    }

    internal static class RemoteConfigSettingsHelper
    {
        [RequiredByNativeCode]
        internal enum Tag
        {
            kUnknown,
            kIntVal,
            kInt64Val,
            kUInt64Val,
            kDoubleVal,
            kBoolVal,
            kStringVal,
            kArrayVal,
            kMixedArrayVal,
            kMapVal,
            kMaxTags
        }

        internal extern static IntPtr GetSafeMap(IntPtr m, string key);
        internal extern static string[] GetSafeMapKeys(IntPtr m);
        internal extern static Tag[] GetSafeMapTypes(IntPtr m);

        internal extern static long GetSafeNumber(IntPtr m, string key, long defaultValue);
        internal extern static float GetSafeFloat(IntPtr m, string key, float defaultValue);
        internal extern static bool GetSafeBool(IntPtr m, string key, bool defaultValue);
        internal extern static string GetSafeStringValue(IntPtr m, string key, string defaultValue);

        internal extern static IntPtr GetSafeArray(IntPtr m, string key);
        internal extern static long GetSafeArraySize(IntPtr a);

        internal extern static IntPtr GetSafeArrayArray(IntPtr a, long i);
        internal extern static IntPtr GetSafeArrayMap(IntPtr a, long i);
        internal extern static Tag GetSafeArrayType(IntPtr a, long i);
        internal extern static long GetSafeNumberArray(IntPtr a, long i);
        internal extern static float GetSafeArrayFloat(IntPtr a, long i);
        internal extern static bool GetSafeArrayBool(IntPtr a, long i);
        internal extern static string GetSafeArrayStringValue(IntPtr a, long i);

        public static IDictionary<string, object> GetDictionary(IntPtr m, string key)
        {
            if (m == IntPtr.Zero)
                return null;
            if (!String.IsNullOrEmpty(key))
            {
                m = GetSafeMap(m, key);
                if (m == IntPtr.Zero)
                    return null;
            }
            return RemoteConfigSettingsHelper.GetDictionary(m);
        }

        internal static IDictionary<string, object> GetDictionary(IntPtr m)
        {
            if (m == IntPtr.Zero)
                return null;
            IDictionary<string, object> dict = new Dictionary<string, object>();
            Tag[] tags = GetSafeMapTypes(m);
            string[] keys = GetSafeMapKeys(m);
            for (int i = 0; i < keys.Length; i++)
                SetDictKeyType(m, dict, keys[i], tags[i]);
            return dict;
        }

        internal static object GetArrayArrayEntries(IntPtr a, long i)
        {
            return GetArrayEntries(GetSafeArrayArray(a, i));
        }

        internal static IDictionary<string, object> GetArrayMapEntries(IntPtr a, long i)
        {
            return GetDictionary(GetSafeArrayMap(a, i));
        }

        internal static T[] GetArrayEntriesType<T>(IntPtr a, long size, Func<IntPtr, long, T> f)
        {
            T[] r = new T[size];
            for (long i = 0; i < size; i++)
                r[i] = f(a, i);
            return r;
        }

        internal static object GetArrayEntries(IntPtr a)
        {
            long size = GetSafeArraySize(a);
            if (size == 0)
                return null;

            switch (GetSafeArrayType(a, 0))
            {
                case Tag.kIntVal:
                case Tag.kInt64Val: return GetArrayEntriesType<long>(a, size, GetSafeNumberArray);
                case Tag.kDoubleVal: return GetArrayEntriesType<float>(a, size, GetSafeArrayFloat);
                case Tag.kBoolVal: return GetArrayEntriesType<bool>(a, size, GetSafeArrayBool);
                case Tag.kStringVal: return GetArrayEntriesType<string>(a, size, GetSafeArrayStringValue);
                case Tag.kArrayVal: return GetArrayEntriesType<object>(a, size, GetArrayArrayEntries);
                case Tag.kMapVal: return GetArrayEntriesType<IDictionary<string, object>>(a, size, GetArrayMapEntries);
            }
            return null;
        }

        internal static object GetMixedArrayEntries(IntPtr a)
        {
            long size = GetSafeArraySize(a);
            if (size == 0)
                return null;

            object[] r = new object[size];
            for (long i = 0; i < size; i++)
            {
                Tag tag = GetSafeArrayType(a, i);
                switch (tag)
                {
                    case Tag.kIntVal:
                    case Tag.kInt64Val: r[i] = GetSafeNumberArray(a, i); break;
                    case Tag.kDoubleVal: r[i] = GetSafeArrayFloat(a, i); break;
                    case Tag.kBoolVal: r[i] = GetSafeArrayBool(a, i); break;
                    case Tag.kStringVal: r[i] = GetSafeArrayStringValue(a, i); break;
                    case Tag.kArrayVal: r[i] = GetArrayArrayEntries(a, i); break;
                    case Tag.kMapVal: r[i] = GetArrayMapEntries(a, i); break;
                }
            }
            return r;
        }

        internal static void SetDictKeyType(IntPtr m, IDictionary<string, object> dict, string key, Tag tag)
        {
            switch (tag)
            {
                case Tag.kIntVal:
                case Tag.kInt64Val: dict[key] = GetSafeNumber(m, key, 0); break;
                case Tag.kDoubleVal: dict[key] = GetSafeFloat(m, key, 0); break;
                case Tag.kBoolVal: dict[key] = GetSafeBool(m, key, false); break;
                case Tag.kStringVal: dict[key] = GetSafeStringValue(m, key, ""); break;
                case Tag.kArrayVal: dict[key] = GetArrayEntries(GetSafeArray(m, key)); break;
                case Tag.kMixedArrayVal: dict[key] = GetMixedArrayEntries(GetSafeArray(m, key)); break;
                case Tag.kMapVal: dict[key] = GetDictionary(GetSafeMap(m, key)); break;
            }
        }
    }
}

