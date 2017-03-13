// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.SocialPlatforms.Impl
{
    public class LocalUser : UserProfile, ILocalUser
    {
        private IUserProfile[] m_Friends;
        private bool m_Authenticated;
        private bool m_Underage;

        public LocalUser()
        {
            m_Friends = new UserProfile[0];
            m_Authenticated = false;
            m_Underage = false;
        }

        public void Authenticate(Action<bool> callback)
        {
            ActivePlatform.Instance.Authenticate((ILocalUser)this, callback);
        }

        public void Authenticate(Action<bool, string> callback)
        {
            ActivePlatform.Instance.Authenticate((ILocalUser)this, callback);
        }

        public void LoadFriends(Action<bool> callback)
        {
            ActivePlatform.Instance.LoadFriends((ILocalUser)this, callback);
        }

        // Setters for implementors
        public void SetFriends(IUserProfile[] friends)
        {
            m_Friends = friends;
        }

        public void SetAuthenticated(bool value)
        {
            m_Authenticated = value;
        }

        public void SetUnderage(bool value)
        {
            m_Underage = value;
        }

        public IUserProfile[] friends { get { return m_Friends; } }
        public bool authenticated { get { return m_Authenticated; } }
        public bool underage { get { return m_Underage; } }
    }

    public class UserProfile : IUserProfile
    {
        protected string m_UserName;
        protected string m_ID;
        protected bool m_IsFriend;
        protected UserState m_State;
        protected Texture2D m_Image;

        public UserProfile()
        {
            m_UserName = "Uninitialized";
            m_ID = "0";
            m_IsFriend = false;
            m_State = UserState.Offline;
            m_Image = new Texture2D(32, 32);
        }

        public UserProfile(string name, string id, bool friend) : this(name, id, friend, UserState.Offline, new Texture2D(0, 0)) {}

        public UserProfile(string name, string id, bool friend, UserState state, Texture2D image)
        {
            m_UserName = name;
            m_ID = id;
            m_IsFriend = friend;
            m_State = state;
            m_Image = image;
        }

        public override string ToString()
        {
            return id + " - " +
                userName + " - " +
                isFriend + " - " +
                state;
        }

        public void SetUserName(string name)
        {
            m_UserName = name;
        }

        public void SetUserID(string id)
        {
            m_ID = id;
        }

        public void SetImage(Texture2D image)
        {
            m_Image = image;
        }

        public void SetIsFriend(bool value)
        {
            m_IsFriend = value;
        }

        public void SetState(UserState state)
        {
            m_State = state;
        }

        public string userName { get { return m_UserName; } }
        public string id { get { return m_ID; } }
        public bool isFriend { get { return m_IsFriend; } }
        public UserState state { get { return m_State; } }
        public Texture2D image { get { return m_Image; } }
    }

    public class Achievement : IAchievement
    {
        private bool m_Completed;
        private bool m_Hidden;
        private DateTime m_LastReportedDate;

        public Achievement(string id,
                           double percentCompleted,
                           bool completed,
                           bool hidden,
                           DateTime lastReportedDate)
        {
            this.id = id;
            this.percentCompleted = percentCompleted;
            m_Completed = completed;
            m_Hidden = hidden;
            m_LastReportedDate = lastReportedDate;
        }

        public Achievement(string id, double percent)
        {
            this.id = id;
            percentCompleted = percent;
            m_Hidden = false;
            m_Completed = false;
            m_LastReportedDate = DateTime.MinValue;
        }

        public Achievement() : this("unknown", 0.0) {}

        public override string ToString()
        {
            return id + " - " +
                percentCompleted + " - " +
                completed + " - " +
                hidden + " - " +
                lastReportedDate;
        }

        public void ReportProgress(Action<bool> callback)
        {
            ActivePlatform.Instance.ReportProgress(id, percentCompleted, callback);
        }

        // Setters for implementors
        public void SetCompleted(bool value)
        {
            m_Completed = value;
        }

        public void SetHidden(bool value)
        {
            m_Hidden = value;
        }

        public void SetLastReportedDate(DateTime date)
        {
            m_LastReportedDate = date;
        }

        public string id { get; set; }
        public double percentCompleted { get; set; }
        public bool completed { get { return m_Completed; } }
        public bool hidden { get { return m_Hidden; } }
        public DateTime lastReportedDate { get { return m_LastReportedDate; } }

        // TODO: How to have a static method for resetting all achivements?
        //public abstract void ResetAllAchievements();
    }

    public class AchievementDescription : IAchievementDescription
    {
        private string m_Title;
        private Texture2D m_Image;
        private string m_AchievedDescription;
        private string m_UnachievedDescription;
        private bool m_Hidden;
        private int m_Points;

        public AchievementDescription(string id,
                                      string title,
                                      Texture2D image,
                                      string achievedDescription,
                                      string unachievedDescription,
                                      bool hidden,
                                      int points)
        {
            this.id = id;
            m_Title = title;
            m_Image = image;
            m_AchievedDescription = achievedDescription;
            m_UnachievedDescription = unachievedDescription;
            m_Hidden = hidden;
            m_Points = points;
        }

        public override string ToString()
        {
            return id + " - " +
                title + " - " +
                achievedDescription + " - " +
                unachievedDescription + " - " +
                points + " - " +
                hidden;
        }

        public void SetImage(Texture2D image)
        {
            m_Image = image;
        }

        public string id { get; set; }
        public string title { get { return m_Title; } }
        public Texture2D image { get { return m_Image; } }
        public string achievedDescription { get { return m_AchievedDescription; } }
        public string unachievedDescription { get { return m_UnachievedDescription; } }
        public bool hidden { get { return m_Hidden; } }
        public int points { get { return m_Points; } }
    }

    public class Score : IScore
    {
        private DateTime m_Date;
        private string m_FormattedValue;
        private string m_UserID;
        private int m_Rank;

        public Score() : this("unkown", -1) {}

        public Score(string leaderboardID, Int64 value)
            : this(leaderboardID, value, "0", DateTime.Now, "", -1)
        {}

        public Score(string leaderboardID, Int64 value, string userID, DateTime date, string formattedValue, int rank)
        {
            this.leaderboardID = leaderboardID;
            this.value = value;
            m_UserID = userID;
            m_Date = date;
            m_FormattedValue = formattedValue;
            m_Rank = rank;
        }

        public override string ToString()
        {
            return "Rank: '" + m_Rank + "' Value: '" + value + "' Category: '" + leaderboardID + "' PlayerID: '" +
                m_UserID + "' Date: '" + m_Date;
        }

        public void ReportScore(Action<bool> callback)
        {
            ActivePlatform.Instance.ReportScore(value, leaderboardID, callback);
        }

        public void SetDate(DateTime date)
        {
            m_Date = date;
        }

        public void SetFormattedValue(string value)
        {
            m_FormattedValue = value;
        }

        public void SetUserID(string userID)
        {
            m_UserID = userID;
        }

        public void SetRank(int rank)
        {
            m_Rank = rank;
        }

        public string leaderboardID { get; set; }
        // TODO: This is just an int64 here, but should be able to represent all supported formats, except for float type scores ...
        public Int64 value { get; set; }
        public DateTime date { get { return m_Date; } }
        public string formattedValue { get { return m_FormattedValue; } }
        public string userID { get { return m_UserID; } }
        public int rank { get { return m_Rank; } }
    }

    public class Leaderboard : ILeaderboard
    {
        private bool m_Loading;
        private IScore m_LocalUserScore;
        private uint m_MaxRange;
        private IScore[] m_Scores;
        private string m_Title;
        private string[] m_UserIDs;

        public Leaderboard()
        {
            id = "Invalid";
            range = new Range(1, 10);
            userScope = UserScope.Global;
            timeScope = TimeScope.AllTime;
            m_Loading = false;
            m_LocalUserScore = new Score("Invalid", 0);
            m_MaxRange = 0;
            m_Scores = new Score[0];
            m_Title = "Invalid";
            m_UserIDs = new string[0];
        }

        // TODO: Implement different behaviour when this is populated
        /*public Leaderboard(string[] userIDs): this()
        {
            m_UserIDs = userIDs;
        }*/

        public void SetUserFilter(string[] userIDs)
        {
            m_UserIDs = userIDs;
        }

        public override string ToString()
        {
            return "ID: '" + id + "' Title: '" + m_Title + "' Loading: '" + m_Loading + "' Range: [" +
                range.from + "," + range.count + "] MaxRange: '" + m_MaxRange + "' Scores: '" +
                m_Scores.Length + "' UserScope: '" + userScope + "' TimeScope: '" + timeScope + "' UserFilter: '"
                + m_UserIDs.Length;
        }

        public void LoadScores(Action<bool> callback)
        {
            ActivePlatform.Instance.LoadScores(this, callback);
        }

        public bool loading
        {
            get { return ActivePlatform.Instance.GetLoading(this); }
        }

        // Setters for implementors
        public void SetLocalUserScore(IScore score)
        {
            m_LocalUserScore = score;
        }

        public void SetMaxRange(uint maxRange)
        {
            m_MaxRange = maxRange;
        }

        public void SetScores(IScore[] scores)
        {
            m_Scores = scores;
        }

        public void SetTitle(string title)
        {
            m_Title = title;
        }

        public string[] GetUserFilter()
        {
            return m_UserIDs;
        }

        public string id { get; set; }
        public UserScope userScope { get; set; }
        public Range range { get; set; }
        public TimeScope timeScope { get; set; }
        public IScore localUserScore { get { return m_LocalUserScore; } }
        public uint maxRange { get { return m_MaxRange; } }
        public IScore[] scores { get { return m_Scores; } }
        public string title { get { return m_Title; } }
    }
}

