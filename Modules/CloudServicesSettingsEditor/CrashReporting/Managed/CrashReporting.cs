// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor.Connect;
using UnityEditor.Utils;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.CrashReporting
{
    internal class CrashReporting
    {
        public static string ServiceBaseUrl
        {
            get
            {
                string configUrl = CrashReportingSettings.GetEventUrl();
                if (!string.IsNullOrEmpty(configUrl))
                {
                    return configUrl;
                }

                return string.Empty;
            }
        }

        public static string SignedUrlSourceUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(ServiceBaseUrl))
                {
                    return new Uri(new Uri(ServiceBaseUrl), "url").ToString();
                }

                return string.Empty;
            }
        }

        public static string ServiceTokenUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(ServiceBaseUrl) && !string.IsNullOrEmpty(CloudProjectSettings.projectId))
                {
                    return new Uri(new Uri(ServiceBaseUrl), string.Format("token/{0}", CloudProjectSettings.projectId)).ToString();
                }

                return string.Empty;
            }
        }

        public static string GetUsymUploadAuthToken()
        {
            string token = string.Empty;
            var originalCallback = ServicePointManager.ServerCertificateValidationCallback;

            try
            {
                string environmentValue = Environment.GetEnvironmentVariable("USYM_UPLOAD_AUTH_TOKEN");
                if (!string.IsNullOrEmpty(environmentValue))
                {
                    return environmentValue;
                }

                CrashReportingSettings.EnsureConnectReady();

                string accessToken = UnityConnect.instance.GetAccessToken();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return string.Empty;
                }

                // Only OSX supports SSL certificate validation, disable checking on other platforms.
                // Fix when a Unity Web framework supports SSL.
                if (Application.platform != RuntimePlatform.OSXEditor)
                    ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;

                WebRequest request = WebRequest.Create(ServiceTokenUrl);
                request.Timeout = 15000; // 15 Seconds
                request.Headers.Add("Authorization", string.Format("Bearer {0}", accessToken));
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                string tokenJson = string.Empty;
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    tokenJson = sr.ReadToEnd();
                }
                JSONValue parsedJson = JSONParser.SimpleParse(tokenJson);
                if (parsedJson.ContainsKey("AuthToken"))
                {
                    token = parsedJson["AuthToken"].AsString();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarningFormat("Exception occurred attempting to connect to Unity Cloud Diagnostics service.  Native symbols will not be uploaded for this build. Exception details:\n{0}\n{1}", ex.ToString(), ex.StackTrace);
            }

            ServicePointManager.ServerCertificateValidationCallback = originalCallback;

            return token;
        }

        private class UploadPlatformConfig
        {
            public string UsymtoolPath;
            public string LzmaPath;
            public string LogFilePath;
        }

        static UploadPlatformConfig GetUploadPlatformConfig()
        {
            UploadPlatformConfig config = new UploadPlatformConfig();

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                config.UsymtoolPath = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "usymtool.exe");
                config.LzmaPath = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "lzma.exe");
                config.LogFilePath = Paths.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", "symbol_upload.log");
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                string usymtoolFileName = RuntimeInformation.ProcessArchitecture.ToString().ToLower() == "arm64" ? "usymtoolarm64" :"usymtool";
                config.UsymtoolPath = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "macosx", usymtoolFileName);
                config.LzmaPath = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "lzma");
                config.LogFilePath = Paths.Combine(Environment.GetEnvironmentVariable("HOME"), "Library", "Logs", "Unity", "symbol_upload.log");
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor)
            {
                config.UsymtoolPath = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "usymtool");
                config.LzmaPath = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "lzma-linux64");
                config.LogFilePath = Paths.Combine(Environment.GetEnvironmentVariable("HOME"), ".config", "unity3d", "symbol_upload.log");
            }

            return config;
        }

        public static void UploadSymbolsInPath(string authToken, string symbolPath, string il2cppOutputPath, string il2cppFileRoot, string includeFilter, string excludeFilter, bool waitForExit)
        {
            try
            {
                UploadPlatformConfig platformConfig = GetUploadPlatformConfig();

                string args = string.Format("-symbolPath \"{0}\" -log \"{1}\"",
                    symbolPath, platformConfig.LogFilePath);

                if (!String.IsNullOrEmpty(includeFilter))
                {
                    args = string.Format("{0} -filter \"{1}\"", args, includeFilter);
                }

                if (!String.IsNullOrEmpty(excludeFilter))
                {
                    args = string.Format("{0} -excludeFilter \"{1}\"", args, excludeFilter);
                }

                if (!String.IsNullOrEmpty(il2cppOutputPath))
                {
                    args = string.Format("{0} -il2cppOutputPath \"{1}\"", args, il2cppOutputPath);
                }

                if (!String.IsNullOrEmpty(il2cppFileRoot))
                {
                    args = string.Format("{0} -il2cppFileRoot \"{1}\"", args, il2cppFileRoot);
                }

                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo()
                {
                    Arguments = args,
                    CreateNoWindow = true,
                    FileName = platformConfig.UsymtoolPath,
                    WorkingDirectory = Directory.GetParent(Application.dataPath).FullName,
                    UseShellExecute = false
                };

                psi.EnvironmentVariables["USYM_UPLOAD_AUTH_TOKEN"] = authToken;
                psi.EnvironmentVariables["USYM_UPLOAD_URL_SOURCE"] = SignedUrlSourceUrl;
                psi.EnvironmentVariables["LZMA_PATH"] = platformConfig.LzmaPath;

                System.Diagnostics.Process nativeProgram = new System.Diagnostics.Process();
                nativeProgram.StartInfo = psi;

                nativeProgram.Start();

                if (waitForExit)
                {
                    nativeProgram.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarningFormat("Exception occurred attempting to upload symbols to Unity Cloud Diagnostics service.  Native symbols will not be available for this build. Exception details:\n{0}\n{1}", ex.ToString(), ex.StackTrace);
            }
        }
    }
}
