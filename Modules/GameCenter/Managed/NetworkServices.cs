// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    using UnityEngine.SocialPlatforms;

    // A facade for the social API namespace, no state, only helper functions which delegate into others
    public static class Social
    {
        public static ISocialPlatform Active
        {
            get { return ActivePlatform.Instance; }
            set { ActivePlatform.Instance = value; }
        }

        public static ILocalUser localUser { get { return Active.localUser; } }

        public static void LoadUsers(string[] userIDs, Action<IUserProfile[]> callback)
        {
            Active.LoadUsers(userIDs, callback);
        }

        public static void ReportProgress(string achievementID, double progress, Action<bool> callback)
        {
            Active.ReportProgress(achievementID, progress, callback);
        }

        public static void LoadAchievementDescriptions(Action<IAchievementDescription[]> callback)
        {
            Active.LoadAchievementDescriptions(callback);
        }

        public static void LoadAchievements(Action<IAchievement[]> callback)
        {
            Active.LoadAchievements(callback);
        }

        public static void ReportScore(Int64 score, string board, Action<bool> callback)
        {
            Active.ReportScore(score, board, callback);
        }

        public static void LoadScores(string leaderboardID, Action<IScore[]> callback)
        {
            Active.LoadScores(leaderboardID, callback);
        }

        public static ILeaderboard CreateLeaderboard()
        {
            return Active.CreateLeaderboard();
        }

        public static IAchievement CreateAchievement()
        {
            return Active.CreateAchievement();
        }

        public static void ShowAchievementsUI()
        {
            Active.ShowAchievementsUI();
        }

        public static void ShowLeaderboardUI()
        {
            Active.ShowLeaderboardUI();
        }
    }
}

namespace UnityEngine.SocialPlatforms
{
    // The state of the current active social implementation
    internal static class ActivePlatform
    {
        private static ISocialPlatform _active;

        internal static ISocialPlatform Instance
        {
            get
            {
                if (_active == null)
                    _active = SelectSocialPlatform();
                return _active;
            }
            set
            {
                _active = value;
            }
        }

        private static ISocialPlatform SelectSocialPlatform()
        {
            // statically selecting community
            return new UnityEngine.SocialPlatforms.Local();
        }
    }

    public interface ISocialPlatform
    {
        ILocalUser localUser { get; }

        void LoadUsers(string[] userIDs, Action<IUserProfile[]> callback);

        void ReportProgress(string achievementID, double progress, Action<bool> callback);
        void LoadAchievementDescriptions(Action<IAchievementDescription[]> callback);
        void LoadAchievements(Action<IAchievement[]> callback);
        IAchievement CreateAchievement();

        void ReportScore(Int64 score, string board, Action<bool> callback);
        void LoadScores(string leaderboardID, Action<IScore[]> callback);
        ILeaderboard CreateLeaderboard();

        void ShowAchievementsUI();
        void ShowLeaderboardUI();

        // ===> These should be explicitly implemented <===
        void Authenticate(ILocalUser user, Action<bool> callback);
        void Authenticate(ILocalUser user, Action<bool, string> callback);
        void LoadFriends(ILocalUser user, Action<bool> callback);
        void LoadScores(ILeaderboard board, Action<bool> callback);
        bool GetLoading(ILeaderboard board);
    }

    public interface ILocalUser : IUserProfile
    {
        void Authenticate(Action<bool> callback);
        void Authenticate(Action<bool, string> callback);

        void LoadFriends(Action<bool> callback);

        IUserProfile[] friends { get; }
        bool authenticated { get; }
        bool underage { get; }
    }

    public enum UserState
    {
        Online,
        OnlineAndAway,
        OnlineAndBusy,
        Offline,
        Playing
    }

    public interface IUserProfile
    {
        string userName { get; }
        string id { get; }
        bool isFriend { get; }
        UserState state { get; }
        Texture2D image { get; }
    }

    public interface IAchievement
    {
        void ReportProgress(Action<bool> callback);

        string id { get; set; }
        double percentCompleted { get; set; }
        bool completed { get; }
        bool hidden { get; }
        DateTime lastReportedDate { get; }
    }

    public interface IAchievementDescription
    {
        string id { get; set; }
        string title { get; }
        Texture2D image { get; }
        string achievedDescription { get; }
        string unachievedDescription { get; }
        bool hidden { get; }
        int points { get; }
    }

    public interface IScore
    {
        void ReportScore(Action<bool> callback);

        string leaderboardID { get; set; }
        // TODO: This is just an int64 here, but should be able to represent all supported formats, except for float type scores ...
        Int64 value { get; set; }
        DateTime date { get; }
        string formattedValue { get; }
        string userID { get; }
        int rank { get; }
    }

    public enum UserScope
    {
        Global = 0,
        FriendsOnly
    }

    public enum TimeScope
    {
        Today = 0,
        Week,
        AllTime
    }

    public struct Range
    {
        public int from;
        public int count;

        public Range(int fromValue, int valueCount)
        {
            from = fromValue;
            count = valueCount;
        }
    }

    public interface ILeaderboard
    {
        void SetUserFilter(string[] userIDs);
        void LoadScores(Action<bool> callback);
        bool loading { get; }

        string id { get; set; }
        UserScope userScope { get; set; }
        Range range { get; set; }
        TimeScope timeScope { get; set; }
        IScore localUserScore { get; }
        uint maxRange { get; }
        IScore[] scores { get; }
        string title { get; }
    }
}