namespace UnityEngine.SocialPlatforms
{
    using UnityEngine.SocialPlatforms.Impl;

    public class Local : ISocialPlatform
    {
        static LocalUser m_LocalUser = null;
        List<UserProfile> m_Friends = new List<UserProfile>();
        List<UserProfile> m_Users = new List<UserProfile>();
        List<AchievementDescription> m_AchievementDescriptions = new List<AchievementDescription>();
        List<Achievement> m_Achievements = new List<Achievement>();
        List<Leaderboard> m_Leaderboards = new List<Leaderboard>();
        Texture2D m_DefaultTexture;

        public ILocalUser localUser
        {
            get
            {
                if (m_LocalUser == null)
                    m_LocalUser = new LocalUser();
                return m_LocalUser;
            }
        }

        void ISocialPlatform.Authenticate(ILocalUser user, System.Action<bool> callback)
        {
            LocalUser thisUser = (LocalUser)user;
            m_DefaultTexture = CreateDummyTexture(32, 32);
            PopulateStaticData();
            thisUser.SetAuthenticated(true);
            thisUser.SetUnderage(false);
            thisUser.SetUserID("1000");
            thisUser.SetUserName("Lerpz");
            thisUser.SetImage(m_DefaultTexture);
            if (callback != null)
                callback(true);
        }

