// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Android;
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
}
