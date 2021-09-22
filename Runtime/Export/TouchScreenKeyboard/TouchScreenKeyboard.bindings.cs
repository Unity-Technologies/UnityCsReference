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

    // Interface into the native iPhone, Android, UWP, PS4 and Switch on-screen keyboards - stubbed on other platforms.
    [NativeConditional("ENABLE_ONSCREEN_KEYBOARD")]
    [NativeHeader("Runtime/Export/TouchScreenKeyboard/TouchScreenKeyboard.bindings.h")]
    [NativeHeader("Runtime/Input/KeyboardOnScreen.h")]
    public class TouchScreenKeyboard
    {
        // The status of the on-screen keyboard
        public enum Status
        {
            // The on-screen keyboard is open.
            Visible = 0,
            // The on-screen keyboard was closed with ok / done buttons.
            Done = 1,
            // The on-screen keyboard was closed with a back button.
            Canceled = 2,
            // The on-screen keyboard was closed by touching outside of the keyboard.
            LostFocus = 3,
        }

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
                    case RuntimePlatform.PS4:
                    case RuntimePlatform.PS5:
                    case RuntimePlatform.Stadia:

                    // The OnScreen keyboard is ALWAYS supported in UWP
                    // Whether or not it can be used is determined automatically by the OS at runtime
                    case RuntimePlatform.WSAPlayerX86:
                    case RuntimePlatform.WSAPlayerX64:
                    case RuntimePlatform.WSAPlayerARM:
                        return true;

                    default:
                        return false;
                }
            }
        }

        // Used by tests, we want touch screen keyboard to show up even if physical keyboard is connected.
        // That's the case with Android devices on Katana, where NVIDIA shields have physical keyboards connected
        // Meaning when clicking on input fields, touch screen keyboard won't show, since physical keyboard is present.
        // Disabling in place editing, forces touch screen keyboard to show up.
        internal static bool disableInPlaceEditing { get; set; }

        public static bool isInPlaceEditingAllowed
        {
            get
            {
                if (disableInPlaceEditing)
                    return false;
                return false;
            }
        }


        // Opens the native keyboard provided by OS on the screen.
        public static TouchScreenKeyboard Open(string text, [DefaultValue("TouchScreenKeyboardType.Default")]  TouchScreenKeyboardType keyboardType, [DefaultValue("true")]  bool autocorrection, [DefaultValue("false")]  bool multiline, [DefaultValue("false")]  bool secure, [DefaultValue("false")]  bool alert, [DefaultValue("\"\"")]  string textPlaceholder, [DefaultValue("0")]  int characterLimit)
        {
            return new TouchScreenKeyboard(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder, characterLimit);
        }

        [ExcludeFromDocs]
        public static TouchScreenKeyboard Open(string text, TouchScreenKeyboardType keyboardType, bool autocorrection, bool multiline, bool secure, bool alert, string textPlaceholder)
        {
            int characterLimit = 0;
            return Open(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder, characterLimit);
        }

        [ExcludeFromDocs]
        public static TouchScreenKeyboard Open(string text, TouchScreenKeyboardType keyboardType, bool autocorrection, bool multiline, bool secure, bool alert)
        {
            int characterLimit = 0;
            string textPlaceholder = "";
            return Open(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder, characterLimit);
        }

        [ExcludeFromDocs]
        public static TouchScreenKeyboard Open(string text, TouchScreenKeyboardType keyboardType, bool autocorrection, bool multiline, bool secure)
        {
            int characterLimit = 0;
            string textPlaceholder = "";
            bool alert = false;
            return Open(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder, characterLimit);
        }

        [ExcludeFromDocs]
        public static TouchScreenKeyboard Open(string text, TouchScreenKeyboardType keyboardType, bool autocorrection, bool multiline)
        {
            int characterLimit = 0;
            string textPlaceholder = "";
            bool alert = false;
            bool secure = false;
            return Open(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder, characterLimit);
        }

        [ExcludeFromDocs]
        public static TouchScreenKeyboard Open(string text, TouchScreenKeyboardType keyboardType, bool autocorrection)
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

        // Returns the text displayed by the input field of the keyboard.
        public extern string text
        {
            [NativeName("GetText")]
            get;
            [NativeName("SetText")]
            set;
        }

        // Specifies if text input field above the keyboard will be hidden when the keyboard is on screen.
        public static extern bool hideInput
        {
            [NativeName("IsInputHidden")]
            get;
            [NativeName("SetInputHidden")]
            set;
        }

        // Specifies if the keyboard is visible or is sliding into the position on screen.
        public extern bool active
        {
            [NativeName("IsActive")]
            get;
            [NativeName("SetActive")]
            set;
        }

        [FreeFunction("TouchScreenKeyboard_GetDone")]
        private static extern bool GetDone(IntPtr ptr);

        // Specifies if input process was finished (RO)
        [Obsolete("Property done is deprecated, use status instead")]
        public bool done
        {
            get { return GetDone(m_Ptr); }
        }

        [FreeFunction("TouchScreenKeyboard_GetWasCanceled")]
        private static extern bool GetWasCanceled(IntPtr ptr);

        // Specifies if input process was canceled (RO)
        [Obsolete("Property wasCanceled is deprecated, use status instead.")]
        public bool wasCanceled
        {
            get { return GetWasCanceled(m_Ptr); }
        }

        // Returns the status of the touch screen keyboard (RO). See [[TouchScreenKeyboard.Status]] enumeration for possible values.
        public extern Status status
        {
            [NativeName("GetKeyboardStatus")]
            get;
        }

        // Set character limit for keyboard input
        public extern int characterLimit
        {
            [NativeName("GetCharacterLimit")]
            get;
            [NativeName("SetCharacterLimit")]
            set;
        }

        public extern bool canGetSelection
        {
            [NativeName("CanGetSelection")]
            get;
        }

        public extern bool canSetSelection
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

        private static extern void GetSelection(out int start, out int length);

        private static extern void SetSelection(int start, int length);

        // Returns the type of keyboard being displayed. (RO)
        public extern TouchScreenKeyboardType type
        {
            [NativeName("GetKeyboardType")]
            get;
        }

        public int targetDisplay
        {
            get { return 0; }
            set {}
        }

        // Returns portion of the screen which is covered by the keyboard.
        [NativeConditional("ENABLE_ONSCREEN_KEYBOARD", "RectT<float>()")]
        public static extern Rect area
        {
            [NativeName("GetRect")]
            get;
        }

        // Returns true whenever any keyboard is completely visible on the screen.
        public static extern bool visible
        {
            [NativeName("IsVisible")]
            get;
        }
    }
}
