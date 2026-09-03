// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    using UnityEngine.SocialPlatforms;

    // A facade for the social API namespace, no state, only helper functions which delegate into others
    ///<summary>Generic access to the Social API.</summary>
    ///<remarks>
    ///  <see cref="Social.Active" /> can be used to target a specific social platform implementation. All
    ///platforms default to the Local implementation which can be used for testing.
    ///See [Social API Reference Manual](xref:net-SocialAPI) for an overview.
    ///
    ///The <see cref="Social" /> class should always be used as an entry point. It contains
    ///helper functions for accessing the current active implementation and always
    ///uses the interfaces of the other Social API classes. This way it is easier
    ///to use versions of the interfaces which have been extended beyond the generic API by the implementation.
    ///
    ///There are various classes associated with the Social API and all of these reside
    ///in the <see cref="UnityEngine.SocialPlatforms" /> namespace. You need to import/use this namespace in order to use these classes.</remarks>
    [Obsolete("Social is deprecated and will be removed in a future release.", false)]
    public static class Social
    {
        ///<summary>This is the currently active social platform.</summary>
        ///<remarks>If not explicitly set, a default is picked depending on the target platform.</remarks>
        public static ISocialPlatform Active
        {
            get { return ActivePlatform.Instance; }
            set { ActivePlatform.Instance = value; }
        }

        ///<summary>The local user (potentially not logged in).</summary>
        ///<remarks>Until the user logs in or authenticates themself, the profile data will be invalid and no other Social API functionality will work.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Social.localUser.Authenticate(success => {
        ///            if (success)
        ///            {
        ///                Debug.Log("Authentication successful");
        ///                string userInfo = "Username: " + Social.localUser.userName +
        ///                    "\nUser ID: " + Social.localUser.id +
        ///                    "\nIsUnderage: " + Social.localUser.underage;
        ///                Debug.Log(userInfo);
        ///            }
        ///            else
        ///                Debug.Log("Authentication failed");
        ///        });
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static ILocalUser localUser { get { return Active.localUser; } }

        ///<summary>Load the user profiles associated with the given array of user IDs.</summary>
        public static void LoadUsers(string[] userIDs, Action<IUserProfile[]> callback)
        {
            Active.LoadUsers(userIDs, callback);
        }

        ///<summary>Reports the progress of an achievement.</summary>
        ///<remarks>The achievement ID number must match an achievement description associated with this application. Reporting a progress of 0.0 usually means the achievement can be shown if it was hidden before. Depending on the platform, partial progress cannot always be reported, in which case 100.0 is the only other value which can be used.</remarks>
        public static void ReportProgress(string achievementID, double progress, Action<bool> callback)
        {
            Active.ReportProgress(achievementID, progress, callback);
        }

        ///<summary>Loads the achievement descriptions associated with this application.</summary>
        ///<remarks>This is usually set up outside Unity on some external service provided by the implementation provider.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.SocialPlatforms;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Social.LoadAchievementDescriptions(descriptions => {
        ///            if (descriptions.Length > 0)
        ///            {
        ///                Debug.Log("Got " + descriptions.Length + " achievement descriptions");
        ///                string achievementDescriptions = "Achievement Descriptions:\n";
        ///                foreach (IAchievementDescription ad in descriptions)
        ///                {
        ///                    achievementDescriptions += "\t" +
        ///                        ad.id + " " +
        ///                        ad.title + " " +
        ///                        ad.unachievedDescription + "\n";
        ///                }
        ///                Debug.Log(achievementDescriptions);
        ///            }
        ///            else
        ///                Debug.Log("Failed to load achievement descriptions");
        ///        });
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void LoadAchievementDescriptions(Action<IAchievementDescription[]> callback)
        {
            Active.LoadAchievementDescriptions(callback);
        }

        ///<summary>Load the achievements the logged in user has already achieved or reported progress on.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.SocialPlatforms;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Social.LoadAchievements(achievements => {
        ///            if (achievements.Length > 0)
        ///            {
        ///                Debug.Log("Got " + achievements.Length + " achievement instances");
        ///                string myAchievements = "My achievements:\n";
        ///                foreach (IAchievement achievement in achievements)
        ///                {
        ///                    myAchievements += "\t" +
        ///                        achievement.id + " " +
        ///                        achievement.percentCompleted + " " +
        ///                        achievement.completed + " " +
        ///                        achievement.lastReportedDate + "\n";
        ///                }
        ///                Debug.Log(myAchievements);
        ///            }
        ///            else
        ///                Debug.Log("No achievements returned");
        ///        });
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void LoadAchievements(Action<IAchievement[]> callback)
        {
            Active.LoadAchievements(callback);
        }

        ///<summary>Report a score to a specific leaderboard.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.SocialPlatforms;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void ReportScore(long score, string leaderboardID)
        ///    {
        ///        Debug.Log("Reporting score " + score + " on leaderboard " + leaderboardID);
        ///        Social.ReportScore(score, leaderboardID, success => {
        ///            Debug.Log(success ? "Reported score successfully" : "Failed to report score");
        ///        });
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void ReportScore(Int64 score, string board, Action<bool> callback)
        {
            Active.ReportScore(score, board, callback);
        }

        ///<summary>Load a default set of scores from the given leaderboard.</summary>
        ///<remarks>This uses default leaderboard parameters.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.SocialPlatforms;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Social.LoadScores("Leaderboard01", scores => {
        ///            if (scores.Length > 0)
        ///            {
        ///                Debug.Log("Got " + scores.Length + " scores");
        ///                string myScores = "Leaderboard:\n";
        ///                foreach (IScore score in scores)
        ///                    myScores += "\t" + score.userID + " " + score.formattedValue + " " + score.date + "\n";
        ///                Debug.Log(myScores);
        ///            }
        ///            else
        ///                Debug.Log("No scores loaded");
        ///        });
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void LoadScores(string leaderboardID, Action<IScore[]> callback)
        {
            Active.LoadScores(leaderboardID, callback);
        }

        ///<summary>Create an ILeaderboard instance.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.SocialPlatforms;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // Use this for initialization
        ///    void Start()
        ///    {
        ///        ILeaderboard leaderboard = Social.CreateLeaderboard();
        ///        leaderboard.id = "Leaderboard012";
        ///        leaderboard.LoadScores(result =>
        ///        {
        ///            Debug.Log("Received " + leaderboard.scores.Length + " scores");
        ///            foreach (IScore score in leaderboard.scores)
        ///                Debug.Log(score);
        ///        });
        ///    }
        ///
        ///    // Update is called once per frame
        ///    void Update()
        ///    {
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.SocialPlatforms;
        ///using System.Collections;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    ILeaderboard m_Leaderboard;
        ///
        ///    void DoLeaderboard()
        ///    {
        ///        m_Leaderboard = Social.CreateLeaderboard();
        ///        m_Leaderboard.id = "Leaderboard01";
        ///        m_Leaderboard.LoadScores(result => DidLoadLeaderboard(result));
        ///    }
        ///
        ///    void DidLoadLeaderboard(bool result)
        ///    {
        ///        Debug.Log("Received " + m_Leaderboard.scores.Length + " scores");
        ///        foreach (IScore score in m_Leaderboard.scores)
        ///            Debug.Log(score);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static ILeaderboard CreateLeaderboard()
        {
            return Active.CreateLeaderboard();
        }

        ///<summary>Create an IAchievement instance.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.SocialPlatforms;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Example()
        ///    {
        ///        IAchievement achievement = Social.CreateAchievement();
        ///        achievement.id = "Achievement01";
        ///        achievement.percentCompleted = 100.0;
        ///        achievement.ReportProgress(result => {
        ///            if (result)
        ///                Debug.Log("Successfully reported progress");
        ///            else
        ///                Debug.Log("Failed to report progress");
        ///        });
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static IAchievement CreateAchievement()
        {
            return Active.CreateAchievement();
        }

        ///<summary>Show a default/system view of the games achievements.</summary>
        public static void ShowAchievementsUI()
        {
            Active.ShowAchievementsUI();
        }

        ///<summary>Show a default/system view of the games leaderboards.</summary>
        public static void ShowLeaderboardUI()
        {
            Active.ShowLeaderboardUI();
        }
    }
}

