// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Android
{
    ///<summary>Use this class to access the runtime data of your Android application.</summary>
    [NativeHeader("Modules/AndroidJNI/Public/AndroidApplication.bindings.h")]
    [StaticAccessor("AndroidApplication", StaticAccessorType.DoubleColon)]
    public static partial class AndroidApplication
    {
        [NoAutoStaticsCleanup]
        private static SynchronizationContext m_MainThreadSynchronizationContext;
        [NoAutoStaticsCleanup]
        private static AndroidJavaObjectUnityOwned m_Context = null;
        [NoAutoStaticsCleanup]
        private static AndroidJavaObjectUnityOwned m_Activity = null;
        [NoAutoStaticsCleanup]
        private static AndroidJavaObjectUnityOwned m_UnityPlayer = null;
        internal static extern IntPtr UnityPlayerRaw { [NativeMethod(IsThreadSafe = true)] get; }
        private static extern IntPtr CurrentContextRaw { [NativeMethod(IsThreadSafe = true)] get; }
        private static extern IntPtr CurrentActivityRaw { [NativeMethod(IsThreadSafe = true)] get; }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void AcquireMainThreadSynchronizationContext()
        {
            m_MainThreadSynchronizationContext = UnitySynchronizationContext.Current;
            if (m_MainThreadSynchronizationContext == null)
                throw new Exception("Failed to acquire main thread synchronization context");
        }

        ///<summary>Indicates the Java instance of the current context.</summary>
        ///<remarks>For more information, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/Context"&gt;Context&lt;/a&gt;.
        ///
        ///**Note:** This object is managed by Unity runtime, so do not call <see cref="AndroidJavaObject.Dispose" /> on it.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var isRunningGameActivity = AndroidApplication.currentContext.Call<string>("getLocalClassName").Equals("com.unity3d.player.UnityPlayerGameActivity");
        ///        var isRunningActivity = AndroidApplication.currentContext.Call<string>("getLocalClassName").Equals("com.unity3d.player.UnityPlayerActivity");
        ///        Debug.Log($"GameActivity {isRunningGameActivity}");
        ///        Debug.Log($"Activity {isRunningActivity}");
        ///    }
        ///}]]></code>
        ///</example>
        public static AndroidJavaObject currentContext
        {
            get
            {
                return m_Context;
            }
        }

        ///<summary>Indicates the Java instance of the current activity.</summary>
        ///<remarks>**Notes:**
        ///
        ///- This object is managed by Unity runtime, so do not call <see cref="AndroidJavaObject.Dispose" /> on it.
        ///- If the application is a service rather than an activity, this property returns null.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var isRunningGameActivity = AndroidApplication.currentActivity.Call<string>("getLocalClassName").Equals("com.unity3d.player.UnityPlayerGameActivity");
        ///        var isRunningActivity = AndroidApplication.currentActivity.Call<string>("getLocalClassName").Equals("com.unity3d.player.UnityPlayerActivity");
        ///        Debug.Log($"GameActivity {isRunningGameActivity}");
        ///        Debug.Log($"Activity {isRunningActivity}");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AndroidApplication.currentContext" />
        public static AndroidJavaObject currentActivity
        {
            get
            {
                return m_Activity;
            }
        }


        ///<summary>Indicates the Unity bridge Java instance used by an activity or a service.</summary>
        ///<remarks>You can access this property from Unity's main thread which means you can use it in <c>Start()</c>, <c>Awake()</c>, or in methods with <see cref="RuntimeInitializeOnLoadMethodAttribute" />. However, you cannot access it from constructors or field initializers as these are called on the loader thread.
        ///
        ///**Note:** This object is managed by Unity runtime, so do not call <see cref="AndroidJavaObject.Dispose" /> on it.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var bridgeClass = AndroidApplication.unityPlayer.Call<string>("toString");
        ///        var isActivity = bridgeClass.StartsWith("com.unity3d.player.UnityPlayerForActivityOrService");
        ///        var isGameActivity = bridgeClass.StartsWith("com.unity3d.player.UnityPlayerForGameActivity");
        ///        Debug.Log($"Class for Activity: {isActivity}, Class for GameActivity: {isGameActivity}");
        ///    }
        ///}]]></code>
        ///</example>
        public static AndroidJavaObject unityPlayer
        {
            get
            {
                return m_UnityPlayer;
            }
        }

        ///<summary>Provides current window insets for the application.</summary>
        ///<remarks>The window insets represent the system UI elements, such as the status and navigation bars.
        ///
        ///**Note:** The insets are relative to the activity's frame layout. Querying insets manually through this property is a slow operation</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        var insets = AndroidApplication.currentWindowInsets;
        ///        Debug.Log($"NavigationBars visible " + insets.IsVisible(AndroidWindowInsets.Type.NavigationBars));
        ///        Debug.Log($"StatusBars visible " + insets.IsVisible(AndroidWindowInsets.Type.StatusBars));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AndroidApplication.onWindowInsetsChanged" />
        public static AndroidWindowInsets currentWindowInsets => GetCurrentWindowInsets();

        [NoAutoStaticsCleanup]
        private static AndroidConfiguration m_CurrentConfiguration;
        [NoAutoStaticsCleanup]
        private static AndroidWindowInsets m_CurrentWindowInsets;

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void SetCurrentConfiguration(AndroidConfiguration config)
        {
            m_CurrentConfiguration = config;
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static AndroidConfiguration GetCurrentConfiguration()
        {
            return m_CurrentConfiguration;
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void DispatchConfigurationChanged(bool notifySubscribers)
        {
            if (notifySubscribers)
                onConfigurationChanged?.Invoke(m_CurrentConfiguration);
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void SetCurrentWindowInsets(AndroidWindowInsets insets)
        {
            m_CurrentWindowInsets = insets;
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static AndroidWindowInsets GetCurrentWindowInsets()
        {
            return m_CurrentWindowInsets;
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void DispatchWindowInsetsChanged()
        {
            onWindowInsetsChanged?.Invoke(m_CurrentWindowInsets);
        }

        ///<summary>Provides current configuration for the running application.</summary>
        ///<example nocheck="true">
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class ShowConfiguration : MonoBehaviour
        ///{
        ///
        ///    public void Start()
        ///    {
        ///        var contents = new System.Text.StringBuilder();
        ///        var c = AndroidApplication.currentConfiguration;
        ///        contents.AppendLine($"* ColorMode, Hdr: {c.colorModeHdr}");
        ///        contents.AppendLine($"* ColorMode, Gamut: {c.colorModeWideColorGamut}");
        ///        contents.AppendLine($"* DensityDpi: {c.densityDpi}");
        ///        contents.AppendLine($"* FontScale: {c.fontScale}");
        ///        contents.AppendLine($"* FontWeightAdj: {c.fontWeightAdjustment}");
        ///        contents.AppendLine($"* Keyboard: {c.keyboard}");
        ///        contents.AppendLine($"* Keyboard Hidden, Hard: {c.hardKeyboardHidden}");
        ///        contents.AppendLine($"* Keyboard Hidden, Normal: {c.keyboardHidden}");
        ///        contents.AppendLine($"* Mcc: {c.mobileCountryCode}");
        ///        contents.AppendLine($"* Mnc: {c.mobileNetworkCode}");
        ///        contents.AppendLine($"* Navigation: {c.navigation}");
        ///        contents.AppendLine($"* NavigationHidden: {c.navigationHidden}");
        ///        contents.AppendLine($"* Orientation: {c.orientation}");
        ///        contents.AppendLine($"* ScreenHeightDp: {c.screenHeightDp}");
        ///        contents.AppendLine($"* ScreenWidthDp: {c.screenWidthDp}");
        ///        contents.AppendLine($"* SmallestScreenWidthDp: {c.smallestScreenWidthDp}");
        ///        contents.AppendLine($"* ScreenLayout, Direction: {c.screenLayoutDirection}");
        ///        contents.AppendLine($"* ScreenLayout, Size: {c.screenLayoutSize}");
        ///        contents.AppendLine($"* ScreenLayout, Long: {c.screenLayoutLong}");
        ///        contents.AppendLine($"* ScreenLayout, Round: {c.screenLayoutRound}");
        ///        contents.AppendLine($"* TouchScreen: {c.touchScreen}");
        ///        contents.AppendLine($"* UiMode, Night: {c.uiModeNight}");
        ///        contents.AppendLine($"* UiMode, Type: {c.uiModeType}");
        ///
        ///        contents.AppendLine($"* Locales ({c.locales.Length}):");
        ///        for (int i = 0; i < c.locales.Length; i++)
        ///        {
        ///            var l = c.locales[i];
        ///            contents.AppendLine($"* Locale[{i}] {l.country}-{l.language}");
        ///        };
        ///
        ///        Debug.Log($"Current Config:\n{contents}");
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="AndroidApplication.onConfigurationChanged" />
        public static AndroidConfiguration currentConfiguration => m_CurrentConfiguration;

        ///<summary>A callback to detect the device configuration changes when the application is running.</summary>
        ///<remarks>Unity invokes this callback for the configuration changes related to the following aspects.
        ///
        ///- Orientation
        ///- Keyboard visibility
        ///- Dark theme
        ///- Screen layout
        ///- Screen size
        ///
        ///For more information on the configuration changes, refer to the &lt;a href="https://developer.android.com/guide/topics/resources/runtime-changes"&gt;Android developer documentation&lt;/a&gt;.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class MyApplication : MonoBehaviour
        ///{
        ///    AndroidConfiguration m_PrevConfig;
        ///
        ///    public void Start()
        ///    {
        ///        m_PrevConfig = new AndroidConfiguration(AndroidApplication.currentConfiguration);
        ///        AndroidApplication.onConfigurationChanged += OnConfigurationChanged;
        ///    }
        ///
        ///    public void OnDisable()
        ///    {
        ///        AndroidApplication.onConfigurationChanged -= OnConfigurationChanged;
        ///    }
        ///
        ///    private void OnConfigurationChanged(AndroidConfiguration newConfig)
        ///    {
        ///        if (m_PrevConfig.orientation != newConfig.orientation ||
        ///            m_PrevConfig.screenLayoutSize != newConfig.screenLayoutSize)
        ///        {
        ///            ApplyUIChanges(newConfig.orientation, newConfig.screenLayoutSize);
        ///        }
        ///
        ///        if (m_PrevConfig.uiModeNight != newConfig.uiModeNight)
        ///        {
        ///            ApplyUINightMode(newConfig.uiModeNight);
        ///        }
        ///
        ///        if (m_PrevConfig.screenHeightDp != newConfig.screenHeightDp ||
        ///            m_PrevConfig.screenWidthDp != newConfig.screenWidthDp)
        ///        {
        ///            ApplyScreenSizeChanges();
        ///        }
        ///
        ///        m_PrevConfig.CopyFrom(newConfig);
        ///    }
        ///
        ///    private void ApplyUIChanges(AndroidOrientation orientation, AndroidScreenLayoutSize layoutSize)
        ///    {
        ///
        ///    }
        ///
        ///    private void ApplyUINightMode(AndroidUIModeNight nightMode)
        ///    {
        ///
        ///    }
        ///
        ///    private void ApplyScreenSizeChanges()
        ///    {
        ///
        ///    }
        ///}]]></code>
        ///</example>
        [AutoStaticsCleanupOnCodeReload]
        public static event Action<AndroidConfiguration> onConfigurationChanged;

        ///<summary>A callback to detect changes in the window insets when the application is running.</summary>
        ///<remarks>**Notes:**
        ///
        ///* The callback is only supported on Android 11 (API 30) or later versions. On older devices, you need to detect changes in the insets manually as demonstrated in the following code example.
        ///* Querying insets manually using <see cref="AndroidApplication.currentWindowInsets" /> is a slow operation.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    private bool InsetsCallbackSupported => AndroidBuild.Version.apiLevel >= 30;
        ///    private bool StatusBars;
        ///    private bool NavigationBars;
        ///
        ///    public void Start()
        ///    {
        ///        if (InsetsCallbackSupported)
        ///            AndroidApplication.onWindowInsetsChanged += OnInsetsChanged;
        ///        else
        ///        {
        ///            var insets = AndroidApplication.currentWindowInsets;
        ///            StatusBars = insets.IsVisible(AndroidWindowInsets.Type.StatusBars);
        ///            NavigationBars = insets.IsVisible(AndroidWindowInsets.Type.NavigationBars);
        ///        }
        ///    }
        ///
        ///    public void Update()
        ///    {
        ///        if (!InsetsCallbackSupported)
        ///        {
        ///            var insets = AndroidApplication.currentWindowInsets;
        ///            var currentStatusBars = insets.IsVisible(AndroidWindowInsets.Type.StatusBars);
        ///            var currentNavigationBars = insets.IsVisible(AndroidWindowInsets.Type.NavigationBars);
        ///            if (StatusBars != currentStatusBars || NavigationBars != currentNavigationBars)
        ///            {
        ///                OnInsetsChanged(insets);
        ///                StatusBars = currentStatusBars;
        ///                NavigationBars = currentNavigationBars;
        ///            }
        ///        }
        ///    }
        ///
        ///    public void OnDisable()
        ///    {
        ///        if (InsetsCallbackSupported)
        ///            AndroidApplication.onWindowInsetsChanged -= OnInsetsChanged;
        ///    }
        ///
        ///    void OnInsetsChanged(AndroidWindowInsets insets)
        ///    {
        ///        Debug.Log($"NavigationBars visible " + insets.IsVisible(AndroidWindowInsets.Type.NavigationBars));
        ///        Debug.Log($"StatusBars visible " + insets.IsVisible(AndroidWindowInsets.Type.StatusBars));
        ///    }
        ///}]]></code>
        ///</example>
        [AutoStaticsCleanupOnCodeReload]
        public static event Action<AndroidWindowInsets> onWindowInsetsChanged;

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void DispatchOnMultiWindowModeChanged(bool newValue)
        {
            onMultiWindowModeChanged?.Invoke(newValue);
        }

        ///<summary>Called by the system when the activity changes from fullscreen mode to multi-window mode and vice-versa.</summary>
        ///<seealso href="https://developer.android.com/reference/android/app/Activity#onMultiWindowModeChanged(boolean)">onMultiWindowModeChanged</seealso>
        [AutoStaticsCleanupOnCodeReload]
        public static event Action<bool> onMultiWindowModeChanged;

        ///<summary>Returns true if the activity is currently in multi-window mode.</summary>
        ///<seealso href="https://developer.android.com/reference/android/app/Activity#isInMultiWindowMode()">isInMultiWindowMode</seealso>
        public static extern bool isInMultiWindowMode { get; }

        ///<summary>Invokes delegate on Android application's UI thread.</summary>
        ///<remarks>**Note:** Certain Android Java functions, such as those related to the user interface can only be called on the UI thread.</remarks>
        ///<example nocheck="true">
        ///  <code><![CDATA[using System;
        ///using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///
        ///public class JavaThreads: MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        AndroidApplication.InvokeOnUIThread(() =>
        ///        {
        ///            // Button can only be added on UI thread
        ///            using var button = new AndroidJavaObject("android.widget.Button", AndroidApplication.currentActivity);
        ///            button.Call("setText", "Hello World");
        ///            using var layoutParams = new AndroidJavaObject("android.widget.LinearLayout$LayoutParams", 500, 100);
        ///            button.Call("setLayoutParams", layoutParams);
        ///            AndroidApplication.unityPlayer
        ///                .Call<AndroidJavaObject>("getFrameLayout")
        ///                .Call("addView", button);
        ///        });
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso href="https://developer.android.com/guide/components/processes-and-threads#Threads">Threads</seealso>
        public static void InvokeOnUIThread(Action action)
        {
        }

        ///<summary>Invokes delegate on Android application's main thread.</summary>
        ///<remarks>This is useful if you receive a Java callback on the UI thread, but want to process result on Unity's main thread.
        ///
        ///This method blocks the calling thread until the Unity main thread finishes running the delegate. It has no effect in the Editor.</remarks>
        ///<example nocheck="true">
        ///  <code><![CDATA[using System.Threading;
        ///using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class JavaThreads : MonoBehaviour
        ///{
        ///    public class MyButtonListener : AndroidJavaProxy
        ///    {
        ///        public MyButtonListener()
        ///            : base("android.view.View$OnClickListener")
        ///        {
        ///        }
        ///
        ///        public void onClick(AndroidJavaObject view)
        ///        {
        ///            Debug.Log($"onClick called on UI thread ${Thread.CurrentThread.ManagedThreadId}");
        ///
        ///            AndroidApplication.InvokeOnUnityMainThread(() =>
        ///            {
        ///                Debug.Log($"Delegating to main thread ${Thread.CurrentThread.ManagedThreadId}");
        ///            });
        ///
        ///            view.Dispose();
        ///        }
        ///    }
        ///    public void Start()
        ///    {
        ///        AndroidApplication.InvokeOnUIThread(() =>
        ///        {
        ///            // Button can only be added on UI thread
        ///            using var button = new AndroidJavaObject("android.widget.Button", AndroidApplication.currentActivity);
        ///            button.Call("setText", "Hello World");
        ///            using var layoutParams = new AndroidJavaObject("android.widget.LinearLayout$LayoutParams", 500, 100);
        ///            button.Call("setLayoutParams", layoutParams);
        ///            button.Call("setOnClickListener", new MyButtonListener());
        ///
        ///            AndroidApplication.unityPlayer
        ///                .Call<AndroidJavaObject>("getFrameLayout")
        ///                .Call("addView", button);
        ///        });
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso href="https://developer.android.com/guide/components/processes-and-threads#Threads">Threads</seealso>
        public static void InvokeOnUnityMainThread(Action action)
        {
        }
    }
}
