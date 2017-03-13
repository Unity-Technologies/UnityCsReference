// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine.SocialPlatforms.GameCenter
{
    public class GameCenterPlatform : UnityEngine.SocialPlatforms.Local
    {
        static public void ResetAllAchievements(Action<bool> callback)
        {
            Debug.Log("ResetAllAchievements - no effect in editor");
            if (callback != null)
                callback(true);
        }

        static public void ShowDefaultAchievementCompletionBanner(bool value)
        {
            Debug.Log("ShowDefaultAchievementCompletionBanner - no effect in editor");
        }

        static public void ShowLeaderboardUI(string leaderboardID, TimeScope timeScope)
        {
            Debug.Log("ShowLeaderboardUI - no effect in editor");
        }
    }
}
