// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using uei = UnityEngine.Internal;


namespace UnityEngine
{
    [NativeHeader("Modules/UnityConnect/RemoteSettings.h")]
    [NativeHeader("UnityConnectScriptingClasses.h")]
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
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityConnect/RemoteSettings.h")]
    [NativeHeader("UnityConnectScriptingClasses.h")]
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
    }
}

