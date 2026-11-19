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
using Unity.Scripting.LifecycleManagement;


namespace UnityEngine
{
    ///<exclude />
    [NativeHeader("Modules/UnityAnalytics/RemoteSettings/RemoteSettings.h")]
    [NativeHeader("UnityAnalyticsScriptingClasses.h")]
    public static partial class RemoteSettings
    {
        ///<summary>Defines the delegate signature for handling <see cref="RemoteSettings.Updated" /> events.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class HandleRemoteSettings : MonoBehaviour
        ///{
        ///    private void Start()
        ///    {
        ///        // Add this class's updated settings handler to the RemoteSettings.Updated event.
        ///        RemoteSettings.Updated += RemoteSettingsUpdated;
        ///    }
        ///
        ///    private static void RemoteSettingsUpdated()
        ///    {
        ///        Debug.Log("***** GOT NEW REMOTE SETTINGS ******");
        ///        Debug.Log(RemoteSettings.GetInt("testInt"));
        ///        Debug.Log(RemoteSettings.GetString("testString"));
        ///        Debug.Log(RemoteSettings.GetFloat("testFloat"));
        ///        Debug.Log(RemoteSettings.GetBool("testBool"));
        ///        Debug.Log(RemoteSettings.GetBool("testFakeKey"));
        ///        Debug.Log(RemoteSettings.GetBool("testFakeKey", true));
        ///        Debug.Log(RemoteSettings.HasKey("qqq"));
        ///        Debug.Log(RemoteSettings.HasKey("testInt"));
        ///        Debug.Log(RemoteSettings.GetBool("unity.heatmaps"));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public delegate void UpdatedEventHandler();
        ///<summary>Dispatched when a remote settings configuration is fetched and successfully parsed from the server or from local cache.</summary>
        ///<remarks>**Updated** is always dispatched unless the computer or device has no Internet connection or cannot communicate with the Analytics Service, and no local, cached version of the remote settings file exists. This situation can occur when a player has no network connection the first time they run your game). In this situation, the **RemoteSettings** object does not dispatch an **Updated** event, and so does not update your game variables.
        ///
        ///Requesting the remote settings configuration over the network is an asynchronous process that might not complete before your initial Scene has finished loading, or might not complete at all, so you should always initialize your game variables to reasonable defaults.</remarks>
        [AutoStaticsCleanupOnCodeReload] // holds user-registered remote-settings updated handlers
        public static event UpdatedEventHandler Updated;
        ///<summary>Dispatched before the <see cref="RemoteSettings" /> object makes the network request for the latest settings.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class HandleRemoteSettings : MonoBehaviour
        ///{
        ///    private void Start()
        ///    {
        ///        RemoteSettings.BeforeFetchFromServer += RemoteSettingsBeforeFetchFromServer;
        ///    }
        ///
        ///    private static void RemoteSettingsBeforeFetchFromServer() { /*...*/ }
        ///}
        ///]]></code>
        ///</example>
        [AutoStaticsCleanupOnCodeReload] // holds user-registered pre-fetch handlers
        public static event Action BeforeFetchFromServer;
        ///<summary>Dispatched when the network request made by the <see cref="RemoteSettings" /> object to fetch the remote configuration file is complete.</summary>
        ///<remarks>Your event handler function must have the signature: <c>Handler(bool wasUpdatedFromServer, bool settingsChanged, int serverResponse)</c>.
        ///
        ///Check the <c>wasUpdatedFromServer</c> parameter passed to your event handler to determine whether a remote configration file was received as a result of the request. (This file could be identical to the local, cached version if you have not updated your settings.)
        ///
        ///Check the <c>settingsChanged</c> parameter to determine if any values in the received configuration changed since the last remote update.
        ///
        ///Check the <c>serverResponse</c> parameter passed to determine whether the request succeeded or not. This parameter contains a standard HTTP response code (for example, **200** on success).</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class HandleRemoteSettings : MonoBehaviour
        ///{
        ///    private void Start()
        ///    {
        ///        RemoteSettings.Completed += RemoteSettingsUpdateCompleted;
        ///    }
        ///
        ///    private static void RemoteSettingsUpdateCompleted(bool wasUpdatedFromServer, bool settingsChanged, int serverResponse) { /*...*/}
        ///}
        ///]]></code>
        ///</example>
        [AutoStaticsCleanupOnCodeReload] // holds user-registered fetch-completed handlers
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

        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Calling CallOnUpdate() is not necessary any more and should be removed. Use RemoteSettingsUpdated instead", true)]
        public static void CallOnUpdate()
        {
            throw new NotSupportedException("Calling CallOnUpdate() is not necessary any more and should be removed.");
        }

