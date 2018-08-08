// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// this is just one big file to get all deprecated api together
// as there are no upsides to keep them around in scattered files
//
// about using: UNITY_IPHONE_API || UNITY_ANDROID_API
// i have no idea why we do android api here but i guess lets keep it to be on the safe side

using System;
using System.Collections;

namespace UnityEngine
{
    //
    // iPhoneSettings
    //

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("iPhoneNetworkReachability enumeration is deprecated. Please use NetworkReachability instead (UnityUpgradable) -> NetworkReachability", true)]
    public enum iPhoneNetworkReachability
    {
        NotReachable,
        ReachableViaCarrierDataNetwork,
        [Obsolete] ReachableViaWiFiNetwork
    }
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("iPhoneGeneration enumeration is deprecated. Please use iOS.DeviceGeneration instead (UnityUpgradable) -> UnityEngine.iOS.DeviceGeneration", true)]
    public enum iPhoneGeneration
    {
        Unknown,
        iPhone,
        iPhone3G,
        iPhone3GS,
        iPodTouch1Gen,
        iPodTouch2Gen,
        iPodTouch3Gen,
        iPad1Gen,
        iPhone4,
        iPodTouch4Gen,
        iPad2Gen,
        iPhone4S,
        iPad3Gen,
        iPhone5,
        iPodTouch5Gen,
        iPadMini1Gen,
        iPad4Gen,
        iPhone5C,
        iPhone5S,
        iPhoneUnknown,
        iPadUnknown,
        iPodTouchUnknown
    }

    partial class iPhoneSettings
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("screenOrientation property is deprecated. Please use Screen.orientation instead (UnityUpgradable) -> Screen.orientation", true)]
        public static iPhoneScreenOrientation screenOrientation { get { return default(iPhoneScreenOrientation); } }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("uniqueIdentifier property is deprecated. Please use SystemInfo.deviceUniqueIdentifier instead (UnityUpgradable) -> SystemInfo.deviceUniqueIdentifier", true)]
        public static string uniqueIdentifier { get { return string.Empty; } }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("name property is deprecated (UnityUpgradable). Please use SystemInfo.deviceName instead (UnityUpgradable) -> SystemInfo.deviceName", true)]
        public static string name { get { return string.Empty; } }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("model property is deprecated. Please use SystemInfo.deviceModel instead (UnityUpgradable) -> SystemInfo.deviceModel", true)]
        public static string model { get { return string.Empty; } }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("systemName property is deprecated. Please use SystemInfo.operatingSystem instead (UnityUpgradable) -> SystemInfo.operatingSystem", true)]
        public static string systemName { get { return string.Empty; } }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("internetReachability property is deprecated. Please use Application.internetReachability instead (UnityUpgradable) -> Application.internetReachability", true)]
        public static iPhoneNetworkReachability internetReachability { get { return default(iPhoneNetworkReachability); } }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("systemVersion property is deprecated. Please use iOS.Device.systemVersion instead (UnityUpgradable) -> UnityEngine.iOS.Device.systemVersion", true)]
        public static string systemVersion { get { return string.Empty; } }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("generation property is deprecated. Please use iOS.Device.generation instead (UnityUpgradable) -> UnityEngine.iOS.Device.generation", true)]
        public static iPhoneGeneration generation { get { return default(iPhoneGeneration); } }
    }

    public partial class iPhoneSettings
    {
        [Obsolete("verticalOrientation property is deprecated. Please use Screen.orientation == ScreenOrientation.Portrait instead.", false)]
        public static bool verticalOrientation { get { return false; } }

        [Obsolete("screenCanDarken property is deprecated. Please use (Screen.sleepTimeout != SleepTimeout.NeverSleep) instead.", false)]
        public static bool screenCanDarken { get { return false; } }

        [Obsolete("StartLocationServiceUpdates method is deprecated. Please use Input.location.Start instead.", false)]
        public static void StartLocationServiceUpdates(float desiredAccuracyInMeters, float updateDistanceInMeters)
        {
            Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);
        }

        [Obsolete("StartLocationServiceUpdates method is deprecated. Please use Input.location.Start instead.", false)]
        public static void StartLocationServiceUpdates(float desiredAccuracyInMeters)
        {
            Input.location.Start(desiredAccuracyInMeters);
        }

        [Obsolete("StartLocationServiceUpdates method is deprecated. Please use Input.location.Start instead.", false)]
        public static void StartLocationServiceUpdates()
        {
            Input.location.Start();
        }

        [Obsolete("StopLocationServiceUpdates method is deprecated. Please use Input.location.Stop instead.", false)]
        public static void StopLocationServiceUpdates()
        {
            Input.location.Stop();
        }

        [Obsolete("locationServiceEnabledByUser property is deprecated. Please use Input.location.isEnabledByUser instead.", false)]
        public static bool locationServiceEnabledByUser { get { return Input.location.isEnabledByUser; } }

        [Obsolete("locationServiceStatus property is deprecated. Please use Input.location.status instead.", false)]
        public static LocationServiceStatus locationServiceStatus { get { return Input.location.status; } }
    }

    //
    // iPhoneTouch
    //

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("iPhoneTouchPhase enumeration is deprecated. Please use TouchPhase instead (UnityUpgradable) -> TouchPhase", true)]
    public enum iPhoneTouchPhase
    {
        Began,
        Moved,
        Stationary,
        Ended,
        Canceled
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("iPhoneTouch struct is deprecated. Please use Touch instead (UnityUpgradable) -> Touch", true)]
    public struct iPhoneTouch
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("positionDelta property is deprecated. Please use Touch.deltaPosition instead (UnityUpgradable) -> Touch.deltaPosition", true)]
        public Vector2 positionDelta { get { return new Vector2(); } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("timeDelta property is deprecated. Please use Touch.deltaTime instead (UnityUpgradable) -> Touch.deltaTime", true)]
        public float timeDelta { get {  return default(int); } }

        public int fingerId { get { return default(int); } }
        public Vector2 position { get { return default(Vector2); } }
        public Vector2 deltaPosition { get { return default(Vector2); } }
        public float deltaTime { get {  return default(float); } }
        public int tapCount { get { return default(int); } }
        public iPhoneTouchPhase phase { get { return default(iPhoneTouchPhase); } }
    }

    //
    // iPhoneUtils
    //

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("iPhoneMovieControlMode enumeration is deprecated. Please use FullScreenMovieControlMode instead (UnityUpgradable) -> FullScreenMovieControlMode", true)]
    public enum iPhoneMovieControlMode
    {
        Full,
        Minimal,
        [Obsolete] CancelOnTouch,
        Hidden,
        [Obsolete] VolumeOnly
    }
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("iPhoneMovieScalingMode enumeration is deprecated. Please use FullScreenMovieScalingMode instead  (UnityUpgradable) -> FullScreenMovieScalingMode", true)]
    public enum iPhoneMovieScalingMode
    {
        None,
        AspectFit,
        AspectFill,
        Fill
    }

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

    //
    // iPhoneKeyboard
    //

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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

    //
    // iPhoneInput
    //

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("iPhoneAccelerationEvent struct is deprecated. Please use AccelerationEvent instead (UnityUpgradable) -> AccelerationEvent", true)]
    public struct iPhoneAccelerationEvent
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("timeDelta property is deprecated. Please use AccelerationEvent.deltaTime instead (UnityUpgradable) -> AccelerationEvent.deltaTime", true)]
        public float timeDelta { get { return 0; } }

        public Vector3 acceleration { get { return default(Vector3); } }
        public float deltaTime { get { return -1.0f; } }
    }
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("iPhoneInput class is deprecated. Please use Input instead (UnityUpgradable) -> Input", true)]
    public class iPhoneInput
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("orientation property is deprecated. Please use Input.deviceOrientation instead (UnityUpgradable) -> Input.deviceOrientation", true)]
        public static iPhoneOrientation orientation { get { return default(iPhoneOrientation); } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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

    //
    // iPhone
    //


    //
    // LocalNotification/RemoteNotification/NotificationServices
    //


    //
    // iAD
    //

}

namespace UnityEngine.iOS
{
    //
    // iAD
    //

}
