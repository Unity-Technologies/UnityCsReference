// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;


namespace UnityEngine.XR.WSA
{
    // Augmented Reality specific origin controls.
    // Objects can be body Anchored, world Anchored or
    // simply inherit their position from the current state
    // of the virtual world.

    [MovedFrom("UnityEngine.VR.WSA")]
    [NativeHeader("Modules/VR/HoloLens/WorldAnchor/WorldAnchor.h")]
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    public class WorldAnchor : Component
    {
        private WorldAnchor() {}

        // Allows you to register for notices about when this object loses it's ability to be tracked
        // by the world tracking systems.
        // By default objects that are world Anchored but not locatable become deactivated
        // listening to this event will override this behaviour.
        public delegate void OnTrackingChangedDelegate(WorldAnchor worldAnchor, bool located);
        public event OnTrackingChangedDelegate OnTrackingChanged;

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public extern bool isLocated { get; }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("SetSpatialAnchor_Internal")]
        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public extern void SetNativeSpatialAnchorPtr(IntPtr spatialAnchorPtr);

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("GetSpatialAnchor_Internal")]
        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public extern IntPtr GetNativeSpatialAnchorPtr();

        //////////////////////////////////////////////////////////////////
        // Enumerability
        //////////////////////////////////////////////////////////////////

        [RequiredByNativeCode]
        private static void Internal_TriggerEventOnTrackingLost(WorldAnchor worldAnchor, bool located)
        {
            if (worldAnchor != null && worldAnchor.OnTrackingChanged != null)
            {
                // The user asked to handle this behaviour
                worldAnchor.OnTrackingChanged(worldAnchor, located);
            }
        }
    }
}

