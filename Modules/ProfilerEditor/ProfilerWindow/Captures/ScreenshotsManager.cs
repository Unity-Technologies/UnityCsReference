// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditorInternal;

namespace Unity.Profiling.Editor.UI
{
    internal class ScreenshotsManager : IDisposable
    {
        readonly struct Request
        {
            public readonly string FilePath;
            public readonly Texture2D Texture;
            public readonly Action CallbackOnUpdate;

            public Request(string filePath, Texture2D texture, Action callbackOnUpdate)
            {
                FilePath = filePath;
                Texture = texture;
                CallbackOnUpdate = callbackOnUpdate;
            }
        }

        readonly Queue<Request> m_Queue;
        readonly Dictionary<string, Texture2D> m_LoadedTextures;
        EditorCoroutine m_Loader;
        Texture2D m_TemporaryTexture;

        const int k_ThumbWidth = 256;
        const int k_ThumbHeight = 144;
        const string k_CaptureScreenshotFileExtension = ".png";
        const string k_CaptureScreenshotThumbExtension = ".bc7";
        const int k_WidthOrHeightDataSize = sizeof(int);
        const int k_ThumbWidthFileOffset = k_WidthOrHeightDataSize * 2;
        const int k_ThumbHeightFileOffset = k_WidthOrHeightDataSize;
        readonly string m_TemporaryScreenshotCachePath;

        public event Action<string> ScreenshotLoaded;

        public ScreenshotsManager()
        {
            EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            m_Queue = new Queue<Request>();
            m_TemporaryTexture = new Texture2D(1, 1, TextureFormat.RGB24, false);
            m_LoadedTextures = new Dictionary<string, Texture2D>();
            m_TemporaryScreenshotCachePath = Path.GetFullPath(Path.Combine(
                Application.temporaryCachePath, "ProfilerTempScreenshot" + k_CaptureScreenshotFileExtension));
        }

        static string ToScreenshotPath(string thePath)
        {
            return Path.ChangeExtension(thePath, k_CaptureScreenshotFileExtension);
        }

        static string ToThumbnailPath(string thePath)
        {
            return Path.ChangeExtension(thePath, k_CaptureScreenshotThumbExtension);
        }

        public static void CaptureDeleted(string capturePath)
        {
            var screenshotPath = ToScreenshotPath(capturePath);
            if (File.Exists(screenshotPath))
                File.Delete(screenshotPath);

            var thumbsPath = ToThumbnailPath(capturePath);
            if (File.Exists(thumbsPath))
                File.Delete(thumbsPath);
        }

        public static void CaptureRenamed(string captureFrom, string captureTo)
        {
            var sourceScreenshotPath = ToScreenshotPath(captureFrom);
            if (File.Exists(sourceScreenshotPath))
            {
                var targetScreenshotPath = ToScreenshotPath(captureTo);
                File.Move(sourceScreenshotPath, targetScreenshotPath);
            }

            var thumbPathFrom = ToThumbnailPath(captureFrom);
            if (File.Exists(thumbPathFrom))
            {
                var thumbPathTo = ToThumbnailPath(captureTo);
                File.Move(thumbPathFrom, thumbPathTo);
            }
        }

        public static void CaptureImported(string captureFrom, string captureTo)
        {
            var sourceScreenshotPath = ToScreenshotPath(captureFrom);
            if (File.Exists(sourceScreenshotPath))
            {
                var targetScreenshotPath = ToScreenshotPath(captureTo);
                File.Copy(sourceScreenshotPath, targetScreenshotPath);
            }

            var thumbPathFrom = ToThumbnailPath(captureFrom);
            if (File.Exists(thumbPathFrom))
            {
                var thumbPathTo = ToThumbnailPath(captureTo);
                File.Copy(thumbPathFrom, thumbPathTo);
            }
        }

        public Texture Enqueue(string fileName, Action callbackOnUpdate)
        {
            if (m_LoadedTextures.TryGetValue(fileName, out var texture))
                return texture;

            texture = new Texture2D(1, 1, TextureFormat.RGB24, false)
            {
                name = fileName,
                // Avoid the texture being unloaded on scene changes. Also, regardless of the hideflags, the ScreenshotManager is responsible for the destruction of this Texture.
                hideFlags = HideFlags.HideAndDontSave
            };
            m_LoadedTextures.Add(fileName, texture);

            var request = new Request(fileName, texture, callbackOnUpdate);
            m_Queue.Enqueue(request);

            m_Loader ??= EditorCoroutineUtility.StartCoroutine(ProcessRequest(), this);

            return texture;
        }