        void ISocialPlatform.Authenticate(ILocalUser user, System.Action<bool, string> callback)
        {
            ((ISocialPlatform)this).Authenticate(user, success => callback(success, null));
        }

        void ISocialPlatform.LoadFriends(ILocalUser user, System.Action<bool> callback)
        {
            if (!VerifyUser()) return;
            ((LocalUser)user).SetFriends(m_Friends.ToArray());
            if (callback != null)
                callback(true);
        }

        public void LoadUsers(string[] userIDs, Action<IUserProfile[]> callback)
        {
            List<UserProfile> matches = new List<UserProfile>();
            if (!VerifyUser()) return;
            foreach (string userId in userIDs)
            {
                foreach (UserProfile user in m_Users)
                    if (user.id == userId)
                        matches.Add(user);
                foreach (UserProfile user in m_Friends)
                    if (user.id == userId)
                        matches.Add(user);
            }
            callback(matches.ToArray());
        }

        public void ReportProgress(string id, double progress, System.Action<bool> callback)
        {
            if (!VerifyUser()) return;
            // Update achievement if it's already been reported
            foreach (Achievement achoo in m_Achievements)
            {
                // TODO: Only allow increase in progress, figure out if xbox/gc report errors when lower progress is reported
                if (achoo.id == id && achoo.percentCompleted <= progress)
                {
                    if (progress >= 100.0)
                        achoo.SetCompleted(true);
                    achoo.SetHidden(false);
                    achoo.SetLastReportedDate(DateTime.Now);
                    achoo.percentCompleted = progress;
                    if (callback != null)
                        callback(true);
                    // No need to process this report any more
                    return;
                }
            }

            // If it's not been reported already check if it's a valid achievement description ID and add it, else it's an error
            foreach (AchievementDescription achoo in m_AchievementDescriptions)
            {
                // TODO: Only allow increase in progress, figure out if xbox/gc report errors when lower progress is reported
                if (achoo.id == id)
                {
                    bool completed = (progress >= 100.0 ? true : false);
                    Achievement newAchievement = new Achievement(id, progress, completed, false, DateTime.Now);
                    m_Achievements.Add(newAchievement);
                    if (callback != null)
                        callback(true);
                    return;
                }
            }

            Debug.LogError("Achievement ID not found");
            if (callback != null)
                callback(false);
        }