        ///<summary>Forces the game to download the newest settings from the server and update its values.</summary>
        ///<remarks>Primarily for use in development and testing; in normal operation, the remote settings are downloaded from the server at the beginning of every session, and so forcing an update is not necessary. However, during testing, when settings are changing frequently, it can be useful to update them immediately within a session.</remarks>
        public extern static void ForceUpdate();

        ///<summary>Reports whether or not the settings available from the <see cref="RemoteSettings" /> object were received from the Analytics Service during the current session.</summary>
        ///<remarks>When the remote settings were loaded from the local cache, this method returns false.
        ///
        ///Note that this method does not indicate whether the setting values have changed since the last successful network request.</remarks>
        ///<returns>True, if the remote settings file was received from the Analytics Service in the current session. False, if the remote settings file was received during an earlier session and cached.</returns>
        public extern static bool WasLastUpdatedFromServer();

        [uei.ExcludeFromDocs]
        public static int GetInt(string key) { return GetInt(key, 0); }
        ///<summary>Gets the value corresponding to remote setting identified by **key**, if it exists.</summary>
        ///<remarks>If it doesn't exist, it will return <c>defaultValue</c>. If you don't provide a default value, it will return 0.</remarks>
        ///<param name="key">The key identifying the setting.</param>
        ///<param name="defaultValue">The default value to use if the setting identified by the **key** parameter cannot be found or is unavailable.</param>
        ///<returns>The current value of the setting identified by **key**, or the default value.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class HandleRemoteSettings : MonoBehaviour
        ///{
        ///    private void Start()
        ///    {
        ///        Debug.Log(RemoteSettings.GetInt("maxLevelDifficulty"));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern static int GetInt(string key, [UnityEngine.Internal.DefaultValue("0")] int defaultValue);

        [uei.ExcludeFromDocs]
        public static long GetLong(string key) { return GetLong(key, 0); }
        ///<summary>Gets the value corresponding to remote setting identified by **key**, if it exists.</summary>
        ///<remarks>If it doesn't exist, it will return <c>defaultValue</c>. If you don't provide a default value, it will return 0.</remarks>
        ///<param name="key">The key identifying the setting.</param>
        ///<param name="defaultValue">The default value to use if the setting identified by the **key** parameter cannot be found or is unavailable.</param>
        ///<returns>The current value of the setting identified by **key**, or the default value.</returns>
        public extern static long GetLong(string key, [UnityEngine.Internal.DefaultValue("0")] long defaultValue);

        [uei.ExcludeFromDocs]
        public static float GetFloat(string key) { return GetFloat(key, 0.0F); }
        ///<summary>Gets the value corresponding to remote setting identified by **key**, if it exists.</summary>
        ///<remarks>If it doesn't exist, it will return <c>defaultValue</c>. If you don't provide a default value, it will return 0.0.</remarks>
        ///<param name="key">The key identifying the setting.</param>
        ///<param name="defaultValue">The default value to use if the setting identified by the **key** parameter cannot be found or is unavailable.</param>
        ///<returns>The current value of the setting identified by **key**, or the default value.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class HandleRemoteSettings : MonoBehaviour
        ///{
        ///    private void Start()
        ///    {
        ///        Debug.Log(RemoteSettings.GetFloat("gameBaseAcceleration"));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern static float GetFloat(string key, [UnityEngine.Internal.DefaultValue("0.0F")] float defaultValue);

        [uei.ExcludeFromDocs]
        public static string GetString(string key) { return GetString(key, ""); }
        ///<summary>Gets the value corresponding to remote setting identified by **key**, if it exists.</summary>
        ///<remarks>If it doesn't exist, it will return <c>defaultValue</c>. If you don't provide a default value, it will return "" (empty string).</remarks>
        ///<param name="key">The key identifying the setting.</param>
        ///<param name="defaultValue">The default value to use if the setting identified by the **key** parameter cannot be found or is unavailable.</param>
        ///<returns>The current value of the setting identified by **key**, or the default value.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class HandleRemoteSettings : MonoBehaviour
        ///{
        ///    private void Start()
        ///    {
        ///        Debug.Log(RemoteSettings.GetString("defaultPlayerName"));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern static string GetString(string key, [UnityEngine.Internal.DefaultValue("\"\"")] string defaultValue);

