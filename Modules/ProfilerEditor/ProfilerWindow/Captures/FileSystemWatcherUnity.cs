// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Profiling.Editor.UI
{
    internal class FileSystemWatcherUnity : IDisposable
    {
        internal enum State
        {
            None,
            Changed,
            DirectoryDeleted,
            Exception
        }

        public DirectoryInfo Directory { get; private set; }

        DateTime m_LastDirectoryWriteTimeUTC;
        public static double TimeBetweenDirectoryChecks => k_TimeBetweenDirectoryChecks;
        const double k_TimeBetweenDirectoryChecks = 5.0;

        Task m_Updater;
        CancellationTokenSource m_CancellationTokenSource;

        public FileSystemWatcherUnity(string path)
        {
            m_LastDirectoryWriteTimeUTC = DateTime.MinValue;
            Directory = new DirectoryInfo(path);

            if (!Directory.Exists)
                throw new DirectoryNotFoundException($"Directory not found: {path}");

            CheckDirectoryForUpdates(out var ex);
            if (ex != null)
                throw ex;
            m_CancellationTokenSource = new CancellationTokenSource();

            m_Updater = Task.Run(() => Update(m_CancellationTokenSource.Token), m_CancellationTokenSource.Token);
        }

        public event Action<State, object> Changed;

        public void Dispose()
        {
            m_CancellationTokenSource?.Cancel();
            if (m_Updater != null)
            {
                m_Updater.ContinueWith(new Action<Task>(t => Cleanup()));
            }
            else
            {
                Cleanup();
            }
        }

        void Cleanup()
        {
            m_CancellationTokenSource?.Dispose();
            m_CancellationTokenSource = null;
            m_Updater?.Dispose();
            m_Updater = null;
        }

        async void Update(CancellationToken token)
        {
            do
            {
                Refresh();
                if (token.IsCancellationRequested)
                    return;
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(k_TimeBetweenDirectoryChecks), token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
            while (!token.IsCancellationRequested);
        }

        public void Refresh()
        {
            var state = CheckDirectoryForUpdates(out var exception);
            switch (state)
            {
                case State.Changed:
                case State.DirectoryDeleted:
                case State.Exception:
                    Changed?.Invoke(state, exception);
                    break;
                case State.None:
                default:
                    break;
            }
        }

        State CheckDirectoryForUpdates(out Exception exception)
        {
            // FileSystemWatcher is only available in Windows, so instead of separate implementation for each platform,
            // we just do a manual watch for the folder.
            DateTime lastWriteTime;
            exception = null;

            try
            {
                Directory.Refresh();
            }
            catch (Exception ex)
            {
                // Directory.Refresh may throw a System.IO.IOException when:
                //     A device such as a disk drive is not ready.
                exception = ex;
                return State.Exception;
            }

            if (!Directory.Exists)
            {
                return State.DirectoryDeleted;
            }
            try
            {
                lastWriteTime = Directory.LastWriteTimeUtc;
                var iter = Directory.EnumerateDirectories("*", SearchOption.AllDirectories);
                foreach (var dir in iter)
                {
                    if (dir.LastWriteTimeUtc > lastWriteTime)
                    {
                        lastWriteTime = dir.LastWriteTimeUtc;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is DirectoryNotFoundException)
                {
                    try
                    {
                        Directory.Refresh();
                    }
                    catch (Exception innerEx)
                    {
                        // Directory.Refresh may throw a System.IO.IOException when:
                        //     A device such as a disk drive is not ready.
                        exception = innerEx;
                        return State.Exception;
                    }

                    if (Directory.Exists)
                    {
                        // The base directory seems to still exist, maybe a subfolder got deleted while iterating over it.
                        // Recheck
                        var state = CheckDirectoryForUpdates(out exception);
                        // Everything is fine on rechecking, return
                        if (state != State.Exception)
                            return state;
                    }
                    else
                        return State.DirectoryDeleted;
                }

                exception = ex;
                return State.Exception;
            }
            var dirty = lastWriteTime != m_LastDirectoryWriteTimeUTC;
            m_LastDirectoryWriteTimeUTC = lastWriteTime;
            return dirty ? State.Changed : State.None;
        }
    }
}