        public void LoadAchievementDescriptions(System.Action<IAchievementDescription[]> callback)
        {
            if (!VerifyUser()) return;
            if (callback != null)
                callback(m_AchievementDescriptions.ToArray());
        }

        public void LoadAchievements(System.Action<IAchievement[]> callback)
        {
            if (!VerifyUser()) return;
            if (callback != null)
                callback(m_Achievements.ToArray());
        }

        public void ReportScore(Int64 score, string board, System.Action<bool> callback)
        {
            if (!VerifyUser()) return;
            foreach (Leaderboard current in m_Leaderboards)
            {
                if (current.id == board)
                {
                    // TODO: IIRC GameCenter only adds scores if they are higher than the users previous score, maybe mirror this here.
                    List<Score> scores = new List<Score>((Score[])current.scores);
                    scores.Add(new Score(board, score, localUser.id, DateTime.Now, score + " points", 0));
                    current.SetScores(scores.ToArray());
                    if (callback != null)
                        callback(true);
                    return;
                }
            }
            Debug.LogError("Leaderboard not found");
            if (callback != null)
                callback(false);
        }

        public void LoadScores(string leaderboardID, Action<IScore[]> callback)
        {
            if (!VerifyUser()) return;
            foreach (Leaderboard current in m_Leaderboards)
            {
                if (current.id == leaderboardID)
                {
                    SortScores(current);
                    if (callback != null)
                        callback(current.scores);
                    return;
                }
            }
            Debug.LogError("Leaderboard not found");
            if (callback != null)
                callback(new Score[0]);
        }

        void ISocialPlatform.LoadScores(ILeaderboard board, System.Action<bool> callback)
        {
            if (!VerifyUser()) return;
            // Fill in fake server side data on leaderboard
            Leaderboard thisBoard = (Leaderboard)board;
            foreach (Leaderboard lb in m_Leaderboards)
            {
                // TODO: In the real world the board might have thousands of scores but only 100
                // are returned, for now we can assume they are less than 100 here
                // TODO: Apply the filters which the leaderboard has, right now it always returns everything found
                if (lb.id == thisBoard.id)
                {
                    thisBoard.SetTitle(lb.title);
                    thisBoard.SetScores(lb.scores);
                    thisBoard.SetMaxRange((uint)lb.scores.Length);
                }
            }
            SortScores(thisBoard);
            SetLocalPlayerScore(thisBoard);
            if (callback != null)
                callback(true);
        }

