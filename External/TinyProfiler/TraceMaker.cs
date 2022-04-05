using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NiceIO;

namespace Unity.TinyProfiling
{
    // Produces Google Chrome tracing viewer compatible JSON output.
    // See format documentation at https://docs.google.com/document/d/1CvAClvFfyA5R-PhYUmn5OOQtYMH4h6I0nSsKchNAySU/edit
    class TraceMaker
    {
        public static void EmitProfileJsonFile(List<TinyProfiler.ThreadContext> threadContexts, StreamWriter textWriter, string processName, int processSortIndex, IEnumerable<NPath> externalTraceEventFiles)
        {
            textWriter.WriteLine("{");
            EmitInstructions(textWriter);
            textWriter.WriteLine("\"traceEvents\":[");

            EmitRawTraceEvents(threadContexts, textWriter, processName, processSortIndex, externalTraceEventFiles);

            //emit an empty dummy object so that the rest of the code does not have to worry about emitting trailing commas
            textWriter.WriteLine("{}");
            textWriter.WriteLine("\n],");

            EmitMetadata(textWriter);

            textWriter.WriteLine("}");
        }

        public static void EmitRawTraceEvents(List<TinyProfiler.ThreadContext> threadContexts, StreamWriter textWriter, string processName, int processSortIndex, IEnumerable<NPath> externalTraceEventFiles)
        {
            textWriter.Write("{ \"pid\": \"");
            textWriter.Write(processName);
            textWriter.Write("\", \"ph\":\"M\", \"name\": \"process_name\", \"args\": {\"name\": \"");
            textWriter.Write(Escape(processName));
            textWriter.WriteLine("\"} },");

            textWriter.Write("{ \"pid\": \"");
            textWriter.Write(processName);
            textWriter.Write("\", \"ph\":\"M\", \"name\": \"process_sort_index\", \"args\": {\"sort_index\": ");
            textWriter.Write(processSortIndex);
            textWriter.WriteLine("} },");

            foreach (var ctx in threadContexts.Where(t => t.Sections.Any()))
                EmitSingleThread(textWriter, ctx, processName);

            textWriter.Flush();
            foreach (var externalFile in externalTraceEventFiles ?? Array.Empty<NPath>())
            {
                if (!externalFile.FileExists())
                    continue;

                // Important: need to read this file as text mode, to avoid copying byte-order-marks.
                using (var stream = File.OpenText(externalFile.ToString(SlashMode.Native)))
                {
                    while (!stream.EndOfStream)
                        textWriter.WriteLine(stream.ReadLine());
                }
            }
        }

        static void EmitSingleThread(TextWriter textWriter, TinyProfiler.ThreadContext threadContext, string processName)
        {
            var threadID = threadContext.ThreadID;

            textWriter.Write("{ \"pid\": \"");
            textWriter.Write(processName);
            textWriter.Write("\", \"tid\": ");
            textWriter.Write(threadID);
            textWriter.Write(", \"ph\":\"M\", \"name\": \"thread_name\", \"args\": {\"name\": \"");
            textWriter.Write(Escape(threadContext.ThreadName));
            textWriter.WriteLine("\"} },");

            var timedSections = threadContext.Sections;
            for (var i = 0; i != timedSections.Count; i++)
            {
                var section = timedSections[i];
                var startUS = section.StartInMicroSeconds;
                var durUS = section.DurationInMicroSeconds;

                textWriter.Write("{ \"pid\": \"");
                textWriter.Write(processName);
                textWriter.Write("\", \"tid\": ");
                textWriter.Write(threadID);
                textWriter.Write(",\"ts\": ");
                textWriter.Write(startUS.ToString(CultureInfo.InvariantCulture));
                textWriter.Write(",\"dur\": ");
                textWriter.Write(durUS.ToString(CultureInfo.InvariantCulture));
                textWriter.Write(", \"ph\":\"X\", \"name\": \"");
                textWriter.Write(Escape(section.Label));
                textWriter.Write("\"");
                if (!string.IsNullOrEmpty(section.Details))
                {
                    textWriter.Write(", \"args\": { ");
                    //textWriter.Write("\", \"args\": { \"durationMS\": ");
                    //textWriter.Write(section.Duration.ToString(CultureInfo.InvariantCulture));
                    textWriter.Write("\"detail\": \"");
                    textWriter.Write(Escape(section.Details));
                    textWriter.WriteLine("\"}");
                }
                textWriter.WriteLine("},");
            }
        }

        static void EmitInstructions(TextWriter textWriter)
        {
            // JSON does not support comments, so emit "how to use this" as a fake string value
            textWriter.WriteLine("\"instructions_readme\": \"1) Open Chrome, 2) go to chrome://tracing, 3) click Load, 4) navigate to this file.\",\n");
        }

        static void EmitMetadata(TextWriter textWriter)
        {
            textWriter.WriteLine($"\"meta_datetime\": \"{Escape (DateTime.Now.ToString (CultureInfo.InvariantCulture))}\",");
            textWriter.WriteLine($"\"meta_command_line\": \"{Escape(Environment.CommandLine)}\",");
            textWriter.WriteLine($"\"meta_command_line_args\": \"{Escape (string.Join(" ", Environment.GetCommandLineArgs()))}\",");
            textWriter.WriteLine($"\"meta_user_name\": \"{Escape (Environment.UserName.ToString ())}\",");
            textWriter.WriteLine($"\"meta_os_version\": \"{Escape (Environment.OSVersion.ToString ())}\",");
            textWriter.WriteLine($"\"meta_cpu_count\": \"{Environment.ProcessorCount}\"");
        }

        public static string Escape(string val)
        {
            if (string.IsNullOrEmpty(val))
                return string.Empty;

            var sb = new StringBuilder(val.Length);
            for (var i = 0; i < val.Length; ++i)
            {
                char c = val[i];
                switch (c)
                {
                    case '"':   sb.Append("\\\""); break;
                    case '\\':  sb.Append("\\\\"); break;
                    case '\b':  sb.Append("\\b"); break;
                    case '\f':  sb.Append("\\f"); break;
                    case '\n':  sb.Append("\\n"); break;
                    case '\r':  sb.Append("\\r"); break;
                    case '\t':  sb.Append("\\t"); break;
                    default:
                        int cp = Convert.ToInt32(c);
                        if ((cp >= 32) && (cp <= 126))
                            sb.Append(c);
                        else
                            sb.Append("\\u" + Convert.ToString(cp, 16).PadLeft(4, '0'));
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
