// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using UnityEngine.Internal;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Simple struct that contains all the arguments needed by
    internal struct TouchScreenKeyboard_InternalConstructorHelperArguments
    {
        public uint keyboardType;
        public uint autocorrection;
        public uint multiline;
        public uint secure;
        public uint alert;
        public int characterLimit;
    }

    // Describes options for displaying movie playback controls.
    public enum FullScreenMovieControlMode
    {
        // Display the standard controls for controlling movie playback. This
        Full = 0,
        // Display minimal set of controls controlling movie playback. Set of
        Minimal = 1,
        // Do not display any controls, but cancel movie playback if input occurs.
        CancelOnInput = 2,
        // Do not display any controls. This mode prevents the user from
        Hidden = 3,
    }

    // Describes scaling modes for displaying movies.
    public enum FullScreenMovieScalingMode
    {
        // Do not scale the movie.
        None = 0,
        // Scale the movie until one dimension fits on the screen exactly. In
        AspectFit = 1,
        // Scale the movie until the movie fills the entire screen. Content at
        AspectFill = 2,
        // Scale the movie until both dimensions fit the screen exactly. The
        Fill = 3,
    }

    public enum AndroidActivityIndicatorStyle
    {
        /// Do not show ActivityIndicator
        DontShow = -1,
        /// Large (android.R.attr.progressBarStyleLarge).
        Large = 0,
        /// Large Inversed (android.R.attr.progressBarStyleLargeInverse).
        InversedLarge = 1,
        /// Small (android.R.attr.progressBarStyleSmall).
        Small = 2,
        /// Small Inversed (android.R.attr.progressBarStyleSmallInverse).
        InversedSmall = 3,
    }

    [NativeHeader("Runtime/Video/MoviePlayback.h")]
    [NativeHeader("Runtime/Export/Handheld.bindings.h")]
    [NativeHeader("Runtime/Input/GetInput.h")]
    // Interface into functionality unique to handheld devices.
    public class Handheld
    {
        //Plays a full-screen movie.
        public static bool PlayFullScreenMovie(string path, [DefaultValue("Color.black")]  Color bgColor , [DefaultValue("FullScreenMovieControlMode.Full")]  FullScreenMovieControlMode controlMode , [DefaultValue("FullScreenMovieScalingMode.AspectFit")]  FullScreenMovieScalingMode scalingMode)
        {
            return PlayFullScreenMovie_Bindings(path, bgColor, controlMode, scalingMode);
        }

        [ExcludeFromDocs]
        public static bool PlayFullScreenMovie(string path, Color bgColor , FullScreenMovieControlMode controlMode)
        {
            FullScreenMovieScalingMode scalingMode = FullScreenMovieScalingMode.AspectFit;
            return PlayFullScreenMovie_Bindings(path, bgColor, controlMode, scalingMode);
        }

        [ExcludeFromDocs]
        public static bool PlayFullScreenMovie(string path, Color bgColor)
        {
            FullScreenMovieScalingMode scalingMode = FullScreenMovieScalingMode.AspectFit;
            FullScreenMovieControlMode controlMode = FullScreenMovieControlMode.Full;
            return PlayFullScreenMovie_Bindings(path, bgColor, controlMode, scalingMode);
        }

        [ExcludeFromDocs]
        public static bool PlayFullScreenMovie(string path)
        {
            FullScreenMovieScalingMode scalingMode = FullScreenMovieScalingMode.AspectFit;
            FullScreenMovieControlMode controlMode = FullScreenMovieControlMode.Full;
            Color bgColor = Color.black;
            return PlayFullScreenMovie_Bindings(path, bgColor, controlMode, scalingMode);
        }

        [FreeFunction("PlayFullScreenMovie")]
        private static extern bool PlayFullScreenMovie_Bindings(string path, Color bgColor, FullScreenMovieControlMode controlMode, FullScreenMovieScalingMode scalingMode);

        // Triggers device vibration.
        [FreeFunction("Vibrate")]
        public static extern void Vibrate();

        [Obsolete("Property Handheld.use32BitDisplayBuffer has been deprecated. Modifying it has no effect, use PlayerSettings instead.")]
        public static bool use32BitDisplayBuffer
        {
            get { return GetUse32BitDisplayBuffer_Bindings(); }
            set {}
        }

        [FreeFunction("GetUse32BitDisplayBuffer_Bindings")]
        private static extern bool GetUse32BitDisplayBuffer_Bindings();

        [FreeFunction("SetActivityIndicatorStyle_Bindings")]
        private static extern void SetActivityIndicatorStyleImpl_Bindings(int style);


        /// Sets ActivityIndicator style. See AndroidActivityIndicatorStyle enumeration for possible values.
        /// Be warned that it will take effect on next call to StartActivityIndicator.
        public static void SetActivityIndicatorStyle(AndroidActivityIndicatorStyle style)
        {
            SetActivityIndicatorStyleImpl_Bindings((int)style);
        }

        // Gets current ActivityIndicator style.
        [FreeFunction("GetActivityIndicatorStyle_Bindings")]
        public static extern int GetActivityIndicatorStyle();

        // Starts os activity indicator
        [FreeFunction("StartActivityIndicator_Bindings")]
        public static extern void StartActivityIndicator();

        // Stops os activity indicator
        [FreeFunction("StopActivityIndicator_Bindings")]
        public static extern void StopActivityIndicator();

        //*undocumented*
        [FreeFunction("ClearShaderCache_Bindings")]
        public static extern void ClearShaderCache();
    }

    // Interface into the native iPhone, Android, Windows Phone and Switch on-screen keyboards - it is not available on other platforms.
    [NativeHeader("Runtime/Export/Handheld.bindings.h")]
    [NativeHeader("Runtime/Input/OnScreenKeyboard.h")]
    public partial class TouchScreenKeyboard
    {
        // We are matching the KeyboardOnScreen class here so we can directly
        // access it.
        [System.NonSerialized]
        internal IntPtr m_Ptr;

        [FreeFunction("TouchScreenKeyboard_Destroy", IsThreadSafe = true)]
        private static extern void Internal_Destroy(IntPtr ptr);

        private void Destroy()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        //*undocumented*
        ~TouchScreenKeyboard()
        {
            Destroy();
        }

        //*undocumented*
        public TouchScreenKeyboard(string text, TouchScreenKeyboardType keyboardType, bool autocorrection, bool multiline, bool secure, bool alert, string textPlaceholder, int characterLimit)
        {
            TouchScreenKeyboard_InternalConstructorHelperArguments arguments = new TouchScreenKeyboard_InternalConstructorHelperArguments();
            arguments.keyboardType = Convert.ToUInt32(keyboardType);
            arguments.autocorrection = Convert.ToUInt32(autocorrection);
            arguments.multiline = Convert.ToUInt32(multiline);
            arguments.secure = Convert.ToUInt32(secure);
            arguments.alert = Convert.ToUInt32(alert);
            arguments.characterLimit = characterLimit;
            m_Ptr = TouchScreenKeyboard_InternalConstructorHelper(ref arguments, text, textPlaceholder);
        }

        [FreeFunction("TouchScreenKeyboard_InternalConstructorHelper")]
        private static extern IntPtr TouchScreenKeyboard_InternalConstructorHelper(ref TouchScreenKeyboard_InternalConstructorHelperArguments arguments, string text, string textPlaceholder);


        public static bool isSupported
        {
            get
            {
                RuntimePlatform platform = Application.platform;
                switch (platform)
                {
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.tvOS:
                    case RuntimePlatform.Android:
                    case RuntimePlatform.Switch:
                        return true;
                    case RuntimePlatform.WSAPlayerX86:
                    case RuntimePlatform.WSAPlayerX64:
                    case RuntimePlatform.WSAPlayerARM:
                        return false;
                    default:
                        return false;
                }
            }
        }


        // Opens the native keyboard provided by OS on the screen.
        public static TouchScreenKeyboard Open(string text, [DefaultValue("TouchScreenKeyboardType.Default")]  TouchScreenKeyboardType keyboardType , [DefaultValue("true")]  bool autocorrection , [DefaultValue("false")]  bool multiline , [DefaultValue("false")]  bool secure , [DefaultValue("false")]  bool alert , [DefaultValue("\"\"")]  string textPlaceholder , [DefaultValue("0")]  int characterLimit)
        {
            return new TouchScreenKeyboard(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder, characterLimit);
        }

        [ExcludeFromDocs]
        public static TouchScreenKeyboard Open(string text, TouchScreenKeyboardType keyboardType , bool autocorrection , bool multiline , bool secure , bool alert , string textPlaceholder)
        {
            int characterLimit = 0;
            return Open(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder, characterLimit);
        }

        [ExcludeFromDocs]
        public static TouchScreenKeyboard Open(string text, TouchScreenKeyboardType keyboardType , bool autocorrection , bool multiline , bool secure , bool alert)
        {
            int characterLimit = 0;
            string textPlaceholder = "";
            return Open(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder, characterLimit);
        }

        [ExcludeFromDocs]
        public static TouchScreenKeyboard Open(string text, TouchScreenKeyboardType keyboardType , bool autocorrection , bool multiline , bool secure)
        {
            int characterLimit = 0;
            string textPlaceholder = "";
            bool alert = false;
            return Open(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder, characterLimit);
        }

        [ExcludeFromDocs]
        public static TouchScreenKeyboard Open(string text, TouchScreenKeyboardType keyboardType , bool autocorrection , bool multiline)
        {
            int characterLimit = 0;
            string textPlaceholder = "";
            bool alert = false;
            bool secure = false;
            return Open(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder, characterLimit);
        }

        [ExcludeFromDocs]
        public static TouchScreenKeyboard Open(string text, TouchScreenKeyboardType keyboardType , bool autocorrection)
        {
            int characterLimit = 0;
            string textPlaceholder = "";
            bool alert = false;
            bool secure = false;
            bool multiline = false;
            return Open(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder, characterLimit);
        }

        [ExcludeFromDocs]
        public static TouchScreenKeyboard Open(string text, TouchScreenKeyboardType keyboardType)
        {
            int characterLimit = 0;
            string textPlaceholder = "";
            bool alert = false;
            bool secure = false;
            bool multiline = false;
            bool autocorrection = true;
            return Open(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder, characterLimit);
        }

        [ExcludeFromDocs]
        public static TouchScreenKeyboard Open(string text)
        {
            int characterLimit = 0;
            string textPlaceholder = "";
            bool alert = false;
            bool secure = false;
            bool multiline = false;
            bool autocorrection = true;
            TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default;
            return Open(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder, characterLimit);
        }

        // Returns the text displayed by the input field of the keyboard. This
        extern public string text
        {
            [NativeName("getText")]
            get;
            [NativeName("setText")]
            set;
        }

        // Specifies if text input field above the keyboard will be hidden when
        extern public static bool hideInput
        {
            [NativeName("isInputHidden")]
            get;
            [NativeName("setInputHidden")]
            set;
        }

        // Specifies if the keyboard is visible or is sliding into the position on
        extern public bool active
        {
            [NativeName("isActive")]
            get;
            [NativeName("setActive")]
            set;
        }

        [FreeFunction("TouchScreenKeyboard_GetDone")]
        extern private static bool GetDone(IntPtr ptr);

        // Specifies if input process was finished (RO)
        [Obsolete("Property done is deprecated, use status instead")]
        public bool done
        {
            get { return GetDone(m_Ptr); }
        }

        [FreeFunction("TouchScreenKeyboard_GetWasCanceled")]
        extern private static bool GetWasCanceled(IntPtr ptr);

        // Specifies if input process was canceled (RO)
        [Obsolete("Property wasCanceled is deprecated, use status instead.")]
        public bool wasCanceled
        {
            get { return GetWasCanceled(m_Ptr); }
        }

        // Returns the status of the touch screen keyboard (RO). See [[TouchScreenKeyboard.Status]] enumeration for possible values.
        extern public Status status
        {
            [NativeName("GetKeyboardStatus")]
            get;
        }

        // Set character limit for keyboard input
        extern public int characterLimit
        {
            [NativeName("getCharacterLimit")]
            get;
            [NativeName("setCharacterLimit")]
            set;
        }

        public bool canGetSelection
        {
            [NativeName("CanGetSelection")]
            get;
        }

        public bool canSetSelection
        {
            [NativeName("CanSetSelection")]
            get;
        }

        public RangeInt selection
        {
            get
            {
                RangeInt range;
                GetSelection(out range.start, out range.length);
                return range;
            }
            set
            {
                if (value.start < 0 || value.length < 0 || value.start + value.length > text.Length)
                    throw new ArgumentOutOfRangeException(nameof(selection), "Selection is out of range.");
                SetSelection(value.start, value.length);
            }
        }

        extern private static void GetSelection(out int start, out int length);

        extern private static void SetSelection(int start, int length);

        // Returns the type of keyboard being displayed. (RO)
        public TouchScreenKeyboardType type
        {
            [NativeName("GetKeyboardType")]
            get;
        }

        public int targetDisplay
        {
            get { return 0; }
            set {}
        }

        // Returns portion of the screen which is covered by the keyboard. Returns
        extern public static Rect area
        {
            [NativeName("GetRect")]
            get;
        }

        // Returns true whenever any keyboard is completely visible on the screen.
        extern public static bool visible
        {
            [NativeName("IsVisible")]
            get;
        }
    }


}
