// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;


namespace UnityEngine.XR.WSA
{
    // [[WorldManager]] reports the state of the positional locator systems
    [NativeHeader("Runtime/VR/HoloLens/HoloLensWorldManager.h")]
    [MovedFrom("UnityEngine.VR.WSA")]
    public enum PositionalLocatorState
    {
        Unavailable = 0,
        OrientationOnly = 1,
        Activating = 2,
        Active = 3,
        Inhibited = 4
    };

    // Augmented Reality specific world methods
    // exposes information about the real world
    // tracking systems to managed systems.
    [StaticAccessor("HoloLensWorldManager", StaticAccessorType.DoubleColon)]
    [MovedFrom("UnityEngine.VR.WSA")]
    public class WorldManager
    {
        // Notification on when the world tracking systems state has changed
        public delegate void OnPositionalLocatorStateChangedDelegate(PositionalLocatorState oldState, PositionalLocatorState newState);

        // Notification on when the world tracking systems state has changed
        public static event OnPositionalLocatorStateChangedDelegate OnPositionalLocatorStateChanged;

        // If the user has registered notify of tracking loss
        [RequiredByNativeCode]
        private static void Internal_TriggerPositionalLocatorStateChanged(PositionalLocatorState oldState, PositionalLocatorState newState)
        {
            if (OnPositionalLocatorStateChanged != null)
                OnPositionalLocatorStateChanged(oldState, newState);
        }

        // Query the state of the world tracking systems.
        public static PositionalLocatorState state { get { return PositionalLocatorState.Unavailable; } }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("GetNativeISpatialCoordinateSystemPtrForScript")]
        public extern static IntPtr GetNativeISpatialCoordinateSystemPtr();
    }
}

