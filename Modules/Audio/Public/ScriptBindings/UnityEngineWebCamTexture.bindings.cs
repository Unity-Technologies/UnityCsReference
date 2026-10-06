// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{

    //*undocumented*
    ///<exclude />
    public enum WebCamFlags
    {
        // Camera faces the same direction as screen
        FrontFacing = 1,
        // Camera supports arbitrary focus point
        AutoFocusPointSupported = 2,
    }

    ///<summary>Enum representing the different types of web camera device.</summary>
    ///<remarks>On iOS devices, the <see cref="WebCamDevice.kind" /> is reported directly by the hardware.
    ///On Android devices, the hardware does not report this value, so Unity determines the <see cref="WebCamDevice.kind" /> by calculating the &lt;a href="https://en.wikipedia.org/wiki/35_mm_equivalent_focal_length"&gt;Equivalent Focal Length&lt;/a&gt; from a calculation based on the reported focal length and matrix size. Therefore, on some Android devices, the default camera may be detected as <see cref="WebCamKind.UltraWideAngle" /> or <see cref="WebCamKind.Telephoto" />.
    ///As there is currently no web API that returns the focal length of a webcam device, the WebGL applications always return <see cref="WebCamDevice.kind" /> as WideAngle.</remarks>
    ///<seealso cref="WebCamDevice.kind" />
    public enum WebCamKind
    {
        ///<summary>The camera type is unknown.</summary>
        ///<remarks>The type of camera is not recognized or its properties cannot be determined.</remarks>
        Unknown = 0,
        ///<summary>Wide angle (default) camera.</summary>
        ///<remarks>On iOS devices, this value is reported directly by the hardware.
        ///
        ///On Android devices, this value is reported for cameras with a calculated Equivalent Focal Length from 20mm to 45mm.</remarks>
        ///<seealso cref="WebCamDevice.kind" />
        WideAngle = 1,
        ///<summary>A Telephoto camera device. These devices have a longer focal length than a wide-angle camera.</summary>
        ///<remarks>On iOS devices, this value is reported directly by the hardware.
        ///
        ///On Android devices, this value is reported for cameras with a calculated Equivalent Focal Length of greater than 45mm.</remarks>
        ///<seealso cref="WebCamDevice.kind" />
        Telephoto = 2,
        // Camera which supports synchronized color and depth data (Dual or TrueDepth)
        ///<summary>Camera which supports synchronized color and depth data (currently these are only dual back and true depth cameras on latest iOS devices).</summary>
        ///<seealso cref="WebCamDevice.kind" />
        ///<seealso cref="WebCamDevice.depthCameraName" />
        ///<seealso cref="WebCamTexture.isDepth" />
        ColorAndDepth = 3,
        ///<summary>Ultra wide angle camera. These devices have a shorter focal length than a wide-angle camera.</summary>
        ///<remarks>On iOS devices, this value is reported directly by the hardware.
        ///
        ///On Android devices, this value is reported for cameras with a calculated Equivalent Focal Length of less than 20mm.</remarks>
        ///<seealso cref="WebCamDevice.kind" />
        UltraWideAngle = 4,
    }

    ///<summary>A structure describing the webcam device.</summary>
    [UsedByNativeCode]
    public struct WebCamDevice
    {
        ///<summary>A human-readable name of the device. Varies across different systems.</summary>
        public string name { get { return m_Name; } }

        ///<summary>True if camera faces the same direction a screen does, false otherwise.</summary>
        public bool isFrontFacing { get { return (m_Flags & ((int)WebCamFlags.FrontFacing)) != 0; } }

        ///<summary>Property of type <see cref="WebCamKind" /> denoting the kind of webcam device.</summary>
        ///<seealso cref="WebCamKind" />
        public WebCamKind kind { get { return m_Kind; } }

        ///<summary>A string identifier used to create a depth data based WebCamTexture.</summary>
        ///<remarks>
        ///  <para>This string is null for webcam devices that do not support depth data. For webcams with depth data support (those of kind <see cref="WebCamKind.ColorAndDepth" />) this string is not empty. Currently, only dual back and true depth cameras on latest iOS devices support depth data.</para>
        ///  <para />
        ///</remarks>
        ///<seealso cref="WebCamDevice.kind" />
        ///<seealso cref="M:UnityEngine.WebCamTexture.#ctor" />
        public string depthCameraName { get { return m_DepthCameraName == "" ? null : m_DepthCameraName; } }

        ///<summary>Returns true if the camera supports automatic focusing on points of interest and false otherwise.</summary>
        ///<seealso cref="WebCamTexture.autoFocusPoint" />
        public bool isAutoFocusPointSupported { get { return (m_Flags & ((int)WebCamFlags.AutoFocusPointSupported)) != 0; } }

        ///<summary>Possible <see cref="WebCamTexture" /> resolutions for this device.</summary>
        ///<remarks>
        ///  <para>Available on **iOS** and **Android** only. Returns null for other platforms.
        ///
        ///**Note:** For webcams with depth data support (those of kind <see cref="WebCamKind.ColorAndDepth" />) this array consists of one element only - resolution of the depth data based texture.</para>
        ///  <para />
        ///</remarks>
        ///<seealso cref="Resolution" />
        ///<seealso cref="M:UnityEngine.WebCamTexture.#ctor" />
        public Resolution[] availableResolutions { get { return m_Resolutions; } }

        [NativeName("name")]
        internal string m_Name;

        [NativeName("depthCameraName")]
        internal string m_DepthCameraName;

        [NativeName("flags")]
        internal int m_Flags;

        [NativeName("kind")]
        internal WebCamKind m_Kind;

        [NativeName("resolutions")]
        internal Resolution[] m_Resolutions;

        [RequiredByNativeCode]
        private static void ReconstructArrayElementRaw(WebCamDevice[] array, int i, object name, object depthCameraName, int flags, WebCamKind kind, Resolution[] resolutions)
        {
            array[i] = new WebCamDevice 
            {
                m_Name = (string)name,
                m_DepthCameraName = (string)depthCameraName,
                m_Flags = flags,
                m_Kind = kind,
                m_Resolutions = resolutions 
            };
        }
    }

    ///<summary>WebCam Textures are textures onto which the live video input is rendered.</summary>
    ///<remarks>On Android, iOS, and WebGL platforms, <c>WebCamTexture</c> requires the camera permission.
    ///On Android, you can request it at runtime using the <see cref="T:UnityEngine.Android.Permission" /> API. For more information, refer to [Request runtime permissions](xref:android-RequestingPermissions) documentation.
    ///
    ///On iOS and WebGL, you can request camera permission at runtime using <see cref="Application.RequestUserAuthorization" />.
    ///
    ///**Note**: On Android and iOS platforms, Unity doesn't support multiple WebCamTextures simultaneously.
    ///
    ///The following code example demonstrates how to request the user for camera permission on iOS, Android, and WebGL platforms.</remarks>
    ///<example>
    ///  <code><![CDATA[using UnityEngine;
    ///using System;
    ///using System.Collections;
    ///using UnityEngine;
    ///#if UNITY_ANDROID
    ///using UnityEngine.Android;
    ///#endif
    ///
    ///public class WebCam : MonoBehaviour
    ///{
    ///#if UNITY_IOS || UNITY_WEBGL
    ///    private bool CheckPermissionAndRaiseCallbackIfGranted(UserAuthorization authenticationType, Action authenticationGrantedAction)
    ///    {
    ///        if (Application.HasUserAuthorization(authenticationType))
    ///        {
    ///            if (authenticationGrantedAction != null)
    ///                authenticationGrantedAction();
    ///
    ///            return true;
    ///        }
    ///        return false;
    ///    }
    ///
    ///    private IEnumerator AskForPermissionIfRequired(UserAuthorization authenticationType, Action authenticationGrantedAction)
    ///    {
    ///        if (!CheckPermissionAndRaiseCallbackIfGranted(authenticationType, authenticationGrantedAction))
    ///        {
    ///            yield return Application.RequestUserAuthorization(authenticationType);
    ///            if (!CheckPermissionAndRaiseCallbackIfGranted(authenticationType, authenticationGrantedAction))
    ///                Debug.LogWarning($"Permission {authenticationType} Denied");
    ///        }
    ///    }
    ///#elif UNITY_ANDROID
    ///    private void PermissionCallbacksPermissionGranted(string permissionName)
    ///    {
    ///        StartCoroutine(DelayedCameraInitialization());
    ///    }
    ///
    ///    private IEnumerator DelayedCameraInitialization()
    ///    {
    ///        yield return null;
    ///        InitializeCamera();
    ///    }
    ///
    ///    private void PermissionCallbacksPermissionDenied(string permissionName)
    ///    {
    ///        Debug.LogWarning($"Permission {permissionName} Denied");
    ///    }
    ///
    ///    private void AskCameraPermission()
    ///    {
    ///        var callbacks = new PermissionCallbacks();
    ///        callbacks.PermissionDenied += PermissionCallbacksPermissionDenied;
    ///        callbacks.PermissionGranted += PermissionCallbacksPermissionGranted;
    ///        Permission.RequestUserPermission(Permission.Camera, callbacks);
    ///    }
    ///#endif
    ///
    ///    void Start()
    ///    {
    ///#if UNITY_IOS || UNITY_WEBGL
    ///        StartCoroutine(AskForPermissionIfRequired(UserAuthorization.WebCam, () => { InitializeCamera(); }));
    ///        return;
    ///#elif UNITY_ANDROID
    ///        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
    ///        {
    ///            AskCameraPermission();
    ///            return;
    ///        }
    ///#endif
    ///        InitializeCamera();
    ///    }
    ///
    ///    private void InitializeCamera()
    ///    {
    ///        WebCamTexture webcamTexture = new WebCamTexture();
    ///        Renderer renderer = GetComponent<Renderer>();
    ///        renderer.material.mainTexture = webcamTexture;
    ///        webcamTexture.Play();
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [NativeHeader("Runtime/Video/BaseWebCamTexture.h")]
    [NativeHeader("Runtime/Video/ScriptBindings/WebCamTexture.bindings.h")]
    [global::UnityEngine.NativeClass("WebCamTexture", PersistentTypeId = 158)]
    [NativeHeader("AudioScriptingClasses.h")]
    public sealed class WebCamTexture : Texture
    {
        internal WebCamTexture(global::UnityEngine.EntityId id) : base(id) {}
        ///<summary>Return a list of available devices.</summary>
        ///<remarks>This queries the system for the list of devices connected and it can be slow.
        ///You should cache this value by keeping a copy of the result if you want to use it repeatedly.
        ///
        ///**Note:** On devices running Android 10 and newer versions, this list includes both &lt;a href="https://developer.android.com/media/camera/camera2/multi-camera#logical"&gt;logical and physical&lt;/a&gt; cameras, if the device exposes them. On Android 9, this list includes only logical cameras because requesting characteristics for physical cameras isn't supported.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // Gets the list of devices and prints them to the console.
        ///    void Start()
        ///    {
        ///        WebCamDevice[] devices = WebCamTexture.devices;
        ///        for (int i = 0; i < devices.Length; i++)
        ///            Debug.Log(devices[i].name);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern static WebCamDevice[] devices
        {
            [StaticAccessor("WebCamTextureBindings", StaticAccessorType.DoubleColon)]
            [NativeName("Internal_GetDevices")]
            [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
            get;
        }

        ///<summary>Create a WebCamTexture.</summary>
        ///<remarks>Use <see cref="WebCamTexture.devices" /> to get a list of the names of available camera devices. If no device name is supplied to the constructor or is passed as a null string, the first device found will be used.
        ///
        ///The requested width, height and framerate specified by the parameters may not be supported by the chosen camera. In such cases, the closest available values will be used.
        ///
        ///Call <see cref="Application.RequestUserAuthorization" /> before creating a
        ///WebCamTexture.
        ///
        ///**Note:** If you want to use a WebCamTexture to play the camera stream from a device connected through Unity Remote, then you must initialize it through use of the constructor. It is not possible to change the device later using <see cref="WebCamTexture.deviceName" /> from a regular device to a remote device and vice versa.
        ///
        ///**Note:** For camera devices of kind <see cref="WebCamKind.ColorAndDepth" /> (currently these are only dual back and true depth cameras on latest iOS devices), it is possible to create a WebCamTexture instance to receive depth data using <see cref="WebCamDevice.depthCameraName" /> as the deviceName. This WebCamTexture always contains one channel and is in half-precision floating point format with distance values in meters.
        ///
        ///If required, it is also possible to create a second WebCamTexture instance using <see cref="WebCamDevice.name" /> as deviceName to receive color data. In this case, both color and depth data will be synchronized.
        ///
        ///Currently, iOS supports only limited combinations of color/depth data resolutions. **requestedWidth** and **requestedHeight** parameters are ignored, when creating WebCamTexture instances for ColorAndDepth devices. For iPhone 7+/8+ dual back cameras, the size of the WebCamTexture for color data is 1440x1080 and for iPhone X dual back and front true depth cameras, it is 1500x1126. The depth data resolution is always a maximum of 320x240 for iPhone 4+/8+/X dual back cameras and 640x480 for iPhone X front true depth cameras.</remarks>
        ///<param name="deviceName">The name of the video input device to be used.</param>
        ///<param name="requestedWidth">The requested width of the texture.</param>
        ///<param name="requestedHeight">The requested height of the texture.</param>
        ///<param name="requestedFPS">The requested frame rate of the texture.</param>
        public WebCamTexture(string deviceName, int requestedWidth, int requestedHeight, int requestedFPS)
        {
            SetEntityIdFromConstructor(Internal_CreateWebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFPS));
        }

        ///<summary>Create a WebCamTexture.</summary>
        ///<remarks>Use <see cref="WebCamTexture.devices" /> to get a list of the names of available camera devices. If no device name is supplied to the constructor or is passed as a null string, the first device found will be used.
        ///
        ///The requested width, height and framerate specified by the parameters may not be supported by the chosen camera. In such cases, the closest available values will be used.
        ///
        ///Call <see cref="Application.RequestUserAuthorization" /> before creating a
        ///WebCamTexture.
        ///
        ///**Note:** If you want to use a WebCamTexture to play the camera stream from a device connected through Unity Remote, then you must initialize it through use of the constructor. It is not possible to change the device later using <see cref="WebCamTexture.deviceName" /> from a regular device to a remote device and vice versa.
        ///
        ///**Note:** For camera devices of kind <see cref="WebCamKind.ColorAndDepth" /> (currently these are only dual back and true depth cameras on latest iOS devices), it is possible to create a WebCamTexture instance to receive depth data using <see cref="WebCamDevice.depthCameraName" /> as the deviceName. This WebCamTexture always contains one channel and is in half-precision floating point format with distance values in meters.
        ///
        ///If required, it is also possible to create a second WebCamTexture instance using <see cref="WebCamDevice.name" /> as deviceName to receive color data. In this case, both color and depth data will be synchronized.
        ///
        ///Currently, iOS supports only limited combinations of color/depth data resolutions. **requestedWidth** and **requestedHeight** parameters are ignored, when creating WebCamTexture instances for ColorAndDepth devices. For iPhone 7+/8+ dual back cameras, the size of the WebCamTexture for color data is 1440x1080 and for iPhone X dual back and front true depth cameras, it is 1500x1126. The depth data resolution is always a maximum of 320x240 for iPhone 4+/8+/X dual back cameras and 640x480 for iPhone X front true depth cameras.</remarks>
        ///<param name="deviceName">The name of the video input device to be used.</param>
        ///<param name="requestedWidth">The requested width of the texture.</param>
        ///<param name="requestedHeight">The requested height of the texture.</param>
        public WebCamTexture(string deviceName, int requestedWidth, int requestedHeight)
        {
            SetEntityIdFromConstructor(Internal_CreateWebCamTexture(deviceName, requestedWidth, requestedHeight, 0));
        }

        ///<summary>Create a WebCamTexture.</summary>
        ///<remarks>Use <see cref="WebCamTexture.devices" /> to get a list of the names of available camera devices. If no device name is supplied to the constructor or is passed as a null string, the first device found will be used.
        ///
        ///The requested width, height and framerate specified by the parameters may not be supported by the chosen camera. In such cases, the closest available values will be used.
        ///
        ///Call <see cref="Application.RequestUserAuthorization" /> before creating a
        ///WebCamTexture.
        ///
        ///**Note:** If you want to use a WebCamTexture to play the camera stream from a device connected through Unity Remote, then you must initialize it through use of the constructor. It is not possible to change the device later using <see cref="WebCamTexture.deviceName" /> from a regular device to a remote device and vice versa.
        ///
        ///**Note:** For camera devices of kind <see cref="WebCamKind.ColorAndDepth" /> (currently these are only dual back and true depth cameras on latest iOS devices), it is possible to create a WebCamTexture instance to receive depth data using <see cref="WebCamDevice.depthCameraName" /> as the deviceName. This WebCamTexture always contains one channel and is in half-precision floating point format with distance values in meters.
        ///
        ///If required, it is also possible to create a second WebCamTexture instance using <see cref="WebCamDevice.name" /> as deviceName to receive color data. In this case, both color and depth data will be synchronized.
        ///
        ///Currently, iOS supports only limited combinations of color/depth data resolutions. **requestedWidth** and **requestedHeight** parameters are ignored, when creating WebCamTexture instances for ColorAndDepth devices. For iPhone 7+/8+ dual back cameras, the size of the WebCamTexture for color data is 1440x1080 and for iPhone X dual back and front true depth cameras, it is 1500x1126. The depth data resolution is always a maximum of 320x240 for iPhone 4+/8+/X dual back cameras and 640x480 for iPhone X front true depth cameras.</remarks>
        ///<param name="deviceName">The name of the video input device to be used.</param>
        public WebCamTexture(string deviceName)
        {
            SetEntityIdFromConstructor(Internal_CreateWebCamTexture(deviceName, 0, 0, 0));
        }

        ///<summary>Create a WebCamTexture.</summary>
        ///<remarks>Use <see cref="WebCamTexture.devices" /> to get a list of the names of available camera devices. If no device name is supplied to the constructor or is passed as a null string, the first device found will be used.
        ///
        ///The requested width, height and framerate specified by the parameters may not be supported by the chosen camera. In such cases, the closest available values will be used.
        ///
        ///Call <see cref="Application.RequestUserAuthorization" /> before creating a
        ///WebCamTexture.
        ///
        ///**Note:** If you want to use a WebCamTexture to play the camera stream from a device connected through Unity Remote, then you must initialize it through use of the constructor. It is not possible to change the device later using <see cref="WebCamTexture.deviceName" /> from a regular device to a remote device and vice versa.
        ///
        ///**Note:** For camera devices of kind <see cref="WebCamKind.ColorAndDepth" /> (currently these are only dual back and true depth cameras on latest iOS devices), it is possible to create a WebCamTexture instance to receive depth data using <see cref="WebCamDevice.depthCameraName" /> as the deviceName. This WebCamTexture always contains one channel and is in half-precision floating point format with distance values in meters.
        ///
        ///If required, it is also possible to create a second WebCamTexture instance using <see cref="WebCamDevice.name" /> as deviceName to receive color data. In this case, both color and depth data will be synchronized.
        ///
        ///Currently, iOS supports only limited combinations of color/depth data resolutions. **requestedWidth** and **requestedHeight** parameters are ignored, when creating WebCamTexture instances for ColorAndDepth devices. For iPhone 7+/8+ dual back cameras, the size of the WebCamTexture for color data is 1440x1080 and for iPhone X dual back and front true depth cameras, it is 1500x1126. The depth data resolution is always a maximum of 320x240 for iPhone 4+/8+/X dual back cameras and 640x480 for iPhone X front true depth cameras.</remarks>
        ///<param name="requestedWidth">The requested width of the texture.</param>
        ///<param name="requestedHeight">The requested height of the texture.</param>
        ///<param name="requestedFPS">The requested frame rate of the texture.</param>
        public WebCamTexture(int requestedWidth, int requestedHeight, int requestedFPS)
        {
            SetEntityIdFromConstructor(Internal_CreateWebCamTexture("", requestedWidth, requestedHeight, requestedFPS));
        }

        ///<summary>Create a WebCamTexture.</summary>
        ///<remarks>Use <see cref="WebCamTexture.devices" /> to get a list of the names of available camera devices. If no device name is supplied to the constructor or is passed as a null string, the first device found will be used.
        ///
        ///The requested width, height and framerate specified by the parameters may not be supported by the chosen camera. In such cases, the closest available values will be used.
        ///
        ///Call <see cref="Application.RequestUserAuthorization" /> before creating a
        ///WebCamTexture.
        ///
        ///**Note:** If you want to use a WebCamTexture to play the camera stream from a device connected through Unity Remote, then you must initialize it through use of the constructor. It is not possible to change the device later using <see cref="WebCamTexture.deviceName" /> from a regular device to a remote device and vice versa.
        ///
        ///**Note:** For camera devices of kind <see cref="WebCamKind.ColorAndDepth" /> (currently these are only dual back and true depth cameras on latest iOS devices), it is possible to create a WebCamTexture instance to receive depth data using <see cref="WebCamDevice.depthCameraName" /> as the deviceName. This WebCamTexture always contains one channel and is in half-precision floating point format with distance values in meters.
        ///
        ///If required, it is also possible to create a second WebCamTexture instance using <see cref="WebCamDevice.name" /> as deviceName to receive color data. In this case, both color and depth data will be synchronized.
        ///
        ///Currently, iOS supports only limited combinations of color/depth data resolutions. **requestedWidth** and **requestedHeight** parameters are ignored, when creating WebCamTexture instances for ColorAndDepth devices. For iPhone 7+/8+ dual back cameras, the size of the WebCamTexture for color data is 1440x1080 and for iPhone X dual back and front true depth cameras, it is 1500x1126. The depth data resolution is always a maximum of 320x240 for iPhone 4+/8+/X dual back cameras and 640x480 for iPhone X front true depth cameras.</remarks>
        ///<param name="requestedWidth">The requested width of the texture.</param>
        ///<param name="requestedHeight">The requested height of the texture.</param>
        public WebCamTexture(int requestedWidth, int requestedHeight)
        {
            SetEntityIdFromConstructor(Internal_CreateWebCamTexture("", requestedWidth, requestedHeight, 0));
        }

        ///<summary>Create a WebCamTexture.</summary>
        ///<remarks>Use <see cref="WebCamTexture.devices" /> to get a list of the names of available camera devices. If no device name is supplied to the constructor or is passed as a null string, the first device found will be used.
        ///
        ///The requested width, height and framerate specified by the parameters may not be supported by the chosen camera. In such cases, the closest available values will be used.
        ///
        ///Call <see cref="Application.RequestUserAuthorization" /> before creating a
        ///WebCamTexture.
        ///
        ///**Note:** If you want to use a WebCamTexture to play the camera stream from a device connected through Unity Remote, then you must initialize it through use of the constructor. It is not possible to change the device later using <see cref="WebCamTexture.deviceName" /> from a regular device to a remote device and vice versa.
        ///
        ///**Note:** For camera devices of kind <see cref="WebCamKind.ColorAndDepth" /> (currently these are only dual back and true depth cameras on latest iOS devices), it is possible to create a WebCamTexture instance to receive depth data using <see cref="WebCamDevice.depthCameraName" /> as the deviceName. This WebCamTexture always contains one channel and is in half-precision floating point format with distance values in meters.
        ///
        ///If required, it is also possible to create a second WebCamTexture instance using <see cref="WebCamDevice.name" /> as deviceName to receive color data. In this case, both color and depth data will be synchronized.
        ///
        ///Currently, iOS supports only limited combinations of color/depth data resolutions. **requestedWidth** and **requestedHeight** parameters are ignored, when creating WebCamTexture instances for ColorAndDepth devices. For iPhone 7+/8+ dual back cameras, the size of the WebCamTexture for color data is 1440x1080 and for iPhone X dual back and front true depth cameras, it is 1500x1126. The depth data resolution is always a maximum of 320x240 for iPhone 4+/8+/X dual back cameras and 640x480 for iPhone X front true depth cameras.</remarks>
        public WebCamTexture()
        {
            SetEntityIdFromConstructor(Internal_CreateWebCamTexture("", 0, 0, 0));
        }

        ///<summary>Starts the camera.</summary>
        ///<remarks>Call <see cref="Application.RequestUserAuthorization" /> before creating a
        ///WebCamTexture.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Starts the default camera and assigns the texture to the current renderer
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        WebCamTexture webcamTexture = new WebCamTexture();
        ///        Renderer renderer = GetComponent<Renderer>();
        ///        renderer.material.mainTexture = webcamTexture;
        ///        webcamTexture.Play();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern void Play();
        ///<summary>Pauses the camera.</summary>
        ///<remarks>Call <see cref="Application.RequestUserAuthorization" /> before creating a
        ///WebCamTexture.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Starts a camera and assigns the texture to the current renderer.
        /// // Pauses the camera when the "Pause" button is clicked and released.
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public WebCamTexture webcamTexture;
        ///
        ///    void Start()
        ///    {
        ///        webcamTexture = new WebCamTexture();
        ///        Renderer renderer = GetComponent<Renderer>();
        ///        renderer.material.mainTexture = webcamTexture;
        ///        webcamTexture.Play();
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        if (webcamTexture.isPlaying)
        ///            if (GUILayout.Button("Pause"))
        ///                webcamTexture.Pause();
        ///
        ///            else if (GUILayout.Button("Play"))
        ///                webcamTexture.Play();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern void Pause();
        ///<summary>Stops the camera.</summary>
        ///<remarks>Call <see cref="Application.RequestUserAuthorization" /> before creating a
        ///WebCamTexture.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Starts a camera and assigns the texture to the current renderer.
        /// // Stops the camera when the "Stop" button is clicked and released.
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public WebCamTexture webcamTexture;
        ///
        ///    void Start()
        ///    {
        ///        webcamTexture = new WebCamTexture();
        ///        webcamTexture.Play();
        ///
        ///        Renderer renderer = GetComponent<Renderer>();
        ///
        ///        if (webcamTexture.isPlaying)
        ///            renderer.material.mainTexture = webcamTexture;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        if (webcamTexture.isPlaying)
        ///            if (GUILayout.Button("Stop"))
        ///                webcamTexture.Stop();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern void Stop();

        ///<summary>Returns if the camera is currently playing.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Tries to start the camera and outputs to the console if is was sucessful or not.
        ///    void Start()
        ///    {
        ///        WebCamTexture webcamTexture = new WebCamTexture();
        ///        webcamTexture.Play();
        ///        Debug.Log(webcamTexture.isPlaying);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern bool isPlaying
        {
            [NativeName("IsPlaying")]
            get;
        }

        ///<summary>Set this to specify the name of the device to use.</summary>
        ///<remarks>This only has an effect when set while the camera is not running.
        ///
        ///**Note:** If you want to use WebCamTexture to get the camera stream from a device connected through Unity Remote, you must initialize it through the constructor. It's not possible to change the device using <see cref="WebCamTexture.deviceName" /> from regular to remote devices and vice versa.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Sets the device of the WebCamTexture to the first one available and starts playing it
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        WebCamDevice[] devices = WebCamTexture.devices;
        ///        WebCamTexture webcamTexture = new WebCamTexture();
        ///
        ///        if (devices.Length > 0)
        ///        {
        ///            webcamTexture.deviceName = devices[0].name;
        ///            webcamTexture.Play();
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeName("Device")]
        public extern string deviceName { get; set; }

        ///<summary>Set the requested frame rate of the camera device (in frames per second).</summary>
        ///<remarks>It will use a closest frame rate to the one requested which is supported by the camera.
        ///The requested values only have an effect when set while the camera is not running.</remarks>
        public extern float requestedFPS { get; set; }
        ///<summary>Set the requested width of the camera device.</summary>
        ///<remarks>It will use a closest resolution to the one requested which is supported by the camera.
        ///The requested values only have an effect when set while the camera is not running.</remarks>
        public extern int requestedWidth { get; set; }
        ///<summary>Set the requested height of the camera device.</summary>
        ///<remarks>It will use a closest resolution to the one requested which is supported by the camera.
        ///The requested values only have an effect when set while the camera is not running.</remarks>
        public extern int requestedHeight { get; set; }

        ///<summary>Returns an clockwise angle (in degrees), which can be used to rotate a polygon so camera contents are shown in correct orientation.</summary>
        ///<remarks>Call <see cref="Application.RequestUserAuthorization" /> before creating a
        ///WebCamTexture.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Starts a camera and assigns the texture to the current renderer.
        /// // Updates polygon's orientation according to camera's given angle.
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public WebCamTexture webcamTexture;
        ///    public Quaternion baseRotation;
        ///    void Start()
        ///    {
        ///        webcamTexture = new WebCamTexture();
        ///        Renderer renderer = GetComponent<Renderer>();
        ///        renderer.material.mainTexture = webcamTexture;
        ///        baseRotation = transform.rotation;
        ///        webcamTexture.Play();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        transform.rotation = baseRotation * Quaternion.AngleAxis(webcamTexture.videoRotationAngle, Vector3.up);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern int videoRotationAngle { get; }
        ///<summary>Returns if the texture image is vertically flipped.</summary>
        ///<remarks>Please note, that this will query platform-specific part, which might be not ready before actual video feed started; so it is not enough to call it once after play.</remarks>
        public extern bool videoVerticallyMirrored
        {
            [NativeName("IsVideoVerticallyMirrored")]
            get;
        }
        ///<summary>Did the video buffer update this frame?</summary>
        ///<remarks>Use this to check if the video buffer has changed since the last frame. When setting a low frame rate,
        ///it is likely that the video will update slower than the game. Since it would not make sense to do expensive video
        ///processing in each Update call, check this value before doing any processing.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    WebCamTexture webcamTexture;
        ///    Color32[] data;
        ///
        ///    void Start()
        ///    {
        ///        // Start web cam feed
        ///        webcamTexture =  new WebCamTexture();
        ///        webcamTexture.Play();
        ///        data = new Color32[webcamTexture.width * webcamTexture.height];
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (webcamTexture.didUpdateThisFrame)
        ///        {
        ///            webcamTexture.GetPixels32(data);
        ///            // Do processing of data here.
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern bool didUpdateThisFrame
        {
            [NativeName("DidUpdateThisFrame")]
            get;
        }

        ///<summary>Gets the pixel color at coordinates (<c>x</c>, <c>y</c>).</summary>
        ///<remarks>This method gets pixel data from the texture in CPU memory. <see cref="Texture.isReadable" /> must be <c>true</c>.
        ///
        ///The lower left corner is (0, 0). If the pixel coordinate is outside the texture's dimensions, Unity clamps or repeats it, depending on the texture's <see cref="TextureWrapMode" />.
        ///
        ///If you need to get a large block of pixels, it might be faster to use <see cref="GetPixels" />.
        ///
        ///**Note:** For depth data based WebCamTexture instances, the depth value (distance in meters) can be accessed using the <see cref="Color.r" /> property. .</remarks>
        ///<param name="x">The x coordinate of the pixel to get. The range is <c>0</c> through the (texture width - 1).</param>
        ///<param name="y">The y coordinate of the pixel to get. The range is <c>0</c> through the (texture height - 1).</param>
        ///<returns>The pixel color.</returns>
        ///<seealso cref="WebCamTexture.isDepth" />
        ///<seealso cref="GetPixels32" />
        ///<seealso cref="GetPixels" />
        [FreeFunction("WebCamTextureBindings::Internal_GetPixel", HasExplicitThis = true)]
        public extern Color GetPixel(int x, int y);

        ///<summary>Gets the pixel color data for a mipmap level as <see cref="Color" /> structs.</summary>
        ///<remarks>This method gets pixel data from the texture in CPU memory. <see cref="Texture.isReadable" /> must be <c>true</c>.
        ///
        ///The array contains the pixels row by row, starting at the bottom left of the texture. The size of the array is the width × height of the texture. 
        ///
        ///Each pixel is a <see cref="Color" /> struct.
        ///
        ///A single call to <c>GetPixels</c> is usually faster than multiple calls to <see cref="GetPixel" />, especially for large textures. If a lower-precision representation is acceptable, <see cref="GetPixels32" /> is faster and uses less memory because it does not perform integer-to-float conversions.
        ///
        ///If <c>GetPixels</c> fails, Unity throws an exception. <c>GetPixels</c> might fail if the array contains too much data.
        ///
        ///**Note:** For depth data based WebCamTexture instances, this method returns an array of the depth values via <see cref="Color.r" /> property. .</remarks>
        ///<returns>An array that contains the pixel colors.</returns>
        ///<seealso cref="WebCamTexture.isDepth" />
        public Color[] GetPixels()
        {
            return GetPixels(0, 0, width, height);
        }

        ///<summary>Gets the pixel color data for part of the texture as <see cref="Color" /> structs.</summary>
        ///<remarks>This version of <c>GetPixels</c> returns part of the texture instead of the whole texture.
        ///
        ///**Note:** For depth data based WebCamTexture instances, this method returns an array of the depth values via <see cref="Color.r" /> property. .</remarks>
        ///<param name="x">The starting x position of the section to fetch.</param>
        ///<param name="y">The starting y position of the section to fetch.</param>
        ///<param name="blockWidth">The width of the section to fetch.</param>
        ///<param name="blockHeight">The height of the section to fetch.</param>
        ///<returns>An array that contains the pixel colors.</returns>
        ///<seealso cref="WebCamTexture.isDepth" />
        [FreeFunction("WebCamTextureBindings::Internal_GetPixels", HasExplicitThis = true, ThrowsException = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public extern Color[] GetPixels(int x, int y, int blockWidth, int blockHeight);

        [UnityEngine.Internal.ExcludeFromDocs]
        public Color32[] GetPixels32()
        {
            return GetPixels32(null);
        }

        ///<summary>Gets the pixel color data for a mipmap level as <see cref="Color32" /> structs.</summary>
        ///<remarks>This method gets pixel data from the texture in CPU memory. <see cref="Texture.isReadable" /> must be <c>true</c>.
        ///
        ///The array contains the pixels row by row, starting at the bottom left of the texture. The size of the array is the width × height of the texture. 
        ///
        ///Each pixel is a <see cref="Color32" /> struct.
        ///
        ///A single call to <c>GetPixels32</c> is usually faster than multiple calls to <see cref="GetPixel" />, especially for large textures.
        ///
        ///If <c>GetPixels32</c> fails, Unity throws an exception. <c>GetPixels32</c> might fail if the array contains too much data.
        ///
        ///You can optionally pass in an array of <see cref="Color32" /> structs to avoid allocating new memory each frame. This can improve performance if you are continuously reading data from the camera. The array must be initialized to the dimensions <c>width * height</c> of the texture. If you don't pass an array, Unity will allocate one and return it.</remarks>
        ///<param name="colors">An optional array to write the pixel data to.</param>
        ///<returns>An array that contains the pixel colors.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    WebCamTexture webcamTexture;
        ///    Color32[] data;
        ///
        ///    void Start()
        ///    {
        ///        // Start web cam feed
        ///        webcamTexture = new WebCamTexture();
        ///        webcamTexture.Play();
        ///        data = new Color32[webcamTexture.width * webcamTexture.height];
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        webcamTexture.GetPixels32(data);
        ///        // Do processing of data here.
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [FreeFunction("WebCamTextureBindings::Internal_GetPixels32", HasExplicitThis = true, ThrowsException = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public extern Color32[] GetPixels32([UnityEngine.Internal.DefaultValue("null"), UnityMarshalAs(NativeType.ScriptingObjectPtr)] Color32[] colors);

        ///<summary>This property allows you to set/get the auto focus point of the camera. This works only on **Android** and **iOS** devices.</summary>
        ///<remarks>
        ///  <see cref="Vector2.x" /> and <see cref="Vector2.y" /> components are relative values in the range 0..1 with the origin (0, 0) positioned at the bottom left corner of the texture.
        ///This property can be set when the current texture is playing (after <see cref="WebCamTexture.Play" /> method has been called). After a new value has been set, the device camera automatically refocuses using the new auto focus point.
        ///After refocusing, the camera focus is then locked. In order to disable use of the focus point and switch back to continuous auto-focus mode, the autoFocusPoint property should
        ///be set to **null**. If this feature is not supported by the camera device or if it is currently not possible to focus (for example because the previous focus attempt has not yet finished)
        ///then the previous value for the focus point setting is not changed. Setting this property to a value where either x or y is outside of the range 0..1 causes the focus point to be reset to null
        ///and the camera to be switched back to continuous auto-focus mode.
        ///
        ///**Note:** this feature may not be supported by front-facing camera devices.</remarks>
        ///<seealso cref="WebCamDevice.isAutoFocusPointSupported" />
        public Vector2? autoFocusPoint
        {
            get { return internalAutoFocusPoint.x < 0 ? null : new Vector2 ? (internalAutoFocusPoint); }
            set { internalAutoFocusPoint = (value == null) ? new Vector2(-1, -1) : value.Value; }
        }
        internal extern Vector2 internalAutoFocusPoint { get; set; }

        ///<summary>This property is true if the texture is based on depth data.</summary>
        ///<seealso cref="WebCamDevice.depthCameraName" />
        ///<seealso cref="M:UnityEngine.WebCamTexture.#ctor" />
        public extern bool isDepth { get; }

        [StaticAccessor("WebCamTextureBindings", StaticAccessorType.DoubleColon)]
        private static extern EntityId Internal_CreateWebCamTexture(string scriptingDevice, int requestedWidth, int requestedHeight, int maxFramerate);
    }

}
