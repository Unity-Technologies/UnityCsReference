// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngineInternal;

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

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct Touch
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

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AccelerationEvent
{
    private float x, y, z;
    private float m_TimeDelta;
    
    
    public Vector3 acceleration { get { return new Vector3(x, y, z); } }
    
    
    public float deltaTime { get { return m_TimeDelta; } }
}

public sealed partial class Gyroscope
{
    internal Gyroscope(int index)
        {
            m_GyroIndex = index;
        }
    
    
    private int m_GyroIndex;
    private static Vector3 rotationRate_Internal (int idx) {
        Vector3 result;
        INTERNAL_CALL_rotationRate_Internal ( idx, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_rotationRate_Internal (int idx, out Vector3 value);
    private static Vector3 rotationRateUnbiased_Internal (int idx) {
        Vector3 result;
        INTERNAL_CALL_rotationRateUnbiased_Internal ( idx, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_rotationRateUnbiased_Internal (int idx, out Vector3 value);
    private static Vector3 gravity_Internal (int idx) {
        Vector3 result;
        INTERNAL_CALL_gravity_Internal ( idx, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_gravity_Internal (int idx, out Vector3 value);
    private static Vector3 userAcceleration_Internal (int idx) {
        Vector3 result;
        INTERNAL_CALL_userAcceleration_Internal ( idx, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_userAcceleration_Internal (int idx, out Vector3 value);
    private static Quaternion attitude_Internal (int idx) {
        Quaternion result;
        INTERNAL_CALL_attitude_Internal ( idx, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_attitude_Internal (int idx, out Quaternion value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool getEnabled_Internal (int idx) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void setEnabled_Internal (int idx, bool enabled) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  float getUpdateInterval_Internal (int idx) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void setUpdateInterval_Internal (int idx, float interval) ;

    public Vector3 rotationRate { get { return rotationRate_Internal(m_GyroIndex); } }
    
    
    public Vector3 rotationRateUnbiased { get { return rotationRateUnbiased_Internal(m_GyroIndex); } }
    
    
    public Vector3 gravity { get { return gravity_Internal(m_GyroIndex); } }
    
    
    public Vector3 userAcceleration { get { return userAcceleration_Internal(m_GyroIndex); } }
    
    
    public Quaternion attitude { get { return attitude_Internal(m_GyroIndex); } }
    
    
    public bool enabled { get { return getEnabled_Internal(m_GyroIndex); } set { setEnabled_Internal(m_GyroIndex, value); } }
    
    
    public float updateInterval { get { return getUpdateInterval_Internal(m_GyroIndex); } set { setUpdateInterval_Internal(m_GyroIndex, value); } }
    
    
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct LocationInfo
{
    private double m_Timestamp;
    private float m_Latitude;
    private float m_Longitude;
    private float m_Altitude;
    private float m_HorizontalAccuracy;
    private float m_VerticalAccuracy;
    
    
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

public sealed partial class LocationService
{
    public extern  bool isEnabledByUser
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  LocationServiceStatus status
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  LocationInfo lastData
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Start ( [uei.DefaultValue("10f")] float desiredAccuracyInMeters , [uei.DefaultValue("10f")]  float updateDistanceInMeters ) ;

    [uei.ExcludeFromDocs]
    public void Start (float desiredAccuracyInMeters ) {
        float updateDistanceInMeters = 10f;
        Start ( desiredAccuracyInMeters, updateDistanceInMeters );
    }

    [uei.ExcludeFromDocs]
    public void Start () {
        float updateDistanceInMeters = 10f;
        float desiredAccuracyInMeters = 10f;
        Start ( desiredAccuracyInMeters, updateDistanceInMeters );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Stop () ;

}

public sealed partial class Compass
{
    public extern  float magneticHeading
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  float trueHeading
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  float headingAccuracy
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public  Vector3 rawVector
    {
        get { Vector3 tmp; INTERNAL_get_rawVector(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_rawVector (out Vector3 value) ;


    public extern  double timestamp
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool enabled
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class Input
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int mainGyroIndex_Internal () ;

    private static Gyroscope m_MainGyro = null;
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool GetKeyInt (int key) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool GetKeyString (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool GetKeyUpInt (int key) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool GetKeyUpString (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool GetKeyDownInt (int key) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool GetKeyDownString (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float GetAxis (string axisName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float GetAxisRaw (string axisName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetButton (string buttonName) ;

    public extern static bool compensateSensors
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("isGyroAvailable property is deprecated. Please use SystemInfo.supportsGyroscope instead.")]
    public extern static bool isGyroAvailable
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public static Gyroscope gyro { get { if (m_MainGyro == null) m_MainGyro = new Gyroscope(mainGyroIndex_Internal()); return m_MainGyro; } }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetButtonDown (string buttonName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetButtonUp (string buttonName) ;

    public static bool GetKey(string name)
        {
            return GetKeyString(name);
        }
    
    
    public static bool GetKey(KeyCode key)
        {
            return GetKeyInt((int)key);
        }
    
    
    public static bool GetKeyDown(string name)
        {
            return GetKeyDownString(name);
        }
    
    
    public static bool GetKeyDown(KeyCode key)
        {
            return GetKeyDownInt((int)key);
        }
    
    
    public static bool GetKeyUp(string name)
        {
            return GetKeyUpString(name);
        }
    
    
    public static bool GetKeyUp(KeyCode key)
        {
            return GetKeyUpInt((int)key);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetJoystickNames () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsJoystickPreconfigured (string joystickName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetMouseButton (int button) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetMouseButtonDown (int button) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetMouseButtonUp (int button) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ResetInputAxes () ;

    public static Vector3 mousePosition
    {
        get { Vector3 tmp; INTERNAL_get_mousePosition(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_mousePosition (out Vector3 value) ;


    public static Vector2 mouseScrollDelta
    {
        get { Vector2 tmp; INTERNAL_get_mouseScrollDelta(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_mouseScrollDelta (out Vector2 value) ;


    public extern static bool mousePresent
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool simulateMouseWithTouches
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool anyKey
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool anyKeyDown
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static string inputString
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public static Vector3 acceleration
    {
        get { Vector3 tmp; INTERNAL_get_acceleration(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_acceleration (out Vector3 value) ;


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
    
    
    
    public static AccelerationEvent GetAccelerationEvent (int index) {
        AccelerationEvent result;
        INTERNAL_CALL_GetAccelerationEvent ( index, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetAccelerationEvent (int index, out AccelerationEvent value);
    public extern static int accelerationEventCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
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
    
    
    public static Touch GetTouch (int index) {
        Touch result;
        INTERNAL_CALL_GetTouch ( index, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetTouch (int index, out Touch value);
    public extern static int touchCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("eatKeyPressOnTextFieldFocus property is deprecated, and only provided to support legacy behavior.")]
    public extern static bool eatKeyPressOnTextFieldFocus
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool touchPressureSupported
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool stylusTouchSupported
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool touchSupported
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool multiTouchEnabled
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
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
    
    
    public extern static DeviceOrientation deviceOrientation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static IMECompositionMode imeCompositionMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static string compositionString
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool imeIsSelected
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public static Vector2 compositionCursorPos
    {
        get { Vector2 tmp; INTERNAL_get_compositionCursorPos(out tmp); return tmp;  }
        set { INTERNAL_set_compositionCursorPos(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_compositionCursorPos (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_compositionCursorPos (ref Vector2 value) ;

    public extern static bool backButtonLeavesApp
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

}
