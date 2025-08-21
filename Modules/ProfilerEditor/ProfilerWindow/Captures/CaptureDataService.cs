// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;

namespace Unity.Profiling.Editor.UI
{
    interface ICaptureDataService
    {
        void SetCapturesFolderDirty();
        string GetCaptureFolderPath();
    }

    internal class CaptureDataService : IDisposable, ICaptureDataService
    {
        const string k_FileExtensionData = ".data";
        const string k_FileExtensionRaw = ".raw";

        readonly ProfilerWindow m_ProfilerWindow;
        CaptureFileListModel m_CaptureFileListModel;
        AsyncWorker<CaptureFileListModel> m_BuildAllCapturesWorker;
        FileSystemWatcherUnity m_CaptureFolderWatcher;
        bool m_CaptureFolderIsDirty;
        bool m_CaptureFolderWasDeleted;
        [NonSerialized]
        bool m_Disposed;

        public bool CaptureListModelIsUpToDate
        {
            get
            {
                if (m_CaptureFolderIsDirty || m_CaptureFileListModel == null || m_CaptureFolderWatcher == null)
                {
                    return false;
                }
                m_CaptureFolderWatcher.Refresh();
                return m_CaptureFileListModel.CaptureDirectoryLastWriteTimestampUtc >= m_CaptureFolderWatcher.Directory.LastWriteTimeUtc;
            }
        }

        public CaptureDataService(ProfilerWindow profilerWindow)
        {
            m_ProfilerWindow = profilerWindow;
            var allCaptures = new List<CaptureFileModel>();
            // Build an empty list model
            var listBuilder = new CaptureFileListModelBuilder(allCaptures, DateTime.MinValue);
            m_CaptureFileListModel = listBuilder.Build();

            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            ProfilerUserSettings.CaptureStoragePathChanged += SetupCaptureFolderWatcher;

            SetupCaptureFolderWatcher();
            SyncCapturesFolder();
        }

        void Update()
        {
            if (m_Disposed)
            {
                // Disposing might happen while the Update calls are being cycled through,
                // so the previous deregistration might not have affected the list of event subscribers currently being called yet.
                // Just for good measure, deregister again here though.
                EditorApplication.update -= Update;
                return;
            }

            if (m_CaptureFolderWasDeleted)
            {
                // delayed folder restoring on main thread
                m_CaptureFolderWasDeleted = false;
                SetupCaptureFolderWatcher();
            }

            if (m_CaptureFolderIsDirty)
            {
                SyncCapturesFolder();
            }
        }

        void DirectoryChanged(FileSystemWatcherUnity.State state, object exception)
        {
            switch (state)
            {
                case FileSystemWatcherUnity.State.None:
                    break;
                case FileSystemWatcherUnity.State.Changed:
                    SetCapturesFolderDirty();
                    break;
                case FileSystemWatcherUnity.State.DirectoryDeleted:
                    // Delay setting up a new watcher to the Update cycle so it happens on the main thread
                    // Otherwise this method (as it's called by the task worker thread) will silently fail in EditorPrefs.GetString when fetching the directory to recreate
                    m_CaptureFolderWasDeleted = true;

                    break;
                case FileSystemWatcherUnity.State.Exception:
                    Debug.LogException(exception as Exception);
                    SetupCaptureFolderWatcher();
                    break;
                default:
                    break;
            }
        }

        void SetupCaptureFolderWatcher()
        {
            var path = GetOrCreateCaptureFolderPath();

            m_CaptureFolderWatcher?.Dispose();
            m_CaptureFolderWatcher = new FileSystemWatcherUnity(path);
            m_CaptureFolderWatcher.Changed += DirectoryChanged;

            // Always assume the directory is dirty after setup
            m_CaptureFolderIsDirty = true;
        }

        public IReadOnlyList<CaptureFileModel> AllCaptures => m_CaptureFileListModel.AllCaptures;
        public IReadOnlyDictionary<uint, string> SessionNames => m_CaptureFileListModel.SessionNames;
        public CaptureFileListModel FullCaptureList => m_CaptureFileListModel;

        public event Action LoadedCapturesChanged;
        public event Action AllCapturesChanged;