        public void Dispose()
        {
            if (m_Loader != null)
                EditorCoroutineUtility.StopCoroutine(m_Loader);

            // The manager created the textures, it is responsible for cleaning them up
            foreach (var queueItem in m_Queue)
            {
                UnityEngine.Object.DestroyImmediate(queueItem.Texture);
            }

            foreach (var loadedTexture in m_LoadedTextures.Values)
            {
                UnityEngine.Object.DestroyImmediate(loadedTexture);
            }

            EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            m_Queue.Clear();
            m_LoadedTextures.Clear();
        }

        IEnumerator ProcessRequest()
        {
            while (m_Queue.Count > 0)
            {
                yield return null;

                var request = m_Queue.Dequeue();

                var thumbPath = ToThumbnailPath(request.FilePath);

                if (File.Exists(thumbPath))
                {
                    byte[] data;
                    // guarding against hypothetical race conditions here in case the file gets deleted off of the main thread (or by another process)
                    try
                    {
                        data = File.ReadAllBytes(thumbPath);
                    }
                    catch (FileNotFoundException)
                    {
                        // a file in the queue was deleted or renamed, moving on...
                        continue;
                    }

                    var fileSize = data.Length;

                    // Read in width and height that we appended to the end of the file
                    var thumbW = BitConverter.ToInt32(data, fileSize - k_ThumbWidthFileOffset);
                    var thumbH = BitConverter.ToInt32(data, fileSize - k_ThumbHeightFileOffset);
                    Array.Resize(ref data, fileSize - k_WidthOrHeightDataSize);

                    request.Texture.Reinitialize(thumbW, thumbH, TextureFormat.BC7, false);
                    request.Texture.LoadRawTextureData(data);
                    request.Texture.Apply(false, true);
                }
                else
                {
                    byte[] dataOriginal;
                    // Read in the full size screenshot so we can make a thumbnail
                    // This has thrown an exception at least once while debugging and deleting a file via the context menu.
                    try
                    {
                        dataOriginal = File.ReadAllBytes(request.FilePath);
                    }
                    catch (FileNotFoundException)
                    {
                        // a file in the queue was deleted or renamed, moving on...
                        continue;
                    }

                    var fullSizeTex = new Texture2D(1, 1, TextureFormat.RGB24, false);
                    fullSizeTex.LoadImage(dataOriginal, false);
                    fullSizeTex.filterMode = FilterMode.Bilinear;

                    // Make a rendertexture with the same aspect ratio, scaled to fit within thumbnail bounds:
                    // First find out what ratio we're dealing with.
                    var screenshotAspect = (float)fullSizeTex.width / (float)fullSizeTex.height;
                    var thumbWidth = k_ThumbWidth;
                    var thumbHeight = k_ThumbHeight;

                    // Find our final shrunk width and height.
                    // Round the numbers so that our final dimensions are multiples of 4 for BC7 compression.
                    if (screenshotAspect > 16.0f / 9.0f)
                        thumbHeight = 4 * (int)((float)thumbWidth / (screenshotAspect * 4));
                    else
                        thumbWidth = 4 * (int)((float)(thumbHeight / 4) * screenshotAspect);

                    // Get a temp RT with the final dimensions and send our original image to it
                    var tempRt = RenderTexture.GetTemporary(thumbWidth, thumbHeight);
                    tempRt.filterMode = FilterMode.Bilinear;
                    RenderTexture.active = tempRt;
                    Graphics.Blit(fullSizeTex, tempRt);

                    // Read the final texture back from the RT and compress to BC7.
                    request.Texture.Reinitialize(thumbWidth, thumbHeight);
                    request.Texture.ReadPixels(new Rect(0, 0, thumbWidth, thumbHeight), 0, 0);
                    EditorUtility.CompressTexture(request.Texture, TextureFormat.BC7, TextureCompressionQuality.Fast);

                    // Write out the compressed raw data to file + the dimensions.
                    using (var fStream = new FileStream(thumbPath, FileMode.Append))
                    {
                        var rawData = request.Texture.GetRawTextureData();
                        fStream.Write(rawData, 0, rawData.Length);
                        fStream.Write(BitConverter.GetBytes(thumbWidth), 0, k_WidthOrHeightDataSize);
                        fStream.Write(BitConverter.GetBytes(thumbHeight), 0, k_WidthOrHeightDataSize);
                    }

                    File.SetAttributes(thumbPath, File.GetAttributes(thumbPath) | FileAttributes.Hidden);

                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(tempRt);
                    UnityEngine.Object.DestroyImmediate(fullSizeTex);
                }

                request.Texture.name = request.FilePath;
                request.CallbackOnUpdate?.Invoke();
                ScreenshotLoaded?.Invoke(request.FilePath);
            }

            EditorCoroutineUtility.StopCoroutine(m_Loader);
            m_Loader = null;
        }

