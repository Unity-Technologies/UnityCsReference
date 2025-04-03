// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Android
{
    /// <summary>
    /// <seealso href="https://developer.android.com/reference/android/app/GameState">developer.android.com</seealso>
    /// </summary>
    [NativeType(Header = "Modules/AndroidJNI/Public/GameStateHelper.h")]
    public enum AndroidGameState
    {
        /// <summary>
        /// <para>Default Game state is unknown.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/GameState#MODE_UNKNOWN">developer.android.com</seealso>
        /// </summary>
        Unknown = 0x00000000,

        /// <summary>
        /// <para>No state means that the game is not in active play, for example the user is using the game menu.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/GameState#MODE_NONE">developer.android.com</seealso>
        /// </summary>
        None = 0x00000001,

        /// <summary>
        /// <para>Indicates if the game is in active, but interruptible, game play.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/GameState#MODE_GAMEPLAY_INTERRUPTIBLE">developer.android.com</seealso>
        /// </summary>
        GamePlayInterruptible = 0x00000002,

        /// <summary>
        /// <para>Indicates if the game is in active user play mode, which is real time and cannot be interrupted.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/GameState#MODE_GAMEPLAY_UNINTERRUPTIBLE">developer.android.com</seealso>
        /// </summary>
        GamePlayUninterruptible = 0x00000003,

        /// <summary>
        /// <para>Indicates that the current content shown is not game play related. For example it can be an ad, a web page, a text, or a video.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/GameState#MODE_CONTENT">developer.android.com</seealso>
        /// </summary>
        Content = 0x00000004
    }

    [NativeType(Header = "Modules/AndroidJNI/Public/GameStateHelper.h")]
    internal enum GameStateLabel
    {
        Default = -1,
        InitialLoading = -2,
        AssetPacksLoading = -3,
        WebRequest = -4
    }

    [NativeHeader("Modules/AndroidJNI/Public/GameStateHelper.h")]
    [StaticAccessor("GameStateHelper::Get()", StaticAccessorType.Dot)]
    public static partial class AndroidGame
    {
        [StaticAccessor("GameStateHelper::Get()", StaticAccessorType.Dot)]
        public static partial class Automatic
        {
            /// <summary>
            /// <para>Set current GameState mode which will be used for automated GameState hinting.</para>
            /// </summary>
            /// <param name="mode">GameState mode value.</param>
            [NativeMethod("SetGameStateMode")]
            public static extern void SetGameState(AndroidGameState mode);
        }
        // Required for automated SetGameState calls, indicates to the operating system when the application is in loading state, level is the type of loading
        internal static extern void StartLoading(int label);
        // Required for automated SetGameState calls, indicates to the operating system when loading state is ended, level is the type of loading
        internal static extern void StopLoading(int label);
    }
}
