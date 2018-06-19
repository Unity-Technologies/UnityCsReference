// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/CrashReport.bindings.h")]
    public sealed class CrashReport
    {
        static List<CrashReport> internalReports;
        static object reportsLock = new object();

        static int Compare(CrashReport c1, CrashReport c2)
        {
            long t1 = c1.time.Ticks;
            long t2 = c2.time.Ticks;
            if (t1 > t2)
                return 1;
            if (t1 < t2)
                return -1;
            return 0;
        }

        static void PopulateReports()
        {
            lock (reportsLock)
            {
                if (internalReports != null)
                    return;

                string[] ids = GetReports();
                internalReports = new List<CrashReport>(ids.Length);
                foreach (var id in ids)
                {
                    double secondsSinceUnixEpoch;
                    string text = GetReportData(id, out secondsSinceUnixEpoch);
                    DateTime time = new DateTime(1970, 1, 1).AddSeconds(secondsSinceUnixEpoch);
                    internalReports.Add(new CrashReport(id, time, text));
                }
                internalReports.Sort(Compare);
            }
        }

        public static CrashReport[] reports
        {
            get
            {
                PopulateReports();
                lock (reportsLock)
                {
                    return internalReports.ToArray();
                }
            }
        }

        public static CrashReport lastReport
        {
            get
            {
                PopulateReports();
                lock (reportsLock)
                {
                    if (internalReports.Count > 0)
                    {
                        return internalReports[internalReports.Count - 1];
                    }
                }

                return null;
            }
        }

        public static void RemoveAll()
        {
            foreach (var report in reports)
                report.Remove();
        }

        readonly string id;
        public readonly DateTime time;
        public readonly string text;

        CrashReport(string id, DateTime time, string text)
        {
            this.id = id;
            this.time = time;
            this.text = text;
        }

        public void Remove()
        {
            if (RemoveReport(id))
            {
                lock (reportsLock)
                {
                    internalReports.Remove(this);
                }
            }
        }

        [FreeFunction(Name = "CrashReport_Bindings::GetReports", IsThreadSafe = true)]
        extern private static string[] GetReports();

        [FreeFunction(Name = "CrashReport_Bindings::GetReportData", IsThreadSafe = true)]
        extern private static string GetReportData(string id, out double secondsSinceUnixEpoch);

        [FreeFunction(Name = "CrashReport_Bindings::RemoveReport", IsThreadSafe = true)]
        extern private static bool RemoveReport(string id);
    }
}
