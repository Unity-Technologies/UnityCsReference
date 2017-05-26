// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    partial class iPhoneSettings
    {
        [Obsolete("screenOrientation property is deprecated. Please use Screen.orientation instead (UnityUpgradable) -> Screen.orientation", true)]
        public static iPhoneScreenOrientation screenOrientation { get { return default(iPhoneScreenOrientation); } }

        [Obsolete("uniqueIdentifier property is deprecated. Please use SystemInfo.deviceUniqueIdentifier instead (UnityUpgradable) -> SystemInfo.deviceUniqueIdentifier", true)]
        public static string uniqueIdentifier { get { return string.Empty; } }

        [Obsolete("name property is deprecated (UnityUpgradable). Please use SystemInfo.deviceName instead (UnityUpgradable) -> SystemInfo.deviceName", true)]
        public static string name { get { return string.Empty; } }

        [Obsolete("model property is deprecated. Please use SystemInfo.deviceModel instead (UnityUpgradable) -> SystemInfo.deviceModel", true)]
        public static string model { get { return string.Empty; } }

        [Obsolete("systemName property is deprecated. Please use SystemInfo.operatingSystem instead (UnityUpgradable) -> SystemInfo.operatingSystem", true)]
        public static string systemName { get { return string.Empty; } }

        [Obsolete("internetReachability property is deprecated. Please use Application.internetReachability instead (UnityUpgradable) -> Application.internetReachability", true)]
        public static iPhoneNetworkReachability internetReachability { get { return default(iPhoneNetworkReachability); } }

        [Obsolete("systemVersion property is deprecated. Please use iOS.Device.systemVersion instead (UnityUpgradable) -> UnityEngine.iOS.Device.systemVersion", true)]
        public static string systemVersion { get { return string.Empty; } }

        [Obsolete("generation property is deprecated. Please use iOS.Device.generation instead (UnityUpgradable) -> UnityEngine.iOS.Device.generation", true)]
        public static iPhoneGeneration generation { get { return default(iPhoneGeneration); } }
    }

    [Obsolete("iPhoneTouch struct is deprecated. Please use Touch instead (UnityUpgradable) -> Touch", true)]
    public struct iPhoneTouch
    {
        [Obsolete("positionDelta property is deprecated. Please use Touch.deltaPosition instead (UnityUpgradable) -> Touch.deltaPosition", true)]
        public Vector2 positionDelta { get { return new Vector2(); } }

        [Obsolete("timeDelta property is deprecated. Please use Touch.deltaTime instead (UnityUpgradable) -> Touch.deltaTime", true)]
        public float timeDelta { get {  return default(int); } }

        public int fingerId { get { return default(int); } }
        public Vector2 position { get { return default(Vector2); } }
        public Vector2 deltaPosition { get { return default(Vector2); } }
        public float deltaTime { get {  return default(float); } }
        public int tapCount { get { return default(int); } }
        public iPhoneTouchPhase phase { get { return default(iPhoneTouchPhase); } }
    }


    // Interface into iPhone miscellaneous functionality.
    public class iPhoneUtils
    {
        // we want to avoid obsolete warnings:
        // if method is not marked as obsolete and uses obsolete types we get warning
        // so prototypes for c-side calls will use ints and we manually create default-params overloads
        [Obsolete("PlayMovie method is deprecated. Please use Handheld.PlayFullScreenMovie instead (UnityUpgradable) -> [mscorlib] System.Boolean Handheld.PlayFullScreenMovie(*)")]
        public static void PlayMovie(string path, Color bgColor, iPhoneMovieControlMode controlMode, iPhoneMovieScalingMode scalingMode) {}

        [Obsolete("PlayMovie method is deprecated. Please use Handheld.PlayFullScreenMovie instead (UnityUpgradable) -> [mscorlib] System.Boolean Handheld.PlayFullScreenMovie(*)")]
        public static void PlayMovie(string path, Color bgColor, iPhoneMovieControlMode controlMode) {}

        [Obsolete("PlayMovie method is deprecated. Please use Handheld.PlayFullScreenMovie instead (UnityUpgradable) -> [mscorlib] System.Boolean Handheld.PlayFullScreenMovie(*)")]
        public static void PlayMovie(string path, Color bgColor) {}

        [Obsolete("PlayMovieURL method is deprecated. Please use Handheld.PlayFullScreenMovie instead (UnityUpgradable) -> [mscorlib] System.Boolean Handheld.PlayFullScreenMovie(*)")]
        public static void PlayMovieURL(string url, Color bgColor, iPhoneMovieControlMode controlMode, iPhoneMovieScalingMode scalingMode) {}

        [Obsolete("PlayMovieURL method is deprecated. Please use Handheld.PlayFullScreenMovie instead (UnityUpgradable) -> [mscorlib] System.Boolean Handheld.PlayFullScreenMovie(*)")]
        public static void PlayMovieURL(string url, Color bgColor, iPhoneMovieControlMode controlMode) {}

        [Obsolete("PlayMovieURL method is deprecated. Please use Handheld.PlayFullScreenMovie instead (UnityUpgradable) -> [mscorlib] System.Boolean Handheld.PlayFullScreenMovie(*)")]
        public static void PlayMovieURL(string url, Color bgColor) {}

        [Obsolete("Vibrate method is deprecated. Please use Handheld.Vibrate instead (UnityUpgradable) -> Handheld.Vibrate()")]
        public static void Vibrate() {}

        [Obsolete("isApplicationGenuine property is deprecated. Please use Application.genuine instead (UnityUpgradable) -> Application.genuine")]
        public static bool isApplicationGenuine { get { return false; } }

        [Obsolete("isApplicationGenuineAvailable property is deprecated. Please use Application.genuineCheckAvailable instead (UnityUpgradable) -> Application.genuineCheckAvailable")]
        public static bool isApplicationGenuineAvailable { get { return false; } }
    }

    [Obsolete("iPhoneTouchPhase enumeration is deprecated. Please use TouchPhase instead (UnityUpgradable) -> TouchPhase", true)]
    public enum iPhoneTouchPhase
    {
        Began,
        Moved,
        Stationary,
        Ended,
        Canceled
    }

    [Obsolete("iPhoneAccelerationEvent struct is deprecated. Please use AccelerationEvent instead (UnityUpgradable) -> AccelerationEvent", true)]
    public struct iPhoneAccelerationEvent
    {
        [Obsolete("timeDelta property is deprecated. Please use AccelerationEvent.deltaTime instead (UnityUpgradable) -> AccelerationEvent.deltaTime", true)]
        public float timeDelta { get { return 0; } }

        public Vector3 acceleration { get { return default(Vector3); } }
        public float deltaTime { get { return -1.0f; } }
    }

    [Obsolete("iPhoneOrientation enumeration is deprecated. Please use DeviceOrientation instead (UnityUpgradable) -> DeviceOrientation", true)]
    public enum iPhoneOrientation
    {
        Unknown,
        Portrait,
        PortraitUpsideDown,
        LandscapeLeft,
        LandscapeRight,
        FaceUp,
        FaceDown
    }

    // The iPhoneInput class acts as the interface into the iPhone's unique Input systems.
    [Obsolete("iPhoneInput class is deprecated. Please use Input instead (UnityUpgradable) -> Input", true)]
    public class iPhoneInput
    {
        [Obsolete("orientation property is deprecated. Please use Input.deviceOrientation instead (UnityUpgradable) -> Input.deviceOrientation", true)]
        public static iPhoneOrientation orientation { get { return default(iPhoneOrientation); } }

        [Obsolete("lastLocation property is deprecated. Please use Input.location.lastData instead.", true)]
        public static LocationInfo lastLocation { get { return default(LocationInfo); } }

        // Folowing members are not strictly required to update references from iPhoneInput -> Input, but
        // any method that takes an argument like iPhoneInput.touchCount will fail resolution if the member (touchCount) is not present
        // in the "old" version of the type and the update may fail.

        public static iPhoneAccelerationEvent[] accelerationEvents { get { return null; } }
        public static iPhoneTouch[] touches { get { return null; }  }
        public static int touchCount { get { return 0; }    }
        public static bool multiTouchEnabled { get { return false; } set {} }
        public static int accelerationEventCount { get { return 0; }    }
        public static Vector3 acceleration { get { return default(Vector3); } }
        public static iPhoneTouch GetTouch(int index) { return default(iPhoneTouch); }
        public static iPhoneAccelerationEvent GetAccelerationEvent(int index) { return default(iPhoneAccelerationEvent); }
    }

    [Obsolete("iPhoneScreenOrientation enumeration is deprecated. Please use ScreenOrientation instead (UnityUpgradable) -> ScreenOrientation", true)]
    public enum iPhoneScreenOrientation
    {
        Unknown,
        Portrait,
        PortraitUpsideDown,
        LandscapeLeft,
        LandscapeRight,
        AutoRotation,
        Landscape
    }

    [Obsolete("iPhoneKeyboardType enumeration is deprecated. Please use TouchScreenKeyboardType instead (UnityUpgradable) -> TouchScreenKeyboardType", true)]
    public enum iPhoneKeyboardType
    {
        Default,
        ASCIICapable,
        NumbersAndPunctuation,
        URL,
        NumberPad,
        PhonePad,
        NamePhonePad,
        EmailAddress
    }

    [Obsolete("iPhoneKeyboard class is deprecated. Please use TouchScreenKeyboard instead (UnityUpgradable) -> TouchScreenKeyboard", true)]
    public class iPhoneKeyboard
    {
        public string text { get { return string.Empty; } set {} }
        public static bool hideInput { get { return false; } set {} }
        public bool active { get { return false; } set {} }
        public bool done { get { return false; } }
        public static Rect area { get { return default(Rect); } }
        public static bool visible { get { return false; } }
    }

    [Obsolete("iPhoneMovieControlMode enumeration is deprecated. Please use FullScreenMovieControlMode instead (UnityUpgradable) -> FullScreenMovieControlMode", true)]
    public enum iPhoneMovieControlMode
    {
        Full,
        Minimal,
        [Obsolete] CancelOnTouch,
        Hidden,
        [Obsolete] VolumeOnly
    }

    [Obsolete("iPhoneMovieScalingMode enumeration is deprecated. Please use FullScreenMovieScalingMode instead  (UnityUpgradable) -> FullScreenMovieScalingMode", true)]
    public enum iPhoneMovieScalingMode
    {
        None,
        AspectFit,
        AspectFill,
        Fill
    }

    [Obsolete("iPhoneNetworkReachability enumeration is deprecated. Please use NetworkReachability instead (UnityUpgradable) -> NetworkReachability", true)]
    public enum iPhoneNetworkReachability
    {
        NotReachable,
        ReachableViaCarrierDataNetwork,
        [Obsolete] ReachableViaWiFiNetwork
    }
}
