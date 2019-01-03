// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Input/TimeManager.h")]
    [StaticAccessor("GetTimeManager()", StaticAccessorType.Dot)]
    // The interface to get time information from Unity.
    public class Time
    {
        // The time this frame has started (RO). This is the time in seconds since the start of the game.
        [NativeProperty("CurTime")]
        public static extern float time { get; }

        // The time this frame has started (RO). This is the time in seconds since the last level has been loaded.
        [NativeProperty("TimeSinceSceneLoad")]
        public static extern float timeSinceLevelLoad { get; }

        // The time in seconds it took to complete the last frame (RO).
        public static extern float deltaTime { get; }

        // The time the latest MonoBehaviour::pref::FixedUpdate has started (RO). This is the time in seconds since the start of the game.
        public static extern float fixedTime { get; }

        // The cached real time (realTimeSinceStartup) at the start of this frame
        public static extern float unscaledTime { get; }

        // The real time corresponding to this fixed frame
        public static extern float fixedUnscaledTime { get; }

        // The delta time based upon the realTime
        public static extern float unscaledDeltaTime { get; }

        // The delta time based upon the realTime
        public static extern float fixedUnscaledDeltaTime { get; }

        // The interval in seconds at which physics and other fixed frame rate updates (like MonoBehaviour's MonoBehaviour::pref::FixedUpdate) are performed.
        public static extern float fixedDeltaTime  { get; set; }

        // The maximum time a frame can take. Physics and other fixed frame rate updates (like MonoBehaviour's MonoBehaviour::pref::FixedUpdate)
        public static extern float maximumDeltaTime  { get; set; }

        // A smoothed out Time.deltaTime (RO).
        public static extern float smoothDeltaTime { get; }

        // The maximum time a frame can spend on particle updates. If the frame takes longer than this, then updates are split into multiple smaller updates.
        public static extern float maximumParticleDeltaTime  { get; set; }

        // The scale at which the time is passing. This can be used for slow motion effects.
        public static extern float timeScale { get; set; }

        // The total number of frames that have passed (RO).
        public static extern int frameCount { get; }

        //*undocumented*
        [NativeProperty("RenderFrameCount")]
        public static extern int renderedFrameCount { get; }

        // The real time in seconds since the game started (RO).
        [NativeProperty("Realtime")]
        public static extern float realtimeSinceStartup { get; }

        // If /captureFramerate/ is set to a value larger than 0, time will advance in
        public static extern int captureFramerate { get; set; }

        // Returns true if inside a fixed time step callback such as FixedUpdate, otherwise false.
        public static extern bool inFixedTimeStep
        {
            [NativeName("IsUsingFixedTimeStep")]
            get;
        }
    }
}
