// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Android
{
    /// <summary>Provides control over the windows that generate insets.</summary>
    /// <remarks>These windows represent the system UI elements for system bars, such as the status and navigation bars. Use this class to determine the current state of the system bars, control their visibility and behavior at runtime, allowing your application to make full use of the device screen space.</remarks>
    /// <example>
    ///  <code><![CDATA[using UnityEngine;
    ///using UnityEngine.Android;
    ///
    ///public class AndroidInsets : MonoBehaviour
    ///{
    ///    public string GetInsetsVisibility(AndroidWindowInsets.Type type)
    ///    {
    ///        if (AndroidApplication.currentWindowInsets == null)
    ///            return "Unknown";
    ///        return AndroidApplication.currentWindowInsets.IsVisible(type) ? "Visible" : "Hidden";
    ///    }
    ///
    ///    public void OnGUI()
    ///    {
    ///        var options = new[] { GUILayout.Height(100), GUILayout.ExpandWidth(true) };
    ///        GUILayout.Space(100);
    ///        GUILayout.Label($"Status Bars: {GetInsetsVisibility(AndroidWindowInsets.Type.StatusBars)}", options);
    ///        GUILayout.Label($"Navigation Bars: {GetInsetsVisibility(AndroidWindowInsets.Type.NavigationBars)}", options);
    ///
    ///        var insets = AndroidApplication.currentWindowInsets;
    ///        GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));
    ///        if (GUILayout.Button("Show Status Bars", options))
    ///            insets.Show(AndroidWindowInsets.Type.StatusBars);
    ///        if (GUILayout.Button("Hide Status Bars", options))
    ///            insets.Hide(AndroidWindowInsets.Type.StatusBars);
    ///        GUILayout.EndHorizontal();
    ///
    ///        GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));
    ///        if (GUILayout.Button("Show Navigation Bars", options))
    ///            insets.Show(AndroidWindowInsets.Type.NavigationBars);
    ///        if (GUILayout.Button("Hide Navigation Bars", options))
    ///            insets.Hide(AndroidWindowInsets.Type.NavigationBars);
    ///        GUILayout.EndHorizontal();
    ///
    ///        GUILayout.Label($"System Bars Behavior: {insets.GetSystemBarsBehavior()}", options);
    ///        GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));
    ///        if (GUILayout.Button("Default", options))
    ///            insets.SetSystemBarsBehavior(AndroidWindowInsets.SystemBarsBehavior.Default);
    ///        if (GUILayout.Button("ShowTransientBarsBySwipe", options))
    ///            insets.SetSystemBarsBehavior(AndroidWindowInsets.SystemBarsBehavior.ShowTransientBarsBySwipe);
    ///        GUILayout.EndHorizontal();
    ///    }
    ///}
    ///]]></code>
    /// </example>
    /// <seealso href="https://developer.android.com/develop/ui/compose/system/insets">About window insets (Android)</seealso>
    [NativeHeader("Modules/AndroidJNI/Public/AndroidWindowInsets.bindings.h")]
    [StaticAccessor("AndroidWindowInsets", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public class AndroidWindowInsets
    {
        /// <summary>Options for specifying different types of system UI elements that generate window insets.</summary>
        /// <remarks>Use this enum with <see cref="AndroidWindowInsets.Hide" />, <see cref="AndroidWindowInsets.IsVisible" />, and <see cref="AndroidWindowInsets.Show" /> methods to retrieve and modify the current state of the status bar, navigation bar, or both at runtime.</remarks>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        AndroidApplication.currentWindowInsets.Show(AndroidWindowInsets.Type.StatusBars | AndroidWindowInsets.Type.NavigationBars);
        ///    }
        ///}]]></code>
        /// </example>
        [Flags]
        public enum Type
        {
            /// <summary>No inset type selected. No screen area is assigned for system UI elements.</summary>
            None = 0,

            /// <summary>Represents the area of the screen the system status bar occupies at the top of the screen.</summary>
            /// <remarks>This area displays information, such as time, battery level, and notifications amongst other system status information.</remarks>
            StatusBars = 1 << 0,

            /// <summary>Represents the area of the screen the system navigation elements occupy.</summary>
            /// <remarks>This area includes the gesture bar or the traditional navigation buttons.</remarks>
            NavigationBars = 1 << 1,
            /*
            CaptionBar = 1 << 2,
            IME = 1 << 3,
            SystemGestures = 1 << 4,
            MandatorySystemGestures = 1 << 5,
            TappableElement = 1 << 6,
            DisplayCutout = 1 << 7
            */
        }

        /// <summary>Controls the foreground appearance of system bar icons and text.</summary>
        /// <remarks>Use this enum with <see cref="AndroidWindowInsets.SetAppearance" /> and <see cref="AndroidWindowInsets.GetAppearance" /> to control the appearance of system bar icons per inset type at runtime. Requires Android 11 (API 30) and later.</remarks>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        var insets = AndroidApplication.currentWindowInsets;
        ///        // Light icons on the status bar (suitable for dark game backgrounds).
        ///        insets.SetAppearance(AndroidWindowInsets.Type.StatusBars, AndroidWindowInsets.Appearance.Light);
        ///        // Dark icons on the navigation bar (suitable for light game backgrounds).
        ///        insets.SetAppearance(AndroidWindowInsets.Type.NavigationBars, AndroidWindowInsets.Appearance.Dark);
        ///        // You can also pass multiple inset types to set the same appearance for all of them.
        ///        insets.SetAppearance(AndroidWindowInsets.Type.StatusBars | AndroidWindowInsets.Type.NavigationBars, AndroidWindowInsets.Appearance.Light);
        ///    }
        ///}]]></code>
        /// </example>
        public enum Appearance : int
        {
            /// <summary>Light icons, suitable for dark backgrounds behind the system bars.</summary>
            Light,
            /// <summary>Dark icons, suitable for light backgrounds behind the system bars.</summary>
            Dark
        }

        /// <summary>Options for controlling the behavior of system bars, such as the status and navigation bars.</summary>
        /// <remarks>These options determine how system gestures, such as swiping from screen edges, reveal the system bars when hidden. Use this enum with <see cref="AndroidWindowInsets.GetSystemBarsBehavior" /> and <see cref="AndroidWindowInsets.SetSystemBarsBehavior" /> methods to control the system bars behavior for your application window at runtime.</remarks>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        AndroidApplication.currentWindowInsets.SetSystemBarsBehavior(AndroidWindowInsets.SystemBarsBehavior.ShowTransientBarsBySwipe);
        ///    }
        ///}]]></code>
        /// </example>
        public enum SystemBarsBehavior : int
        {
            /// <summary>The system bars behavior is undefined, for example, when running on unsupported Android versions, API 29 or earlier.</summary>
            Undefined = -1,
            /// <summary>Reveals the system bars with system gestures, such as swiping from the edge of the screen where the bar is hidden.</summary>
            /// <remarks>The system bars remain visible until those are hidden again by calling <see cref="Android.AndroidWindowInsets.Hide" />.</remarks>
            Default = 1,
            /// <summary>Reveals the system bars temporarily with system gestures, such as swiping from the edge of the screen where the bar is hidden.</summary>
            /// <remarks>These temporary system bars overlay app content, might have some transparency, and automatically hide after a short timeout.</remarks>
            ShowTransientBarsBySwipe = 2
        }

        /// <summary>Controls whether system bars have an opaque or transparent background.</summary>
        /// <remarks>Use this enum with <see cref="AndroidWindowInsets.SetSystemBarsBackground" /> and <see cref="AndroidWindowInsets.GetSystemBarsBackground" /> to control the system bars background at runtime. Set to <c>Transparent</c> when insets are overlapping with the application window to allow game content to show through. Requires Android 11 (API 30) and later.</remarks>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        // Make system bars transparent for edge-to-edge rendering.
        ///        AndroidApplication.currentWindowInsets.SetSystemBarsBackground(AndroidWindowInsets.SystemBarsBackground.Transparent);
        ///    }
        ///}]]></code>
        /// </example>
        public enum SystemBarsBackground : int
        {
            /// <summary>System bars have an opaque background. This is the default.</summary>
            Opaque,
            /// <summary>System bars have a transparent background, allowing game content to show through when insets overlap with the application window.</summary>
            Transparent
        }


        IntPtr m_NativeHandle;

        internal AndroidWindowInsets()
        {
        }

        [RequiredByNativeCode]
        private static void SetNativeHandle(AndroidWindowInsets self, IntPtr ptr)
        {
            self.m_NativeHandle = ptr;
        }

        [RequiredByNativeCode]
        private static int[] GetSupportedInsets()
        {
            // For internal purposes, remove if is exposed in Type
            const Type CaptionBar = (Type)(1 << 2);
            const Type Ime = (Type)(1 << 3);
            return new[]
            {
                (int)Type.StatusBars,
                (int)Type.NavigationBars,
                (int)CaptionBar,
                (int)Ime
            };
        }

        private static extern void InternalShow(Type type);

        /// <summary>Displays a set of windows that generate insets on screen.</summary>
        /// <remarks>Use this method to display the system bars, such as the status and navigation bars at runtime.</remarks>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        AndroidApplication.currentWindowInsets.Show(AndroidWindowInsets.Type.NavigationBars | AndroidWindowInsets.Type.StatusBars);
        ///    }
        ///}]]></code>
        /// </example>
        /// <seealso cref="AndroidApplication.onWindowInsetsChanged" />
        /// <seealso cref="AndroidApplication.currentWindowInsets" />
        /// <seealso cref="Android.AndroidWindowInsets.Type">Type</seealso>
        public void Show(Type type)
        {
            InternalShow(type);
        }

        private static extern void InternalHide(Type type);

        /// <summary>Hides a set of windows that generate insets.</summary>
        /// <remarks>Use this method to hide the system bars, allowing your application to use the available screen space when in full-screen mode.</remarks>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        AndroidApplication.currentWindowInsets.Hide(AndroidWindowInsets.Type.NavigationBars | AndroidWindowInsets.Type.StatusBars);
        ///    }
        ///}]]></code>
        /// </example>
        /// <seealso cref="AndroidApplication.onWindowInsetsChanged" />
        /// <seealso cref="AndroidApplication.currentWindowInsets" />
        /// <seealso cref="Android.AndroidWindowInsets.Type">Type</seealso>
        public void Hide(Type type)
        {
            InternalHide(type);
        }

        private static extern RectInt InternalGetInsets(IntPtr handle, Type type);

        internal RectInt GetInsets(Type type)
        {
            return InternalGetInsets(m_NativeHandle, type);
        }

        /// <summary>Indicates whether a set of windows that might generate insets is currently visible on screen, regardless of whether they overlap with your application window.</summary>
        /// <remarks>This method allows you to check whether the system UI elements for the status and navigation bars are currently visible or hidden on the screen.</remarks>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        var insets = AndroidApplication.currentWindowInsets;
        ///        Debug.Log("NavigationBars: " + insets.IsVisible(AndroidWindowInsets.Type.NavigationBars));
        ///        Debug.Log("StatusBars: " + insets.IsVisible(AndroidWindowInsets.Type.StatusBars));
        ///    }
        ///}]]></code>
        /// </example>
        /// <seealso cref="Android.AndroidWindowInsets.Type">Type</seealso>
        public bool IsVisible(Type type)
        {
            var insets = GetInsets(type);
            // Note: Android has different coordinate system, thus height can be negative here
            return insets.width != 0 || insets.height != 0;
        }

        private static extern void InternalSetSystemBarsBehavior(int behavior);

        /// <summary>Controls the behavior of system bars.</summary>
        /// <remarks>This method allows you to configure how the system bars, such as the status and navigation bars, should respond to system gestures in your application.
        ///
        ///**Note:** Only supported on Android 11 (API 30) or later. Has no effect on Android 10 (API 29) or earlier versions.</remarks>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        AndroidApplication.currentWindowInsets.SetSystemBarsBehavior(AndroidWindowInsets.SystemBarsBehavior.ShowTransientBarsBySwipe);
        ///    }
        ///}]]></code>
        /// </example>
        /// <seealso cref="Android.AndroidWindowInsets.SystemBarsBehavior">SystemBarsBehavior</seealso>
        public void SetSystemBarsBehavior(SystemBarsBehavior behavior)
        {
            InternalSetSystemBarsBehavior((int)behavior);
        }

        private static extern int InternalGetSystemBarsBehavior();

        /// <summary>Retrieves the configured behavior of system bars, such as status and navigation bars.</summary>
        /// <remarks>This behavior determines how system gestures reveal the system bars when hidden at runtime.</remarks>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        Debug.Log("SystemBarsBehavior: " + AndroidApplication.currentWindowInsets.GetSystemBarsBehavior());
        ///    }
        ///}]]></code>
        /// </example>
        public SystemBarsBehavior GetSystemBarsBehavior()
        {
            return (SystemBarsBehavior)InternalGetSystemBarsBehavior();
        }

        private static extern void InternalSetOverlapping(Type type);

        /// <summary>Sets which insets are allowed to overlap with the application window.</summary>
        /// <remarks>When an inset is overlapping, the application window extends behind it instead of being resized to avoid it. <see cref="Screen.safeArea" /> excludes areas covered by overlapping insets. The parameter replaces the entire overlapping mask.
        ///
        ///**Note:** Only supported on Android 11 (API 30) or later.</remarks>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        AndroidApplication.currentWindowInsets.SetOverlapping(AndroidWindowInsets.Type.StatusBars | AndroidWindowInsets.Type.NavigationBars);
        ///    }
        ///}]]></code>
        /// </example>
        /// <param name="type">The complete set of inset types that should overlap with the application window. Replaces the entire overlapping mask.</param>
        public void SetOverlapping(Type type)
        {
            InternalSetOverlapping(type);
        }

        private static extern int InternalGetOverlapping();

        /// <summary>Returns which insets are currently configured to overlap with the application window.</summary>
        /// <remarks>Returns a bitmask of inset types that are currently overlapping. When an inset is overlapping, the application window extends behind it and <see cref="Screen.safeArea" /> excludes the area it covers.</remarks>
        /// <returns>The bitmask of inset types that currently overlap with the application window.</returns>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        var overlapping = AndroidApplication.currentWindowInsets.GetOverlapping();
        ///        Debug.Log("Status bar overlapping: " + overlapping.HasFlag(AndroidWindowInsets.Type.StatusBars));
        ///        Debug.Log("Navigation bar overlapping: " + overlapping.HasFlag(AndroidWindowInsets.Type.NavigationBars));
        ///    }
        ///}]]></code>
        /// </example>
        public Type GetOverlapping()
        {
            return (Type)InternalGetOverlapping();
        }

        [NativeMethod(ThrowsException = true)]
        private static extern Rect InternalGetBounds(Type type);

        /// <summary>Returns the screen-space rectangle where an overlapping inset covers the application window.</summary>
        /// <remarks>Returns the bounds in Unity screen coordinates (origin bottom-left, y-up), matching the coordinate system of <see cref="Screen.safeArea" />. Returns <see cref="Rect.zero" /> if the specified inset is not overlapping or if <see cref="Type.None" /> is passed. Only a single inset type may be passed; passing combined flags throws an <see cref="ArgumentException" />.
        ///
        ///The returned rect accounts for resolution scaling when <see cref="Screen.SetResolution" /> is used.</remarks>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Update()
        ///    {
        ///        var statusBarBounds = AndroidApplication.currentWindowInsets.GetBounds(AndroidWindowInsets.Type.StatusBars);
        ///        if (statusBarBounds.width > 0)
        ///            Debug.Log("Status bar covers: " + statusBarBounds);
        ///    }
        ///}]]></code>
        /// </example>
        /// <param name="type">A single inset type to query. Passing combined flags throws an <see cref="ArgumentException" />.</param>
        /// <returns>The screen-space rectangle where the overlapping inset covers the application window, or <see cref="Rect.zero" /> if the inset is not overlapping.</returns>
        public Rect GetBounds(Type type)
        {
            return InternalGetBounds(type);
        }

        private static extern void InternalShowOverlapping(Type type, Type overlapping);

        /// <summary>Shows the specified insets and sets the overlapping mask atomically in a single UI thread dispatch.</summary>
        /// <remarks>The <paramref name="overlapping"/> parameter replaces the entire overlapping mask, it does not accumulate with previous values. For example, calling <c>Show(StatusBars, StatusBars)</c> followed by <c>Show(NavigationBars, NavigationBars)</c> leaves only NavigationBars as overlapping.</remarks>
        /// <param name="type">The set of inset types to make visible in the application window.</param>
        /// <param name="overlapping">The complete set of inset types that should overlap with the application window.</param>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        // Show both bars and make them overlap with the application windw.
        ///        AndroidApplication.currentWindowInsets.Show(
        ///            AndroidWindowInsets.Type.StatusBars | AndroidWindowInsets.Type.NavigationBars,
        ///            AndroidWindowInsets.Type.StatusBars | AndroidWindowInsets.Type.NavigationBars);
        ///    }
        ///}]]></code>
        /// </example>
        public void Show(Type type, Type overlapping)
        {
            InternalShowOverlapping(type, overlapping);
        }

        private static extern void InternalSetSystemBarsBackground(int background);

        /// <summary>Sets the system bars background to opaque or transparent.</summary>
        /// <remarks>Set to Transparent when insets are overlapping with the application window to allow game content to show through the system bars. This applies to all system bars and cannot be set per inset.
        ///
        ///**Note:** Only supported on Android 11 (API 30) or later.</remarks>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        AndroidApplication.currentWindowInsets.SetSystemBarsBackground(AndroidWindowInsets.SystemBarsBackground.Transparent);
        ///    }
        ///}]]></code>
        /// </example>
        /// <param name="background">The background mode to set for all system bars.</param>
        public void SetSystemBarsBackground(SystemBarsBackground background)
        {
            InternalSetSystemBarsBackground((int)background);
        }

        private static extern int InternalGetSystemBarsBackground();

        /// <summary>Returns the current system bars background setting.</summary>
        /// <remarks>Returns whether the system bars currently have an opaque or transparent background. Use <c>Transparent</c> when insets are overlapping with the application window to allow game content to show through the system bars.</remarks>
        /// <returns>The current system bars background, either <see cref="SystemBarsBackground.Opaque" /> or <see cref="SystemBarsBackground.Transparent" />.</returns>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        var bg = AndroidApplication.currentWindowInsets.GetSystemBarsBackground();
        ///        Debug.Log("System bars background: " + bg);
        ///    }
        ///}]]></code>
        /// </example>
        public SystemBarsBackground GetSystemBarsBackground()
        {
            return (SystemBarsBackground)InternalGetSystemBarsBackground();
        }

        private static extern void InternalSetAppearance(Type type, int appearance);

        /// <summary>Sets the foreground appearance of the specified inset types at runtime.</summary>
        /// <remarks>Controls whether system bar icons appear light or dark for individual inset types. Multiple inset types can be combined to set the same appearance for all of them.
        ///
        ///**Note:** Only supported on Android 11 (API 30) or later.</remarks>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        var insets = AndroidApplication.currentWindowInsets;
        ///        insets.SetAppearance(AndroidWindowInsets.Type.StatusBars, AndroidWindowInsets.Appearance.Light);
        ///        // Set both bars at once.
        ///        insets.SetAppearance(AndroidWindowInsets.Type.StatusBars | AndroidWindowInsets.Type.NavigationBars, AndroidWindowInsets.Appearance.Dark);
        ///    }
        ///}]]></code>
        /// </example>
        /// <param name="type">The inset types to apply the appearance to. Multiple types can be combined with bitwise OR.</param>
        /// <param name="appearance">The foreground appearance to set.</param>
        public void SetAppearance(Type type, Appearance appearance)
        {
            InternalSetAppearance(type, (int)appearance);
        }

        [NativeMethod(ThrowsException = true)]
        private static extern int InternalGetAppearance(Type type);

        /// <summary>Returns the current foreground appearance of the specified inset type.</summary>
        /// <remarks>Returns whether the specified inset type currently has light or dark icons. Only a single inset type may be passed; passing combined flags throws an <see cref="ArgumentException" />. Returns <c>Light</c> for <see cref="Type.None" />.
        ///
        ///**Note:** Only supported on Android 11 (API 30) or later.</remarks>
        /// <example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Controller : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        var appearance = AndroidApplication.currentWindowInsets.GetAppearance(AndroidWindowInsets.Type.StatusBars);
        ///        Debug.Log("Status bar appearance: " + appearance);
        ///    }
        ///}]]></code>
        /// </example>
        /// <param name="type">A single inset type to query. Passing combined flags throws an <see cref="ArgumentException" />.</param>
        /// <returns>The current appearance, either <c>Light</c> or <c>Dark</c>.</returns>
        public Appearance GetAppearance(Type type)
        {
            return (Appearance)InternalGetAppearance(type);
        }
    }
}
