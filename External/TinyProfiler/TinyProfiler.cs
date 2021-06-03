using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using NiceIO;

namespace Unity.TinyProfiling
{
	class TinyProfiler
	{
		[ThreadStatic] private static List<TimedSection> ts_Sections;
		[ThreadStatic] private static Stack<int> ts_OpenSections;

		static bool m_Started;
		private static Thread s_StartingThread;
	    public static int MinDuration = (int)(TimedSection.TicksPerSecond/ 3000);

		static NPath s_Filename;
		private static string s_ProcessName;
		private static int s_ProcessSortIndex;
		private static readonly List<ThreadContext> s_ThreadContexts = new List<ThreadContext>();
		private static HashSet<NPath> s_ExternalTraceEventsFiles = new HashSet<NPath>();

		public class ThreadContext
		{
			public List<TimedSection> Sections;
			public Stack<int> OpenSections;
			public int ThreadID;
			public string ThreadName;
		}

		public struct TimedSection
		{
			public string Label;
			public string Details;
		    public Int64 StartInTicks;
			public Int64 DurationInTicks;
			
			// 400ms   
			//  60 ticks per second.
			// microsecond per tick = 1000 * 1000 / frequency

			static TimedSection()
			{
				if (Environment.OSVersion.Platform == PlatformID.Unix)
					TicksPerSecond = 1000;
				else
				{
					QueryPerformanceFrequency(out var frequency);
					TicksPerSecond = frequency;
				}
			}

			public TimedSection(string label, string details)
			{
				Label = label;
				Details = details;
				StartInTicks = GetProfileTimeUS();
				DurationInTicks = 0;
			}

			public void Close()
			{
				var timestamp = GetProfileTimeUS();
				var duration = timestamp - StartInTicks;
				DurationInTicks = duration;
			}

			static long GetProfileTimeUS()
			{
				if (Environment.OSVersion.Platform == PlatformID.Unix)
					return DateTimeOffset.Now.ToUnixTimeMilliseconds();
				else
				{
					QueryPerformanceCounter(out var timestamp);
					return timestamp;
				}
			}
			public static long TicksPerSecond { get; }
			static double TicksPerMicroSecond => TicksPerSecond / (1000.0*1000.0);
			
			public double DurationInSeconds => DurationInTicks / TicksPerSecond;
			public double DurationInMicroSeconds => DurationInTicks / TicksPerMicroSecond;
            public double StartInMicroSeconds => StartInTicks / TicksPerMicroSecond;
            public string Summary { get { return Label + " " + Details + " (" + DurationInMicroSeconds/1000 + "ms)"; }}

            [DllImport("Kernel32.dll")] private static extern bool QueryPerformanceFrequency(out long frequency);
            [DllImport("Kernel32.dll")] private static extern bool QueryPerformanceCounter(out long ticks);
		}

		struct TimedSectionHandle : IDisposable
		{
			internal int m_Index;

			public void Dispose()
			{
				CloseSection(m_Index);
			}
		}

		public static IDisposable Section(string label, string details = "")
		{
			if (ts_Sections == null)
				InitializeProfilerForCurrentThread();

			if (!m_Started)
			{
				Start();
			}

		    var section = new TimedSection(label, details);
			var index = ts_Sections.Count;
			ts_Sections.Add(section);

			ts_OpenSections.Push(index);
			return new TimedSectionHandle { m_Index = index};
		}

		public static ReadOnlyCollection<ThreadContext> CaptureSnapshot()
		{
			return new List<ThreadContext>(s_ThreadContexts).AsReadOnly();
		}

		private static void InitializeProfilerForCurrentThread()
		{
			ts_Sections = new List<TimedSection>(5000);
			ts_OpenSections = new Stack<int>(50);
			lock (s_ThreadContexts)
				s_ThreadContexts.Add(new ThreadContext() {OpenSections = ts_OpenSections, Sections = ts_Sections, ThreadID = Thread.CurrentThread.ManagedThreadId, ThreadName = Thread.CurrentThread.Name});
		}

		private static void Start()
		{
			m_Started = true;
			s_StartingThread = Thread.CurrentThread;
		}

		public static void AddExternalTraceEventFile(NPath traceEventsFile)
		{
			s_ExternalTraceEventsFiles.Add(traceEventsFile);
		}
		
	    public static void Finish()
		{
			if (s_Filename != null)
				WriteReport(s_ProcessName, s_ProcessSortIndex, s_ExternalTraceEventsFiles);
			s_ExternalTraceEventsFiles.Clear();
			s_Filename = null;
			m_Started = false;
			foreach(var context in s_ThreadContexts)
			{
				context.OpenSections.Clear();
				context.Sections.Clear();
			}
		}

		private static void CloseSection(int index)
		{
			var last = ts_OpenSections.Pop();
			if (last != index)
				throw new ArgumentException($"TimedSection being closed {ts_Sections[index].Label} is not the most recently opened {ts_Sections[last].Label}");

            var section = ts_Sections[index];
            section.Close();
            ts_Sections[index] = section;


            // if we are the last section in the list, and we're too short, we just discard this section, so that the
            // resulting output will not have a lot of tiny noise sections
            if (ts_Sections.Count == index + 1 && section.DurationInTicks < MinDuration)
            {
                ts_Sections.RemoveAt(ts_Sections.Count - 1);
            }

            if (ts_OpenSections.Count == 0 && s_StartingThread == Thread.CurrentThread)
                Finish();
        }

        static void WriteReport(string processName, int sortIndex, IEnumerable<NPath> externalTraceEventFiles)
        {
            using (var writeStream = File.CreateText(s_Filename.ToString(SlashMode.Native)))
            {
                if (s_Filename.HasExtension("traceevents"))
                    TraceMaker.EmitRawTraceEvents(s_ThreadContexts, writeStream, processName, sortIndex, externalTraceEventFiles);
                else
                    TraceMaker.EmitProfileJsonFile(s_ThreadContexts, writeStream, processName, sortIndex, externalTraceEventFiles);
            }
        }

        public static List<TimedSection> GetCurrentThreadSections()
        {
            return ts_Sections;
        }

        // report is Google Chrome tracing viewer profiling file
        public static void ConfigureOutput(NPath reportFileName, string processName = "host", int processSortIndex = 0)
        {
            s_Filename = reportFileName;
            s_ProcessName = processName;
            s_ProcessSortIndex = processSortIndex;
        }
    }
}
