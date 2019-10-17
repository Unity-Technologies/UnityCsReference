// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using UnityEngine.Internal;
using UnityEngine.Bindings;

namespace UnityEngine
{
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
    [NativeHeader("Runtime/Export/Handheld/Handheld.bindings.h")]
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

        /// Sets ActivityIndicator style. See iOS.ActivityIndicatorStyle enumeration for possible values.
        /// Be warned that it will take effect on next call to StartActivityIndicator.
        public static void SetActivityIndicatorStyle(iOS.ActivityIndicatorStyle style)
        {
            SetActivityIndicatorStyleImpl_Bindings((int)style);
        }


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
}