        internal bool ReadInOrReset(string path)
        {
            var screenshotPath = Path.ChangeExtension(path, k_CaptureScreenshotFileExtension);
            if (File.Exists(screenshotPath))
                return ReadInTempScreenshot(screenshotPath);

            ResetTemporaryScreenshot();
            return false;
        }

        internal void ResetTemporaryScreenshot()
        {
            // TODO: Add a default "blank" image, rather than an empty one
            if (m_TemporaryTexture != null)
                m_TemporaryTexture.Reinitialize(1, 1, TextureFormat.RGB24, false);
        }

        bool WriteOutTempScreenshot(string path)
        {
            // Don't write out an empty texture
            if (m_TemporaryTexture == null || m_TemporaryTexture.width <= 1 || m_TemporaryTexture.height <= 1)
                return false;

            path = Path.ChangeExtension(path, k_CaptureScreenshotFileExtension);
            if (File.Exists(path))
                return false;

            File.WriteAllBytes(path, m_TemporaryTexture.EncodeToPNG());
            return true;
        }

        bool ReadInTempScreenshot(string path)
        {
            if (!File.Exists(path))
                return false;

            var data = File.ReadAllBytes(path);

            if (m_TemporaryTexture == null)
                m_TemporaryTexture = new Texture2D(1, 1, TextureFormat.RGB24, false);
            m_TemporaryTexture.LoadImage(data, false);

            return true;
        }

        static class TextureInfoElem
        {
            public const int TextureFormat = 0;
            public const int Width = 1;
            public const int Height = 2;
            public const int TotalElems = 3;
        }

        internal bool WriteOutMostRecentScreenshot(string filePath, int frameIndex)
        {
            if (frameIndex > ProfilerDriver.lastFrameIndex || frameIndex < ProfilerDriver.firstFrameIndex || frameIndex < 0)
                return false;

            using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
            {
                var texInfo = frameData.GetFrameMetaData<int>(ProfilerDriver.profilerInternalSessionMetaDataGuid,
                    (int)ProfilingSessionMetaDataEntry.ScreenshotTextureInfo);

                if (texInfo.Length == TextureInfoElem.TotalElems)
                {
                    var data = frameData.GetFrameMetaData<byte>(ProfilerDriver.profilerInternalSessionMetaDataGuid,
                        (int)ProfilingSessionMetaDataEntry.ScreenshotRawTextureData);

                    var format = (TextureFormat)texInfo[TextureInfoElem.TextureFormat];
                    var width = texInfo[TextureInfoElem.Width];
                    var height = texInfo[TextureInfoElem.Height];

                    if (data.Length > 1)
                    {
                        if (m_TemporaryTexture == null)
                            m_TemporaryTexture = new Texture2D(width, height, format, false);
                        else
                            m_TemporaryTexture.Reinitialize(width, height, format, false);
                        CopyDataToTexture(m_TemporaryTexture, data);
                        return WriteOutTempScreenshot(filePath);
                    }
                }
                else
                {
                    // If we didn't find screenshot data, try and find the frame
                    // that most recently had a screenshot.
                    var framesSinceLast = frameData.GetFrameMetaData<int>(ProfilerDriver.profilerInternalSessionMetaDataGuid,
                        (int)ProfilingSessionMetaDataEntry.FramesSinceLastScreenshot);

                    if (framesSinceLast.Length == 1 && framesSinceLast[0] >= 0)
                    {
                        var prevFrameToTry = frameIndex - framesSinceLast[0];
                        if (prevFrameToTry < frameIndex)
                            return WriteOutMostRecentScreenshot(filePath, prevFrameToTry);
                    }
                }
            }

            return false;
        }

        void CopyDataToTexture(Texture2D tex, NativeArray<byte> byteArray)
        {
            unsafe
            {
                var srcPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(byteArray);
                var dstPtr = tex.GetRawTextureData<byte>().GetUnsafeReadOnlyPtr();
                UnsafeUtility.MemCpy(dstPtr, srcPtr, byteArray.Length * sizeof(byte));
            }
        }

        // The temporary texture can get deleted without warning, so save it to disk if it looks like that's about to happen.
        void OnBeforeAssemblyReload()
        {
            WriteOutTempScreenshot(m_TemporaryScreenshotCachePath);
        }

        void OnAfterAssemblyReload()
        {
            ReadInTempScreenshot(m_TemporaryScreenshotCachePath);
        }

        void OnPlaymodeStateChanged(PlayModeStateChange playModeState)
        {
            switch (playModeState)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    WriteOutTempScreenshot(m_TemporaryScreenshotCachePath);
                    break;
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    ReadInTempScreenshot(m_TemporaryScreenshotCachePath);
                    break;
            }
        }
    }
}