namespace UnityEngine.SocialPlatforms
{
    // The state of the current active social implementation

    [Obsolete("ActivePlatform is deprecated and will be removed in a future release.", false)]
    internal static partial class ActivePlatform
    {
        [AutoStaticsCleanupOnCodeReload]
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
            return new UnityEngine.SocialPlatforms.Local();
        }
    }

    ///<summary>The generic Social API interface which implementations must inherit.</summary>
    ///<remarks>See <see cref="Social" /> for details on usage.</remarks>
    [Obsolete("ISocialPlatform is deprecated and will be removed in a future release.", false)]
    public interface ISocialPlatform
    {
        ///<summary>See Social.localUser.</summary>
        ILocalUser localUser { get; }

        ///<summary>See <see cref="Social.LoadUsers" />.</summary>
        void LoadUsers(string[] userIDs, Action<IUserProfile[]> callback);

        ///<summary>See <see cref="Social.ReportProgress" />.</summary>
        void ReportProgress(string achievementID, double progress, Action<bool> callback);
        ///<summary>See <see cref="Social.LoadAchievementDescriptions" />.</summary>
        void LoadAchievementDescriptions(Action<IAchievementDescription[]> callback);
        ///<summary>See <see cref="Social.LoadAchievements" />.</summary>
        void LoadAchievements(Action<IAchievement[]> callback);
        ///<summary>See <see cref="Social.CreateAchievement" />.</summary>
        IAchievement CreateAchievement();

        ///<summary>See <see cref="Social.ReportScore" />.</summary>
        void ReportScore(Int64 score, string board, Action<bool> callback);
        ///<summary>See <see cref="Social.LoadScores" />.</summary>
        void LoadScores(string leaderboardID, Action<IScore[]> callback);
        ///<summary>See <see cref="Social.CreateLeaderboard" />.</summary>
        ILeaderboard CreateLeaderboard();

        ///<summary>See <see cref="Social.ShowAchievementsUI" />.</summary>
        void ShowAchievementsUI();
        ///<summary>See <see cref="Social.ShowLeaderboardUI" />.</summary>
        void ShowLeaderboardUI();

        // ===> These should be explicitly implemented <===
        ///<exclude />
        void Authenticate(ILocalUser user, Action<bool> callback);
        void Authenticate(ILocalUser user, Action<bool, string> callback);
        ///<exclude />
        void LoadFriends(ILocalUser user, Action<bool> callback);
        ///<summary>See <see cref="Social.LoadScores" />.</summary>
        void LoadScores(ILeaderboard board, Action<bool> callback);
        ///<exclude />
        bool GetLoading(ILeaderboard board);
    }

    ///<summary>Represents the local or currently logged in user.</summary>
    [Obsolete("ILocalUser is deprecated and will be removed in a future release.", false)]
    public interface ILocalUser : IUserProfile
    {
        ///<summary>Authenticate the local user to the current active Social API implementation and fetch his profile data.</summary>
        ///<remarks>This should be done before any other calls into the API. Depending on the platform, this might trigger a potentially blocking dialog for providing login details.
        ///
        ///On certain platforms (including but not limited to iOS and tvOS), the callback is only invoked on the first call to Authenticate(). Subsequent calls to Authenticate() on such platforms results in no callback being triggered. This can occur if, for example, the user or the OS cancels the authentication operation before it has completed. Please ensure you test for this situation.</remarks>
        ///<param name="callback">Callback that is called whenever the authentication operation is finished. The first parameter is a Boolean identifying whether the authentication operation was successful. The optional second argument contains a string identifying any errors (if available) if the operation was unsuccessful.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.SocialPlatforms;
        ///using System.Collections;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Social.localUser.Authenticate(success => {
        ///            if (success)
        ///            {
        ///                Debug.Log("Authentication successful");
        ///                string userInfo = "Username: " + Social.localUser.userName +
        ///                    "\nUser ID: " + Social.localUser.id +
        ///                    "\nIsUnderage: " + Social.localUser.underage;
        ///                Debug.Log(userInfo);
        ///            }
        ///            else
        ///                Debug.Log("Authentication failed");
        ///        });
        ///    }
        ///}
        ///]]></code>
        ///</example>
        void Authenticate(Action<bool> callback);
        ///<summary>Authenticate the local user to the current active Social API implementation and fetch his profile data.</summary>
        ///<remarks>This should be done before any other calls into the API. Depending on the platform, this might trigger a potentially blocking dialog for providing login details.
        ///
        ///On certain platforms (including but not limited to iOS and tvOS), the callback is only invoked on the first call to Authenticate(). Subsequent calls to Authenticate() on such platforms results in no callback being triggered. This can occur if, for example, the user or the OS cancels the authentication operation before it has completed. Please ensure you test for this situation.</remarks>
        ///<param name="callback">Callback that is called whenever the authentication operation is finished. The first parameter is a Boolean identifying whether the authentication operation was successful. The optional second argument contains a string identifying any errors (if available) if the operation was unsuccessful.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.SocialPlatforms;
        ///using System.Collections;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Social.localUser.Authenticate(success => {
        ///            if (success)
        ///            {
        ///                Debug.Log("Authentication successful");
        ///                string userInfo = "Username: " + Social.localUser.userName +
        ///                    "\nUser ID: " + Social.localUser.id +
        ///                    "\nIsUnderage: " + Social.localUser.underage;
        ///                Debug.Log(userInfo);
        ///            }
        ///            else
        ///                Debug.Log("Authentication failed");
        ///        });
        ///    }
        ///}
        ///]]></code>
        ///</example>
        void Authenticate(Action<bool, string> callback);

        ///<summary>Fetches the friends list of the logged in user. The friends list on the <see cref="ISocialPlatform.localUser">Social.localUser</see> instance is populated if this call succeeds.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.SocialPlatforms;
        ///using System.Collections;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Social.localUser.LoadFriends(success => {
        ///            Debug.Log(success ? "Loaded " + Social.localUser.friends.Length + " friends" : "Loading friends failed");
        ///        });
        ///    }
        ///}
        ///]]></code>
        ///</example>
        void LoadFriends(Action<bool> callback);

        ///<summary>The users friends list.</summary>
        IUserProfile[] friends { get; }
        ///<summary>Checks if the current user has been authenticated.</summary>
        ///<remarks>If not, they will need to log on.</remarks>
        bool authenticated { get; }
        ///<summary>Is the user underage?</summary>
        bool underage { get; }
    }

    ///<summary>User presence state.</summary>
    [Obsolete("UserState is deprecated and will be removed in a future release.", false)]
    public enum UserState
    {
        ///<summary>The user is online.</summary>
        Online,
        ///<summary>The user is online but away from their computer.</summary>
        OnlineAndAway,
        ///<summary>The user is online but set their status to busy.</summary>
        OnlineAndBusy,
        ///<summary>The user is offline.</summary>
        Offline,
        ///<summary>The user is playing a game.</summary>
        Playing
    }

    ///<summary>Represents generic user instances, like friends of the local user.</summary>
    [Obsolete("IUserProfile is deprecated and will be removed in a future release.", false)]
    public interface IUserProfile
    {
        ///<summary>This user's username or alias.</summary>
        string userName { get; }
        ///<summary>This user's global unique identifier.</summary>
        ///<remarks>For devices running iOS version 12.4 and later, <see cref="IUserProfile.id" /> returns &lt;a href="https://developer.apple.com/documentation/gamekit/gkplayer/3174857-teamplayerid?language=objc"&gt;GKPlayer.teamPlayerID&lt;/a&gt;.
        ///
        ///For devices running iOS version 12.3 and earlier, <see cref="IUserProfile.id" /> returns &lt;a href="https://developer.apple.com/documentation/gamekit/gkplayer/1521127-playerid?language=objc"&gt;GKPlayer.playerID&lt;/a&gt;.
        ///
        ///Use <see cref="IUserProfile.id" /> instead of <see cref="UnityEngine.SocialPlatforms.Impl.UserProfile.legacyId" />. Only use <see cref="UnityEngine.SocialPlatforms.Impl.UserProfile.legacyId" /> if you need to access &lt;a href="https://developer.apple.com/documentation/gamekit/gkplayer/1521127-playerid?language=objc"&gt;GKPlayer.playerID&lt;/a&gt; to migrate player data in your existing project.</remarks>
        string id { get; }
        ///<summary>Is this user a friend of the current logged in user?</summary>
        bool isFriend { get; }
        ///<summary>Presence state of the user.</summary>
        UserState state { get; }
        ///<summary>Avatar image of the user.</summary>
        Texture2D image { get; }
    }

    ///<summary>Information for a user's achievement.</summary>
    ///<remarks>This defines the relation between a particular achievement (described by
    ///<see cref="IAchievementDescription">IAchievementDescription</see>) and the local user, what progress they have, last date they reported progress and so on.
    ///
    ///Use <see cref="Social.CreateAchievement" /> to create an instance of this object.</remarks>
    [Obsolete("IAchievement is deprecated and will be removed in a future release.", false)]
    public interface IAchievement
    {
        ///<summary>Send notification about progress on this achievement.</summary>
        void ReportProgress(Action<bool> callback);

        ///<summary>The unique identifier of this achievement.</summary>
        string id { get; set; }
        ///<summary>Progress for this achievement.</summary>
        ///<remarks>Progress towards an achievement can be reported, when this reaches 100.0 it is considered complete.</remarks>
        double percentCompleted { get; set; }
        ///<summary>Set to true when percentCompleted is 100.0.</summary>
        bool completed { get; }
        ///<summary>This achievement is currently hidden from the user.</summary>
        bool hidden { get; }
        ///<summary>Set by server when percentCompleted is updated.</summary>
        DateTime lastReportedDate { get; }
    }

    ///<summary>Static data describing an achievement.</summary>
    ///<remarks>Retreive the achievement descriptions by using <see cref="Social.LoadAchievementDescriptions" />.</remarks>
    [Obsolete("IAchievementDescription is deprecated and will be removed in a future release.", false)]
    public interface IAchievementDescription
    {
        ///<summary>Unique identifier for this achievement description.</summary>
        string id { get; set; }
        ///<summary>Human readable title.</summary>
        string title { get; }
        ///<summary>Image representation of the achievement.</summary>
        Texture2D image { get; }
        ///<summary>Description when the achivement is completed.</summary>
        string achievedDescription { get; }
        ///<summary>Description when the achivement has not been completed.</summary>
        string unachievedDescription { get; }
        ///<summary>Hidden achievement are not shown in the list until the percentCompleted has been touched (even if it's 0.0).</summary>
        ///<remarks>Can be used for achievements which are enabled when an addon is bought.</remarks>
        bool hidden { get; }
        ///<summary>Point value of this achievement.</summary>
        int points { get; }
    }

    ///<summary>A game score.</summary>
    ///<remarks>It can be received from a ILeaderboard instance or using the <see cref="Social.LoadScores" /> call which uses the default leaderboard filters.</remarks>
    [Obsolete("IScore is deprecated and will be removed in a future release.", false)]
    public interface IScore
    {
        ///<summary>Report this score instance.</summary>
        void ReportScore(Action<bool> callback);

        ///<summary>The ID of the leaderboard this score belongs to.</summary>
        string leaderboardID { get; set; }
        // TODO: This is just an int64 here, but should be able to represent all supported formats, except for float type scores ...
        ///<summary>The score value achieved.</summary>
        Int64 value { get; set; }
        ///<summary>The date the score was achieved.</summary>
        DateTime date { get; }
        ///<summary>The correctly formatted value of the score, like X points or X kills.</summary>
        ///<remarks>You should not use the value parameter directly but this string instead.</remarks>
        string formattedValue { get; }
        ///<summary>The user who owns this score.</summary>
        ///<remarks>You can load the users information using <see cref="Social.LoadUsers" />.</remarks>
        string userID { get; }
        ///<summary>The rank or position of the score in the leaderboard.</summary>
        ///<remarks>Only valid when the score is retreived from a server.</remarks>
        int rank { get; }
    }

    ///<summary>The scope of the users searched through when querying the leaderboard.</summary>
    [Obsolete("UserScope is deprecated and will be removed in a future release.", false)]
    public enum UserScope
    {
        ///<exclude />
        Global = 0,
        ///<exclude />
        FriendsOnly
    }

    ///<summary>The scope of time searched through when querying the leaderboard.</summary>
    [Obsolete("TimeScope is deprecated and will be removed in a future release.", false)]
    public enum TimeScope
    {
        ///<exclude />
        Today = 0,
        ///<exclude />
        Week,
        ///<exclude />
        AllTime
    }

    ///<summary>The score range a leaderboard query should include.</summary>
    [Obsolete("Range is deprecated and will be removed in a future release.", false)]
    public struct Range
    {
        ///<summary>The rank of the first score which is returned.</summary>
        public int from;
        ///<summary>The total amount of scores retreived.</summary>
        public int count;

        ///<summary>Constructor for a score range, the range starts from a specific value and contains a maxium score count.</summary>
        ///<param name="fromValue">The minimum allowed value.</param>
        ///<param name="valueCount">The number of possible values.</param>
        public Range(int fromValue, int valueCount)
        {
            from = fromValue;
            count = valueCount;
        }
    }

    ///<summary>The leaderboard contains the scores of all players for a particular game.</summary>
    ///<remarks>Each game can have multiple leaderboards with different scores. A leaderboard object can be customized to perform a particular query. The leaderboard ID defines which leaderboard is being queried and there are filters to narrow down the results, <see cref="ILeaderboard.userScope"> UserScope</see>, <see cref="ILeaderboard.timeScope"> TimeScope</see>,  <see cref="ILeaderboard.range"> Range</see> and <see cref="ILeaderboard.SetUserFilter">SetUserFilter</see>.
    ///Use <see cref="Social.CreateLeaderboard" /> to create an instance of this object.</remarks>
    [Obsolete("ILeaderboard is deprecated and will be removed in a future release.", false)]
    public interface ILeaderboard
    {
        ///<summary>Only search for these user IDs.</summary>
        ///<remarks>This will ignore conflicting filters like the UserScope.</remarks>
        ///<param name="userIDs">List of user ids.</param>
        void SetUserFilter(string[] userIDs);
        ///<summary>Load scores according to the filters set on this leaderboard.</summary>
        void LoadScores(Action<bool> callback);
        ///<summary>The leaderboad is in the process of loading scores.</summary>
        bool loading { get; }

        ///<summary>Unique identifier for this leaderboard.</summary>
        string id { get; set; }
        ///<summary>The users scope searched by this leaderboard.</summary>
        UserScope userScope { get; set; }
        ///<summary>The rank range this leaderboard returns.</summary>
        Range range { get; set; }
        ///<summary>The time period/scope searched by this leaderboard.</summary>
        TimeScope timeScope { get; set; }
        ///<summary>The leaderboard score of the logged in user.</summary>
        IScore localUserScore { get; }
        ///<summary>The total amount of scores the leaderboard contains.</summary>
        uint maxRange { get; }
        ///<summary>The leaderboard scores returned by a query.</summary>
        IScore[] scores { get; }
        ///<summary>The human readable title of this leaderboard.</summary>
        string title { get; }
    }
}
