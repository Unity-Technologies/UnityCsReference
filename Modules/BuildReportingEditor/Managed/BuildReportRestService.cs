// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

namespace UnityEditor
{
    internal class BuildReportRestService : ScriptableSingleton<BuildReportRestService>
    {
        public CancellationTokenSource m_cts;

        public HttpListener m_listener;
        public TaskScheduler m_mainThreadScheduler;
        public Thread m_listenerThread;

        public static Regex regex = new Regex(@"/unity/build-report/(?<reportid>\w+?)/(?<request>\w+)(?<args>(?:/\w*)+/?)?$");
        public static Regex regexArgs = new Regex(@"/(?<type>\w+?)(?:/(?<index>\w+?))?(?:/(?<method>\w+?))?$");
        public static BuildReport GetReport(string reportId)
        {
            if (reportId == "latest")
                return BuildReport.GetLatestReport();

            return BuildReport.GetReport(new GUID(reportId));
        }

        public BuildReportRestService()
        {
        }

        public string ProcessRequest(BuildReport report, string request, string args, HttpListenerContext context)
        {
            if (request == "report")
                return EditorJsonUtility.ToJson(report);

            if (request == "summary")
                return BuildReportRestAPI.GetSummaryResponse(report);

            if (request == "steps")
                return BuildReportRestAPI.GetStepsResponse(report);

            int depth = 0;

            try {
                depth = Int32.Parse(context.Request.QueryString["depth"]);
            } catch {}; // No ?depth=X

            if (request == "assets")
                return BuildReportRestAPI.GetAssetsResponse(report, args, depth);

            if (request == "files")
                return BuildReportRestAPI.GetFilesResponse(report, args, depth);

            if (request == "appendices")
            {
                Match matchArgs = regexArgs.Match(args);

                if (matchArgs.Success)
                {
                    var type = matchArgs.Groups["type"];
                    var index = matchArgs.Groups["index"];
                    var method = matchArgs.Groups["method"];

                    if (type.Success && index.Success && method.Success)
                    {
                        string postData;
                        using (var reader = new StreamReader(context.Request.InputStream,
                                                             context.Request.ContentEncoding))
                        {
                            postData = reader.ReadToEnd();
                        }

                        return BuildReportRestAPI.GetAppendicesResponseWithMethod(report, type.ToString(), int.Parse(index.ToString()), method.ToString(), postData);
                    }

                    if (type.Success && index.Success)
                        return BuildReportRestAPI.GetAppendicesResponseWithIndex(report, type.ToString(), int.Parse(index.ToString()));

                    if (type.Success)
                        return BuildReportRestAPI.GetAppendicesResponse(report, type.ToString());
                }
            }

            return "{}";
        }

        [InitializeOnLoadMethod]
        public static void BootStrapper()
        {
            BuildReportRestService.instance.Boot();
        }

        public void RunServer()
        {
            m_listener = new HttpListener();
            m_listener.Prefixes.Add("http://localhost:38000/unity/build-report/");
            m_listener.Prefixes.Add("http://127.0.0.1:38000/unity/build-report/");
            m_listener.Start();

            while(!m_cts.IsCancellationRequested)
            {
                try{

                     HttpListenerContext context = m_listener.GetContext();

                     // We are only accepting local requests for the security reasons.
                     if (!context.Request.IsLocal)
                         continue;

                     var host = context.Request.Headers["Host"];

                     // Protection from the DNS rebinding attacks (https://en.wikipedia.org/wiki/DNS_rebinding)
                     if ( host != "localhost:38000" && host != "127.0.0.1:38000")
                         continue;

                     var split = context.Request.RawUrl.IndexOf("?");
                     string uri = split < 0 ? context.Request.RawUrl : context.Request.RawUrl.Substring(0, split);

                     Match match = regex.Match(uri);

                     string response = "{}";

                     if (match.Success)
                     {

                        Task t = new Task(() =>
                        {
                            var report     = GetReport(match.Groups["reportid"].ToString());
                            string request = match.Groups["request"].ToString();
                            string args     = match.Groups["args"]?.ToString();
                            response = ProcessRequest(report, request, args, context);
                        });

                        t.Start(m_mainThreadScheduler);

                        m_cts = new CancellationTokenSource();
                        t.Wait(m_cts.Token);
                     }


                     byte[] buffer= System.Text.Encoding.UTF8.GetBytes(response);

                     // Get a response stream and write the response to it.
                     context.Response.ContentLength64 = buffer.Length;
                     System.IO.Stream output = context.Response.OutputStream;
                     output.Write(buffer,0,buffer.Length);
                     output.Close();

                }
                catch(TaskCanceledException)
                {}
                catch(HttpListenerException)
                {}
                catch(Exception e)
                {
                    Debug.Log(e.Message);
                }
            }

            m_listener = null;
        }

        public void RunServerWithRetries(int retries)
        {
            for (int i=0; i<=retries; ++i)
            {
                try{
                    RunServer();
                    return;
                }
                catch(SocketException)
                {
                    // We had an instances where on our infrastructure domain reload happened
                    // and socket is still not free. In such a case we are retrying this multiple
                    // times before giving up on starting server.
                    Thread.Sleep(250);
                }
                catch(Exception e)
                {
                    throw e;
                }
            }
        }

        public void Boot() {}

        public void OnEnable()
        {
            m_mainThreadScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            m_cts = new CancellationTokenSource();
            m_listenerThread = new Thread(()=>RunServerWithRetries(20));
            m_listenerThread.Start();
        }

        public void OnDisable()
        {
            m_cts.Cancel();
            m_listener.Abort();
            m_listenerThread.Join();
            m_cts.Dispose();
        }
    }
}