        public void Load(bool keepExisting, string filePath)
        {
            m_ProfilerWindow.LoadProfilingData(keepExisting, filePath, true);
            LoadedCapturesChanged?.Invoke();
        }

        public bool ValidateName(string fileName)
        {
            return fileName.IndexOfAny(Path.GetInvalidFileNameChars()) == -1;
        }

        public bool CanRename(string sourceFilePath, string targetFileName)
        {
            bool isRaw = sourceFilePath.EndsWith(k_FileExtensionRaw);
            var targetFilePath = Path.Combine(
                Path.GetDirectoryName(sourceFilePath), targetFileName + (isRaw ? k_FileExtensionRaw : k_FileExtensionData));
            if (File.Exists(targetFilePath))
                return false;

            return true;
        }

        public void Rename(string sourceFilePath, string targetFileName)
        {
            bool isRaw = sourceFilePath.EndsWith(k_FileExtensionRaw);
            var targetFilePath = Path.Combine(
                Path.GetDirectoryName(sourceFilePath), targetFileName + (isRaw ? k_FileExtensionRaw : k_FileExtensionData));
            if (File.Exists(targetFilePath))
            {
                Debug.LogError($"Can't rename {sourceFilePath} to {targetFileName}, file with the same name is already exist!");
                return;
            }

            ScreenshotsManager.CaptureRenamed(sourceFilePath, targetFilePath);
            File.Move(sourceFilePath, targetFilePath);
            var bottleneckSource = Path.ChangeExtension(sourceFilePath, BottlenecksChartViewModel.k_HighlightFileExtension);
            if (File.Exists(bottleneckSource))
                File.Move(bottleneckSource, Path.ChangeExtension(targetFilePath, BottlenecksChartViewModel.k_HighlightFileExtension));

            SyncCapturesFolder();
        }

        public bool Import(string filePath)
        {
            var ret = ImportCapture(filePath);
            SyncCapturesFolder();

            return ret;
        }

        public void Delete(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            ScreenshotsManager.CaptureDeleted(filePath);
            File.Delete(filePath);

            var bottleneckPath = Path.ChangeExtension(filePath, BottlenecksChartViewModel.k_HighlightFileExtension);
            if (File.Exists(bottleneckPath))
                File.Delete(bottleneckPath);

            SyncCapturesFolder();
        }

        public void SetCapturesFolderDirty()
        {
            m_CaptureFolderIsDirty = true;
        }

