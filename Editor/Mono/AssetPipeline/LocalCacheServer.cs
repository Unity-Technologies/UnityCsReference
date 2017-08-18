// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System.IO;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEditor.Scripting;
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
    internal class LocalCacheServer : ScriptableSingleton<LocalCacheServer>
    {
        [SerializeField] public string path;
        [SerializeField] public int port;
        [SerializeField] public ulong size;
        [SerializeField] public int pid = -1;
        [SerializeField] public string time;

        public const string SizeKey = "LocalCacheServerSize";
        public const string PathKey = "LocalCacheServerPath";
        public const string CustomPathKey = "LocalCacheServerCustomPath";

        public static string GetCacheLocation()
        {
            var cachePath = EditorPrefs.GetString(PathKey);
            var enableCustomPath = EditorPrefs.GetBool(CustomPathKey);
            var result = cachePath;
            if (!enableCustomPath || string.IsNullOrEmpty(cachePath))
                result = Paths.Combine(OSUtil.GetDefaultCachePath(), "CacheServer");
            return result;
        }

        public static void CreateCacheDirectory()
        {
            string cacheDirectoryPath = GetCacheLocation();
            if (Directory.Exists(cacheDirectoryPath) == false)
                Directory.CreateDirectory(cacheDirectoryPath);
        }

        void Create(int _port, ulong _size)
        {
            var nodeExecutable = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "nodejs");
            if (Application.platform == RuntimePlatform.WindowsEditor)
                nodeExecutable = Paths.Combine(nodeExecutable, "node.exe");
            else
                nodeExecutable = Paths.Combine(nodeExecutable, "bin", "node");

            CreateCacheDirectory();
            path = GetCacheLocation();
            var cacheServerJs = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "CacheServer", "main.js");
            var processStartInfo = new ProcessStartInfo(nodeExecutable)
            {
                Arguments = "\"" + cacheServerJs + "\""
                    + " --port " + _port
                    + " --path \"" + path
                    + "\" --nolegacy"
                    + " --monitor-parent-process " + Process.GetCurrentProcess().Id
                    // node.js has issues running on windows with stdout not redirected.
                    // so we silence logging to avoid that. And also to avoid CacheServer
                    // spamming the editor logs on OS X.
                    + " --silent"
                    + " --size " + _size,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var p = new Process();
            p.StartInfo = processStartInfo;
            p.Start();

            port = _port;
            pid = p.Id;
            size = _size;
            time = p.StartTime.ToString();
            Save(true);
        }

        public static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public static bool PingHost(string host, int port, int timeout)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(host, port, null, null);
                    result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout));
                    return client.Connected;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool WaitForServerToComeAlive(int port)
        {
            DateTime start = DateTime.Now;
            DateTime maximum = start.AddSeconds(5);
            while (DateTime.Now < maximum)
            {
                if (PingHost("localhost", port, 10))
                {
                    System.Console.WriteLine("Server Came alive after {0} ms", (DateTime.Now - start).TotalMilliseconds);
                    return true;
                }
            }
            return false;
        }

        public static void Kill()
        {
            if (instance.pid == -1)
                return;

            Process p = null;
            try
            {
                p = Process.GetProcessById(instance.pid);
                p.Kill();
                instance.pid = -1;
            }
            catch
            {
                // if we could not get a process, there is non alive. continue.
            }
        }

        public static void CreateIfNeeded()
        {
            // See if we can get an existing process with the PID we remembered.
            Process p = null;
            try
            {
                p = Process.GetProcessById(instance.pid);
            }
            catch
            {
                // if we could not get a process, there is non alive. continue.
            }

            ulong size = (ulong)EditorPrefs.GetInt(SizeKey, 10) * 1024 * 1024 * 1024;
            // Check if this process is really the one we used (and not another one reusing the PID).
            if (p != null && p.StartTime.ToString() == instance.time)
            {
                if (instance.size == size && instance.path == GetCacheLocation())
                {
                    // We have a server running for this setup, which we can reuse, but make sure that the cache server directory exists in case it was cleaned earlier
                    CreateCacheDirectory();
                    return;
                }
                else
                {
                    // This server does not match our setup. Kill it, so we can start a new one.
                    Kill();
                }
            }

            // No existing server we can use. Start a new one.
            instance.Create(GetRandomUnusedPort(), size);
            WaitForServerToComeAlive(instance.port);
        }

        public static void Setup()
        {
            var mode = (CacheServerPreferences.CacheServerMode)EditorPrefs.GetInt("CacheServerMode");

            if (mode == CacheServerPreferences.CacheServerMode.Local)
                CreateIfNeeded();
            else
                Kill();
        }

        [UsedByNativeCode]
        public static int GetLocalCacheServerPort()
        {
            Setup();
            return instance.port;
        }

        public static void Clear()
        {
            Kill();
            string cacheDirectoryPath = GetCacheLocation();
            if (Directory.Exists(cacheDirectoryPath))
                Directory.Delete(cacheDirectoryPath, true);
        }

        public static bool CheckCacheLocationExists()
        {
            return Directory.Exists(GetCacheLocation());
        }

        public static bool CheckValidCacheLocation(string path)
        {
            if (Directory.Exists(path))
            {
                var contents = Directory.GetFileSystemEntries(path);
                foreach (var dir in contents)
                {
                    var name = Path.GetFileName(dir).ToLower();
                    if (name.Length == 2)
                        continue;
                    if (name == "temp")
                        continue;
                    if (name == ".ds_store")
                        continue;
                    if (name == "desktop.ini")
                        continue;
                    return false;
                }
            }
            return true;
        }
    }
}