        bool ISocialPlatform.GetLoading(ILeaderboard board)
        {
            if (!VerifyUser()) return false;
            return ((Leaderboard)board).loading;
        }

        // TODO: Sorting seems to be broken...
        private void SortScores(Leaderboard board)
        {
            List<Score> scores = new List<Score>((Score[])board.scores);
            scores.Sort(delegate(Score s1, Score s2)
                {
                    return s2.value.CompareTo(s1.value);
                });
            for (int i = 0; i < scores.Count; i++)
                scores[i].SetRank(i + 1);
        }

        private void SetLocalPlayerScore(Leaderboard board)
        {
            foreach (Score score in board.scores)
            {
                if (score.userID == localUser.id)
                {
                    board.SetLocalUserScore(score);
                    break;
                }
            }
        }

        public void ShowAchievementsUI()
        {
            Debug.Log("ShowAchievementsUI not implemented");
        }

        public void ShowLeaderboardUI()
        {
            Debug.Log("ShowLeaderboardUI not implemented");
        }

        public ILeaderboard CreateLeaderboard()
        {
            Leaderboard lb = new Leaderboard();
            return (ILeaderboard)lb;
        }

        public IAchievement CreateAchievement()
        {
            Achievement achoo = new Achievement();
            return (IAchievement)achoo;
        }

        private bool VerifyUser()
        {
            if (!localUser.authenticated)
            {
                Debug.LogError("Must authenticate first");
                return false;
            }
            return true;
        }

        private void PopulateStaticData()
        {
            m_Friends.Add(new UserProfile("Fred", "1001", true, UserState.Online, m_DefaultTexture));
            m_Friends.Add(new UserProfile("Julia", "1002", true, UserState.Online, m_DefaultTexture));
            m_Friends.Add(new UserProfile("Jeff", "1003", true, UserState.Online, m_DefaultTexture));
            m_Users.Add(new UserProfile("Sam", "1004", false, UserState.Offline, m_DefaultTexture));
            m_Users.Add(new UserProfile("Max", "1005", false, UserState.Offline, m_DefaultTexture));
            m_AchievementDescriptions.Add(new AchievementDescription("Achievement01", "First achievement", m_DefaultTexture, "Get first achievement", "Received first achievement", false, 10));
            m_AchievementDescriptions.Add(new AchievementDescription("Achievement02", "Second achievement", m_DefaultTexture, "Get second achievement", "Received second achievement", false, 20));
            m_AchievementDescriptions.Add(new AchievementDescription("Achievement03", "Third achievement", m_DefaultTexture, "Get third achievement", "Received third achievement", false, 15));
            Leaderboard board = new Leaderboard();
            board.SetTitle("High Scores");
            board.id = "Leaderboard01";
            List<Score> scores = new List<Score>();
            scores.Add(new Score("Leaderboard01", 300, "1001", DateTime.Now.AddDays(-1), "300 points", 1));
            scores.Add(new Score("Leaderboard01", 255, "1002", DateTime.Now.AddDays(-1), "255 points", 2));
            scores.Add(new Score("Leaderboard01", 55, "1003", DateTime.Now.AddDays(-1), "55 points", 3));
            scores.Add(new Score("Leaderboard01", 10, "1004", DateTime.Now.AddDays(-1), "10 points", 4));
            board.SetScores(scores.ToArray());
            m_Leaderboards.Add(board);
        }

        private Texture2D CreateDummyTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    Color color = (x & y) > 0 ? Color.white : Color.gray;
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return texture;
        }
    }
}
