// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public enum TouchPhase
    {
        Began = 0,
        Moved = 1,
        Stationary = 2,
        Ended = 3,
        Canceled = 4
    }

    // Controls IME input
    public enum IMECompositionMode
    {
        Auto = 0,
        On = 1,
        Off = 2
    }

    public enum TouchType
    {
        Direct,
        Indirect,
        Stylus
    }

    [NativeHeader("Runtime/Input/InputBindings.h")]
    public struct Touch
    {
        private int m_FingerId;
        private Vector2 m_Position;
        private Vector2 m_RawPosition;
        private Vector2 m_PositionDelta;
        private float m_TimeDelta;
        private int m_TapCount;
        private TouchPhase m_Phase;
        private TouchType m_Type;
        private float m_Pressure;
        private float m_maximumPossiblePressure;
        private float m_Radius;
        private float m_RadiusVariance;
        private float m_AltitudeAngle;
        private float m_AzimuthAngle;

        public int fingerId { get { return m_FingerId; } set { m_FingerId = value; } }
        public Vector2 position { get { return m_Position; } set { m_Position = value; }  }
        public Vector2 rawPosition { get { return m_RawPosition; } set { m_RawPosition = value; }  }
        public Vector2 deltaPosition { get { return m_PositionDelta; } set { m_PositionDelta = value; }  }
        public float deltaTime { get { return m_TimeDelta; } set { m_TimeDelta = value; }  }
        public int tapCount { get { return m_TapCount; } set { m_TapCount = value; }  }
        public TouchPhase phase { get { return m_Phase; } set { m_Phase = value; }  }
        public float pressure { get { return m_Pressure; } set { m_Pressure = value; }  }
        public float maximumPossiblePressure { get { return m_maximumPossiblePressure; } set { m_maximumPossiblePressure = value; }  }

        public TouchType type { get { return m_Type; } set { m_Type = value; }  }
        public float altitudeAngle { get { return m_AltitudeAngle; } set { m_AltitudeAngle = value; }  }
        public float azimuthAngle { get { return m_AzimuthAngle; } set { m_AzimuthAngle = value; }  }
        public float radius { get { return m_Radius; } set { m_Radius = value; }  }
        public float radiusVariance { get { return m_RadiusVariance; } set { m_RadiusVariance = value; }  }
    }

    public enum DeviceOrientation
    {
        Unknown = 0,
        Portrait = 1,
        PortraitUpsideDown = 2,
        LandscapeLeft = 3,
        LandscapeRight = 4,
        FaceUp = 5,
        FaceDown = 6
    }

    public struct AccelerationEvent
    {
        internal float x, y, z;
        internal float m_TimeDelta;

        public Vector3 acceleration { get { return new Vector3(x, y, z); } }
        public float deltaTime { get { return m_TimeDelta; } }
    }

    [NativeHeader("Runtime/Input/GetInput.h")]
    public class Gyroscope
    {
        internal Gyroscope(int index)
        {
            m_GyroIndex = index;
        }

        private int m_GyroIndex;

        [FreeFunction("GetGyroRotationRate")]
        extern private static Vector3 rotationRate_Internal(int idx);
        [FreeFunction("GetGyroRotationRateUnbiased")]
        extern private static Vector3 rotationRateUnbiased_Internal(int idx);
        [FreeFunction("GetGravity")]
        extern private static Vector3 gravity_Internal(int idx);
        [FreeFunction("GetUserAcceleration")]
        extern private static Vector3 userAcceleration_Internal(int idx);
        [FreeFunction("GetAttitude")]
        extern private static Quaternion attitude_Internal(int idx);
        [FreeFunction("IsGyroEnabled")]
        extern private static bool getEnabled_Internal(int idx);
        [FreeFunction("SetGyroEnabled")]
        extern private static void setEnabled_Internal(int idx, bool enabled);
        [FreeFunction("GetGyroUpdateInterval")]
        extern private static float getUpdateInterval_Internal(int idx);
        [FreeFunction("SetGyroUpdateInterval")]
        extern private static void setUpdateInterval_Internal(int idx, float interval);

        public Vector3 rotationRate { get { return rotationRate_Internal(m_GyroIndex); } }
        public Vector3 rotationRateUnbiased { get { return rotationRateUnbiased_Internal(m_GyroIndex); } }
        public Vector3 gravity { get { return gravity_Internal(m_GyroIndex); } }
        public Vector3 userAcceleration { get { return userAcceleration_Internal(m_GyroIndex); } }
        public Quaternion attitude { get { return attitude_Internal(m_GyroIndex); } }
        public bool enabled { get { return getEnabled_Internal(m_GyroIndex); } set { setEnabled_Internal(m_GyroIndex, value); } }
        public float updateInterval { get { return getUpdateInterval_Internal(m_GyroIndex); } set { setUpdateInterval_Internal(m_GyroIndex, value); } }
    }

    public struct LocationInfo
    {
        internal double m_Timestamp;
        internal float m_Latitude;
        internal float m_Longitude;
        internal float m_Altitude;
        internal float m_HorizontalAccuracy;
        internal float m_VerticalAccuracy;

        public float latitude { get { return m_Latitude; } }
        public float longitude { get { return m_Longitude; } }
        public float altitude { get { return m_Altitude; } }
        public float horizontalAccuracy { get { return m_HorizontalAccuracy; } }
        public float verticalAccuracy { get { return m_VerticalAccuracy; } }
        public double timestamp { get { return m_Timestamp; } }
    }

    public enum LocationServiceStatus
    {
        Stopped = 0,
        Initializing = 1,
        Running = 2,
        Failed = 3
    }

    [NativeHeader("Runtime/Input/LocationService.h")]
    [NativeHeader("Runtime/Input/InputBindings.h")]
    public class LocationService
    {
        internal struct HeadingInfo
        {
            public float magneticHeading;
            public float trueHeading;
            public float headingAccuracy;
            public Vector3 raw;
            public double timestamp;
        }

        [FreeFunction("LocationService::IsServiceEnabledByUser")]
        internal extern static bool IsServiceEnabledByUser();
        [FreeFunction("LocationService::GetLocationStatus")]
        internal extern static LocationServiceStatus GetLocationStatus();
        [FreeFunction("LocationService::GetLastLocation")]
        internal extern static LocationInfo GetLastLocation();
        [FreeFunction("LocationService::SetDesiredAccuracy")]
        internal extern static void SetDesiredAccuracy(float value);
        [FreeFunction("LocationService::SetDistanceFilter")]
        internal extern static void SetDistanceFilter(float value);
        [FreeFunction("LocationService::StartUpdatingLocation")]
        internal extern static void StartUpdatingLocation();
        [FreeFunction("LocationService::StopUpdatingLocation")]
        internal extern static void StopUpdatingLocation();
        [FreeFunction("LocationService::GetLastHeading")]
        internal extern static HeadingInfo GetLastHeading();
        [FreeFunction("LocationService::IsHeadingUpdatesEnabled")]
        internal extern static bool IsHeadingUpdatesEnabled();
        [FreeFunction("LocationService::SetHeadingUpdatesEnabled")]
        internal extern static void SetHeadingUpdatesEnabled(bool value);

        public bool isEnabledByUser { get { return IsServiceEnabledByUser(); } }
        public LocationServiceStatus status { get { return GetLocationStatus(); } }
        public LocationInfo lastData
        {
            get
            {
                if (status != LocationServiceStatus.Running)
                    Debug.Log("Location service updates are not enabled. Check LocationService.status before querying last location.");

                return GetLastLocation();
            }
        }

        public void Start(float desiredAccuracyInMeters, float updateDistanceInMeters)
        {
            SetDesiredAccuracy(desiredAccuracyInMeters);
            SetDistanceFilter(updateDistanceInMeters);
            StartUpdatingLocation();
        }

        public void Start(float desiredAccuracyInMeters)
        {
            Start(desiredAccuracyInMeters, 10f);
        }

        public void Start()
        {
            Start(10f, 10f);
        }

        public void Stop()
        {
            StopUpdatingLocation();
        }
    }

    public class Compass
    {
        public float magneticHeading
        {
            get { return LocationService.GetLastHeading().magneticHeading; }
        }
        public float trueHeading
        {
            get { return LocationService.GetLastHeading().trueHeading; }
        }
        public float headingAccuracy
        {
            get { return LocationService.GetLastHeading().headingAccuracy; }
        }
        public Vector3 rawVector
        {
            get { return LocationService.GetLastHeading().raw; }
        }
        public double timestamp
        {
            get { return LocationService.GetLastHeading().timestamp; }
        }
        public bool enabled
        {
            get { return LocationService.IsHeadingUpdatesEnabled(); }
            set { LocationService.SetHeadingUpdatesEnabled(value); }
        }
    }

    [NativeHeader("Runtime/Input/InputBindings.h")]
    public class Input
    {
        [NativeThrows]
        private extern static bool GetKeyInt(KeyCode key);
        [NativeThrows]
        private extern static bool GetKeyString(string name);
        [NativeThrows]
        private extern static bool GetKeyUpInt(KeyCode key);
        [NativeThrows]
        private extern static bool GetKeyUpString(string name);
        [NativeThrows]
        private extern static bool GetKeyDownInt(KeyCode key);
        [NativeThrows]
        private extern static bool GetKeyDownString(string name);
        [NativeThrows]
        public extern static float GetAxis(string axisName);
        [NativeThrows]
        public extern static float GetAxisRaw(string axisName);
        [NativeThrows]
        public extern static bool GetButton(string buttonName);
        [NativeThrows]
        public extern static bool GetButtonDown(string buttonName);
        [NativeThrows]
        public extern static bool GetButtonUp(string buttonName);
        [NativeThrows]
        public extern static bool GetMouseButton(int button);
        [NativeThrows]
        public extern static bool GetMouseButtonDown(int button);
        [NativeThrows]
        public extern static bool GetMouseButtonUp(int button);
        [FreeFunction("ResetInput")]
        public extern static void ResetInputAxes();
        public extern static bool IsJoystickPreconfigured(string joystickName);
        public extern static string[] GetJoystickNames();
        [NativeThrows]
        public extern static Touch GetTouch(int index);
        [NativeThrows]
        public extern static AccelerationEvent GetAccelerationEvent(int index);

        public static bool GetKey(KeyCode key)
        {
            return GetKeyInt(key);
        }

        public static bool GetKey(string name)
        {
            return GetKeyString(name);
        }

        public static bool GetKeyUp(KeyCode key)
        {
            return GetKeyUpInt(key);
        }

        public static bool GetKeyUp(string name)
        {
            return GetKeyUpString(name);
        }

        public static bool GetKeyDown(KeyCode key)
        {
            return GetKeyDownInt(key);
        }

        public static bool GetKeyDown(string name)
        {
            return GetKeyDownString(name);
        }

        public extern static bool simulateMouseWithTouches { get; set; }
        public extern static bool anyKey { get; }
        public extern static bool anyKeyDown { get; }
        public extern static string inputString { get; }
        public extern static Vector3 mousePosition { get; }
        public extern static Vector2 mouseScrollDelta { get; }
        public extern static IMECompositionMode imeCompositionMode { get; set; }
        public extern static string compositionString { get; }
        public extern static bool imeIsSelected { get; }
        public extern static Vector2 compositionCursorPos { get; set; }
        [Obsolete("eatKeyPressOnTextFieldFocus property is deprecated, and only provided to support legacy behavior.")]
        public extern static bool eatKeyPressOnTextFieldFocus { get; set; }

        public extern static bool mousePresent
        {
            [FreeFunction("GetMousePresent")]
            get;
        }
        public extern static int touchCount
        {
            [FreeFunction("GetTouchCount")]
            get;
        }
        public extern static bool touchPressureSupported
        {
            [FreeFunction("IsTouchPressureSupported")]
            get;
        }
        public extern static bool stylusTouchSupported
        {
            [FreeFunction("IsStylusTouchSupported")]
            get;
        }
        public extern static bool touchSupported
        {
            [FreeFunction("IsTouchSupported")]
            get;
        }
        public extern static bool multiTouchEnabled
        {
            [FreeFunction("IsMultiTouchEnabled")]
            get;
            [FreeFunction("SetMultiTouchEnabled")]
            set;
        }
        [Obsolete("isGyroAvailable property is deprecated. Please use SystemInfo.supportsGyroscope instead.")]
        public extern static bool isGyroAvailable
        {
            [FreeFunction("IsGyroAvailable")]
            get;
        }
        public extern static DeviceOrientation deviceOrientation
        {
            [FreeFunction("GetOrientation")]
            get;
        }
        public extern static Vector3 acceleration
        {
            [FreeFunction("GetAcceleration")]
            get;
        }
        public extern static bool compensateSensors
        {
            [FreeFunction("IsCompensatingSensors")]
            get;
            [FreeFunction("SetCompensatingSensors")]
            set;
        }
        public extern static int accelerationEventCount
        {
            [FreeFunction("GetAccelerationCount")]
            get;
        }
        public extern static bool backButtonLeavesApp
        {
            [FreeFunction("GetBackButtonLeavesApp")]
            get;
            [FreeFunction("SetBackButtonLeavesApp")]
            set;
        }

        private static LocationService locationServiceInstance;
        public static LocationService location
        {
            get
            {
                if (locationServiceInstance == null)
                    locationServiceInstance = new LocationService();
                return locationServiceInstance;
            }
        }
        private static Compass compassInstance;
        public static Compass compass
        {
            get
            {
                if (compassInstance == null)
                    compassInstance = new Compass();
                return compassInstance;
            }
        }
        [FreeFunction("GetGyro")]
        private extern static int GetGyroInternal();
        private static Gyroscope s_MainGyro;
        public static Gyroscope gyro
        {
            get
            {
                if (s_MainGyro == null)
                    s_MainGyro = new Gyroscope(GetGyroInternal());
                return s_MainGyro;
            }
        }

        public static Touch[] touches
        {
            get
            {
                int count = touchCount;
                Touch[] touches = new Touch[count];
                for (int q = 0; q < count; ++q)
                    touches[q] = GetTouch(q);
                return touches;
            }
        }

        public static AccelerationEvent[] accelerationEvents
        {
            get
            {
                int count = accelerationEventCount;
                AccelerationEvent[] events = new AccelerationEvent[count];
                for (int q = 0; q < count; ++q)
                    events[q] = GetAccelerationEvent(q);
                return events;
            }
        }
    }
}
