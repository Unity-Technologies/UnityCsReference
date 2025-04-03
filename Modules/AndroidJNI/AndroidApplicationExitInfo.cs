// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Android
{
    public enum ProcessImportance
    {
        /// <summary>
        /// <para>This process is running the foreground UI; that is, it is the thing currently at the top of the screen that the user is interacting with.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ActivityManager.RunningAppProcessInfo#IMPORTANCE_FOREGROUND">developer.android.com</seealso>
        /// </summary>
        Foreground = 100,

        /// <summary>
        /// <para>This process is running a foreground service, for example to perform music playback even while the user is not immediately in the app. This generally indicates that the process is doing something the user actively cares about.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ActivityManager.RunningAppProcessInfo#IMPORTANCE_FOREGROUND_SERVICE">developer.android.com</seealso>
        /// </summary>
        ForeGroundService = 125,

        /// <summary>
        /// <para>This process is running something that is actively visible to the user, though not in the immediate foreground. This may be running a window that is behind the current foreground (so paused and with its state saved, not interacting with the user, but visible to them to some degree); it may also be running other services under the system's control that it inconsiders important.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ActivityManager.RunningAppProcessInfo#IMPORTANCE_VISIBLE">developer.android.com</seealso>
        /// </summary>
        Visible = 200,

        /// <summary>
        /// <para>This process is not something the user is directly aware of, but is otherwise perceptible to them to some degree.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ActivityManager.RunningAppProcessInfo#IMPORTANCE_PERCEPTIBLE">developer.android.com</seealso>
        /// </summary>
        Perceptible = 230,

        /// <summary>
        /// <para>This process is running the foreground UI, but the device is asleep so it is not visible to the user. Though the system will try hard to keep its process from being killed, in all other ways we consider it a kind of cached process, with the limitations that go along with that state: network access, running background services, etc.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ActivityManager.RunningAppProcessInfo#IMPORTANCE_TOP_SLEEPING">developer.android.com</seealso>
        /// </summary>
        TopSleeping = 325,

        /// <summary>
        /// <para>This process is running an application that can not save its state, and thus can't be killed while in the background. This will be used with apps that have R.attr.cantSaveState set on their application tag.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ActivityManager.RunningAppProcessInfo#IMPORTANCE_CANT_SAVE_STATE">developer.android.com</seealso>
        /// </summary>
        CantSaveState = 350,

        /// <summary>
        /// <para>This process contains services that should remain running. These are background services apps have started, not something the user is aware of, so they may be killed by the system relatively freely (though it is generally desired that they stay running as long as they want to).</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ActivityManager.RunningAppProcessInfo#IMPORTANCE_SERVICE">developer.android.com</seealso>
        /// </summary>
        Service = 300,

        /// <summary>
        /// <para>This process process contains cached code that is expendable, not actively running any app components we care about.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ActivityManager.RunningAppProcessInfo#IMPORTANCE_CACHED">developer.android.com</seealso>
        /// </summary>
        Cached = 400,

        /// <summary>
        /// <para>This process does not exist.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ActivityManager.RunningAppProcessInfo#IMPORTANCE_GONE">developer.android.com</seealso>
        /// </summary>
        Gone = 1000
    }

    public enum ExitReason
    {
        /// <summary>
        /// <para>Application process died due to unknown reason.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_UNKNOWN">developer.android.com</seealso>
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// <para>Application process exit normally by itself, for example, via System.exit(int); getStatus() will specify the exit code.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_EXIT_SELF">developer.android.com</seealso>
        /// </summary>
        ExitSelf = 1,

        /// <summary>
        /// <para>Application process died due to the result of an OS signal; for example, OsConstants.SIGKILL; getStatus() will specify the signal number.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_SIGNALED">developer.android.com</seealso>
        /// </summary>
        Signaled = 2,

        /// <summary>
        /// <para>Application process was killed by the system low memory killer, meaning the system was under memory pressure at the time of kill.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_LOW_MEMORY">developer.android.com</seealso>
        /// </summary>
        LowMemory = 3,

        /// <summary>
        /// <para>Application process died because of an unhandled exception in Java code.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_CRASH">developer.android.com</seealso>
        /// </summary>
        Crash = 4,

        /// <summary>
        /// <para>Application process died because of a native code crash.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_CRASH_NATIVE">developer.android.com</seealso>
        /// </summary>
        CrashNative = 5,

        /// <summary>
        /// <para>Application process was killed due to being unresponsive (ANR).</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_ANR">developer.android.com</seealso>
        /// </summary>
        ANR = 6,

        /// <summary>
        /// <para>Application process was killed because of initialization failure, for example, it took too long to attach to the system during the start, or there was an error during initialization.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_INITIALIZATION_FAILURE">developer.android.com</seealso>
        /// </summary>
        InititalizationFailure = 7,

        /// <summary>
        /// <para>Application process was killed due to a runtime permission change.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_PERMISSION_CHANGE">developer.android.com</seealso>
        /// </summary>
        PermissionChange = 8,

        /// <summary>
        /// <para>Application process was killed by the system due to excessive resource usage.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_EXCESSIVE_RESOURCE_USAGE">developer.android.com</seealso>
        /// </summary>
        ExcessiveResourceUsage = 9,

        /// <summary>
        /// <para>Application process was killed because of the user request, for example, user clicked the "Force stop" button of the application in the Settings, or removed the application away from Recents.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_USER_REQUESTED">developer.android.com</seealso>
        /// </summary>
        UserRequested = 10,

        /// <summary>
        /// <para>Application process was killed, because the user it is running as on devices with mutlple users, was stopped.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_USER_STOPPED">developer.android.com</seealso>
        /// </summary>
        UserStopped = 11,

        /// <summary>
        /// <para>Application process was killed because its dependency was going away, for example, a stable content provider connection's client will be killed if the provider is killed.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_DEPENDENCY_DIED">developer.android.com</seealso>
        /// </summary>
        DependencyDied = 12,

        /// <summary>
        /// <para>Application process was killed by the system for various other reasons which are not by problems in apps and not actionable by apps, for example, the system just finished updates; getDescription() will specify the cause given by the system.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_OTHER">developer.android.com</seealso>
        /// </summary>
        Other = 13,

        /// <summary>
        /// <para>Application process was killed by App Freezer, for example, because it receives sync binder transactions while being frozen.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_FREEZER">developer.android.com</seealso>
        /// </summary>
        Freezer = 14,

        /// <summary>
        /// <para>Application process was killed because the app was disabled, or any of its component states have changed without PackageManager.DONT_KILL_APP</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_PACKAGE_STATE_CHANGE">developer.android.com</seealso>
        /// </summary>
        PackageStateChange = 15,

        /// <summary>
        /// <para>Application process was killed because it was updated.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#REASON_PACKAGE_UPDATED">developer.android.com</seealso>
        /// </summary>
        PackageUpdated = 16
    }

    public interface IApplicationExitInfo
    {
        /// <summary>
        /// <para>The human readable description of the process's death, given by the system; could be null.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#getDescription()">developer.android.com</seealso>
        /// </summary>
        /// <returns>string</returns>
        string description { get; }

        /// <summary>
        /// <para>Describe the kinds of special objects contained in this Parcelable instance's marshaled representation.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#describeContents()">developer.android.com</seealso>
        /// </summary>
        /// <returns>a bitmask indicating the set of special object types marshaled by this Parcelable object instance. Value is either 0 or CONTENTS_FILE_DESCRIPTOR</returns>
        int describeContents { get; }

        /// <summary>
        /// <para>Return the defining kernel user identifier, maybe different from getRealUid() and getPackageUid(), if an external service has the android:useAppZygote set to true and was bound with the flag Context.BIND_EXTERNAL_SERVICE - in this case, this field here will be the kernel user identifier of the external service provider.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#getDefiningUid()">developer.android.com</seealso>
        /// </summary>
        /// <returns>int</returns>
        int definingUid { get; }

        /// <summary>
        /// <para>The importance of the process that it used to have before the death.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#getImportance()">developer.android.com</seealso>
        /// </summary>
        /// <returns>ProcessImportance</returns>
        ProcessImportance importance { get; }

        /// <summary>
        /// <para>Similar to getRealUid(), it's the kernel user identifier that is assigned at the package installation time.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#getPackageUid()">developer.android.com</seealso>
        /// </summary>
        /// <returns>int</returns>
        int packageUid { get; }

        /// <summary>
        /// <para>The process id of the process that died.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#getPid()">developer.android.com</seealso>
        /// </summary>
        /// <returns>int</returns>
        int pid { get; }

        /// <summary>
        /// <para>The actual process name it was running with.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#getProcessName()">developer.android.com</seealso>
        /// </summary>
        /// <returns>String</returns>
        String processName { get; }

        /// <summary>
        /// <para>Return the state data set by calling ApplicationExitInfoProvider.setProcessStateSummary(byte[]) from the process before its death.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#getProcessStateSummary()">developer.android.com</seealso>
        /// </summary>
        /// <returns>byte[] containing the process-customized data. This value may be null.</returns>
        sbyte[] processStateSummary { get; }

        /// <summary>
        /// <para>Last proportional set size of the memory that the process had used in kB.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#getPss()">developer.android.com</seealso>
        /// </summary>
        /// <returns>long</returns>
        long pss { get; }

        /// <summary>
        /// <para>The kernel user identifier of the process, most of the time the system uses this to do access control checks.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#getRealUid()">developer.android.com</seealso>
        /// </summary>
        /// <returns>int</returns>
        int realUid { get; }

        /// <summary>
        /// <para>The reason code of the process's death.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#getReason()">developer.android.com</seealso>
        /// </summary>
        /// <returns>ExitReason</returns>
        ExitReason reason { get; }

        /// <summary>
        /// <para>Last resident set size of the memory that the process had used in kB.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#getRss()">developer.android.com</seealso>
        /// </summary>
        /// <returns>long</returns>
        long rss { get; }

        /// <summary>
        /// <para>The exit status argument of exit() if the application calls it, or the signal number if the application is signaled.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#getStatus()">developer.android.com</seealso>
        /// </summary>
        /// <returns>int</returns>
        int status { get; }

        /// <summary>
        /// <para>The timestamp of the process's death, in milliseconds since the epoch, as returned by System.currentTimeMillis(). Value is a non-negative timestamp measured as the number of milliseconds since 1970-01-01T00:00:00Z.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#getTimestamp()">developer.android.com</seealso>
        /// </summary>
        /// <returns>long Value is a non-negative timestamp measured as the number of milliseconds since 1970-01-01T00:00:00Z.</returns>
        long timestamp { get; }

        /// <summary>
        /// <para>Return the traces that was taken by the system prior to the death of the process; typically it'll be available when the reason is REASON_ANR, though if the process gets an ANR but recovers, and dies for another reason later, this trace will be included in the record of ApplicationExitInfo still.</para>
        /// <seealso href="https://developer.android.com/reference/android/app/ApplicationExitInfo#getTraceInputStream()">developer.android.com</seealso>
        /// </summary>
        /// <returns>byte[]</returns>
        byte[] trace { get; }

        /// <summary>
        /// <para>Return the trace data in string format</para>
        /// </summary>
        /// <returns>string</returns>
        public String traceAsString { get; }
    }


    public static class ApplicationExitInfoProvider
    {
        /// <summary>
        /// <para>Return a list of ApplicationExitInfo records containing the reasons for the most recent app deaths.</para>
        /// <seealso href="https://developer.android.com/reference/kotlin/android/app/ActivityManager#gethistoricalprocessexitreasons">developer.android.com</seealso>
        /// </summary>
        /// <param name="packageName">Optional, a null value means match all packages belonging to the caller's UID. If this package belongs to another UID, you must hold android.Manifest.permission.DUMP in order to retrieve it.</param>
        /// <param name="pid">A process ID that used to belong to this package but died later; a value of 0 means to ignore this parameter and return all matching records. Value is 0 or greater</param>
        /// <param name="maxNum">The maximum number of results to be returned; a value of 0 means to ignore this parameter and return all matching records Value is 0 or greater.</param>
        /// <returns>IApplicationExitInfo[] a list of ApplicationExitInfo records matching the criteria, sorted in the order from most recent to least recent. This value cannot be null.</returns>
        public static IApplicationExitInfo[] GetHistoricalProcessExitInfo(string packageName = null, int pid = 0, int maxNum = 0)
        {
            IApplicationExitInfo[] result = null;
            if (result == null)
                result = new IApplicationExitInfo[0];

            return result;
        }

        ///<summary>
        ///<para>Set custom state data for this process.It will be included in the record of ApplicationExitInfo on the death of the current calling process; the new process of the app can retrieve this state data by calling processStateSummary on the IApplicationExitInfo record returned by ApplicationExitInfoProvider.getHistoricalProcessExitReasons(String, int, int).</para>
        ///<seealso href="https://developer.android.com/reference/kotlin/android/app/ActivityManager#setprocessstatesummary">developer.android.com</seealso>
        ///</summary>
        ///<param name = "buffer" > The state data.To be advised, DO NOT include sensitive information/data (PII, SPII, or other sensitive user data) here.Maximum length is 128 bytes.</param>
        public static void SetProcessStateSummary(SByte[] buffer)
        {
        }
    }
}
