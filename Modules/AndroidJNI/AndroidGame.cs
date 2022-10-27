// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Android
{
    /// <summary>
    /// <seealso href="https://developer.android.com/reference/android/app/GameManager">developer.android.com</seealso>
    /// </summary>
    public enum AndroidGameMode
    {
        /// <summary>
        /// <para>Game mode is not supported for this application.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/GameManager#GAME_MODE_UNSUPPORTED">developer.android.com</seealso>
        /// </summary>
        Unsupported = 0x00000000,

        /// <summary>
        /// <para>Standard game mode means the platform will use the game's default performance characteristics.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/GameManager#GAME_MODE_STANDARD">developer.android.com</seealso>
        /// </summary>
        Standard = 0x00000001,

        /// <summary>
        /// <para>Performance game mode maximizes the game's performance.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/GameManager#GAME_MODE_PERFORMANCE">developer.android.com</seealso>
        /// </summary>
        Performance = 0x00000002,

        /// <summary>
        /// <para>Battery game mode will save battery and give longer game play time.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/GameManager#GAME_MODE_BATTERY">developer.android.com</seealso>
        /// </summary>
        Battery = 0x00000003
    }

    /// <summary>
    /// <seealso href="https://developer.android.com/reference/android/app/GameState">developer.android.com</seealso>
    /// </summary>
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

    public static class AndroidGame
    {
        private static AndroidJavaObject m_UnityGameManager;
        private static AndroidJavaObject m_UnityGameState;

        private static AndroidJavaObject GetUnityGameManager()
        {
            if (m_UnityGameManager != null)
            {
                return m_UnityGameManager;
            }

            m_UnityGameManager = new AndroidJavaClass("com.unity3d.player.UnityGameManager");

            return m_UnityGameManager;
        }

        private static AndroidJavaObject GetUnityGameState()
        {
            if (m_UnityGameState != null)
            {
                return m_UnityGameState;
            }

            m_UnityGameState = new AndroidJavaClass("com.unity3d.player.UnityGameState");

            return m_UnityGameState;
        }

        /// <summary>
        /// <para>Get the user selected game mode for the application.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/GameManager#getGameMode()">developer.android.com</seealso>
        /// </summary>
        /// <returns>User selected <see cref="Android.GameMode"/></returns>
        public static AndroidGameMode GameMode
        {
            get
            {
                return AndroidGameMode.Unsupported;
            }
        }

        /// <summary>
        /// <para>Create a GameState with the specified loading status.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/GameState#GameState(boolean,%20int)">developer.android.com</seealso>
        /// </summary>
        /// <param name="isLoading">Whether the game is in the loading state.</param>
        /// <param name="gameState">The game state of type <see cref="AndroidGameState"/></param>
        public static void SetGameState(bool isLoading, AndroidGameState gameState)
        {
        }

        /// <summary>
        /// <para>Create a GameState with the given state variables.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/GameState#GameState(boolean,%20int,%20int,%20int)">developer.android.com</seealso>
        /// </summary>
        /// <param name="isLoading">Whether the game is in the loading state.</param>
        /// <param name="gameState">The game state of type <see cref="AndroidGameState"/></param>
        /// <param name="label">An developer-supplied value e.g. for the current level.</param>
        /// <param name="quality">An developer-supplied value, e.g. for the current quality level.</param>
        public static void SetGameState(bool isLoading, AndroidGameState gameState, int label, int quality)
        {
        }
    }
}