        [uei.ExcludeFromDocs]
        public static bool GetBool(string key) { return GetBool(key, false); }
        ///<summary>Gets the value corresponding to remote setting identified by **key**, if it exists.</summary>
        ///<remarks>If it doesn't exist, it will return <c>defaultValue</c>. If you don't provide a default value, it will return false.</remarks>
        ///<param name="key">The key identifying the setting.</param>
        ///<param name="defaultValue">The default value to use if the setting identified by the **key** parameter cannot be found or is unavailable.</param>
        ///<returns>The current value of the setting identified by **key**, or the default value.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class HandleRemoteSettings : MonoBehaviour
        ///{
        ///    private void Start()
        ///    {
        ///        Debug.Log(RemoteSettings.GetBool("enableBoss"));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern static bool GetBool(string key, [UnityEngine.Internal.DefaultValue("false")] bool defaultValue);

        ///<summary>Reports whether the specified <c>key</c> exists in the remote settings configuration.</summary>
        ///<param name="key">The key identifying the setting.</param>
        ///<returns>True, if the key exists.</returns>
        public extern static bool HasKey(string key);

        ///<summary>Gets the number of keys in the remote settings configuration.</summary>
        public extern static int GetCount();

        ///<summary>Gets an array containing all the keys in the remote settings configuration.</summary>
        public extern static string[] GetKeys();

        ///<summary>Gets the object corresponding to the remote setting identified by **key**, if it exists.</summary>
        ///<remarks>Remote Settings constructs an object of type <c>T</c> and sets its fields or properties to the corresponding remote value, matching field name to key name. The process ignores fields in the object that do not correspond to a remote value and, likewise, ignores remote values that do not correspond to a field or property in the type.
        ///
        ///If you do not specify a key when calling <c>GetObject()</c>, Remote Settings treats all of your remote settings as a single object. If you specify a key that does not exist, this function returns null.
        ///
        ///Remote Settings converts numbers and boolean types, but it does not convert string types. For example, if you map a float setting to an integer field, the float value is cast to an integer. However, if you attempt to map a numeric or boolean setting to a string field, the string field is left as null. If a remote setting contains an object, that object is converted according to the type of the field in the parent object struct or class.</remarks>
        ///<param name="key">The key identifying the setting.</param>
        ///<returns>An instance of the object with fields assigned the corresponding remote values.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class HandleRemoteSettingsGetObject : MonoBehaviour
        ///{
        ///    [System.Serializable]
        ///    public struct MySettings
        ///    {
        ///        public bool enableBoss;
        ///        public int maxLevelDifficulty;
        ///        public string defaultPlayerName;
        ///        public float gameBaseAcceleration;
        ///    }
        ///
        ///    private void Start()
        ///    {
        ///        MySettings ms = RemoteSettings.GetObject<MySettings>("myGameSettings");
        ///        Debug.Log(ms.maxLevelDifficulty);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static T GetObject<T>(string key = "") { return (T)GetObject(typeof(T), key); }

        ///<summary>Gets the object corresponding to the remote setting identified by **key**, if it exists.</summary>
        ///<remarks>Remote Settings constructs an object of the type specified by the <c>type</c> parameter and sets its fields or properties to the corresponding remote value, matching field name to key name. The process ignores fields in the object that do not correspond to a remote value and, likewise, ignores remote values that do not correspond to a field or property in the type.
        ///
        ///If you do not specify a key when calling <c>GetObject()</c>, Remote Settings treats all of your remote settings as a single object. If you specify a key that does not exist, this function returns null.
        ///
        ///Remote Settings converts numbers and boolean types, but it does not convert string types. For example, if you map a float setting to an integer field, the float value is cast to an integer. However, if you attempt to map a numeric or boolean setting to a string field, the string field is left as null. If a remote setting contains an object, that object is converted according to the type of the field in the parent object struct or class.</remarks>
        ///<param name="key">The key identifying the setting.</param>
        ///<param name="type">The type of object represented in RemoteSettings.</param>
        ///<returns>An instance of the object with fields assigned the corresponding remote values.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class HandleRemoteSettingsGetObjectWithType : MonoBehaviour
        ///{
        ///    [System.Serializable]
        ///    public struct MyCustomSettings
        ///    {
        ///        public bool enableBoss;
        ///        public int maxLevelDifficulty;
        ///        public string defaultPlayerName;
        ///        public float gameBaseAcceleration;
        ///    }
        ///
        ///    private void Start()
        ///    {
        ///        MyCustomSettings ms = (MyCustomSettings)RemoteSettings.GetObject(typeof(MyCustomSettings), "myGameSettings");
        ///        Debug.Log(ms.maxLevelDifficulty);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static object GetObject(Type type, string key = "")
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (type.IsAbstract || type.IsSubclassOf(typeof(UnityEngine.Object)))
                throw new ArgumentException("Cannot deserialize to new instances of type '" + type.Name + ".'");