        public void SyncCapturesFolder()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CaptureDataService));

            m_BuildAllCapturesWorker?.Dispose();
            m_BuildAllCapturesWorker = null;

            m_BuildAllCapturesWorker = new AsyncWorker<CaptureFileListModel>();
            // Check and store updated state
            m_CaptureFolderWatcher.Refresh();
            // grab a copy of the directory so that a directory change on main thread will not bleed into the worker thread
            var CaptureDirectory = m_CaptureFolderWatcher.Directory;
            // pre-declare directory as clean, because it will be once the worker is done
            m_CaptureFolderIsDirty = false;

            m_BuildAllCapturesWorker.Execute((token) =>
            {
                try
                {
                    return BuildCapturesInfo(token, CaptureDirectory, AllCaptures);
                }
                catch (TaskCanceledException)
                {
                    // We expect a TaskCanceledException to be thrown when cancelling an in-progress builder. Do not log an error to the console.
                    return null;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return null;
                }
            }, (result) =>
            {
                // Dispose asynchronous worker.
                m_BuildAllCapturesWorker?.Dispose();
                m_BuildAllCapturesWorker = null;

                // Update on success
                if (result != null)
                {
                    if (result.Equals(m_CaptureFileListModel))
                    {
                        m_CaptureFileListModel.UpdateTimeStamp(result.CaptureDirectoryLastWriteTimestampUtc);
                    }
                    else
                    {
                        // only really update if there is an actual change
                        m_CaptureFileListModel = result;
                        AllCapturesChanged?.Invoke();
                    }
                }
            });
        }

        static CaptureFileListModel BuildCapturesInfo(CancellationToken token, DirectoryInfo CapturesDirectory, IReadOnlyList<CaptureFileModel> CaptureInfos)
        {
            var CapturesMap = new Dictionary<string, CaptureFileModel>(CaptureInfos.Count);
            foreach (var captureFilemodel in CaptureInfos)
            {
                CapturesMap[captureFilemodel.Name] = captureFilemodel;
            }

            var allCaptures = new List<CaptureFileModel>();
            foreach (var CaptureFile in GetCaptureFiles(CapturesDirectory))
            {
                if (token.IsCancellationRequested)
                    return null;
                if (!CapturesMap.TryGetValue(CaptureFile, out var CapturesFile))
                {
                    var builder = new CaptureFileModelBuilder(CaptureFile);
                    CapturesFile = builder.Build();
                }

                if (CapturesFile != null)
                    allCaptures.Add(CapturesFile);
            }

            if (token.IsCancellationRequested)
                return null;

            var CaptureFileListBuilder = new CaptureFileListModelBuilder(allCaptures, CapturesDirectory.LastWriteTimeUtc);
            var CaptureFileListModel = CaptureFileListBuilder.Build();
            return CaptureFileListModel;
        }

        static IEnumerable<string> GetCaptureFiles(DirectoryInfo directory)
        {
            try
            {
                if (!directory.Exists)
                    yield break;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to fetch Captures from: {directory.FullName}\n{e}");
                yield break;
            }

            var filesEnumData = directory.EnumerateFiles('*' + k_FileExtensionData, SearchOption.AllDirectories);
            var filesEnumRaw = directory.EnumerateFiles('*' + k_FileExtensionRaw, SearchOption.AllDirectories);
            foreach (var file in filesEnumData)
                yield return file.FullName;
            foreach (var file in filesEnumRaw)
                yield return file.FullName;
        }

        bool ImportCapture(string sourceFilePath)
        {
            var CaptureFolderPath = GetCaptureFolderPath();
            bool isRaw = sourceFilePath.EndsWith(k_FileExtensionRaw);
            var targetFilePath = Path.Combine(
                CaptureFolderPath, Path.GetFileNameWithoutExtension(sourceFilePath) + (isRaw ? k_FileExtensionRaw : k_FileExtensionData));
            if (File.Exists(targetFilePath))
                return false;

            try
            {
                ScreenshotsManager.CaptureImported(sourceFilePath, targetFilePath);
                File.Copy(sourceFilePath, targetFilePath);
                var bottleneckSource = Path.ChangeExtension(sourceFilePath, BottlenecksChartViewModel.k_HighlightFileExtension);
                if (File.Exists(bottleneckSource))
                    File.Copy(bottleneckSource, Path.ChangeExtension(targetFilePath, BottlenecksChartViewModel.k_HighlightFileExtension));
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to import capture from: \"{sourceFilePath}\": {e}");
            }

            return true;
        }

        public void Dispose()
        {
            m_Disposed = true;
            // Unload without notify
            LoadedCapturesChanged = null;

            m_BuildAllCapturesWorker?.Dispose();
            m_BuildAllCapturesWorker = null;

            m_CaptureFolderWatcher?.Dispose();
            m_CaptureFolderWatcher = null;

            EditorApplication.update -= Update;
            ProfilerUserSettings.CaptureStoragePathChanged -= SetupCaptureFolderWatcher;
        }

        public string GetCaptureFolderPath()
        {
            if (m_CaptureFolderWatcher != null && m_CaptureFolderWatcher.Directory != null)
            {
                m_CaptureFolderWatcher.Directory.Refresh();
                if (m_CaptureFolderWatcher.Directory.Exists && m_CaptureFolderWatcher.Directory.FullName == ProfilerUserSettings.AbsoluteProfilerCaptureStoragePath)
                    return m_CaptureFolderWatcher.Directory.FullName;
            }
            SetupCaptureFolderWatcher();
            return m_CaptureFolderWatcher.Directory.FullName;
        }

        string GetOrCreateCaptureFolderPath()
        {
            var CaptureFolderPath = ProfilerUserSettings.AbsoluteProfilerCaptureStoragePath;
            if (!Directory.Exists(CaptureFolderPath) && ProfilerUserSettings.UsingDefaultProfilerCaptureStoragePath())
            {
                // If the path points to the default folder but doesn't exist, create it.
                // We don't create non default path folders though. As that could lead to unexpected behaviour for the User.
                Directory.CreateDirectory(CaptureFolderPath);
            }
            return CaptureFolderPath;
        }
    }
}