            return GetAsScriptingObject(type, null, key);
        }

        ///<summary>Gets the object corresponding to the remote setting identified by **key**, if it exists.</summary>
        ///<remarks>Remote Settings constructs an object of the type specified by the <c>type</c> parameter and sets its fields or properties to the corresponding remote value, matching field name to key name. The process ignores fields in the object that do not correspond to a remote value and, likewise, ignores remote values that do not correspond to a field or property in the type.
        ///
        ///If you do not specify a key when calling <c>GetObject()</c>, Remote Settings treats all of your remote settings as a single object. If you specify a key that does not exist, this function returns null.
        ///
        ///Remote Settings converts numbers and boolean types, but it does not convert string types. For example, if you map a float setting to an integer field, the float value is cast to an integer. However, if you attempt to map a numeric or boolean setting to a string field, the string field is left as null. If a remote setting contains an object, that object is converted according to the type of the field in the parent object struct or class.</remarks>
        ///<param name="defaultValue">The object that should be for default values.</param>
        ///<param name="key">The key identifying the setting.</param>
        ///<returns>An instance of the object with fields assigned the corresponding remote values.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class HandleRemoteSettingsGetObjectWithDefault : MonoBehaviour
        ///{
        ///    [System.Serializable]
        ///    public struct MySettingValues
        ///    {
        ///        public bool enableBoss;
        ///        public int maxLevelDifficulty;
        ///        public string defaultPlayerName;
        ///        public float gameBaseAcceleration;
        ///    }
        ///
        ///    private void Start()
        ///    {
        ///        MySettingValues defaultValue = new MySettingValues();
        ///        defaultValue.enableBoss = true;
        ///        MySettingValues ms = (MySettingValues)RemoteSettings.GetObject("myGameSettings", defaultValue);
        ///        Debug.Log(ms.maxLevelDifficulty);
        ///    }
        ///}
        ///]]></code>
        ///</example>
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

        ///<summary>Gets a dictionary corresponding to the remote setting identified by **key**, if it exists.</summary>
        ///<remarks>Remote Settings creates a <c> Dictionary&lt;string, object&gt; </c> instance and adds the remote setting corresponding to the <c>key</c> parameter to it. If the setting is a simple type, then the dictionary contains a single element, which has a key matching the <c>key</c> parameter. If the remote setting is an object, then Remote Settings adds the field names and values of that object to the dictionary as the key-value pairs. If a remote value is an object rather than a simple type, Remote Settings adds a sub-dictionary containing the key-value pairs for the sub-object.
        ///
        ///If you do not specify a key when calling <c>GetDictionary()</c>, Remote Settings treats all of your remote settings as a single object. If you specify a key that does not exist, this function returns null.</remarks>
        ///<param name="key">The key identifying the setting.</param>
        ///<returns>An instance of <c> Dictionary&lt;string, object&gt; </c> containing the corresponding remote value or values.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections.Generic;
        ///
        ///public class HandleRemoteSettings : MonoBehaviour
        ///{
        ///    private void Start()
        ///    {
        ///        IDictionary<string, object> ms = RemoteSettings.GetDictionary("myGameSettings");
        ///        Debug.Log(ms);
        ///    }
        ///}
        ///]]></code>
        ///</example>
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
    [NativeHeader("Modules/UnityAnalyticsCommon/Public/UnityAnalyticsCommon.h")]
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

#pragma warning disable UAC0045 // Frees the native settings; Internal_Destroy is thread-safe and public users may forget Dispose.
        ~RemoteConfigSettings()
        {
            Destroy();
        }
#pragma warning restore UAC0045

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

        internal static extern IntPtr Internal_Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] RemoteConfigSettings rcs, string configKey);
        [NativeMethod(IsThreadSafe = true)]
        internal static extern void Internal_Destroy(IntPtr ptr);

        [RequiredByNativeCode]
        internal static void RemoteConfigSettingsUpdated(RemoteConfigSettings rcs, bool wasLastUpdatedFromServer)
        {
            var handler = rcs.Updated;
            if (handler != null)
                handler(wasLastUpdatedFromServer);
        }

        public extern static Analytics.AnalyticsResult QueueConfig(string name, object param, int ver = 1, string prefix = "");

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

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(RemoteConfigSettings remoteConfigSettings) => remoteConfigSettings.m_Ptr;
        }
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

